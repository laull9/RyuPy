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

    def test_mismatched_executable_is_rejected_before_any_child(self):
        with tempfile.TemporaryDirectory() as folder:
            with patch.object(full_check.h, 'REPO', Path(folder)), patch.object(full_check.h, 'run') as child, \
                 patch.dict(os.environ, PYTHON='/bin/sh', CHECK_MODE='pilot', GITHUB_SHA='test-only'):
                with self.assertRaises(RuntimeError): full_check.main()
                child.assert_not_called()


if __name__ == '__main__':
    unittest.main()
