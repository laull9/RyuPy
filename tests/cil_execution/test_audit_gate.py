"""执行门负例：缺测、伪零、丢帧、替换求值器及计数损坏都必须失败。"""
import copy
import unittest

from check_audit import PHASES, audit_failures


class AuditGateTests(unittest.TestCase):
    def complete(self):
        return {'state': 'closed', 'execution_audit': {
            'enabled': True, 'complete': True, 'startup_complete': True,
            'hook_installed': True, 'finalized': True,
            'dropped_frames': 0, 'coverage_failures': 0,
            **{phase + '_frames': 0 for phase in PHASES}, 'frames': []}}

    def test_complete_zero_contract(self):
        self.assertEqual(audit_failures(self.complete()), [])

    def test_incomplete_zero_never_passes(self):
        for field in ('enabled', 'complete', 'startup_complete', 'hook_installed', 'finalized'):
            report = self.complete()
            report['execution_audit'][field] = False
            self.assertTrue(audit_failures(report), field)
        report = self.complete()
        report['state'] = 'running'
        self.assertTrue(audit_failures(report))

    def test_missing_audit_and_missing_fields(self):
        for report in (None, [], {}, {'execution_audit': {}}):
            self.assertTrue(audit_failures(report))
        for field in self.complete()['execution_audit']:
            report = self.complete()
            del report['execution_audit'][field]
            self.assertTrue(audit_failures(report), field)

    def test_each_phase_executing_is_failure(self):
        for phase in PHASES:
            report = self.complete()
            report['execution_audit'][phase + '_frames'] = 1
            report['execution_audit']['frames'] = [
                {'phase': phase, 'file': '<frozen importlib>', 'function': 'run', 'calls': 1}]
            self.assertTrue(audit_failures(report))

    def test_invalid_or_inconsistent_counts(self):
        for field in ('dropped_frames', 'coverage_failures', 'startup_frames', 'runtime_frames', 'shutdown_frames'):
            for value in (True, -1, '0', None, 1):
                report = self.complete()
                report['execution_audit'][field] = value
                self.assertTrue(audit_failures(report), (field, value))
        report = self.complete()
        report['execution_audit']['frames'] = [
            {'phase': 'runtime', 'file': 'x', 'function': 'f', 'calls': 1}]
        self.assertTrue(audit_failures(report))

    def test_malformed_rows(self):
        row = {'phase': 'runtime', 'file': 'x', 'function': 'f', 'calls': 1}
        for key, value in (('phase', 'unknown'), ('file', None), ('function', 1),
                           ('calls', True), ('calls', 0)):
            malformed = copy.copy(row)
            malformed[key] = value
            report = self.complete()
            report['execution_audit']['frames'] = [malformed]
            self.assertTrue(audit_failures(report))


if __name__ == '__main__':
    unittest.main()
