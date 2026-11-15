"""实体key证据门合成正反例；不替代C#/Rust运行。"""
import unittest
import xml.etree.ElementTree as ET
from check_records_materialized import CASES, CLASS, MIR, IGNORED, NS, check_cs, check_mir


def tag(name):
    return '{'+NS['t']+'}'+name


def fixture():
    root = ET.Element(tag('TestRun')); rows = ET.SubElement(root, tag('Results'))
    for i, name in enumerate(sorted(CASES)):
        ET.SubElement(rows, tag('UnitTestResult'), testName=CLASS+name, outcome='Passed', testId=str(i), executionId=str(i))
    summary = ET.SubElement(root, tag('ResultSummary'), outcome='Completed')
    counter = ET.SubElement(summary, tag('Counters'), total='11', executed='11', passed='11')
    for name in ('failed', 'error', 'timeout', 'aborted', 'inconclusive', 'passedButRunAborted',
                 'notRunnable', 'notExecuted', 'disconnected', 'warning', 'completed', 'inProgress', 'pending'):
        counter.set(name, '0')
    return root


class EvidenceTests(unittest.TestCase):
    def test_cs_valid_and_bad_identity_outcome(self):
        self.assertEqual(check_cs(fixture())['passed'], 11)
        for key, value in [('testId', '1'), ('executionId', ''), ('testName', CLASS+'wrong'), ('outcome', 'NotExecuted')]:
            root = fixture(); root.find('.//t:UnitTestResult', NS).set(key, value)
            with self.subTest(key=key), self.assertRaises(AssertionError): check_cs(root)

    def test_cs_bad_summary_missing_case(self):
        root = fixture();root.find('t:ResultSummary', NS).set('outcome', 'Failed')
        with self.assertRaises(AssertionError): check_cs(root)
        root = fixture();root.find('.//t:Counters', NS).set('failed', '1')
        with self.assertRaises(AssertionError): check_cs(root)
        root = fixture();rows=root.find('t:Results', NS);rows.remove(rows[0])
        with self.assertRaises(AssertionError): check_cs(root)

    def test_mir_complete_and_bad_matrix(self):
        lines = [f'test {n} ... ok' for n in sorted(MIR)]+['test old::case ... ok']
        lines += [f'test {n} ... ignored' for n in sorted(IGNORED)]
        lines += ['test result: ok. 4 passed; 0 failed; 4 ignored; 0 measured; 0 filtered out;']
        text='\n'.join(lines);self.assertEqual(check_mir(text)['new_structure_tests'],3)
        for bad in ('', text.replace('0 filtered out','1 filtered out'), text.replace('4 passed','0 passed'),
                    text.replace(next(iter(MIR)), 'unknown::case'), text.replace('... ok','... FAILED',1),
                    text+'\n'+lines[0]):
            with self.assertRaises(AssertionError): check_mir(bad)


if __name__ == '__main__':
    unittest.main()
