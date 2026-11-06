"""CPython清单解析合同轻测；编译器能力字段不改变Python执行参数或环境。"""
from pathlib import Path
import tempfile
import unittest
from unittest.mock import patch

from run_python_baseline import execute, parse_manifest


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


if __name__ == "__main__":
    unittest.main()
