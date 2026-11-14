"""双构建策略纯反例；只mock构建/产品，真实state与临时文件校验不运行产物。"""
import copy
import json
from pathlib import Path
import tempfile
import unittest
from unittest.mock import Mock, patch
import distribution_metadata as gate
import distribution_metadata_modes as m
import distribution_metadata_products as p
from distribution_metadata_identity import IdentityError


class ModeTests(unittest.TestCase):
    def runner(self, root):
        with patch.object(gate, 'ROOT', root): r = gate.Runner()
        r.before = {'head': 'fixed', 'tree': 'fixed', 'files': {}}
        r.identity = Mock(return_value=r.before)
        r.dotnet = root / 'dotnet'; r.environment()
        r.cli = r.out / 'tools/ryupy'; r.cli.parent.mkdir(); r.cli.write_bytes(b'pilot')
        r.runtime = r.out / 'tools/RyuPy.Runtime.dll'; r.runtime.write_bytes(b'runtime')
        r.frozen[str(r.cli)] = gate.sha(r.cli); r.frozen[str(r.runtime)] = gate.sha(r.runtime)
        return r

    def ready(self, root):
        r = self.runner(root); r.explicit_cli = m.tool_path(r)
        r.explicit_cli.parent.mkdir(); r.explicit_cli.write_bytes(b'featureoff')
        r.frozen[str(r.explicit_cli)] = gate.sha(r.explicit_cli)
        r.result['stages']['explicit-tools'].update(passed=True, status='passed')
        r.persist(); return r

    def test_single_isolated_build_no_pilot_and_no_overwrite(self):
        with tempfile.TemporaryDirectory() as temp:
            root = Path(temp); r = self.runner(root); pilot = r.cli.read_bytes()
            def command(label, argv, timeout, env):
                self.assertEqual(label, 'explicit-compiler-build'); self.assertEqual(timeout, 1200)
                self.assertEqual(argv[:6], ['cargo', 'build', '--locked', '--no-default-features', '-p', 'ryupy-cli'])
                self.assertNotIn('--features', argv); self.assertNotIn('CARGO_TARGET_DIR', env)
                target = Path(argv[-1]); (target / 'debug').mkdir(parents=True)
                (target / 'debug/ryupy').write_bytes(b'featureoff')
                return {'exit': 0, 'failure': None, 'raw_streams_complete': True}
            with patch.object(m.old, 'ROOT', root), patch.object(r, 'command', side_effect=command) as call:
                m.build(r)
                with self.assertRaises(ValueError): m.build(r)
                self.assertEqual(call.call_count, 1)
            self.assertEqual(r.cli.read_bytes(), pilot); self.assertEqual(r.explicit_cli.read_bytes(), b'featureoff')
            self.assertFalse((root / 'target/metadata-explicit-only-build/debug/ryupy').exists())

    def test_build_failure_timeout_incomplete_cannot_publish_tool(self):
        for record in ({'exit': 101, 'failure': None, 'raw_streams_complete': True},
                       {'exit': -9, 'failure': 'timeout', 'raw_streams_complete': True},
                       {'exit': 0, 'failure': None, 'raw_streams_complete': False}):
            with tempfile.TemporaryDirectory() as temp:
                root = Path(temp); r = self.runner(root)
                with patch.object(m.old, 'ROOT', root), patch.object(r, 'command', return_value=record):
                    self.assertFalse(r.phase('explicit-tools', lambda: m.build(r)))
                self.assertIsNone(r.explicit_cli); self.assertFalse(m.tool_path(r).exists())

    def test_real_resume_requires_new_field_version_path_hash_and_stage(self):
        for mutation in ['none', 'missing-field', 'old-version', 'changed-path', 'changed-bytes',
                         'missing-file', 'unfrozen', 'changed-hash', 'stage-false', 'null-success',
                         'failed-status', 'invalidated-status', 'started-status', 'null-passed-status']:
            with self.subTest(mutation=mutation), tempfile.TemporaryDirectory() as temp:
                root = Path(temp); r = self.ready(root); state = json.loads((r.out / 'state.json').read_text())
                if mutation == 'missing-field': del state['explicit_cli']
                if mutation == 'old-version': state['schema_version'] = 2
                if mutation == 'changed-path':
                    other = r.out / 'alternate'; other.write_bytes(b'featureoff')
                    state['explicit_cli'] = str(other); state['frozen'][str(other)] = gate.sha(other)
                if mutation == 'changed-bytes': r.explicit_cli.write_bytes(b'changed')
                if mutation == 'missing-file': r.explicit_cli.unlink()
                if mutation == 'unfrozen': del state['frozen'][str(r.explicit_cli)]
                if mutation == 'changed-hash': state['frozen'][str(r.explicit_cli)] = '0' * 64
                if mutation == 'stage-false': state['result']['stages']['explicit-tools']['passed'] = False
                if mutation == 'null-success': state['explicit_cli'] = None
                if mutation.endswith('-status') and mutation != 'null-passed-status':
                    state['result']['stages']['explicit-tools']['status'] = {
                        'failed-status': 'failed', 'invalidated-status': 'invalidated-identity',
                        'started-status': 'started'}[mutation]
                if mutation == 'null-passed-status':
                    state['explicit_cli'] = None
                    state['result']['stages']['explicit-tools']['passed'] = False
                (r.out / 'state.json').write_text(json.dumps(state))
                with patch.object(gate, 'ROOT', root): resumed = gate.Runner(resume=True)
                resumed.identity = Mock(return_value=r.before)
                with patch.object(m, 'build') as build, patch.object(p, 'native') as native:
                    if mutation == 'none':
                        resumed.load_state(); resumed.barrier('resume-proof')
                        self.assertEqual(resumed.explicit_cli, r.explicit_cli)
                    else:
                        with self.assertRaises(IdentityError): resumed.load_state()
                        self.assertTrue(resumed.identity_failed)
                    build.assert_not_called(); native.assert_not_called()

    def test_never_built_null_is_explicit_and_not_success(self):
        with tempfile.TemporaryDirectory() as temp:
            root = Path(temp); r = self.runner(root); r.persist()
            r.load_state(); self.assertIsNone(r.explicit_cli)
            state = json.loads((r.out / 'state.json').read_text())
            state['result']['stages']['unconfigured-jit']['passed'] = True
            (r.out / 'state.json').write_text(json.dumps(state))
            with self.assertRaises(IdentityError): r.load_state()

    def test_policy_and_actual_pe_calls_not_just_missing_report(self):
        off = {'compiled_feature': False, 'session_policy': 'explicit-only'}
        m.validate_policy(off, False)
        for wrong in [dict(off, compiled_feature=True), dict(off, session_policy='ordinary-dynamic-module-pilot')]:
            with self.assertRaises(ValueError): m.validate_policy(wrong, False)
        m.forbid_metadata_calls({'methods': [{'body': {'instructions': []}}]})
        for owner, method in [('PyDistributionSnapshot', 'Configure'), ('PyDistributionSnapshot', 'ConfigureAutoUnavailable'),
                              ('PyModules', 'ConfigureOptionalSources')]:
            with self.assertRaises(ValueError): m.forbid_metadata_calls({'methods': [{'body': {'instructions': [
                {'opcode': '0x28', 'target': {'owner': 'RyuPy.Runtime.' + owner, 'name': method}}]}}]})

    def test_empty_requires_actual_root_inventory_payload_and_order(self):
        with tempfile.TemporaryDirectory() as temp:
            root = Path(temp); data = {'schema_version': 1, 'mode': 'closed_snapshot', 'roots': ['search-0'], 'distributions': []}
            def check(data, report=None):
                payload = json.dumps(data)
                m.validate_empty(payload, report or dict(data, payload_sha256=m.contract.digest(payload.encode())), [root])
            check(data)
            for altered in [dict(data, roots=[]), dict(data, roots=['search-1']), dict(data, distributions=[{}])]:
                with self.assertRaises(ValueError): check(altered)
            with self.assertRaises(ValueError): check(data, dict(data, payload_sha256='wrong'))
            (root / 'Unexpected-1.dist-info').mkdir()
            with self.assertRaises(ValueError): check(data)

    def test_autoempty_does_not_write_synthetic_payload(self):
        # 真实checkpoint控制流/文件校验，只mock编译器/解码和JIT，不运行任何产物。
        with tempfile.TemporaryDirectory() as temp:
            root = Path(temp); r = self.runner(root); r.payload = root / 'original-synthetic'
            original = r.payload
            entry = r.fixture / 'automatic_empty/main.py'; entry.parent.mkdir(parents=True); entry.write_text('pass')
            target = r.out / 'products/automatic-empty'
            data = {'schema_version': 1, 'mode': 'closed_snapshot', 'roots': ['search-0'], 'distributions': []}
            payload = json.dumps(data)
            def compile(*args):
                target.mkdir(parents=True); (target / 'main.dll').write_bytes(b'fake PE')
                (target / 'RyuPy.Runtime.dll').write_bytes(r.runtime.read_bytes())
                (target / 'module-environment.json').write_text(json.dumps(
                    {'compiled_feature': True, 'session_policy': 'ordinary-dynamic-module-pilot'}))
                (target / 'distribution-metadata.json').write_text(json.dumps(
                    dict(data, payload_sha256=m.contract.digest(payload.encode()))))
                return {'exit': 0, 'failure': None, 'raw_streams_complete': True}
            with patch.object(r, 'command', side_effect=compile), patch.object(m, 'decode', return_value={}), \
                 patch.object(m.contract, 'pe_structure', return_value={}), \
                 patch.object(m.contract, 'pe_payload', return_value=payload), patch.object(r, 'run_positive') as run:
                m.autoempty(r); run.assert_called_once()
                self.assertEqual(r.payload, original)
                self.assertEqual(json.loads((r.out / 'automatic-empty-before-execution.json').read_text())['roots_from_argv'],
                                 [str(entry.parent)])
                with self.assertRaises(ValueError): m.autoempty(r)
            self.assertEqual((p.FIXTURES / 'automatic_empty/main.py').read_bytes(), (p.FIXTURES / 'empty.py').read_bytes())

    def test_missing_off_tool_blocks_only_old_negative(self):
        with tempfile.TemporaryDirectory() as temp:
            r = self.runner(Path(temp)); root = r.fixture / 'automatic_empty'; root.mkdir(parents=True)
            (root / 'main.py').write_bytes(b'original'); (r.fixture / 'empty.py').write_bytes(b'original')
            with patch.object(p, 'oracle'), patch.object(m, 'autoempty') as auto, patch.object(m, 'unconfigured') as old:
                m.run(r); old.assert_not_called(); auto.assert_called_once()
            self.assertEqual(r.result['stages']['unconfigured-jit']['status'], 'blocked')
            self.assertTrue(r.result['stages']['automatic-empty-jit']['passed'])
