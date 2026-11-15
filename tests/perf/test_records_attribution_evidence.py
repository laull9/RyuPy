"""归因证据门的合成正负例；不替代真实SDK测试，禁止以重复/旧/错误阶段数据冒充通过。"""
import copy
import json
from pathlib import Path
import subprocess
import sys
import tempfile
import unittest
from unittest.mock import patch
import xml.etree.ElementTree as ET

from check_records_attribution import (CASES, CLASS, NS, SAFETY, SCENARIOS, STAGES,
                                      expected_sums, identity, verify_all, verify_case)

TOKEN = "a" * 32
SHA = "b" * 40
ZERO_COUNTERS = ("failed", "error", "timeout", "aborted", "inconclusive", "passedButRunAborted",
                 "notRunnable", "notExecuted", "disconnected", "warning", "completed", "inProgress", "pending")


def tag(name):
    return "{" + NS["t"] + "}" + name


def fixture(folder, case="SmallTable", tier="default", reverse="0"):
    name = identity(case, tier, reverse)
    if case == SAFETY:
        lines = [f"attribution-safety token={TOKEN}"]
    else:
        size, collision = SCENARIOS[case]
        lines = [f"attribution-config keys={size} collision={collision} reverse={reverse == '1'} tiering={'0' if tier == 'off' else 'default'} framework=.NET 10.0.15 cpu=2 serverGC=False token={TOKEN}"]
        stages = list(STAGES) if reverse == "0" else list(reversed(STAGES))
        sums = expected_sums(size, collision)
        for round_no in range(6):
            for stage in stages if round_no % 2 == 0 else stages[::-1]:
                lines.append(f"attribution keys={size} collision={collision} round={round_no} stage={stage} ms=1.000 bytes=1024 gen0=0 gen1=0 gen2=0 pauseMs=0.000 sum={sums[stage]}")
    root = ET.Element(tag("TestRun"))
    results = ET.SubElement(root, tag("Results"))
    result = ET.SubElement(results, tag("UnitTestResult"), testName=f"{CLASS}.{case}", outcome="Passed", testId="test-id", executionId="execution-id")
    out = ET.SubElement(result, tag("Output")); ET.SubElement(out, tag("StdOut")).text = "\n".join(lines)
    definitions = ET.SubElement(root, tag("TestDefinitions"))
    definition = ET.SubElement(definitions, tag("UnitTest"), id="test-id")
    ET.SubElement(definition, tag("Execution"), id="execution-id")
    ET.SubElement(definition, tag("TestMethod"), className=CLASS, name=case)
    summary = ET.SubElement(root, tag("ResultSummary"), outcome="Completed")
    ET.SubElement(summary, tag("Counters"), **{**{key: "0" for key in ZERO_COUNTERS}, "total": "1", "executed": "1", "passed": "1"})
    write(folder, name, root)
    return root


def write(folder, name, root, sync_log=True):
    ET.ElementTree(root).write(folder / f"{name}.trx", encoding="utf-8", xml_declaration=True)
    if sync_log:
        (folder / f"{name}.log").write_text(root.find(".//t:StdOut", NS).text)


class EvidenceTests(unittest.TestCase):
    def setUp(self):
        self.temp = tempfile.TemporaryDirectory(); self.addCleanup(self.temp.cleanup)
        self.folder = Path(self.temp.name); self.name = identity("SmallTable", "default", "0")

    def rejects(self, mutate, sync_log=True):
        root = fixture(self.folder); mutate(root); write(self.folder, self.name, root, sync_log)
        with self.assertRaises((ValueError, ET.ParseError)):
            verify_case(self.folder, "SmallTable", "default", "0", TOKEN)

    def test_all_configs_and_complete_summary(self):
        for case in CASES:
            fixture(self.folder, *case); verify_case(self.folder, *case, TOKEN)
        manifest = {"commit": SHA, "sdk": "10.0.403", "token": TOKEN}
        (self.folder / "manifest.json").write_text(json.dumps(manifest))
        summary = {**manifest, "runs": [{"case": identity(*case), "exit": 0, "passed": True} for case in CASES]}
        (self.folder / "summary.json").write_text(json.dumps(summary)); verify_all(self.folder, SHA)
        for field, bad in (("commit", "c" * 40), ("token", "d" * 32), ("sdk", "10.0.404"), ("runs", summary["runs"][:-1]), ("runs", summary["runs"] + summary["runs"][:1])):
            with self.subTest(field=field, bad=str(bad)[:40]):
                broken = {**summary, field: bad}; (self.folder / "summary.json").write_text(json.dumps(broken))
                with self.assertRaises(ValueError): verify_all(self.folder, SHA)

    def test_rejects_old_72_line_false_positive(self):
        self.rejects(lambda root: setattr(root.find(".//t:StdOut", NS), "text",
            "\n".join(["attribution keys=999 collision=False round=0 stage=invalid ms=nan bytes=0"] * 72)))

    def test_duplicate_missing_unknown_reordered_round_or_stage(self):
        def change(root, mode):
            node = root.find(".//t:StdOut", NS); lines = node.text.splitlines()
            if mode == "duplicate": lines[2] = lines[1]
            elif mode == "missing": lines.pop()
            elif mode == "extra": lines.append(lines[-1])
            elif mode == "unknown": lines[1] = lines[1].replace("stage=guard", "stage=invalid")
            elif mode == "round": lines[1] = lines[1].replace("round=0", "round=6")
            else: lines[1], lines[2] = lines[2], lines[1]
            node.text = "\n".join(lines)
        for mode in ("duplicate", "missing", "extra", "unknown", "round", "reorder"):
            with self.subTest(mode=mode): self.rejects(lambda root: change(root, mode))

    def test_config_numeric_oracle_and_log_mismatch(self):
        for old, new in (("keys=32", "keys=4096"), ("collision=False", "collision=True"),
                         ("reverse=False", "reverse=True"), ("tiering=default", "tiering=0"),
                         (TOKEN, "e" * 32), ("ms=1.000", "ms=nan"), ("ms=1.000", "ms=inf"),
                         ("ms=1.000", "ms=-1.0"), ("pauseMs=0.000", "pauseMs=nan"),
                         ("bytes=1024", "bytes=-1"), ("gen0=0", "gen0=-1"),
                         ("sum=120000", "sum=0")):
            with self.subTest(new=new):
                self.rejects(lambda root: setattr(root.find(".//t:StdOut", NS), "text", root.find(".//t:StdOut", NS).text.replace(old, new)))
        self.rejects(lambda root: setattr(root.find(".//t:StdOut", NS), "text", root.find(".//t:StdOut", NS).text.replace("bytes=1024", "bytes=2048")), False)

    def test_exact_result_definition_execution_and_all_counters(self):
        for path, attr, bad in ((".//t:UnitTestResult", "testName", "Wrong"), (".//t:UnitTestResult", "outcome", "NotExecuted"),
                                (".//t:UnitTestResult", "testId", "other"), (".//t:Execution", "id", "old"),
                                (".//t:TestMethod", "className", "Wrong"), (".//t:TestMethod", "name", "Other"),
                                ("t:ResultSummary", "outcome", "Failed")):
            with self.subTest(attr=attr): self.rejects(lambda root: root.find(path, NS).set(attr, bad))
        for key in ZERO_COUNTERS + ("total", "executed", "passed"):
            with self.subTest(counter=key): self.rejects(lambda root: root.find(".//t:Counters", NS).set(key, "9"))
        self.rejects(lambda root: root.find("t:Results", NS).append(copy.deepcopy(root.find(".//t:UnitTestResult", NS))))
        self.rejects(lambda root: root.append(copy.deepcopy(root.find("t:ResultSummary", NS))))

    def test_runner_rejects_even_empty_existing_directory_without_dotnet(self):
        script = Path(__file__).with_name("records_probe_attribution.py")
        result = subprocess.run([sys.executable, "-B", str(script), "--output", str(self.folder),
                                 "--expected-sha", SHA, "--dotnet", "does-not-exist"], capture_output=True, text=True)
        self.assertNotEqual(result.returncode, 0); self.assertIn("拒绝复用", result.stderr)

    def test_timeout_or_missing_trx_writes_failed_current_summary(self):
        import records_probe_attribution as runner
        for mode in ("timeout", "missing-trx"):
            with self.subTest(mode=mode):
                destination = self.folder / mode
                fake = subprocess.CompletedProcess([], 0)
                with patch.object(sys, "argv", ["runner", "--output", str(destination), "--expected-sha", SHA]), \
                     patch.object(runner.subprocess, "check_output", side_effect=["10.0.403\n", SHA + "\n"]), \
                     patch.object(runner.subprocess, "run", side_effect=subprocess.TimeoutExpired([], 600) if mode == "timeout" else None, return_value=fake):
                    with self.assertRaises((subprocess.TimeoutExpired, FileNotFoundError)):
                        runner.main()
                summary = json.loads((destination / "summary.json").read_text())
                self.assertEqual(summary["commit"], SHA)
                self.assertEqual(len(summary["runs"]), 1)
                self.assertIs(summary["runs"][0]["passed"], False)
                self.assertEqual(summary["runs"][0]["case"], identity(*CASES[0]))
                self.assertTrue((destination / "failure.txt").exists())


if __name__ == "__main__":
    unittest.main()
