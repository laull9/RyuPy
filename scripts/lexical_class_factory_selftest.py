"""只做合成原流/临时文本及mock纯测试；禁止CP业务、产品、构建或下载。"""
import ast
import hashlib
import copy
import json
import os
from pathlib import Path
import tempfile
import unittest
from unittest.mock import patch
import lexical_class_factory_contract as c
import lexical_class_factory_validation as gate
import textwrap_dedent_selftest as selector


def expected(exit=0):
    return {'expected_exit': exit, 'stdout': 'body\n', 'stderr': 'NotImplementedError: capability\n' if exit else '',
            'diagnostic': 'NotImplementedError: capability' if exit else ''}


def context():
    env = {'GITHUB_ACTIONS': 'true', 'GITHUB_EVENT_NAME': 'push', 'GITHUB_REF': c.old.shared.REF,
           'GITHUB_REPOSITORY': 'laull9/RyuPy', 'GITHUB_RUN_ATTEMPT': '1', 'GITHUB_SHA': 'a' * 40, 'RUNNER_ENVIRONMENT': 'github-hosted', 'RUNNER_OS': 'Linux', 'RUNNER_ARCH': 'X64'}
    return env, {'repository': {'full_name': 'laull9/RyuPy'}, 'ref': env['GITHUB_REF'], 'after': env['GITHUB_SHA'],
                 'head_commit': {'id': env['GITHUB_SHA'], 'message': c.TITLE}}


class Tests(unittest.TestCase):
    def test_unfrozen_blocks_before_any_work(self):
        data = c.load_manifest(ready=False)
        if not data['frozen']:
            with self.assertRaisesRegex(ValueError, '未冻结'): c.load_manifest()
            with patch.object(Path, 'mkdir', side_effect=AssertionError('不应创建产物')):
                with self.assertRaisesRegex(ValueError, '未冻结'): gate.Runner()
        else:
            self.assertEqual(c.load_manifest(), data)

    def test_no_fabricated_frozen_manifest(self):
        data = c.load_manifest(ready=False)
        empty = dict(data, frozen=True, sources={}, cases=[], facts=[], rust_tests=[])
        with self.assertRaises(ValueError): c.validate(empty)

    def test_metadata_is_accounted_without_weakening_oracle(self):
        data = c.load_manifest(ready=True)
        self.assertEqual(len(data['sources']), 18)
        for case in data['cases']:
            if Path(case['source']).stem in c.SIDECAR_STEMS:
                sidecar = str(Path(case['source']).with_suffix('.case.txt'))
                self.assertEqual(case['support'], [sidecar])
                changed = copy.deepcopy(data)
                next(row for row in changed['cases'] if row['id'] == case['id'])['support'] = []
                with self.assertRaisesRegex(ValueError, 'support合同'):
                    c.validate(changed, c.ROOT)
        extra = copy.deepcopy(data)
        extra['sources'].pop(next(p for p in data['sources'] if p.endswith('.case.txt')))
        with self.assertRaisesRegex(ValueError, '完整源域'):
            c.validate(extra, c.ROOT)

    @patch.object(c, 'verify_test_names')
    def test_frozen_scope_regressions_and_exact_old_authority(self, _names):
        # 惰性临时源码从不执行；旧保护复制真实固定bytes，不制造生产oracle。
        with tempfile.TemporaryDirectory() as temp:
            root = Path(temp)
            for name in c.PROTECTED:
                p = root / name; p.parent.mkdir(parents=True, exist_ok=True)
                p.write_bytes((c.ROOT / name).read_bytes())
            paths = ['tests/e2e/phase3/m3_11_lexical_class_' + suffix + '.py'
                     for suffix in ('positive', 'error', 'ni', 'namespace')]
            for name in paths:
                p = root / name; p.parent.mkdir(parents=True, exist_ok=True)
                p.write_text('# ryupy: pending\n# 合成惰性文本，绝不执行\n')
            cases = []
            for index, kind in enumerate(('positive', 'cp_error', 'ni')):
                cases.append({'id': kind.replace('_', '-'), 'source': paths[index], 'kind': kind,
                    'product_stage': 'compile' if kind == 'ni' else 'run',
                    'cp': expected(1 if kind == 'cp_error' else 0), 'product': expected(0 if kind == 'positive' else 1),
                    'include': [], 'modules': {'__main__': paths[index]}, 'support': [], 'instance_modules': [],
                    'native': 'positive' if kind == 'positive' else 'none', 'native_refusal': None})
            data = dict(c.load_manifest(ready=False), frozen=True,
                sources={n: c.sha(root / n) for n in paths}, cases=cases, facts=['X.Fact'],
                rust_tests=[{'package': 'ryupy-hir', 'name': 'tests::one'}], old_protected=dict(c.PROTECTED),
                pending_only=[{'source': paths[-1], 'reason': c.PENDING_REASON, 'cp': expected()}],
                oracle={'python_sha256': c.old.shared.PYTHON_SHA, 'version': '3.12.13', 'records': {n: {} for n in paths}})
            for case in [*data['cases'], *data['pending_only']]:
                cp = case['cp']
                data['oracle']['records'][case['source']] = {'source_sha256': data['sources'][case['source']],
                    'exit': cp['expected_exit'], 'stdout': {'bytes': len(cp['stdout'].encode()), 'sha256': hashlib.sha256(cp['stdout'].encode()).hexdigest()},
                    'stderr': {'bytes': len(cp['stderr'].encode()), 'sha256': hashlib.sha256(cp['stderr'].encode()).hexdigest()},
                    'normalized_stderr_sha256': hashlib.sha256(cp['stderr'].encode()).hexdigest()}
            c.validate(data, root)
            # 分别检查三类缺失、保护身份缺失/篡改与同域漏项，不能用同一个失败冒领全部检查。
            only_positive = copy.deepcopy(data)
            only_positive['cases'] = only_positive['cases'][:1]
            with self.assertRaisesRegex(ValueError, '三类'): c.validate(only_positive, root)
            unrelated = copy.deepcopy(data); unrelated['old_protected'] = {paths[0]: data['sources'][paths[0]]}
            with self.assertRaisesRegex(ValueError, '保护完整键'): c.validate(unrelated, root)
            missing = copy.deepcopy(data); del missing['old_protected']['tests/projects/entries/markdown_sample.py']
            with self.assertRaisesRegex(ValueError, '保护完整键'): c.validate(missing, root)
            changed = copy.deepcopy(data); changed['old_protected']['tests/projects/lock.tsv'] = '0' * 64
            with self.assertRaisesRegex(ValueError, '保护完整键'): c.validate(changed, root)
            cp_compile = copy.deepcopy(data); cp_compile['cases'][1]['product_stage'] = 'compile'
            with self.assertRaisesRegex(ValueError, '真实run'): c.validate(cp_compile, root)
            omitted = root / 'tests/e2e/phase3/m3_11_lexical_class_omitted.py'; omitted.write_text('# 惰性漏项')
            with self.assertRaisesRegex(ValueError, '完整源域'): c.validate(data, root)
            omitted.unlink()
            wrong = copy.deepcopy(data); wrong['cases'][0]['product']['stdout'] = 'different'
            with self.assertRaises(ValueError): c.validate(wrong, root)
            wrong = copy.deepcopy(data); wrong['cases'].append(dict(cases[0], id='second'))
            with self.assertRaisesRegex(ValueError, 'Native最多'): c.validate(wrong, root)
            wrong = copy.deepcopy(data); wrong['pending_only'][0]['reason'] = 'passed'
            with self.assertRaises(ValueError): c.validate(wrong, root)

    def test_exact_independent_review_counterexample_now_rejected(self):
        # 与独审旧实现accepted反例相同：单positive、任意onehash、遗漏同根源。
        with tempfile.TemporaryDirectory() as temp:
            root = Path(temp); p = root / 'case.py'; p.write_text('# 惰性文本，不执行')
            case = {'id': 'main', 'source': 'case.py', 'kind': 'positive', 'product_stage': 'run',
                'cp': expected(), 'product': expected(), 'include': [], 'modules': {'__main__': 'case.py'},
                'native': 'positive', 'native_refusal': None}
            data = dict(c.load_manifest(ready=False), frozen=True, sources={'case.py': c.sha(p)}, cases=[case],
                facts=['X.Fact'], rust_tests=[{'package': 'ryupy-hir', 'name': 'tests::one'}],
                pending_only=[], old_protected={'case.py': c.sha(p)})
            with self.assertRaisesRegex(ValueError, '保护完整键'): c.validate(data, root)
            (root / 'omitted.py').write_text('# 惰性遗漏，不执行')
            with self.assertRaisesRegex(ValueError, '保护完整键'): c.validate(data, root)

    def test_fixed_project_and_abi_keys_are_not_new_production(self):
        entries = [n for n in c.PROTECTED if n.startswith('tests/projects/entries/')]
        self.assertEqual(len(entries), 20)
        for name in ('tests/projects/lock.tsv', 'tests/projects/manifest.tsv', 'tests/projects/protected.json',
                     'tests/projects/baselines/markdown/stdout.txt', 'tests/aot/wcwidth_ci/trace_state_s1.py',
                     'runtime/RyuPy.Runtime.Tests/PyTraceStateArtifactTests.cs'):
            self.assertIn(name, c.PROTECTED)
        self.assertFalse(any(n.startswith(('crates/', 'runtime/RyuPy.Runtime/')) for n in c.PROTECTED))
        for name, digest in c.PROTECTED.items(): self.assertEqual(c.sha(c.ROOT / name), digest)

    def test_schema_duplicates_and_path_escape(self):
        with self.assertRaises(ValueError): c.parse('{"a":1,"a":2}')
        for path in ('../x', '/x', './x', 'x//y', 'x\\y', ''):
            with self.assertRaises(ValueError): c.relative(path)
        data = c.load_manifest(ready=False)
        with self.assertRaises(ValueError): c.validate(dict(data, unknown=True), ready=False)

    def test_strict_complete_streams_and_expectedexit1(self):
        with tempfile.TemporaryDirectory() as temp:
            prefix = Path(temp) / 'run'
            for code in (0, 1):
                item = expected(code)
                for stream in ('stdout', 'stderr'): prefix.with_suffix('.' + stream).write_text(item[stream])
                good = {'exit': code, 'failure': None, 'raw_streams_complete': True}
                c.verify_output(good, prefix, item)
                for wrong in (-11, -6, 134, 139, 2, None, 1 - code):
                    with self.assertRaises(ValueError): c.verify_output(dict(good, exit=wrong), prefix, item)
                for failure in ('timeout', 'stream-limit', 'spawn-error'):
                    with self.assertRaises(ValueError): c.verify_output(dict(good, failure=failure), prefix, item)
                with self.assertRaises(ValueError): c.verify_output(dict(good, raw_streams_complete=False), prefix, item)
                prefix.with_suffix('.stderr').write_text('some unrelated crash')
                with self.assertRaises(ValueError): c.verify_output(good, prefix, item)

    def test_exit1_requires_concrete_diagnostic(self):
        for item in (dict(expected(1), diagnostic=''), dict(expected(1), diagnostic='different'),
                     dict(expected(1), expected_exit=-1), dict(expected(), expected_exit=True)):
            with self.assertRaises(ValueError): c.expectation(item)

    def test_context_exact_and_no_mutation(self):
        env, event = context(); before = copy.deepcopy(event)
        c.check_context(env, event); self.assertEqual(event, before)
        for title in (c.TITLE.upper(), c.TITLE + '\n', 'ordinary'):
            with self.assertRaises(ValueError): c.check_context(env, dict(event, head_commit=dict(event['head_commit'], message=title)))
        for key, value in [('GITHUB_RUN_ATTEMPT', '2'), ('GITHUB_EVENT_NAME', 'workflow_dispatch'),
                           ('RUNNER_ENVIRONMENT', 'self-hosted'), ('LD_PRELOAD', '/bad')]:
            with self.assertRaises(ValueError): c.check_context(dict(env, **{key: value}), event)

    def test_current_dag_old_fanout_and_byte_protection(self):
        expected_jobs = selector.ORDINARY | {'lexical-class-factory', 'distribution-metadata', 'default-source-deployment'}
        self.assertEqual(selector.select(c.TITLE)[2], expected_jobs)
        for options in ({'event': 'workflow_dispatch'}, {'attempt': '2'}):
            self.assertEqual(selector.select(c.TITLE, **options)[2], selector.ORDINARY)
        for title in (c.TITLE.upper(), c.TITLE + '\n', 'ordinary'):
            self.assertEqual(selector.select(title)[2], selector.ORDINARY)
        _, blocks = selector.jobs()
        for name, digest in selector.DATA['unchanged_job_blocks'].items():
            self.assertEqual(selector.protected_job_digest(name, blocks[name]), digest)
        for name in selector.ORDINARY | {'distribution-metadata', 'default-source-deployment'}:
            self.assertIn('    needs: select-mode\n', blocks[name])
            self.assertNotIn('needs: lexical-class-factory', blocks[name])

    def test_jit_red_does_not_block_safe_native_or_become_passed(self):
        case = {'id': 'one'}
        result = {'stages': {name: {'passed': True} for name in ('identity', 'tools', 'sourceprep', 'one-cp')}}
        result['stages']['one-jit'] = {'passed': False}
        self.assertTrue(gate.native_ready(result, case))
        self.assertFalse(all(row['passed'] for row in result['stages'].values()))
        for required in ('identity', 'tools', 'sourceprep', 'one-cp'):
            bad = copy.deepcopy(result); bad['stages'][required]['passed'] = False
            self.assertFalse(gate.native_ready(bad, case))

    def test_real_resume_preserves_unbound_build_failure_but_invalidates_replacement(self):
        # 完整走load_state/Native编排与真实文件hash barrier；不启动任何子进程。
        import types
        for variant in ('unbound', 'proof-success', 'proof-truncated', 'passed-but-null', 'wrong-path', 'changed-byte', 'wrong-state',
                        'bound-missing', 'bound-wrong-path', 'bound-changed-byte', 'bound-jit-red'):
            with self.subTest(variant=variant), tempfile.TemporaryDirectory() as temp:
                root = Path(temp); out = root / 'evidence'; out.mkdir()
                python = root / 'python'; dotnet = root / 'dotnet'
                python.write_bytes(b'fixed python'); dotnet.write_bytes(b'fixed dotnet')
                event = root / 'event.json'; event.write_text('{}')
                with patch.dict(os.environ, PYTHON=str(python), DOTNET=str(dotnet), GITHUB_EVENT_PATH=str(event)):
                    runner = object.__new__(gate.Runner)
                    runner.data = c.load_manifest(); runner.out = out; runner.inputs_dir = out / 'inputs'
                    runner.python, runner.dotnet, runner.cli, runner.runtime = python, dotnet, None, None
                    runner.before = {'head': 'same-head'}; runner.identity_failed = False
                    runner.labels = {'compiler-build'}; runner.isolation = {}; runner.input_inventories = {}
                    runner.frozen = {str(p): c.sha(p) for p in (python, dotnet)}
                    runner.result = {'passed': False, 'retries': 0, 'default_project_delta': 0,
                        'pending_only': [], 'case_counts': {}, 'stages': {n: {'passed': False, 'status': 'not-run'}
                            for n in gate.stages(runner.data)}}
                    for name in ('identity', 'csharp-build', 'csharp-tests'):
                        runner.result['stages'][name] = {'passed': True, 'status': 'passed'}
                    for name in ('rust-tests', 'tools'):
                        runner.result['stages'][name] = {'passed': False, 'status': 'failed', 'error': 'original build failure'}
                    record = {'argv': ['cargo', 'build', '--locked', '-p', 'ryupy-cli', '--features', c.old.shared.FEATURE],
                              'cwd': str(gate.ROOT), 'exit': 101, 'failure': None, 'raw_streams_complete': True, 'streams': {}}
                    for stream in ('stdout', 'stderr'):
                        p = out / ('compiler-build.' + stream); p.write_bytes(b'' if stream == 'stdout' else b'E0624 E0503')
                        record['streams'][stream] = {'bytes': p.stat().st_size, 'sha256': c.sha(p)}
                    if variant == 'proof-success': record['exit'] = 0
                    if variant == 'proof-truncated': record['raw_streams_complete'] = False
                    c.save(out / 'compiler-build.json', record)
                    runner.environment(); runner.persist()
                    state = c.parse((out / 'state.json').read_bytes())
                    if variant == 'passed-but-null': state['result']['stages']['tools'] = {'passed': True, 'status': 'passed'}
                    if variant == 'wrong-state': state['result']['stages']['sourceprep'] = {'passed': True, 'status': 'passed'}
                    if variant == 'wrong-path': state['tools']['python'] = str(root / 'replacement')
                    if variant == 'changed-byte': python.write_bytes(b'changed')
                    if variant.startswith('bound-'):
                        target = out / 'tools'; target.mkdir(); runner.inputs_dir.mkdir()
                        for name, file in [('cli', 'ryupy'), ('runtime', 'RyuPy.Runtime.dll')]:
                            path = target / file; path.write_bytes(b'compiled')
                            state['tools'][name] = str(path); state['frozen'][str(path)] = c.sha(path)
                        state['input_inventories'] = {str(runner.inputs_dir): {'.': {'type': 'directory'}}}
                        for name in ('tools', 'sourceprep'):
                            state['result']['stages'][name] = {'passed': True, 'status': 'passed'}
                        for case in runner.data['cases']:
                            state['result']['stages'][case['id'] + '-cp'] = {'passed': True, 'status': 'passed'}
                            state['result']['stages'][case['id'] + '-jit'] = {'passed': False, 'status': 'failed'}
                        if variant == 'bound-missing': (target / 'ryupy').unlink()
                        if variant == 'bound-wrong-path': state['tools']['cli'] = str(target / 'other-ryupy')
                        if variant == 'bound-changed-byte': (target / 'ryupy').write_bytes(b'replaced CLI')
                    c.save(out / 'state.json', state)
                    runner.identity = types.MethodType(lambda self, label: self.before, runner)
                    called = []
                    runner.native = lambda case: called.append(case['id']) or {}
                    with patch.object(c, 'check_context'):
                        code = runner.execute('native')
                    self.assertEqual(code, 1)
                    if variant in ('unbound', 'bound-jit-red'):
                        self.assertFalse(runner.identity_failed)
                        self.assertTrue(runner.result['stages']['csharp-tests']['passed'])
                        self.assertEqual(runner.result['stages']['rust-tests']['error'], 'original build failure')
                        self.assertEqual(len(called), 2 if variant == 'bound-jit-red' else 0)
                        if variant == 'unbound':
                            self.assertEqual(runner.result['stages']['fresh-native']['status'], 'blocked')
                            self.assertIn('未执行', runner.result['stages']['fresh-native']['reason'])
                    else:
                        self.assertTrue(runner.identity_failed)
                        self.assertEqual(called, [])
                        self.assertEqual(runner.result['stages']['csharp-tests']['status'], 'invalidated-identity')

    def test_markdown_selector_still_rejects_unknown_job_and_lost_old_chain(self):
        import markdown_preflight as preflight
        path = c.ROOT / '.github/workflows/wcwidth-legacy-aot.yml'
        original = path.read_text()
        test = preflight.PureTests('test_workflow_job_filters_keep_original_chains')
        # 原精确集合不改为子集；任意新增与删除旧链都必须继续失败。
        for raw in (original + '\n  unknown-job:\n    needs: select-mode\n',
                    original.replace('  baseline:\n', '  baseline-lost:\n', 1)):
            old_read = Path.read_text
            def read(p, *args, **kwargs):
                return raw if p == path else old_read(p, *args, **kwargs)
            with patch.object(Path, 'read_text', read), self.assertRaises(AssertionError):
                test.test_workflow_job_filters_keep_original_chains()

    def test_real_workflow_timeouts_permissions_and_checkpoint_order(self):
        raw = (c.ROOT / '.github/workflows/lexical-class-factory.yml').read_text()
        self.assertIn('timeout-minutes: 65', raw)
        self.assertEqual(raw.count('timeout-minutes: 25'), 2)
        self.assertIn('timeout-minutes: 10', raw)
        self.assertIn('permissions:\n  contents: read', raw)
        self.assertNotIn('continue-on-error', raw)
        self.assertIn('!cancelled()', raw)
        self.assertLess(raw.index('Native前保原双流'), raw.index('重核同源后'))
        self.assertLess(raw.index('lexical_class_factory_contract.py'), raw.index('tests/aot/wcwidth_ci/prepare.py'))
        self.assertEqual(raw.count('uses: actions/upload-artifact@'), 3)
        for path in (c.ROOT / '.github/workflows/wcwidth-legacy-aot.yml', c.ROOT / '.github/workflows/lexical-class-factory.yml'):
            self.assertLessEqual(len(path.read_text().splitlines()), 600)

    def test_trace_missing_class_frame_stays_unaligned_and_path_only_maps(self):
        with tempfile.TemporaryDirectory() as temp:
            prefix = Path(temp) / 'trace'
            paths = {'case.py': Path('/verified/input/case.py')}
            full = 'Traceback (most recent call last):\n  File "/verified/input/case.py", line 8, in C\n    fail()\nValueError: body\n'
            expected_trace = c.normalize_trace(full.encode(), paths).decode()
            item = {'expected_exit': 1, 'stdout': '', 'stderr': expected_trace, 'diagnostic': 'ValueError: body'}
            good = {'exit': 1, 'failure': None, 'raw_streams_complete': True}
            prefix.with_suffix('.stdout').write_text(''); prefix.with_suffix('.stderr').write_text(full)
            self.assertTrue(c.compare_cp_error(good, prefix, item, paths)['aligned'])
            missing = 'Traceback (most recent call last):\nValueError: body\n'
            prefix.with_suffix('.stderr').write_text(missing)
            report = c.compare_cp_error(good, prefix, item, paths)
            self.assertFalse(report['aligned']); self.assertEqual(prefix.with_suffix('.stderr').read_text(), missing)
            for bad in (-11, 139, 0):
                with self.assertRaises(ValueError): c.compare_cp_error(dict(good, exit=bad), prefix, item, paths)
            # 未核路径、行号、类帧名、插入符都不可被normalize悄悄改掉。
            self.assertIn(b'line 8, in C', c.normalize_trace(full.encode(), paths))
            self.assertEqual(c.normalize_trace(b'File "/unverified/case.py", line 1', paths), b'File "/unverified/case.py", line 1')

    def test_trace_mapping_never_rewrites_message_or_source_body(self):
        paths = {'case.py': Path('/verified/case.py')}
        raw = (b'Traceback (most recent call last):\n'
               b'  File "/verified/case.py", line 2, in C\n'
               b'    print(\'File "/verified/case.py", line 9, in fake\')\n'
               b'ValueError: File "/verified/case.py", line 9, in fake\n'
               b'  File "/verified/case.py", line 9, in message_continuation\n')
        want = raw.replace(b'  File "/verified/case.py", line 2, in C', b'  File "<source:case.py>", line 2, in C')
        self.assertEqual(c.normalize_trace(raw, paths), want)
        self.assertEqual(c.normalize_trace(b'File "/verified/case.py", line 2, in outside', paths),
                         b'File "/verified/case.py", line 2, in outside')
        # 旧raw.replace会改正文及异常消息；此反例在旧实现断言失败。
        self.assertNotEqual(raw.replace(b'File "/verified/case.py", line ', b'File "<source:case.py>", line '), want)

    def test_native_static_ni_rejects_products_after_preservation(self):
        with tempfile.TemporaryDirectory() as temp:
            root = Path(temp); target = root / 'native-build'; target.mkdir()
            c.reject_native_success_products(target, 'entry')
            nested = target / 'host/bin'; nested.mkdir(parents=True)
            pe = nested / 'entry.dll'; pe.write_bytes(b'preserved synthetic PE')
            prefix = root / 'ni'; prefix.with_suffix('.stdout').write_text('')
            prefix.with_suffix('.stderr').write_text('[E0100] Error: 精确边界\n')
            # 旧负门仅这一步会绿：即使成功PE已存在，exit1+准确诊断仍可通过。
            c.verify_output(dict(exit=1, failure=None, raw_streams_complete=True), prefix,
                dict(expected_exit=1, stdout='', stderr=None, diagnostic='E0100 精确边界'), refusal=True)
            with self.assertRaisesRegex(ValueError, '入口PE/dist'): c.reject_native_success_products(target, 'entry')
            self.assertEqual(pe.read_bytes(), b'preserved synthetic PE')
            pe.unlink(); dist = target / 'entry.dist'; dist.mkdir()
            (dist / 'entry').write_bytes(b'preserved synthetic ELF')
            with self.assertRaisesRegex(ValueError, '入口PE/dist'): c.reject_native_success_products(target, 'entry')
            self.assertTrue((dist / 'entry').exists())
        # 真实runner必须先归档，再拒成功产品，再对诊断；不能只在mock中排序。
        text = (c.ROOT / 'scripts/lexical_class_factory_validation.py').read_text()
        start = text.index("self.barrier('negative-before-publish')")
        block = text[start:text.index('return {', start)]
        self.assertLess(block.index('preserve_before_validation'), block.index('reject_native_success_products'))
        self.assertLess(block.index('reject_native_success_products'), block.index('c.verify_output'))

    def test_ni_is_not_cp_equivalence_and_never_accepts_crash(self):
        with tempfile.TemporaryDirectory() as temp:
            p = Path(temp) / 'ni'; p.with_suffix('.stdout').write_text('')
            item = dict(expected_exit=1, stdout='', stderr=None, diagnostic='E0100 精确边界')
            good = dict(exit=1, failure=None, raw_streams_complete=True)
            p.with_suffix('.stderr').write_text('[E0100] Error: 精确边界\nsource context\n')
            self.assertFalse(c.verify_output(good, p, item, refusal=True)['cp_equivalent'])
            for text in ('[E0100] Error: 其它错误\n', '[E0100] Error: 精确边界\nUnhandled exception\n',
                         '[E0100] Error: 精确边界\n[E0200] Error: second\n'):
                p.with_suffix('.stderr').write_text(text)
                with self.assertRaises(ValueError): c.verify_output(good, p, item, refusal=True)
            with self.assertRaises(ValueError): c.expectation(item)

    def test_multifile_instance_owner_is_explicit_not_inferred(self):
        data = c.load_manifest(ready=False); case = next(x for x in data['cases'] if x['id'] == 'module-owner')
        import types
        runner = types.SimpleNamespace(cli='ryupy', inputs_dir=Path('/inputs'))
        argv = c.build_argv(runner, case, Path('/out'))
        self.assertEqual(argv[-2:], ['--instance-module', 'owner'])
        self.assertIn('--include-module', argv)
        with tempfile.TemporaryDirectory() as temp:
            path = Path(temp) / 'plan.json'
            plan = {'compiled_feature': True, 'session_policy': 'ordinary-dynamic-module-pilot', 'modules': [
                {'module': n, 'compiled': True, 'execution': 'InstanceGlobals' if n == 'owner' else 'LegacySingleton',
                 'reason': 'explicit-instance' if n == 'owner' else 'entry-or-package'} for n in case['modules']]}
            path.write_text(json.dumps(plan)); c.verify_plan(path, case)
            plan['modules'][-1]['reason'] = 'fallback'; path.write_text(json.dumps(plan))
            with self.assertRaises(ValueError): c.verify_plan(path, case)

    def test_real_manifest_names_and_frozen_candidate_validation(self):
        data = c.load_manifest(ready=False)
        if data['cases']:
            c.verify_test_names(data, c.ROOT)
            c.validate(dict(data, frozen=True))
            self.assertEqual(len(data['oracle']['records']), 14)
            self.assertEqual(len(data['cases']), 13)

    def test_python_sources_parse_without_cache(self):
        for name in ('contract', 'validation', 'selftest'):
            path = c.ROOT / ('scripts/lexical_class_factory_' + name + '.py')
            ast.parse(path.read_text(), filename=str(path))
            self.assertLessEqual(len(path.read_text().splitlines()), 600)

    def test_trx_rejects_zero_skip_and_wrong_fact(self):
        with tempfile.TemporaryDirectory() as temp:
            p = Path(temp) / 'test.trx'
            prefix = '<TestRun xmlns="http://microsoft.com/schemas/VisualStudio/TeamTest/2010"><Results>'
            suffix = '</Results></TestRun>'
            for outcome in ('Passed', 'NotExecuted', 'Failed'):
                p.write_text(prefix + '<UnitTestResult testName="X.Fact" outcome="' + outcome + '"/>' + suffix)
                if outcome == 'Passed': c.verify_trx(p, ['X.Fact'])
                else:
                    with self.assertRaises(ValueError): c.verify_trx(p, ['X.Fact'])
                with self.assertRaises(ValueError): c.verify_trx(p, ['Other.Fact'])
            p.write_text(prefix + suffix)
            with self.assertRaises(ValueError): c.verify_trx(p, [])


if __name__ == '__main__':
    unittest.main()
