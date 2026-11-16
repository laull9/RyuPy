"""纯mock验证CI接线/portable消费，不运行工具、CP fixture或产品。"""
import contextlib
import json
from pathlib import Path
import shutil
import sys
import tempfile
import types
import unittest
from unittest.mock import Mock, patch
import bootstrap as boot
import product_contract as c

HERE = Path(__file__).resolve().parent
HEAD, TREE = '1' * 40, '2' * 40


class BootstrapTests(unittest.TestCase):
    @contextlib.contextmanager
    def fixture(self, failure=None):
        with tempfile.TemporaryDirectory(dir=HERE) as directory:
            repo = Path(directory)
            dotnet, git = repo / 'dotnet', repo / 'git'
            dotnet.write_bytes(b'fake-dotnet'); git.write_bytes(b'fake-git')
            cli = repo / 'target/debug/ryupy'; cli.parent.mkdir(parents=True); cli.write_bytes(b'fake-cli')
            runtime = repo / 'runtime/RyuPy.Runtime/bin/Release/net10.0/RyuPy.Runtime.dll'
            runtime.parent.mkdir(parents=True); runtime.write_bytes(b'fake-runtime')
            baseline = repo / 'target/wcwidth-aot-evidence'; baseline.mkdir()
            argv = {'git': ['git', 'rev-parse', 'HEAD', 'HEAD^{tree}'], 'runtime-build': ['make', 'runtime'],
                    'compiler-build': ['cargo', 'build', '--locked', '-p', 'ryupy-cli', '--features', 'ryupy-driver/ordinary-dynamic-module-pilot'],
                    'rustc': ['rustc', '--version'], 'cargo': ['cargo', '--version'], 'dotnet': ['dotnet', '--info']}
            commands = [{'name': name, 'argv': value, 'exit': 1 if failure == 'baseline' and name == 'runtime-build' else 0}
                        for name, value in argv.items()]
            (baseline / 'commands.json').write_text(json.dumps(commands))
            for name in argv:
                (baseline / (name + '.stdout')).write_text(HEAD + '\n' + TREE + '\n' if name == 'git' else '')
                (baseline / (name + '.stderr')).write_text('')
            (baseline / 'compiler-identity.json').write_text(json.dumps({'cli_sha256': c.sha(cli), 'runtime_sha256': c.sha(runtime),
                'feature': 'ryupy-driver/ordinary-dynamic-module-pilot', 'configuration': 'debug'}))
            downloads = repo / 'tests/aot/wcwidth_ci/downloads.json'; downloads.parent.mkdir(parents=True)
            downloads.write_text('[{"kind":"dotnet","version":"10.0.403"}]')
            calls = []
            def capture(command, cwd, env, prefix, timeout, watch):
                calls.append((prefix.name, command, timeout))
                stdout = (HEAD + '\n' + TREE + '\n').encode() if prefix.name == 'git-freeze' else b'10.0.403\n' if prefix.name == 'sdk-version' else b''
                if prefix.name == 'git-clean' and failure == 'dirty': stdout = b' M source.py\n'
                prefix.with_suffix('.stdout').write_bytes(stdout); prefix.with_suffix('.stderr').write_bytes(b'')
                prefix.with_suffix('.json').write_text(json.dumps({'exit': 0, 'failure': None, 'raw_streams_complete': True}))
                if prefix.name == 'decoder-build':
                    if failure == 'decoder': raise RuntimeError('synthetic decoder failure')
                    output = Path(command[command.index('--artifacts-path') + 1]) / 'bin/PeAudit/release'
                    output.mkdir(parents=True)
                    (output / 'PeAudit.dll').write_bytes(b'fake-decoder')
                    (output / 'PeAudit.runtimeconfig.json').write_text('{}')
            p0 = types.SimpleNamespace(run_process=capture, candidate_identity=Mock(return_value={'head': HEAD, 'status': '', 'files': {'source.py': 'stable'}}))
            mocked_runner = Mock()
            def product(contract):
                output = Path(contract['output']); output.mkdir()
                (output / 'result.json').write_text('{"passed":true}')
                return mocked_runner
            with patch.object(boot.gate, 'load_reused', return_value=(p0, None)), \
                 patch.object(boot.gate, 'Runner', side_effect=product) as runner, \
                 patch.object(boot.shutil, 'which', side_effect=lambda name: str(dotnet if name == 'dotnet' else git)), \
                 patch.object(boot.c, 'oracle'), patch.object(boot.c, 'hot_oracle'), \
                 patch.object(boot.sys, 'version_info', (3, 12, 13)), \
                 patch.dict(boot.os.environ, {'DOTNET': str(dotnet), 'DOTNET_ROOT': str(repo), 'GITHUB_SHA': HEAD}):
                before = {p.name: c.sha(p) for p in baseline.iterdir()}
                yield repo, calls, runner, mocked_runner, before
                self.assertEqual(before, {p.name: c.sha(p) for p in baseline.iterdir()})

    def test_real_contract_is_derived_without_rebuilding_product_tools(self):
        with self.fixture() as (repo, calls, runner, product, before):
            value = boot.Bootstrap(repo); value.execute()
            self.assertEqual([name for name, _, _ in calls], ['git-freeze', 'git-clean', 'sdk-version', 'decoder-build'])
            runner.assert_called_once(); product.execute.assert_called_once()
            contract = json.loads((value.out / 'contract.json').read_text())
            self.assertEqual(contract['commit'], HEAD); self.assertEqual(contract['tree'], TREE)
            self.assertEqual(contract['sdk_version'], '10.0.403')
            self.assertEqual(contract['runtime']['sha256'], c.sha(contract['runtime']['path']))
            self.assertEqual(contract['goldens'], str(repo / 'tests/validation/generator_product/goldens'))
            self.assertLessEqual(next(timeout for name, _, timeout in calls if name == 'decoder-build'), 300)
            self.assertNotIn('oracle', contract); self.assertNotIn('hot_oracle', contract)
            with self.assertRaises(FileExistsError): boot.Bootstrap(repo)

    def test_failed_old_build_or_dirty_tree_never_starts_decoder_or_product(self):
        for failure in ('baseline', 'dirty'):
            with self.subTest(failure=failure), self.fixture(failure) as (repo, calls, runner, product, _):
                value = boot.Bootstrap(repo)
                with self.assertRaises(ValueError): value.execute()
                self.assertNotIn('decoder-build', [name for name, _, _ in calls])
                runner.assert_not_called(); product.execute.assert_not_called()
                self.assertFalse((value.out / 'product-started.json').exists())

    def test_decoder_failure_is_once_with_original_streams(self):
        with self.fixture('decoder') as (repo, calls, runner, product, _):
            value = boot.Bootstrap(repo)
            with self.assertRaises(RuntimeError): value.execute()
            self.assertEqual([name for name, _, _ in calls].count('decoder-build'), 1)
            runner.assert_not_called()
            self.assertTrue((value.out / 'decoder-build.stdout').is_file())
            self.assertFalse((value.out / 'contract.json').exists())

    def test_portable_goldens_are_exact_small_set_and_reject_extra_files(self):
        rows = c.load_goldens(HERE / 'goldens')
        self.assertEqual(set(rows), {*c.CASES, c.HOT})
        self.assertEqual(len(list((HERE / 'goldens').iterdir())), 21)
        with tempfile.TemporaryDirectory(dir=HERE) as directory:
            copy = Path(directory) / 'goldens'; shutil.copytree(HERE / 'goldens', copy)
            (copy / 'report.txt').write_text('not permitted')
            with self.assertRaises(ValueError): c.load_goldens(copy)
            (copy / 'report.txt').unlink()
            (copy / 'b_target_failure.stderr').write_bytes(b'changed')
            with self.assertRaises(ValueError): c.load_goldens(copy)


if __name__ == '__main__':
    unittest.main()
