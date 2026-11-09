"""只使用合成JSON/TRX/文本检查拒绝边界；不构建、不执行CP/CLI/PE/Native。"""
import json
from pathlib import Path
import tempfile
import unittest
from unittest.mock import Mock, patch

import bootstrap
from bootstrap import verify_trx, pe_review, dynamic_none
from host_gate import compare_host, old_pe_review


class SourceAdmissionTests(unittest.TestCase):
    def trx(self, rows):
        root = '<TestRun><TestDefinitions>'
        for i, (name, _, cls) in enumerate(rows):
            root += f'<UnitTest id="{i}"><TestMethod name="{name}" className="{cls}"/></UnitTest>'
        root += '</TestDefinitions><Results>'
        for i, (_, outcome, _) in enumerate(rows):
            root += f'<UnitTestResult testId="{i}" outcome="{outcome}"/>'
        return root + '</Results></TestRun>'

    def verify_rows(self, rows):
        with tempfile.TemporaryDirectory() as temp:
            path = Path(temp) / 'result.trx'; path.write_text(self.trx(rows))
            return verify_trx(path, {'Actual': 1}, 'ExpectedClass')

    def test_exact_named_runtime_result(self):
        self.assertEqual(self.verify_rows([('Actual', 'Passed', 'ExpectedClass')]), {'Actual': 1})

    def test_zero_missing_duplicate_skip_or_wrong_class_rejected(self):
        for rows in [[], [('Other', 'Passed', 'ExpectedClass')], [('Actual', 'NotExecuted', 'ExpectedClass')],
                     [('Actual', 'Passed', 'WrongClass')], [('Actual', 'Passed', 'ExpectedClass')] * 2]:
            with self.subTest(rows=rows), self.assertRaises((RuntimeError, KeyError)):
                self.verify_rows(rows)

    def proof(self):
        return {'role': 'new', 'case_id': 'fixture', 'sha256': 'actual', 'pe_sha256': 'actual',
                'admitted': True, 'unit_link_mapping_verified': True, 'frame_carriers_verified': True,
                'location_mode': 'jit_files'}

    def test_complete_fixed_proof_is_consumed(self):
        self.assertTrue(pe_review(self.proof(), 'new', 'fixture', 'actual')['admitted'])

    def test_same_named_native_consumer_is_valid_default_jit(self):
        proof = dict(self.proof(), case_id='native_consumers')
        review = pe_review(proof, 'new', 'native_consumers', 'actual')
        self.assertTrue(review['admitted'])
        self.assertNotIn('native_manifest_sha256', review)

    def test_native_mode_requires_explicit_same_manifest_and_fixed_case(self):
        proof = dict(self.proof(), case_id='native_consumers', location_mode='native_consumers_aot',
                     native_manifest_sha256='d' * 64)
        with self.assertRaises(RuntimeError): pe_review(proof, 'new', 'native_consumers', 'actual')
        for kwargs in ({'location_mode': 'native_consumers_aot'},
                       {'location_mode': 'native_consumers_aot', 'expected_manifest_sha256': 'e' * 64},
                       {'location_mode': 'unknown', 'expected_manifest_sha256': 'd' * 64}):
            with self.subTest(kwargs=kwargs), self.assertRaises(RuntimeError):
                pe_review(proof, 'new', 'native_consumers', 'actual', **kwargs)
        native = {'location_mode': 'native_consumers_aot', 'expected_manifest_sha256': 'd' * 64}
        self.assertTrue(pe_review(proof, 'new', 'native_consumers', 'actual', **native)['admitted'])
        for key in ('location_mode', 'native_manifest_sha256'):
            bad = dict(proof); del bad[key]
            with self.assertRaises(RuntimeError): pe_review(bad, 'new', 'native_consumers', 'actual', **native)
        for role, case in [('old', 'native_consumers'), ('new', 'fixture')]:
            bad = dict(proof, role=role, case_id=case)
            with self.assertRaises(RuntimeError): pe_review(bad, role, case, 'actual', **native)
        jit = dict(self.proof(), case_id='native_consumers')
        with self.assertRaises(RuntimeError): pe_review(jit, 'new', 'native_consumers', 'actual', **native)
        with self.assertRaises(RuntimeError):
            pe_review(jit, 'new', 'native_consumers', 'actual', expected_manifest_sha256='d' * 64)

    def exercise_jit_calls(self, proof):
        """执行现存编排函数，所有产品/Host执行入口均mock；只核审核参数与停止边界。"""
        input_root = Path(bootstrap.INPUT_SOURCE)
        contract = json.loads((input_root / 'contract.json').read_text())
        case = next(case for case in contract['cases'] if case['id'] == 'native_consumers')
        with tempfile.TemporaryDirectory() as temp:
            out = Path(temp)
            for folder in ('build/results', 'build/logs'): (out / folder).mkdir(parents=True)
            (out / 'build/results/native_consumers-default.json').write_text(json.dumps({'pe': {'sha256': 'actual'}}))
            (out / 'build/logs/native_consumers-default-pe-read.stdout').write_text(json.dumps(proof))
            runner = Mock()
            with patch.object(bootstrap, 'verify_trx', return_value={}), patch.object(bootstrap, 'space'), \
                 patch.object(bootstrap, 'dynamic_none', return_value={}), patch('host_gate.execute') as host, \
                 patch.object(bootstrap, 'pe_review', wraps=pe_review) as checked:
                try:
                    bootstrap.execute_jit(out, runner, input_root, {'cases': [case], 'boundary_cases': []},
                                          {'files': {'dotnet': {'path': 'not-executed-dotnet'}}}, {})
                except RuntimeError:
                    self.assertNotIn('jit-stage', [call.args[0] for call in runner.run.call_args_list])
                    host.assert_not_called()
                    raise
                host.assert_called_once()
            self.assertEqual(checked.call_args.kwargs, {'location_mode': 'jit_files'})
            labels = [call.args[0] for call in runner.run.call_args_list]
            self.assertEqual(labels, ['runtime-tests', 'cp-stage', 'build-stage', 'jit-stage'])
            return json.loads((out / 'pe-reviews/native_consumers-default.json').read_text())

    def test_execute_jit_same_named_consumer_uses_jit_domain(self):
        review = self.exercise_jit_calls(dict(self.proof(), case_id='native_consumers'))
        self.assertTrue(review['admitted'])
        self.assertNotIn('native_manifest_sha256', review)

    def test_execute_jit_rejects_aot_proof_before_jit_run(self):
        proof = dict(self.proof(), case_id='native_consumers', location_mode='native_consumers_aot',
                     native_manifest_sha256='d' * 64)
        with self.assertRaisesRegex(RuntimeError, '审核域'):
            self.exercise_jit_calls(proof)

    def test_execute_native_forwards_published_manifest_and_domain(self):
        input_root = Path(bootstrap.INPUT_SOURCE)
        proof = dict(self.proof(), case_id='native_consumers', location_mode='native_consumers_aot',
                     native_manifest_sha256='d' * 64)
        for mode in ('native_consumers_aot', 'jit_files'):
            with self.subTest(mode=mode), tempfile.TemporaryDirectory() as temp:
                out = Path(temp); (out / 'publish/logs').mkdir(parents=True)
                (out / 'publish/logs/native-pe-read.stdout').write_text(json.dumps(dict(proof, location_mode=mode)))
                (out / 'publish/publish.json').write_text(json.dumps({'managed_sha256': 'actual', 'manifest_sha256': 'd' * 64}))
                runner = Mock(); runner.run.return_value = Mock(returncode=0, stdout=b'model-only\n', stderr=b'')
                with patch.object(bootstrap, 'pe_review', wraps=pe_review) as checked:
                    if mode == 'jit_files':
                        with self.assertRaisesRegex(RuntimeError, '审核域'):
                            bootstrap.execute_native(out, runner, input_root, {'files': {'cp': {'path': 'not-executed-cp'}}}, {})
                    else:
                        bootstrap.execute_native(out, runner, input_root, {'files': {'cp': {'path': 'not-executed-cp'}}}, {})
                        review = json.loads((out / 'native-pe-review.json').read_text())
                        self.assertEqual(review['native_manifest_sha256'], 'd' * 64)
                self.assertEqual(checked.call_args.kwargs,
                                 {'location_mode': 'native_consumers_aot', 'expected_manifest_sha256': 'd' * 64})
                labels = [call.args[0] for call in runner.run.call_args_list]
                self.assertEqual(labels, ['native-cp', 'native-publish'] + (['native-run'] if mode != 'jit_files' else []))

    def test_finite_symbols_are_not_admission(self):
        with self.assertRaises(RuntimeError):
            pe_review({'role': 'new', 'case_id': 'fixture', 'sha256': 'actual', 'finite_checks_passed': True},
                      'new', 'fixture', 'actual')

    def test_wrong_pe_role_or_missing_chain_rejected(self):
        for key, value in [('role', 'old'), ('pe_sha256', 'different'), ('case_id', 'other'),
                           ('unit_link_mapping_verified', False), ('frame_carriers_verified', False)]:
            proof = self.proof(); proof[key] = value
            with self.subTest(key=key), self.assertRaises(RuntimeError):
                pe_review(proof, 'new', 'fixture', 'actual')

    def test_old_host_locator_never_becomes_new_source_admission(self):
        proof = {'role': 'old', 'case_id': 'host_xy', 'sha256': 'actual', 'pe_sha256': 'actual',
                 'admitted': False, 'old_locator_verified': True}
        self.assertTrue(old_pe_review(proof, 'actual')['old_locator_verified'])
        for key, value in [('role', 'new'), ('case_id', 'fixture'), ('sha256', 'wrong'),
                           ('pe_sha256', 'wrong'), ('admitted', True), ('old_locator_verified', False)]:
            bad = dict(proof); bad[key] = value
            with self.subTest(key=key), self.assertRaises(RuntimeError): old_pe_review(bad, 'actual')
        with self.assertRaises(RuntimeError): pe_review(proof, 'old', 'host_xy', 'actual')

    def dynamic(self, old, cp, new):
        with tempfile.TemporaryDirectory() as temp:
            root = Path(temp)
            for folder in ['input/oracles', 'cp/td_dynamic', 'jit/logs']: (root / folder).mkdir(parents=True)
            (root / 'input/oracles/td_dynamic.old.stdout').write_text(old + '\n')
            (root / 'cp/td_dynamic/stdout').write_text(cp + '\n')
            (root / 'jit/logs/td_dynamic-default-jit.stdout').write_text(new + '\n')
            return dynamic_none(root / 'input', root / 'cp', root / 'jit')

    def test_dynamic_none_guard_cannot_regress_old_correct_output(self):
        cp = "fallback none-control ok str 'typing' False ['module-bool']"
        ni = "fallback none-control error NotImplementedError TypedDict metaclass default module filling is not supported ['module-bool']"
        with self.assertRaises(RuntimeError): self.dynamic(cp, cp, ni)
        value = self.dynamic("fallback none-control ok str 'subject' False []", cp, ni)
        self.assertEqual(value['status'], 'explicit-boundary-old-was-incorrect')

    def test_host_none_requires_its_own_old_control(self):
        cp = "entry-fallback none-control ok str 'typing' False ['entry-bool']\n"
        ni = "entry-fallback none-control error NotImplementedError TypedDict metaclass default module filling is not supported ['entry-bool']\n"
        with self.assertRaises(RuntimeError): compare_host(cp.encode(), cp.encode(), ni.encode())
        with self.assertRaises(RuntimeError): compare_host(cp.encode(), b'no none observation\n', ni.encode())
        accepted = compare_host(cp.encode(), b"entry-fallback none-control ok str 'source_x' False []\n", ni.encode())
        self.assertEqual(len(accepted['none']), 1)

    def test_host_ordinary_mismatch_is_not_boundary(self):
        cp = "entry-fallback none-control ok str 'typing' False ['entry-bool']\nxy-count 1\n"
        bad = cp.replace('xy-count 1', 'xy-count 2')
        with self.assertRaises(RuntimeError): compare_host(cp.encode(), cp.encode(), bad.encode())


if __name__ == '__main__':
    unittest.main()
