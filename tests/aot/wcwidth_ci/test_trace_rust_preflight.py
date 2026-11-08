"""前置采集器的本地纯脚本自测：不启动cargo/Rust、CP oracle或产品。"""
import json
from pathlib import Path
import sys
import tempfile
import unittest
from unittest.mock import Mock, patch

import trace_rust_preflight as gate
import prepare


def universe(group):
    if group not in gate.MIR_MODULES:
        return gate.expected_names(group)
    return set().union(*(gate.expected_names(name) for name in gate.MIR_MODULES), {"other::test"})


def listing():
    names = universe("domain")
    return "\n".join(f"{name}: test" for name in sorted(names)) + f"\n{len(names)} tests, 0 benchmarks\n"


def output(group):
    names = gate.expected_names(group)
    isolated = gate.isolated_names(group)
    filtered = len(universe(group)) - len(names)
    return f"running {len(names)} tests\n" + "\n".join(f"test {name} ... " + ("ignored, known experimental failure" if name in isolated else "ok") for name in sorted(names)) + (
        f"\ntest result: ok. {len(names - isolated)} passed; 0 failed; {len(isolated)} ignored; 0 measured; {filtered} filtered out; finished in 0.01s\n"
    )


def outputs():
    return {"mir-list": listing(), **{group: output(group) for group in gate.EXPECTED}}


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
            self.assertTrue(gate.tests_complete(good, group, universe(group)))
            self.assertFalse(gate.tests_complete("test result: ok. 0 passed; 0 failed; 0 ignored; 0 measured; 99 filtered out;", group, universe(group)))
            self.assertFalse(gate.tests_complete(good.replace(" ... ok", " ... ignored", 1), group, universe(group)))
            self.assertFalse(gate.tests_complete(good.replace(sorted(gate.expected_names(group))[0], "fake_name", 1), group, universe(group)))
            self.assertFalse(gate.tests_complete(good + good, group, universe(group)))

    def test_aggregation_never_hides_fmt_compile_or_skipped_failure(self):
        labels = gate.REQUIRED
        codes = dict.fromkeys(labels, 0)
        evidence = outputs()
        self.assertTrue(gate.aggregate(codes, evidence)["success"])
        for label in labels:
            with self.subTest(label=label):
                failed = dict(codes, **{label: None if label in gate.EXPECTED else 1})
                self.assertFalse(gate.aggregate(failed, evidence)["success"])
        self.assertFalse(gate.aggregate(codes, dict(evidence, domain=""))["success"])
        self.assertFalse(gate.aggregate(codes, dict(evidence, **{"git-status": " M file.rs\n"}))["success"])

    def test_fmt_failure_still_compiles_and_runs_both_groups(self):
        for check_exit in (0, 1):
            with self.subTest(check_exit=check_exit), tempfile.TemporaryDirectory() as temporary:
                directory = Path(temporary) / "evidence"
                called = []

                def fake_capture(out, label, argv, cwd, timeout=900):
                    called.append(label)
                    text = output(label) if label in gate.EXPECTED else listing() if label == "mir-list" else ""
                    if label in ("git-head", "git-head-after"):
                        text = "fixed-head\n"
                    (out / f"{label}.stdout").write_text(text)
                    return 1 if label == "fmt" else check_exit if label == "check" else 0

                with patch.object(gate, "verify_tools"), patch.object(gate, "OUTPUT", directory), patch.object(gate, "capture", fake_capture), patch.dict(gate.os.environ, {"GITHUB_SHA": "fixed-head"}), patch("builtins.print"):
                    self.assertEqual(gate.main(), 1)
                self.assertLess(called.index("fmt"), called.index("check"))
                self.assertEqual("domain" in called, check_exit == 0)
                self.assertEqual("lowering" in called, check_exit == 0)
                self.assertEqual("frame" in called, check_exit == 0)
                self.assertIn("guard-default", called)
                self.assertIn("guard-pilot", called)
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
        labels = gate.REQUIRED
        codes = dict.fromkeys(labels, 0)
        evidence = outputs()
        evidence.update({"git-head": "old", "git-head-after": "new"})
        self.assertFalse(gate.aggregate(codes, evidence)["success"])

    def test_lists_filtered_and_running_counts_are_strict(self):
        self.assertEqual(gate.listed_tests(listing()), universe("domain"))
        self.assertIsNone(gate.listed_tests(listing() + listing()))
        self.assertIsNone(gate.listed_tests(listing().replace("0 benchmarks", "1 benchmarks")))
        for group in gate.EXPECTED:
            good = output(group)
            self.assertFalse(gate.tests_complete(gate.re.sub(r"\d+ filtered out", "999999 filtered out", good), group, universe(group)))
            self.assertFalse(gate.tests_complete(good.replace("running ", "running 0", 1), group, universe(group)))
            self.assertFalse(gate.tests_complete(good, group, None))

    def test_frame_names_match_all_actual_definitions(self):
        import re
        source = (gate.ROOT / "crates/ryupy-mir/src/trace_frame_tests.rs").read_text()
        names = re.findall(r"#\[test\]\s*(?:#\[ignore[^\n]*\]\s*)?fn (\w+)\(", source)
        self.assertEqual(len(names), 12)
        self.assertEqual(set(names), set(gate.EXPECTED["frame"]))
        guard = (gate.ROOT / "crates/ryupy-driver/tests/dynamic_except_structure.rs").read_text()
        self.assertEqual(re.findall(r"#\[test\]\s*fn (\w+)\(", guard), gate.EXPECTED["guard-default"])

    def test_dataflow_route_and_closed_observation_are_explicit(self):
        source = (gate.ROOT / "crates/ryupy-mir/src/trace_frame_tests.rs").read_text()
        self.assertNotIn("trace_frame::inspect", source)
        self.assertNotIn("observer::start", source)
        self.assertNotIn("observer::finish", source)
        self.assertIn("trace_frame_dataflow::inspect", source)
        new = (gate.ROOT / "crates/ryupy-mir/src/trace_frame_dataflow_tests.rs").read_text()
        names = gate.re.findall(r"#\[test\]\s*(?:#\[ignore[^\n]*\]\s*)?fn (\w+)\(", new)
        self.assertEqual(len(names), 7)
        self.assertEqual(set(names), set(gate.EXPECTED["dataflow"]))
        collector = Path(gate.__file__).read_text()
        self.assertNotIn("trace_frame_observation.collect(", collector)
        self.assertNotIn('os.environ["RYUPY_FRAME_OBSERVATION"] =', collector)
        self.assertIn('"not-requested"', collector)

    def test_unexpected_observation_request_is_not_swallowed(self):
        with tempfile.TemporaryDirectory() as temporary:
            directory = Path(temporary) / "evidence"
            def fake_capture(out, label, argv, cwd, timeout=900):
                text = output(label) if label in gate.EXPECTED else listing() if label == "mir-list" else ""
                if label in ("git-head", "git-head-after"):
                    text = "fixed-head\n"
                (out / f"{label}.stdout").write_text(text)
                return 0
            with patch.object(gate, "verify_tools"), patch.object(gate, "OUTPUT", directory), patch.object(gate, "capture", fake_capture), patch.dict(gate.os.environ, {"GITHUB_SHA": "fixed-head", "RYUPY_FRAME_OBSERVATION": "unexpected"}), patch("builtins.print"):
                self.assertEqual(gate.main(), 1)
            status = json.loads((directory / "frame-observation-result.json").read_text())
            self.assertEqual(status["status"], "unexpected-request")
            self.assertNotIn("complete", status)
            report = json.loads((directory / "result.json").read_text())
            self.assertTrue(any("unexpected request" in value for value in report["failures"]))

    def test_driver_modes_are_independent_locked_exact_processes(self):
        with tempfile.TemporaryDirectory() as temporary:
            directory = Path(temporary) / "evidence"
            calls = {}

            def fake_capture(out, label, argv, cwd, timeout=900):
                calls[label] = argv
                text = output(label) if label in gate.EXPECTED else listing() if label == "mir-list" else ""
                if label in ("git-head", "git-head-after"):
                    text = "fixed-head\n"
                (out / f"{label}.stdout").write_text(text)
                return 1 if label == "check-guard-default" else 0

            with patch.object(gate, "verify_tools"), patch.object(gate, "OUTPUT", directory), patch.object(gate, "capture", fake_capture), patch.dict(gate.os.environ, {"GITHUB_SHA": "fixed-head"}), patch("builtins.print"):
                self.assertEqual(gate.main(), 1)
            observation = json.loads((directory / "frame-observation-result.json").read_text())
            self.assertEqual(observation["status"], "not-requested")
            self.assertNotIn("complete", observation)
            self.assertNotIn("guard-default", calls)
            self.assertIn("guard-pilot", calls)
            self.assertIn("--locked", calls["check-guard-default"])
            self.assertNotIn("--features", calls["check-guard-default"])
            for label in ("check-guard-pilot", "guard-pilot"):
                self.assertIn("ryupy-driver/ordinary-dynamic-module-pilot", calls[label])
                self.assertIn("--no-default-features", calls[label])
                self.assertIn("--locked", calls[label])
            self.assertIn("--exact", calls["guard-pilot"])
            self.assertIn("--show-output", calls["frame"])

    def test_only_four_named_experiments_can_be_isolated(self):
        evidence = outputs()
        result = gate.aggregate(dict.fromkeys(gate.REQUIRED, 0), evidence)
        self.assertTrue(result["success"])
        self.assertEqual(result["isolated_experiments"]["count"], 4)
        self.assertEqual(result["isolated_experiments"]["status"], "not-executed-known-failed")
        for group in gate.EXPERIMENTAL:
            good = output(group)
            self.assertFalse(gate.tests_complete(good.replace("ignored, known experimental failure", "ok", 1), group, universe(group)))

    def test_all_four_product_groups_match_current_source_names_without_ignores(self):
        files = {
            "product": ("generator_product_tests.rs", 7),
            "product-structure": ("generator_product_structure_tests.rs", 4),
            "product-edge-witness": ("generator_product_edge_witness_tests.rs", 7),
            "product-edges": ("generator_product_edges_tests.rs", 6),
        }
        self.assertEqual(set(files), set(gate.PRODUCT_GROUPS))
        self.assertEqual(sum(count for _, count in files.values()), 24)
        for group, (filename, count) in files.items():
            source = (gate.ROOT / "crates/ryupy-mir/src" / filename).read_text()
            names = gate.re.findall(r"#\[test\]\s*fn (\w+)\(", source)
            self.assertEqual(len(names), count)
            self.assertEqual(set(names), set(gate.EXPECTED[group]))
            self.assertNotIn("#[ignore", source)
            self.assertFalse(gate.isolated_names(group))
            self.assertIn(group, gate.REQUIRED)

    def test_actual_list_must_select_exact_product_names_before_running(self):
        for group in gate.PRODUCT_GROUPS:
            available = universe(group)
            pattern = gate.MIR_MODULES[group] + "::"
            self.assertEqual(gate.product_filter(group, available), pattern)
            self.assertIsNone(gate.product_filter(group, None))
            self.assertIsNone(gate.product_filter(group, available - {next(iter(gate.expected_names(group)))}))
            self.assertIsNone(gate.product_filter(group, available | {pattern + "unexpected"}))
            # cargo筛选是substring；嵌套同名模块也必须在运行前拒绝。
            self.assertIsNone(gate.product_filter(group, available | {"other::" + pattern + "unexpected"}))
            good = output(group)
            self.assertFalse(gate.tests_complete(good.replace(" ... ok", " ... ignored", 1), group, available))

    def test_product_groups_use_fixed_filters_and_missing_list_never_runs_group(self):
        for bad_list in (False, True):
            with self.subTest(bad_list=bad_list), tempfile.TemporaryDirectory() as temporary:
                directory = Path(temporary) / "evidence"
                calls = {}
                def fake_capture(out, label, argv, cwd, timeout=900):
                    calls[label] = argv
                    text = output(label) if label in gate.EXPECTED else listing() if label == "mir-list" else ""
                    if label == "mir-list" and bad_list:
                        names = universe("domain") - {next(iter(gate.expected_names("product-edges")))}
                        text = "\n".join(f"{name}: test" for name in sorted(names)) + f"\n{len(names)} tests, 0 benchmarks\n"
                    if label in ("git-head", "git-head-after"):
                        text = "fixed-head\n"
                    (out / f"{label}.stdout").write_text(text)
                    return 0
                with patch.object(gate, "verify_tools"), patch.object(gate, "OUTPUT", directory), patch.object(gate, "capture", fake_capture), patch.dict(gate.os.environ, {"GITHUB_SHA": "fixed-head"}), patch("builtins.print"):
                    self.assertEqual(gate.main(), int(bad_list))
                for group in gate.PRODUCT_GROUPS:
                    if bad_list and group == "product-edges":
                        self.assertNotIn(group, calls)
                        self.assertTrue((directory / (group + ".skipped.json")).exists())
                    else:
                        self.assertIn(gate.MIR_MODULES[group] + "::", calls[group])
                        self.assertNotIn("--ignored", calls[group])
                self.assertIn("git-head-after", calls)

    def test_total_budget_skips_work_but_keeps_final_identity(self):
        with tempfile.TemporaryDirectory() as temporary:
            directory = Path(temporary) / "evidence"
            called = []

            def fake_capture(out, label, argv, cwd, timeout=900):
                called.append(label)
                (out / f"{label}.stdout").write_text("")
                return 0

            clock = iter([0] + [gate.WORK_BUDGET_SECONDS + 1] * 100)
            with patch.object(gate, "verify_tools"), patch.object(gate, "OUTPUT", directory), patch.object(gate, "capture", fake_capture), patch.object(gate.time, "monotonic", side_effect=lambda: next(clock)), patch("builtins.print"):
                self.assertEqual(gate.main(), 1)
            self.assertEqual(called, ["git-head-after", "git-tree-after", "git-status-after"])
            self.assertTrue((directory / "check.skipped.json").exists())
            self.assertTrue((directory / "guard-pilot.skipped.json").exists())


if __name__ == "__main__":
    unittest.main()
