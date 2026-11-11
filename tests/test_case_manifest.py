"""CPython清单解析合同轻测；编译器能力字段不改变Python执行参数或环境。"""
from pathlib import Path
import tempfile
import sys
import subprocess
import unittest
from unittest.mock import patch

from run_python_baseline import (execute, parse_manifest, parse_single_expectation,
                                 check_single, check_single_result)


class CaseManifestTests(unittest.TestCase):
    def parse(self, text):
        with tempfile.TemporaryDirectory(prefix="ryupy-case-manifest-") as directory:
            path = Path(directory) / "case.txt"
            path.write_text(text, encoding="utf-8")
            return parse_manifest(path)

    def test_default_has_no_instance_selection(self):
        self.assertEqual(self.parse("entry = entry.py\n")["instance_modules"], [])

    def test_instance_modules_are_repeatable_and_preserve_order(self):
        spec = self.parse(
            "instance_module = a\ninstance_module = pkg.child\ninstance_module = a\n"
            "args = first 'two words'\nenv = NAME=value\n"
        )
        self.assertEqual(spec["instance_modules"], ["a", "pkg.child", "a"])
        self.assertEqual(spec["args"], ["first", "two words"])
        self.assertEqual(spec["env"], {"NAME": "value"})

    def test_empty_instance_module_is_rejected(self):
        for line in ["instance_module =", "instance_module =   ", "instance_module"]:
            with self.subTest(line=line), self.assertRaisesRegex(AssertionError, "不能为空"):
                self.parse(line)

    def test_unknown_keys_are_still_rejected(self):
        with self.assertRaisesRegex(AssertionError, "未知键"):
            self.parse("instance_modules = a\n")

    def test_compiler_selection_is_not_forwarded_to_python(self):
        spec = self.parse("instance_module = pkg.child\nargs = hello\n")
        with patch("run_python_baseline.subprocess.run") as run:
            execute("python-test", spec["entry"], spec)
        self.assertEqual(run.call_args.args[0], ["python-test", "-B", str(spec["entry"]), "hello"])
        self.assertNotIn("instance_modules", run.call_args.kwargs["env"])
        self.assertNotIn("instance_module", run.call_args.kwargs["env"])


class SingleSidecarTests(unittest.TestCase):
    valid = "expect_exit = 1\nexpect_exception = ValueError: concrete failure\n"

    def test_parser_fail_closed(self):
        with tempfile.TemporaryDirectory() as directory:
            source = Path(directory) / "sample.py"
            sidecar = source.with_suffix(".case.txt")
            self.assertIsNone(parse_single_expectation(source))
            sidecar.write_text(self.valid)
            self.assertEqual(parse_single_expectation(source), ("ValueError", "concrete failure"))
            bad = ["", "expect_exit=1", "expect_exception=ValueError: message",
                   "expect_exit=1\nexpect_exception=ValueError:",
                   "expect_exit=1\nexpect_exception=9Error: message",
                   "expect_exit=1\vexpect_exception=ValueError: message",
                   "expect_exit=1\rexpect_exception=ValueError: message"]
            bad += [self.valid.replace("= 1", "= " + code) for code in ["0", "2", "-9", "nonzero", "01"]]
            bad += [self.valid + extra for extra in ["expect_exit=1", "expect_exception=ValueError: other",
                                                    "pending=true", "timeout=30", "broken"]]
            for text in bad:
                sidecar.write_text(text)
                with self.subTest(text=text), self.assertRaises(AssertionError):
                    parse_single_expectation(source)

    def test_exit_and_diagnostic_checks(self):
        expected = ("ValueError", "concrete failure")
        for code in [0, 2, -9, 139]:
            result = subprocess.CompletedProcess([], code, "", "ValueError: concrete failure\n")
            with self.subTest(code=code), self.assertRaises(AssertionError):
                check_single_result(result, expected)
        for stderr in ["", "TypeError: concrete failure", "ValueError: wrong", "  ValueError: concrete failure", "ValueError: concrete failure\ncrash"]:
            with self.subTest(stderr=stderr), self.assertRaises(AssertionError):
                check_single_result(subprocess.CompletedProcess([], 1, "", stderr), expected)

    def test_actual_synthetic_processes(self):
        # 仅宿主合成脚本；不是固定CP oracle，也不执行任何业务fixture。
        with tempfile.TemporaryDirectory() as directory:
            source = Path(directory) / "sample.py"
            sidecar = source.with_suffix(".case.txt")
            source.write_text("print('ordinary success')\n")
            check_single(sys.executable, source)
            sidecar.write_text(self.valid)
            for code in ["pass", "raise SystemExit(2)", "import os, signal; os.kill(os.getpid(), signal.SIGTERM)"]:
                source.write_text(code + "\n")
                with self.subTest(code=code), self.assertRaises(AssertionError):
                    check_single(sys.executable, source)
            source.write_text("raise ValueError('concrete failure')\n")
            check_single(sys.executable, source)
            sidecar.unlink()
            with self.assertRaises(AssertionError):
                check_single(sys.executable, source)

    def test_timeout_is_not_expected_failure(self):
        with tempfile.TemporaryDirectory() as directory:
            source = Path(directory) / "sample.py"
            source.with_suffix(".case.txt").write_text(self.valid)
            with patch("run_python_baseline.execute", side_effect=subprocess.TimeoutExpired("synthetic", 30)):
                with self.assertRaises(subprocess.TimeoutExpired):
                    check_single(sys.executable, source)


if __name__ == "__main__":
    unittest.main()
