"""只使用合成JSON/TRX/文本检查拒绝边界；不构建、不执行CP/CLI/PE/Native。"""
import json
from pathlib import Path
import tempfile
import unittest

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
                'admitted': True, 'unit_link_mapping_verified': True, 'frame_carriers_verified': True}

    def test_complete_fixed_proof_is_consumed(self):
        self.assertTrue(pe_review(self.proof(), 'new', 'fixture', 'actual')['admitted'])

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
