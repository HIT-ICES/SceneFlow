from __future__ import annotations

import asyncio
import hashlib
import json
import os
from typing import Any

from pydantic import BaseModel, Field

from agents.dynamic_detection_agent import DynamicDetectionResult
from agents.dynamic_detection_agent_v2 import detect_script_agent_v2
from agents.dynamic_detection_tools import DynamicDetectionContext, DynamicDetectionContextScript
from loguru import logger


AGENT_CACHE_VERSION = "agent-v2-static-full-submit-fallback-20260709"
AGENT_MAX_CONCURRENT_SCRIPTS = 8


class DynamicDetectionAgentConfig(BaseModel):
    max_tool_calls: int = 6
    enable_global_script_search: bool = True


class DynamicDetectionAgentParams(BaseModel):
    scene_id: str | None = None
    scripts: list[DynamicDetectionContextScript]
    target_script_paths: list[str] | None = None
    components: list[str] = Field(default_factory=list)
    use_cache: bool = True
    agent_config: DynamicDetectionAgentConfig = Field(default_factory=DynamicDetectionAgentConfig)


class DynamicDetectionAgentScriptResult(BaseModel):
    scriptPath: str
    dynamics: list[Any] = Field(default_factory=list)
    warnings: list[str] = Field(default_factory=list)


class DynamicDetectionAgentResponse(BaseModel):
    scripts: list[DynamicDetectionAgentScriptResult]
    promptTokens: int = 0
    completionTokens: int = 0
    toolCalls: int = 0


class _ScriptDetectionUnit(BaseModel):
    script: DynamicDetectionAgentScriptResult
    prompt_tokens: int = 0
    completion_tokens: int = 0
    tool_calls: int = 0


def _context_hash(params: DynamicDetectionAgentParams) -> str:
    payload = {
        "cache_version": AGENT_CACHE_VERSION,
        "scene_id": params.scene_id,
        "scripts": [
            {
                "scriptPath": script.scriptPath,
                "className": script.className,
                "sourceHash": script.sourceHash,
                "source": script.source,
                "fields": script.fields,
                "isBuiltin": script.isBuiltin,
            }
            for script in params.scripts
        ],
        "components": params.components,
        "agent_config": params.agent_config.model_dump(),
    }
    data = json.dumps(payload, sort_keys=True, ensure_ascii=False)
    return hashlib.sha1(data.encode("utf-8")).hexdigest()


def _script_cache_path(script: DynamicDetectionContextScript, context_hash: str) -> str:
    safe_name = hashlib.sha1(script.scriptPath.encode("utf-8")).hexdigest()
    source_hash = script.sourceHash or hashlib.sha1(script.source.encode("utf-8")).hexdigest()
    return f"cache/dynamic_detection_agent/{safe_name}-{source_hash}-{context_hash}.json"


def _load_cache(script: DynamicDetectionContextScript, context_hash: str) -> DynamicDetectionResult | None:
    path = _script_cache_path(script, context_hash)
    if not os.path.exists(path):
        logger.info("agent cache miss: script [{}] path={}", script.scriptPath, path)
        return None
    logger.info("agent cache load: script [{}] path={}", script.scriptPath, path)
    try:
        with open(path, "r", encoding="utf-8") as f:
            return DynamicDetectionResult.model_validate_json(f.read())
    except Exception as e:
        logger.error("load agent dynamic cache error: {}", e)
        try:
            os.remove(path)
        except OSError:
            pass
        return None


def _save_cache(script: DynamicDetectionContextScript, context_hash: str, result: DynamicDetectionResult) -> None:
    path = _script_cache_path(script, context_hash)
    os.makedirs(os.path.dirname(path), exist_ok=True)
    with open(path, "w", encoding="utf-8") as f:
        f.write(result.model_dump_json())
    logger.info(
        "agent cache saved: script [{}] path={} result_count={} promptTokens={} completionTokens={}",
        script.scriptPath,
        path,
        len(result.results),
        result.promptTokens,
        result.completionTokens,
    )


async def detect_scene_scripts_agent(params: DynamicDetectionAgentParams) -> DynamicDetectionAgentResponse:
    logger.info(
        "agent scene request: scene_id={} scripts={} target_paths={} components={} use_cache={} max_tool_calls={} max_concurrent_scripts={}",
        params.scene_id,
        len(params.scripts),
        len(params.target_script_paths or []),
        len(params.components),
        params.use_cache,
        params.agent_config.max_tool_calls,
        AGENT_MAX_CONCURRENT_SCRIPTS,
    )
    for index, script in enumerate(params.scripts):
        logger.info(
            "agent scene script[{}]: path={} class={} source_chars={} fields={} is_builtin={} source_hash={}",
            index,
            script.scriptPath,
            script.className,
            len(script.source),
            len(script.fields),
            script.isBuiltin,
            script.sourceHash,
        )
    context = DynamicDetectionContext(
        sceneId=params.scene_id,
        scripts=params.scripts,
        components=params.components,
    )
    context_hash = _context_hash(params)
    logger.info("agent scene context_hash={}", context_hash)
    response = DynamicDetectionAgentResponse(scripts=[])
    target_paths = set(params.target_script_paths or [script.scriptPath for script in params.scripts])
    logger.info("agent scene target_paths={}", sorted(target_paths))
    target_scripts: list[DynamicDetectionContextScript] = []
    for script in params.scripts:
        if script.scriptPath not in target_paths:
            logger.info("agent scene skip non-target script [{}]", script.scriptPath)
            continue
        target_scripts.append(script)

    semaphore = asyncio.Semaphore(max(1, AGENT_MAX_CONCURRENT_SCRIPTS))

    async def detect_one_script(script: DynamicDetectionContextScript) -> _ScriptDetectionUnit:
        async with semaphore:
            logger.info("agent scene script start [{}]", script.scriptPath)
            warnings: list[str] = []
            result = _load_cache(script, context_hash) if params.use_cache else None
            tool_calls = 0
            if result is not None:
                logger.info("agent scene script cache hit [{}] result_count={}", script.scriptPath, len(result.results))
            else:
                logger.info("agent scene script invoking v2 [{}]", script.scriptPath)
                result, tool_calls, warnings = await detect_script_agent_v2(
                    script,
                    context,
                    max_tool_calls=params.agent_config.max_tool_calls,
                )
                logger.info(
                    "agent scene script v2 done [{}] result_count={} tool_calls={} warnings={}",
                    script.scriptPath,
                    len(result.results),
                    tool_calls,
                    len(warnings),
                )
                _save_cache(script, context_hash, result)
            logger.info("agent scene script done [{}] toolCalls={}", script.scriptPath, tool_calls)
            return _ScriptDetectionUnit(
                script=DynamicDetectionAgentScriptResult(
                    scriptPath=script.scriptPath,
                    dynamics=result.results,
                    warnings=warnings,
                ),
                prompt_tokens=result.promptTokens,
                completion_tokens=result.completionTokens,
                tool_calls=tool_calls,
            )

    logger.info(
        "agent scene detecting target scripts: count={} max_concurrent_scripts={}",
        len(target_scripts),
        AGENT_MAX_CONCURRENT_SCRIPTS,
    )
    detection_units = await asyncio.gather(
        *(detect_one_script(script) for script in target_scripts)
    )
    for unit in detection_units:
        response.scripts.append(unit.script)
        response.promptTokens += unit.prompt_tokens
        response.completionTokens += unit.completion_tokens
        response.toolCalls += unit.tool_calls
        logger.info(
            "agent scene script accumulated [{}] accumulated_scripts={} accumulated_toolCalls={}",
            unit.script.scriptPath,
            len(response.scripts),
            response.toolCalls,
        )
    logger.info(
        "agent scene done: returned_scripts={} promptTokens={} completionTokens={} toolCalls={}",
        len(response.scripts),
        response.promptTokens,
        response.completionTokens,
        response.toolCalls,
    )
    return response
