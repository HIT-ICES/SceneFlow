import json
from typing import Literal, Optional

from autogen_agentchat.agents import AssistantAgent
from pydantic import BaseModel

AGENT_ANALYZE_DESCRIPTION = """
You are a Unity C# code analysis expert, specializing in analyzing the behavior of Unity C# script.
"""
AGENT_ANALYZE_PROMPT = """
You are a Unity C# code analysis expert, specializing in analyzing the behavior of Unity C# script.

Your task is to identify which objects' rendering results are dynamically altered by scripts, including modifications to properties such as position, scale, rotation, material, color, and texture, or when objects' visibility is controlled via scripts.
If the script only changes properties unrelated to rendering (such as physics properties or logical states), it should NOT be considered as dynamically changing the rendering result.
Note that modifications here must be valid changes. If you are certain that the modification does not affect the rendering result, do not include it in the results.
Note that you must base your analysis entirely on the provided source code and DO NOT making assumptions about the author's intent, even if you think the code is written incorrectly.
The analysis should be at the GameObject level. Since scripts often reference Components, if it is unclear whether multiple Components belong to the same GameObject, assume they may belong to different GameObjects.

Dynamic types are categorized into two types:
1. Players passively observe object changes (dynamicType=1): Scripts dynamically alter an object's rendering without direct user triggering, yet impact the user's visual experience. Examples include animations driven by the global clock and environmental effects.
2. Player interacts in real-time and observes object changes (dynamicType=2): Scripts engage in real-time bidirectional interaction with the user, altering object rendering where visual states are directly driven by local user input.

Note: You should be conservative in your assessments, especially when determining whether an object is dynamic. As long as the script has the potential to modify the object, you should at least set `dynamicType=1`; if user interaction is involved, set `dynamicType=2`.
If the object a to which the script is attached receives player interaction and then manipulates the rendering of object b, then object a has dynamicType=2 and object b has dynamicType=1.
You should only consider a script to be static if you are certain that it does not accept user input and does not alter the rendering.

If the script is user-defined or in third-party libraries, you will get full code.
If the script is builtin in Unity or other well-known libraries, you will get the full name and its serialize fields, you can assume its behavior according to your knowledge.

You should analyze the code step by step:
First, read the provided Unity C# script, analyze it, explain your reasoning process.
Then, identify and categorize game objects that may dynamically alter their rendering results during runtime, briefly explaining why you believe this to be the case.
Finally, summarize and output the final result in the following format:
Each line represents a GameObject that may have its rendering result dynamically changed, with the following fields: type, name, description.
Types:
- type="field": The object is referenced via a field; name is the field name; no description
- type="this": The GameObject to which this script is attached; name is "this"; no description
- type="hierarchy": The object is found via scene hierarchy; name is the object name; no description
"""

AGENT_SUMMARY_DESCRIPTION = """
An agent that converts the analysis results of the previous agent into a strictly valid JSON object according to the specified schema.
"""
AGENT_SUMMARY_PROMPT = """
Your task is to convert the results provided by the previous agent into a strictly valid JSON object according to the specified schema.
Each object in the "results" array represents a GameObject that may have its rendering result dynamically changed, with the following fields: type, dynamicType, name, description.
'dynamicType' field indicates the category of dynamic behavior:
- 1: Player passively observes object changes (dynamicType=1)
- 2: Player actively interacts and observes object changes (dynamicType=2)
types:
- type="field": The object is referenced via a field; name is the field name; description is null
- type="this": The GameObject to which this script is attached; name is "this"; description is null
- type="hierarchy": The object is found via scene hierarchy; name is the object name; description is null
- type="other": Other types; name is null; description is what the object is.
"""



PROMPT_DESCRIPTION = """
You are a Unity C# code analysis expert, specializing in analyzing which GameObjects may have their rendering results dynamically changed at runtime.
"Dynamically changing rendering results" refers to modifying properties such as position, scale, rotation, material, color, texture, or controlling the visibility of the object via script.
If the script only changes properties unrelated to rendering (such as physics properties or logical states), it should NOT be considered as dynamically changing the rendering result.
The analysis should be at the GameObject level. Since scripts often reference Components, if it is unclear whether multiple Components belong to the same GameObject, assume they may belong to different GameObjects.
First, read the provided Unity C# script, analyze it step by step, explain your reasoning process.
Then, identify the GameObjects that may have their rendering results dynamically changed at runtime, and briefly explain why you think so.
Finally, output the final result in the following format:
"""

PROMPT_OUTPUT_FORMAT_CSV = """
The first line should be "RESULTS:"
Then, for each GameObject that may have its rendering result dynamically changed, output one line in CSV format, columns: type, name, description.
Types:
- type="field": The object is referenced via a field; name is the field name; no description
- type="this": The GameObject to which this script is attached; name is "this"; no description
- type="hierarchy": The object is found via scene hierarchy; name is the object name; no description
- type="other": Other types; no name; description is what the object is.
"""

PROMPT_OUTPUT_FORMAT_JSON = """
The first line should be "RESULTS:"
Then, output the final result in JSON format, a JSON array of objects.
Each object represents a GameObject that may have its rendering result dynamically changed, with the following fields: type, name, description.
Types:
- type="field": The object is referenced via a field; name is the field name; description is null
- type="this": The GameObject to which this script is attached; name is "this"; description is null
- type="hierarchy": The object is found via scene hierarchy; name is the object name; description is null
- type="other": Other types; name  is null; description is what the object is.
"""

PROMPT_OUTPUT_FORMAT_JSON_FULL = """
The first line should be "RESULTS:"
Then, output the final result in JSON format, a JSON array of objects.
Each object represents a GameObject that may have its rendering result dynamically changed, with the following fields: type, name, description, extern_methods.
Types:
- type="field": The object is referenced via a field; name is the field name; description is null
- type="this": The GameObject to which this script is attached; name is "this"; description is null
- type="hierarchy": The object is found via scene hierarchy; name is the object name; description is null
- type="other": Other types; name  is null; description is what the object is.
If whether the object is dynamic depends on a extern method call that the object is passed to, 
the extern_methods field should contain a list of method names (should be full name such as 'name.space.class_a.func_b' if you can name it) that may affect the rendering result, 
note that if the object can be dynamic without the extern method call, the extern_methods field should be null.
"""



PROMPT = PROMPT_DESCRIPTION + PROMPT_OUTPUT_FORMAT_JSON_FULL



DESCRIPTION = """
An agent that reads Unity C# scripts and identifies objects that may dynamically change their rendering results at runtime.
"""
class DynamicDetectionAnalyzeAgent(AssistantAgent):
    def __init__(self, model_client):
        super().__init__(
            "dynamic_object_detection_analyze_agent",
            model_client=model_client,
            system_message=AGENT_ANALYZE_PROMPT,
            description=AGENT_ANALYZE_DESCRIPTION,
            model_client_stream=False,
        )
class DynamicDetectionSummaryAgent(AssistantAgent):
    def __init__(self, model_client):
        super().__init__(
            "dynamic_object_detection_summary_agent",
            model_client=model_client,
            system_message=AGENT_SUMMARY_PROMPT,
            description=AGENT_SUMMARY_DESCRIPTION,
            model_client_stream=False,
            output_content_type=DynamicDetectionResultMessage,
        )
class DynamicDetectionObject(BaseModel):
    type: Literal["field", "this", "hierarchy", "other"]
    dynamicType: Literal[0,1,2] = 1
    name: Optional[str] = None
    description: Optional[str] = None
    extern_methods: Optional[list[str]] = None
class DynamicDetectionResultMessage(BaseModel):
    results: list[DynamicDetectionObject]
class DynamicDetectionResult(BaseModel):
    results: list[DynamicDetectionObject]
    promptTokens: int
    completionTokens: int


class DynamicDetectionAgent(AssistantAgent):
    def __init__(self, model_client):
        super().__init__(
            "dynamic_object_detection_agent",
            model_client=model_client,
            system_message=PROMPT,
            description=DESCRIPTION,
            model_client_stream=False,
        )
    async def detect(self, task: str) -> list:
        result = await self.run(task="Please analyse the following code: \n" + task)
        # for msg in result.messages:
        #     print(f"msg: {msg}")
        msg = result.messages[len(result.messages) - 1]
        msg = msg.to_text()
        # print(msg)
        msg = msg.split("RESULTS:")[-1].strip()
        return json.loads(extract_json(msg))


def extract_json(text: str) -> str:
    """
    Extracts the JSON from the given text.
    """
    lo, ro = text.find("{"), text.rfind("}")
    la, ra = text.find("["), text.rfind("]")
    exist_object = lo != -1 and ro != -1 and lo < ro
    exist_array = la != -1 and ra != -1 and la < ra
    if not exist_object and not exist_array:
        raise ValueError("Invalid result format, no JSON found.")
    if not exist_array or (lo < la and ro > ra):
        return text[lo:ro+1].strip()
    elif not exist_object or (la < lo and ra > ro):
        return text[la:ra+1].strip()
    else:
        raise ValueError("Invalid result format, no JSON array found.")

