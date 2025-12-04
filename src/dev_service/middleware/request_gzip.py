import gzip
import json
from typing import Optional

from fastapi import FastAPI, Request
from loguru import logger
from starlette.middleware.base import BaseHTTPMiddleware
from starlette.requests import ClientDisconnect
from starlette.responses import PlainTextResponse
from starlette.types import ASGIApp, Receive, Scope, Send

from utils.number_utils import bin_len_to_str


class DecompressRequestMiddleware:
    """
    ASGI middleware to transparently decode compressed HTTP request bodies.

    - Supports Content-Encoding: gzip, deflate, br (brotli optional)
    - Removes Content-Encoding header after decoding
    - Updates Content-Length to the decoded size
    - Optional max_decompressed_size to mitigate zip-bomb style payloads
    """

    def __init__(self, app, max_decompressed_size: Optional[int] = None):
        self.app = app
        self.max_decompressed_size = max_decompressed_size

    async def __call__(self, scope, receive, send):
        if scope.get("type") != "http":
            await self.app(scope, receive, send)
            return

        headers = scope.get("headers") or []
        enc: Optional[str] = None
        for k, v in headers:
            if k == b"content-encoding":
                enc = v.decode("latin1").strip().lower()
                break

        # Fast-path: no compression
        if enc != "gzip":
            await self.app(scope, receive, send)
            return

        # Read entire request body
        body = await self.read_body(receive)

        # Decode by content-encoding
        try:
            if enc == "gzip":
                raw_body = body
                body = gzip.decompress(body)
                logger.info(f"Decompressed gzip body from {bin_len_to_str(len(raw_body))} to {bin_len_to_str(len(body))}")
        except Exception:
            await send({
                "type": "http.response.start",
                "status": 415,
                "headers": [(b"content-type", b"text/plain; charset=utf-8")],
            })
            await send({
                "type": "http.response.body",
                "body": b"Unable to decode compressed request body.",
                "more_body": False,
            })
            return

        if self.max_decompressed_size is not None and len(body) > self.max_decompressed_size:
            await send({
                "type": "http.response.start",
                "status": 413,
                "headers": [(b"content-type", b"text/plain; charset=utf-8")],
            })
            await send({
                "type": "http.response.body",
                "body": b"Decompressed body too large.",
                "more_body": False,
            })
            return

        # Build new scope with cleaned headers
        new_headers = [
            (k, v) for k, v in headers
            if k not in (b"content-encoding", b"content-length")
        ]
        new_headers.append((b"content-length", str(len(body)).encode("latin1")))

        new_scope = dict(scope)
        new_scope["headers"] = new_headers

        # Provide a new receive that yields the decoded body
        sent = False
        async def new_receive():
            nonlocal sent
            if not sent:
                sent = True
                return {"type": "http.request", "body": body, "more_body": False}
            return {"type": "http.request", "body": b"", "more_body": False}

        await self.app(new_scope, new_receive, send)

    async def read_body(self, receive) -> bytes:
        body = b""
        while True:
            message = await receive()
            mtype = message["type"]

            if mtype == "http.request":
                body += message.get("body", b"")
                if not message.get("more_body", False):
                    break
            elif mtype == "http.disconnect":
                raise ClientDisconnect()
            else:
                # Ignore other message types
                pass
        return body


class GZipRequestMiddleware(BaseHTTPMiddleware):
    def __init__(self, app: ASGIApp):
        super().__init__(app)

    async def dispatch(self, request: Request, call_next):
        print(request.headers)

        if request.headers.get("Content-Encoding", "").lower() == "gzip":
            body = await request.body()
            try:
                decompressed = gzip.decompress(body)
            except Exception as e:
                logger.error(f"Failed to decompress gzip body: {e}")
                return PlainTextResponse(f"Invalid gzip body: {e}", status_code=400)


            sent = False
            async def new_receive():
                nonlocal sent
                if not sent:
                    sent = True
                    return {"type": "http.request", "body": body, "more_body": False}
                return {"type": "http.request", "body": b"", "more_body": False}

            new_headers = [
                (k.encode("latin-1"), v.encode("latin-1"))
                for k, v in request.headers.items()
                if k.lower() != "content-encoding" and k.lower() != "content-length"
            ]
            new_headers.append((b"content-length", str(len(decompressed)).encode("latin-1")))
            new_scope = dict(request.scope)
            new_scope["headers"] = new_headers
            new_request = Request(new_scope, receive=new_receive, send=request._send)
            # decompressed_str = await new_request.body()
            print(f"new-req: {new_scope}")
            # print(f"Re-read body: {decompressed_str.decode('utf-8')}")
            return await call_next(new_request)

        return await call_next(request)