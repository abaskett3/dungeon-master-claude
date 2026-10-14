"""The PDF extractor, run against a small PDF made on the spot."""
import os
import tempfile
import unittest

from support import has_pymupdf, need, run_script

PAGES = 4
LONG = "The goblins of the Silver Egg keep watch from the gallery. "


def setUpModule():
    need(has_pymupdf(), "PyMuPDF")
    global pymupdf, pdf_extract
    import pymupdf
    import pdf_extract


def make_pdf(path, password=None):
    """Four pages. Page 2 has plenty of text; the others have a line each."""
    doc = pymupdf.open()
    for n in range(1, PAGES + 1):
        page = doc.new_page(width=612, height=792)
        page.insert_text((72, 72), f"Silver Egg page {n}")
        if n == 2:
            for i in range(8):
                page.insert_text((72, 100 + i * 14), LONG)
    if password:
        doc.save(path, encryption=pymupdf.PDF_ENCRYPT_AES_256, user_pw=password, owner_pw=password)
    else:
        doc.save(path)
    doc.close()


class ParsePages(unittest.TestCase):
    def test_default_is_every_page(self):
        self.assertEqual(pdf_extract.parse_pages(None, 3), [1, 2, 3])
        self.assertEqual(pdf_extract.parse_pages("", 3), [1, 2, 3])

    def test_ranges_and_single_pages(self):
        self.assertEqual(pdf_extract.parse_pages("1-3,5", 10), [1, 2, 3, 5])
        self.assertEqual(pdf_extract.parse_pages("7", 10), [7])
        self.assertEqual(pdf_extract.parse_pages(" 2 - 4 , 9 ", 10), [2, 3, 4, 9])
        self.assertEqual(pdf_extract.parse_pages("10-10", 10), [10])

    def test_sorted_without_repeats(self):
        self.assertEqual(pdf_extract.parse_pages("5,1-3,2,1", 10), [1, 2, 3, 5])
        self.assertEqual(pdf_extract.parse_pages("3,,1,", 10), [1, 3])

    def test_pages_outside_the_document(self):
        for spec in ("0", "11", "9-11", "5-2", "0-3"):
            with self.subTest(spec=spec):
                with self.assertRaises(SystemExit) as caught:
                    pdf_extract.parse_pages(spec, 10)
                self.assertIn("outside 1-10", str(caught.exception))


class CommandLine(unittest.TestCase):
    def setUp(self):
        self.tmp = tempfile.TemporaryDirectory()
        self.addCleanup(self.tmp.cleanup)
        self.pdf = os.path.join(self.tmp.name, "module.pdf")
        self.out = os.path.join(self.tmp.name, "out")
        make_pdf(self.pdf)

    def extract(self, command, *args, pdf=None, out=True):
        args = list(args) + (["--out", self.out] if out else [])
        return run_script("pdf_extract.py", command, pdf or self.pdf, *args)

    def listing(self, *parts):
        folder = os.path.join(self.out, *parts)
        return sorted(os.listdir(folder)) if os.path.isdir(folder) else None

    def read(self, *parts):
        with open(os.path.join(self.out, *parts), encoding="utf-8") as handle:
            return handle.read()

    def test_info(self):
        result = self.extract("info")
        self.assertEqual(result.returncode, 0, result.stderr)
        lines = result.stdout.splitlines()
        self.assertEqual(lines[0], f"pages: {PAGES}")
        self.assertEqual([line.split(":")[0] for line in lines[1:PAGES + 1]], ["p1", "p2", "p3", "p4"])
        self.assertRegex(lines[1], r"^p1: 17 chars, 0 images, 8\.5 x 11\.0 in$")
        self.assertTrue(lines[-1].endswith("1, 3, 4"), "pages with little text should be listed")
        self.assertIsNone(self.listing(), "info must not write anything")

    def test_info_for_some_pages_still_reports_the_whole_page_count(self):
        result = self.extract("info", "--pages", "2")
        self.assertEqual(result.stdout.splitlines(), [f"pages: {PAGES}", result.stdout.splitlines()[1]])
        self.assertTrue(result.stdout.splitlines()[1].startswith("p2: "))

    def test_text(self):
        result = self.extract("text")
        self.assertEqual(result.returncode, 0, result.stderr)
        self.assertEqual(self.listing(), ["all-text.txt", "text"])
        self.assertEqual(self.listing("text"), [f"page-00{n}.txt" for n in range(1, PAGES + 1)])
        self.assertEqual(self.read("text", "page-003.txt").strip(), "Silver Egg page 3")
        self.assertIn(LONG.strip(), self.read("text", "page-002.txt"))

        combined = self.read("all-text.txt")
        marks = [combined.index(f"===== PDF PAGE {n} =====\nSilver Egg page {n}") for n in range(1, PAGES + 1)]
        self.assertEqual(marks, sorted(marks))

    def test_text_for_some_pages_leaves_the_combined_file_alone(self):
        result = self.extract("text", "--pages", "2,4")
        self.assertEqual(result.returncode, 0, result.stderr)
        self.assertEqual(self.listing(), ["text"])
        self.assertEqual(self.listing("text"), ["page-002.txt", "page-004.txt"])

    def test_render(self):
        result = self.extract("render", "--pages", "1,3", "--dpi", "40")
        self.assertEqual(result.returncode, 0, result.stderr)
        self.assertIn("render: 2 pages at 40 dpi", result.stdout)
        self.assertEqual(self.listing(), ["pages"])
        self.assertEqual(self.listing("pages"), ["page-001.jpg", "page-003.jpg"])
        pix = pymupdf.Pixmap(os.path.join(self.out, "pages", "page-001.jpg"))
        self.assertEqual((pix.width, pix.height), (340, 440))  # 8.5 x 11 in at 40 dpi

    def test_all(self):
        result = self.extract("all", "--dpi", "40")
        self.assertEqual(result.returncode, 0, result.stderr)
        self.assertEqual(self.listing(), ["all-text.txt", "pages", "text"])
        self.assertEqual(len(self.listing("pages")), PAGES)
        self.assertEqual(len(self.listing("text")), PAGES)

    def test_default_output_is_next_to_the_pdf(self):
        result = self.extract("text", "--pages", "1", out=False)
        self.assertEqual(result.returncode, 0, result.stderr)
        self.assertEqual(os.listdir(os.path.join(self.tmp.name, "extracted", "text")), ["page-001.txt"])

    def test_missing_file(self):
        result = self.extract("info", pdf=os.path.join(self.tmp.name, "nope.pdf"))
        self.assertEqual(result.returncode, 1)
        self.assertIn("No such file", result.stderr)

    def test_page_outside_the_document(self):
        result = self.extract("text", "--pages", "3-9")
        self.assertEqual(result.returncode, 1)
        self.assertIn(f"outside 1-{PAGES}", result.stderr)
        self.assertIsNone(self.listing())

    def test_password_protected(self):
        locked = os.path.join(self.tmp.name, "locked.pdf")
        make_pdf(locked, password="mellon")
        result = self.extract("text", pdf=locked)
        self.assertEqual(result.returncode, 1)
        self.assertIn("password-protected", result.stderr)
        self.assertIsNone(self.listing())


if __name__ == "__main__":
    unittest.main()
