import hashlib
import os
from typing import Union
import time  # Used for timing.

import numpy as np
from fastapi.encoders import jsonable_encoder
from fastapi import FastAPI, HTTPException, Request, Body
from fastapi.responses import JSONResponse
from pydantic import BaseModel

import clusters
from clusters.dbscan_calibration import select_dbscan_parameters
from dynamic_detection import detect_script2
from dynamic_detection_agent_service import DynamicDetectionAgentParams, detect_scene_scripts_agent
from middleware.request_gc import GCMiddleware
from middleware.request_gzip import GZipRequestMiddleware, DecompressRequestMiddleware
from utils import dataset

# Configure logging.
import logging_config  # noqa: F401
from loguru import logger

app = FastAPI()
app.add_middleware(DecompressRequestMiddleware)
app.add_middleware(GCMiddleware, request_matcher=["/scene_division"])


@app.get("/")
def read_root():
    return {"Hello": "World"}


class SceneDivisionParams(BaseModel):
    scene_info_path: str | None = None
    scene_info: str | None = None
    method: str = "DBSCAN"
    extra: Union[dict, None] = None


def load_scene_info(params: SceneDivisionParams) -> dict:
    if params.scene_info_path:
        logger.info(f"Loading scene info from {params.scene_info_path}")
        return dataset.load_scene_json_file(params.scene_info_path)
    elif params.scene_info:
        logger.info("Loading scene info from provided data")
        import orjson
        return orjson.loads(params.scene_info)
    else:
        raise ValueError("Either scene_info_path or scene_info must be provided.")


def parse_int_sequence(value) -> list[int] | None:
    if value is None:
        return None
    if isinstance(value, int):
        return [value]
    if isinstance(value, str):
        return [int(item) for item in value.split(",") if item.strip()]
    return [int(item) for item in value]


@app.post("/data_exists")
def data_exists(sha256: str):
    logger.info("data_exists: sha256={}", sha256)
    path = os.path.join("cache/data", sha256 + ".bin")
    return {"success": True, "exists": os.path.exists(path)}


@app.post("/upload_data")
def upload(data: bytes = Body(...)):
    logger.info("upload: data size={}", len(data))
    sha256 = hashlib.sha256(data).hexdigest()
    path = "cache/data"
    if not os.path.exists(path):
        os.makedirs(path)
    file_path = os.path.join(path, sha256 + ".bin")
    with open(file_path, "wb") as f:
        f.write(data)
    return {"success": True, "sha256": sha256}


@app.post("/scene_division")
def scene_division(params: SceneDivisionParams):
    timestamp = time.monotonic()
    logger.info("scene_division: scene_info_path={} method={}, extra={}", params.scene_info_path, params.method,
                params.extra)
    params.method = params.method.upper()
    extra = params.extra or {}
    scene_info = load_scene_info(params)
    downsample_step = extra.get("downsample_step") or 0.5

    if "VOXEL" in params.method:
        data_v, data_id = dataset.load_voxels(
            scene_info,
            downsample_step=downsample_step
        )
    else:
        data_v, data_id = dataset.load_verticals(
            scene_info,
            downsample_step=downsample_step
        )


    if "DBSCAN" in params.method:
        eps = extra.get("eps") or 2
        min_samples = extra.get("min_samples") or 10
        if extra.get("auto_calibration") is True:
            calibration = select_dbscan_parameters(
                data_v,
                data_id,
                step=downsample_step,
                pipeline=extra.get("auto_calibration_pipeline") or "legacy_xz",
                seed=int(extra.get("auto_calibration_seed") or extra.get("seed") or 42),
                sample_for_knee=int(extra.get("sample_for_knee") or 50_000),
                min_samples_candidates=parse_int_sequence(extra.get("min_samples_candidates")),
                progress=lambda message: logger.info("dbscan auto calibration: {}", message),
            )
            eps = calibration["eps"]
            min_samples = calibration["min_samples"]
            logger.info(
                "dbscan auto calibration selected eps={} min_samples={} score={:.3f} "
                "clusters={} raw_noise={:.2%} post_noise={:.2%} largest={:.2%}",
                eps,
                min_samples,
                calibration["score"],
                calibration["clusters"],
                calibration["noise_ratio"],
                calibration["post_noise_ratio"],
                calibration["largest_ratio"],
            )
        labels = clusters.dbscan(
            data_v, data_id, eps=eps,
            min_samples=min_samples,
            metric="euclidean"
        )
    elif "KMEANS" in params.method:
        labels = clusters.kmeans(
            data_v, data_id, n_clusters=extra.get("num_clusters") or 5
        )
    elif "GRID" in params.method:
        num_slices = extra.get("num_slices")
        num = [num_slices, 1, num_slices] if num_slices is not None else [4, 1, 4]
        labels = clusters.grid(data_v, data_id, num=np.array(num))
    else:
        raise ValueError("Unsupported method.")

    result = dataset.group_object_by_labels(data_id, labels)

    logger.info("scene_division: method={} time_cost={:.2f}s", params.method, time.monotonic() - timestamp)

    return jsonable_encoder(result, custom_encoder={np.int64: int, np.float64: float})


class DynamicDetectionParams(BaseModel):
    script_name: str
    script: str
    use_cache: bool = True


@app.post("/dynamic_detection")
async def dynamic_detection(params: DynamicDetectionParams):
    logger.info("start script={}", params.script_name)
    start_time = time.monotonic()
    retry_count = 3
    result = None  # Initialize the result.
    while retry_count > 0:
        try:
            retry_count -= 1
            result = await detect_script2(params.script_name, params.script, params.use_cache)
            break
        except Exception as e:
            logger.exception("detect script {} error: {}", params.script_name, e)
    elapsed = time.monotonic() - start_time
    logger.info("detect script {} time cost: {:.2f}s", params.script_name, elapsed)
    if result is None:
        raise HTTPException(status_code=500, detail="Dynamic detection failed after multiple retries.")
    logger.info("detect script {} result: {}", params.script_name, result)
    return result


@app.post("/dynamic_detection_agent")
async def dynamic_detection_agent(params: DynamicDetectionAgentParams):
    logger.info("start agent dynamic detection, scene_id={} scripts={}", params.scene_id, len(params.scripts))
    logger.info(
        "agent dynamic detection request targets={} use_cache={} max_tool_calls={}",
        params.target_script_paths or [script.scriptPath for script in params.scripts],
        params.use_cache,
        params.agent_config.max_tool_calls,
    )
    start_time = time.monotonic()
    try:
        result = await detect_scene_scripts_agent(params)
    except Exception as e:
        logger.exception("agent dynamic detection error: {}", e)
        raise HTTPException(status_code=500, detail="Agent dynamic detection failed.") from e
    elapsed = time.monotonic() - start_time
    logger.info(
        "agent dynamic detection done. scripts={} time_cost={:.2f}s promptTokens={} completionTokens={} toolCalls={}",
        len(result.scripts),
        elapsed,
        result.promptTokens,
        result.completionTokens,
        result.toolCalls,
    )
    return result


@app.exception_handler(Exception)
async def global_exception_handler(request: Request, exc: Exception):
    import traceback
    logger.error(f"Unhandled exception: {'\n'.join(traceback.format_exception(exc))}")
    # Capture the complete exception stack trace.
    tb_str = ''.join(traceback.format_exception(exc))
    return JSONResponse(
        status_code=500,
        content={
            "error": str(exc),
            "type": type(exc).__name__,
            "args": exc.args,
            "traceback": tb_str,  # Development diagnostics only; omit stack traces from production responses.
        }
    )
