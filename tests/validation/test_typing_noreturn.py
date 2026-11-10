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
        self.assertEqual(len(n.FROZEN), 15)
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
        self.assertIn("RYUPY_VALIDATION_MODE:", called)
        self.assertIn("&& 'text-expandtabs' || ''", called)
        self.assertIn('&& 120 || 65', called)
        self.assertEqual(called.count('&& 98 || 65'), 1)
        self.assertEqual(called.count('&& 10 || 65'), 1)
        self.assertEqual(called.count('&& 1 || 65'), 8)
        self.assertLessEqual(98 + 10 + 8, 120)
        self.assertEqual(called.count('if: always()'), 3)
        self.assertEqual(called.count('if: "always() &&'), 3)
        self.assertEqual(called.count('if-no-files-found: error'), 2)
        self.assertIn('!target/typing-noreturn-evidence/native-build/**', called)
        self.assertEqual(called.count('if-no-files-found: warn'), 4)
        self.assertNotIn('continue-on-error:', called)
        uploads = called.split('uses: actions/upload-artifact@')[1:]
        self.assertEqual(len(uploads), 6)
        paths = [re.search(r'^          path: (.*?)(?=^          (?:#|if-no-files-found))',
                           block, re.M | re.S)[1].strip() for block in uploads]
        self.assertEqual(paths[1], 'target/typing-noreturn-evidence/tools')
        self.assertEqual(paths[2], 'target/typing-noreturn-evidence/native-dist.tar.gz')
        self.assertEqual(paths[3:], ['target/typing-noreturn-evidence/native-domains/' + name +
                                   '/native-dist.tar.gz' for name in n.EXPAND_CONTRACTS])
        self.assertEqual(paths[0].splitlines()[0], '|')
        self.assertEqual([line.strip() for line in paths[0].splitlines()[1:]], [
            'target/typing-noreturn-evidence', '!target/typing-noreturn-evidence/tools/**',
            '!target/typing-noreturn-evidence/native-dist.tar.gz',
            '!target/typing-noreturn-evidence/native-build/**',
            '!target/typing-noreturn-evidence/relocated/**',
            '!target/typing-noreturn-evidence/native-domains/*/native-build/**',
            '!target/typing-noreturn-evidence/native-domains/*/relocated/**',
            '!target/typing-noreturn-evidence/native-domains/*/native-dist.tar.gz',
            'target/typing-noreturn-prepare',
            '${{ runner.temp }}/wcwidth-aot-tools/download-identity.json'])

        for text in [main, called]:
            self.assertLessEqual(len(text.splitlines()), 600)

    def test_base_tree_is_frozen_public_parent_tree_not_private_history(self):
        self.assertEqual(n.BASE_TREE, 'e5a1e790d0bbf8a1c0ce3dff27a2771a8fae0944')
        self.assertEqual(n.REMOTE_BASE_HEAD, '1e263ff824ef8f50d2559e1562ff3d71b17db228')
        self.assertEqual(n.BASE_SOURCE_HEAD, 'b0aee23f15ed1662039756bd60eef1d32e9e00c8')
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
                runner.jit('supported', Path('/fake/contract.py'), b'golden')
            self.assertEqual([x[0] for x in calls], ['supported-compile'])
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

    def test_boundary_jit_skips_cp_and_preserves_actual_pe_before_refusal(self):
        with tempfile.TemporaryDirectory() as tmp:
            runner = object.__new__(n.Runner)
            runner.out = Path(tmp); runner.env = {}; runner.frozen = {}
            runner.cli = Path('/fake/ryupy'); runner.dotnet = Path('/fake/dotnet')
            runner.runtime = runner.out / 'runtime.dll'; runner.runtime.write_bytes(b'runtime')
            entry = Path('/fake/m3_7_typing_noreturn.py')
            calls = []
            def command(name, argv, timeout, env):
                calls.append(name)
                target = Path(argv[argv.index('--out-dir') + 1]); target.mkdir(parents=True)
                (target / (entry.stem + '.dll')).write_bytes(b'actual-pe')
                (target / 'RyuPy.Runtime.dll').write_bytes(b'runtime')
                return dict(exit=0, failure=None)
            def product(name, argv, env):
                calls.append(name)
                saved = runner.out / 'preserved-pe/full-source-deletion-boundary'
                self.assertEqual((saved / (entry.stem + '.dll')).read_bytes(), b'actual-pe')
                return dict(exit=1, failure=None, raw_streams_complete=True)
            runner.command = command; runner.run_product = product
            runner.run_positive = lambda *args: self.fail('原完整边界不得新跑CP或按positive执行')
            with patch.object(n, 'expected_refusal', return_value=True) as verify:
                result = runner.jit('full-source-deletion-boundary', entry, None)
                self.assertEqual(verify.call_count, 1)
            self.assertEqual(calls, ['full-source-deletion-boundary-compile', 'full-source-deletion-boundary-jit'])
            self.assertEqual(result, {'status': 'expected_refusal', 'cp_equivalent': False, 'supported': False})

    def test_deletion_boundary_requires_exact_original_streams_and_exit_one(self):
        # run42754725069同一原源的实际原流；只测试判定器，不执行CP或产品。
        stdout = b'True typing.NoReturn True\nNoReturn NoReturn typing None ()\ncall TypeError Cannot instantiate typing.NoReturn\nexplicit-call TypeError Cannot instantiate typing.NoReturn\nsubscribe TypeError typing.NoReturn is not subscriptable\ninstance TypeError typing.NoReturn cannot be used with isinstance()\nsubclass TypeError typing.NoReturn cannot be used with issubclass()\nbase TypeError Cannot subclass typing.NoReturn\nList True True\nSet True True\nFrozenSet True True\nIterable True True\nIterator True True\nSequence True True\nCollection True True\nContainer True True\nDict True\nMapping True\nMutableMapping True\nTrue typing.Optional[typing.NoReturn] typing.ClassVar[typing.NoReturn]\ntyping.Tuple[typing.NoReturn, ...] typing.Callable[[typing.NoReturn], typing.NoReturn]\nTrue\nTrue\nTrue\nTrue True\ntyping.Union[typing.NoReturn, int] typing.Union[int, typing.NoReturn]\n'
        stderr = b'Traceback (most recent call last):\n  File "m3_7_typing_noreturn.py", line 36, in <module>\nNotImplementedError: module attribute deletion requires global-slot synchronization\n'
        with tempfile.TemporaryDirectory() as tmp:
            prefix = Path(tmp) / 'boundary'
            out, err = prefix.with_suffix('.stdout'), prefix.with_suffix('.stderr')
            out.write_bytes(stdout); err.write_bytes(stderr)
            good = dict(exit=1, failure=None, raw_streams_complete=True)
            self.assertTrue(n.expected_refusal(good, prefix))
            for change in [dict(exit=0), dict(exit=2), dict(exit=-9), dict(failure='timeout'),
                           dict(raw_streams_complete=False)]:
                self.assertFalse(n.expected_refusal(dict(good, **change), prefix))
            for bad in [stdout[:-1], stdout + b'x', b'x' * len(stdout)]:
                out.write_bytes(bad)
                self.assertFalse(n.expected_refusal(good, prefix))
            out.write_bytes(stdout)
            for bad in [stderr[:-1], stderr.replace(b'line 36', b'line 37'), b'x' * len(stderr)]:
                err.write_bytes(bad)
                self.assertFalse(n.expected_refusal(good, prefix))

    def test_orchestration_early_stop_and_independent_product_failures(self):
        # 所有命令都是内存mock；专门覆盖早门停链与full失败仍非零。
        actual_root = n.ROOT
        for failure in ['csharp', 'tools', 'supported-oracle', 'supported-jit', 'supported-native',
                        'full-source-deletion-boundary', 'markdown-plain', 'markdown-full', None]:
            with self.subTest(failure=failure), tempfile.TemporaryDirectory() as tmp:
                root = Path(tmp)
                calls = []
                for name in (n.CONTRACT, n.SUPPORTED):
                    path = root / name; path.parent.mkdir(parents=True, exist_ok=True)
                    path.write_bytes((actual_root / name).read_bytes())
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
                        (self.out / (name + '.stdout')).write_bytes(b'x' * 1401)
                        (self.out / (name + '.stderr')).write_bytes(b'')
                        return dict(exit=1 if failure == name else 0, failure=None, raw_streams_complete=True)
                    def jit(self, name, *args):
                        calls.append(name + '-jit')
                        if failure in [name, name + '-jit']:
                            raise ValueError('product error')
                        if name == 'full-source-deletion-boundary':
                            return {'status': 'expected_refusal', 'cp_equivalent': False, 'supported': False}
                    def native(self, entry, golden):
                        self_outer.assertEqual(entry, root / n.SUPPORTED)
                        self_outer.assertEqual(len(golden), 1401)
                        calls.append('native')
                        if failure == 'supported-native':
                            raise ValueError('native error')
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
                    if path.name == 'supported-oracle.stdout':
                        return n.SUPPORTED_ORACLE_SHA
                    if path.name == 'markdown_sample.py':
                        return n.shared.ENTRY_SHA
                    return 'f' * 64
                env = {'GITHUB_EVENT_PATH': str(event), 'GITHUB_SHA': identity['head'],
                       'PYTHON': str(root / 'python'), 'DOTNET': str(root / 'dotnet')}
                with patch.object(n, 'ROOT', root), patch.object(n, 'FROZEN', {n.CONTRACT: 'f' * 64}), \
                     patch.object(n, 'check_context'), patch.object(n, 'sha', sha), \
                     patch.object(n.shutil, 'which', return_value=str(root / 'tool')), \
                     patch.object(n.sys, 'version_info', (3, 12, 13)), patch.dict(os.environ, env):
                    runner = Fake()
                    code = runner.execute()
                self.assertEqual(code, 0 if failure is None else 1)
                if failure in ['csharp', 'tools']:
                    self.assertNotIn('supported-oracle', calls)
                    self.assertEqual(runner.result['stages']['supported-jit']['status'], 'not-run')
                else:
                    if failure != 'supported-oracle':
                        self.assertEqual(calls.count('native'), 1)
                        self.assertLess(calls.index('supported-jit'), calls.index('native'))
                    else:
                        self.assertNotIn('native', calls)
                    self.assertIn('markdown-full-jit', calls)
                    self.assertIn('full-source-deletion-boundary-jit', calls)
                    self.assertNotIn('contract-oracle', calls)
                    self.assertFalse(runner.result['full_contract_supported'])
                    if failure in ('markdown-plain', 'markdown-full', 'full-source-deletion-boundary', None):
                        self.assertTrue(runner.result['supported_contract_passed'])
                    else:
                        self.assertFalse(runner.result['supported_contract_passed'])
                    if failure != 'full-source-deletion-boundary':
                        boundary = runner.result['stages']['full-source-deletion-boundary']
                        self.assertEqual(boundary['status'], 'expected_refusal')
                        self.assertFalse(boundary['cp_equivalent'])
                    self.assertLess(calls.index('markdown-plain-jit'), calls.index('markdown-full-jit'))

    def test_expand_frozen_oracles_and_unchanged_production(self):
        import hashlib
        self.assertEqual(set(n.EXPAND_CONTRACTS), {'core', 'slots', 'warning_contract'})
        self.assertEqual(len(n.EXPAND_FROZEN), 15)
        for path, digest in n.EXPAND_FROZEN.items():
            self.assertEqual(n.sha(n.ROOT / path), digest)
        for name, (source, size, digest, encoded) in n.EXPAND_CONTRACTS.items():
            self.assertEqual(n.sha(n.ROOT / ('tests/fixtures/text_expandtabs/' + name + '.py')), source)
            golden = bytes.fromhex(encoded)
            self.assertEqual(len(golden), size)
            self.assertEqual(hashlib.sha256(golden).hexdigest(), digest)
        self.assertEqual([x[1] for x in n.EXPAND_CONTRACTS.values()], [47, 17, 20])
        self.assertEqual(n.EXPAND_PARENT, 'f306cae57860d4d8e46ed288984b8b7eafd39830')
        self.assertEqual(n.EXPAND_TREE, '989b15a877c7fdbda6dcc6383ede93d1f66caa60')

    def test_expand_domains_continue_without_repeating_or_joining_sources(self):
        for failure in ['core-cp', 'slots-jit', 'warning_contract-native', None]:
            with tempfile.TemporaryDirectory() as tmp:
                runner = object.__new__(n.Runner)
                runner.out = Path(tmp); runner.result = {'stages': {}}
                calls = []
                def cp(name):
                    calls.append(name + '-cp')
                    if failure == name + '-cp':
                        raise ValueError('CP原失败')
                def jit(name, entry, golden):
                    calls.append(entry.stem + '-jit')
                    self.assertEqual(golden, bytes.fromhex(n.EXPAND_CONTRACTS[entry.stem][3]))
                    if failure == entry.stem + '-jit':
                        raise ValueError('JIT原失败')
                def native(entry, golden, *, name, timeout):
                    calls.append(name + '-native')
                    self.assertEqual(name, entry.stem)
                    self.assertEqual(timeout, 180)
                    if failure == name + '-native':
                        raise ValueError('Native原失败')
                runner.expand_oracle, runner.jit, runner.native = cp, jit, native
                runner.expand_products()
                self.assertEqual(len(calls), len(set(calls)))
                self.assertIn('warning_contract-native', calls)
                self.assertEqual('core-native' in calls, failure != 'core-cp')
                self.assertEqual(runner.product_timeout, 30)

    def test_native_named_scope_restores_defaults_and_keeps_four_archives_separate(self):
        with tempfile.TemporaryDirectory() as tmp:
            runner = object.__new__(n.Runner)
            root = runner.out = Path(tmp); runner.labels = {'old'}
            calls = []
            def native(entry, golden, timeout):
                calls.append((runner.out, timeout, runner.product_timeout))
                (runner.out / 'native-dist.tar.gz').write_bytes(entry.name.encode())
            runner._native = native
            runner.product_timeout = 30
            runner.native(Path('supported.py'), b'original')
            for name in n.EXPAND_CONTRACTS:
                runner.native(Path(name + '.py'), b'golden', name=name, timeout=180)
                self.assertEqual(runner.out, root)
                self.assertEqual(runner.labels, {'old'})
                self.assertEqual(runner.product_timeout, 30)
            self.assertEqual([x[1:] for x in calls], [(1200, 30)] + [(180, 15)] * 3)
            self.assertEqual(len(list(root.rglob('native-dist.tar.gz'))), 4)
            runner._native = lambda *args: (_ for _ in ()).throw(ValueError('publish失败'))
            with self.assertRaises(ValueError):
                runner.native(Path('failed.py'), b'', name='failed', timeout=180)
            self.assertEqual(runner.out, root)
            self.assertEqual(runner.labels, {'old'})

    def test_expand_cp_uses_only_bounded_direct_source_and_preserves_raw(self):
        with tempfile.TemporaryDirectory() as tmp:
            runner = object.__new__(n.Runner)
            runner.out = Path(tmp); runner.labels = set(); runner.python = Path('/fake/python')
            runner.env = {'DOTNET': '/must/not/be/used'}
            name = 'core'; digest = n.EXPAND_CONTRACTS[name][0]
            def capture(prefix, argv, cwd, timeout, env, *, limit, memory_limit):
                self.assertEqual(argv, [runner.python, '-B', n.ROOT / 'tests/fixtures/text_expandtabs/core.py'])
                self.assertEqual((timeout, limit, memory_limit), (15, 131072, 536870912))
                self.assertNotIn('DOTNET', env)
                prefix.with_suffix('.stdout').write_bytes(bytes.fromhex(n.EXPAND_CONTRACTS[name][3]))
                prefix.with_suffix('.stderr').write_bytes(b'')
                return dict(exit=0, failure=None, raw_streams_complete=True)
            original_sha = n.sha
            def sha(path):
                return n.shared.PYTHON_SHA if Path(path) == runner.python else original_sha(path)
            with patch.object(n.shared, 'capture', capture), patch.object(n, 'sha', sha):
                runner.expand_oracle(name)
                with self.assertRaises(ValueError):
                    runner.expand_oracle(name)

    def test_expand_full_orchestration_independent_failures_and_parent_only(self):
        actual_root = n.ROOT
        for failure in ['csharp-build', 'csharp-typing', 'csharp-expandtabs', 'tools',
                        'supported-cp', 'supported-jit', 'supported-native',
                        'full-source-deletion-boundary', 'expandtabs', 'markdown-full',
                        'after-identity', 'frozen-hash', None]:
            with self.subTest(failure=failure), tempfile.TemporaryDirectory() as tmp:
                root = Path(tmp); identity = {'head': '1' * 40, 'tree': '2' * 40, 'files': {}}
                for name in (n.CONTRACT, n.SUPPORTED, *('tests/fixtures/text_expandtabs/' + x + '.py' for x in n.EXPAND_CONTRACTS)):
                    path = root / name; path.parent.mkdir(parents=True, exist_ok=True)
                    path.write_bytes((actual_root / name).read_bytes())
                event = root / 'event'; event.write_text(json.dumps({'head_commit': {'message': n.EXPAND_TITLE}}))
                calls = []; outer = self
                after = []
                class Fake(n.Runner):
                    def identity(self, label):
                        if label == 'after':
                            after.append(True)
                            if failure == 'after-identity':
                                return dict(identity, tree='changed')
                        return identity
                    def preserve(self, path, label):
                        return {}
                    def checked(self, name, argv, timeout):
                        if name == 'direct-parent':
                            outer.assertEqual(argv, ['git', 'rev-parse', 'HEAD^', 'HEAD^{tree}', 'HEAD^^{tree}'])
                        values = {'direct-parent': n.EXPAND_PARENT + '\n' + identity['tree'] + '\n' + n.EXPAND_TREE,
                            'expand-base-files': '', 'dotnet-version': '10.0.403',
                            'rustc-version': 'rustc 1.99.1 (58cf8f9c3 2026-10-13)',
                            'cargo-version': 'cargo 1.99.1 (5f94df478 2026-08-27)'}
                        (self.out / (name + '.stdout')).write_text(values[name])
                    def operation(self, name):
                        calls.append(name)
                        if name == failure:
                            raise ValueError('mock失败')
                    def managed(self, *, build_only=False):
                        outer.assertTrue(build_only); self.operation('csharp-build')
                    def managed_domain(self, name):
                        self.operation('csharp-' + name)
                    def build_tools(self):
                        self.operation('tools')
                    def supported_oracle(self):
                        self.operation('supported-cp')
                        (self.out / 'supported-oracle.stdout').write_bytes(b'x' * 1401)
                    def jit(self, name, *args):
                        self.operation(name + '-jit' if name == 'supported' else name)
                    def native(self, *args):
                        self.operation('supported-native')
                    def expand_products(self):
                        self.phase('expandtabs-core-cp', lambda: self.operation('expandtabs'))
                        for key in self.result['stages']:
                            if key.startswith('expandtabs-') and key != 'expandtabs-core-cp':
                                self.phase(key, lambda: None)
                    def markdown_products(self):
                        self.phase('markdown-plain', lambda: self.operation('markdown-plain'))
                        self.phase('markdown-full', lambda: self.operation('markdown-full'))
                env = {'GITHUB_EVENT_PATH': str(event), 'GITHUB_SHA': identity['head'],
                       'PYTHON': str(root / 'python'), 'DOTNET': str(root / 'dotnet')}
                with patch.object(n, 'ROOT', root), patch.object(n, 'FROZEN', {n.CONTRACT: n.shared.PYTHON_SHA}), \
                     patch.object(n, 'EXPAND_FROZEN', {}), patch.object(n, 'check_context'), \
                     patch.object(n, 'sha', side_effect=lambda path:
                         '0' * 64 if failure == 'frozen-hash' and after else n.shared.PYTHON_SHA), \
                     patch.object(n.shutil, 'which', return_value='/fake/tool'), \
                     patch.object(n.sys, 'version_info', (3, 12, 13)), patch.dict(os.environ, env):
                    runner = Fake(expandtabs=True); code = runner.execute()
                self.assertEqual(code, 0 if failure is None else 1)
                if failure in ('csharp-build', 'tools'):
                    self.assertNotIn('supported-cp', calls)
                else:
                    self.assertIn('markdown-full', calls)
                    self.assertIn('full-source-deletion-boundary', calls)
                    self.assertIn('expandtabs', calls)
                    self.assertEqual('supported-native' in calls, failure != 'supported-cp')
                if failure != 'csharp-build':
                    self.assertIn('csharp-typing', calls)
                    self.assertIn('csharp-expandtabs', calls)
                self.assertFalse(runner.result['full_contract_supported'])
                if failure in ('after-identity', 'frozen-hash'):
                    self.assertFalse(runner.result['passed'])
                    self.assertFalse(runner.result['identity_preserved'])
                    self.assertFalse(runner.result['supported_contract_passed'])
                    self.assertTrue(runner.result['stages']['supported-native']['passed'])


    def test_capture_cp_limits_do_not_leak_into_default_dotnet_calls(self):
        from unittest.mock import MagicMock
        with tempfile.TemporaryDirectory() as tmp:
            root = Path(tmp)
            for memory in [None, 536870912]:
                resource = MagicMock(); process = MagicMock(); process.poll.return_value = 0
                process.returncode = 0
                def popen(*args, **kwargs):
                    before = kwargs['preexec_fn']
                    if memory is None:
                        self.assertIsNone(before)
                    else:
                        before()
                        self.assertEqual(resource.setrlimit.call_count, 2)
                        resource.setrlimit.assert_any_call(resource.RLIMIT_AS, (memory, memory))
                        resource.setrlimit.assert_any_call(resource.RLIMIT_FSIZE, (131072, 131072))
                    return process
                with patch.dict('sys.modules', resource=resource), patch.object(n.shared.subprocess, 'Popen', popen):
                    record = n.shared.capture(root / str(memory), ['/fake/tool'], root, 15,
                                              limit=131072, memory_limit=memory)
                self.assertEqual(record['exit'], 0)
                self.assertEqual(record['memory_limit'], memory)
                self.assertTrue(record['raw_streams_complete'])
                if memory is None:
                    resource.setrlimit.assert_not_called()

    def test_expand_trx_requires_all_eighteen_distinct_facts(self):
        with tempfile.TemporaryDirectory() as tmp:
            runner = object.__new__(n.Runner); runner.out = Path(tmp); runner.dotnet = Path('/fake/dotnet')
            names = sorted(n.EXPAND_FACTS)
            for items, outcome in [(names, 'Passed'), (names[:-1], 'Passed'),
                                   (names + names[:1], 'Passed'), (names, 'Failed'), (names, 'NotExecuted')]:
                def checked(*args):
                    target = runner.out / 'trx-expandtabs'; target.mkdir(exist_ok=True)
                    root = ET.Element('TestRun')
                    for name in items:
                        ET.SubElement(root, '{http://microsoft.com/schemas/VisualStudio/TeamTest/2010}UnitTestResult',
                                      testName='RyuPy.Runtime.Tests.' + name, outcome=outcome)
                    ET.ElementTree(root).write(target / 'result.trx')
                runner.checked = checked
                if items == names and outcome == 'Passed':
                    self.assertEqual(runner.managed_domain('expandtabs')['total'], 18)
                else:
                    with self.assertRaises(ValueError):
                        runner.managed_domain('expandtabs')


if __name__ == '__main__':
    unittest.main()
