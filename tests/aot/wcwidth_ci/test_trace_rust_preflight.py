"""前置采集器的本地纯脚本自测：不启动cargo/Rust、CP oracle或产品。"""
import json
from pathlib import Path
import sys
import tempfile
import unittest
from unittest.mock import Mock, patch

import trace_rust_preflight as gate
import prepare


def output(group):
    module = "trace_domain_tests" if group == "domain" else "trace_lowering_tests"
    names = gate.EXPECTED[group]
    return "\n".join(f"test {module}::{name} ... ok" for name in names) + (
        f"\ntest result: ok. {len(names)} passed; 0 failed; 0 ignored; 0 measured; 99 filtered out; finished in 0.01s\n"
    )


class Preflight(unittest.TestCase):
    def test_capture_preserves_argv_streams_and_nonzero_exit(self):
        with tempfile.TemporaryDirectory() as temporary:
            root = Path(temporary)
            argv = [sys.executable, "-c", "import sys; print('out'); print('err', file=sys.stderr); sys.exit(7)"]
            self.assertEqual(gate.capture(root, "probe", argv, root), 7)
            self.assertEqual((root / "probe.stdout").read_bytes(), b"out\n")
            self.assertEqual((root / "probe.stderr").read_bytes(), b"err\n")
            self.assertEqual((root / "probe.exit").read_text(), "7\n")
            self.assertEqual(json.loads((root / "probe.argv.json").read_text())["argv"], argv)

    def test_capture_missing_tool_is_explicit_failure(self):
        with tempfile.TemporaryDirectory() as temporary:
            root = Path(temporary)
            self.assertEqual(gate.capture(root, "missing", [str(root / "not-a-tool")], root), 127)
            self.assertTrue((root / "missing.stderr").read_bytes())

    def test_exact_names_and_counts_are_required(self):
        for group in gate.EXPECTED:
            good = output(group)
            self.assertTrue(gate.tests_complete(good, group))
            self.assertFalse(gate.tests_complete("test result: ok. 0 passed; 0 failed; 0 ignored; 0 measured; 99 filtered out;", group))
            self.assertFalse(gate.tests_complete(good.replace(" ... ok", " ... ignored", 1), group))
            self.assertFalse(gate.tests_complete(good.replace("test trace_", "test fake_trace_", 1), group))
            self.assertFalse(gate.tests_complete(good + good, group))

    def test_aggregation_never_hides_fmt_compile_or_skipped_failure(self):
        labels = ("cargo-version", "rustc-version", "rustfmt-version", "git-head", "git-tree", "git-status", "git-head-after", "git-tree-after", "git-status-after", "fmt", "check", "domain", "lowering")
        codes = dict.fromkeys(labels, 0)
        outputs = {group: output(group) for group in gate.EXPECTED}
        self.assertTrue(gate.aggregate(codes, outputs)["success"])
        for label in labels:
            with self.subTest(label=label):
                failed = dict(codes, **{label: None if label in gate.EXPECTED else 1})
                self.assertFalse(gate.aggregate(failed, outputs)["success"])
        self.assertFalse(gate.aggregate(codes, dict(outputs, domain=""))["success"])
        self.assertFalse(gate.aggregate(codes, dict(outputs, **{"git-status": " M file.rs\n"}))["success"])

    def test_fmt_failure_still_compiles_and_runs_both_groups(self):
        for check_exit in (0, 1):
            with self.subTest(check_exit=check_exit), tempfile.TemporaryDirectory() as temporary:
                directory = Path(temporary) / "evidence"
                called = []

                def fake_capture(out, label, argv, cwd, timeout=900):
                    called.append(label)
                    text = output(label) if label in gate.EXPECTED else ""
                    if label in ("git-head", "git-head-after"):
                        text = "fixed-head\n"
                    (out / f"{label}.stdout").write_text(text)
                    return 1 if label == "fmt" else check_exit if label == "check" else 0

                with patch.object(gate, "verify_tools"), patch.object(gate, "OUTPUT", directory), patch.object(gate, "capture", fake_capture), patch.dict(gate.os.environ, {"GITHUB_SHA": "fixed-head"}), patch("builtins.print"):
                    self.assertEqual(gate.main(), 1)
                self.assertLess(called.index("fmt"), called.index("check"))
                self.assertEqual("domain" in called, check_exit == 0)
                self.assertEqual("lowering" in called, check_exit == 0)
                report = json.loads((directory / "result.json").read_text())
                self.assertIn("fmt", report["failures"])
                if check_exit:
                    self.assertTrue((directory / "domain.skipped.json").exists())

    def test_timeout_kills_and_reaps_the_whole_process_group(self):
        with tempfile.TemporaryDirectory() as temporary:
            root = Path(temporary)
            process = Mock(pid=12345)
            process.wait.side_effect = [gate.subprocess.TimeoutExpired(["fake"], 1), -9]
            with patch.object(gate.subprocess, "Popen", return_value=process) as spawn, patch.object(gate.os, "killpg") as kill:
                self.assertEqual(gate.capture(root, "timeout", ["fake"], root, timeout=1), 124)
            self.assertTrue(spawn.call_args.kwargs["start_new_session"])
            kill.assert_called_once_with(12345, gate.signal.SIGKILL)
            self.assertEqual(process.wait.call_count, 2)
            self.assertEqual((root / "timeout.exit").read_text(), "124\n")

    def test_download_selection_preserves_default_and_pins_only_rust(self):
        original = json.loads((prepare.HERE / "downloads.json").read_text())
        self.assertEqual(prepare.selected_downloads(), original)
        selected = prepare.selected_downloads(True)
        self.assertEqual(selected, [item for item in original if item["kind"] == "rust"])
        self.assertEqual(len(selected), 5)
        self.assertTrue(all(item["version"] == "1.99.1" and item["algorithm"] == "sha256" for item in selected))

    def test_system_rust_fallback_is_rejected_without_execution(self):
        with tempfile.TemporaryDirectory() as temporary:
            root = Path(temporary)
            with patch.dict(gate.os.environ, {"TRACE_RUST_ROOT": str(root)}), patch.object(gate.shutil, "which", return_value="/usr/bin/cargo"):
                with self.assertRaises(RuntimeError):
                    gate.verify_tools(root)

    def test_changed_git_identity_is_rejected(self):
        labels = ("cargo-version", "rustc-version", "rustfmt-version", "git-head", "git-tree", "git-status", "git-head-after", "git-tree-after", "git-status-after", "fmt", "check", "domain", "lowering")
        codes = dict.fromkeys(labels, 0)
        outputs = {group: output(group) for group in gate.EXPECTED}
        outputs.update({"git-head": "old", "git-head-after": "new"})
        self.assertFalse(gate.aggregate(codes, outputs)["success"])


if __name__ == "__main__":
    unittest.main()
