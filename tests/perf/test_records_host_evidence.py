"""host诊断证据门合成负例；不能替代真实SDK运行。"""
from pathlib import Path
import tempfile
import unittest
import xml.etree.ElementTree as ET
from check_records_host_evidence import SCENARIOS, verify
from check_records_probe_evidence import NS


def tag(name):
    return '{' + NS['t'] + '}' + name


class HostEvidenceTests(unittest.TestCase):
    def setUp(self):
        self.temp = tempfile.TemporaryDirectory(); self.addCleanup(self.temp.cleanup)
        self.folder = Path(self.temp.name)
        (self.folder / 'commit.txt').write_text('abc'); (self.folder / 'sdk.txt').write_text('10.0.403')
        self.root = ET.Element(tag('TestRun')); self.results = ET.SubElement(self.root, tag('Results'))
        for i, (count, miss, collide, invalidate) in enumerate(sorted(SCENARIOS)):
            name = ('RyuPy.Runtime.Tests.PyDictStringPairProbeDiagnosticTests.'
                    f'PairedGetIncrementSetHostDiagnostic(cardinality: {count}, missPercent: {miss}, collision: {collide}, invalidate: {invalidate})')
            result = ET.SubElement(self.results, tag('UnitTestResult'), testName=name, outcome='Passed', testId=str(i), executionId=str(i))
            output = ET.SubElement(result, tag('Output'))
            text = '\n'.join(f'host-probe keys={count} missPercent={miss} collision={collide} invalidate={invalidate} round={r} order={"AB" if r%2==0 else "BA"} A_ms=2.000 B_ms=1.000 A_bytes=2000 B_bytes=1000 fallback={30000 if invalidate else 0}' for r in range(6))
            ET.SubElement(output, tag('StdOut')).text = text+'\nhost-probe median A_ms=2.000 B_ms=1.000; host only'
        self.summary = ET.SubElement(self.root, tag('ResultSummary'), outcome='Completed')
        self.counter = ET.SubElement(self.summary, tag('Counters'), total='6', executed='6', passed='6')
        for key in ('failed', 'error', 'timeout', 'aborted', 'inconclusive', 'passedButRunAborted', 'notRunnable', 'notExecuted', 'disconnected', 'warning', 'completed', 'inProgress', 'pending'):
            self.counter.set(key, '0')

    def check(self):
        ET.ElementTree(self.root).write(self.folder / 'host.trx')
        return verify(self.folder, 'abc')

    def test_valid(self):
        self.assertEqual(self.check()['samples'], 36)

    def test_corrupt_samples(self):
        output = self.results[0].find('.//'+tag('StdOut')); original = output.text
        for before, after in [('round=0', 'round=1'), ('order=AB', 'order=BA'),
                              ('fallback=0', 'fallback=1'), ('A_bytes=2000', 'A_bytes=0'),
                              ('A_ms=2.000', 'A_ms=nan'), ('keys=32', 'keys=33'),
                              ('median A_ms=2.000', 'median A_ms=9.000')]:
            output.text = original.replace(before, after, 1)
            with self.subTest(before=before), self.assertRaises(AssertionError): self.check()
        output.text = '\n'.join(original.splitlines()[1:])
        with self.assertRaises(AssertionError): self.check()

    def test_ids_status_and_summary(self):
        for key, bad in [('testId', self.results[1].get('testId')), ('executionId', ''), ('outcome', 'NotExecuted'), ('testName', self.results[1].get('testName'))]:
            old = self.results[0].get(key); self.results[0].set(key, bad)
            with self.subTest(key=key), self.assertRaises(AssertionError): self.check()
            self.results[0].set(key, old)
        self.summary.set('outcome', 'Failed')
        with self.assertRaises(AssertionError): self.check()
        self.summary.set('outcome', 'Completed'); self.counter.set('failed', '1')
        with self.assertRaises(AssertionError): self.check()

    def test_missing_test_and_wrong_identity(self):
        (self.folder / 'sdk.txt').write_text('10.0.999')
        with self.assertRaises(AssertionError): self.check()
        (self.folder / 'sdk.txt').write_text('10.0.403')
        self.results.remove(self.results[-1])
        with self.assertRaises(AssertionError): self.check()


if __name__ == '__main__':
    unittest.main()
