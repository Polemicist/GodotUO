import struct
import tempfile
import unittest
from pathlib import Path

from guo.uomap import STATIC_FMT, WIDE_MAGIC, WIDE_STATIC_FMT, open_facet

# Synthetic block 0 only: a 196-byte land block, one staidx entry, a few statics records.
RECORDS = [(0x0E75, 1, 2, 5, 0), (0x112C3, 7, 7, -3, 0x21), (0x0000, 0, 0, 0, 0)]


def write_facet(root: Path, wide: bool) -> None:
    (root / 'map4.mul').write_bytes(bytes(4) + struct.pack('<Hb', 3, 0) * 64)
    head = WIDE_MAGIC if wide else b''
    fmt = WIDE_STATIC_FMT if wide else STATIC_FMT
    records = RECORDS if wide else [r for r in RECORDS if r[0] <= 0xFFFF]
    body = b''.join(struct.pack(fmt, *r) for r in records)
    (root / 'statics4.mul').write_bytes(head + body)
    (root / 'staidx4.mul').write_bytes(struct.pack('<iii', len(head), len(body), 0))


class WideStaticsTests(unittest.TestCase):
    def test_stock_file_reads_seven_byte_records(self):
        with tempfile.TemporaryDirectory() as tmp:
            write_facet(Path(tmp), wide=False)
            with open_facet(Path(tmp), 4) as f:
                self.assertFalse(f.wide)
                self.assertEqual(f.read(0, 0).statics, [(0x0E75, 1, 2, 5, 0)])

    def test_wide_file_reads_nine_byte_records_past_its_header(self):
        with tempfile.TemporaryDirectory() as tmp:
            write_facet(Path(tmp), wide=True)
            with open_facet(Path(tmp), 4) as f:
                self.assertTrue(f.wide)
                self.assertEqual(f.static_size, 9)
                self.assertEqual(f.read(0, 0).statics, [(0x0E75, 1, 2, 5, 0), (0x112C3, 7, 7, -3, 0x21)])

    def test_partial_trailing_record_is_ignored(self):
        with tempfile.TemporaryDirectory() as tmp:
            write_facet(Path(tmp), wide=True)
            with open_facet(Path(tmp), 4) as f:
                offset, length = f.statics_span(0)
                self.assertEqual(len(list(f.statics_records(offset, length - 4))), 2)


if __name__ == '__main__':
    unittest.main()
