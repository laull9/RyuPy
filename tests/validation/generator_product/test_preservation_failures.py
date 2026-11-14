"""首败归档路径纯mock：精确核目标异常/到达点，不运行任何Rust、.NET或CP源。"""
import json
from pathlib import Path
from types import SimpleNamespace
import tarfile
import tempfile
import unittest
from unittest.mock import patch
import generator_product_gate as gate
import product_contract as c

HERE = Path(__file__).resolve().parent
REPO = HERE.parents[2]


def trace(rejected=False):
    names = ('trace-v1-prepare',) if rejected else ('trace-v1-prepare', 'trace-v1-lower')
    raw = ''.join('[ryupy-stage] event=' + event + ' stage=' + name + ' module="__main__"\n'
                  for name in names for event in ('begin', 'end'))
    if rejected: raw += '[E0310] trace V1 整产物拒绝：mock\n'
    return raw.encode()


class PreservationFailures(unittest.TestCase):
    def runner(self, root):
        _, native = gate.load_reused(REPO)
        runner = gate.Runner.__new__(gate.Runner)
        runner.out, runner.work = root / 'evidence', root / 'work'
        runner.out.mkdir(); runner.work.mkdir()
        runtime = root / 'RyuPy.Runtime.dll'; runtime.write_bytes(b'mock-runtime')
        runner.tools = {'cli': root / 'not-executed', 'dotnet': root / 'not-executed-dotnet', 'runtime': runtime}
        runner.config = {'pic_modes': ['1', '0']}
        runner.oracle = c.oracle(HERE / 'goldens', REPO / 'tests/cases/generator_dynamic/frames/entry.py')
        runner.native = native
        runner.decode = lambda *args: None
        return runner, native

    def test_jit_compile_plan_runtime_and_run_first_failures_keep_exact_pe(self):
        for failure in ('compile', 'plan', 'runtime', 'run', 'rejection'):
            with self.subTest(failure=failure), tempfile.TemporaryDirectory() as directory:
                root = Path(directory); runner, native = self.runner(root)
                calls, events = [], []
                def preserve(output, evidence):
                    events.append('archive'); return native.preserve_dist(output, evidence)
                runner.native = SimpleNamespace(preserve_dist=preserve, snapshot=native.snapshot)
                rejected = failure == 'rejection'
                def command(stage, label, argv, expected, timeout, pic=None):
                    calls.append(label); events.append(label)
                    if label != 'compile': raise ValueError('first-run-failure')
                    output = Path(argv[argv.index('--out-dir') + 1]); output.mkdir()
                    (output / 'entry.dll').write_bytes(b'exact-first-pe')
                    (output / 'RyuPy.Runtime.dll').write_bytes(b'wrong' if failure == 'runtime' else b'mock-runtime')
                    if failure == 'compile': raise ValueError('first-compile-failure')
                    return {'exit': 1 if rejected else 0, 'failure': None, 'raw_streams_complete': True}, b'', trace(rejected)
                def plan(path):
                    events.append('plan')
                    if failure == 'plan': raise ValueError('first-plan-failure')
                runner.command = command
                name = sorted(c.REJECTED)[0] if rejected else c.CASES[0]
                expected = {'runtime': 'JIT实际runtime不符', 'rejection': '拒绝后发射了托管/交付产物'}.get(failure, 'first-' + failure + '-failure')
                with patch.object(gate, 'require_plan', plan), self.assertRaisesRegex(ValueError, expected):
                    runner.stage(name, 'jit')
                self.assertEqual(calls, ['compile', 'run-pic-1'] if failure == 'run' else ['compile'])
                self.assertEqual(events[:2], ['compile', 'archive'])
                evidence = runner.out / (name + '-jit')
                with tarfile.open(evidence / 'native-dist.tar.gz') as archive:
                    self.assertEqual(archive.extractfile('output/entry.dll').read(), b'exact-first-pe')
                self.assertTrue((evidence / 'dist-identity.json').is_file())

    def test_preservation_failure_never_replaces_compile_failure_but_fails_success_path(self):
        for primary in (False, True):
            with self.subTest(primary=primary), tempfile.TemporaryDirectory() as directory:
                root = Path(directory); runner, _ = self.runner(root)
                calls = []
                def command(stage, label, argv, *args):
                    calls.append(label)
                    output = Path(argv[argv.index('--out-dir') + 1]); output.mkdir()
                    (output / 'entry.dll').write_bytes(b'partial')
                    if primary: raise ValueError('first-compile-failure')
                    return {}, b'', trace()
                def broken_archive(*args): raise OSError('archive-failure')
                runner.command = command
                runner.native = SimpleNamespace(preserve_dist=broken_archive)
                error, text = (ValueError, 'first-compile-failure') if primary else (OSError, 'archive-failure')
                with self.assertRaisesRegex(error, text): runner.stage(c.CASES[0], 'jit')
                self.assertEqual(calls, ['compile'])
                record = json.loads((runner.out / (c.CASES[0] + '-jit') / 'preservation-failure.json').read_text())
                self.assertEqual(record['error'], 'archive-failure')
                self.assertEqual(record['primary_error'], 'first-compile-failure' if primary else None)

    def test_native_compile_failure_saves_top_level_managed_input_not_obj(self):
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory); runner, _ = self.runner(root)
            def command(stage, label, argv, *args):
                self.assertEqual(label, 'compile')
                output = Path(argv[argv.index('--out-dir') + 1]); (output / 'obj').mkdir(parents=True)
                (output / 'entry.dll').write_bytes(b'partial-managed')
                (output / 'obj/not-in-evidence.dll').write_bytes(b'not-selected')
                raise ValueError('first-native-compile-failure')
            runner.command = command
            with self.assertRaisesRegex(ValueError, 'first-native-compile-failure'):
                runner.stage(c.CASES[0], 'native')
            with tarfile.open(runner.out / (c.CASES[0] + '-native') / 'native-dist.tar.gz') as archive:
                self.assertEqual(archive.extractfile('partial-managed-input/entry.dll').read(), b'partial-managed')
                self.assertFalse(any('obj' in name for name in archive.getnames()))

    def test_actual_run_preservation_failure_keeps_run_first_cause(self):
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory); runner, native = self.runner(root)
            calls = []
            def preserve(output, evidence):
                if output.parent.name == '__pycache__': raise OSError('run-archive-failure')
                return native.preserve_dist(output, evidence)
            runner.native = SimpleNamespace(preserve_dist=preserve, snapshot=native.snapshot)
            def command(stage, label, argv, expected, timeout, pic=None):
                calls.append(label)
                if label == 'compile': output = Path(argv[argv.index('--out-dir') + 1])
                elif label == 'cli-run-no-flag': output = Path(argv[2]).parent / '__pycache__/entry'
                else: return {'exit': expected}, b'', b''
                output.mkdir(parents=True)
                (output / 'entry.dll').write_bytes(b'exact-pe')
                (output / 'RyuPy.Runtime.dll').write_bytes(b'mock-runtime')
                if label == 'cli-run-no-flag': raise ValueError('first-cli-run-failure')
                return {}, b'', trace()
            runner.command = command
            with patch.object(gate, 'require_plan'), patch.object(c, 'compare', return_value=b''), self.assertRaisesRegex(ValueError, 'first-cli-run-failure'):
                runner.stage(c.CASES[1], 'jit', False)
            self.assertEqual(calls, ['compile', 'run-pic-1', 'run-pic-0', 'cli-run-no-flag'])
            record = json.loads((runner.out / (c.CASES[1] + '-jit-default/cli-run-pe/preservation-failure.json')).read_text())
            self.assertEqual(record['error'], 'run-archive-failure')
            self.assertEqual(record['primary_error'], 'first-cli-run-failure')


if __name__ == '__main__': unittest.main()
