"""真实Windows宿主测试TRX门，防止零匹配/skip伪装成验收。"""
from pathlib import Path
import tempfile
import unittest
from platform_project import check_windows_path_tests

class WindowsPathReceiptTests(unittest.TestCase):
    def test_exact_three_executed_tests_required(self):
        names=('LexicalObjectsAndContextBoundaries','FactoriesHaveHostIdentity','RegisteredResourceUsesRealIsolatedFiles')
        rows=''.join('<UnitTestResult testName="RyuPy.Runtime.Tests.PyWindowsPathHostTests.'+n+'" outcome="Passed" />' for n in names)
        good='<TestRun xmlns="urn:trx"><Results>'+rows+'</Results><ResultSummary><Counters total="3" executed="3" passed="3" failed="0" notExecuted="0" /></ResultSummary></TestRun>'
        with tempfile.TemporaryDirectory() as folder:
            p=Path(folder)/'receipt.trx';p.write_text(good);check_windows_path_tests(p)
            for bad in (good.replace('outcome="Passed"','outcome="NotExecuted"',1),good.replace('executed="3"','executed="0"'),good.replace(names[0],'wrong'),good.replace(rows,''),good.replace(rows,rows+rows)):
                p.write_text(bad)
                with self.assertRaises(ValueError):check_windows_path_tests(p)

if __name__=='__main__':unittest.main()
