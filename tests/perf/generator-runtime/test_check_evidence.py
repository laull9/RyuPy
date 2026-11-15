#!/usr/bin/env python3
"""TRX 门禁正负例；合成测试仅用于门禁，不冒充 C# 验证。"""
import copy
import tempfile
import unittest
import xml.etree.ElementTree as ET
from pathlib import Path
from check_evidence import ALLOWED_SKIPS, CONTRACTS, PREFIX, check


class EvidenceTests(unittest.TestCase):
    def setUp(self):
        self.temp = tempfile.TemporaryDirectory()
        self.addCleanup(self.temp.cleanup)
        self.path = Path(self.temp.name) / "test.trx"
        self.root = ET.Element("TestRun", xmlns="http://microsoft.com/schemas/VisualStudio/TeamTest/2010")
        results = ET.SubElement(self.root, "Results")
        for family, count in CONTRACTS.items():
            for i in range(count):
                name = PREFIX + family + (f"(case: {i})" if count > 1 else "")
                ET.SubElement(results, "UnitTestResult", testId=f"{family}-{i}",
                              testName=name, outcome="Passed")
        summary = ET.SubElement(self.root, "ResultSummary", outcome="Completed")
        ET.SubElement(summary, "Counters", total="16", executed="16", passed="16", failed="0")

    def verify(self, mode="focused"):
        # 模拟 VSTest 的定义表，将结果 testId 映射到实际类/方法身份。
        definitions = self.root.find("TestDefinitions")
        if definitions is not None: self.root.remove(definitions)
        definitions = ET.SubElement(self.root, "TestDefinitions")
        seen = set()
        for result in self.root[0]:
            test_id = result.get("testId")
            if test_id in seen: continue
            seen.add(test_id)
            unit = ET.SubElement(definitions, "UnitTest", id=test_id)
            identity = result.get("testName").split("(", 1)[0]
            cls, method = identity.rsplit(".", 1)
            ET.SubElement(unit, "TestMethod", className=cls, name=method)
        ET.ElementTree(self.root).write(self.path)
        check(self.path, mode)

    def test_valid_focused_and_full(self):
        self.verify()
        ET.SubElement(self.root[0], "UnitTestResult", testId="other", testName="Other.Test", outcome="Passed")
        for field in ("total", "executed", "passed"):
            self.root[1][0].set(field, "17")
        self.verify("full")

    def test_full_rejects_focused_only(self):
        with self.assertRaises(AssertionError): self.verify("full")

    def test_duplicate_id(self):
        self.root[0][1].set("testId", self.root[0][0].get("testId"))
        with self.assertRaises(AssertionError): self.verify()

    def test_repeated_record(self):
        first = copy.deepcopy(self.root[0][0])
        self.root[0][:] = [copy.deepcopy(first) for _ in range(16)]
        with self.assertRaises(AssertionError): self.verify()

    def test_missing_family(self):
        self.root[0][-1].set("testName", PREFIX + "Fake")
        with self.assertRaises(AssertionError): self.verify()

    def test_summary_mismatch(self):
        for field in ("total", "executed", "passed"):
            with self.subTest(field=field):
                self.root[1][0].set(field, "15")
                with self.assertRaises(AssertionError): self.verify()
                self.root[1][0].set(field, "16")

    def test_failed_result(self):
        self.root[0][0].set("outcome", "Failed")
        with self.assertRaises(AssertionError): self.verify()

    def add_skips(self, names):
        for index, name in enumerate(names):
            ET.SubElement(self.root[0], "UnitTestResult", testId=f"skip-{index}",
                          testName=name, outcome="NotExecuted")
        self.root[1][0].set("total", str(16 + len(names)))
        self.root[1][0].set("notExecuted", str(len(names)))

    def test_two_allowed_skips(self):
        self.add_skips(sorted(ALLOWED_SKIPS))
        ET.SubElement(self.root[0], "UnitTestResult", testId="other", testName="Other.Test", outcome="Passed")
        for field, value in (("total", "19"), ("executed", "17"), ("passed", "17")):
            self.root[1][0].set(field, value)
        self.verify("full")

    def test_focused_plus_two_skips_is_not_full(self):
        self.add_skips(sorted(ALLOWED_SKIPS))
        with self.assertRaises(AssertionError): self.verify("full")

    def test_other_skip_rejected(self):
        self.add_skips(["Other.Skipped"])
        with self.assertRaises(AssertionError): self.verify("full")

    def test_extra_skip_rejected(self):
        self.add_skips(sorted(ALLOWED_SKIPS) + ["Other.Skipped"])
        with self.assertRaises(AssertionError): self.verify("full")

    def test_duplicate_allowed_skip_rejected(self):
        name = sorted(ALLOWED_SKIPS)[0]
        self.add_skips([name, name])
        with self.assertRaises(AssertionError): self.verify("full")

    def test_focused_forbids_allowed_skip(self):
        self.add_skips(sorted(ALLOWED_SKIPS))
        with self.assertRaises(AssertionError): self.verify()

    def test_source_identity_mismatch(self):
        self.verify()
        tree = ET.parse(self.path)
        ns = {"t": "http://microsoft.com/schemas/VisualStudio/TeamTest/2010"}
        method = tree.getroot().find("t:TestDefinitions/t:UnitTest/t:TestMethod", ns)
        method.set("name", "WrongMethod")
        tree.write(self.path)
        with self.assertRaises(AssertionError): check(self.path, "focused")

    def test_zero(self):
        self.root[0][:] = []
        with self.assertRaises(AssertionError): self.verify()


if __name__ == "__main__":
    unittest.main()
