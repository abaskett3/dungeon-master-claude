#!/usr/bin/env python3
"""Extract text and page images from a PDF so DMC can read it.

The Read tool cannot open very large PDFs, and text alone misses maps,
illustrations and handouts. This script writes the text of each page and a
picture of each page to disk; DMC then reads the text files and looks at the
page images with the Read tool.

Usage:
  python scripts/pdf_extract.py info   <pdf>
  python scripts/pdf_extract.py text   <pdf> [--pages 1-20,35] [--out DIR]
  python scripts/pdf_extract.py render <pdf> [--pages 1-20,35] [--dpi 110] [--out DIR]
  python scripts/pdf_extract.py all    <pdf> [--pages ...] [--dpi 110] [--out DIR]

Output (default DIR is "extracted" next to the PDF):
  DIR/all-text.txt         every page's text, split by "===== PDF PAGE N =====" lines
  DIR/text/page-NNN.txt    one file per page
  DIR/pages/page-NNN.jpg   one picture per page

Page numbers are PDF page numbers, starting at 1.

Needs the PyMuPDF library. Install it into the toolkit (not system-wide) with:
  python -m pip install --target scripts/.pylib -r scripts/requirements.txt
"""
import argparse
import os
import sys

HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, os.path.join(HERE, ".pylib"))

try:
    import pymupdf as fitz  # PyMuPDF
except ImportError:
    sys.exit(
        "PyMuPDF is not installed. Run:\n"
        "  python -m pip install --target scripts/.pylib -r scripts/requirements.txt"
    )

# A page with less text than this is probably a scan, a map or full-page art.
SPARSE_TEXT = 200


def parse_pages(spec, count):
    """Turn "1-20,35" into a sorted list of 1-based page numbers."""
    if not spec:
        return list(range(1, count + 1))
    pages = set()
    for part in spec.split(","):
        part = part.strip()
        if not part:
            continue
        first, _, last = part.partition("-")
        first = int(first)
        last = int(last) if last else first
        if first < 1 or last > count or first > last:
            sys.exit(f"Page range {part!r} is outside 1-{count}.")
        pages.update(range(first, last + 1))
    return sorted(pages)


def page_text(page):
    try:
        return page.get_text("text", sort=True)
    except Exception as e:  # keep going on a bad page
        return f"[text extraction failed: {e}]"


def cmd_info(doc, pages, args):
    print(f"pages: {doc.page_count}")
    sparse = []
    for n in pages:
        page = doc[n - 1]
        chars = len(page_text(page).strip())
        images = len(page.get_images(full=True))
        w, h = page.rect.width / 72, page.rect.height / 72
        print(f"p{n}: {chars} chars, {images} images, {w:.1f} x {h:.1f} in")
        if chars < SPARSE_TEXT:
            sparse.append(n)
    if sparse:
        print("little or no text (scan, map or art; look at the page image):",
              ", ".join(str(n) for n in sparse))


def cmd_text(doc, pages, args):
    text_dir = os.path.join(args.out, "text")
    os.makedirs(text_dir, exist_ok=True)
    whole = len(pages) == doc.page_count
    combined = []
    for n in pages:
        t = page_text(doc[n - 1])
        with open(os.path.join(text_dir, f"page-{n:03d}.txt"), "w", encoding="utf-8") as f:
            f.write(t)
        combined.append(f"===== PDF PAGE {n} =====\n{t}")
    if whole:
        with open(os.path.join(args.out, "all-text.txt"), "w", encoding="utf-8") as f:
            f.write("\n\n".join(combined))
    print(f"text: {len(pages)} pages -> {text_dir}"
          + (f" and {os.path.join(args.out, 'all-text.txt')}" if whole else ""))


def cmd_render(doc, pages, args):
    page_dir = os.path.join(args.out, "pages")
    os.makedirs(page_dir, exist_ok=True)
    for n in pages:
        pix = doc[n - 1].get_pixmap(dpi=args.dpi, alpha=False)
        pix.save(os.path.join(page_dir, f"page-{n:03d}.jpg"), jpg_quality=85)
    print(f"render: {len(pages)} pages at {args.dpi} dpi -> {page_dir}")


def main():
    ap = argparse.ArgumentParser(description="Extract text and page images from a PDF.")
    ap.add_argument("command", choices=["info", "text", "render", "all"])
    ap.add_argument("pdf")
    ap.add_argument("--pages", help='PDF pages, for example "1-20,35". Default: all.')
    ap.add_argument("--dpi", type=int, default=110, help="Resolution of page images (default 110).")
    ap.add_argument("--out", help='Output folder. Default: "extracted" next to the PDF.')
    args = ap.parse_args()

    if not os.path.isfile(args.pdf):
        sys.exit(f"No such file: {args.pdf}")
    if not args.out:
        args.out = os.path.join(os.path.dirname(os.path.abspath(args.pdf)), "extracted")

    doc = fitz.open(args.pdf)
    if doc.needs_pass:
        sys.exit("This PDF is password-protected. Ask the player for an unlocked copy.")
    pages = parse_pages(args.pages, doc.page_count)

    if args.command == "info":
        cmd_info(doc, pages, args)
    if args.command in ("text", "all"):
        cmd_text(doc, pages, args)
    if args.command in ("render", "all"):
        cmd_render(doc, pages, args)


if __name__ == "__main__":
    main()
