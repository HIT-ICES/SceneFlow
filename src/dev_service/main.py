import hashlib
import os
from typing import Union
import time  # 添加计时

import numpy as np
from fastapi.encoders import jsonable_encoder
from fastapi import FastAPI, HTTPException, Request, Body
from fastapi.responses import JSONResponse
from pydantic import BaseModel

import clusters
from dynamic_detection import detect_script2
from middleware.request_gc import GCMiddleware
from middleware.request_gzip import GZipRequestMiddleware, DecompressRequestMiddleware
from utils import dataset

# 日志引入（新增）
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
    logger.info("scene_division: scene_info_path={} method={}, extra={}", params.scene_info_path, params.method,
                params.extra)
    params.method = params.method.upper()
    scene_info = load_scene_info(params)
    if params.method == "DBSCAN" or params.method == "VOXELDBSCAN":
        if params.method == "DBSCAN":
            data_v, data_id = dataset.load_verticals(
                scene_info,
                downsample_step=params.extra.get("downsample_step") or 0.5
            )
        else:
            data_v, data_id = dataset.load_voxels(
                scene_info,
                downsample_step=params.extra.get("downsample_step") or 0.5
            )
        labels = clusters.dbscan(
            data_v, data_id, eps=params.extra.get("eps") or 2,
            min_samples=params.extra.get("min_samples") or 10,
            metric="euclidean"
        )
        result = dataset.group_object_by_labels(data_id, labels)
    else:
        raise ValueError("Unsupported method.")
    return jsonable_encoder(result, custom_encoder={np.int64: int, np.float64: float})


class DynamicDetectionParams(BaseModel):
    script_name: str
    script: str
    use_cache: bool = True


@app.post("/dynamic_detection")
async def dynamic_detection(params: DynamicDetectionParams):
    logger.info("start script={}", params.script_name)
    from dynamic_detection import detect_script
    start_time = time.monotonic()
    retry_count = 3
    result = None  # 初始化
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


@app.exception_handler(Exception)
async def global_exception_handler(request: Request, exc: Exception):
    logger.error(f"Unhandled exception: {exc}")
    import traceback
    # 获取完整的异常堆栈信息
    tb_str = ''.join(traceback.format_exception(exc))
    return JSONResponse(
        status_code=500,
        content={
            "error": str(exc),
            "type": type(exc).__name__,
            "args": exc.args,
            "traceback": tb_str,  # 打印堆栈信息（开发调试用，生产环境慎用）
        }
    )
