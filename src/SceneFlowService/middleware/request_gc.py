import gc
import re
from typing import Optional, List

from loguru import logger
from starlette.middleware.base import BaseHTTPMiddleware
from starlette.requests import Request


class GCMiddleware(BaseHTTPMiddleware):
    request_matcher: Optional[List[str]] = None

    def __init__(self, app, request_matcher=None):
        super().__init__(app)
        self.request_matcher = request_matcher

    async def dispatch(self, request: Request, call_next):
        if not self.match_request(request):
            return await call_next(request)
        response = await call_next(request)
        logger.info("Run garbage collection after request: {}", request.url.path)
        gc.collect()
        return response

    def match_request(self, request: Request) -> bool:
        if not self.request_matcher:
            return True
        for path in self.request_matcher:
            if request.url.path.find(path) != -1:
                return True
            if re.match(path, request.url.path):
                return True
        return False
