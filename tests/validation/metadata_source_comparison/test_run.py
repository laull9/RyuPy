#!/usr/bin/env python3
"""控制器与实际workflow selector纯自测；所有产品调用均mock，不下载/构建/运行产品。"""
import copy
import json
import os
from pathlib import Path
import tempfile
import unittest
from unittest.mock import patch
import run as d

OK = {'exit': 0, 'failure': None, 'raw_streams_complete': True}
HEAD = '1' * 40


class Tests(unittest.TestCase):
    def test_context_exact(self):
        env = dict(GITHUB_ACTIONS='true', RUNNER_ENVIRONMENT='github-hosted', GITHUB_EVENT_NAME='push',
                   GITHUB_RUN_ATTEMPT='1', GITHUB_REF=d.REF, GITHUB_SHA=HEAD)
        event = {'ref': d.REF, 'after': HEAD, 'head_commit': {'message': d.TITLE, 'id': HEAD}}
        d.context(env, event)
        for key, value in [('GITHUB_EVENT_NAME', 'workflow_dispatch'), ('GITHUB_RUN_ATTEMPT', '2'),
                           ('GITHUB_REF', d.REF.upper()), ('RUNNER_ENVIRONMENT', 'self-hosted'),
                           ('RUSTFLAGS', '-O'), ('CARGO_TARGET_DIR', '/tmp/mixed')]:
            with self.subTest(key=key), self.assertRaises(ValueError): d.context(dict(env, **{key: value}), event)
        for title in (d.TITLE.upper(), d.TITLE + '\n', 'normal commit', None):
            changed = copy.deepcopy(event); changed['head_commit']['message'] = title
            with self.assertRaises(ValueError): d.context(env, changed)

    def test_actual_workflow_selector_all_branches(self):
        text = (d.ROOT / '.github/retired-workflows/metadata-source-comparison.yml').read_text()
        raw = text.split("          python3 - <<'PY'\n", 1)[1].split('\n          PY', 1)[0]
        code = '\n'.join(line[10:] for line in raw.splitlines())
        base = {'ref': d.REF, 'after': HEAD, 'head_commit': {'message': d.TITLE, 'id': HEAD}}
        env = dict(GITHUB_EVENT_NAME='push', GITHUB_REF=d.REF, GITHUB_RUN_ATTEMPT='1', GITHUB_SHA=HEAD)
        cases = [(base, env, True)]
        for title in (d.TITLE.upper(), d.TITLE + '\n', 'normal', None):
            event = copy.deepcopy(base); event['head_commit']['message'] = title
            cases.append((event, env, False))
        for key, value in [('GITHUB_EVENT_NAME', 'workflow_dispatch'), ('GITHUB_EVENT_NAME', 'pull_request'),
                           ('GITHUB_RUN_ATTEMPT', '2'), ('GITHUB_REF', 'refs/heads/main'),
                           ('GITHUB_REF', d.REF.upper()), ('GITHUB_SHA', '2' * 40)]:
            cases.append((base, dict(env, **{key: value}), False))
        for field in ('after', 'ref', 'head_commit'):
            event = dict(base); del event[field]; cases.append((event, env, False))
        with tempfile.TemporaryDirectory() as temp:
            p = Path(temp)
            for event, current, expected in cases:
                (p / 'event').write_text(json.dumps(event)); (p / 'out').write_text('')
                with patch.dict(os.environ, dict(current, GITHUB_EVENT_PATH=str(p / 'event'), GITHUB_OUTPUT=str(p / 'out')), clear=True):
                    exec(compile(code, 'actual-workflow-selector', 'exec'), {})
                self.assertEqual((p / 'out').read_text(), 'run=' + str(expected).lower() + '\n')
        self.assertNotIn('${{', raw)
        self.assertEqual(text.count('prepare.py >'), 1)
        self.assertNotIn('workflow_dispatch:', text)
        self.assertNotIn('uses: ./.github/workflows/', text)

    def test_old_workflows_do_not_trigger_on_diagnostic_branch(self):
        # 直接审当前触发块；没有新增宽泛push或由新workflow调用旧门。
        for file in (d.ROOT / '.github/retired-workflows').glob('*.yml'):
            if file.name == 'metadata-source-comparison.yml': continue
            block = file.read_text().split('jobs:', 1)[0]
            self.assertNotIn('ci/metadata-source-comparison-001', block)
            if 'push:' in block:
                self.assertTrue(any(x in block for x in ('branches: [main]', 'branches: [wip/aot-jit-recovery]',
                    'branches: [ci/wcwidth-legacy-aot-2909056]')), file.name)

    def test_fixture_and_frozen_contracts(self):
        self.assertEqual(d.shared.snapshot(d.ROOT / d.FIXTURE), d.FROZEN['fixtures'])
        for name, digest in d.FROZEN['sources']['B']['contracts'].items():
            self.assertEqual(d.sha(d.ROOT / name), digest)
        self.assertEqual(d.FROZEN['sources']['A']['contracts']['Cargo.lock'], d.FROZEN['sources']['B']['contracts']['Cargo.lock'])

    def test_original_argv_and_different_search_domains(self):
        f, out = Path('/fresh/fixtures'), Path('/fresh/product')
        a = d.commands('/tools/ryupy', f, out, 'synthetic')
        b = d.commands('/tools/ryupy', f, out, 'missing-include')
        self.assertIn('--include-module', a); self.assertNotIn('--include-module', b)
        self.assertEqual(a[-2:], ['--distribution-metadata', f / 'request.json'])
        self.assertIn(f, b)
        for name in d.DOMAINS:
            argv, env = d.cp_command('/fixed/python', f, name)
            self.assertEqual(argv[1:4], ['-S', '-B', '-c'])
            self.assertEqual(env['PYTHONPATH'], '/fresh/fixtures:' + '/fresh/fixtures/root-a:/fresh/fixtures/root-b'
                             if name == 'synthetic' else '/fresh/fixtures/root-a:/fresh/fixtures/root-b')
            self.assertNotIn('HOME', env)

    def runner(self, root):
        r = d.Runner.__new__(d.Runner)
        r.out = root; r.result = {'stages': {}, 'windows': {}, 'comparisons': {}}
        r.invalid_sources, r.global_invalid = set(), False
        r.identity_errors = {}
        r.labels, r.frozen, r.inputs = set(), {}, {}
        r.sources = {'A': root / 'source-A', 'B': root / 'source-B'}
        r.env = {}; r.tools = {'dotnet': '/fixed/dotnet'}
        r.controller = {}; r.golden = {name: 'golden\n' for name in d.DOMAINS}
        return r

    def test_failed_or_incomplete_compile_never_runs_jit_or_retries(self):
        for record in [dict(OK, exit=1), dict(OK, failure='timeout', raw_streams_complete=False),
                       dict(OK, raw_streams_complete=False)]:
            with tempfile.TemporaryDirectory() as temp:
                r = self.runner(Path(temp)); (r.out / 'A').mkdir()
                with patch.object(r, 'call', return_value=record) as call:
                    r.product('A', 'synthetic', Path('/tools'))
                    self.assertEqual(call.call_count, 1)
                    self.assertEqual(call.call_args.args[0], 'A-synthetic-compile')

    def test_existing_product_rejected_before_call(self):
        with tempfile.TemporaryDirectory() as temp:
            r = self.runner(Path(temp)); (r.out / 'A/products/synthetic').mkdir(parents=True)
            with patch.object(r, 'call') as call, self.assertRaises(ValueError):
                r.product('A', 'synthetic', Path('/tools'))
            call.assert_not_called()

    def test_raw_red_is_retained_and_equal_failure_is_not_pass(self):
        with tempfile.TemporaryDirectory() as temp:
            r = self.runner(Path(temp))
            for name in d.DOMAINS:
                for key in ('A', 'B', 'cp'):
                    label = name + '-cp' if key == 'cp' else key + '-' + name + '-jit'
                    r.result['stages'][label] = dict(OK, exit=1)
                    window = name + '-cp-window' if key == 'cp' else key + '-' + name + '-window'
                    r.result['windows'][window] = 'valid'
                    (r.out / (label + '.stdout')).write_bytes(b'partial\n')
                    (r.out / (label + '.stderr')).write_bytes(b'NotImplementedError: outside\n')
            r.summarize()
            self.assertFalse(r.result['all_goldens_pass'])
            self.assertTrue(r.result['comparisons']['synthetic']['same_observation'])

    def test_a_failure_does_not_skip_b_or_other_domain(self):
        with tempfile.TemporaryDirectory() as temp:
            r = self.runner(Path(temp)); calls = []
            def build(key):
                calls.append(key + '-build')
                return Path('/tools')
            def product(key, name, tools):
                calls.append(key + '-' + name)
                if key == 'A' and name == 'synthetic': raise ValueError('原红')
            with patch.object(r, 'setup'), patch.object(r, 'guard'), patch.object(r, 'cp'), \
                 patch.object(r, 'prepare_dependency_cache', return_value=True), \
                 patch.object(r, 'build', side_effect=build), patch.object(r, 'product', side_effect=product):
                self.assertEqual(r.execute(), 1)
            self.assertEqual(calls, ['A-build', 'A-synthetic', 'A-missing-include', 'B-build', 'B-synthetic', 'B-missing-include'])

    def test_source_blob_mode_and_dirty_identity_rejected(self):
        with tempfile.TemporaryDirectory() as temp:
            root = Path(temp); path = root / 'x'; path.write_bytes(b'old')
            blob = d.hashlib.sha1(b'blob 3\0old').hexdigest()
            def git(_, *args):
                return {('rev-parse', 'HEAD'): (HEAD + '\n').encode(),
                        ('rev-parse', 'HEAD^{tree}'): b'tree\n',
                        ('status', '--porcelain', '--untracked-files=all'): b'',
                        ('ls-tree', '-rz', 'HEAD'): ('100644 blob ' + blob + '\tx\0').encode()}[args]
            with patch.object(d, 'git', side_effect=git):
                d.source_identity(root, {'head': HEAD, 'tree': 'tree'})
                path.write_bytes(b'new')
                with self.assertRaises(ValueError): d.source_identity(root, {'head': HEAD, 'tree': 'tree'})
                path.write_bytes(b'old'); path.chmod(0o755)
                with self.assertRaises(ValueError): d.source_identity(root, {'head': HEAD, 'tree': 'tree'})

    def test_wrong_runtime_blocks_jit_after_successful_compile(self):
        with tempfile.TemporaryDirectory() as temp:
            r = self.runner(Path(temp)); (r.out / 'A').mkdir()
            tools = r.out / 'tools'; tools.mkdir(); (tools / 'RyuPy.Runtime.dll').write_bytes(b'right')
            def compile_only(*args):
                product = r.out / 'A/products/synthetic'; product.mkdir()
                (product / 'main.dll').write_bytes(b'pe')
                (product / 'RyuPy.Runtime.dll').write_bytes(b'wrong')
                return OK
            with patch.object(r, 'call', side_effect=compile_only) as call, patch.object(r, 'guard'):
                with self.assertRaises(ValueError): r.product('A', 'synthetic', tools)
                self.assertEqual(call.call_count, 1)

    def test_source_build_failure_blocks_only_its_products(self):
        with tempfile.TemporaryDirectory() as temp:
            r = self.runner(Path(temp)); calls = []
            with patch.object(r, 'setup'), patch.object(r, 'guard'), patch.object(r, 'cp'), \
                 patch.object(r, 'prepare_dependency_cache', return_value=True), \
                 patch.object(r, 'build', side_effect=[ValueError('A build failed'), Path('/tools')]), \
                 patch.object(r, 'product', side_effect=lambda key, name, tools: calls.append((key, name))):
                self.assertEqual(r.execute(), 1)
            self.assertEqual(calls, [('B', 'synthetic'), ('B', 'missing-include')])

    def test_post_jit_identity_failure_invalidates_comparison_but_preserves_raw(self):
        with tempfile.TemporaryDirectory() as temp:
            r = self.runner(Path(temp))
            label = 'A-synthetic-jit'
            def operation():
                r.result['stages'][label] = dict(OK)
                (r.out / (label + '.stdout')).write_bytes(b'golden\n')
                (r.out / (label + '.stderr')).write_bytes(b'')
            with patch.object(r, 'guard', side_effect=[None, d.IdentityError('changed')]):
                r.stage('A-synthetic-window', operation, 'A')
            r.summarize()
            observed = r.result['comparisons']['synthetic']
            self.assertEqual(observed['observations']['A']['status'], 'invalidated')
            self.assertIsNone(observed['same_observation'])
            self.assertNotIn('golden_pass', observed['observations']['A'])
            self.assertTrue(d.complete(r.result['stages'][label]))
            self.assertEqual((r.out / (label + '.stdout')).read_bytes(), b'golden\n')

    def test_guards_bind_controller_and_selected_source_and_stay_invalid(self):
        with tempfile.TemporaryDirectory() as temp:
            r = self.runner(Path(temp))
            with patch.object(d, 'source_identity') as identity:
                r.guard('A')
                self.assertEqual([c.args[0] for c in identity.call_args_list], [d.ROOT, r.sources['A']])
            with patch.object(d, 'source_identity', side_effect=ValueError('wrong tree')):
                with self.assertRaises(d.IdentityError): r.guard('A')
            with patch.object(d, 'source_identity') as identity:
                with self.assertRaises(d.IdentityError): r.guard('A')
                identity.assert_not_called()
                r.guard('B')
                self.assertEqual(identity.call_args.args[0], r.sources['B'])

    def cache_fixture(self, root):
        r = self.runner(root); r.tools['cargo'] = '/fixed/cargo'
        for source in r.sources.values():
            source.mkdir(); (source / 'Cargo.toml').write_text('[workspace]')
            (source / 'Cargo.lock').write_bytes((d.ROOT / 'Cargo.lock').read_bytes())
        return r

    def test_fetch_once_full_locked_graph_no_platform_filter(self):
        with tempfile.TemporaryDirectory() as temp:
            r = self.cache_fixture(Path(temp))
            with patch.object(r, 'guard') as guard, patch.object(d.shared, 'capture', return_value=OK) as cap:
                self.assertTrue(r.prepare_dependency_cache())
                argv = cap.call_args.args[1]
                self.assertEqual(argv, ['/fixed/cargo', 'fetch', '--locked', '--manifest-path', r.sources['A'] / 'Cargo.toml'])
                self.assertEqual(cap.call_args.args[3], 600)
                self.assertEqual(cap.call_args.args[4]['CARGO_NET_RETRY'], '0')
                self.assertEqual([x.args for x in guard.call_args_list], [('A',), ('B',), ('A',), ('B',)])
                with self.assertRaises(ValueError): r.prepare_dependency_cache()
                self.assertEqual(cap.call_count, 1)

    def test_fetch_rejects_bad_lock_before_network_and_mutation_after(self):
        for moment in ('before', 'during'):
            with tempfile.TemporaryDirectory() as temp:
                r = self.cache_fixture(Path(temp)); lock = r.sources['B'] / 'Cargo.lock'
                if moment == 'before': lock.write_text('changed')
                def capture(*args, **kwargs):
                    lock.write_text('changed'); return OK
                with patch.object(r, 'guard'), patch.object(d.shared, 'capture', side_effect=capture) as cap:
                    with self.assertRaises(ValueError): r.prepare_dependency_cache()
                    self.assertEqual(cap.call_count, 0 if moment == 'before' else 1)

    def test_fetch_failed_timeout_or_incomplete_no_build_or_product(self):
        for record in (dict(OK, exit=101), dict(OK, failure='timeout', raw_streams_complete=False),
                       dict(OK, raw_streams_complete=False)):
            with tempfile.TemporaryDirectory() as temp:
                r = self.cache_fixture(Path(temp))
                with patch.object(r, 'setup'), patch.object(r, 'guard'), patch.object(r, 'cp') as cp, \
                     patch.object(d.shared, 'capture', return_value=record) as cap, \
                     patch.object(r, 'build') as build, patch.object(r, 'product') as product:
                    self.assertEqual(r.execute(), 1)
                    self.assertEqual(cp.call_count, 2); self.assertEqual(cap.call_count, 1)
                    build.assert_not_called(); product.assert_not_called()
                    self.assertEqual(r.result['stages']['dependency-cache-fetch'], record)
                    self.assertEqual(r.result['windows']['dependency-cache-window'], 'failed')

    def test_capture_single_use_and_parameters(self):
        with tempfile.TemporaryDirectory() as temp:
            r = self.runner(Path(temp))
            with patch.object(d.shared, 'capture', return_value=OK) as cap:
                r.call('one', ['/mock'], r.out, 30, {'PATH': '/fixed'}, cp=True)
                self.assertEqual(cap.call_args.kwargs['memory_limit'], 512 * 1024**2)
                with self.assertRaises(ValueError): r.call('one', ['/mock'], r.out, 30)
                self.assertEqual(cap.call_count, 1)


if __name__ == '__main__':
    unittest.main()
