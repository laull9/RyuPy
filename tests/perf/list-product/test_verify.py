#!/usr/bin/env python3
"""产品门禁合成正负例；不冒充实际编译/性能验收。"""
import copy
import json
from pathlib import Path
import tempfile
import unittest
import verify
from unittest.mock import patch
from aot_pair_evidence import save_manifest, require_space, cleanup_case


class PerformanceGate(unittest.TestCase):
    def setUp(self):
        self.temp = tempfile.TemporaryDirectory()
        self.addCleanup(self.temp.cleanup)
        self.path = Path(self.temp.name) / "samples.json"
        self.data = {"runs": 5, "warmup": 1, "rows": [
            {"name": b.name, "samples": {arm: [{"ms": 1, "rss": 42}] * 5 for arm in ("CP", "A", "B")},
             "stats": {arm: {"main_ms": 1, "alloc_bytes": 1} for arm in ("A", "B")}}
            for b in verify.suite.discover(verify.ROOT / "tests/benchmarks")]}

        for row in self.data["rows"]:
            for arm in ("A", "B"):
                manifest = self.path.parent / "manifests" / row["name"] / arm / "ryupy-artifact.json"
                manifest.parent.mkdir(parents=True)
                manifest.write_text(json.dumps({"toolchain": {"sdk_version": "10.0.403"}}))

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

    def test_wrong_actual_sdk(self):
        manifest = next((self.path.parent / "manifests").rglob("*.json"))
        manifest.write_text(json.dumps({"toolchain": {"sdk_version": "11.0.100"}}))
        with self.assertRaises(AssertionError): self.run_gate()

    def test_missing_actual_sdk(self):
        manifest = next((self.path.parent / "manifests").rglob("*.json"))
        manifest.unlink()
        with self.assertRaises(FileNotFoundError): self.run_gate()


class BuildEvidence(unittest.TestCase):
    def setUp(self):
        self.temp = tempfile.TemporaryDirectory()
        self.addCleanup(self.temp.cleanup)
        self.root = Path(self.temp.name)

    def test_wrong_sdk_preserved_before_rejection(self):
        exe = self.root / "app" / "main"
        exe.parent.mkdir()
        raw = '{"toolchain":{"sdk_version":"11.0.100","target_framework_major":10}}'
        (exe.parent / "ryupy-artifact.json").write_text(raw)
        with self.assertRaises(AssertionError): save_manifest(exe, self.root / "evidence", "A", "case")
        self.assertEqual((self.root / "evidence/case/A/ryupy-artifact.json").read_text(), raw)

    def test_valid_sdk_manifest(self):
        exe = self.root / "app" / "main"
        exe.parent.mkdir()
        (exe.parent / "ryupy-artifact.json").write_text(
            '{"toolchain":{"sdk_version":"10.0.403","target_framework_major":10}}')
        self.assertTrue(save_manifest(exe, self.root / "evidence", "B", "case").is_file())

    def test_space_limit_rejects(self):
        with patch("aot_pair_evidence.shutil.disk_usage") as usage:
            usage.return_value.free = 100
            with self.assertRaises(RuntimeError): require_space(self.root, 101)
            self.assertEqual(require_space(self.root, 100), 100)

    def test_cleanup_only_current_case(self):
        case = self.root / "case"
        case.mkdir()
        other = self.root / "other"
        other.mkdir()
        cleanup_case(case, self.root)
        self.assertFalse(case.exists())
        self.assertTrue(other.exists())

    def test_cleanup_rejects_outside_and_links(self):
        with self.assertRaises(AssertionError): cleanup_case(self.root, self.root)
        link = self.root / "link"
        link.symlink_to(self.root, target_is_directory=True)
        with self.assertRaises(AssertionError): cleanup_case(link, self.root)


if __name__ == "__main__": unittest.main()
