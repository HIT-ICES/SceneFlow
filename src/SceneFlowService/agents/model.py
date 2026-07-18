from autogen_ext.models.openai import OpenAIChatCompletionClient

MODEL_CLIENT_DETECT = OpenAIChatCompletionClient(
    model="gpt-5.4-mini-high",
    api_key="",
    base_url="",
    timeout=600,
    model_info={
        "vision": False,
        "function_calling": True,
        "json_output": True,
        "family": "gpt-5",
        "structured_output": True,
    },
    temperature=0.1,
    seed=42,
)
