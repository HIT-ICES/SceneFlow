from __future__ import annotations

import dataclasses
import hashlib
import json
import time
from datetime import datetime, timezone
from pathlib import Path
from typing import Any

from autogen_agentchat.agents import AssistantAgent
from loguru import logger
from pydantic import BaseModel

from agents.dynamic_detection_agent import DynamicDetectionObject, DynamicDetectionResult
from agents.dynamic_detection_tools import (
    DynamicDetectionContext,
    DynamicDetectionContextScript,
    DynamicDetectionToolbox,
    SubmittedDynamicInfo,
    simple_symbol_candidates,
)
from agents.model import MODEL_CLIENT_DETECT


CONVERSATION_DUMP_DIR = Path("cache/dynamic_detection_agent_conversations")


AGENT_V2_PROMPT = """
You are a Unity C# script dynamic-rendering analyzer.

Analyze exactly one target script at a time. You may use tools to search other exported
scripts in the same scene context, then you must submit the final answer by calling
submit_script_analysis. Do not output object ids or concrete scene instances.

For non-builtin scripts, inspect the class declaration before judging dynamics. If the
target class inherits from a project/package base class instead of a direct common Unity
base such as MonoBehaviour, ScriptableObject, Editor, or PropertyAttribute, use
search_scripts/get_script to inspect the relevant base class chain and include inherited
runtime visual behavior in the target script analysis.

For Unity builtin components, the exported source may be a placeholder like
"Unity Builtin Script: UnityEngine.Video.VideoPlayer" and target script metadata may
have isBuiltin=true. In that case, call query_builtin_component_document with the
full qualified component name. The tool returns cleaned Markdown from local Unity
ScriptReference documentation, or exactly "Not Found". If it returns "Not Found",
analyze conservatively from the exported fields and component name.

Classify only GameObjects whose visible rendering can change at runtime: transform,
scale, rotation, material, color, texture, shader, Renderer or Canvas visibility,
Animator, ParticleSystem, Video, UI visual state, or similar visible output.
Do not mark audio, pure physics, counters, networking, logging, or data-only state
unless that state drives a visible change.

Association type means how this script reaches the GameObject whose rendering changes:
- this: this script makes its attached GameObject dynamic; name must be "this".
- field: this script classifies the GameObject/Component referenced by a field or property; name is the field/property name.
- hierarchy: this script makes a named hierarchy lookup target dynamic; name is the searched object name/path.
- other: this script can affect rendering, but the Unity rule layer cannot map the affected GameObject deterministically.

For non-builtin scripts, prefer field names from Target fields when reporting type="field".
The field/property may be declared directly in the target script or inherited from an
inspected base class.
For builtin Unity components, Target fields may be empty; use query_builtin_component_document
to identify public target object/component properties.

Prefer mappable results. Do not use type="other" for:
- public/serialized fields such as Transform target, Renderer targetRenderer, GameObject objectToMove.
- same-GameObject components obtained by GetComponent<T>() or required by RequireComponent; use type="this".
- direct writes to transform, renderer/material, enabled, gameObject.SetActive, or this component's visual state; use type="this".
Use type="other" only when you cannot name a field, hierarchy lookup, or the attached GameObject.

Dynamic types:
- Static: no visible runtime change, or the field/property only stores non-visual data.
- Dynamic: visible runtime change without direct user interaction.
- DynamicInteractive: a user/input/XR/UI/collision/trigger interaction path changes visible output.

Complete classification requirement:
- Always submit exactly one classification for "this".
- Always submit exactly one classification for every name listed in Target fields.
- If you inspect a project/package base class, also submit one classification for every
  inherited field/property from that base class that is public, serialized, or otherwise
  reachable by this target class.
- Submit hierarchy entries for named hierarchy lookup targets that the script affects.
- Do not omit Static items. Use dynamicType="Static" for required targets that do not
  change visible output.

Interaction rule for attached objects: if the target script or an inspected base class
handles user input, UI click, XR selection, OnMouseDown, collision/trigger interaction,
or selection/range interaction on the attached GameObject, classify "this" as
DynamicInteractive when that interaction can change visible output anywhere or makes
the attached GameObject an interaction target. Body parts or children of such an
interactive root may be interaction targets through propagation, so the root must not
be downgraded to Dynamic or Static in that case.

Be conservative but evidence-based. If there is no visible runtime change, still submit
the required "this" and field classifications with dynamicType="Static".
"""


AGENT_V2_SUBMIT_FALLBACK_PROMPT = """
The previous analysis ended without a submit_script_analysis call, likely because the
tool iteration budget was exhausted.

You must now call submit_script_analysis exactly once. Do not call search_scripts,
get_script, query_builtin_component_document, or ask for more context. Base the final
answer only on the target source, Target fields, and any prior tool results already
available in the conversation.

Follow the complete classification requirement:
- include "this";
- include every Target fields entry;
- include inherited fields/properties already discovered from base-class inspection;
- include hierarchy lookup targets that are visible interaction/rendering targets;
- use Static/Dynamic/DynamicInteractive, and use Static for required entries that do
  not visibly change at runtime.
"""


def _normalize_dynamic_type(value: str | int) -> int:
    if isinstance(value, int):
        if value <= 0:
            return 0
        return 2 if value == 2 else 1
    value_lower = value.lower()
    if value_lower in {"static", "0"}:
        return 0
    if value_lower in {"dynamicinteractive", "interactive", "2"}:
        return 2
    return 1


def _normalize_type(value: str | None) -> str:
    if value is None:
        return "other"
    value_lower = value.strip().lower()
    if value_lower in {"field", "this", "hierarchy", "other"}:
        return value_lower
    if value_lower in {"runtime_find", "unknown"}:
        return "other"
    return "other"


def _to_detection_object(info: SubmittedDynamicInfo) -> DynamicDetectionObject:
    assoc_type = _normalize_type(info.type)
    name = info.name.strip() if isinstance(info.name, str) else info.name
    if assoc_type == "this":
        name = "this"
    elif assoc_type == "other":
        name = None
    return DynamicDetectionObject(
        type=assoc_type,
        dynamicType=_normalize_dynamic_type(info.dynamicType),
        name=name,
        description=None,
        extern_methods=None,
    )


def _empty_result() -> DynamicDetectionResult:
    return DynamicDetectionResult(results=[], promptTokens=0, completionTokens=0)


def _deduplicate_results(results: list[DynamicDetectionObject]) -> list[DynamicDetectionObject]:
    deduped: dict[tuple[str, str | None], DynamicDetectionObject] = {}
    for item in results:
        key = (item.type, item.name)
        old = deduped.get(key)
        if old is None or old.dynamicType < item.dynamicType:
            deduped[key] = item
    return list(deduped.values())


def _json_safe(value: Any, depth: int = 0) -> Any:
    if depth > 12:
        return repr(value)
    if value is None or isinstance(value, str | int | float | bool):
        return value
    if isinstance(value, BaseModel):
        try:
            return _json_safe(value.model_dump(mode="json"), depth + 1)
        except Exception:
            return repr(value)
    if dataclasses.is_dataclass(value) and not isinstance(value, type):
        try:
            return _json_safe(dataclasses.asdict(value), depth + 1)
        except Exception:
            return repr(value)
    if isinstance(value, dict):
        return {str(k): _json_safe(v, depth + 1) for k, v in value.items()}
    if isinstance(value, list | tuple | set):
        return [_json_safe(item, depth + 1) for item in value]
    if hasattr(value, "model_dump"):
        try:
            return _json_safe(value.model_dump(mode="json"), depth + 1)
        except Exception:
            pass
    if hasattr(value, "__dict__"):
        try:
            return {
                str(k): _json_safe(v, depth + 1)
                for k, v in vars(value).items()
                if not k.startswith("_")
            }
        except Exception:
            pass
    return repr(value)


def _serialize_message(index: int, message: Any) -> dict[str, Any]:
    serialized: dict[str, Any] = {
        "index": index,
        "type": type(message).__name__,
        "source": getattr(message, "source", None),
    }

    if hasattr(message, "model_dump"):
        try:
            data = message.model_dump(mode="json")
        except Exception as e:
            serialized["serializationError"] = repr(e)
            data = {}
        for key in ("id", "created_at", "metadata", "content"):
            if key in data and data[key] not in (None, {}, []):
                serialized[_camel_case(key)] = _json_safe(data[key])
        usage = data.get("models_usage")
        if usage is not None:
            serialized["modelsUsage"] = _json_safe(usage)
    else:
        usage = getattr(message, "models_usage", None)
        if usage is not None:
            serialized["modelsUsage"] = _json_safe(usage)
        content = getattr(message, "content", None)
        if content is not None:
            serialized["content"] = _json_safe(content)
    return serialized


def _camel_case(value: str) -> str:
    parts = value.split("_")
    return parts[0] + "".join(part.capitalize() for part in parts[1:])


def _conversation_dump_path(script_path: str) -> Path:
    timestamp = datetime.now(timezone.utc).strftime("%Y%m%dT%H%M%S.%fZ")
    script_hash = hashlib.sha1(script_path.encode("utf-8")).hexdigest()[:10]
    safe_name = "".join(c if c.isalnum() or c in {"-", "_", "."} else "_" for c in script_path)
    safe_name = safe_name.strip("._")[:80] or "script"
    return CONVERSATION_DUMP_DIR / f"{timestamp}_{safe_name}_{script_hash}.json"


def _dump_agent_conversation(
    *,
    target_script: DynamicDetectionContextScript,
    context: DynamicDetectionContext,
    max_tool_calls: int,
    tasks: list[str],
    task_results: list[Any],
    toolbox: DynamicDetectionToolbox,
    elapsed: float,
    result: DynamicDetectionResult,
    warnings: list[str],
) -> None:
    dump_path = _conversation_dump_path(target_script.scriptPath)
    payload = {
        "dumpVersion": 2,
        "createdAt": datetime.now(timezone.utc).isoformat(),
        "scriptPath": target_script.scriptPath,
        "className": target_script.className,
        "isBuiltin": target_script.isBuiltin,
        "sourceHash": target_script.sourceHash,
        "sceneId": context.sceneId,
        "contextScriptCount": len(context.scripts),
        "components": context.components,
        "maxToolCalls": max_tool_calls,
        "elapsedSeconds": elapsed,
        "systemPrompt": AGENT_V2_PROMPT,
        "tasks": tasks,
        "messages": [
            _serialize_message(index, message)
            for index, message in enumerate(
                message
                for task_result in task_results
                for message in getattr(task_result, "messages", [])
            )
        ],
        "toolCallRecords": _json_safe(toolbox.tool_call_records),
        "submitted": _json_safe(toolbox.submitted),
        "warnings": warnings,
        "result": _json_safe(result),
    }
    try:
        CONVERSATION_DUMP_DIR.mkdir(parents=True, exist_ok=True)
        dump_path.write_text(json.dumps(payload, ensure_ascii=False, indent=2), encoding="utf-8")
        logger.info("agent v2 conversation dump saved scriptPath={} path={}", target_script.scriptPath, dump_path)
    except Exception as e:
        logger.error("agent v2 conversation dump failed scriptPath={} error={}", target_script.scriptPath, e)


async def detect_script_agent_v2(
    target_script: DynamicDetectionContextScript,
    context: DynamicDetectionContext,
    max_tool_calls: int = 6,
) -> tuple[DynamicDetectionResult, int, list[str]]:
    logger.info(
        "agent v2 prepare scriptPath={} class={} source_chars={} context_scripts={} max_tool_calls={}",
        target_script.scriptPath,
        target_script.className,
        len(target_script.source),
        len(context.scripts),
        max_tool_calls,
    )
    toolbox = DynamicDetectionToolbox(context=context)
    agent = AssistantAgent(
        "dynamic_detection_agent_v2",
        model_client=MODEL_CLIENT_DETECT,
        tools=[
            toolbox.search_scripts,
            toolbox.get_script,
            toolbox.query_builtin_component_document,
            toolbox.submit_script_analysis,
        ],
        system_message=AGENT_V2_PROMPT,
        model_client_stream=False,
        reflect_on_tool_use=False,
        max_tool_iterations=max(1, max_tool_calls),
    )

    symbols = ", ".join(simple_symbol_candidates(target_script.source)[:30])
    task = f"""
Target scriptPath: {target_script.scriptPath}
Target className: {target_script.className}
Target isBuiltin: {target_script.isBuiltin}
Target fields: {target_script.fields}
Important symbols to consider searching if needed: {symbols}

Source:
```csharp
{target_script.source}
```

Call submit_script_analysis with scriptPath="{target_script.scriptPath}" when finished.
"""
    logger.info(
        "agent v2 run start scriptPath={} task_chars={} symbol_count={}",
        target_script.scriptPath,
        len(task),
        len(symbols.split(", ")) if symbols else 0,
    )
    start_time = time.monotonic()
    task_result = await agent.run(task=task)
    task_results = [task_result]
    tasks = [task]
    if toolbox.submitted is None:
        logger.warning(
            "agent v2 no submission after primary run scriptPath={}, starting submit fallback",
            target_script.scriptPath,
        )
        fallback_agent = AssistantAgent(
            "dynamic_detection_agent_v2_submit_fallback",
            model_client=MODEL_CLIENT_DETECT,
            tools=[toolbox.submit_script_analysis],
            system_message=AGENT_V2_PROMPT,
            model_client_stream=False,
            reflect_on_tool_use=False,
            max_tool_iterations=2,
        )
        fallback_task = f"""
{AGENT_V2_SUBMIT_FALLBACK_PROMPT}

Target scriptPath: {target_script.scriptPath}
Target className: {target_script.className}
Target isBuiltin: {target_script.isBuiltin}
Target fields: {target_script.fields}

Source:
```csharp
{target_script.source}
```

Call submit_script_analysis with scriptPath="{target_script.scriptPath}" now.
"""
        fallback_result = await fallback_agent.run(task=fallback_task)
        task_results.append(fallback_result)
        tasks.append(fallback_task)
    elapsed = time.monotonic() - start_time
    logger.info(
        "agent v2 run done scriptPath={} elapsed={:.2f}s messages={} tool_calls={}",
        target_script.scriptPath,
        elapsed,
        sum(len(getattr(item, "messages", [])) for item in task_results),
        toolbox.tool_calls,
    )
    prompt_tokens = 0
    completion_tokens = 0
    message_index = 0
    for task_result_item in task_results:
        for msg in getattr(task_result_item, "messages", []):
            logger.info(
                "agent v2 message scriptPath={} index={} source={} type={} has_usage={}",
                target_script.scriptPath,
                message_index,
                getattr(msg, "source", None),
                type(msg).__name__,
                msg.models_usage is not None,
            )
            message_index += 1
            if msg.models_usage is None:
                continue
            prompt_tokens += msg.models_usage.prompt_tokens
            completion_tokens += msg.models_usage.completion_tokens
    logger.info(
        "agent v2 usage scriptPath={} promptTokens={} completionTokens={}",
        target_script.scriptPath,
        prompt_tokens,
        completion_tokens,
    )

    warnings: list[str] = []
    if toolbox.submitted is None:
        warnings.append("agent did not call submit_script_analysis")
        logger.warning("agent v2 no submission scriptPath={}", target_script.scriptPath)
        result = _empty_result()
    else:
        result = DynamicDetectionResult(
            results=_deduplicate_results([
                _to_detection_object(item)
                for item in toolbox.submitted.dynamics
            ]),
            promptTokens=prompt_tokens,
            completionTokens=completion_tokens,
        )
        warnings.extend(toolbox.submitted.warnings)
        logger.info(
            "agent v2 submitted scriptPath={} result_count={} warnings={}",
            target_script.scriptPath,
            len(result.results),
            len(warnings),
        )
    _dump_agent_conversation(
        target_script=target_script,
        context=context,
        max_tool_calls=max_tool_calls,
        tasks=tasks,
        task_results=task_results,
        toolbox=toolbox,
        elapsed=elapsed,
        result=result,
        warnings=warnings,
    )
    return result, toolbox.tool_calls, warnings
