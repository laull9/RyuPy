"""仅host侧纯mock/文件验证；不启动CP fixture、产品、C#、下载或发布。"""
import copy
import json
import os
from pathlib import Path
import tempfile
import types
import unittest
from unittest.mock import patch, Mock
import distribution_metadata as gate
import distribution_metadata_contract as c
import distribution_metadata_products as p
import manifest_options
from distribution_metadata_identity import IdentityError, inventory, isolated


def sample(root):
    dist = root / 'root/Alpha_Pkg-1.dist-info'; dist.mkdir(parents=True)
    (dist / 'METADATA').write_bytes(b'Name: Alpha.Pkg\nVersion: 1\n')
    (dist / 'unknown.bin').write_bytes(b'\x00\xff')
    request = root / 'request.json'
    request.write_text(json.dumps({'schema_version': 1, 'mode': 'closed_snapshot', 'roots': [{'id': 'r', 'path': 'root'}]}))
    data = {'schema_version': 1, 'mode': 'closed_snapshot', 'roots': ['r'], 'distributions': [{
        'root': 'r', 'path': dist.name, 'kind': 'dist-info', 'metadata_name': 'Alpha.Pkg', 'metadata_version': '1',
        'discovery_key': 'alpha_pkg', 'shadowed': False, 'directories': [], 'files': [
            {'path': f.name, 'size': len(f.read_bytes()), 'sha256': c.sha(f), 'hex': f.read_bytes().hex()}
            for f in sorted(dist.iterdir())]}]}
    return request, data


def serialize(data):
    payload = json.dumps(data, ensure_ascii=False, separators=(',', ':'))
    report = copy.deepcopy(data)
    for dist in report['distributions']:
        for row in dist['files']: del row['hex']
    report['payload_sha256'] = c.digest(payload.encode())
    return payload, report


class Tests(unittest.TestCase):
    def test_class_factory_title_is_exact_addition(self):
        env = {'RUNNER_ENVIRONMENT': 'github-hosted', 'RYUPY_VALIDATION_MODE': 'distribution-metadata',
               'GITHUB_ACTIONS': 'true', 'GITHUB_EVENT_NAME': 'push', 'GITHUB_REF': gate.old.shared.REF,
               'GITHUB_RUN_ATTEMPT': '1', 'GITHUB_SHA': 'a' * 40}
        title = 'validation: lexical-class-factory-001'
        event = {'ref': env['GITHUB_REF'], 'after': env['GITHUB_SHA'],
                 'head_commit': {'id': env['GITHUB_SHA'], 'message': title}}
        gate.check_context(env, event)
        for wrong in (title.upper(), title + '\n'):
            with self.assertRaises(ValueError):
                gate.check_context(env, dict(event, head_commit=dict(event['head_commit'], message=wrong)))

    def test_context_is_exact_and_does_not_mutate_original(self):
        env = {'RUNNER_ENVIRONMENT': 'github-hosted', 'RYUPY_VALIDATION_MODE': 'distribution-metadata',
               'GITHUB_ACTIONS': 'true', 'GITHUB_EVENT_NAME': 'push', 'GITHUB_REF': gate.old.shared.REF,
               'GITHUB_RUN_ATTEMPT': '1', 'GITHUB_SHA': 'a' * 40}
        event = {'ref': env['GITHUB_REF'], 'after': env['GITHUB_SHA'],
                 'head_commit': {'id': env['GITHUB_SHA'], 'message': gate.DATA['title']}}
        before = copy.deepcopy(event); gate.check_context(env, event); self.assertEqual(event, before)
        for title in (gate.DATA['title'].upper(), gate.DATA['title'] + '\n', 'validation: text-search-002'):
            with self.assertRaises(ValueError): gate.check_context(env, dict(event, head_commit=dict(event['head_commit'], message=title)))
        for key, value in [('GITHUB_EVENT_NAME', 'workflow_dispatch'), ('GITHUB_RUN_ATTEMPT', '2'), ('RUSTFLAGS', '-A warnings')]:
            with self.assertRaises(ValueError): gate.check_context(dict(env, **{key: value}), event)

    def test_actual_bytes_and_complete_inventory(self):
        with tempfile.TemporaryDirectory() as temp:
            request, data = sample(Path(temp)); payload, report = serialize(data)
            self.assertEqual(c.validate_snapshot(payload, report, request), data)
            (request.parent / 'root/Alpha_Pkg-1.dist-info/new-unknown').write_bytes(b'new')
            with self.assertRaises(ValueError): c.validate_snapshot(payload, report, request)

    def test_self_consistent_forged_bytes_hash_still_rejected(self):
        with tempfile.TemporaryDirectory() as temp:
            request, data = sample(Path(temp))
            file = data['distributions'][0]['files'][1]
            file.update(hex=b'wrong'.hex(), size=5, sha256=c.digest(b'wrong'))
            payload, report = serialize(data)
            with self.assertRaises(ValueError): c.validate_snapshot(payload, report, request)

    def test_missing_extra_duplicate_and_replaced_snapshot_rejected(self):
        with tempfile.TemporaryDirectory() as temp:
            request, data = sample(Path(temp)); payload, report = serialize(data)
            for key in report:
                altered = dict(report); del altered[key]
                with self.assertRaises(ValueError): c.validate_snapshot(payload, altered, request)
            altered = dict(report, extra=1)
            with self.assertRaises(ValueError): c.validate_snapshot(payload, altered, request)
            changed = copy.deepcopy(data); changed['distributions'][0]['metadata_name'] = 'Other'
            raw, derived = serialize(changed)
            with self.assertRaises(ValueError): c.validate_snapshot(raw, derived, request)
            with self.assertRaises(Exception): c.parse('{"mode":1,"mode":2}')

    def test_discovery_key_counterexample_not_name_only(self):
        self.assertEqual(c.normalize('Alpha_Pkg'), 'alpha_pkg')
        self.assertEqual(c.normalize('alpha-pkg'.split('-')[0]), 'alpha')
        with tempfile.TemporaryDirectory() as temp:
            request, data = sample(Path(temp)); data['distributions'][0]['discovery_key'] = 'wrong'
            payload, report = serialize(data)
            with self.assertRaises(ValueError): c.validate_snapshot(payload, report, request)

    def test_pe_requires_real_prefix_and_passes_actual_string(self):
        instructions = [dict(opcode='0xd0', operand_hex='01000002'),
            dict(opcode='0x28', target={'owner': 'System.Type', 'name': 'GetTypeFromHandle', 'signature_hex': '000112051109'}),
            dict(opcode='0x72', string='actual payload'),
            dict(opcode='0x28', target={'owner': 'RyuPy.Runtime.PyDistributionSnapshot', 'name': 'Configure', 'signature_hex': '00020112050e'})]
        data = {'methods': [{'method': {'name': '__init_functions', 'owner': 'RyuPy.Generated.Module'}, 'body': {'instructions': instructions}}]}
        types = {'0x2000001': 'RyuPy.Generated.Module', '0x2000002': 'Other',
                 '0x1000001': 'System.Type', '0x1000002': 'System.RuntimeTypeHandle'}
        self.assertEqual(c.pe_payload(data, types), 'actual payload')
        for index in range(4):
            bad = copy.deepcopy(data); bad['methods'][0]['body']['instructions'][index]['opcode'] = '0x00'
            # call解析还需opcode本身为call，不能只信伪target字典。
            with self.assertRaises(ValueError): c.pe_payload(bad, types)

    def test_manifest_projection_preserves_original_and_old_assertions(self):
        options = {key: False for key in manifest_options.BOOL_OPTIONS}
        options.update(instance_modules=[], module_environment_policy='ordinary-dynamic-module-pilot', distribution_metadata={'checked': True})
        data = {'compiler': {'options': options}}; before = copy.deepcopy(data)
        projected = c.schema_projection(data, {'checked': True})
        manifest_options.validate_options(projected['compiler']['options']); self.assertEqual(data, before)
        for changed in ({}, {'checked': False}):
            with self.assertRaises(ValueError): c.schema_projection(data, changed)
        missing = copy.deepcopy(data); del missing['compiler']['options']['distribution_metadata']
        with self.assertRaises(ValueError): c.schema_projection(missing, {'checked': True})
        data['compiler']['options']['unexpected'] = 1
        with self.assertRaises(Exception): manifest_options.validate_options(c.schema_projection(data, {'checked': True})['compiler']['options'])

    def test_trx_requires_all_facts_no_skip_duplicate_or_zero(self):
        with tempfile.TemporaryDirectory() as temp:
            path = Path(temp) / 'result.trx'
            def write(names, outcome='Passed'):
                path.write_text('<TestRun xmlns="http://microsoft.com/schemas/VisualStudio/TeamTest/2010">'+''.join(
                    '<UnitTestResult testName="RyuPy.Runtime.Tests.' + n + '" outcome="' + outcome + '"/>' for n in names)+'</TestRun>')
            write(gate.DATA['facts']); self.assertEqual(gate.verify_trx(path)['total'], len(gate.DATA['facts']))
            for names, outcome in [([], 'Passed'), (gate.DATA['facts'] * 2, 'Passed'), (gate.DATA['facts'], 'NotExecuted')]:
                write(names, outcome)
                with self.assertRaises(ValueError): gate.verify_trx(path)

    def test_failed_stage_does_not_skip_next_independent_domain(self):
        with tempfile.TemporaryDirectory() as temp:
            r = self.runner(temp)
            self.assertFalse(r.phase('bad', lambda: (_ for _ in ()).throw(ValueError('real failure'))))
            self.assertTrue(r.phase('good', lambda: {'detail': 'executed'}))
            self.assertEqual(r.result['stages']['bad']['status'], 'failed')

    def runner(self, temp):
        with patch.object(gate, 'ROOT', Path(temp)):
            r = gate.Runner()
        r.before = {'head': 'fixed', 'tree': 'fixed', 'files': {}}
        r.identity = Mock(return_value=r.before)
        r.env = {}
        return r

    def test_native_resume_never_runs_on_changed_source_or_tools(self):
        for failure in ('audit', 'between'):
            with self.subTest(failure=failure), tempfile.TemporaryDirectory() as temp:
                r = self.runner(temp); r.load_state = Mock(); r.cli = Path('/not-executed')
                for name in ('tools', 'sourceprep', 'synthetic-cp'): r.result['stages'][name] = {'passed': True, 'status': 'passed'}
                tool = r.out / 'tool'; tool.write_bytes(b'original'); r.frozen[str(tool)] = c.sha(tool)
                def native(*args, **kwargs):
                    if failure == 'audit': r.audit(lambda: (_ for _ in ()).throw(ValueError('PE/runtime/payload mismatch')))
                    tool.write_bytes(b'changed')
                with patch.object(gate, 'check_context'), patch.dict(os.environ, {'GITHUB_EVENT_PATH': str(r.out / 'event')}), patch.object(p, 'native', side_effect=native) as run:
                    (r.out / 'event').write_text('{}')
                    self.assertEqual(r.execute('native'), 1); self.assertEqual(run.call_count, 1)
                self.assertTrue(r.identity_failed)
                self.assertFalse(r.result['stages']['tools']['passed'])
                self.assertEqual(r.result['stages']['markdown-aot']['status'], 'not-run')


    def test_ordinary_native_semantic_failure_still_runs_independent_second_domain(self):
        with tempfile.TemporaryDirectory() as temp:
            r = self.runner(temp); r.load_state = Mock(); r.cli = Path('/not-executed')
            for name in ('tools', 'sourceprep', 'synthetic-cp'): r.result['stages'][name] = {'passed': True, 'status': 'passed'}
            (r.out / 'event').write_text('{}')
            with patch.object(gate, 'check_context'), patch.dict(os.environ, {'GITHUB_EVENT_PATH': str(r.out / 'event')}), patch.object(p, 'native', side_effect=[ValueError('stdout mismatch'), None]) as run:
                self.assertEqual(r.execute('native'), 1); self.assertEqual(run.call_count, 2)
            self.assertFalse(r.identity_failed); self.assertEqual(r.result['stages']['synthetic-native']['status'], 'failed')
            self.assertTrue(r.result['stages']['markdown-aot']['passed'])

    def test_current_markdown_argv_only_adds_metadata_option(self):
        r = types.SimpleNamespace(cli=Path('/cli'), fixture=Path('/fixture'), inputs_dir=Path('/inputs'))
        entry, target, request = Path('/inputs/markdown_sample.py'), Path('/out'), Path('/inputs/metadata-request.json')
        original = gate.old.shared.build_command(r.cli, entry, r.inputs_dir, target)
        self.assertEqual(p.argv(r, entry, target, request, markdown=True), original + ['--distribution-metadata', request])
        self.assertEqual(len(gate.DATA['goldens']['real-discovery'].splitlines()), 24)
        self.assertIn('total 19\n', gate.DATA['goldens']['real-discovery'])
        self.assertIn('extensions 18\nconsole 1\n', gate.DATA['goldens']['real-discovery'])

    def test_no_runtime_reset_or_repeated_native_framework(self):
        host = (p.FIXTURES / 'host/Program.cs').read_text()
        self.assertNotIn('Reset(', host)
        source = Path(p.__file__).read_text()
        self.assertEqual(source.count("+ ['--aot']"), 1)
        self.assertNotIn('expected_refusal', source)
        main = Path(gate.__file__).read_text()
        self.assertNotIn('HEAD^', main)
        self.assertNotIn('text_search.Runner', main)

    def test_workflow_independent_dag_checkpoint_and_single_tools(self):
        text = (gate.ROOT / '.github/workflows/wcwidth-legacy-aot.yml').read_text()
        self.assertLessEqual(len(text.splitlines()), 600)
        old_jobs = text.split('  baseline:\n', 1)[1]
        self.assertNotIn('needs: distribution-metadata', old_jobs)
        new = (gate.ROOT / '.github/workflows/distribution-metadata.yml').read_text()
        self.assertEqual(new.count('--phase jit'), 1); self.assertEqual(new.count('--phase native'), 1)
        self.assertLess(new.index('metadata-checkpoint-'), new.index('--phase native'))
        self.assertNotIn('continue-on-error', new)
        self.assertIn('!cancelled()', new)
        for path in gate.ROOT.glob('tests/validation/distribution_metadata*.py'):
            self.assertLessEqual(len(path.read_text().splitlines()), 600)

    def test_pe_wrong_rid_and_full_call_signature_rejected(self):
        types = {'0x2000001': 'RyuPy.Generated.Module', '0x2000002': 'Other',
                 '0x1000001': 'System.Type', '0x1000002': 'System.RuntimeTypeHandle'}
        for blob in ('200112051109', '00020112050e00'):
            with self.assertRaises(ValueError): c.signature(blob, types)
        # 同表其它RID绝不能因为仍是TypeDef而被接受。
        data = {'methods': [{'method': {'name': '__init_functions', 'owner': 'RyuPy.Generated.Module'}, 'body': {'instructions': [
            {'opcode': '0xd0', 'operand_hex': '02000002'},
            {'opcode': '0x28', 'target': {'owner': 'System.Type', 'name': 'GetTypeFromHandle', 'signature_hex': '000112051109'}},
            {'opcode': '0x72', 'string': 'payload'},
            {'opcode': '0x28', 'target': {'owner': 'RyuPy.Runtime.PyDistributionSnapshot', 'name': 'Configure', 'signature_hex': '00020112050e'}}]}}]}
        with self.assertRaises(ValueError): c.pe_payload(data, types)
        data['methods'][0]['body']['instructions'][0]['operand_hex'] = '01000002'
        for index, blob in [(1, '000112050e'), (3, '0001011205')]:
            altered = copy.deepcopy(data); altered['methods'][0]['body']['instructions'][index]['target']['signature_hex'] = blob
            with self.assertRaises(ValueError): c.pe_payload(altered, types)

    def test_complete_inventory_new_empty_directory_distribution_and_type(self):
        for kind in ('empty-dir', 'new-dist', 'new-file', 'symlink'):
            with self.subTest(kind=kind), tempfile.TemporaryDirectory() as temp:
                r = self.runner(temp); r.fixture.mkdir(); file = r.fixture / 'original'; file.write_bytes(b'a')
                r.freeze_inputs(r.fixture)
                if kind == 'empty-dir': (r.fixture / 'empty').mkdir()
                if kind == 'new-dist': (r.fixture / 'Shadow-1.dist-info').mkdir()
                if kind == 'new-file': (r.fixture / 'new').write_bytes(b'b')
                if kind == 'symlink': file.unlink(); file.symlink_to(r.out / 'result.json')
                with self.assertRaises(IdentityError): r.barrier('changed')
                self.assertTrue(r.identity_failed)

    def test_identity_is_sticky_even_if_bytes_restored(self):
        with tempfile.TemporaryDirectory() as temp:
            r = self.runner(temp); tool = r.out / 'tool'; tool.write_bytes(b'a'); r.frozen[str(tool)] = c.sha(tool)
            r.result['stages']['tools'] = {'passed': True, 'status': 'passed'}
            tool.write_bytes(b'b')
            with self.assertRaises(IdentityError): r.barrier('changed')
            tool.write_bytes(b'a')
            with self.assertRaises(IdentityError): r.barrier('restored')
            self.assertFalse(json.loads((r.out / 'result.json').read_text())['identity_preserved'])
            with patch.object(gate.old.Runner, 'run_product') as execute:
                with self.assertRaises(IdentityError): r.run_product('forbidden', ['/not-executed'])
                execute.assert_not_called()

    def test_resume_environment_and_malformed_state_invalidate_saved_passes(self):
        for failure in ('environment', 'json'):
            with self.subTest(failure=failure), tempfile.TemporaryDirectory() as temp:
                r = self.runner(temp); r.dotnet = Path(temp) / 'dotnet'; r.environment()
                r.result['stages']['tools'] = {'passed': True, 'status': 'passed'}; r.persist()
                state_path = r.out / 'state.json'
                if failure == 'environment':
                    state = json.loads(state_path.read_text()); state['environment']['TZ'] = 'changed'; state_path.write_text(json.dumps(state))
                else: state_path.write_text('{broken')
                with patch.object(gate, 'ROOT', Path(temp)): resumed = gate.Runner(resume=True)
                with patch.object(p, 'native') as execute:
                    self.assertEqual(resumed.execute('native'), 1); execute.assert_not_called()
                result = json.loads((r.out / 'result.json').read_text())
                self.assertFalse(result['stages']['tools']['passed']); self.assertFalse(result['identity_preserved'])

    def test_isolation_mapping_preserves_authority_and_checks_bytes_before_execution(self):
        with tempfile.TemporaryDirectory() as temp:
            r = self.runner(temp); root = r.fixture / 'root-a'; root.mkdir(parents=True)
            file = root / 'data'; file.write_bytes(b'unchanged'); (root / 'empty').mkdir()
            r.freeze_inputs(r.fixture); r.frozen[str(file)] = c.sha(file)
            original = copy.deepcopy(r.input_inventories); frozen = dict(r.frozen)
            with isolated(r, [root], 'native'):
                self.assertFalse(root.exists()); self.assertEqual(inventory(r.fixture, r.isolation), original[str(r.fixture)])
                r.barrier('during')
            self.assertTrue(root.exists()); self.assertEqual(r.input_inventories, original); self.assertEqual(r.frozen, frozen)
            self.assertEqual(r.isolation, {})

    def test_isolation_mutation_and_restoration_failure_are_sticky(self):
        for failure in ('bytes', 'original-reappears'):
            with self.subTest(failure=failure), tempfile.TemporaryDirectory() as temp:
                r = self.runner(temp); root = r.fixture / 'root-a'; root.mkdir(parents=True)
                (root / 'data').write_bytes(b'unchanged'); r.freeze_inputs(r.fixture)
                with self.assertRaises(IdentityError):
                    with isolated(r, [root], 'native'):
                        if failure == 'bytes': (root.with_name('root-a.isolated') / 'data').write_bytes(b'changed')
                        else: root.mkdir()
                        r.barrier('before-execute')
                self.assertTrue(r.identity_failed)
                with self.assertRaises(IdentityError): r.barrier('second-native')
                self.assertEqual(r.result['restoration_full_check'], 'failed')

    def test_collection_rejections_require_correct_collector_diagnostic(self):
        record = {'exit': 1, 'failure': None, 'raw_streams_complete': True}
        for case, tokens in p.REJECTIONS.items():
            stderr = ('error: ' + ' '.join(tokens) + '\n').encode()
            p.rejection_diagnostic(case, record, b'', stderr)
            for unrelated in (b'error: E0100 invalid syntax\n', b'error: No such file or directory (os error 2)\n' if case != 'missing-root' else b'error: other\n', b'[ryupy-stage] stage=graph-load\n' + stderr):
                with self.assertRaises(ValueError): p.rejection_diagnostic(case, record, b'', unrelated)
            with self.assertRaises(ValueError): p.rejection_diagnostic(case, dict(record, exit=127), b'', stderr)

    def test_native_verifier_retains_original_name_parent_plain_file_guards(self):
        with tempfile.TemporaryDirectory() as temp:
            root = Path(temp); good = root / 'good'; good.write_bytes(b'elf')
            alias = root / 'alias'; os.link(good, alias)
            report = {'verified': True}
            data = {'compiler': {'options': {'distribution_metadata': report}}, 'artifacts': [{'role': 'native_executable', 'relative_path': 'good'}]}
            (root / 'ryupy-artifact.json').write_text(json.dumps(data))
            with patch.object(c.manifest, 'validate_schema'), patch.object(c.manifest, 'check_artifacts', return_value={'good': good}):
                with self.assertRaises(ValueError): c.verify_native(root, alias, '10.0.403', report, root / 'runtime', root / 'pe', {})
            manifest_path = root / 'ryupy-artifact.json'; manifest_path.unlink(); manifest_path.symlink_to(good)
            with self.assertRaises(ValueError): c.verify_native(root, good, '10.0.403', report, root / 'runtime', root / 'pe', {})

    def test_host_injection_is_native_parent_once_and_source_parent_separate(self):
        source = (p.FIXTURES / 'host/Program.cs').read_text()
        block = source.split('static void FailedMetadata(', 1)[1].split('static void SourceParent()', 1)[0]
        self.assertIn('PyModules.RegisterNative("importlib", attributes)', block)
        self.assertNotIn('PyModules.Register("importlib"', block)
        self.assertIn('comparer.Armed = () =>', block)
        self.assertIn('Armed = null; //', source)
        self.assertIn('case "source-parent": SourceParent()', source)




class AuthorityTests(unittest.TestCase):
    """只构造记录/PE解码数据；不执行新fixture或产品。"""
    def test_authority_budget_and_complete_frozen_fixtures(self):
        import distribution_metadata_authority as a
        self.assertEqual(a.DATA['budget'], {'cp': 8, 'compile': 9, 'jit': 9, 'native': 0})
        self.assertEqual(len(a.DATA['oracles']), 8)
        self.assertEqual(len(a.DATA['cases']), 9)
        self.assertEqual(len(set(a.stage_names())), 18)
        root = p.FIXTURES / 'authority'
        self.assertEqual({f.relative_to(root).as_posix(): c.sha(f) for f in root.rglob('*') if f.is_file()},
                         a.DATA['fixture_hashes'])
        for file in root.rglob('*.py'):
            compile(file.read_bytes(), str(file), 'exec')

    def test_authority_negative_requires_exact_natural_exit_and_streams(self):
        import distribution_metadata_authority as a
        record = {'exit': 1, 'failure': None, 'raw_streams_complete': True}
        a.verdict(record, b'before\n', a.NI['hooks'].encode(), b'before\n', 'hooks')
        for key, value in [('exit', 0), ('exit', -11), ('exit', 124), ('failure', 'timeout'),
                           ('failure', 'launch'), ('raw_streams_complete', False)]:
            with self.assertRaises(ValueError):
                a.verdict(dict(record, **{key: value}), b'before\n', a.NI['hooks'].encode(), b'before\n', 'hooks')
        for stdout, stderr in [(b'', a.NI['hooks'].encode()), (b'before\n', a.NI['outside'].encode()),
                               (b'before\n', b'NotImplementedError: unrelated\n')]:
            with self.assertRaises(ValueError):
                a.verdict(record, stdout, stderr, b'before\n', 'hooks')
        with self.assertRaises(ValueError):
            a.verdict(record, b'before\n', a.NI['hooks'].encode(), b'before\n')

    def test_authority_plan_cannot_replace_automatic_with_empty_or_closed(self):
        import distribution_metadata_authority as a
        payload = {'schema_version': 1, 'roots': ['neutral_pkg.target'],
                   'dependencies': {'neutral_pkg.target': ['neutral_pkg']},
                   'locations': {'neutral_pkg.target': {}}, 'deferred': {},
                   'outside': sorted(a.TARGETS - {'neutral_pkg.target'}), 'protected': []}
        report = dict(payload, diagnostics={}, metadata_unavailable=None)
        modules = {'modules': [{'module': 'neutral_pkg.target', 'compiled': True, 'candidate': True}]}
        row = a.DATA['cases']['automatic-selected']
        a.validate_plan(payload, report, modules, row)
        for key, value in [('roots', []), ('outside', []), ('closed_unplanned', []), ('schema_version', 2),
                           ('deferred', {'neutral_pkg.target': 'compile failure'})]:
            bad = dict(payload, **{key: value})
            with self.assertRaises(ValueError):
                a.validate_plan(bad, dict(bad, diagnostics={}, metadata_unavailable=None), modules, row)
        with self.assertRaises(ValueError):
            a.validate_plan(payload, dict(report, outside=[]), modules, row)

    def test_authority_payload_must_be_actual_unique_ldstr_call(self):
        import distribution_metadata_authority as a
        target = {'owner': 'RyuPy.Runtime.PyModules', 'name': 'ConfigureOptionalSources',
                  'signature_hex': '0001010e'}
        code = [{'opcode': '0x72', 'string': '{"schema_version":1}'}, {'opcode': '0x28', 'target': target}]
        def decoded(ins): return {'methods': [{'body': {'instructions': ins}}]}
        self.assertEqual(a.optional_payload(decoded(code)), {'schema_version': 1})
        for ins in [[], code + code, [dict(code[0], opcode='0x00'), code[1]],
                    [code[0], dict(code[1], target=dict(target, signature_hex='000001'))]]:
            with self.assertRaises(ValueError): a.optional_payload(decoded(ins))


class AuthorityIdentityPropagationTests(unittest.TestCase):
    def test_checkpoint_identity_failure_invalidates_and_stops_successor(self):
        import distribution_metadata_authority as a
        # 复用真实Runner.audit/phase/fail_identity/invalidate，只mock持久化和产品边界。
        runner = gate.Runner.__new__(gate.Runner)
        runner.result = {'stages': {n: {'passed': False, 'status': 'not-run'} for n in gate.stage_names()}}
        runner.identity_failed = False
        runner.persist = Mock()
        runner.out = Path('/unused-authority-selftest')
        runner.fixture = Path('/unused-authority-selftest/fixtures')
        runner.cli = Path('/unused-cli')
        runner.dotnet = Path('/unused-dotnet')
        runner.env = {}
        runner.barrier = Mock()
        runner.command = Mock(return_value={'exit': 0, 'failure': None})
        runner.run_product = Mock(side_effect=RuntimeError('产品绝不能执行'))
        stage = 'authority-explicit-selected-jit'
        successor = Mock(side_effect=RuntimeError('后继绝不能执行'))
        with patch.object(a, 'checkpoint', side_effect=ValueError('PE/runtime/payload identity mismatch')), \
             patch.object(gate, 'save'):
            with self.assertRaises(IdentityError):
                runner.phase(stage, lambda: a.jit(runner, 'explicit-selected', a.DATA['cases']['explicit-selected']))
            self.assertTrue(runner.identity_failed)
            self.assertFalse(runner.result['passed'])
            self.assertEqual(runner.result['stages'][stage]['status'], 'invalidated-identity')
            with self.assertRaises(IdentityError):
                runner.phase('authority-automatic-selected-jit', successor)
        runner.run_product.assert_not_called()
        successor.assert_not_called()
        self.assertTrue(runner.persist.called)


if __name__ == '__main__': unittest.main()
