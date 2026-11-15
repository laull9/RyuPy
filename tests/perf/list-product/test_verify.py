#!/usr/bin/env python3
"""产品门禁合成正负例；不冒充实际编译/性能验收。"""
import copy
import json
from pathlib import Path
import tempfile
import unittest
import verify


class PerformanceGate(unittest.TestCase):
    def setUp(self):
        self.temp = tempfile.TemporaryDirectory()
        self.addCleanup(self.temp.cleanup)
        self.path = Path(self.temp.name) / "samples.json"
        self.data = {"runs": 5, "warmup": 1, "rows": [
            {"name": b.name, "samples": {arm: [{"ms": 1, "rss": 42}] * 5 for arm in ("CP", "A", "B")},
             "stats": {arm: {"main_ms": 1, "alloc_bytes": 1} for arm in ("A", "B")}}
            for b in verify.suite.discover(verify.ROOT / "tests/benchmarks")]}

    def run_gate(self):
        self.path.write_text(json.dumps(self.data))
        verify.performance(self.path)

    def test_valid(self): self.run_gate()

    def test_partial(self):
        self.data["rows"].pop()
        with self.assertRaises(AssertionError): self.run_gate()

    def test_duplicate(self):
        self.data["rows"][0] = copy.deepcopy(self.data["rows"][1])
        with self.assertRaises(AssertionError): self.run_gate()

    def test_missing_arm(self):
        del self.data["rows"][0]["samples"]["B"]
        with self.assertRaises(AssertionError): self.run_gate()

    def test_short_samples(self):
        self.data["rows"][0]["samples"]["A"].pop()
        with self.assertRaises(AssertionError): self.run_gate()

    def test_missing_stats(self):
        del self.data["rows"][0]["stats"]["A"]["alloc_bytes"]
        with self.assertRaises(AssertionError): self.run_gate()


if __name__ == "__main__": unittest.main()
