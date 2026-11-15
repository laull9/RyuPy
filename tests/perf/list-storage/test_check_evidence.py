#!/usr/bin/env python3
"""证据门禁的正负例；全部工件仅写临时目录，不伪称真实 C# 测试结果。"""
import copy
import json
import tempfile
import unittest
import xml.etree.ElementTree as ET
from pathlib import Path

from check_evidence import CONTRACTS, check


class EvidenceTests(unittest.TestCase):
    def setUp(self):
        self.directory = tempfile.TemporaryDirectory()
        self.addCleanup(self.directory.cleanup)
        self.path = Path(self.directory.name)
        self.root = ET.Element("TestRun", xmlns="http://microsoft.com/schemas/VisualStudio/TeamTest/2010")
        results = ET.SubElement(self.root, "Results")
        for family, count in CONTRACTS.items():
            for index in range(count):
                name = "RyuPy.Runtime.Tests.PySegmentedListStorageTests." + family
                if count > 1:
                    name += f"(case: {index})"
                ET.SubElement(results, "UnitTestResult", testId=f"{family}-{index}",
                              testName=name, outcome="Passed")
        summary = ET.SubElement(self.root, "ResultSummary", outcome="Completed")
        ET.SubElement(summary, "Counters", total="40", executed="40", passed="40",
                      failed="0", notExecuted="0", error="0")
        self.data = dict(schema="list-storage-prototype-v3", frequency=1000,
                         framework=".NET 10.0.0", samples=[
                             dict(round=i, mode=mode, stage=stage, ticks=1, allocated_bytes=0)
                             for i in range(10) for mode in ("contiguous", "segmented")
                             for stage in ("append", "slice", "slice_normalized", "extend")])

    def verify(self):
        ET.ElementTree(self.root).write(self.path / "list-storage.trx")
        (self.path / "samples.json").write_text(json.dumps(self.data))
        check(self.path)

    def test_valid(self):
        self.verify()

    def test_duplicate_id(self):
        self.root[0][1].set("testId", self.root[0][0].get("testId"))
        with self.assertRaises(AssertionError): self.verify()

    def test_repeated_record(self):
        first = copy.deepcopy(self.root[0][0])
        self.root[0][:] = [copy.deepcopy(first) for _ in range(40)]
        with self.assertRaises(AssertionError): self.verify()

    def test_missing_family_with_unique_ids(self):
        self.root[0][-1].set("testName", "RyuPy.Runtime.Tests.PySegmentedListStorageTests.Fake")
        with self.assertRaises(AssertionError): self.verify()

    def test_inconsistent_summary(self):
        for field in ("total", "executed", "passed"):
            with self.subTest(field=field):
                self.root[1][0].set(field, "39")
                with self.assertRaises(AssertionError): self.verify()
                self.root[1][0].set(field, "40")

    def test_failed_result(self):
        self.root[0][0].set("outcome", "Failed")
        with self.assertRaises(AssertionError): self.verify()

    def test_zero_tests(self):
        self.root[0][:] = []
        with self.assertRaises(AssertionError): self.verify()

    def test_duplicate_sample(self):
        self.data["samples"][0] = self.data["samples"][1]
        with self.assertRaises(AssertionError): self.verify()

    def test_v2_rejected(self):
        self.data["schema"] = "list-storage-prototype-v2"
        with self.assertRaises(AssertionError): self.verify()

    def test_normalized_stage_missing(self):
        self.data["samples"] = [s for s in self.data["samples"] if s["stage"] != "slice_normalized"]
        with self.assertRaises(AssertionError): self.verify()

    def test_old_26_contracts_rejected(self):
        self.root[0][:] = list(self.root[0])[:26]
        for field in ("total", "executed", "passed"):
            self.root[1][0].set(field, "26")
        with self.assertRaises(AssertionError): self.verify()

    def test_missing_sample(self):
        self.data["samples"].pop()
        with self.assertRaises(AssertionError): self.verify()


if __name__ == "__main__":
    unittest.main()
