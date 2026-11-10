"""新搜索验收的纯控制测试：全部产品调用mock，不运行CP/编译器/SDK或下载。"""
import copy
import json
import os
from pathlib import Path
import tempfile
import unittest
from unittest.mock import patch
import xml.etree.ElementTree as ET
import text_search as s
import text_search_native as p


class Tests(unittest.TestCase):
    def test_context_exact_title_and_original_event_unchanged(self):
        head = 'a' * 40
        env = dict(GITHUB_ACTIONS='true', GITHUB_EVENT_NAME='push', GITHUB_REF=s.shared.REF,
                   GITHUB_SHA=head, GITHUB_RUN_ATTEMPT='1', RUNNER_ENVIRONMENT='github-hosted',
                   RYUPY_VALIDATION_MODE='text-search')
        event = dict(ref=s.shared.REF, after=head, head_commit=dict(id=head, message=s.DATA['title']))
        original = copy.deepcopy(event)
        s.check_context(env, event)
        self.assertEqual(event, original)
        for title in [s.DATA['title'].upper(), s.DATA['title'] + '\n', s.old.TITLE, s.old.EXPAND_TITLE]:
            with self.assertRaises(ValueError):
                s.check_context(env, dict(event, head_commit=dict(id=head, message=title)))
        for key, value in [('GITHUB_RUN_ATTEMPT', '2'), ('GITHUB_EVENT_NAME', 'workflow_dispatch'),
                           ('RUNNER_ENVIRONMENT', 'self-hosted'), ('RUSTFLAGS', '-A warnings'),
                           ('GITHUB_SHA', 'b' * 40), ('RYUPY_VALIDATION_MODE', '')]:
            with self.assertRaises(ValueError): s.check_context(dict(env, **{key: value}), event)

    def test_frozen_files_and_fact_names_are_real(self):
        import re
        self.assertEqual(len(set(s.DATA['implementation_paths'])), 21)
        self.assertEqual(set(s.DATA['frozen']), set(s.DATA['implementation_paths']) |
                         set(s.old.FROZEN) | set(s.old.EXPAND_FROZEN))
        for path, expected in s.DATA['frozen'].items():
            self.assertEqual(s.sha(s.ROOT / path), expected, path)
        names = []
        for path in (s.ROOT / 'runtime/RyuPy.Runtime.Tests').glob('PyTextSearch*Tests.cs'):
            names += [path.stem + '.' + n for n in re.findall(r'\[Fact\]\s+public void (\w+)\(', path.read_text())]
        self.assertCountEqual(names, s.DATA['facts']); self.assertEqual(len(names), 14)
        for row in s.DATA['fixtures'].values():
            golden = bytes.fromhex(row['stdout_hex'])
            import hashlib
            self.assertEqual(len(golden), row['bytes'])
            self.assertEqual(hashlib.sha256(golden).hexdigest(), row['stdout_sha256'])

    def test_supported_ast_preserves_all_windows_and_legal_assertions(self):
        # 不执行fixture；只将chr常量折叠为CP码点，再比较整棵原AST。
        import ast
        original = ast.parse((s.ROOT / s.DATA['fixtures']['core']['source']).read_text())
        supported = ast.parse((s.ROOT / s.DATA['fixtures']['core_supported']['source']).read_text())
        class Fold(ast.NodeTransformer):
            def visit_Call(self, node):
                node = self.generic_visit(node)
                if isinstance(node.func, ast.Name) and node.func.id == 'chr':
                    self_value = ast.literal_eval(node.args[0])
                    return ast.Constant(chr(self_value))
                return node
            def visit_BinOp(self, node):
                node = self.generic_visit(node)
                if isinstance(node.op, ast.Add) and isinstance(node.left, ast.Constant) and isinstance(node.right, ast.Constant):
                    return ast.Constant(node.left.value + node.right.value)
                return node
        supported = Fold().visit(supported)
        text = next(n for n in original.body if isinstance(n, ast.ClassDef) and n.name == 'Text')
        self.assertEqual([n.name for n in text.body], ['__str__', '__len__'])
        text.body = text.body[:1]
        self.assertEqual(original.body[-1].value.args[0].value, 'text search core')
        self.assertEqual(supported.body[-1].value.args[0].value, 'text search core supported')
        supported.body[-1] = original.body[-1]
        self.assertEqual(ast.dump(original), ast.dump(supported))

    def test_full_core_stays_failed_and_native_budget_stays_eight(self):
        self.assertFalse(s.DATA['fixtures']['core']['native'])
        self.assertEqual(bytes.fromhex(s.DATA['fixtures']['core']['stdout_hex']), b'text search core\n')
        self.assertEqual(1 + len(s.old.EXPAND_CONTRACTS) + sum(
            row.get('native', True) for row in s.DATA['fixtures'].values()), 8)
        with tempfile.TemporaryDirectory() as tmp, patch.object(s, 'ROOT', Path(tmp)):
            runner = s.Runner(); runner.env = {}; runner.oracle = lambda *a: None
            with patch.object(p, 'jit', side_effect=ValueError('original core failure')), patch.object(p, 'native') as native:
                runner.domain('core', Path('core.py'), b'text search core\n', native=False)
            native.assert_not_called()
            self.assertFalse(runner.result['stages']['core-jit']['passed'])
            self.assertEqual(runner.result['stages']['core-jit']['status'], 'failed')
            self.assertNotIn('core-native', runner.result['stages'])
            self.assertFalse(runner.result['full_contract_supported'])
        self.assertEqual(s.DATA['legacy_parent'], '98cdc560427eb047598924f0fad3d7fc13caaabc')
        self.assertEqual(s.DATA['legacy_tree'], '911e9c735ef01ed878a17f9f1953307010a3afa5')
        self.assertIn('fetch-depth: 3', (s.ROOT / '.github/workflows/text-search.yml').read_text())

    def test_trx_requires_exact_all_passed_counter(self):
        with tempfile.TemporaryDirectory() as tmp:
            target = Path(tmp) / 'results.trx'
            def write(names, outcome='Passed'):
                root = ET.Element('TestRun', xmlns='http://microsoft.com/schemas/VisualStudio/TeamTest/2010')
                for name in names:
                    ET.SubElement(root, 'UnitTestResult', testName='RyuPy.Runtime.Tests.' + name, outcome=outcome)
                ET.ElementTree(root).write(target)
            write(s.DATA['facts']); self.assertEqual(s.verify_trx(target)['total'], 14)
            for names in [[], s.DATA['facts'][:-1], s.DATA['facts'] + s.DATA['facts'][:1], ['unexpected']]:
                write(names)
                with self.assertRaises(ValueError): s.verify_trx(target)
            for outcome in ['Skipped', 'Failed']:
                write(s.DATA['facts'], outcome)
                with self.assertRaises(ValueError): s.verify_trx(target)

    def test_source_manifest_rejects_missing_extra_hash_package_and_logical_path(self):
        with tempfile.TemporaryDirectory() as tmp:
            entry = Path(tmp) / 'entry.py'; entry.write_bytes(b'print(1)\n')
            sources = p.source_map(entry, None, s.DATA)
            row = dict(module_id=0, module_name='__main__', compiled_text_sha256=s.sha(entry), package='', logical_source='entry/entry.py')
            p.verify_sources({'sources': [row], 'entry': {'module_id': 0}}, sources)
            for rows in [[], [row, row], [dict(row, module_name='extra')],
                         [dict(row, compiled_text_sha256='0' * 64)], [dict(row, package='bad')],
                         [dict(row, logical_source='modules/entry.py')]]:
                with self.assertRaises(ValueError): p.verify_sources({'sources': rows, 'entry': {'module_id': 0}}, sources)
            with self.assertRaises(ValueError):
                p.verify_sources({'sources': [row], 'entry': {'module_id': 1}}, sources)
            entry.write_bytes(b'changed')
            with self.assertRaises(ValueError): p.verify_sources({'sources': [row], 'entry': {'module_id': 0}}, sources)

    def test_exact_module_policy_and_original_markdown_command(self):
        with tempfile.TemporaryDirectory() as tmp:
            path = Path(tmp) / 'env.json'
            expected = s.DATA['markdown_environment']
            path.write_text(json.dumps(expected)); p.verify_environment(path, s.DATA, {}, True)
            for change in ['session_policy', 'modules']:
                bad = copy.deepcopy(expected)
                bad[change] = 'explicit-only' if change == 'session_policy' else bad[change][:-1]
                path.write_text(json.dumps(bad))
                with self.assertRaises(ValueError): p.verify_environment(path, s.DATA, {}, True)
            runner = object.__new__(s.Runner); runner.cli = Path('/tool/ryupy')
            args = p.build_argv(runner, Path('entry.py'), Path('out'), Path('inputs'), True)
            self.assertEqual(args, s.shared.build_command(runner.cli, Path('entry.py'), Path('inputs'), Path('out')))
            self.assertEqual(len(s.DATA['markdown_sources']), 26)
            self.assertEqual(len(expected['modules']), 27)

    def test_aot_environment_is_not_jit_and_markdown_boundary_stays_failed(self):
        with tempfile.TemporaryDirectory() as tmp:
            path = Path(tmp) / 'env.json'
            sources = {'__main__': {}}
            aot = dict(s.DATA['aot_environment'], modules=s.DATA['aot_environment']['modules'][:1])
            path.write_text(json.dumps(aot)); p.verify_environment(path, s.DATA, sources, False, True)
            with self.assertRaises(ValueError): p.verify_environment(path, s.DATA, sources, False)
            runner = object.__new__(s.Runner); runner.out = Path(tmp); runner.cli = Path('cli')
            runner.env = {}; runner.data = s.DATA; calls = []
            def command(label, argv, timeout, env):
                calls.append(argv)
                self.assertEqual(timeout, 120)
                (runner.out / (label + '.stderr')).write_text('[E0100] 显式实例 AOT 需要资源清单且不支持原生绑定')
                return dict(exit=1, failure=None, raw_streams_complete=True)
            runner.command = command
            with patch.object(p, 'source_map', return_value={'__main__': {}}):
                with self.assertRaisesRegex(ValueError, 'Native未运行'):
                    p.markdown_boundary(runner, 'markdown-plain', Path('entry.py'), Path('inputs'))
            self.assertEqual(len(calls), 1); self.assertIn('--instance-module', calls[0])
            self.assertNotIn('--resources', calls[0]); self.assertIn('--aot', calls[0])
            record = json.loads((Path(tmp) / 'markdown-plain-aot-boundary-classification.json').read_text())
            self.assertTrue(record['known_configuration_boundary_observed'])
            self.assertFalse(record['supported']); self.assertEqual(record['native_original'], 'blocked')

    def test_jit_failure_does_not_skip_native_and_cp_failure_blocks_only_domain(self):
        with tempfile.TemporaryDirectory() as tmp, patch.object(s, 'ROOT', Path(tmp)):
            runner = s.Runner(); runner.env = {}; calls = []
            runner.oracle = lambda *a: calls.append('cp')
            with patch.object(p, 'jit', side_effect=ValueError('real failure')), \
                 patch.object(p, 'native', side_effect=lambda *a: calls.append('native')):
                runner.domain('core', Path('core.py'), b'golden')
            self.assertEqual(calls, ['cp', 'native'])
            self.assertFalse(runner.result['stages']['core-jit']['passed'])
            runner.oracle = lambda *a: (_ for _ in ()).throw(ValueError('CP failure'))
            with patch.object(p, 'jit') as jit, patch.object(p, 'native') as native:
                runner.domain('slots', Path('slots.py'), b'golden')
            jit.assert_not_called(); native.assert_not_called()
            self.assertEqual(runner.result['stages']['slots-native']['status'], 'blocked')

    def test_original_native_failure_blocks_relocation_without_reexecution(self):
        # 不模拟成功manifest：只验证publish失败的必要依赖与独立label目录。
        with tempfile.TemporaryDirectory() as tmp:
            runner = object.__new__(s.Runner); runner.out = Path(tmp); runner.labels = {'outer'}
            runner.data = s.DATA; runner.env = {}; runner.cli = Path('cli')
            entry = Path(tmp) / 'entry.py'; entry.write_bytes(b'pass\n')
            runner.command = lambda *a: dict(exit=1, failure=None)
            runner.preserve_publish_evidence = lambda *a: None
            with patch.object(p.shutil, 'disk_usage', return_value=type('D', (), {'free': 3 * 1024**3})()), \
                 patch.object(s.old, 'relocate_manifest') as move:
                with self.assertRaises(ValueError): p.native(runner, 'core', entry, b'', None, False)
            move.assert_not_called()
            self.assertEqual(runner.out, Path(tmp)); self.assertEqual(runner.labels, {'outer'})
            state = json.loads((Path(tmp) / 'native-domains/search-core/native-stages.json').read_text())
            self.assertEqual(state, dict(publish='failed', validation='blocked', original='blocked', relocated='blocked'))

    def test_native_original_error_preserves_archive_and_blocks_move(self):
        from types import SimpleNamespace
        with tempfile.TemporaryDirectory() as tmp:
            runner = object.__new__(s.Runner); runner.out = Path(tmp); runner.labels = set()
            runner.data = s.DATA; runner.env = {}; runner.cli = Path('cli'); runner.frozen = {}
            entry = Path(tmp) / 'entry.py'; entry.write_bytes(b'pass\n')
            managed = Path(tmp) / 'managed'; managed.mkdir()
            pe = managed / 'entry.dll'; pe.write_bytes(b'PE')
            runner.runtime = managed / 'RyuPy.Runtime.dll'; runner.runtime.write_bytes(b'runtime')
            environment = dict(s.DATA['aot_environment'], modules=s.DATA['aot_environment']['modules'][:1])
            (managed / 'module-environment.json').write_text(json.dumps(environment))
            dist = Path(tmp) / 'dist'; dist.mkdir(); binary = dist / 'app'; binary.write_bytes(b'ELF')
            layout = SimpleNamespace(dist=dist, native=binary, managed=pe, host=Path(tmp))
            manifest = {'entry': {'module_id': 0}, 'sources': [dict(module_id=0, module_name='__main__', compiled_text_sha256=s.sha(entry),
                package='', logical_source='entry/entry.py')], 'compiler': {'options': {
                'module_environment_policy': 'ordinary-dynamic-module-pilot', 'instance_modules': []}},
                'modules': [dict(name='__main__', environment_reason='aot-not-migrated')],
                'build_inputs': [dict(role='runtime_managed_input', sha256=s.sha(runner.runtime)),
                                 dict(role='compiled_managed_input', sha256=s.sha(pe))]}
            def command(*args):
                (runner.out / 'native-publish.stderr').write_bytes(b'layout')
                return dict(exit=0, failure=None)
            runner.command = command; runner.preserve_publish_evidence = lambda *a: None
            calls = []
            def original(*args):
                calls.append(args[0])
                self.assertTrue((runner.out / 'native-dist.tar.gz').is_file())
                raise ValueError('original failed')
            runner.run_positive = original
            def archive(dist, out): (out / 'native-dist.tar.gz').write_bytes(b'archive')
            with patch.object(p.shutil, 'disk_usage', return_value=SimpleNamespace(free=3 * 1024**3)), \
                 patch.object(s.old, 'reported_aot', return_value=layout), \
                 patch.object(s.old, 'preserve_dist', side_effect=archive), \
                 patch.object(s.old, 'read_sdk_version', return_value='10.0.403'), \
                 patch.object(s.old, 'verify_manifest', return_value=manifest), \
                 patch.object(s.old, 'relocate_manifest') as move:
                with self.assertRaisesRegex(ValueError, 'original failed'):
                    p.native(runner, 'core', entry, b'', None, False)
            move.assert_not_called(); self.assertEqual(calls, ['native-original'])
            state = json.loads((Path(tmp) / 'native-domains/search-core/native-stages.json').read_text())
            self.assertEqual(state, dict(publish='passed', validation='passed', original='failed', relocated='blocked'))

    def test_publish_success_validation_failure_keeps_raw_manifest(self):
        from types import SimpleNamespace
        with tempfile.TemporaryDirectory() as tmp:
            runner = object.__new__(s.Runner); runner.out = Path(tmp); runner.labels = set()
            runner.data = s.DATA; runner.env = {}; runner.cli = Path('cli'); runner.frozen = {}
            entry = Path(tmp) / 'entry.py'; entry.write_bytes(b'pass\n')
            def command(*args):
                target = runner.out / 'native-build'; target.mkdir()
                (target / 'ryupy-artifact.json').write_bytes(b'{invalid-original}')
                (runner.out / 'native-publish.stderr').write_bytes(b'invalid layout')
                return dict(exit=0, failure=None)
            runner.command = command; runner.preserve_publish_evidence = lambda *a: None
            with patch.object(p.shutil, 'disk_usage', return_value=SimpleNamespace(free=3 * 1024**3)), \
                 patch.object(s.old, 'reported_aot', side_effect=ValueError('layout failed')):
                with self.assertRaisesRegex(ValueError, 'layout failed'):
                    p.native(runner, 'core', entry, b'', None, False)
            domain = Path(tmp) / 'native-domains/search-core'
            self.assertEqual((domain / 'manifest-raw-0.json').read_bytes(), b'{invalid-original}')
            state = json.loads((domain / 'native-stages.json').read_text())
            self.assertEqual(state, dict(publish='passed', validation='failed', original='blocked', relocated='blocked'))

    def test_identity_failure_invalidates_every_positive_stage(self):
        with tempfile.TemporaryDirectory() as tmp, patch.object(s, 'ROOT', Path(tmp)):
            runner = s.Runner()
            runner.identities = lambda: None; runner.managed = lambda **k: None
            runner.managed_domain = lambda name: None; runner.managed_search = lambda: None
            runner.build_tools = lambda: None
            def products():
                for row in runner.result['stages'].values(): row.update(passed=True, status='passed')
            runner.products = products
            runner.finish_identity = lambda: (_ for _ in ()).throw(ValueError('changed'))
            self.assertEqual(runner.execute(), 1)
            self.assertFalse(runner.result['passed']); self.assertFalse(runner.result['supported_contract_passed'])
            self.assertTrue(all(not row['passed'] for row in runner.result['stages'].values()))

    def test_workflow_limits_and_old_unchanged_callers(self):
        text = (s.ROOT / '.github/workflows/wcwidth-legacy-aot.yml').read_text()
        self.assertLessEqual(len(text.splitlines()), 600)
        block = text.split('  text-search:\n')[1].split('  baseline:\n')[0]
        self.assertIn('needs: select-mode', block)
        self.assertIn('uses: ./.github/workflows/text-search.yml', block)
        called = (s.ROOT / '.github/workflows/text-search.yml').read_text()
        self.assertIn('timeout-minutes: 170', called)
        self.assertNotIn('continue-on-error:', called)
        self.assertIn('persist-credentials: false', called)
        for filename in ['text_search.py', 'text_search_native.py', 'test_text_search.py', 'text_search.json']:
            self.assertLessEqual(len((s.ROOT / 'tests/validation' / filename).read_text().splitlines()), 600)


def legacy_control(local_tree=False):
    """19项检查当前源，唯一候选hash方法检查固定历史公共祖父的15路径；不改原方法。"""
    import subprocess
    import test_typing_noreturn as legacy
    historical = 'test_expand_frozen_oracles_and_unchanged_production'
    names = unittest.defaultTestLoader.getTestCaseNames(legacy.Tests)
    s.require(len(names) == 20 and historical in names, '旧测试集合改变，不能自动迁移其它方法')
    current = unittest.TextTestRunner(verbosity=2).run(unittest.TestSuite(
        legacy.Tests(name) for name in names if name != historical))
    print('legacy-control: 19 current-source methods; one historical identity method follows', flush=True)
    parent = s.DATA['legacy_tree'] if local_tree else s.DATA['legacy_parent']
    tree = subprocess.check_output(['git', 'rev-parse', parent + '^{tree}'], cwd=s.ROOT, text=True).strip()
    s.require(tree == s.DATA['legacy_tree'], '固定历史tree不符')
    with tempfile.TemporaryDirectory() as tmp:
        root = Path(tmp)
        for name, expected in s.old.EXPAND_FROZEN.items():
            raw = subprocess.check_output(['git', 'show', parent + ':' + name], cwd=s.ROOT)
            path = root / name; path.parent.mkdir(parents=True, exist_ok=True); path.write_bytes(raw)
            s.require(s.sha(path) == expected, '历史真实blob不是冻结源：' + name)
        with patch.object(s.old, 'ROOT', root):
            history = unittest.TextTestRunner(verbosity=2).run(legacy.Tests(historical))
    print('legacy-control: historical 15 real git blobs; not current product acceptance', flush=True)
    return 0 if current.wasSuccessful() and history.wasSuccessful() else 1


if __name__ == '__main__':
    import sys
    if sys.argv[1:] in (['--legacy-control'], ['--legacy-control-local-tree']):
        raise SystemExit(legacy_control(local_tree=sys.argv[1] == '--legacy-control-local-tree'))
    unittest.main()
