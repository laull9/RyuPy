"""搬迁流程纯mock回归；伪ELF只校验结构，绝不执行产物或启动SDK。"""
import json
import os
from pathlib import Path
import subprocess
import tempfile
from types import SimpleNamespace
import unittest
from unittest.mock import patch

from bench_relocation import hidden_originals, verify_relocation
from test_bench_smoke import bundle


class BenchRelocationTests(unittest.TestCase):
    def setup_case(self, root):
        source=root/'hello.py';source.write_text('print("hello")\n')
        product=root/'generated';bundle(product,source)
        out=root/'evidence';out.mkdir()
        return source,product,out

    def test_relocated_only_dist_with_hidden_original_and_clean_environment(self):
        with tempfile.TemporaryDirectory() as temp:
            root=Path(temp);source,product,out=self.setup_case(root);calls=[]
            def run(label, command, **kwargs):
                calls.append(label)
                self.assertEqual(kwargs['timeout'],30)
                self.assertTrue(kwargs['check'])
                env=kwargs['env']
                self.assertNotIn('LD_LIBRARY_PATH',env)
                self.assertNotIn('RYUPY_STATS',env)
                self.assertEqual(list(Path(env['PATH']).iterdir()),[])
                self.assertEqual(list(kwargs['cwd'].iterdir()),[])
                if label=='relocation-cpython':
                    self.assertEqual(command[1:3],['-I','-B'])
                    self.assertEqual(Path(command[-1]).read_bytes(),source.read_bytes())
                    self.assertTrue(product.exists())
                else:
                    self.assertFalse(product.exists())
                    self.assertTrue(source.exists())  # 用户/仓库原文件从不移动。
                    native=Path(command[0]);self.assertTrue(native.is_file())
                    self.assertIn('搬迁 空格 & 验证',str(native))
                    self.assertNotEqual(native.parent,product/'hello.dist')
                    self.assertFalse((native.parent/'hello.build').exists())
                    self.assertFalse((native.parent.parent/'oracle-input').exists())
                return subprocess.CompletedProcess(command,0,b'hello\n',b'')
            with patch.dict(os.environ,{'LD_LIBRARY_PATH':'poison','RYUPY_STATS':'poison'}):
                result=verify_relocation(product,source,out,'10.0.403',SimpleNamespace(run=run))
            self.assertEqual(calls,['relocation-cpython','relocation-native'])
            self.assertEqual(result['status'],'passed')
            self.assertFalse(result['clean_machine']);self.assertFalse(result['source_checkout_hidden'])
            self.assertTrue(product.exists());self.assertTrue(source.exists())
            self.assertEqual(list(root.glob('.ryupy-relocation-*')),[])

    def test_mismatch_and_timeout_fail_and_restore_owned_paths(self):
        for mode in ('stdout','stderr','exit','timeout'):
            with self.subTest(mode=mode),tempfile.TemporaryDirectory() as temp:
                root=Path(temp);source,product,out=self.setup_case(root)
                def run(label,command,**kwargs):
                    if label=='relocation-cpython':return subprocess.CompletedProcess(command,0,b'hello\n',b'')
                    self.assertFalse(product.exists())
                    if mode=='timeout':raise subprocess.TimeoutExpired(command,30)
                    return subprocess.CompletedProcess(command,1 if mode=='exit' else 0,
                        b'wrong' if mode=='stdout' else b'hello\n',b'wrong' if mode=='stderr' else b'')
                with self.assertRaises((ValueError,subprocess.TimeoutExpired)):
                    verify_relocation(product,source,out,'10.0.403',SimpleNamespace(run=run))
                report=json.loads((out/'relocation.json').read_text())
                self.assertEqual(report['status'],'failed')
                self.assertTrue(report['original_paths_restored']);self.assertTrue(source.exists())
                self.assertEqual(list(root.glob('.ryupy-relocation-*')),[])

    def test_partial_hide_failure_restores_first_path(self):
        with tempfile.TemporaryDirectory() as temp:
            root=Path(temp);first=root/'owned';first.mkdir()
            with self.assertRaises(FileNotFoundError):
                with hidden_originals((first,root/'missing')):self.fail('不能进入运行阶段')
            self.assertTrue(first.is_dir());self.assertEqual(list(root.glob('.ryupy-relocation-*')),[])

    def test_existing_destination_is_never_overwritten(self):
        with tempfile.TemporaryDirectory() as temp:
            root=Path(temp);source,product,out=self.setup_case(root)
            (out/'搬迁 空格 & 验证').mkdir()
            with self.assertRaises(FileExistsError):
                verify_relocation(product,source,out,'10.0.403',SimpleNamespace(run=lambda *a,**k:self.fail('不应运行')))
            self.assertTrue(product.exists());self.assertTrue(source.exists())


if __name__=='__main__':unittest.main()
