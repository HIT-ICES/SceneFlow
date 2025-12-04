import asyncio
import logging
import time
from typing import Optional

from autogen_agentchat.base import TaskResult
from autogen_agentchat.conditions import MaxMessageTermination, SourceMatchTermination
from autogen_agentchat.messages import StructuredMessage
from autogen_agentchat.teams import RoundRobinGroupChat

from agents.dynamic_detection_agent import DynamicDetectionAgent, DynamicDetectionAnalyzeAgent, \
    DynamicDetectionSummaryAgent, DynamicDetectionResultMessage
from agents.model import MODEL_CLIENT_DETECT
# 引入日志
import logging_config  # noqa: F401
from loguru import logger
import os
import json
import hashlib


def script_detect_cache_key(script_name: str, script_content: str) -> str:
    m = hashlib.sha1()
    m.update(script_content.encode('utf-8'))
    return script_name + "-" + m.hexdigest()


def load_script_detect_cache(script_name: str, script_content: str) -> Optional[DynamicDetectionResultMessage]:
    cache_key = script_detect_cache_key(script_name, script_content)
    cache_path = f"cache/dynamic_detection/{cache_key}.json"
    if not os.path.exists(cache_path):
        return None
    with open(cache_path, "r", encoding="utf-8") as f:
        try:
            return DynamicDetectionResultMessage.model_validate_json(f.read())
        except Exception as e:
            logger.error(f"load_script_detect_cache error: {e}")
            f.close()
            os.remove(cache_path)
            return None


def save_script_detect_cache(script_name: str, script_content: str, result: DynamicDetectionResultMessage,
                             task_result: TaskResult):
    cache_key = script_detect_cache_key(script_name, script_content)
    cache_path = f"cache/dynamic_detection/{cache_key}.json"
    os.makedirs(os.path.dirname(cache_path), exist_ok=True)
    with open(cache_path, "w", encoding="utf-8") as f:
        try:
            json_str = result.model_dump_json()
            f.write(json_str)
        except Exception as e:
            logger.error(f"save result error: {e}")
            f.close()
            os.remove(cache_path)
    # cache_content_path = f"cache/dynamic_detection/{cache_key}.cs"
    # with open(cache_content_path, "w", encoding="utf-8") as f:
    #     try:
    #         f.write(script_content)
    #     except Exception:
    #         logger.error(f"save content error: {script_content}")
    #         f.close()
    #         os.remove(cache_content_path)
    cache_task_path = f"cache/dynamic_detection/{cache_key}.task.log"
    with open(cache_task_path, "w", encoding="utf-8") as f:
        try:
            for msg in task_result.messages:
                f.write("=======================================\n")
                f.write(f"{msg.source}:\n")
                f.write(msg.to_text() + "\n")
        except Exception as e:
            logger.error(f"save task error: {e}")
            f.close()
            os.remove(cache_task_path)


async def detect_script2(script_name: str, script_content: str, use_cache: bool = True):
    if use_cache:
        cache = load_script_detect_cache(script_name, script_content)
        if cache is not None:
            logger.info(f"cache hit: script [{script_name}]")
            return cache.results
    agent1 = DynamicDetectionAnalyzeAgent(MODEL_CLIENT_DETECT)
    agent2 = DynamicDetectionSummaryAgent(MODEL_CLIENT_DETECT)
    team = RoundRobinGroupChat(
        [agent1, agent2],
        termination_condition=SourceMatchTermination([agent2.name]),
        custom_message_types=[StructuredMessage[DynamicDetectionResultMessage]],
    )
    task_result = await team.run(task=script_content)
    # logger.debug(f"Full messages for script [{script_name}]: {task_result.messages}")
    if not isinstance(task_result.messages[-1], StructuredMessage) or not isinstance(task_result.messages[-1].content,
                                                                                DynamicDetectionResultMessage):
        logger.error(f"Error final message script [{script_name}]: {task_result.messages[-1]}")
        raise ValueError("Invalid result format, no JSON found.")
    result = task_result.messages[-1].content
    logger.info(f"Result script [{script_name}]: {result}")
    save_script_detect_cache(script_name, script_content, result, task_result)
    return result.results


async def detect_script(script_content: str):
    agent = DynamicDetectionAgent(MODEL_CLIENT_DETECT)
    result = await agent.detect(script_content)
    return result


async def repeat_detect_script(script_content: str, times: int = 3):
    agent = DynamicDetectionAgent(MODEL_CLIENT_DETECT)
    tasks = []
    for _ in range(times):
        tasks.append(agent.detect(script_content))
    for idx, r in enumerate(await asyncio.gather(*tasks)):
        logger.info("repeat_detect_script 第{}次 结果: {}", idx + 1, r)


if __name__ == "__main__":
    for name in [
        "autogen_agentchat",
        "autogen",
        "autogen_core",
        "autogen_ext",
    ]:
        logging.getLogger(name).setLevel(logging.WARNING)
        logging.getLogger(name).propagate = False
    # path = "data/XRHoverHighlight.cs"
    path = "data/PayByCard.cs"
    # path = "data/Behaviours.cs"
    # path = "data/NPCFollow.cs"
    # path = "data/ItemInfoInteractable.cs"
    t = time.time()
    with open(path, "r", encoding="utf-8") as f:
        script_content = f.read()
    asyncio.run(detect_script(script_content))
    # result = asyncio.run(detect_script(script_content))
    # logger.info("result: {}", result)
    logger.info("time: {}", time.time() - t)
