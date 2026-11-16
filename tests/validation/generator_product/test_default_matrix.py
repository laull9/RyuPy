"""无flag矩阵/真实Run接线的纯mock；不调用产品，也不把mock流当兼容证据。"""
import json
from pathlib import Path
from types import SimpleNamespace
import tempfile
import unittest
from unittest.mock import Mock, patch
import generator_product_gate as gate
import product_contract as c

HERE = Path(__file__).resolve().parent


class DefaultMatrixTests(unittest.TestCase):
    def test_same_candidates_have_exact_default_explicit_and_rejection_matrix(self):
        plan = gate.stage_plan()
        self.assertEqual(len(plan), 18)
        self.assertEqual(len(set(plan)), len(plan))
        for explicit in (False, True):
            positives = [(name, mode) for name, mode, enabled in plan if enabled == explicit and name not in c.REJECTED]
            self.assertEqual(len(positives) * 2, 12)
            self.assertEqual(set(positives), {(name, mode) for name in (*c.CASES[:2], c.HOT) for mode in ('jit', 'native')})
        self.assertEqual(sum(enabled and name in c.REJECTED for name, _, enabled in plan), 4)
        self.assertEqual([(name, mode) for name, mode, enabled in plan if not enabled and name in c.REJECTED], [(name, 'jit') for name in sorted(c.REJECTED)])

    def test_original_b_stage_really_invokes_no_flag_run_and_decodes_that_product(self):
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            runner = gate.Runner.__new__(gate.Runner)
            runner.out = root / 'evidence'; runner.out.mkdir()
            runner.work = root / 'work'; runner.work.mkdir()
            runtime = root / 'RyuPy.Runtime.dll'; runtime.write_bytes(b'mock-runtime')
            runner.tools = {'runtime': runtime, 'cli': root / 'ryupy', 'dotnet': root / 'dotnet'}
            runner.config = {'pic_modes': ['1', '0']}
            runner.repo = HERE.parents[2]
            runner.oracle = c.oracle(HERE / 'goldens', runner.repo / 'tests/cases/generator_dynamic/frames/entry.py')
            runner.native = SimpleNamespace(preserve_dist=lambda *args: 'same', snapshot=lambda *args: ('same', None))
            runner.decode = Mock()
            calls = []
            def command(stage, label, argv, expected, timeout, pic=None):
                calls.append((label, argv, expected, pic))
                if label == 'compile': output = Path(argv[argv.index('--out-dir') + 1])
                elif label == 'cli-run-no-flag': output = Path(argv[2]).parent / '__pycache__' / 'entry'
                else: output = None
                if output:
                    output.mkdir(parents=True)
                    (output / 'RyuPy.Runtime.dll').write_bytes(runtime.read_bytes())
                    (output / 'entry.dll').write_bytes(b'mock-pe')
                return {'exit': expected}, b'ready\n', (''.join('[ryupy-stage] event=' + event + ' stage=' + name + ' module="__main__"\n' for name in ('trace-v1-prepare', 'trace-v1-lower') for event in ('begin', 'end')).encode() if label == 'compile' else b'mock-only')
            runner.command = command
            with patch.object(gate, 'require_plan'), patch.object(c, 'compare', return_value=b'mock-only') as compare:
                runner.stage('b_target_failure', 'jit', False)
            self.assertEqual([item[0] for item in calls], ['compile', 'run-pic-1', 'run-pic-0', 'cli-run-no-flag'])
            self.assertEqual(calls[-1][1][0:2], [runner.tools['cli'], 'run'])
            self.assertEqual(calls[-1][2:], (1, '1'))
            self.assertNotIn(gate.FLAG, calls[-1][1])
            self.assertNotIn('--diagnostic-stages', calls[-1][1])
            self.assertEqual(compare.call_count, 3)
            self.assertEqual(runner.decode.call_count, 2)
            self.assertEqual(runner.decode.call_args.args[1].parts[-3:], ('__pycache__', 'entry', 'entry.dll'))
            self.assertTrue(runner.decode.call_args.args[2])
            result = json.loads((runner.out / 'b_target_failure-jit-default/result.json').read_text())
            self.assertEqual(result['selection_request'], 'ordinary-cli-no-flag')
            self.assertTrue(result['protocol'])


if __name__ == '__main__': unittest.main()
