"""完整门禁解释器路由的纯脚本自检；模拟编排，不启动Rust/.NET或产品。"""
import os
from pathlib import Path
import sys
import tempfile
import unittest
from unittest.mock import patch
import full_check


class InterpreterRoutingTests(unittest.TestCase):
    def test_make_and_child_receive_the_verified_interpreter(self):
        calls = []
        def record(name, args, **kwargs):
            calls.append((name, args, kwargs))
            if name == 'explicit-super-pic-off':
                # 仅路由单测的模拟报告，不作为产品证据输出。
                result = full_check.h.OUT / 'explicit-super-pic-off'
                result.mkdir()
                (result / 'results.trx').write_text('<TestRun><Counters passed="1" failed="0"/></TestRun>')
        with tempfile.TemporaryDirectory() as folder:
            with patch.object(full_check.h, 'REPO', Path(folder)), patch.object(full_check.h, 'run', record), \
                 patch.object(full_check.build_identity, 'before'), patch.object(full_check.build_identity, 'after'), \
                 patch.object(full_check.sys, 'version_info', (3, 12, 13)), \
                 patch.dict(os.environ, PYTHON=sys.executable, DOTNET='not-executed', CHECK_MODE='pilot', GITHUB_SHA='test-only'):
                full_check.main()
        by_name = {name: (args, options) for name, args, options in calls}
        python = str(Path(sys.executable).resolve())
        for name in ('make-plan', 'make-check', 'python-child-version'):
            env = by_name[name][1]['env']
            self.assertEqual(env['PYTHON'], python)
            self.assertEqual(env['RYUPY_FOREACH_PYTHON'], python)
        self.assertEqual(by_name['python-child-version'][0], [Path(python), '--version'])
        self.assertEqual(by_name['python-child-version'][1]['expected'], b'Python 3.12.13\n')
        self.assertIn('CARGO_FEATURES=ryupy-driver/ordinary-dynamic-module-pilot', by_name['make-check'][0])
        self.assertIn('trx;LogFileName=dotnet-test.trx', by_name['make-check'][1]['env']['DOTNET_TEST_ARGS'])
        self.assertIn('FULL_CHECK_EVIDENCE', by_name['make-check'][1]['env'])
        env = by_name['make-check'][1]['env']
        self.assertEqual(Path(env['RYUPY_TRACE_EVIDENCE_DIR']), Path(env['FULL_CHECK_EVIDENCE']) / 'debug-pe')
        self.assertEqual(sum(name == 'make-check' for name, _, _ in calls), 1)
        self.assertFalse(any(name.startswith('trace-') for name, _, _ in calls))

    def test_failed_original_make_still_collects_identity_without_extra_test_process(self):
        calls = []
        def fail(name, args, **kwargs):
            calls.append(name)
            if name == 'make-check':
                raise RuntimeError('原Debug测试失败')
        with tempfile.TemporaryDirectory() as folder:
            with patch.object(full_check.h, 'REPO', Path(folder)), patch.object(full_check.h, 'run', fail), \
                 patch.object(full_check.build_identity, 'before'), patch.object(full_check.build_identity, 'after') as after, \
                 patch.object(full_check.sys, 'version_info', (3, 12, 13)), \
                 patch.dict(os.environ, PYTHON=sys.executable, CHECK_MODE='default', GITHUB_SHA='test-only'):
                with self.assertRaisesRegex(RuntimeError, '原Debug测试失败'):
                    full_check.main()
                after.assert_called_once_with(Path(folder), Path(folder) / 'target/full-check-evidence')
        self.assertEqual(calls[-1], 'make-check')
        self.assertEqual(calls.count('make-check'), 1)

    def test_debug_evidence_is_under_existing_always_uploaded_tree(self):
        workflow = full_check.h.REPO / '.github/workflows/wcwidth-legacy-aot.yml'
        section = workflow.read_text().split('      - name: 保存完整检查原流\n', 1)[1].split('\n  projects:', 1)[0]
        self.assertIn('if: always()', section)
        self.assertIn('            target/full-check-evidence\n', section)
        self.assertIn('name: full-check-${{ matrix.mode }}-', section)

    def test_wrong_interpreter_version_is_rejected_before_any_child(self):
        with patch.object(full_check.h, 'run') as child, \
             patch.object(full_check.sys, 'version_info', (3, 12, 14)), \
             patch.dict(os.environ, PYTHON=sys.executable):
            with self.assertRaisesRegex(RuntimeError, '固定CPython3.12.13'):
                full_check.main()
            child.assert_not_called()

    def test_mismatched_executable_is_rejected_before_any_child(self):
        with tempfile.TemporaryDirectory() as folder:
            with patch.object(full_check.h, 'REPO', Path(folder)), patch.object(full_check.h, 'run') as child, \
                 patch.dict(os.environ, PYTHON='/bin/sh', CHECK_MODE='pilot', GITHUB_SHA='test-only'):
                with self.assertRaises(RuntimeError): full_check.main()
                child.assert_not_called()


if __name__ == '__main__':
    unittest.main()
