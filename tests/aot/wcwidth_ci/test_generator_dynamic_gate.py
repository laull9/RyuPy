"""新门脚本前置：只测命令/结果校验，不执行编译器、容器或项目入口。"""
import json
from pathlib import Path
import tempfile
import unittest
from unittest.mock import patch
import generator_dynamic_gate as gate


class GateTests(unittest.TestCase):
    def test_compile_arguments_use_real_output_switch(self):
        args = ['cli', 'build', Path('entry.py'), '--diagnostic-stages', '--out-dir', Path('out')]
        self.assertEqual(gate.compile_source('cli', Path('entry.py'), Path('out')), args)
        self.assertEqual(gate.compile_source('cli', Path('entry.py'), Path('out'), True),
                         ['cli', '--instance-module', 'subject'] + args[1:])

    def test_missing_or_incomplete_plan_is_not_legacy_success(self):
        with tempfile.TemporaryDirectory() as directory:
            path = Path(directory) / 'plan.json'
            with self.assertRaises(FileNotFoundError):
                gate.require_legacy(path)
            for modules in [[], [{'execution': 'LegacySingleton', 'compiled': False}],
                            [{'execution': 'InstanceGlobals', 'compiled': True}]]:
                path.write_text(json.dumps({'modules': modules}))
                with self.assertRaises(RuntimeError):
                    gate.require_legacy(path)
            path.write_text(json.dumps({'modules': [{'execution': 'LegacySingleton', 'compiled': True}]}))
            self.assertEqual(len(gate.require_legacy(path)['modules']), 1)

    def test_expected_failure_rejects_success_timeout_and_wrong_record(self):
        with patch.object(gate.h, 'run'):
            with self.assertRaises(RuntimeError):
                gate.require_failure('compile', ['cli'])
        for record in [{'name': 'other', 'exit': 1}, {'name': 'compile', 'exit': 0},
                       {'name': 'compile', 'exit': 1, 'timeout_expired': True}]:
            with patch.object(gate.h, 'run', side_effect=RuntimeError('failed')):
                with patch.object(gate.h, 'COMMANDS', [record]):
                    with self.assertRaises(RuntimeError):
                        gate.require_failure('compile', ['cli'])
        record = {'name': 'compile', 'exit': 1}
        with patch.object(gate.h, 'run', side_effect=RuntimeError('failed')):
            with patch.object(gate.h, 'COMMANDS', [record]):
                self.assertEqual(gate.require_failure('compile', ['cli']), record)

    def test_frame_contract_rejects_wrong_line_order_and_chain(self):
        text = ('Traceback (most recent call last):\n'
                '  File "/tmp/entry.py", line 11, in generate\nValueError: original\n\n'
                'During handling of the above exception, another exception occurred:\n\n'
                'Traceback (most recent call last):\n'
                '  File "entry.py", line 20, in <module>\n'
                '  File "entry.py", line 12, in generate\n'
                '  File "entry.py", line 5, in error\nTypeError: target failed\n')
        gate.check_frames(text)
        for bad in [text.replace('line 12', 'line 14'), text.replace('line 5', 'line 11'),
                    text.replace('ValueError: original', 'TypeError: original'),
                    text.replace('During handling', 'Before handling')]:
            with self.assertRaises(RuntimeError):
                gate.check_frames(bad)

    def test_fixture_keeps_observer_and_frames_separate(self):
        root = gate.FIXTURES
        self.assertNotIn('observer', (root / 'frames/entry.py').read_text().split('\n', 1)[1])
        control = (root / 'default/observer_control.py').read_text()
        self.assertIn('observer.run()', control)
        self.assertNotIn('yield', (root / 'default/observer.py').read_text())
        negative = (root / 'instance_negative/subject.py').read_text()
        self.assertIn('except kind:', negative)
        self.assertNotIn('class ', negative.split('\n', 1)[1])
        self.assertNotIn('del ', negative.split('\n', 1)[1])


if __name__ == '__main__':
    unittest.main()
