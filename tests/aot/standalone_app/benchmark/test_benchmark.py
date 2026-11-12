"""对照协议纯负例，不执行产品构建、不将测试成功冒充Native成功。"""
import importlib.util
import json
import os
from pathlib import Path
import tempfile
import unittest

HERE = Path(__file__).resolve().parent
import sys
sys.path.insert(0, str(HERE))
from measure import capture, elf, inventory, positive, recorded_environment, save, tree_rss
spec = importlib.util.spec_from_file_location('standalone_benchmark', HERE/'run.py')
gate = importlib.util.module_from_spec(spec); spec.loader.exec_module(gate)


class BenchmarkTests(unittest.TestCase):
    def test_environment_secret_values_never_serialized(self):
        secrets = {'GITHUB_TOKEN': 'fake-gh-private-092',
                   'ACTIONS_RUNTIME_TOKEN': 'fake-action-private-583',
                   'UNRELATED_SECRET': 'fake-other-private-741',
                   'DOTNET_SECRET': 'fake-prefix-private-862'}
        env = dict(secrets, PATH='/usr/bin:/bin', DOTNET_ROOT='/fixed/dotnet',
                   RYUPY_DEFAULT_AOT_INCLUDE='["pyfiglet"]')
        logged = recorded_environment(env)
        self.assertEqual(logged['env'], {'PATH':'/usr/bin:/bin', 'DOTNET_ROOT':'/fixed/dotnet',
                                       'RYUPY_DEFAULT_AOT_INCLUDE':'["pyfiglet"]'})
        self.assertEqual(logged['omitted_environment_key_count'], 4)
        with tempfile.TemporaryDirectory() as folder:
            prefix = Path(folder)/'actual-capture'
            result = capture(prefix, ['/bin/true'], Path(folder), env, 5)
            self.assertEqual(result['exit'], 0)
            raw = b''.join(p.read_bytes() for p in Path(folder).iterdir())
            for key, value in secrets.items():
                self.assertNotIn(value.encode(), raw)
                self.assertNotIn(key.encode(), raw)
            self.assertIn(b'/fixed/dotnet', raw)

    def test_nuitka_pinned_options_and_same_inputs(self):
        lock = json.loads((HERE/'nuitka-lock.json').read_text())
        self.assertEqual(lock['version'], '4.3.5')
        self.assertEqual(lock['sha256'], 'ca1dabc274b95efa7a46597f464ec27484b30f32cb89872d14d66541284d4f01')
        args = gate.nuitka_command(Path('/cp'), Path('/src/main.py'), Path('/src/package'), Path('/out'))
        for arg in ('--mode=standalone', '--jobs=1', '--include-package=pyfiglet', '--include-distribution-metadata=pyfiglet'):
            self.assertIn(arg, args)
        self.assertNotIn('--onefile', args)
        self.assertIn('--include-data-dir=/src/package/pyfiglet/fonts=pyfiglet/fonts', args)
        self.assertIn('--include-data-dir=/src/package/pyfiglet.egg-info=pyfiglet.egg-info', args)

    def test_no_ratios_for_incomplete_or_unbalanced_samples(self):
        for rows in ([], [{'case':'cli','arm':'ryupy','wall_ms':1}]*120):
            with self.assertRaises(ValueError): gate.summarize(rows)
        rows = [dict(case=c, arm=a, wall_ms=1) for c in ('cli','metadata') for a in ('ryupy','nuitka') for _ in range(30)]
        self.assertEqual(gate.summarize(rows)['cli']['ryupy_over_nuitka'], 1)

    def test_exit_raw_stderr_must_match(self):
        with tempfile.TemporaryDirectory() as folder:
            p = Path(folder)/'sample'; Path(str(p)+'.stdout').write_bytes(b'gold'); Path(str(p)+'.stderr').write_bytes(b'')
            good = dict(exit=0, failure=None, raw_streams_complete=True); positive(good,p,b'gold')
            for record in (dict(good,exit=1), dict(good,failure='timeout'),dict(good,raw_streams_complete=False)):
                with self.assertRaises(ValueError): positive(record,p,b'gold')
            Path(str(p)+'.stderr').write_bytes(b'warning')
            with self.assertRaises(ValueError): positive(good,p,b'gold')

    def test_real_elf_not_jit_or_script(self):
        with tempfile.TemporaryDirectory() as folder:
            p=Path(folder)/'app';p.write_bytes(b'#!/bin/sh\nexit 0\n');p.chmod(0o755)
            with self.assertRaises(ValueError): elf(p)
            body=bytearray(64);body[:6]=b'\x7fELF\x02\x01';body[18:20]=(62).to_bytes(2,'little');p.write_bytes(body);elf(p)
            p.chmod(0o644)
            with self.assertRaises(ValueError): elf(p)

    def test_inventory_and_link_escape(self):
        with tempfile.TemporaryDirectory() as folder:
            root=Path(folder)/'dist';root.mkdir();(root/'file').write_bytes(b'abc');x=inventory(root)
            self.assertEqual(x['logical_bytes'],3)
            (root/'link').symlink_to('../escape')
            with self.assertRaises(ValueError): inventory(root)

    def test_process_tree_rss_sum_not_single_child(self):
        with tempfile.TemporaryDirectory() as folder:
            proc=Path(folder)
            for pid,parent,group,rss in ((10,1,10,3),(11,10,10,5),(12,11,12,7),(99,1,99,100)):
                d=proc/str(pid);d.mkdir();fields=['S',str(parent),str(group)]+['0']*19;fields[21]=str(rss)
                (d/'stat').write_text(str(pid)+' (command with spaces) '+' '.join(fields))
            self.assertEqual(tree_rss(10,set(),proc),15*os.sysconf('SC_PAGE_SIZE'))

    def test_isolation_finally_and_separate_measurement(self):
        source=(HERE/'run.py').read_text()
        for token in ('source.rename(hidden)', 'finally:', 'hidden.rename(source)', "result['compatible'] = True", "result['steady_state'] = 'not-measured"):
            self.assertIn(token,source)
        self.assertIn("record = checked(case+'-'+arm+'-compile', argv, 600, True",source)
        self.assertIn("native_env=clean, expected=golden",source)
        for token in ('validate_app_artifact', "receipt.get('structure_validated')", "'--network', 'none'", "'--read-only'", "'--security-opt=no-new-privileges'", "dst=/app,readonly", "'PYTHONNOUSERSITE': '1'"):
            # mount参数含固定前缀，其余均直接存在。
            self.assertIn(token, source)
        self.assertNotIn("[options.python, '-B', '-S'", source)



if __name__ == '__main__': unittest.main()
