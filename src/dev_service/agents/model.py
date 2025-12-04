from autogen_ext.models.openai import OpenAIChatCompletionClient

MODEL_CLIENT_DETECT = OpenAIChatCompletionClient(
    model="gpt-5-mini",
    api_key="",
    base_url="",
    timeout=100,
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
