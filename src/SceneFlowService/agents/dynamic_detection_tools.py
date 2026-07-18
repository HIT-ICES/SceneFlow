from __future__ import annotations

import re
from dataclasses import dataclass, field
from typing import Any, Literal

from pydantic import AliasChoices, BaseModel, ConfigDict, Field
from loguru import logger

from agents.unity_script_reference import query_builtin_component_document as query_unity_script_reference_document


class DynamicDetectionContextScript(BaseModel):
    model_config = ConfigDict(populate_by_name=True)

    scriptPath: str = Field(validation_alias=AliasChoices("scriptPath", "script_path"))
    className: str | None = Field(default=None, validation_alias=AliasChoices("className", "class_name"))
    sourceHash: str | None = Field(default=None, validation_alias=AliasChoices("sourceHash", "source_hash"))
    source: str
    fields: list[str] = Field(default_factory=list)
    isBuiltin: bool = Field(default=False, validation_alias=AliasChoices("isBuiltin", "is_builtin"))


class DynamicDetectionContext(BaseModel):
    model_config = ConfigDict(populate_by_name=True)

    sceneId: str | None = Field(default=None, validation_alias=AliasChoices("sceneId", "scene_id"))
    scripts: list[DynamicDetectionContextScript]
    components: list[str] = Field(default_factory=list)


class DynamicDetectionEvidence(BaseModel):
    kind: str = "source"
    location: str | None = None
    snippet: str | None = None


class SubmittedDynamicInfo(BaseModel):
    model_config = ConfigDict(populate_by_name=True)

    type: Literal["this", "field", "hierarchy", "other"] = Field(
        description=(
            "How the Unity rule layer can map the affected GameObject. Use 'this' for the attached GameObject "
            "or same-GameObject components from GetComponent/RequireComponent; use 'field' for public or "
            "serialized object/component fields; use 'hierarchy' for named hierarchy lookups; use 'other' only "
            "when no deterministic mapping is available."
        )
    )
    name: str | None = Field(
        default=None,
        description=(
            "For type='this', use 'this'. For type='field', use the exact field/property name. "
            "For type='hierarchy', use the object name/path. For type='other', use null."
        ),
    )
    dynamicType: Literal["Static", "Dynamic", "DynamicInteractive", 0, 1, 2] = Field(
        validation_alias=AliasChoices("dynamicType", "dynamic_type"),
        description=(
            "Static for no visible runtime change, Dynamic for passive visible changes, "
            "DynamicInteractive for user/input/XR/UI driven visible changes."
        ),
    )
    confidence: float = Field(default=1.0, ge=0.0, le=1.0)
    evidence: list[DynamicDetectionEvidence] = Field(default_factory=list)


class SubmittedScriptAnalysis(BaseModel):
    scriptPath: str
    dynamics: list[SubmittedDynamicInfo] = Field(default_factory=list)
    warnings: list[str] = Field(default_factory=list)


@dataclass
class DynamicDetectionToolbox:
    context: DynamicDetectionContext
    submitted: SubmittedScriptAnalysis | None = None
    tool_calls: int = 0
    tool_call_records: list[dict[str, Any]] = field(default_factory=list)

    def _record_tool_call(self, name: str, call_number: int, args: dict[str, Any], result: Any) -> None:
        self.tool_call_records.append({
            "callNumber": call_number,
            "tool": name,
            "args": args,
            "result": result,
        })

    def _find_script(self, script_id: str) -> DynamicDetectionContextScript | None:
        script_id_lower = script_id.lower()
        for script in self.context.scripts:
            if script.scriptPath.lower() == script_id_lower:
                return script
            if script.className and script.className.lower() == script_id_lower:
                return script
        for script in self.context.scripts:
            if script_id_lower in script.scriptPath.lower():
                return script
            if script.className and script_id_lower in script.className.lower():
                return script
        return None

    def search_scripts(self, query: str, limit: int = 10) -> list[dict[str, Any]]:
        """Search all exported script text and return compact source snippets."""
        self.tool_calls += 1
        call_number = self.tool_calls
        query = query.strip()
        logger.info(
            "agent tool search_scripts call={} query={!r} limit={} context_scripts={}",
            call_number,
            query[:120],
            limit,
            len(self.context.scripts),
        )
        if not query:
            logger.info("agent tool search_scripts call={} skipped empty query", call_number)
            self._record_tool_call("search_scripts", call_number, {"query": query, "limit": limit}, [])
            return []
        limit = max(1, min(limit, 20))
        query_lower = query.lower()
        results: list[dict[str, Any]] = []
        for script in self.context.scripts:
            lines = script.source.splitlines()
            for index, line in enumerate(lines):
                if query_lower not in line.lower():
                    continue
                start = max(0, index - 2)
                end = min(len(lines), index + 3)
                snippet = "\n".join(f"{i + 1}: {lines[i]}" for i in range(start, end))
                results.append({
                    "scriptPath": script.scriptPath,
                    "className": script.className,
                    "line": index + 1,
                    "snippet": snippet[:1600],
                })
                if len(results) >= limit:
                    logger.info(
                        "agent tool search_scripts call={} returned {} results (limit reached)",
                        call_number,
                        len(results),
                    )
                    self._record_tool_call("search_scripts", call_number, {"query": query, "limit": limit}, results)
                    return results
        logger.info("agent tool search_scripts call={} returned {} results", call_number, len(results))
        self._record_tool_call("search_scripts", call_number, {"query": query, "limit": limit}, results)
        return results

    def get_script(self, script_id: str) -> dict[str, Any]:
        """Return a complete exported script by path or class name."""
        self.tool_calls += 1
        call_number = self.tool_calls
        logger.info("agent tool get_script call={} script_id={!r}", call_number, script_id[:160])
        script = self._find_script(script_id)
        if script is None:
            logger.warning("agent tool get_script call={} not found: {}", call_number, script_id)
            result = {"found": False, "scriptId": script_id}
            self._record_tool_call("get_script", call_number, {"script_id": script_id}, result)
            return result
        logger.info(
            "agent tool get_script call={} found path={} class={} source_chars={}",
            call_number,
            script.scriptPath,
            script.className,
            len(script.source),
        )
        result = {
            "found": True,
            "scriptPath": script.scriptPath,
            "className": script.className,
            "sourceHash": script.sourceHash,
            "fields": script.fields,
            "isBuiltin": script.isBuiltin,
            "source": script.source,
        }
        self._record_tool_call("get_script", call_number, {"script_id": script_id}, result)
        return result

    def query_builtin_component_document(self, fullQualifiedName: str) -> str:
        """Return cleaned Unity ScriptReference Markdown for a builtin Unity component."""
        self.tool_calls += 1
        call_number = self.tool_calls
        logger.info(
            "agent tool query_builtin_component_document call={} fullQualifiedName={!r}",
            call_number,
            fullQualifiedName[:160],
        )
        result = query_unity_script_reference_document(fullQualifiedName)
        logger.info(
            "agent tool query_builtin_component_document call={} result_chars={}",
            call_number,
            len(result),
        )
        self._record_tool_call(
            "query_builtin_component_document",
            call_number,
            {"fullQualifiedName": fullQualifiedName},
            result,
        )
        return result

    def submit_script_analysis(
        self,
        scriptPath: str,
        dynamics: list[SubmittedDynamicInfo],
        warnings: list[str] | None = None,
    ) -> dict[str, Any]:
        """Submit the final script-level dynamic analysis."""
        self.tool_calls += 1
        call_number = self.tool_calls
        logger.info(
            "agent tool submit_script_analysis call={} scriptPath={} dynamics={} warnings={}",
            call_number,
            scriptPath,
            len(dynamics),
            len(warnings or []),
        )
        self.submitted = SubmittedScriptAnalysis.model_validate({
            "scriptPath": scriptPath,
            "dynamics": dynamics,
            "warnings": warnings or [],
        })
        logger.info(
            "agent tool submit_script_analysis accepted scriptPath={} normalized_dynamics={}",
            self.submitted.scriptPath,
            [
                {"type": item.type, "name": item.name, "dynamicType": item.dynamicType, "confidence": item.confidence}
                for item in self.submitted.dynamics
            ],
        )
        result = {"accepted": True, "dynamicCount": len(self.submitted.dynamics)}
        self._record_tool_call(
            "submit_script_analysis",
            call_number,
            {
                "scriptPath": scriptPath,
                "dynamics": dynamics,
                "warnings": warnings or [],
            },
            result,
        )
        return result


def simple_symbol_candidates(source: str) -> list[str]:
    candidates = set(re.findall(r"\b[A-Za-z_][A-Za-z0-9_]{3,}\b", source))
    noisy = {
        "public", "private", "protected", "internal", "class", "using", "void",
        "float", "string", "return", "false", "true", "null", "static",
        "Update", "Start", "Awake", "MonoBehaviour", "UnityEngine",
    }
    return sorted(c for c in candidates if c not in noisy)[:80]
