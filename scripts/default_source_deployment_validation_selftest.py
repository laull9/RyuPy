"""默认来源部署纯harness自测：不启动Python业务、产品、构建、安装或网络。"""
import copy
import hashlib
from pathlib import Path
import tempfile
import unittest
from unittest.mock import patch
import default_source_deployment_validation as gate
import textwrap_dedent_selftest as selector


class Tests(unittest.TestCase):
    def test_original_parser_and_default_commands(self):
        self.assertEqual(hashlib.sha256(gate.PARSER_SOURCE.encode()).hexdigest(), gate.PARSER_SHA)
        self.assertEqual(hashlib.sha256(gate.PARSER_OUTPUT).hexdigest(), gate.PARSER_OUTPUT_SHA)
        self.assertEqual(len(gate.PARSER_OUTPUT), 48)
        self.assertEqual(gate.sha(gate.ROOT / gate.shared.A_PATH), gate.shared.A_SHA)
        self.assertEqual(gate.sha(gate.ROOT / 'tests/projects/entries/markdown_sample.py'), gate.old.ENTRY_SHA)
        plans = gate.command_plan('ryupy', 'dotnet', Path('/inputs'), Path('/out'))
        self.assertEqual(plans['parser']['compile'], ['ryupy', 'build', '/inputs/parser_isolation.py', '--out-dir', '/out/parser'])
        self.assertEqual(plans['markdown']['compile'], ['ryupy', 'build', '/inputs/markdown_sample.py', '--out-dir', '/out/markdown', '-I', '/inputs/markdown'])
        for plan in plans.values():
            for forbidden in ('--include-module', '--instance-module', '--distribution-metadata', 'stdlib', '--trace-product-v1'):
                self.assertNotIn(forbidden, str(plan))

    def test_business_failure_does_not_skip_other_new_gates(self):
        for failure in ('parser', 'markdown', None):
            names = ('parser', 'markdown')
            result = {'stages': {name: {'passed': False} for name in names}}
            calls, checkpoints = [], []
            def action(name):
                calls.append(name)
                if name == failure: raise ValueError('真实业务红灯')
            passed = gate.run_independent(names, action, result, lambda: checkpoints.append(copy.deepcopy(result)))
            self.assertEqual(calls, list(names))
            self.assertEqual(passed, failure is None)
            self.assertEqual(len(checkpoints), 4)
            if failure: self.assertEqual(result['stages'][failure]['status'], 'failed')

    def test_shared_identity_failure_stops_and_never_passes(self):
        result = {'stages': {'parser': {'passed': False}, 'markdown': {'passed': False}}}
        calls = []
        def action(name):
            calls.append(name); raise gate.shared.IdentityError('来源异源')
        with self.assertRaises(gate.shared.IdentityError):
            gate.run_independent(result['stages'], action, result, lambda: None)
        self.assertEqual(calls, ['parser'])
        self.assertFalse(result['stages']['parser']['passed'])
        self.assertFalse(result['stages']['markdown']['passed'])

    def test_real_dag_fanout_and_all_unchanged_old_job_hashes(self):
        self.assertEqual(selector.select(gate.TITLE)[2], selector.ORDINARY | {'default-source-deployment'})
        for title in (gate.TITLE.upper(), gate.TITLE + '\n', 'ordinary'):
            self.assertEqual(selector.select(title)[2], selector.ORDINARY)
        for options in ({'event': 'workflow_dispatch'}, {'attempt': '2'}):
            self.assertEqual(selector.select(gate.TITLE, **options)[2], selector.ORDINARY)
        _, blocks = selector.jobs()
        for name, digest in selector.DATA['unchanged_job_blocks'].items():
            self.assertEqual(selector.protected_job_digest(name, blocks[name]), digest, name)
        for name in selector.ORDINARY:
            self.assertIn('    needs: select-mode\n', blocks[name])
            self.assertNotIn('needs: default-source-deployment', blocks[name])
        child = (gate.ROOT / '.github/workflows/default-source-deployment.yml').read_text()
        self.assertNotIn('continue-on-error', child)
        self.assertIn('if: always()', child)
        for name in ('wcwidth-legacy-aot', 'default-source-deployment', 'trace-edge-diagnostic'):
            self.assertLessEqual(len((gate.ROOT / ('.github/workflows/' + name + '.yml')).read_text().splitlines()), 600)

    def test_migrated_diagnostic_cannot_expand_permissions_or_change_steps(self):
        _, blocks = selector.jobs()
        original = blocks['trace-edge-diagnostic']
        for mutation in (original.replace('select-mode', 'default-source-deployment'),
                         original.replace("== 'true'", "!= 'true'"), original + '    secrets: inherit\n'):
            with self.assertRaises(ValueError): selector.protected_job_digest('trace-edge-diagnostic', mutation)
        path = gate.ROOT / '.github/workflows/trace-edge-diagnostic.yml'
        text = path.read_text(); read_text = Path.read_text
        for mutation in (text.replace('contents: read', 'contents: write'),
                         text.replace('workflow_call:', 'workflow_call:\n    inputs: {}')):
            def fake(path, *args, **kwargs):
                return mutation if path.name == 'trace-edge-diagnostic.yml' else read_text(path, *args, **kwargs)
            with patch.object(Path, 'read_text', fake), self.assertRaises(ValueError):
                selector.protected_job_digest('trace-edge-diagnostic', original)
        mutation = text.replace('--rust-only', '--changed')
        with patch.object(Path, 'read_text', lambda path: mutation):
            self.assertNotEqual(selector.protected_job_digest('trace-edge-diagnostic', original),
                                selector.DATA['unchanged_job_blocks']['trace-edge-diagnostic'])

    def test_actual_plan_policy_deployed_bytes_and_metadata_are_required(self):
        import json
        with tempfile.TemporaryDirectory() as temp:
            root = Path(temp); inputs = root / 'inputs'; output = root / 'product'
            metadata = inputs / 'markdown/Markdown.egg-info'; metadata.mkdir(parents=True)
            (metadata / 'entry_points.txt').write_text('[console_scripts]\nmarkdown=markdown.__main__:run\n')
            output.mkdir()
            rows = [{'module': 'html.parser', 'compiled': True, 'execution': 'InstanceGlobals',
                     'ordinary_name_handlers': True, 'relative_from_import': True, 'candidate': True,
                     'catalog_authenticated': True, 'hard': True, 'reason': 'compiler-catalog-authenticated-source'}]
            plan = dict(compiled_feature=True, session_policy='ordinary-dynamic-module-pilot', modules=rows)
            gate.save(output / 'module-environment.json', plan)
            hashes = {}
            for name in gate.old.STDLIB:
                path = output / 'compiler-sources/family' / name
                path.parent.mkdir(parents=True, exist_ok=True); path.write_text(name)
                hashes[name] = gate.sha(path)
            report = dict(mode='closed_snapshot', distributions=[dict(path='Markdown.egg-info',
                shadowed=False, directories=[], files=[dict(path='entry_points.txt', size=(metadata / 'entry_points.txt').stat().st_size,
                sha256=gate.sha(metadata / 'entry_points.txt'))])])
            gate.save(output / 'distribution-metadata.json', report)
            with patch.dict(gate.old.STDLIB, hashes, clear=True):
                self.assertEqual(gate.verify_audit(output, inputs, 'markdown')['module_plan'], plan)
                bad = dict(plan, session_policy='explicit-only')
                gate.save(output / 'module-environment.json', bad)
                with self.assertRaises(ValueError): gate.verify_audit(output, inputs, 'markdown')
                gate.save(output / 'module-environment.json', plan)
                report['distributions'][0]['files'] = []
                gate.save(output / 'distribution-metadata.json', report)
                with self.assertRaises(ValueError): gate.verify_audit(output, inputs, 'markdown')
                (output / 'compiler-sources/family/html/parser.py').write_text('tampered')
                with self.assertRaises(ValueError): gate.verify_audit(output, inputs, 'parser')

    def test_no_unfrozen_default_aot_window_or_success_claim(self):
        import inspect
        source = inspect.getsource(gate.Runner)
        self.assertNotIn("'--aot'", source)
        self.assertNotIn('aot_boundary', source)
        self.assertIn("'status': 'not-run', 'support': 'unsupported-not-promoted'", source)
        self.assertIn("('parser', 'markdown')", source)

    def test_production_probe_source_names_and_rust_zero_filter_rejection(self):
        probes = gate.probes
        manifest = probes.source_manifest(gate.ROOT)
        self.assertEqual(len(manifest['rust_default']), 13)
        self.assertEqual(manifest['rust_default'], manifest['rust_pilot'])
        rows = ['test ' + name + ' ... ok' for name in probes.RUST_NAMES]
        summary = 'test result: ok. 13 passed; 0 failed; 0 ignored; 0 measured; 100 filtered out; finished in 0.1s'
        text = '\n'.join(rows + [summary])
        self.assertEqual(probes.verify_rust(text)['total'], 13)
        for wrong in (summary.replace('13 passed', '0 passed'), '\n'.join(rows[1:] + [summary]),
                      text + '\n' + rows[0], text.replace(' ... ok', ' ... ignored', 1),
                      text.replace(' ... ok', ' ... FAILED', 1), text + '\n' + summary):
            with self.assertRaises(ValueError): probes.verify_rust(wrong)

    def test_production_runtime_trx_exact_method_counters_and_outcomes(self):
        import xml.etree.ElementTree as ET
        probes = gate.probes
        with tempfile.TemporaryDirectory() as temp:
            path = Path(temp) / 'result.trx'
            def document():
                root = ET.Element('TestRun', xmlns='http://microsoft.com/schemas/VisualStudio/TeamTest/2010')
                results = ET.SubElement(root, 'Results')
                ET.SubElement(results, 'UnitTestResult', testName=probes.CS_NAMES[0], testId='id', outcome='Passed')
                defs = ET.SubElement(root, 'TestDefinitions')
                definition = ET.SubElement(defs, 'UnitTest', id='id')
                ET.SubElement(definition, 'TestMethod', className=probes.CS_CLASS, name=probes.CS_METHOD)
                summary = ET.SubElement(root, 'ResultSummary', outcome='Completed')
                ET.SubElement(summary, 'Counters', total='1', executed='1', passed='1', failed='0')
                return root
            root = document(); ET.ElementTree(root).write(path)
            self.assertEqual(probes.verify_trx(path)['total'], 1)
            for target, field, value in [('Results/UnitTestResult', 'outcome', 'NotExecuted'),
                    ('Results/UnitTestResult', 'outcome', 'Failed'), ('Results/UnitTestResult', 'testName', 'Other'),
                    ('Results/UnitTestResult', 'testId', 'wrong'), ('TestDefinitions/UnitTest/TestMethod', 'name', 'Other'),
                    ('ResultSummary/Counters', 'total', '0'), ('ResultSummary/Counters', 'failed', '1')]:
                root = document(); root.find(target).set(field, value); ET.ElementTree(root).write(path)
                with self.assertRaises(ValueError): probes.verify_trx(path)
            root = document(); root.find('Results').clear(); ET.ElementTree(root).write(path)
            with self.assertRaises(ValueError): probes.verify_trx(path)
            root = document(); root.find('Results').append(copy.deepcopy(root.find('Results/UnitTestResult')))
            ET.ElementTree(root).write(path)
            with self.assertRaises(ValueError): probes.verify_trx(path)

    def test_production_probe_commands_both_rust_policies_and_process_failures(self):
        from types import SimpleNamespace
        from unittest.mock import Mock
        probes = gate.probes
        with tempfile.TemporaryDirectory() as temp:
            out = Path(temp)
            text = '\n'.join('test ' + name + ' ... ok' for name in probes.RUST_NAMES)
            text += '\ntest result: ok. 13 passed; 0 failed; 0 ignored; 0 measured; 1 filtered out; finished in 1s\n'
            calls = []
            def command(name, argv, timeout, env):
                calls.append(argv); (out / (name + '.stdout')).write_text(text)
                return dict(exit=0, failure=None, raw_streams_complete=True)
            runner = SimpleNamespace(out=out, command=command, barrier=Mock())
            probes.execute(runner, 'rust-default'); probes.execute(runner, 'rust-pilot')
            self.assertNotIn('--features', calls[0])
            self.assertEqual(calls[1][calls[1].index('--features') + 1], gate.old.FEATURE)
            self.assertTrue(all('--lib' in argv and 'default_source_' in argv for argv in calls))
            for record in [dict(exit=-11, failure=None, raw_streams_complete=True),
                           dict(exit=0, failure='timeout', raw_streams_complete=True),
                           dict(exit=0, failure=None, raw_streams_complete=False)]:
                runner.command = Mock(return_value=record)
                with self.assertRaises(ValueError): probes.run_command(runner, 'mock', ['tool'], 1)

    def test_fresh_host_exact_output_and_real_runtime_reference(self):
        from types import SimpleNamespace
        from unittest.mock import Mock
        probes = gate.probes
        for wrong in (None, 'runtime', 'stdout', 'stderr', 'old-two-lines'):
            with tempfile.TemporaryDirectory() as temp:
                out = Path(temp); runtime = out / 'RyuPy.Runtime.dll'; runtime.write_bytes(b'same-runtime')
                commands = []
                def command(name, argv, timeout, env):
                    commands.append((name, argv))
                    if name == 'metadata-host-build':
                        binary = out / 'metadata-fresh-host/bin'; binary.mkdir()
                        (binary / 'Probe.dll').write_bytes(b'MZmock')
                        (binary / runtime.name).write_bytes(b'wrong' if wrong == 'runtime' else runtime.read_bytes())
                    else:
                        (out / (name + '.stdout')).write_bytes(b'wrong' if wrong == 'stdout' else (b'import-ok\nrequire-ni\n' if wrong == 'old-two-lines' else b'import-ok\nrequire-ni\nhooks-ni\n'))
                        (out / (name + '.stderr')).write_bytes(b'error' if wrong == 'stderr' else b'')
                    return dict(exit=0, failure=None, raw_streams_complete=True)
                runner = SimpleNamespace(out=out, dotnet=Path('/sdk/dotnet'), runtime=runtime,
                    frozen={}, env={}, command=command, barrier=Mock())
                if wrong:
                    with self.assertRaises(ValueError): probes.execute(runner, 'metadata-fresh-host')
                else:
                    probes.execute(runner, 'metadata-fresh-host')
                    self.assertEqual([name for name, _ in commands], ['metadata-host-build', 'metadata-host-run'])
                    self.assertIn('-p:RuntimePath=' + str(runtime), commands[0][1])
                    self.assertEqual(commands[1][1], [runner.dotnet, out / 'metadata-fresh-host/bin/Probe.dll'])
                    self.assertEqual(gate.sha(out / 'metadata-fresh-host/Program.cs'), gate.sha(gate.ROOT / probes.HOST_SOURCE))

    def test_probes_and_jit_have_independent_status_but_shared_failure_result(self):
        import inspect
        text = inspect.getsource(gate.Runner.execute)
        self.assertIn("unit_result = {'stages': self.result['production_probes']}", text)
        self.assertIn('probes_passed and jit_passed', text)
        self.assertLess(text.index('probes_passed ='), text.index('jit_passed ='))
        self.assertNotIn('if probes_passed:', text)

    def test_candidate_document_hashes_still_reject_any_appended_byte(self):
        import decimal_percent_contract as contract
        original = contract.safe_file
        with tempfile.TemporaryDirectory() as temp:
            for name in ('docs/semantics.md', 'TODO.md'):
                altered = Path(temp) / Path(name).name
                altered.write_bytes((gate.ROOT / name).read_bytes() + b'\nunauthorized suffix\n')
                def resolve(root, path):
                    return altered if path == name else original(root, path)
                with patch.object(contract, 'safe_file', resolve), self.assertRaisesRegex(ValueError, '冻结源改变'):
                    contract.load_manifest()

    def test_missing_unreadable_execution_inputs_hard_stop_later_stages(self):
        from types import SimpleNamespace
        from unittest.mock import Mock
        import shutil
        probes = gate.probes
        real_root, original_sha = gate.ROOT, probes.sha
        for stage, filenames in [('runtime-facts', ('RyuPy.Runtime.dll', 'RyuPy.Runtime.Tests.dll')),
                                 ('metadata-fresh-host', ('RyuPy.Runtime.dll', 'Probe.dll'))]:
            for filename in filenames:
                for defect in ('missing', 'unreadable'):
                    with self.subTest(stage=stage, filename=filename, defect=defect), tempfile.TemporaryDirectory() as temp:
                        root = Path(temp); out = root / 'out'; out.mkdir()
                        runtime = root / 'reference/RyuPy.Runtime.dll'; runtime.parent.mkdir(); runtime.write_bytes(b'Runtime')
                        for source in (probes.HOST_SOURCE, probes.HOST_PROJECT):
                            target = root / source; target.parent.mkdir(parents=True, exist_ok=True)
                            shutil.copyfile(real_root / source, target)
                        binary = (root / 'runtime/RyuPy.Runtime.Tests/bin/Release/net10.0' if stage == 'runtime-facts'
                                  else out / 'metadata-fresh-host/bin')
                        calls = []
                        def command(name, argv, timeout, env):
                            calls.append(name); binary.mkdir(parents=True, exist_ok=True)
                            for item in filenames:
                                if item != filename or defect != 'missing':
                                    (binary / item).write_bytes(b'Runtime' if item == 'RyuPy.Runtime.dll' else b'MZ')
                            (out / (name + '.stdout')).write_bytes(b'build completed\n')
                            (out / (name + '.stderr')).write_bytes(b'')
                            return dict(exit=0, failure=None, raw_streams_complete=True)
                        def hashing(path):
                            if Path(path) == binary / filename and defect == 'unreadable': raise PermissionError('probe unreadable')
                            return original_sha(path)
                        runner = SimpleNamespace(out=out, dotnet=Path('/sdk/dotnet'), runtime=runtime,
                            frozen={}, env={}, command=command, barrier=Mock())
                        result = {'stages': {name: {'passed': False} for name in (stage, 'next-jit')}}
                        def action(name):
                            if name == 'next-jit': calls.append('next-jit')
                            else: probes.execute(runner, name)
                        with patch.object(probes.shared, 'REPO', root), patch.object(probes, 'sha', hashing):
                            with self.assertRaises(gate.shared.IdentityError):
                                gate.run_independent(result['stages'], action, result, lambda: None)
                        self.assertEqual(calls, ['runtime-probe-build' if stage == 'runtime-facts' else 'metadata-host-build'])
                        self.assertEqual(result['stages'][stage]['status'], 'invalidated-identity')
                        self.assertFalse(result['stages']['next-jit']['passed'])
                        self.assertTrue((out / (calls[0] + '.stdout')).is_file())

    def test_real_test_failure_still_continues_independent_stage(self):
        from types import SimpleNamespace
        from unittest.mock import Mock
        probes = gate.probes
        with tempfile.TemporaryDirectory() as temp:
            root = Path(temp); out = root / 'out'; out.mkdir()
            runtime = root / 'RyuPy.Runtime.dll'; runtime.write_bytes(b'Runtime')
            binary = root / 'runtime/RyuPy.Runtime.Tests/bin/Release/net10.0'; binary.mkdir(parents=True)
            (binary / runtime.name).write_bytes(runtime.read_bytes()); (binary / 'RyuPy.Runtime.Tests.dll').write_bytes(b'MZ')
            calls = []
            def command(name, argv, timeout, env):
                calls.append(name)
                (out / (name + '.stdout')).write_text('real test failure' if name == 'runtime-facts' else 'build ok')
                (out / (name + '.stderr')).write_text('')
                return dict(exit=1 if name == 'runtime-facts' else 0, failure=None, raw_streams_complete=True)
            runner = SimpleNamespace(out=out, dotnet=Path('/sdk/dotnet'), runtime=runtime,
                frozen={}, env={}, command=command, barrier=Mock())
            result = {'stages': {name: {'passed': False} for name in ('runtime-facts', 'next-jit')}}
            def action(name):
                if name == 'next-jit': calls.append(name)
                else: probes.execute(runner, name)
            with patch.object(probes.shared, 'REPO', root):
                self.assertFalse(gate.run_independent(result['stages'], action, result, lambda: None))
            self.assertEqual(calls, ['runtime-probe-build', 'runtime-facts', 'next-jit'])
            self.assertEqual(result['stages']['runtime-facts']['status'], 'failed')
            self.assertTrue(result['stages']['next-jit']['passed'])

    def test_jit_missing_execution_inputs_are_identity_failures(self):
        from types import SimpleNamespace
        from unittest.mock import Mock
        for missing in ('parser_isolation.dll', 'RyuPy.Runtime.dll'):
            with tempfile.TemporaryDirectory() as temp:
                out = Path(temp); output = out / 'products/parser'; output.mkdir(parents=True)
                for name in ('parser_isolation.dll', 'RyuPy.Runtime.dll'):
                    if name != missing: (output / name).write_bytes(b'MZ')
                reference = out / 'reference.dll'; reference.write_bytes(b'MZ')
                runner = SimpleNamespace(out=out, runtime=reference, env={}, barrier=Mock(),
                    plans={'parser': {'compile': ['ryupy'], 'jit': ['dotnet', str(output / 'parser_isolation.dll')]}},
                    command=Mock(return_value=dict(exit=0, failure=None, raw_streams_complete=True)))
                with self.assertRaises(gate.shared.IdentityError): gate.Runner.product(runner, 'parser')
                self.assertEqual(runner.command.call_count, 1)

    def test_archive_original_members_without_execution(self):
        with tempfile.TemporaryDirectory() as temp:
            root = Path(temp) / 'frozen'
            manifest = gate.shared.unpack(gate.shared.BUNDLE, root)
            self.assertEqual(len(gate.shared.targets(root / 'inputs')), 18)
            self.assertEqual((root / 'reference/markdown-full-cp-run.stdout').stat().st_size, 77)
            self.assertEqual(len(manifest['files']) + 1, 54)


if __name__ == '__main__':
    unittest.main()
