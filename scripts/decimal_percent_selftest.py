"""两图薄接线的纯控制反例；只构造假原件，不启动CP fixture、CLI、SDK或Native。"""
import copy
import hashlib
import io
import os
from pathlib import Path
import shutil
import tempfile
import tarfile
import unittest
from unittest import mock
import decimal_percent_contract as c
import decimal_percent_validation as gate
import textwrap_dedent_selftest as old
from re_scanner_selftest import write_process


def trace(row, source, extra=''):
    return ('Traceback (most recent call last):\n' + ''.join(
        f'  File "{source}", line {number}, in {name}\n' for number, name in row['frames'])
        + row['message'] + '\n' + extra).encode()


class PercentControlTests(unittest.TestCase):
    def test_manifest_actual_facts_and_cp_roles(self):
        data = c.load_manifest()
        self.assertEqual(len(data['csharp']['facts']), 42)
        self.assertEqual(sum(row['cp'] for graph in data['graphs'] for row in graph['cases']), 3)
        self.assertEqual(len(data['graphs']), 2)
        self.assertEqual({row['kind'] for row in data['graphs'][1]['cases']}, {'ni', 'capacity', 'trace'})

    def test_exact_selector_aliases_and_unchanged_job_blocks(self):
        _, values, active = old.select(c.TITLE)
        self.assertEqual(active, old.ORDINARY | {'distribution-metadata', 'textwrap-dedent', 're-scanner', 'decimal-percent'})
        self.assertEqual(values['percent'], 'true')
        _, blocks = old.jobs()
        for name, digest in old.DATA['unchanged_job_blocks'].items():
            self.assertEqual(hashlib.sha256(blocks[name].encode()).hexdigest(), digest, name)
        for name in active:
            self.assertIn('    needs: select-mode\n', blocks[name])
        for title in (c.TITLE.upper(), c.TITLE + '\n', 'ordinary'):
            self.assertEqual(old.select(title)[2], old.ORDINARY)
        self.assertEqual(old.select(c.TITLE, event='workflow_dispatch')[2], old.ORDINARY)
        self.assertEqual(old.select(c.TITLE, attempt='2')[2], old.ORDINARY)
        self.assertEqual(old.select(c.TITLE, ref=old.c.old.shared.REF.upper())[2], set())
        self.assertNotIn('percent', old.select(c.shared.TITLE)[1])
        env = dict(RUNNER_ENVIRONMENT='github-hosted', GITHUB_ACTIONS='true', GITHUB_EVENT_NAME='push',
            GITHUB_RUN_ATTEMPT='1', GITHUB_REF=old.c.old.shared.REF, GITHUB_SHA='a' * 40,
            RYUPY_VALIDATION_MODE='distribution-metadata')
        event = dict(ref=env['GITHUB_REF'], after=env['GITHUB_SHA'], head_commit=dict(message=c.TITLE, id=env['GITHUB_SHA']))
        checkers = (c.check_context, c.shared.check_context, old.c.check_context, old.c.metadata.check_context)
        for check in checkers:
            before = copy.deepcopy(event); check(env, event); self.assertEqual(before, event)
            for key, value in [('GITHUB_RUN_ATTEMPT', '2'), ('GITHUB_EVENT_NAME', 'workflow_dispatch'),
                               ('RUNNER_ENVIRONMENT', 'self-hosted'), ('RUSTFLAGS', '-A warnings'), ('GITHUB_SHA', 'b' * 40)]:
                with self.assertRaises(ValueError): check(dict(env, **{key: value}), event)
            for title in (c.TITLE.upper(), c.TITLE + '\n', 'validation: decimal-percent-002'):
                with self.assertRaises(ValueError): check(env, dict(event, head_commit=dict(event['head_commit'], message=title)))

    def test_exit_one_requires_exact_all_user_frames_and_stderr(self):
        data = c.load_manifest()
        for row in [x for x in data['graphs'][1]['cases'] if x['kind'] == 'trace']:
            source = c.ROOT / data['graphs'][1]['source']
            with tempfile.TemporaryDirectory() as temp:
                root = Path(temp); argv = ['dotnet', 'fake.dll', *row['argv']]
                write_process(root, 'run', argv, b'', trace(row, source), exit=1)
                result = c.verify_row(root / 'run', argv, row, source)
                self.assertTrue(result['passed'])
                raw, streams = c.verify_process(root / 'run', argv)
                self.assertFalse(c.shared.semantic_result(raw, streams, row)['passed'])
                for change in ({'exit': 0}, {'failure': 'timeout'}, {'raw_streams_complete': False}, {'exit': -9}):
                    args = dict(exit=1); args.update(change)
                    write_process(root, 'run', argv, b'', trace(row, source), **args)
                    with self.assertRaises(ValueError): c.verify_row(root / 'run', argv, row, source)
                bad = [trace(row, source, 'Unhandled exception\n'), trace(row, source).replace(b'<module>', b'wrong'),
                       trace(row, source).replace(row['message'].encode(), b'TypeError: other')]
                for stderr in bad:
                    write_process(root, 'run', argv, b'', stderr, exit=1)
                    with self.assertRaises(ValueError): c.verify_row(root / 'run', argv, row, source)

    def fixture(self, root, compile_failed=False, csharp_failed=False):
        """小假同轮包复用原write_process；实际恢复谓词运行，产品/机器身份边界显式stub。"""
        with mock.patch.object(gate, 'ROOT', root): runner = gate.Runner()
        runner.before = dict(head='a' * 40, tree='b' * 40)
        runner.python, runner.dotnet = root / 'python', root / 'dotnet'
        runner.cli, runner.runtime = runner.out / 'tools/ryupy', runner.out / 'tools/RyuPy.Runtime.dll'
        for path in (runner.python, runner.dotnet, runner.cli, runner.runtime,
                     runner.out / 'managed/RyuPy.Runtime.dll', runner.out / 'managed/RyuPy.Runtime.Tests.dll'):
            path.parent.mkdir(parents=True, exist_ok=True); path.write_bytes(b'fake')
        runner.data['cp_sha256'] = c.sha(runner.python)
        for name in set(runner.data['sources']) | {row['source'] for row in runner.data['old_cases']}:
            target = runner.inputs_dir / name; target.parent.mkdir(parents=True, exist_ok=True); shutil.copy2(c.ROOT / name, target)
        runner.input_inventories = {str(runner.inputs_dir): gate.base.inventory(runner.inputs_dir)}
        runner.environment = lambda: setattr(runner, 'env', {'test-only': '1'})
        runner.environment(); runner.identity = lambda label: runner.before
        runner.barrier = lambda *args, **kwargs: None
        runner.test_assemblies = lambda: None
        runner.persist = lambda: None
        runner.fresh_identity = lambda: dict(head=runner.before['head'], tree=runner.before['tree'], run_id='1', run_attempt='1',
            manifest_sha256='fake', sources=runner.data['sources'], tools={})
        tests = gate.ROOT / 'runtime/RyuPy.Runtime.Tests/RyuPy.Runtime.Tests.csproj'
        commands = {'csharp-build': [runner.dotnet, 'build', tests, '-c', 'Release', '-nologo'],
                    'compiler-build': ['cargo', 'build', '--locked', '-p', 'ryupy-cli', '--features', gate.helper.old.shared.FEATURE],
                    'percent-csharp-tests': runner.test_command()}
        for graph in runner.data['graphs']:
            commands[graph['id'] + '-compile-command'] = gate.helper.build_argv(runner, c.cases(graph)[0], runner.graph_target(graph))
            for row in graph['cases']:
                source = runner.inputs_dir / graph['source']
                for phase in ('cp', 'jit'):
                    if phase == 'cp' and not row['cp']: continue
                    if phase == 'jit' and compile_failed and graph['id'] == 'supported': continue
                    target = runner.graph_target(graph)
                    argv = ([runner.python, '-I', '-B', source] if phase == 'cp' else [runner.dotnet, target / (source.stem + '.dll')]) + row['argv']
                    self.fake_run(runner, row['id'] + '-' + phase + '-run', argv, row, source)
            self.fake_pe(runner, runner.graph_target(graph), Path(graph['source']).stem)
        for row in runner.data['old_cases']:
            if row['id'] == 'old-termcolor': continue
            source = runner.inputs_dir / row['source']
            commands[row['id'] + '-compile-command'] = runner.old_compile_argv(row)
            self.fake_run(runner, row['id'] + '-cp-run', [runner.python, '-B', source, *row['argv']], row, source)
            pe = runner.root_out / 'products' / row['id'] / (source.stem + '.dll')
            self.fake_run(runner, row['id'] + '-jit-run', [runner.dotnet, pe, *row['argv']], row, source)
            self.fake_pe(runner, pe.parent, source.stem)
        for name, argv in commands.items():
            failed = (name == 'supported-compile-command' and compile_failed) or (name == 'percent-csharp-tests' and csharp_failed)
            write_process(runner.out, name, argv, exit=int(failed))
        trx = runner.out / 'trx/result.trx'; trx.parent.mkdir()
        trx.write_text('<TestRun xmlns="http://microsoft.com/schemas/VisualStudio/TeamTest/2010">' + ''.join(
            '<UnitTestResult testName="RyuPy.Runtime.Tests.' + name + '" outcome="Passed"/>' for name in runner.data['csharp']['facts']) + '</TestRun>')
        checkpoint = dict(schema_version=1, identity=runner.fresh_identity(), runs=runner.run_records,
            files={str(p.relative_to(runner.out)): c.sha(p) for p in runner.out.rglob('*') if p.is_file()})
        state = dict(schema_version=1, before=runner.before, frozen={}, inventories=runner.input_inventories,
                     isolation={}, environment=runner.env, labels=list(commands) + list(runner.run_records), checkpoint=checkpoint)
        return runner, state

    def fake_pe(self, runner, target, stem):
        target.mkdir(parents=True, exist_ok=True)
        for name in (stem + '.dll', 'RyuPy.Runtime.dll', 'module-environment.json'): (target / name).write_bytes(b'fake')

    def fake_run(self, runner, name, argv, row, source):
        stderr = trace(row, source) if row['kind'] == 'trace' else b''
        record = write_process(runner.out, name, argv, row['stdout'].encode(), stderr, exit=row['exit'])
        runner.run_records[name] = dict(record=record, prefix=name, argv=list(map(str, argv)), source_sha256=c.sha(source))

    def resume(self, runner, state, execute=False):
        gate.save(runner.out / 'state.json', state)
        env = dict(PYTHON=str(runner.python), DOTNET=str(runner.dotnet), GITHUB_EVENT_PATH=str(runner.out / 'event.json'))
        (runner.out / 'event.json').write_text('{}')
        with mock.patch.dict(os.environ, env), mock.patch.object(c, 'check_context'), mock.patch.object(c, 'load_manifest'), \
             mock.patch.object(gate.helper, 'verify_plan'), mock.patch.object(gate.sys, 'executable', str(runner.python)), \
             mock.patch.object(gate.sys, 'version_info', (3, 12, 13)):
            return runner.execute('native') if execute else runner.load_state()

    def test_csharp_red_and_one_compile_red_do_not_block_two_native_graphs(self):
        with tempfile.TemporaryDirectory() as temp:
            runner, state = self.fixture(Path(temp), compile_failed=True, csharp_failed=True)
            visited = []; runner.native_graph = lambda graph: visited.append(graph['id'])
            self.assertEqual(self.resume(runner, state, True), 1)
            self.assertEqual(visited, ['supported', 'exceptions'])
            self.assertEqual(runner.result['stages']['supported-all-jit']['status'], 'blocked')
            self.assertFalse(runner.result['stages']['csharp-tests']['passed'])
            self.assertTrue(runner.result['stages']['exception-value-jit']['passed'])

    def test_missing_row_cross_run_and_second_resume_are_rejected(self):
        for change in ('missing', 'identity', 'second'):
            with self.subTest(change=change), tempfile.TemporaryDirectory() as temp:
                runner, state = self.fixture(Path(temp))
                if change == 'missing': del state['checkpoint']['runs']['exception-value-jit-run']
                if change == 'identity': state['checkpoint']['identity']['run_id'] = 'other'
                if change == 'second': self.resume(runner, state)
                with self.assertRaises((ValueError, FileExistsError)): self.resume(runner, state)

    def test_resumed_original_streams_remain_frozen_through_real_barrier(self):
        for suffix in ('stdout', 'argv.json', 'resume'):
            with self.subTest(suffix=suffix), tempfile.TemporaryDirectory() as temp:
                runner, state = self.fixture(Path(temp))
                # 只stub机器identity/程序集。被审的frozen与inventory检查使用真实metadata barrier。
                del runner.barrier
                runner.persist = gate.Runner.persist.__get__(runner)
                self.resume(runner, state)
                name = 'resume-state.raw.json' if suffix == 'resume' else 'supported-all-jit-run.' + suffix
                path = runner.out / name; original = path.read_bytes(); digest = c.sha(path)
                self.assertEqual(runner.frozen[str(path)], digest)
                path.write_bytes(original + b'changed')
                # 即使直接调用persist，已核原件也不能换成新digest。
                runner.persist()
                saved = c.parse((runner.out / 'state.json').read_bytes())
                if suffix != 'resume': self.assertEqual(saved['checkpoint']['files'][name], digest)
                with self.assertRaises(gate.helper.IdentityError): runner.barrier('real-post-resume')
                self.assertTrue(runner.identity_failed); self.assertFalse(runner.ready)
                self.assertFalse(runner.result['passed'])
                self.assertIsNone(c.parse((runner.out / 'state.json').read_bytes())['checkpoint'])
                path.write_bytes(original)
                with self.assertRaises(gate.helper.IdentityError): runner.barrier('sticky-even-if-restored')

    def test_checkpoint_authority_conflict_is_rejected_not_overwritten(self):
        with tempfile.TemporaryDirectory() as temp:
            runner, state = self.fixture(Path(temp))
            path = runner.out / 'supported-all-jit-run.stdout'
            state['frozen'][str(path)] = '0' * 64
            with self.assertRaises(gate.helper.IdentityError): self.resume(runner, state)
            self.assertEqual(runner.frozen[str(path)], '0' * 64)

    def test_uninstalled_termcolor_link_rejection_is_local_but_installed_input_is_global(self):
        with tempfile.TemporaryDirectory() as temp:
            runner, state = self.fixture(Path(temp))
            item = runner.data['termcolor']; archive = runner.out / item['filename']
            # 仅内存中的小tar；真实data解包和真实inventory拒链接，不下载或执行产品。
            payload = io.BytesIO()
            with tarfile.open(fileobj=payload, mode='w') as writer:
                info = tarfile.TarInfo('termcolor-2.4.0/src/regular.py'); info.size = 1
                writer.addfile(info, io.BytesIO(b'x'))
                link = tarfile.TarInfo('termcolor-2.4.0/src/link.py')
                link.type = tarfile.SYMTYPE; link.linkname = 'regular.py'; writer.addfile(link)
            payload.seek(0); reader = tarfile.open(fileobj=payload, mode='r')
            def fake_download(name, argv, timeout):
                self.assertEqual(name, 'termcolor-download'); self.assertIn(item['url'], argv)
                archive.write_bytes(b'test-only-download-boundary')
            runner.checked = fake_download
            with mock.patch.object(gate, 'sha', side_effect=lambda path: item['sha256'] if Path(path) == archive else c.sha(path)), \
                 mock.patch.object(gate.tarfile, 'open', return_value=reader):
                self.assertFalse(runner.phase('termcolor-source', runner.prepare_termcolor))
            self.assertFalse(runner.identity_failed)
            self.assertEqual(runner.result['stages']['termcolor-source']['status'], 'failed')
            self.assertIn('未安装暂存验证拒绝', runner.result['stages']['termcolor-source']['error'])
            self.assertFalse((runner.inputs_dir / 'termcolor-package').exists())
            self.assertFalse((runner.out / 'termcolor-source.json').exists())
            # 跨phase走真实persist/load_state/barrier，不能只直接调用两个phase冒充可恢复。
            del runner.barrier
            runner.persist = gate.Runner.persist.__get__(runner)
            runner.labels = set(state['labels']); runner.ready = True
            runner.persist()
            saved = c.parse((runner.out / 'state.json').read_bytes())
            self.assertFalse(any(name.startswith('termcolor-unverified/') for name in saved['checkpoint']['files']))
            self.assertIn(item['filename'], saved['checkpoint']['files'])
            link_path = runner.out / 'termcolor-unverified/termcolor-2.4.0/src/link.py'
            self.assertTrue(link_path.is_symlink())
            visited = []; runner.native_graph = lambda graph: visited.append(graph['id'])
            self.assertEqual(self.resume(runner, saved, execute=True), 1)
            self.assertEqual(visited, ['supported', 'exceptions'])
            self.assertFalse(runner.identity_failed)
            self.assertTrue(runner.ready)
            for phase in ('cp', 'compile', 'jit'):
                self.assertEqual(runner.result['stages']['old-termcolor-' + phase]['status'], 'blocked')
            runner.barrier('actual-post-termcolor-resume')
            self.assertTrue(link_path.is_symlink())
            # 已纳入全局输入的变化仍走真实barrier的粘性IdentityError，不能被上面局部catch吞掉。
            source = runner.inputs_dir / runner.data['graphs'][0]['source']
            source.write_bytes(source.read_bytes() + b'\n# changed\n')
            with self.assertRaises(gate.helper.IdentityError): runner.barrier('installed-input-changed')
            self.assertTrue(runner.identity_failed)

    def test_native_uses_original_helper_and_uploads_after_failure(self):
        workflow = (c.ROOT / '.github/workflows/decimal-percent.yml').read_text()
        self.assertIn("!cancelled() && hashFiles('target/decimal-percent-evidence/state.json') != ''", workflow)
        self.assertIn('**/native-dist.tar.gz', workflow)
        self.assertLess(workflow.index('percent-checkpoint-'), workflow.index('--phase native'))
        self.assertEqual(gate.native.__module__, 'textwrap_dedent_native')
        self.assertNotIn('needs: re-scanner', workflow)


if __name__ == '__main__': unittest.main()
