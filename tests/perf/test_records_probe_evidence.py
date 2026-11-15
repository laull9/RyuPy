"""证据门自测：合成TRX只验证拒绝假绿，不冒充真实产品测试结果。"""
import copy
from pathlib import Path
import tempfile
import unittest
import xml.etree.ElementTree as ET

from check_records_probe_evidence import EXPECTED, NS, verify


def tag(name):
    return '{' + NS['t'] + '}' + name


class EvidenceTests(unittest.TestCase):
    def setUp(self):
        self.temp = tempfile.TemporaryDirectory()
        self.addCleanup(self.temp.cleanup)
        self.folder = Path(self.temp.name)
        (self.folder / 'commit.txt').write_text('abc')
        (self.folder / 'sdk.txt').write_text('10.0.403')
        self.root = ET.Element(tag('TestRun'))
        self.results = ET.SubElement(self.root, tag('Results'))
        names = sorted(EXPECTED) + [f'HighCardinalityDiagnostic(count: {n})' for n in (4096, 16384)]
        for index, name in enumerate(names):
            result = ET.SubElement(self.results, tag('UnitTestResult'),
                                   testName='RyuPy.Runtime.Tests.PyDictStringPairProbeTests.' + name,
                                   outcome='Passed', testId=f'test-{index}', executionId=f'run-{index}')
            if name.startswith('HighCardinality'):
                count = 4096 if '4096' in name else 16384
                output = ET.SubElement(result, tag('Output'))
                ET.SubElement(output, tag('StdOut')).text = '\n'.join(
                    f'records-probe count={count} probe={arm} ms=1.234 bytes=1234'
                    for arm in ('False', 'True'))
        self.summary = ET.SubElement(self.root, tag('ResultSummary'), outcome='Completed')
        self.counters = ET.SubElement(self.summary, tag('Counters'), total='15', executed='15', passed='15')
        for key in ('failed', 'error', 'timeout', 'aborted', 'inconclusive',
                    'passedButRunAborted', 'notRunnable', 'notExecuted', 'disconnected',
                    'warning', 'completed', 'inProgress', 'pending'):
            self.counters.set(key, '0')

    def check(self):
        ET.ElementTree(self.root).write(self.folder / 'records.trx')
        return verify(self.folder, 'abc')

    def test_valid(self):
        self.assertEqual(self.check()['passed'], 15)

    def test_duplicate_and_missing_ids(self):
        for key in ('testId', 'executionId'):
            original = self.results[1].attrib.copy()
            for bad in (self.results[0].get(key), ''):
                with self.subTest(key=key, bad=bad):
                    self.results[1].set(key, bad)
                    with self.assertRaises(AssertionError):
                        self.check()
            self.results[1].attrib = original

    def test_failed_or_duplicate_summary(self):
        for outcome in ('Failed', 'Aborted', ''):
            self.summary.set('outcome', outcome)
            with self.assertRaises(AssertionError):
                self.check()
        self.summary.set('outcome', 'Completed')
        self.root.append(copy.deepcopy(self.summary))
        with self.assertRaises(AssertionError):
            self.check()

    def test_inconsistent_counters(self):
        for key in self.counters.attrib:
            old = self.counters.get(key)
            self.counters.set(key, '1')
            with self.subTest(key=key), self.assertRaises(AssertionError):
                self.check()
            self.counters.set(key, old)

    def test_failed_skipped_missing_results(self):
        for outcome in ('Failed', 'NotExecuted'):
            self.results[0].set('outcome', outcome)
            with self.assertRaises(AssertionError):
                self.check()
        self.results[0].set('outcome', 'Passed')
        self.results.remove(self.results[-1])
        with self.assertRaises(AssertionError):
            self.check()

    def test_missing_diagnostic(self):
        self.results[-1].remove(self.results[-1].find(tag('Output')))
        with self.assertRaises(AssertionError):
            self.check()


if __name__ == '__main__':
    unittest.main()
