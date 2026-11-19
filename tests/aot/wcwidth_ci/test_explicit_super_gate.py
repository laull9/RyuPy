"""只验证门禁判断，不模拟成真实编译或Native通过。"""
import json
from pathlib import Path
import tempfile
import unittest
from explicit_super_gate import check_plan, check_native, compile_command, aggregate


class ExplicitSuperGateTests(unittest.TestCase):
    def test_both_compile_modes_request_actual_diagnostic_report(self):
        cli, entry, output = Path('ryupy'), Path('entry.py'), Path('jit')
        common = ['build', entry, '--diagnostic-stages', '--out-dir', output]
        self.assertEqual(compile_command(cli, entry, output, False), [cli] + common)
        self.assertEqual(compile_command(cli, entry, output, True),
                         [cli, '--instance-module', 'subject'] + common)

    def test_missing_or_invalid_report_never_becomes_an_expected_plan(self):
        with tempfile.TemporaryDirectory() as directory:
            path = Path(directory) / 'module-environment.json'
            for instance in [False, True]:
                with self.assertRaises(FileNotFoundError):
                    check_plan(path, instance)
            path.write_text('not json')
            for instance in [False, True]:
                with self.assertRaises(json.JSONDecodeError):
                    check_plan(path, instance)

    def test_plan_rejects_silent_fallback(self):
        with tempfile.TemporaryDirectory() as directory:
            path = Path(directory) / 'plan.json'
            path.write_text(json.dumps({'modules': []}))
            check_plan(path, False)
            with self.assertRaises(RuntimeError):
                check_plan(path, True)
            path.write_text(json.dumps({'modules': [{'module': 'subject', 'compiled': True,
                                                      'execution': 'InstanceGlobals'}]}))
            check_plan(path, True)
            with self.assertRaises(RuntimeError):
                check_plan(path, False)

    def test_aggregate_requires_all_four_distinct_propositions(self):
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            for name, status in [('default', 'passed'), ('instance-negative', 'unsupported_expected'),
                                 ('instance-caller', 'passed'), ('native', 'passed')]:
                with self.assertRaises(FileNotFoundError):
                    aggregate(root)
                (root / name).mkdir()
                (root / name / 'result.json').write_text(json.dumps({'passed': True, 'status': status}))
            self.assertEqual(len(aggregate(root)), 4)
            (root / 'instance-negative/result.json').write_text(json.dumps({'passed': True, 'status': 'passed'}))
            with self.assertRaises(RuntimeError):
                aggregate(root)
            (root / 'instance-negative/result.json').write_text(json.dumps({'passed': False, 'status': 'unsupported_expected'}))
            with self.assertRaises(RuntimeError):
                aggregate(root)

    def test_native_rejects_instance_and_python_source(self):
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            path = root / 'ryupy-artifact.json'
            data = {'modules': [{'name': name, 'environment_reason': 'aot-not-migrated'}
                                for name in ['__main__', 'subject']]}
            path.write_text(json.dumps(data))
            check_native(root)
            data['modules'][1]['environment_reason'] = 'explicit-instance'
            path.write_text(json.dumps(data))
            with self.assertRaises(RuntimeError):
                check_native(root)
            data['modules'][1]['environment_reason'] = 'aot-not-migrated'
            path.write_text(json.dumps(data))
            (root / 'subject.py').write_text('pass')
            with self.assertRaises(RuntimeError):
                check_native(root)


if __name__ == '__main__':
    unittest.main()
