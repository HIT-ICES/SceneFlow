from __future__ import annotations

import unittest

from agents.dynamic_detection_agent import DynamicDetectionObject
from agents.dynamic_detection_agent_v2 import (
    AGENT_V2_PROMPT,
    _deduplicate_results,
    _normalize_dynamic_type,
    _to_detection_object,
)
from agents.dynamic_detection_tools import SubmittedDynamicInfo


class DynamicDetectionAgentV2Tests(unittest.TestCase):
    def test_submit_schema_accepts_static_dynamic_types(self) -> None:
        static_info = SubmittedDynamicInfo.model_validate({
            "type": "field",
            "name": "target",
            "dynamicType": "Static",
        })
        zero_info = SubmittedDynamicInfo.model_validate({
            "type": "this",
            "name": "this",
            "dynamicType": 0,
        })

        self.assertEqual(static_info.dynamicType, "Static")
        self.assertEqual(zero_info.dynamicType, 0)
        self.assertEqual(_normalize_dynamic_type(static_info.dynamicType), 0)
        self.assertEqual(_normalize_dynamic_type(zero_info.dynamicType), 0)

    def test_static_submission_converts_to_detection_object(self) -> None:
        info = SubmittedDynamicInfo.model_validate({
            "type": "this",
            "name": "ignored",
            "dynamicType": "Static",
        })

        detection = _to_detection_object(info)

        self.assertEqual(detection.type, "this")
        self.assertEqual(detection.name, "this")
        self.assertEqual(detection.dynamicType, 0)

    def test_deduplicate_keeps_strongest_dynamic_type(self) -> None:
        results = [
            DynamicDetectionObject(type="field", name="target", dynamicType=0),
            DynamicDetectionObject(type="field", name="target", dynamicType=2),
            DynamicDetectionObject(type="field", name="target", dynamicType=1),
        ]

        deduped = _deduplicate_results(results)

        self.assertEqual(len(deduped), 1)
        self.assertEqual(deduped[0].dynamicType, 2)

    def test_prompt_requires_complete_classification(self) -> None:
        self.assertIn('Always submit exactly one classification for "this"', AGENT_V2_PROMPT)
        self.assertIn("every name listed in Target fields", AGENT_V2_PROMPT)
        self.assertIn("Do not omit Static items", AGENT_V2_PROMPT)


if __name__ == "__main__":
    unittest.main()
