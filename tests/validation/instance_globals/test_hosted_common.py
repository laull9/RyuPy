# 纯控制/负例测试；所有子进程为mock，不执行产品或CP。
import json
from pathlib import Path
import tempfile
import unittest
from unittest.mock import patch
import hosted_common as c


class Checks(unittest.TestCase):
    def test_metadata_from_deployment_not_observation(self):
        with tempfile.TemporaryDirectory() as t:
            p = Path(t) / 'manifest.json'
            p.write_text(json.dumps({'module_locations': [{'module_name': 'subject',
                'kind': 'executable_relative', 'relative_file': '_internal/resources/v1/subject.py'}]}))
            self.assertEqual(c.aot_origin(p, Path(t) / 'moved/Host'), str(Path(t) / 'moved/_internal/resources/v1/subject.py'))
            p.write_text(json.dumps({'module_locations': [{'module_name': 'subject',
                'kind': 'executable_relative', 'relative_file': '../escape.py'}]}))
            with self.assertRaises(RuntimeError): c.aot_origin(p, Path(t) / 'Host')

    def test_negative_rejects_signal_and_other_error(self):
        with tempfile.TemporaryDirectory() as t:
            root = Path(t)
            (root / 'result-kind.txt').write_text('has-errors\n')
            (root / 'diagnostics.txt').write_text('[E0100] unsupported')
            c.negative_result(root, {'exit': 1})
            for code in (-11, -6, 2, 134, 139):
                with self.assertRaises(RuntimeError): c.negative_result(root, {'exit': code})
            (root / 'result-kind.txt').write_text('other-error\n')
            with self.assertRaises(RuntimeError): c.negative_result(root, {'exit': 1})

    def test_negative_dll_not_allowed(self):
        with tempfile.TemporaryDirectory() as t:
            root = Path(t)
            (root / 'result-kind.txt').write_text('has-errors\n')
            (root / 'diagnostics.txt').write_text('[E0100] no')
            (root / 'entry.dll').write_bytes(b'bad')
            with self.assertRaises(RuntimeError): c.negative_result(root, {'exit': 1})

    def test_launch_failure_raw_preserved(self):
        with tempfile.TemporaryDirectory() as t:
            out = Path(t)
            with patch.object(c.subprocess, 'Popen', side_effect=OSError('mock failed')):
                with self.assertRaises(RuntimeError): c.capture(out, 'x', ['fake'], {}, out)
            self.assertEqual(json.loads((out / 'x.result.json').read_text())['launch_error'], 'OSError')
            self.assertEqual((out / 'x.stdout').read_bytes(), b'')
            self.assertIn(b'mock failed', (out / 'x.stderr').read_bytes())

    def test_artifact_path_cannot_escape(self):
        with tempfile.TemporaryDirectory() as t:
            root = Path(t); out = root / 'out'; out.mkdir()
            p = root / 'outside.dll'; p.write_bytes(b'PE')
            (out / 'artifact-dll.txt').write_text(str(p))
            with self.assertRaises(RuntimeError): c.read_artifacts(out)

    def test_exact_trx(self):
        with tempfile.TemporaryDirectory() as t:
            p = Path(t) / 'x.trx'
            names = ['CanonicalCallReturnsOneRealMutableDictionary', 'DefaultDeletionIsNameErrorAndStableIdentitySurvivesRebinding',
                     'ExactColdAdditionPreservesAll1716PriorRecords',
                     'ColdAdditionRejectsMissingChangedFlagsSignaturesAndOverloads',
                     'CandidateMatchingForgedPermissionCannotCrossGroupDuplicate']
            p.write_text('<TestRun>'+''.join('<UnitTestResult testName="'+n+'" outcome="Passed"/>' for n in names)+'</TestRun>')
            c.verify_five_trx(p)
            valid = p.read_text()
            p.write_text(valid.replace('</TestRun>', '<UnitTestResult testName="unexpected" outcome="Passed"/></TestRun>'))
            with self.assertRaises(RuntimeError): c.verify_five_trx(p)
            p.write_text(valid.replace('Passed', 'NotExecuted', 1))
            with self.assertRaises(RuntimeError): c.verify_five_trx(p)


if __name__ == '__main__': unittest.main()
