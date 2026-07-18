from __future__ import annotations

import json
import os
import re
import html as html_lib
from dataclasses import dataclass
from html.parser import HTMLParser
from pathlib import Path
from typing import Any

from loguru import logger


DEFAULT_SCRIPT_REFERENCE_DIR = Path(
    r"C:\Program Files\Unity\Hub\Editor\2022.3.53f1c1\Editor\Data\Documentation\en\ScriptReference"
)
CACHE_DIR = Path("cache/unity_script_reference_markdown")
INCLUDE_PROPERTY_DESCRIPTION_PAGES = True
CLEANER_VERSION = "unity-script-reference-html-to-markdown-v3"
NOT_FOUND = "Not Found"

_VOID_TAGS = {"area", "base", "br", "col", "embed", "hr", "img", "input", "link", "meta", "param", "source"}
_SKIP_TAGS = {"head", "script", "style", "noscript", "iframe", "svg", "canvas", "form", "button", "input"}
_SKIP_CLASSES = {
    "footer",
    "footer-wrapper",
    "header",
    "header-wrapper",
    "lang-list",
    "lang-switcher",
    "loading",
    "menu",
    "more",
    "otherversionscontent",
    "scrollToFeedback",
    "search-form",
    "suggest",
    "suggest-failed",
    "suggest-form",
    "suggest-success",
    "suggest-wrap",
    "switch-link",
    "toolbar",
}
_SKIP_IDS = {"DocsAnalyticsData", "OtherVersionsContent", "scrollToFeedback", "ot-sdk-btn-container", "ot-sdk-btn"}


@dataclass(frozen=True)
class ResolvedDocument:
    page_id: str
    path: Path


@dataclass(frozen=True)
class PropertyDocument:
    name: str
    page_id: str
    path: Path


def query_builtin_component_document(full_qualified_name: str) -> str:
    """Return a cleaned Markdown copy of a Unity ScriptReference HTML page."""
    document = _resolve_document(full_qualified_name)
    if document is None:
        logger.warning("Unity ScriptReference document not found: {}", full_qualified_name)
        return NOT_FOUND
    return _load_or_create_markdown(document)


def _script_reference_dir() -> Path:
    configured = os.environ.get("UNITY_SCRIPT_REFERENCE_DIR")
    if configured:
        return Path(configured)
    return DEFAULT_SCRIPT_REFERENCE_DIR


def _resolve_document(full_qualified_name: str) -> ResolvedDocument | None:
    script_reference_dir = _script_reference_dir()
    if not script_reference_dir.exists():
        logger.warning("Unity ScriptReference directory does not exist: {}", script_reference_dir)
        return None

    for page_id in _candidate_page_ids(full_qualified_name):
        path = script_reference_dir / f"{page_id}.html"
        if path.exists():
            return ResolvedDocument(page_id=page_id, path=path)

    index_match = _resolve_from_index(full_qualified_name, script_reference_dir)
    if index_match is not None:
        return index_match
    return None


def _candidate_page_ids(full_qualified_name: str) -> list[str]:
    name = full_qualified_name.strip()
    if name.startswith("global::"):
        name = name[len("global::"):]
    candidates: list[str] = []

    def add(value: str) -> None:
        value = value.strip().strip(".")
        if value and value not in candidates:
            candidates.append(value)

    add(name)
    for prefix in ("UnityEngine.", "UnityEditor."):
        if name.startswith(prefix):
            add(name[len(prefix):])
    if "." in name:
        add(name.split(".")[-1])
    return candidates


def _resolve_from_index(full_qualified_name: str, script_reference_dir: Path) -> ResolvedDocument | None:
    pages = _load_index_pages(script_reference_dir)
    if not pages:
        return None

    candidates = {item.lower() for item in _candidate_page_ids(full_qualified_name)}
    short_name = full_qualified_name.strip().split(".")[-1].lower()
    exact_matches: list[str] = []
    short_matches: list[str] = []
    for page in pages:
        if not isinstance(page, list) or len(page) < 2:
            continue
        page_id = str(page[0])
        display_name = str(page[1])
        if page_id.lower() in candidates or display_name.lower() in candidates:
            exact_matches.append(page_id)
        elif display_name.lower() == short_name or page_id.lower() == short_name:
            short_matches.append(page_id)

    for page_id in [*exact_matches, *short_matches]:
        path = script_reference_dir / f"{page_id}.html"
        if path.exists():
            return ResolvedDocument(page_id=page_id, path=path)
    return None


def _load_index_pages(script_reference_dir: Path) -> list[Any]:
    index_path = script_reference_dir / "docdata" / "index.json"
    if not index_path.exists():
        return []
    try:
        with index_path.open("r", encoding="utf-8") as f:
            data = json.load(f)
        pages = data.get("pages", [])
        return pages if isinstance(pages, list) else []
    except Exception as e:
        logger.warning("Failed to read Unity ScriptReference index {}: {}", index_path, e)
        return []


def _load_or_create_markdown(document: ResolvedDocument) -> str:
    source_stat = document.path.stat()
    html = document.path.read_text(encoding="utf-8", errors="replace")
    property_documents = _resolve_property_documents(document, html) if INCLUDE_PROPERTY_DESCRIPTION_PAGES else []
    cache_path = _cache_markdown_path(document.page_id)
    meta_path = _cache_meta_path(document.page_id)
    meta = _read_meta(meta_path)
    expected_meta = {
        "cleanerVersion": CLEANER_VERSION,
        "includePropertyDescriptionPages": INCLUDE_PROPERTY_DESCRIPTION_PAGES,
        "sourcePath": str(document.path),
        "sourceMtime": source_stat.st_mtime,
        "sourceSize": source_stat.st_size,
        "propertySources": [
            {
                "name": property_document.name,
                "pageId": property_document.page_id,
                "sourcePath": str(property_document.path),
                "sourceMtime": property_document.path.stat().st_mtime,
                "sourceSize": property_document.path.stat().st_size,
            }
            for property_document in property_documents
        ],
    }
    if cache_path.exists() and meta == expected_meta:
        logger.info("Unity ScriptReference Markdown cache hit: {}", cache_path)
        return cache_path.read_text(encoding="utf-8")

    markdown = html_to_markdown(html)
    if INCLUDE_PROPERTY_DESCRIPTION_PAGES:
        markdown = _append_property_descriptions(markdown, property_documents)
    CACHE_DIR.mkdir(parents=True, exist_ok=True)
    cache_path.write_text(markdown, encoding="utf-8")
    meta_path.write_text(json.dumps(expected_meta, indent=2, ensure_ascii=False), encoding="utf-8")
    logger.info("Unity ScriptReference Markdown cache saved: {}", cache_path)
    return markdown


def _read_meta(path: Path) -> dict[str, Any] | None:
    if not path.exists():
        return None
    try:
        with path.open("r", encoding="utf-8") as f:
            data = json.load(f)
        return data if isinstance(data, dict) else None
    except Exception:
        return None


def _cache_markdown_path(page_id: str) -> Path:
    return CACHE_DIR / f"{_safe_cache_name(page_id)}.md"


def _cache_meta_path(page_id: str) -> Path:
    return CACHE_DIR / f"{_safe_cache_name(page_id)}.json"


def _safe_cache_name(page_id: str) -> str:
    return re.sub(r'[<>:"/\\|?*]+', "_", page_id)


def _resolve_property_documents(document: ResolvedDocument, html: str) -> list[PropertyDocument]:
    script_reference_dir = document.path.parent
    documents: list[PropertyDocument] = []
    seen: set[str] = set()
    for name, href in _property_links(html):
        page_id = Path(href).stem
        if page_id in seen:
            continue
        seen.add(page_id)
        path = script_reference_dir / f"{page_id}.html"
        if path.exists():
            documents.append(PropertyDocument(name=name, page_id=page_id, path=path))
    return documents


def _property_links(html: str) -> list[tuple[str, str]]:
    properties_match = re.search(
        r"<h[23][^>]*>\s*Properties\s*</h[23]>(?P<section>.*?)(?=<h[23][^>]*>)",
        html,
        flags=re.IGNORECASE | re.DOTALL,
    )
    if properties_match is None:
        return []

    section = properties_match.group("section")
    links: list[tuple[str, str]] = []
    for match in re.finditer(
        r'<a\s+[^>]*href="(?P<href>[^"]+\.html)"[^>]*>(?P<name>.*?)</a>',
        section,
        flags=re.IGNORECASE | re.DOTALL,
    ):
        href = html_lib.unescape(match.group("href")).strip()
        if "/" in href or "\\" in href:
            continue
        name_html = match.group("name")
        name = re.sub(r"<[^>]+>", "", name_html)
        name = html_lib.unescape(re.sub(r"\s+", " ", name)).strip()
        if name:
            links.append((name, href))
    return links


def _append_property_descriptions(markdown: str, property_documents: list[PropertyDocument]) -> str:
    sections: list[str] = []
    for property_document in property_documents:
        try:
            property_html = property_document.path.read_text(encoding="utf-8", errors="replace")
        except OSError as e:
            logger.warning("Failed to read Unity ScriptReference property page {}: {}", property_document.path, e)
            continue

        description = _extract_description_markdown(html_to_markdown(property_html))
        if description:
            sections.append(f"### {property_document.name}\n\n{description.strip()}")

    if not sections:
        return markdown
    return markdown.rstrip() + "\n\n## Property Descriptions\n\n" + "\n\n".join(sections) + "\n"


def _extract_description_markdown(markdown: str) -> str:
    lines = markdown.splitlines()
    start_index: int | None = None
    for index, line in enumerate(lines):
        if line.strip().lower() in {"## description", "### description"}:
            start_index = index + 1
            break
    if start_index is None:
        return ""

    collected: list[str] = []
    for line in lines[start_index:]:
        if line.startswith("#"):
            break
        collected.append(line)
    return "\n".join(collected).strip()


def html_to_markdown(html: str) -> str:
    parser = _UnityScriptReferenceMarkdownParser()
    parser.feed(html)
    parser.close()
    return parser.markdown()


class _UnityScriptReferenceMarkdownParser(HTMLParser):
    def __init__(self) -> None:
        super().__init__(convert_charrefs=True)
        self._parts: list[str] = []
        self._capture = False
        self._content_depth = 0
        self._skip_depth = 0
        self._pre_depth = 0
        self._inline_code_depth = 0
        self._bold_depth = 0
        self._list_stack: list[str] = []
        self._table_depth = 0
        self._table_rows: list[list[str]] = []
        self._current_row: list[str] | None = None
        self._current_cell: list[str] | None = None

    def handle_starttag(self, tag: str, attrs: list[tuple[str, str | None]]) -> None:
        attr_map = {name: value or "" for name, value in attrs}
        if not self._capture:
            if tag == "div" and attr_map.get("id") == "content-wrap":
                self._capture = True
                self._content_depth = 1
            return

        if tag not in _VOID_TAGS:
            self._content_depth += 1

        if self._skip_depth > 0:
            if tag not in _VOID_TAGS:
                self._skip_depth += 1
            return

        if self._should_skip(tag, attr_map):
            if tag not in _VOID_TAGS:
                self._skip_depth = 1
            return

        if self._table_depth > 0 or tag == "table":
            self._handle_table_start(tag)
            return

        if tag in {"h1", "h2", "h3", "h4"}:
            level = {"h1": 1, "h2": 2, "h3": 3, "h4": 4}[tag]
            self._block()
            self._append_raw("#" * level + " ")
        elif tag == "p":
            self._block()
        elif tag == "br":
            self._append_raw("\n")
        elif tag == "hr":
            self._block()
            self._append_raw("---")
            self._block()
        elif tag == "pre":
            self._block()
            language = "csharp" if "codeExampleCS" in attr_map.get("class", "") else ""
            self._append_raw(f"```{language}\n")
            self._pre_depth += 1
        elif tag == "code" and self._pre_depth == 0:
            self._append_raw("`")
            self._inline_code_depth += 1
        elif tag in {"strong", "b"} and self._pre_depth == 0:
            self._append_raw("**")
            self._bold_depth += 1
        elif tag in {"ul", "ol"}:
            self._block()
            self._list_stack.append(tag)
        elif tag == "li":
            self._append_raw("\n- ")
        elif tag == "div" and "subsection" in attr_map.get("class", ""):
            self._block()

    def handle_endtag(self, tag: str) -> None:
        if not self._capture:
            return

        if self._skip_depth > 0:
            if tag not in _VOID_TAGS:
                self._skip_depth -= 1
                self._content_depth -= 1
                self._finish_capture_if_needed()
            return

        if self._table_depth > 0:
            self._handle_table_end(tag)
        elif tag in {"h1", "h2", "h3", "h4", "p", "div"}:
            self._block()
        elif tag == "pre":
            self._append_raw("\n```")
            self._block()
            self._pre_depth = max(0, self._pre_depth - 1)
        elif tag == "code" and self._inline_code_depth > 0:
            self._append_raw("`")
            self._inline_code_depth -= 1
        elif tag in {"strong", "b"} and self._bold_depth > 0:
            self._append_raw("**")
            self._bold_depth -= 1
        elif tag in {"ul", "ol"}:
            if self._list_stack:
                self._list_stack.pop()
            self._block()
        elif tag == "li":
            self._append_raw("\n")

        if tag not in _VOID_TAGS:
            self._content_depth -= 1
            self._finish_capture_if_needed()

    def handle_startendtag(self, tag: str, attrs: list[tuple[str, str | None]]) -> None:
        self.handle_starttag(tag, attrs)

    def handle_data(self, data: str) -> None:
        if not self._capture or self._skip_depth > 0:
            return
        if self._table_depth > 0:
            if self._current_cell is not None:
                self._current_cell.append(data)
            return
        if self._pre_depth > 0:
            self._append_raw(data)
            return
        text = re.sub(r"\s+", " ", data)
        if not text.strip():
            return
        self._append_text(text)

    def markdown(self) -> str:
        text = "".join(self._parts)
        text = re.sub(r"[ \t]+\n", "\n", text)
        text = re.sub(r"\n{3,}", "\n\n", text)
        lines = [line.rstrip() for line in text.splitlines()]
        lines = [line for line in lines if line.strip() != "/"]
        cleaned = "\n".join(lines).strip()
        return cleaned + "\n" if cleaned else ""

    def _should_skip(self, tag: str, attrs: dict[str, str]) -> bool:
        if tag in _SKIP_TAGS:
            return True
        if attrs.get("id") in _SKIP_IDS:
            return True
        classes = set(attrs.get("class", "").split())
        return bool(classes & _SKIP_CLASSES)

    def _handle_table_start(self, tag: str) -> None:
        if tag == "table":
            self._table_depth += 1
            if self._table_depth == 1:
                self._table_rows = []
        elif tag == "tr" and self._table_depth == 1:
            self._current_row = []
        elif tag in {"td", "th"} and self._current_row is not None:
            self._current_cell = []
        elif tag == "br" and self._current_cell is not None:
            self._current_cell.append("\n")

    def _handle_table_end(self, tag: str) -> None:
        if tag in {"td", "th"} and self._current_row is not None and self._current_cell is not None:
            self._current_row.append(_normalize_cell_text("".join(self._current_cell)))
            self._current_cell = None
        elif tag == "tr" and self._current_row is not None:
            if any(cell for cell in self._current_row):
                self._table_rows.append(self._current_row)
            self._current_row = None
        elif tag == "table":
            self._table_depth -= 1
            if self._table_depth == 0:
                self._append_table()

    def _append_table(self) -> None:
        if not self._table_rows:
            return
        width = max(len(row) for row in self._table_rows)
        rows = [row + [""] * (width - len(row)) for row in self._table_rows]
        self._block()
        header = rows[0]
        self._append_raw("| " + " | ".join(header) + " |\n")
        self._append_raw("| " + " | ".join("---" for _ in header) + " |\n")
        for row in rows[1:]:
            self._append_raw("| " + " | ".join(row) + " |\n")
        self._block()

    def _append_text(self, text: str) -> None:
        if not self._parts:
            self._parts.append(text.strip())
            return
        previous = self._parts[-1]
        value = text.strip()
        if not value:
            return
        if previous.endswith(("\n", " ", "`", "**", "(", "[", "/", ".")):
            self._parts.append(value)
        elif value.startswith((".", ",", ":", ";", ")", "]", "/", "'")):
            self._parts.append(value)
        else:
            self._parts.append(" " + value)

    def _append_raw(self, text: str) -> None:
        self._parts.append(text)

    def _block(self) -> None:
        current = "".join(self._parts)
        if not current:
            return
        if current.endswith("\n\n"):
            return
        if current.endswith("\n"):
            self._parts.append("\n")
        else:
            self._parts.append("\n\n")

    def _finish_capture_if_needed(self) -> None:
        if self._content_depth <= 0:
            self._capture = False


def _normalize_cell_text(text: str) -> str:
    text = re.sub(r"\s+", " ", text).strip()
    return text.replace("|", "\\|")
