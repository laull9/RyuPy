"""Scanner 纯控制自测：只创建小型假原流，不启动 CP fixture、CLI、dotnet 或 Native。"""
import copy
import hashlib
import json
import re
from pathlib import Path
import tempfile
import types
import unittest
from unittest import mock
import re_scanner_contract as c
import re_scanner_workload as workload
import re_scanner_validation as gate
import textwrap_dedent_selftest as old_tests


def write_process(root, name, argv, stdout=b'OK\n', stderr=b'', **changes):
    """小型假原件保留capture四件结构；控制测试不启动任何被测程序。"""
    prefix = root / name
    prefix.parent.mkdir(parents=True, exist_ok=True)
    for suffix, raw in [('stdout', stdout), ('stderr', stderr)]: prefix.with_suffix('.' + suffix).write_bytes(raw)
    record = dict(exit=0, failure=None, raw_streams_complete=True, argv=list(map(str, argv)),
        streams={suffix: dict(bytes=prefix.with_suffix('.' + suffix).stat().st_size,
                              sha256=c.sha(prefix.with_suffix('.' + suffix))) for suffix in ('stdout', 'stderr')})
    record.update(changes)
    prefix.with_suffix('.json').write_text(json.dumps(record))
    prefix.with_suffix('.argv.json').write_text(json.dumps(dict(argv=list(map(str, argv)))))
    return record


class ScannerControlTests(unittest.TestCase):
    def checkpoint_runner(self, root, red=None):
        """纯假证据覆盖真实load_state控制流；host、程序集与source barrier只用显式stub。"""
        runner = gate.Runner.__new__(gate.Runner)
        runner.data = c.load_manifest()
        runner.out = runner.root_out = root
        runner.inputs_dir = root / 'inputs'; runner.inputs_dir.mkdir()
        runner.python, runner.dotnet = Path('/fake/python'), Path('/fake/dotnet')
        runner.cli, runner.runtime = root / 'tools/ryupy', root / 'tools/RyuPy.Runtime.dll'
        runner.labels = runner.root_labels = set()
        runner.before, runner.identity_failed = None, False
        runner.frozen, runner.input_inventories, runner.isolation = {}, {}, {}
        runner.run_records, runner.snapshot_reports = {}, {}
        runner.result = dict(passed=False, collection_ready=False,
            stages={name: dict(passed=False, status='not-run') for name in gate.stage_names(runner.data)})
        identity = dict(head='a', tree='b', run_id='123', run_attempt='1', manifest_sha256='c', sources={}, tools={})
        runner.identity = lambda label: dict(head='a', tree='b')
        runner.fresh_identity = lambda: identity
        runner.environment = lambda: setattr(runner, 'env', {'TEST_ONLY': '1'})
        runner.test_assemblies = lambda: None
        runner.barrier = lambda label: None
        runner.persist = lambda: None
        expected, commands = runner.expected_runs(), runner.successful_commands()
        runs = {}
        for name, row in expected.items():
            changes = red if name == 'tokens-exception-jit-run' and red else {}
            arguments = dict(stdout=row['stdout'].encode()); arguments.update(changes)
            record = write_process(root, name, row['argv'], **arguments)
            runs[name] = dict(record=record, prefix=name, argv=row['argv'], source_sha256=row['source_sha256'])
            pe = Path(row['argv'][1]); pe.parent.mkdir(parents=True, exist_ok=True)
            for filename, content in ((pe.name, b'PE'), ('RyuPy.Runtime.dll', b'Runtime'), ('module-environment.json', b'{}')):
                (pe.parent / filename).write_bytes(content)
        for name, argv in commands.items(): write_process(root, name, argv)
        for name, content in [('tools/ryupy', b'CLI'), ('tools/RyuPy.Runtime.dll', b'Runtime'),
                              ('managed/RyuPy.Runtime.dll', b'Runtime'), ('managed/RyuPy.Runtime.Tests.dll', b'Tests')]:
            path = root / name; path.parent.mkdir(parents=True, exist_ok=True); path.write_bytes(content)
        trx = root / 'trx/result.trx'; trx.parent.mkdir()
        trx.write_text('<TestRun xmlns="http://microsoft.com/schemas/VisualStudio/TeamTest/2010">' + ''.join(
            '<UnitTestResult testName="RyuPy.Runtime.Tests.' + name + '" outcome="Passed"/>'
            for name in runner.data['csharp']['facts']) + '</TestRun>')
        checkpoint = dict(schema_version=1, identity=identity, runs=runs,
            files={str(p.relative_to(root)): c.sha(p) for p in root.rglob('*') if p.is_file()})
        state = dict(schema_version=1, before=dict(head='a', tree='b'), isolation={},
            labels=list(expected) + list(commands), environment={'TEST_ONLY': '1'}, frozen={},
            inventories={str(runner.inputs_dir): {}}, checkpoint=checkpoint, snapshots={},
            result=dict(passed=True, stages={'untrusted': dict(passed=True)}, jit_observed=999))
        return runner, state

    def resume(self, runner, state, execute=False):
        gate.save(runner.out / 'state.json', state)
        event = runner.out / 'event.json'; event.write_text('{}')
        with mock.patch.dict(gate.os.environ, PYTHON='/fake/python', DOTNET='/fake/dotnet', GITHUB_EVENT_PATH=str(event)), \
             mock.patch.object(c, 'check_context', return_value=None):
            return runner.execute('native') if execute else runner.load_state()

    def test_complete_semantic_red_collects_but_native_green_cannot_erase_it(self):
        for red in (dict(exit=1, stderr=b'traceback\n'), dict(stdout=b'wrong\n'), dict(stderr=b'wrong\n'), None):
            with self.subTest(red=red), tempfile.TemporaryDirectory() as temp:
                runner, state = self.checkpoint_runner(Path(temp), red)
                visited = []
                runner.native_graph = lambda graph: visited.append(graph['id'])
                with mock.patch.object(workload, 'native', return_value={'test_only': True}):
                    code = self.resume(runner, state, execute=True)
                self.assertTrue(runner.result['collection_ready'])
                self.assertEqual(code, 1 if red else 0)
                self.assertEqual(runner.result['passed'], not bool(red))
                self.assertEqual(runner.result['jit_observed'], 20)
                self.assertEqual(len(runner.result['compile_commands']), 6)
                self.assertEqual(runner.result['stages']['csharp-tests']['total'], 43)
                self.assertEqual(len(visited), 5)
                self.assertNotIn('untrusted', runner.result['stages'])
                row = runner.result['stages']['tokens-exception-jit']
                self.assertEqual(row['status'], 'semantic_failed' if red else 'passed')
                self.assertEqual(runner.result['jit_semantic_failed'], ['tokens-exception-jit-run'] if red else [])
                for name in state['checkpoint']['files']:
                    self.assertEqual(runner.frozen[str(runner.out / name)], state['checkpoint']['files'][name])

    def test_process_identity_failures_never_authorize_collection(self):
        bad_records = [dict(exit=2), dict(exit=-9), dict(exit=None), dict(exit=False),
            dict(failure='timeout'), dict(failure='launch-failed'), dict(raw_streams_complete=False),
            dict(streams={}), dict(streams={'stdout': dict(bytes=0, sha256='0' * 64), 'stderr': {}})]
        for changes in bad_records:
            with self.subTest(changes=changes), tempfile.TemporaryDirectory() as temp:
                runner, state = self.checkpoint_runner(Path(temp), changes)
                with self.assertRaises(ValueError): self.resume(runner, state)
                self.assertFalse(runner.result['collection_ready'])
                self.assertEqual(runner.result['stages']['csharp-tests']['status'], 'not-run')
                self.assertTrue((runner.out / 'resume-state.raw.json').is_file())

    def test_compile_and_csharp_commands_are_mandatory_successes(self):
        # 六次CLI和三次工具/C#命令逐个负测，不能仅凭PE/TRX存在越过实际失败。
        data = c.load_manifest()
        names = ['csharp-build', 'scanner-csharp-tests', 'compiler-build', 'attr-list-compile']
        names += [g['id'] + '-compile-command' for g in data['graphs'] if g['native']]
        for name in names:
            for fault in ('missing', 'failed', 'argv'):
                with self.subTest(name=name, fault=fault), tempfile.TemporaryDirectory() as temp:
                    runner, state = self.checkpoint_runner(Path(temp))
                    checkpoint = state['checkpoint']; path = runner.out / (name + '.json')
                    if fault == 'missing': del checkpoint['files'][path.name]
                    else:
                        record = c.parse(path.read_bytes())
                        record.update(exit=1) if fault == 'failed' else record.update(argv=['wrong'])
                        gate.save(path, record); checkpoint['files'][path.name] = c.sha(path)
                    with self.assertRaises(ValueError): self.resume(runner, state)
                    self.assertFalse(runner.result['collection_ready'])

    def test_missing_io_alias_unsafe_path_and_isolation_fail_closed(self):
        for fault in ('missing-stream', 'prefix', 'duplicate-label', 'unsafe', 'symlink', 'isolation', 'source', 'record'):
            with self.subTest(fault=fault), tempfile.TemporaryDirectory() as temp:
                runner, state = self.checkpoint_runner(Path(temp))
                checkpoint = state['checkpoint']; name = 'tokens-exception-jit-run'
                if fault == 'missing-stream': (runner.out / (name + '.stderr')).unlink()
                elif fault == 'prefix': checkpoint['runs'][name]['prefix'] = 'tokens-flow-jit-run'
                elif fault == 'duplicate-label': state['labels'].append(state['labels'][0])
                elif fault == 'unsafe': checkpoint['files']['../escape'] = '0' * 64
                elif fault == 'symlink':
                    (runner.out / 'alias').symlink_to(runner.out / 'tools', target_is_directory=True)
                    checkpoint['files']['alias/ryupy'] = c.sha(runner.cli)
                elif fault == 'isolation': state['isolation']['inputs'] = 'lost'
                elif fault == 'source': checkpoint['runs'][name]['source_sha256'] = '0' * 64
                else: checkpoint['runs'][name]['record']['exit'] = 1
                with self.assertRaises(ValueError): self.resume(runner, state)
                self.assertFalse(runner.result['collection_ready'])

    def test_failed_resume_invalidates_without_claimed_pass_and_keeps_original(self):
        with tempfile.TemporaryDirectory() as temp:
            runner, state = self.checkpoint_runner(Path(temp), dict(exit=-9))
            runner.native_graph = lambda graph: self.fail('坏身份不能进入Native')
            self.assertEqual(self.resume(runner, state, execute=True), 1)
            self.assertTrue(runner.identity_failed)
            self.assertFalse(runner.result['collection_ready'])
            self.assertNotIn('untrusted', runner.result['stages'])
            original = (runner.out / 'resume-state.raw.json').read_bytes()
            (runner.out / 'state.json').write_bytes(b'{}')
            with self.assertRaises(FileExistsError): runner.preserve_resume()
            self.assertEqual((runner.out / 'resume-state.raw.json').read_bytes(), original)

    def test_timeout_observation_recomputed_and_malformed_stream_stays_red(self):
        for text, observed in [*c.TIMEOUT_OUTPUTS.items(), ('resource-timeout NI_OK\n', None)]:
            with self.subTest(text=text), tempfile.TemporaryDirectory() as temp:
                runner, state = self.checkpoint_runner(Path(temp))
                name = 'negative-timeout-jit-run'; row = state['checkpoint']['runs'][name]
                row['record'] = write_process(runner.out, name, row['argv'], text.encode())
                for suffix in ('stdout', 'stderr', 'json', 'argv.json'):
                    path = runner.out / (name + '.' + suffix); state['checkpoint']['files'][path.name] = c.sha(path)
                self.resume(runner, state)
                actual = runner.result['stages']['negative-timeout-jit']
                self.assertTrue(runner.result['collection_ready'])
                self.assertEqual(actual['passed'], observed is not None)
                self.assertEqual(actual.get('timeout_observed'), observed)

    def test_current_frozen_sources_and_separate_accounts(self):
        data = c.load_manifest()
        c.qualified_oracles(data)
        self.assertEqual(sum(x['mode'] == 'candidate' for x in data['graphs']), 4)
        self.assertEqual(sum(x['mode'] == 'pending' for x in data['graphs']), 1)
        self.assertEqual(sum(x['mode'] == 'ni' for x in data['graphs']), 1)
        self.assertEqual(data['real_workload']['stdout_bytes'], 202)

    def test_exact_writer_facts_and_small_native_graph_count(self):
        data = c.load_manifest()
        names = []
        for path in c.ROOT.glob('runtime/RyuPy.Runtime.Tests/PyRegexScanner*Tests.cs'):
            names += ['PyRegexScannerTests.' + name for name in re.findall(r'\[Fact\]\s+public void (\w+)\(', path.read_text())]
        self.assertCountEqual(names, data['csharp']['facts'])
        self.assertEqual(len(names), 43)
        native = [graph for graph in data['graphs'] if graph['native']]
        self.assertEqual(len(native), 5)
        self.assertEqual(sum(len(c.native_cases(graph)) for graph in native), 19)
        self.assertEqual(len({graph['source'] for graph in native}), 5)

    def test_new_title_preserves_independent_old_dag_and_exact_guards(self):
        _, values, active = old_tests.select(c.TITLE)
        self.assertEqual(active, old_tests.ORDINARY | {'distribution-metadata', 'textwrap-dedent', 're-scanner'})
        self.assertEqual(values['scanner'], 'true')
        for title in (c.TITLE.upper(), c.TITLE + '\n', 'ordinary'):
            self.assertEqual(old_tests.select(title)[2], old_tests.ORDINARY)
        self.assertNotIn('re-scanner', old_tests.select(c.TITLE, attempt='2')[2])
        env = dict(RUNNER_ENVIRONMENT='github-hosted', GITHUB_ACTIONS='true', GITHUB_EVENT_NAME='push',
                   GITHUB_RUN_ATTEMPT='1', GITHUB_REF=c.shared.old.shared.REF, GITHUB_SHA='a' * 40,
                   RYUPY_VALIDATION_MODE='distribution-metadata')
        event = dict(ref=env['GITHUB_REF'], after=env['GITHUB_SHA'], head_commit=dict(message=c.TITLE, id=env['GITHUB_SHA']))
        before = copy.deepcopy(event)
        c.check_context(env, event); c.shared.check_context(env, event); c.shared.metadata.check_context(env, event)
        self.assertEqual(event, before)
        for key, value in [('GITHUB_RUN_ATTEMPT', '2'), ('GITHUB_EVENT_NAME', 'workflow_dispatch'),
                           ('RUNNER_ENVIRONMENT', 'self-hosted'), ('RUSTFLAGS', '-A warnings')]:
            for checker in (c.check_context, c.shared.check_context, c.shared.metadata.check_context):
                with self.assertRaises(ValueError): checker(dict(env, **{key: value}), event)
        for title in (c.TITLE.upper(), c.TITLE + '\n'):
            for checker in (c.check_context, c.shared.check_context, c.shared.metadata.check_context):
                with self.assertRaises(ValueError): checker(env, dict(event, head_commit=dict(event['head_commit'], message=title)))

    def test_business_native_uses_existing_legacy_not_invalid_instance(self):
        data = c.load_manifest()
        runner = types.SimpleNamespace(cli=Path('/cli'), inputs_dir=Path('/inputs'), data=data)
        jit = list(map(str, workload.argv(runner, Path('/output'))))
        native = list(map(str, workload.argv(runner, Path('/output'), True)))
        self.assertIn('--instance-module', jit)
        self.assertNotIn('--instance-module', native)
        self.assertEqual(native[-1], '--aot')
        self.assertEqual([native[i + 1] for i, token in enumerate(native) if token == '--include-module'],
                         ['html.parser', 'markdown.extensions.attr_list'])
        self.assertNotIn('markdown.extensions.extra', native)
        workflow = (c.ROOT / '.github/retired-workflows/re-scanner.yml').read_text()
        self.assertLess(workflow.index('scanner-checkpoint-'), workflow.index('--phase native'))
        self.assertIn('**/native-dist.tar.gz', workflow)
        self.assertNotIn('continue-on-error', workflow)

    def test_nonzero_signal_timeout_incomplete_and_missing_streams_fail(self):
        with tempfile.TemporaryDirectory() as temp:
            prefix = Path(temp) / 'ni'
            prefix.with_suffix('.stdout').write_bytes(b'ni-consumers NI_OK\n')
            prefix.with_suffix('.stderr').write_bytes(b'')
            record = dict(exit=0, failure=None, raw_streams_complete=True)
            c.verify_output(record, prefix, 'ni-consumers NI_OK\n')
            for change in (dict(exit=1), dict(exit=-9), dict(failure='timeout'), dict(raw_streams_complete=False)):
                with self.assertRaises(ValueError):
                    c.verify_output(dict(record, **change), prefix, 'ni-consumers NI_OK\n')
            for suffix in ('stdout', 'stderr'):
                path = prefix.with_suffix('.' + suffix)
                content = path.read_bytes()
                path.unlink()
                with self.assertRaises((ValueError, FileNotFoundError)):
                    c.verify_output(record, prefix, 'ni-consumers NI_OK\n')
                path.write_bytes(content + b'wrong')
                with self.assertRaises(ValueError):
                    c.verify_output(record, prefix, 'ni-consumers NI_OK\n')
                path.write_bytes(content)

    def test_oracle_rejects_changed_source_or_python_and_ni(self):
        graph = c.load_manifest()['graphs'][0]
        with tempfile.TemporaryDirectory() as temp:
            prefix = Path(temp) / 'cp'
            prefix.with_suffix('.stdout').write_text(graph['expected_stdout'])
            prefix.with_suffix('.stderr').write_bytes(b'')
            record = dict(exit=0, failure=None, raw_streams_complete=True)
            c.verify_oracle(graph, record, prefix, c.shared.old.shared.PYTHON_SHA, graph['source_sha256'])
            for python, source in [('0' * 64, graph['source_sha256']), (c.shared.old.shared.PYTHON_SHA, '0' * 64)]:
                with self.assertRaises(ValueError):
                    c.verify_oracle(graph, record, prefix, python, source)
            with self.assertRaises(ValueError):
                c.verify_oracle(dict(graph, mode='ni'), record, prefix, c.shared.old.shared.PYTHON_SHA, graph['source_sha256'])

    def test_old_resume_state_never_inherits_passed(self):
        with tempfile.TemporaryDirectory() as temp:
            root = Path(temp)
            for name, content in [('run.stdout', b'OK\n'), ('run.stderr', b''), ('entry.dll', b'PE'), ('tests.trx', b'TRX')]:
                (root / name).write_bytes(content)
            identity = dict(head='a' * 40, tree='b' * 40, run_id='123', run_attempt='1',
                            manifest_sha256='c' * 64, sources={'entry.py': 'd' * 64}, tools={'runtime': 'e' * 64})
            expected = {'run': {'argv': ['dotnet', 'entry.dll', 'flow'], 'source_sha256': 'd' * 64, 'stdout': 'OK\n'}}
            record = write_process(root, 'run', expected['run']['argv'])
            state = {'schema_version': 1, 'identity': identity,
                     'runs': {'run': {'record': record,
                                     'prefix': 'run', 'argv': expected['run']['argv'], 'source_sha256': 'd' * 64}},
                     'files': {p.name: c.sha(p) for p in root.iterdir()}}
            self.assertTrue(c.verify_checkpoint(state, identity, expected, root)['run']['passed'])
            c.verify_checkpoint(state, identity, expected, root, {'entry.dll', 'tests.trx'})
            missing = copy.deepcopy(state); del missing['files']['entry.dll']
            with self.assertRaises(ValueError):
                c.verify_checkpoint(missing, identity, expected, root, {'entry.dll', 'tests.trx'})
            mutations = []
            for key, value in [('head', 'f' * 40), ('tree', 'f' * 40), ('run_id', '122'),
                               ('run_attempt', '2'), ('manifest_sha256', 'f' * 64),
                               ('sources', {}), ('tools', {})]:
                bad = copy.deepcopy(state); bad['identity'][key] = value; mutations.append(bad)
            bad = copy.deepcopy(state); bad['runs']['run']['record']['exit'] = -11; mutations.append(bad)
            bad = copy.deepcopy(state); del bad['files']['run.stderr']; mutations.append(bad)
            bad = copy.deepcopy(state); bad['runs']['run']['passed'] = True; mutations.append(bad)
            bad = copy.deepcopy(state); bad['runs']['run']['source_sha256'] = 'f' * 64; mutations.append(bad)
            bad = copy.deepcopy(state); del bad['files']['run.json']; mutations.append(bad)
            bad = copy.deepcopy(state); del bad['files']['run.argv.json']; mutations.append(bad)
            for bad in mutations:
                with self.assertRaises(ValueError):
                    c.verify_checkpoint(bad, identity, expected, root)
                reset = c.fail_closed(['jit', 'native'], '本轮身份未通过')
                self.assertTrue(all(not value['passed'] for value in reset.values()))
            # 即使state为修改过的原record重算hash，也不能把其exit1或错argv伪装成成功record。
            for changes in (dict(exit=1), dict(argv=['other-command'])):
                raw = json.dumps(dict(record, **changes))
                (root / 'run.json').write_text(raw)
                forged = copy.deepcopy(state); forged['files']['run.json'] = c.sha(root / 'run.json')
                with self.assertRaises(ValueError):
                    c.verify_checkpoint(forged, identity, expected, root)
            (root / 'run.json').write_text(json.dumps(record))
            (root / 'entry.dll').write_bytes(b'other PE')
            with self.assertRaises(ValueError):
                c.verify_checkpoint(state, identity, expected, root)

    def test_all_twenty_jit_records_required_before_native(self):
        data = c.load_manifest()
        expected = {}
        for graph in data['graphs']:
            if not graph['native']: continue
            for row in c.native_cases(graph):
                expected[row['id'] + '-jit-run'] = dict(argv=['dotnet', graph['id'] + '.dll', *row['argv']],
                    stdout=row['stdout'], source_sha256=graph['source_sha256'], observation=row['observation'])
        expected['attr-list'] = dict(argv=['dotnet', 'attr-list.dll'],
            stdout=(c.ROOT / data['real_workload']['golden']).read_text(), source_sha256=data['real_workload']['source_sha256'])
        self.assertEqual(len(expected), 20)
        with tempfile.TemporaryDirectory() as temp:
            root = Path(temp)
            identity = dict(head='a', tree='b', run_id='123', run_attempt='1', manifest_sha256='c', sources={}, tools={})
            state = dict(schema_version=1, identity=identity, runs={}, files={})
            for name, row in expected.items():
                record = write_process(root, name, row['argv'], row['stdout'].encode())
                state['runs'][name] = dict(record=record, prefix=name, argv=row['argv'], source_sha256=row['source_sha256'])
                for suffix in ('stdout', 'stderr', 'json', 'argv.json'):
                    path = root / (name + '.' + suffix); state['files'][path.name] = c.sha(path)
            self.assertEqual(len(c.verify_checkpoint(state, identity, expected, root)), 20)
            for removed in (['tokens-flow-jit-run'], [name for name in expected if name.startswith('tokens-')], ['attr-list']):
                missing = copy.deepcopy(state)
                for name in removed: del missing['runs'][name]
                with self.assertRaises(ValueError): c.verify_checkpoint(missing, identity, expected, root)
            failed = copy.deepcopy(state); failed['runs']['tokens-flow-jit-run']['record']['exit'] = 1
            path = root / 'tokens-flow-jit-run.json'; path.write_text(json.dumps(failed['runs']['tokens-flow-jit-run']['record']))
            failed['files'][path.name] = c.sha(path)
            actual = c.verify_checkpoint(failed, identity, expected, root)
            self.assertFalse(actual['tokens-flow-jit-run']['passed'])
            self.assertEqual(actual['tokens-flow-jit-run']['status'], 'semantic_failed')

    def test_exact_timeout_branches_never_accept_process_failure(self):
        with tempfile.TemporaryDirectory() as temp:
            prefix = Path(temp) / 'timeout'
            prefix.with_suffix('.stderr').write_bytes(b'')
            record = dict(exit=0, failure=None, raw_streams_complete=True)
            for text, observed in c.TIMEOUT_OUTPUTS.items():
                prefix.with_suffix('.stdout').write_text(text)
                self.assertIs(c.verify_timeout_output(record, prefix)['timeout_observed'], observed)
                for changes in (dict(exit=1), dict(exit=-9), dict(failure='timeout'), dict(raw_streams_complete=False)):
                    with self.assertRaises(ValueError): c.verify_timeout_output(dict(record, **changes), prefix)
            prefix.with_suffix('.stdout').write_text('resource-timeout NI_OK\n')
            with self.assertRaises(ValueError): c.verify_timeout_output(record, prefix)

    def test_failed_csharp_process_cannot_pass_by_trx_alone(self):
        with tempfile.TemporaryDirectory() as temp:
            prefix = Path(temp) / 'scanner-csharp-tests'
            argv = ['dotnet', 'test', 'tests.csproj', '--no-build']
            prefix.with_suffix('.stdout').write_text('43 Passed\n')
            prefix.with_suffix('.stderr').write_bytes(b'')
            prefix.with_suffix('.argv.json').write_text(json.dumps(dict(argv=argv)))
            record = dict(argv=argv, exit=0, failure=None, raw_streams_complete=True,
                streams={name: dict(bytes=prefix.with_suffix('.' + name).stat().st_size,
                                    sha256=c.sha(prefix.with_suffix('.' + name))) for name in ('stdout', 'stderr')})
            prefix.with_suffix('.json').write_text(json.dumps(record))
            c.verify_command(prefix, argv)
            for changes in (dict(exit=1), dict(exit=-9), dict(failure='timeout'), dict(raw_streams_complete=False),
                            dict(argv=['other-command'])):
                prefix.with_suffix('.json').write_text(json.dumps(dict(record, **changes)))
                with self.assertRaises(ValueError): c.verify_command(prefix, argv)

    def test_nested_native_failure_persists_only_at_stable_job_root(self):
        with tempfile.TemporaryDirectory() as temp:
            root = Path(temp); nested = root / 'native-domains/graph'; nested.mkdir(parents=True)
            runner = gate.Runner.__new__(gate.Runner)
            runner.out, runner.root_out = nested, root
            runner.labels, runner.root_labels = {'native-local'}, {'root-command'}
            runner.cli = runner.runtime = Path('placeholder')
            runner.before, runner.frozen, runner.run_records = {'head': 'a'}, {}, {}
            runner.input_inventories, runner.isolation, runner.snapshot_reports = {}, {}, {}
            runner.result = {'passed': False, 'stages': {}}
            runner.fresh_identity = lambda: {'test_only': True}
            runner.successful_commands = lambda: {}
            runner.persist()
            self.assertTrue((root / 'state.json').is_file())
            self.assertFalse((nested / 'state.json').exists())
            self.assertEqual(c.parse((root / 'state.json').read_bytes())['labels'], ['root-command'])

    def test_trx_skip_duplicate_zero_and_wrong_name_fail(self):
        with tempfile.TemporaryDirectory() as temp:
            path = Path(temp) / 'test.trx'
            def write(names, status):
                path.write_text('<TestRun xmlns="http://microsoft.com/schemas/VisualStudio/TeamTest/2010">' + ''.join(
                    '<UnitTestResult testName="RyuPy.Runtime.Tests.' + name + '" outcome="' + status + '"/>'
                    for name in names) + '</TestRun>')
            write(['Scanner.ExactFact'], 'Passed')
            c.verify_trx(path, ['Scanner.ExactFact'])
            for names, status in [([], 'Passed'), (['Scanner.ExactFact'] * 2, 'Passed'),
                                  (['Scanner.ExactFact'], 'NotExecuted'), (['Wrong'], 'Passed')]:
                write(names, status)
                with self.assertRaises(ValueError):
                    c.verify_trx(path, ['Scanner.ExactFact'])


if __name__ == '__main__':
    unittest.main()
