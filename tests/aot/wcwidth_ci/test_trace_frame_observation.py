"""观察artifact的纯脚本自测；不运行Rust或产品。"""
import json
from pathlib import Path
import tempfile
import unittest

import trace_frame_observation as observation


class ObservationArtifacts(unittest.TestCase):
    def make(self, root, status="complete"):
        state = dict(pc=0, block=1, cursor=2, locals=["None"], slots=["StoredException"],
                     physical_catch=False, dispatching=True, pending="AwaitingRead",
                     expected_retry_pc=None, expected_return_pc=3, written=[True],
                     stored_current=[False], current_sources=[[0]])
        (root / "states.jsonl").write_text(json.dumps({"id": 0, "state": state}) + "\n")
        (root / "events.jsonl").write_text(json.dumps({"dequeue": 1, "id": 0, "inserted": True}) + "\n")
        (root / "ir.txt").write_text("stage=audit-build-output-not-linked\norigin=GeneratorOrigin {}\nlayout=GeneratorLayout { pc_targets: [], catch_reads: [], saved_locals: [] }\nprogram=Program { functions: [] }\n")
        meta = dict(schema=1, limits=observation.LIMITS, observer_status=status, ir_status="complete",
                    attempted=1, retained_states=1, retained_events=1, analysis_result="budget error",
                    analysis_diagnostics=dict(processed=1, unique_states=1, duplicate_states=0,
                                              value_cells=9, states_by_pc={"0": 1}, self_edges=0))
        (root / "metadata.json").write_text(json.dumps(meta))

    def test_complete_and_truncated_are_distinct_and_parseable(self):
        with tempfile.TemporaryDirectory() as temporary:
            root = Path(temporary)
            self.make(root)
            result = observation.verify(root)
            self.assertTrue(result["complete"])
            self.assertEqual(result["states_by_actual_position"], {"0/1/2": 1})
            self.make(root, "byte_limit")
            self.assertFalse(observation.verify(root)["complete"])

    def test_missing_or_bad_records_fail_without_replacing_analysis(self):
        with tempfile.TemporaryDirectory() as temporary:
            root = Path(temporary)
            self.make(root)
            (root / "events.jsonl").write_text('{"dequeue":1,"id":7,"inserted":true}\n')
            with self.assertRaises(ValueError):
                observation.verify(root)
            self.assertEqual(json.loads((root / "metadata.json").read_text())["analysis_result"], "budget error")
            output = root / "upload"
            output.mkdir()
            result = observation.collect(root, output, "head", "tree")
            self.assertFalse(result["complete"])
            self.assertIn("error", result)
            self.assertTrue((output / "frame-observation-result.json").exists())

    def test_exact_byte_limit_and_plus_one(self):
        with tempfile.TemporaryDirectory() as temporary:
            path = Path(temporary) / "bytes"
            path.write_bytes("é".encode())
            self.assertEqual(observation.read_limited(path, 2), "é".encode())
            with self.assertRaises(ValueError):
                observation.read_limited(path, 1)
            path.write_bytes(b"")
            self.assertEqual(observation.read_limited(path, 0), b"")

    def test_empty_complete_and_empty_ir_are_rejected(self):
        with tempfile.TemporaryDirectory() as temporary:
            root = Path(temporary)
            self.make(root)
            (root / "ir.txt").write_text("")
            with self.assertRaisesRegex(ValueError, "static IR"):
                observation.verify(root)
            self.make(root)
            for name in ("states.jsonl", "events.jsonl", "ir.txt"):
                (root / name).write_text("")
            path = root / "metadata.json"
            meta = json.loads(path.read_text())
            meta.update(attempted=0, retained_states=0, retained_events=0)
            path.write_text(json.dumps(meta))
            with self.assertRaisesRegex(ValueError, "independent analysis or states"):
                observation.verify(root)

    def test_missing_event_or_wrong_independent_counts_are_rejected(self):
        with tempfile.TemporaryDirectory() as temporary:
            root = Path(temporary)
            for field, value in (("processed", 2), ("unique_states", 2), ("duplicate_states", 1),
                                 ("value_cells", 10), ("states_by_pc", {"3": 1})):
                self.make(root)
                path = root / "metadata.json"
                meta = json.loads(path.read_text())
                meta["analysis_diagnostics"][field] = value
                path.write_text(json.dumps(meta))
                with self.assertRaisesRegex(ValueError, "analysis/record mismatch"):
                    observation.verify(root)
            self.make(root)
            path = root / "metadata.json"
            meta = json.loads(path.read_text())
            meta["analysis_diagnostics"].update(processed=2, duplicate_states=1)
            # 模拟漏最后重复pop，同时观察自身数同步自洽；独立分析仍为2。
            path.write_text(json.dumps(meta))
            with self.assertRaisesRegex(ValueError, "analysis/record mismatch"):
                observation.verify(root)

    def test_all_fixed_value_variants_decode(self):
        with tempfile.TemporaryDirectory() as temporary:
            root = Path(temporary)
            self.make(root)
            values = ["Uninitialized", "None", "Integer(-7)", "Bool(true)", "Bool(false)",
                      "Parameter(3)", "Opaque", "Frame", "Exception", "StoredException", "Retry"]
            path = root / "states.jsonl"
            record = json.loads(path.read_text())
            record["state"].update(locals=values, current_sources=[[i] for i in range(len(values))])
            path.write_text(json.dumps(record) + "\n")
            path = root / "metadata.json"
            meta = json.loads(path.read_text())
            meta["analysis_diagnostics"]["value_cells"] = 6 * len(values) + 3
            path.write_text(json.dumps(meta))
            self.assertTrue(observation.verify(root)["complete"])

    def test_ci_uses_real_upload_subdirectory_and_four_exact_observer_names(self):
        import trace_rust_preflight as gate
        repo = Path(__file__).resolve().parents[3]
        self.assertEqual(gate.OUTPUT, repo / "target/trace-rust-preflight")
        source = (repo / "crates/ryupy-mir/src/trace_frame_observer_tests.rs").read_text()
        import re
        names = re.findall(r"#\[test\]\s*fn (\w+)", source)
        self.assertEqual(set(names), set(gate.EXPECTED["observer"]))
        self.assertEqual(len(names), 4)
        workflow = (repo / ".github/retired-workflows/wcwidth-legacy-aot.yml").read_text()
        self.assertIn("target/trace-rust-preflight", workflow)
        self.assertIn("if: always()", workflow)


if __name__ == "__main__":
    unittest.main()
