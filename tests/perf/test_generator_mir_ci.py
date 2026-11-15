"""MIR证据解析器的合成正反例，不等同于真实Rust验证。"""
import unittest
from check_generator_mir_ci import EXPECTED, IGNORED, validate


def log(focused=True):
    rows = [f'test {name} ... ok' for name in sorted(EXPECTED)]
    if not focused:
        rows += ['test existing::regression ... ok']
        rows += [f'test {name} ... ignored, 已登记旧实验' for name in sorted(IGNORED)]
    rows.append(f'test result: ok. {len(EXPECTED) if focused else len(EXPECTED)+1} passed; 0 failed; '
                f'{0 if focused else 4} ignored; 0 measured; {100 if focused else 0} filtered out; finished in 0.01s')
    return '\n'.join(rows)


class EvidenceTests(unittest.TestCase):
    def test_valid_focused_and_full(self):
        self.assertEqual(validate(log(), True)['passed'], len(EXPECTED))
        self.assertEqual(validate(log(False), False)['ignored'], sorted(IGNORED))

    def test_missing_failed_ignored_duplicate_case(self):
        original = log()
        line = original.splitlines()[0]
        for changed in ('', line.replace('ok', 'FAILED'), line.replace('ok', 'ignored'), line+'\n'+line):
            with self.subTest(changed=changed), self.assertRaises(AssertionError):
                validate(original.replace(line, changed), True)

    def test_summary_mismatch(self):
        for changed in ('0 passed', '21 passed', '23 passed'):
            with self.subTest(changed=changed), self.assertRaises(AssertionError):
                validate(log().replace('22 passed', changed), True)
        with self.assertRaises(AssertionError):
            validate(log().replace('test result: ok.', 'test result: FAILED.'), True)

    def test_full_filtered_or_ignored_drift(self):
        for text in (log(False).replace('0 filtered out', '1 filtered out'),
                     log(False).replace(next(iter(IGNORED)), 'unexpected::ignored'),
                     log(False).replace('4 ignored', '0 ignored')):
            with self.assertRaises(AssertionError):
                validate(text, False)

    def test_empty_and_duplicate_summary(self):
        for text in ('', log()+'\n'+log().splitlines()[-1]):
            with self.assertRaises(AssertionError):
                validate(text, True)


if __name__ == '__main__':
    unittest.main()
