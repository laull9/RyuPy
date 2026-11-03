"""构造AOT编排轻测：只用mock/普通文件，不启动SDK、CP参考或被测程序。"""
import copy
import hashlib
import json
import os
from pathlib import Path
import subprocess
import tempfile
import shutil
from contextlib import ExitStack
import unittest
from unittest import mock

import construction_smoke as harness


FROZEN = {
    'positive': {'stdout': 'first\nall-ok\n', 'exit': 1,
        'frames': [['construction.py', 91, '<module>'], ['construction.py', 88, 'caller'], ['construction.py', 86, '__new__']],
        'exception': 'ValueError: new-body'},
    'ryupy-boundaries': {'stdout': 'boundary-ok\n', 'exit': 1,
        'frames': [['construction.py', 99, '<module>'], ['construction.py', 97, 'boundary_caller']],
        'exception': 'NotImplementedError: deleting finalized __new__ is not supported'}}


def output(mode='positive', *, python=False, directory='/old/source'):
    expected = FROZEN[mode]
    lines = ['Traceback (most recent call last):']
    for name, line, function in expected['frames']:
        lines.append(f'  File "{directory}/{name}", line {line}, in {function}')
        if python: lines.append('    relevant_source()')
    lines.append(expected['exception'])
    return subprocess.CompletedProcess([], 1, expected['stdout'].replace('\n', os.linesep).encode(),
                                       ('\n'.join(lines) + '\n').encode())


class OutputTests(unittest.TestCase):
    def setUp(self):
        patch = mock.patch.object(harness, 'EXPECTED', copy.deepcopy(FROZEN))
        patch.start(); self.addCleanup(patch.stop)

    def test_two_modes_require_all_markers_exact_frames_and_final_exception(self):
        for mode in harness.MODES:
            self.assertEqual(harness.signature(output(mode), mode)['frames'], FROZEN[mode]['frames'])
            for changed in ('marker', 'exit', 'line', 'exception', 'missing-frame', 'extra-frame', 'chain', 'clr', 'prefix', 'junk'):
                actual = output(mode)
                if changed == 'marker': actual.stdout = b''
                elif changed == 'exit': actual.returncode = 0
                elif changed == 'line': actual.stderr = actual.stderr.replace(b'line 9', b'line 8', 1)
                elif changed == 'exception': actual.stderr = actual.stderr.replace(b'ValueError:', b'TypeError:').replace(b'NotImplementedError:', b'TypeError:')
                elif changed == 'missing-frame': actual.stderr = b'\n'.join(actual.stderr.split(b'\n')[:1] + actual.stderr.split(b'\n')[2:])
                elif changed == 'extra-frame': actual.stderr = actual.stderr.replace(b'Traceback (most recent call last):\n', b'Traceback (most recent call last):\n  File "construction.py", line 2, in fake\n')
                elif changed == 'chain': actual.stderr = b'Traceback (most recent call last):\nValueError: active\nDuring handling of the above exception, another exception occurred:\n' + actual.stderr
                elif changed == 'clr': actual.stderr = b'Unhandled exception\n' + actual.stderr
                elif changed == 'prefix': actual.stderr = b'ignored-prefix\n' + actual.stderr
                else: actual.stderr = actual.stderr.replace(b'Traceback (most recent call last):\n', b'Traceback (most recent call last):\nunknown failure\n')
                with self.subTest(mode=mode, changed=changed), self.assertRaises(AssertionError):
                    harness.signature(actual, mode)

    def test_python_projection_allows_only_source_lines_and_physical_prefix(self):
        reference = output(python=True, directory='/reference')
        actual = output(directory='/compiled')
        harness.compare(reference, actual, 'positive', python=True)
        with self.assertRaises(AssertionError): harness.signature(reference, 'positive')
        with self.assertRaises(AssertionError): harness.compare(output('ryupy-boundaries'), output('ryupy-boundaries'), 'ryupy-boundaries', python=True)

    def test_jit_native_requires_raw_stderr_even_when_frames_equal(self):
        harness.compare(output(), output(), 'positive')
        with self.assertRaises(AssertionError):
            harness.compare(output(directory='/a'), output(directory='/b'), 'positive')
        actual = output(); actual.stderr = actual.stderr.replace(b'\n', b'\r\n')
        with self.assertRaises(AssertionError): harness.compare(output(), actual, 'positive')

    def test_actual_stdout_never_normalized(self):
        actual = output(); actual.stdout = actual.stdout.replace(b'\r\n', b'\n') if os.linesep == '\r\n' else actual.stdout.replace(b'\n', b'\r\n')
        with self.assertRaises(AssertionError): harness.signature(actual, 'positive')
        with mock.patch.object(harness.os, 'linesep', '\r\n'):
            self.assertEqual(harness.expected_stdout('positive'), b'first\r\nall-ok\r\n')

    def test_matching_wrong_results_do_not_pass(self):
        wrong = output(); wrong.stdout = b'missing-all-markers\n'
        with self.assertRaises(AssertionError): harness.compare(wrong, copy.copy(wrong), 'positive')


class BudgetTests(unittest.TestCase):
    def make_budget(self, start=0):
        self.now = start
        runner = mock.Mock()
        record = {}
        budget = harness.Budget(runner, start, record, clock=lambda: self.now)
        return budget, runner, record

    def test_phase_and_total_deadlines_do_not_reset(self):
        budget, runner, record = self.make_budget()
        self.now = 100
        with budget.phase('runtime'):
            self.assertEqual(runner.deadline, 280)
            budget.command('runtime', ['unused'], cwd='.', timeout=180)
            self.now = 279
        self.now = 1750
        with budget.phase('publish'):
            self.assertEqual(runner.deadline, 1800)
            budget.command('publish', ['unused'], cwd='.', timeout=600)
            self.now = 1799
        self.assertEqual(runner.run.call_count, 2)
        self.assertEqual(record['phases']['runtime']['seconds'], 179)
        self.now = 1800
        with self.assertRaises(TimeoutError), budget.phase('applications'):
            budget.command('not-launched', ['unused'], cwd='.')
        self.assertEqual(runner.run.call_count, 2)

    def test_stage_exhaustion_and_duplicate_stage_refuse_launch(self):
        budget, runner, _ = self.make_budget()
        with self.assertRaises(TimeoutError), budget.phase('reference'):
            self.now = 30
            budget.command('not-launched', ['unused'], cwd='.')
        runner.run.assert_not_called()
        with self.assertRaises(AssertionError), budget.phase('reference'): pass

    def test_existing_audit_interface_is_clamped_to_30(self):
        budget, runner, _ = self.make_budget()
        with budget.phase('relocation'):
            budget.run('audit', ['unused'], cwd='.', timeout=180, check=True)
        self.assertEqual(runner.run.call_args.kwargs['timeout'], 30)
        self.assertTrue(runner.run.call_args.kwargs['check'])

    def test_command_failure_stops_later_commands_and_preserves_stage(self):
        budget, runner, record = self.make_budget()
        runner.run.side_effect = RuntimeError('first failure')
        with self.assertRaises(RuntimeError), budget.phase('compiler'):
            budget.command('first', ['unused'], cwd='.')
            budget.command('must-not-start', ['unused'], cwd='.')
        self.assertEqual(runner.run.call_count, 1)
        self.assertEqual(record['stage'], 'compiler')
        self.assertIn('compiler', record['phases'])


class FilesTests(unittest.TestCase):
    def setUp(self):
        temp = tempfile.TemporaryDirectory(); self.addCleanup(temp.cleanup)
        self.root = Path(temp.name)

    def bundle(self):
        source = self.root/'original'; source.mkdir()
        for name, data in [('construction.dll', b'il'), ('construction.runtimeconfig.json', b'config'), ('RyuPy.Runtime.dll', b'runtime')]:
            (source/name).write_bytes(data)
        return source, harness.inventory(source)

    def test_jit_copies_are_independent_and_tree_changes_rejected(self):
        source, expected = self.bundle()
        first = harness.copy_jit(source, self.root/'copy1', expected)
        second = harness.copy_jit(source, self.root/'copy2', expected)
        self.assertFalse(first.samefile(second))
        first.write_bytes(b'changed')
        with self.assertRaises(AssertionError): harness.unchanged(first.parent, expected)
        harness.unchanged(source, expected); harness.unchanged(second.parent, expected)
        extra = second.parent/'extra'; extra.write_bytes(b'bad')
        with self.assertRaises(AssertionError): harness.unchanged(second.parent, expected)
        extra.unlink(); second.unlink()
        with self.assertRaises(AssertionError): harness.unchanged(second.parent, expected)

    @unittest.skipIf(os.name == 'nt', 'Windows符号链接可能需权限；共享manifest测试覆盖reparse')
    def test_linked_tree_rejected(self):
        source, _ = self.bundle()
        (source/'link').symlink_to(source/'construction.dll')
        with self.assertRaises(ValueError): harness.inventory(source)

    def test_rename_retains_original_bytes_without_claiming_deletion(self):
        source, expected = self.bundle()
        evidence = self.root/'evidence'; evidence.mkdir()
        record = harness.retain_owned(source, self.root, evidence, mock.Mock())
        self.assertFalse(source.exists())
        self.assertEqual(record['old'], str(source))
        self.assertEqual(record['files'], expected)
        harness.unchanged(Path(record['new']), expected)

    def test_rename_rejects_non_owned_and_existing_target(self):
        source, _ = self.bundle()
        evidence = self.root/'evidence'; evidence.mkdir()
        with self.assertRaises(AssertionError): harness.retain_owned(source, self.root/'other', evidence, mock.Mock())
        (evidence/source.name).mkdir()
        with self.assertRaises(AssertionError): harness.retain_owned(source, self.root, evidence, mock.Mock())
        self.assertTrue(source.exists())

    def test_managed_and_runtime_hashes_are_verified(self):
        source, _ = self.bundle()
        data = {'schema_version': 1, 'compiler': {'options': {'flag': False}},
                'sources': [{'module_name': '__main__', 'compiled_text_sha256': 'f'*64}],
                'build_inputs': [{'relative_path': key, **harness.file_record(source/name)} for key, name in
                    [('managed-entry.dll', 'construction.dll'), ('RyuPy.Runtime.dll', 'RyuPy.Runtime.dll')]]}
        with mock.patch.object(harness, 'FIXTURE_SHA256', 'f'*64):
            harness.verify_inputs(data, source, source/'RyuPy.Runtime.dll')
            for change in ('managed', 'runtime', 'fixture', 'options'):
                altered = copy.deepcopy(data)
                if change in ('managed', 'runtime'): altered['build_inputs'][0 if change == 'managed' else 1]['sha256'] = '0'*64
                elif change == 'fixture': altered['sources'][0]['compiled_text_sha256'] = '0'*64
                else: altered['compiler']['options']['flag'] = True
                with self.subTest(change=change), self.assertRaises(AssertionError):
                    harness.verify_inputs(altered, source, source/'RyuPy.Runtime.dll')

    def test_unique_same_publish_managed_path(self):
        source, _ = self.bundle()
        native = source/'ryupy-aot-unique/publish/host'; native.parent.mkdir(parents=True); native.write_bytes(b'path-test')
        manifest = native.parent/'ryupy-artifact.json'; manifest.write_bytes(b'{}')
        raw = f'已生成 {source}/construction.dll\n已生成 {native}\nAOT 清单：{manifest}\n'.encode()
        complete = subprocess.CompletedProcess([], 0, b'', raw)
        entry, dll = harness.published_inputs(complete, source, 'linux-x64')
        self.assertEqual(entry, native); self.assertEqual(dll, source/'construction.dll')
        complete.stderr += f'已生成 {source}/construction.dll\n'.encode()
        with self.assertRaises(AssertionError): harness.published_inputs(complete, source, 'linux-x64')


class StaticContractTests(unittest.TestCase):
    def test_fixed_limits_and_modes(self):
        self.assertEqual(harness.MODES, ('positive', 'ryupy-boundaries'))
        self.assertEqual(sum(harness.PHASES.values()), 1770)
        self.assertEqual(harness.PHASES['publish'], 600)
        self.assertEqual(harness.PHASES['applications'], 120)
        self.assertEqual(harness.SDK, '10.0.403')


class MainFlowTests(unittest.TestCase):
    def exercise(self, failure=None):
        temp = tempfile.TemporaryDirectory(); self.addCleanup(temp.cleanup)
        root = Path(temp.name); repo = root/'repo'; repo.mkdir(); out = root/'out'
        fixture = repo/'tests/aot/construction_fixtures/construction.py'
        fixture.parent.mkdir(parents=True); fixture.write_bytes(b'fixture-not-executed')
        source_sha = hashlib.sha256(fixture.read_bytes()).hexdigest()
        labels, commands = [], []
        data = {}
        def fake_run(label, command, **kwargs):
            labels.append(label); commands.append(list(map(str, command)))
            if label == failure: raise RuntimeError('frozen first failure')
            if label == 'git-head': return subprocess.CompletedProcess(command, 0, b'a'*40+b'\n', b'')
            if label == 'git-status': return subprocess.CompletedProcess(command, 0, b'', b'')
            if label == 'cp-positive': return output(python=True)
            if label == 'runtime-build':
                runtime = repo/'runtime/RyuPy.Runtime/bin/Release/net10.0/RyuPy.Runtime.dll'
                runtime.parent.mkdir(parents=True); runtime.write_bytes(b'runtime')
            elif label == 'compiler-build':
                cli = repo/'target/debug/ryupy'; cli.parent.mkdir(parents=True); cli.write_bytes(b'cli')
            elif label == 'aot-publish':
                il = Path(command[command.index('--out-dir')+1]); il.mkdir()
                for name, content in [('construction.dll', b'il'), ('construction.runtimeconfig.json', b'config'), ('RyuPy.Runtime.dll', b'runtime')]:
                    (il/name).write_bytes(content)
                aot = il/'ryupy-aot-unique'; native = aot/'publish/host'
                native.parent.mkdir(parents=True); native.write_bytes(b'native-not-executed')
                (aot/'host').mkdir(); (aot/'host/sdk.version').write_text(harness.SDK+'\n')
                (aot/'publish.stdout.log').write_bytes(b'Generating native code\n'); (aot/'publish.stderr.log').write_bytes(b'')
                (native.parent/'ryupy-artifact.json').write_bytes(b'{}')
                data.update(schema_version=1, compiler={'options': {'test_flag': False}},
                    sources=[{'module_name': '__main__', 'compiled_text_sha256': source_sha}],
                    build_inputs=[{'relative_path': name, **harness.file_record(il/file)} for name,file in
                        [('managed-entry.dll','construction.dll'), ('RyuPy.Runtime.dll','RyuPy.Runtime.dll')]])
                return subprocess.CompletedProcess(command, 0, b'',
                    f'已生成 {il}/construction.dll\n已生成 {native}\nAOT 清单：{native.parent}/ryupy-artifact.json\n'.encode())
            elif label.startswith(('jit-', 'native-')):
                # 这里只返回人工固定结果，绝不执行假文件或fixture。
                self.assertEqual(kwargs['cwd'], Path(command[-2]).parent)
                return output(command[-1])
            return subprocess.CompletedProcess(command, 0, b'', b'')
        def fake_relocate(publish, native, destination, rid, sdk):
            shutil.copytree(publish, destination)
            return destination/Path(native).name, data
        runner = mock.Mock(); runner.run.side_effect = fake_run
        with ExitStack() as patches:
            for name, value in [('EXPECTED', copy.deepcopy(FROZEN)), ('FIXTURE_SHA256', source_sha),
                    ('Runner', mock.Mock(return_value=runner)), ('executable', lambda x, **kw: str(x)),
                    ('host', lambda rid: {'os': 'mock-host'}), ('toolchains', lambda *a: {}),
                    ('verify_project', lambda *a: None), ('verify_manifest', lambda *a: data),
                    ('relocate_manifest', fake_relocate), ('linux_audit', lambda *a: [])]:
                patches.enter_context(mock.patch.object(harness, name, value))
            args = ['--repo', str(repo), '--revision', 'a'*40, '--out', str(out), '--python', 'not-executed-python', '--dotnet', 'not-executed-dotnet', '--rid', 'linux-x64']
            if failure:
                with self.assertRaisesRegex(RuntimeError, 'frozen first failure'): harness.main(args)
            else: self.assertEqual(harness.main(args), 0)
        results = list(out.glob('construction-aot-*/result.json'))
        self.assertEqual(len(results), 1)
        return json.loads(results[0].read_text()), labels, commands

    def test_exact_single_publish_one_reference_four_independent_applications(self):
        result, labels, commands = self.exercise()
        self.assertEqual(result['status'], 'passed')
        self.assertEqual(result['counts'], {'runtime':1, 'compiler':1, 'publish':1, 'reference':1, 'applications':4})
        self.assertEqual(labels, ['git-head','git-status','cp-positive','runtime-build','compiler-build','aot-publish',
                                  'jit-positive','jit-ryupy-boundaries','native-positive','native-ryupy-boundaries'])
        self.assertEqual(sum('--aot' in command for command in commands), 1)
        self.assertEqual(len({str(Path(command[-2]).parent) for label,command in zip(labels,commands) if label.startswith(('jit-', 'native-'))}), 4)
        self.assertTrue(result['original_build_paths_absent']); self.assertTrue(result['build_evidence_retained'])
        self.assertFalse(result['clean_machine_without_runtime_tested'])
        self.assertEqual(result['source_tree_action'], 'renamed')
        self.assertEqual(result['not_executed'], [])

    def test_first_failure_never_launches_following_stage_or_application(self):
        for failure in ['cp-positive', 'runtime-build', 'compiler-build', 'aot-publish', 'jit-positive', 'jit-ryupy-boundaries', 'native-positive']:
            with self.subTest(failure=failure):
                result, labels, _ = self.exercise(failure)
                self.assertEqual(labels[-1], failure)
                self.assertEqual(result['status'], 'failed')
                self.assertIn('frozen first failure', result['failure'])
                self.assertLessEqual(result['counts']['publish'], 1)
                self.assertLess(result['counts']['applications'], 4)


class ToolchainTests(unittest.TestCase):
    def test_owned_global_pin_does_not_replace_runtime_cwd_check(self):
        budget = mock.Mock()
        budget.command.side_effect = [subprocess.CompletedProcess([], 0, b'cpython\n3.12.13\n', b''),
                                     subprocess.CompletedProcess([], 0, b'10.0.402\n', b'')]
        with self.assertRaisesRegex(AssertionError, '实际cwd SDK'):
            harness.toolchains(budget, Path('/repo'), Path('/owned'), 'python', 'dotnet', 'cargo', {}, 'linux-x64')
        self.assertEqual(budget.command.call_count, 2)
        self.assertEqual(budget.command.call_args.kwargs['cwd'], Path('/repo'))
        self.assertEqual(budget.command.call_args.args[0], 'runtime-sdk')


class AuditTests(unittest.TestCase):
    def test_linux_interpreter_direct_path_requires_exact_name_and_system_resolution(self):
        def execute(line, *, system=True):
            budget = mock.Mock()
            budget.command.side_effect = [subprocess.CompletedProcess([], 0,
                b' 0x (NEEDED) Shared library: [ld-linux-x86-64.so.2]\n', b''),
                subprocess.CompletedProcess([], 0, line, b'')]
            with mock.patch.object(harness, 'dependency_binaries', return_value=[Path('/package/host')]), \
                 mock.patch.object(harness, 'executable', side_effect=lambda x: x), \
                 mock.patch.object(Path, 'is_file', return_value=True):
                return harness.linux_audit(budget, Path('/package'), {}, {})
        actual = execute(b' /lib64/ld-linux-x86-64.so.2 (0x001)\n')
        self.assertEqual(actual[0]['needed'], ['ld-linux-x86-64.so.2'])
        for line in [b' /tmp/ld-linux-x86-64.so.2 (0x001)\n', b' /package/ld-linux-x86-64.so.2 (0x001)\n', b' /lib64/ld-linux-other.so.2 (0x001)\n',
                     b' ld-linux-x86-64.so.2 => not found\n', b' /lib64/fakeld-linux-x86-64.so.2 (0x001)\n']:
            with self.subTest(line=line), self.assertRaises(AssertionError): execute(line)

    def test_inventory_checks_deadline_before_and_after_each_digest(self):
        with tempfile.TemporaryDirectory() as tmp:
            root = Path(tmp)
            for name in ('a', 'b'): (root/name).write_bytes(b'content')
            check = mock.Mock()
            harness.inventory(root, check)
            self.assertEqual(check.call_count, 5)
            check.side_effect = [None, None, TimeoutError('deadline')]
            with self.assertRaises(TimeoutError): harness.inventory(root, check)


if __name__ == '__main__':
    unittest.main()
