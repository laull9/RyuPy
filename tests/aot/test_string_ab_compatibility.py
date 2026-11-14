"""已知差异必须精确拒绝，任何漂移也继续观察其余臂。"""
import unittest
from string_ab_compatibility import ARMS, CP_OUTPUT, RYUPY_OUTPUT, observe


class CompatibilityTest(unittest.TestCase):
    def exercise(self, fault=None):
        calls = []
        report = {}
        class Runner:
            def run(self, label, command, **kwargs):
                arm = command[0]
                calls.append(arm)
                output = CP_OUTPUT if arm == 'cpython' else RYUPY_OUTPUT
                stderr = b''
                if arm == 'base_jit':
                    if fault == 'success': output = CP_OUTPUT
                    if fault == 'mismatch': output = b'unexpected\n'
                    if fault == 'stderr': stderr = b'warning'
                    if fault == 'timeout': raise TimeoutError('deadline')
                    if fault == 'exception': raise RuntimeError('different exception')
                return dict(case=label, exit=0, stdout=output, stderr=stderr)
        def invoke():
            observe(Runner(), {a:[a] for a in ARMS}, '.', {}, report, lambda: None)
        if fault:
            with self.assertRaises(ValueError): invoke()
        else:
            invoke()
        self.assertEqual(calls, list(ARMS))
        self.assertFalse(report['known_incompatibility']['cpython_equivalent'])
        self.assertEqual(len(report['known_incompatibility']['observations']), 5)
        self.assertEqual(report['known_incompatibility']['observations'][-1]['status'], 'expected-unsupported')
        return report

    def test_exact_known_difference(self): self.exercise()

    def test_drift_collects_remaining_arms_then_fails(self):
        for fault in ('success', 'mismatch', 'stderr', 'timeout', 'exception'):
            with self.subTest(fault=fault): self.exercise(fault)


if __name__ == '__main__': unittest.main()
