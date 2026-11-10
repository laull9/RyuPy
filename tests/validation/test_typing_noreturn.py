"""NoReturn验收脚本纯mock：不下载工具、不运行CP目标或产品，不把模拟结果冒产品验收。"""
import ast
import json
import os
import re
from pathlib import Path
import tempfile
import unittest
from unittest.mock import patch
import xml.etree.ElementTree as ET

import typing_noreturn as n


class Tests(unittest.TestCase):
    def test_frozen_inputs_and_plain_probe(self):
        self.assertEqual(len(n.FROZEN), 14)
        for path, expected in n.FROZEN.items():
            self.assertEqual(n.sha(n.ROOT / path), expected)
        source = n.ROOT / 'tests/projects/entries/markdown_sample.py'
        full = source.read_bytes()
        probe = n.plain_probe(source)
        self.assertEqual(len(ast.parse(probe).body), 2)
        self.assertEqual(probe, b''.join(full.splitlines(keepends=True)[:4]))
        self.assertEqual(source.read_bytes(), full)
        self.assertEqual(len((n.ROOT / 'tests/projects/baselines/markdown/stdout.txt').read_bytes()), 77)

    def test_reusable_workflow_is_same_commit_read_only_and_preserves_stages(self):
        main = (n.ROOT / '.github/workflows/wcwidth-legacy-aot.yml').read_text()
        called = (n.ROOT / '.github/workflows/typing-noreturn.yml').read_text()
        block = main.split('  typing-noreturn:\n', 1)[1].split('  baseline:\n', 1)[0]
        self.assertIn('    uses: ./.github/workflows/typing-noreturn.yml', block)
        self.assertIn('    needs: select-mode', block)
        self.assertNotIn('@', block)
        self.assertIn('on:\n  workflow_call:\npermissions:\n  contents: read\n', called)
        self.assertNotIn('workflow_dispatch:', called)
        self.assertNotIn('secrets:', called)
        self.assertNotIn('contents: write', called)
        self.assertEqual(len(re.findall(r'^  [a-z][a-z0-9-]*:', called.split('jobs:\n')[1], re.M)), 1)
        for required in ['fetch-depth: 2', 'persist-credentials: false', 'wcwidth_ci/prepare.py',
                         'tests/validation/typing_noreturn.py', 'test_typing_noreturn.py',
                         'typing-noreturn-evidence-${{ github.sha }}', 'typing-noreturn-tools-${{ github.sha }}',
                         'typing-noreturn-native-${{ github.sha }}']:
            self.assertIn(required, called)
        self.assertEqual(called.count('if: always()'), 3)
        self.assertEqual(called.count('if-no-files-found: error'), 2)
        self.assertIn('!target/typing-noreturn-evidence/native-build/**', called)
        self.assertEqual(called.count('if-no-files-found: warn'), 1)
        self.assertNotIn('continue-on-error:', called)
        uploads = called.split('uses: actions/upload-artifact@')[1:]
        self.assertEqual(len(uploads), 3)
        paths = [re.search(r'^          path: (.*?)(?=^          (?:#|if-no-files-found))',
                           block, re.M | re.S)[1].strip() for block in uploads]
        self.assertEqual(paths[1], 'target/typing-noreturn-evidence/tools')
        self.assertEqual(paths[2], 'target/typing-noreturn-evidence/native-dist.tar.gz')
        self.assertEqual(paths[0].splitlines()[0], '|')
        self.assertEqual([line.strip() for line in paths[0].splitlines()[1:]], [
            'target/typing-noreturn-evidence', '!target/typing-noreturn-evidence/tools/**',
            '!target/typing-noreturn-evidence/native-dist.tar.gz',
            '!target/typing-noreturn-evidence/native-build/**',
            '!target/typing-noreturn-evidence/relocated/**', 'target/typing-noreturn-prepare',
            '${{ runner.temp }}/wcwidth-aot-tools/download-identity.json'])

        for text in [main, called]:
            self.assertLessEqual(len(text.splitlines()), 600)

    def test_base_tree_is_frozen_public_parent_tree_not_private_history(self):
        self.assertEqual(n.BASE_TREE, 'eb1d3432a0a00917e6a320ee47c2f7eac7d2c37e')
        self.assertEqual(n.REMOTE_BASE_HEAD, '7c9c895178181664d06a113ba369185e19650688')
        self.assertEqual(n.BASE_SOURCE_HEAD, 'fb9c1214a7ce0f00f9793c9904c2a6fa412bc31e')
        # 全流程mock的base-files命令拒绝两个commit查询；私有原始历史只做来源说明。
        tree = ast.parse((n.ROOT / 'tests/validation/typing_noreturn.py').read_text())
        invocations = [x for x in ast.walk(tree) if isinstance(x, ast.Call) and
                       isinstance(x.func, ast.Attribute) and x.func.attr == 'checked' and
                       x.args and isinstance(x.args[0], ast.Constant) and x.args[0].value == 'base-files']
        self.assertEqual(len(invocations), 1)
        self.assertEqual(invocations[0].args[1].elts[-1].id, 'BASE_TREE')

    def test_exact_context(self):
        head = '1' * 40
        env = dict(GITHUB_ACTIONS='true', RUNNER_ENVIRONMENT='github-hosted', GITHUB_EVENT_NAME='push',
                   GITHUB_REF=n.shared.REF, GITHUB_SHA=head, GITHUB_RUN_ATTEMPT='1')
        event = {'ref': n.shared.REF, 'after': head, 'head_commit': {'id': head, 'message': n.TITLE}}
        n.check_context(env, event)
        for title in [n.TITLE.upper(), n.TITLE + '\n', n.shared.TITLE]:
            with self.assertRaises(ValueError):
                n.check_context(env, dict(event, head_commit={'id': head, 'message': title}))
        for key, value in [('RUNNER_ENVIRONMENT', 'self-hosted'), ('GITHUB_RUN_ATTEMPT', '2'),
                           ('GITHUB_EVENT_NAME', 'workflow_dispatch'), ('RUSTFLAGS', '-A warnings')]:
            with self.assertRaises(ValueError):
                n.check_context(dict(env, **{key: value}), event)
        self.assertEqual(event['head_commit']['message'], n.TITLE)

    def test_trx_rejects_missing_duplicate_skipped_failed_and_wrong_names(self):
        names = ['RyuPy.Runtime.Tests.PyTypingNoReturnTests.' + x for x in n.FACTS]
        names += ['RyuPy.Runtime.Tests.PyTypingCallableCacheTests.Old',
                  'RyuPy.Runtime.Tests.PyTypingNamedTupleAnnotationsTests.Old']
        with tempfile.TemporaryDirectory() as tmp:
            path = Path(tmp) / 'test.trx'
            def write(items, outcome='Passed'):
                root = ET.Element('TestRun')
                for name in items:
                    ET.SubElement(root, '{http://microsoft.com/schemas/VisualStudio/TeamTest/2010}UnitTestResult',
                                  testName=name, outcome=outcome)
                ET.ElementTree(root).write(path)
            write(names)
            self.assertEqual(n.verify_trx(path)['new_facts'], 9)
            for items in [[], names[:-1], names[1:], names + names[:1],
                          [x.replace('PyTypingNoReturnTests.', 'PyTypingNoReturnTestsWrong.') for x in names]]:
                write(items)
                with self.assertRaises(ValueError):
                    n.verify_trx(path)
            for status in ['Failed', 'NotExecuted']:
                write(names, status)
                with self.assertRaises(ValueError):
                    n.verify_trx(path)

    def test_positive_requires_exact_bytes_exit_and_complete_streams(self):
        with tempfile.TemporaryDirectory() as tmp:
            p = Path(tmp) / 'run'
            p.with_suffix('.stdout').write_bytes(b'golden')
            p.with_suffix('.stderr').write_bytes(b'')
            good = dict(exit=0, failure=None, raw_streams_complete=True)
            self.assertTrue(n.positive(good, p, b'golden'))
            for change in [dict(exit=1), dict(exit=-9), dict(failure='timeout'), dict(raw_streams_complete=False)]:
                self.assertFalse(n.positive(dict(good, **change), p, b'golden'))
            self.assertFalse(n.positive(good, p, b'other'))
            p.with_suffix('.stderr').write_bytes(b'error')
            self.assertFalse(n.positive(good, p, b'golden'))

    def test_compile_failure_does_not_run_jit_or_claim_native(self):
        with tempfile.TemporaryDirectory() as tmp:
            runner = object.__new__(n.Runner)
            runner.out = Path(tmp)
            runner.cli = Path('/fake/ryupy')
            runner.env = {}
            calls = []
            def fail(name, argv, timeout, env):
                calls.append((name, list(map(str, argv))))
                return dict(exit=1, failure=None)
            runner.command = fail
            runner.run_positive = lambda *args: self.fail('编译失败后不应执行产品')
            with self.assertRaises(ValueError):
                runner.jit('contract', Path('/fake/contract.py'), b'golden')
            self.assertEqual([x[0] for x in calls], ['contract-compile'])
            self.assertNotIn('--aot', calls[0][1])

    def test_phase_failure_is_recorded_and_later_phase_runs(self):
        with tempfile.TemporaryDirectory() as tmp:
            runner = object.__new__(n.Runner)
            runner.out = Path(tmp)
            runner.result = {'stages': {}}
            def fail():
                raise ValueError('真实full失败不能作为PASS')
            self.assertFalse(runner.phase('markdown-full', fail))
            self.assertTrue(runner.phase('independent', lambda: None))
            record = json.loads((runner.out / 'result.json').read_text())
            self.assertFalse(record['stages']['markdown-full']['passed'])
            self.assertEqual(record['stages']['markdown-full']['status'], 'failed')
            self.assertTrue(record['stages']['independent']['passed'])

    def test_native_failed_publish_preserves_raw_without_executing(self):
        with tempfile.TemporaryDirectory() as tmp:
            runner = object.__new__(n.Runner)
            runner.out = Path(tmp)
            runner.cli = Path('/fake/ryupy'); runner.env = {}
            target = runner.out / 'native-build'
            sdk = target / 'entry.build/attempt-0/ryupy-aot-0'
            sdk.mkdir(parents=True)
            (sdk / 'publish.stderr.log').write_bytes(b'ILC error')
            (sdk / 'obj').mkdir()
            (sdk / 'obj/secret.dll').write_bytes(b'excluded')
            calls = []
            def command(name, argv, timeout, env):
                calls.append(name)
                self.assertIn('--aot', argv)
                return dict(exit=1, failure=None)
            runner.command = command
            runner.run_positive = lambda *args: self.fail('发布失败不得执行')
            with self.assertRaises(ValueError):
                runner.native(Path('/fake/entry.py'), b'golden')
            self.assertEqual(calls, ['native-publish'])
            saved = runner.out / 'sdk-evidence/entry.build/attempt-0/ryupy-aot-0'
            self.assertEqual((saved / 'publish.stderr.log').read_bytes(), b'ILC error')
            self.assertFalse((saved / 'obj').exists())

    def test_product_capture_uses_unique_clean_cwd_and_passed_environment(self):
        with tempfile.TemporaryDirectory() as tmp:
            runner = object.__new__(n.Runner)
            runner.out = Path(tmp); runner.labels = set(); runner.env = {'ignored': 'true'}
            env = {'PATH': '/usr/bin:/bin'}
            def capture(prefix, argv, cwd, timeout, actual_env):
                self.assertEqual(actual_env, env)
                self.assertFalse(any(cwd.iterdir()))
                self.assertEqual(cwd, runner.out / 'cwd/native-relocated')
                prefix.with_suffix('.stdout').write_bytes(b'golden')
                prefix.with_suffix('.stderr').write_bytes(b'')
                return dict(exit=0, failure=None, raw_streams_complete=True)
            with patch.object(n.shared, 'capture', capture):
                runner.run_positive('native-relocated', ['/fake/elf'], b'golden', env)
                with self.assertRaises(ValueError):
                    runner.run_positive('native-relocated', ['/fake/elf'], b'golden', env)

    def test_orchestration_early_stop_and_independent_product_failures(self):
        # 所有命令都是内存mock；专门覆盖早门停链与full失败仍非零。
        actual_root = n.ROOT
        for failure in ['csharp', 'tools', 'contract-jit', 'markdown-plain', 'markdown-full', None]:
            with self.subTest(failure=failure), tempfile.TemporaryDirectory() as tmp:
                root = Path(tmp)
                calls = []
                identity = {'head': '1' * 40, 'files': {}}
                event = root / 'event.json'; event.write_text('{}')
                self_outer = self
                class Fake(n.Runner):
                    def preserve(self, path, label):
                        return {}
                    def identity(self, label):
                        return identity
                    def checked(self, name, argv, timeout):
                        if name == 'base-files':
                            self_outer.assertEqual(argv, ['git', 'ls-tree', '-r', '-z', n.BASE_TREE])
                            self_outer.assertNotIn(n.BASE_SOURCE_HEAD, argv)
                            self_outer.assertNotIn(n.REMOTE_BASE_HEAD, argv)
                        path = self.out / (name + '.stdout')
                        text = {'base-files': '', 'dotnet-version': '10.0.403',
                            'rustc-version': 'rustc 1.99.1 (58cf8f9c3 2026-10-13)',
                            'cargo-version': 'cargo 1.99.1 (5f94df478 2026-08-27)'}[name]
                        path.write_text(text)
                    def managed(self):
                        calls.append('csharp')
                        if failure == 'csharp':
                            raise ValueError('compile error')
                    def build_tools(self):
                        calls.append('tools')
                        if failure == 'tools':
                            raise ValueError('tool error')
                        self.env = {}
                    def command(self, name, argv, timeout, env=None):
                        calls.append(name)
                        (self.out / (name + '.stdout')).write_bytes(b'x' * 1482)
                        (self.out / (name + '.stderr')).write_bytes(b'')
                        return dict(exit=0, failure=None, raw_streams_complete=True)
                    def jit(self, name, *args):
                        calls.append(name + '-jit')
                        if failure in [name, name + '-jit']:
                            raise ValueError('product error')
                    def native(self, *args):
                        calls.append('native')
                    def inputs(self, python):
                        path = self.out / 'inputs'; path.mkdir()
                        (path / 'stdout.txt').write_bytes(b'x' * 77)
                        (path / 'markdown_sample.py').write_bytes(
                            (actual_root / 'tests/projects/entries/markdown_sample.py').read_bytes())
                        return path
                def sha(path):
                    path = Path(path)
                    if path.name == 'python':
                        return n.shared.PYTHON_SHA
                    if path.name == 'contract-oracle.stdout':
                        return n.ORACLE_SHA
                    if path.name == 'markdown_sample.py':
                        return n.shared.ENTRY_SHA
                    return 'f' * 64
                env = {'GITHUB_EVENT_PATH': str(event), 'GITHUB_SHA': identity['head'],
                       'PYTHON': str(root / 'python'), 'DOTNET': str(root / 'dotnet')}
                with patch.object(n, 'ROOT', root), patch.object(n, 'FROZEN', {}), \
                     patch.object(n, 'check_context'), patch.object(n, 'sha', sha), \
                     patch.object(n.shutil, 'which', return_value=str(root / 'tool')), \
                     patch.object(n.sys, 'version_info', (3, 12, 13)), patch.dict(os.environ, env):
                    runner = Fake()
                    code = runner.execute()
                self.assertEqual(code, 0 if failure is None else 1)
                if failure in ['csharp', 'tools']:
                    self.assertNotIn('contract-oracle', calls)
                    self.assertEqual(runner.result['stages']['contract-jit']['status'], 'not-run')
                else:
                    self.assertIn('native', calls)
                    self.assertIn('markdown-full-jit', calls)
                    self.assertLess(calls.index('contract-jit'), calls.index('native'))
                    self.assertLess(calls.index('markdown-plain-jit'), calls.index('markdown-full-jit'))


if __name__ == '__main__':
    unittest.main()
