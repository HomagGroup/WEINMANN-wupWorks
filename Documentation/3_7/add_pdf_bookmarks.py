import re
from pathlib import Path
from html import unescape
from PyPDF2 import PdfReader, PdfWriter


def read_text_with_fallback(path: Path) -> str:
    for enc in ("utf-8", "latin-1", "cp1252"):
        try:
            return path.read_text(encoding=enc)
        except UnicodeDecodeError:
            continue
    return path.read_text(encoding="utf-8", errors="ignore")


def normalize_text(s: str) -> str:
    s = unescape(s)
    s = re.sub(r"\s+", " ", s)
    return s.strip()


def extract_toc_entries(md_text: str) -> list[tuple[str, int, int]]:
    # Parse all TOC rows with indent level and page number.
    table_match = re.search(r"<table class=\"toc-table\">(.*?)</table>", md_text, re.DOTALL | re.IGNORECASE)
    if not table_match:
        return []

    table_html = table_match.group(1)
    row_pattern = re.compile(
        r"<tr>\s*"
        r"<td[^>]*class=\"[^\"]*toc-indent-(\d+)[^\"]*\"[^>]*>"
        r"\s*<a\s+href=\"#[^\"]+\">(.*?)</a>\s*</td>"
        r"\s*<td[^>]*class=\"[^\"]*toc-right[^\"]*\"[^>]*>(\d+)\s*</td>"
        r"\s*</tr>",
        re.IGNORECASE | re.DOTALL,
    )

    entries = []
    for m in row_pattern.finditer(table_html):
        indent = int(m.group(1))
        level = indent + 1
        title_raw = m.group(2)
        page_num = int(m.group(3))
        title = normalize_text(re.sub(r"<[^>]+>", "", title_raw))
        if title:
            entries.append((title, level, page_num))
    return entries


def title_level(title: str) -> int:
    m = re.match(r"^(\d+(?:\.\d+)*)\s+", title)
    if not m:
        return 1
    return m.group(1).count(".") + 1


def title_without_number(title: str) -> str:
    return re.sub(r"^\d+(?:\.\d+)*\s+", "", title).strip()


def find_heading_page_index(reader: PdfReader, title: str, start_page: int) -> int:
    needles = [normalize_text(title).lower()]
    stripped = normalize_text(title_without_number(title)).lower()
    if stripped and stripped not in needles:
        needles.append(stripped)

    page_texts = []
    for p in reader.pages:
        txt = p.extract_text() or ""
        page_texts.append(normalize_text(txt).lower())

    # Prefer forward search to keep bookmark order stable.
    for i in range(start_page, len(page_texts)):
        t = page_texts[i]
        if any(n and n in t for n in needles):
            return i

    # Fallback: full scan.
    for i, t in enumerate(page_texts):
        if any(n and n in t for n in needles):
            return i

    return start_page


def add_bookmarks(pdf_path: Path, md_path: Path) -> None:
    md_text = read_text_with_fallback(md_path)
    toc_entries = extract_toc_entries(md_text)
    if not toc_entries:
        raise RuntimeError("Keine TOC-Titel in der Markdown-Datei gefunden.")

    reader = PdfReader(str(pdf_path))
    writer = PdfWriter()

    for page in reader.pages:
        writer.add_page(page)

    parents: dict[int, object] = {}
    page_count = len(reader.pages)

    for title, level, toc_page in toc_entries:
        page_index = max(0, min(page_count - 1, toc_page - 1))

        # If TOC page points to front-matter mismatch, try text-based correction.
        page_index = find_heading_page_index(reader, title, page_index)

        parent = parents.get(level - 1) if level > 1 else None
        item = writer.add_outline_item(title, page_index, parent=parent)
        parents[level] = item

        # Remove deeper stale levels when moving up.
        stale = [k for k in parents.keys() if k > level]
        for k in stale:
            del parents[k]

    tmp_path = pdf_path.with_suffix(".bookmarked.tmp.pdf")
    with tmp_path.open("wb") as f:
        writer.write(f)

    tmp_path.replace(pdf_path)


if __name__ == "__main__":
    base = Path(__file__).parent
    md_file = base / "Interface_version_3_7.md"
    pdf_file = base / "Interface_version_3_7.pdf"

    if not md_file.exists():
        raise SystemExit(f"Markdown-Datei fehlt: {md_file}")
    if not pdf_file.exists():
        raise SystemExit(f"PDF-Datei fehlt: {pdf_file}")

    add_bookmarks(pdf_file, md_file)
    print(f"Lesezeichen hinzugefuegt: {pdf_file}")
