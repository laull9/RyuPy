"""验收器轻量负例：快照不跟链接、计数边界、失败冒充成功与只读观察。"""
import json
import os
from pathlib import Path
import subprocess
import tempfile
import unittest
from unittest.mock import patch
import cases
from common import digest, save, tree


class Contracts(unittest.TestCase):
    def setUp(self):
        self.temp = tempfile.TemporaryDirectory(); self.root = Path(self.temp.name)
        self.package = self.root/'package'; self.package.mkdir(); (self.package/'native').write_bytes(b'original')
        self.manifest = {'schema_version': 1, 'kind': 'ryupy-native-aot-artifact',
                         'artifacts': [{'relative_path': 'native', 'role': 'native_executable',
                                        'size': 8, 'sha256': digest(self.package/'native')}]}
        save(self.package/cases.MANIFEST, self.manifest)
        self.logs = self.root/'logs'; self.marker = self.root/'marker'

    def tearDown(self):
        self.temp.cleanup()

    def report(self):
        return {'report_schema_version': 1, 'kind': 'ryupy-distribution-integrity-verification',
                'content_integrity_verified': True, 'manifest_schema_version': 1,
                'reported_target_rid': None, 'native_executable': 'native', 'artifact_count': 1,
                'total_bytes': 8, 'authenticity': 'not_checked', 'runtime_compatibility': 'not_checked',
                'schema_semantics': 'not_checked'}

    def check(self, output, success=True):
        with patch('cases.capture', return_value=output):
            return cases.verify('cli', self.package, 'test', self.logs, self.root, {}, self.marker, success)

    def test_valid_report_and_no_write(self):
        self.check(subprocess.CompletedProcess([], 0, json.dumps(self.report()).encode(), b''))

    def test_false_count_rejected(self):
        report = self.report(); report['total_bytes'] += 1
        with self.assertRaises(ValueError):
            self.check(subprocess.CompletedProcess([], 0, json.dumps(report).encode(), b''))

    def test_failure_cannot_claim_success(self):
        with self.assertRaises(ValueError):
            self.check(subprocess.CompletedProcess([], 1, json.dumps(self.report()).encode(), b'bad'), False)

    def test_plain_failure_accepted(self):
        self.check(subprocess.CompletedProcess([], 1, b'', b'bad'), False)

    def test_write_detected(self):
        def run(*_):
            (self.package/'native').write_bytes(b'modified')
            return subprocess.CompletedProcess([], 1, b'', b'bad')
        with patch('cases.capture', side_effect=run), self.assertRaises(ValueError):
            cases.verify('cli', self.package, 'test', self.logs, self.root, {}, self.marker, False)

    def test_link_not_followed(self):
        outside = self.root/'outside'; outside.write_bytes(b'one')
        try:
            (self.package/'link').symlink_to(outside)
        except OSError as error:
            if os.name == 'nt' and error.winerror == 1314:
                self.skipTest('Windows创建符号链接权限不足')
            raise
        before = tree(self.package); outside.write_bytes(b'two')
        self.assertEqual(before, tree(self.package))
        self.assertEqual(next(r for r in before if r['path'] == 'link')['kind'], 'link')

    def test_tool_marker_rejected(self):
        self.marker.write_text('started')
        with self.assertRaises(ValueError):
            self.check(subprocess.CompletedProcess([], 0, json.dumps(self.report()).encode(), b''))


if __name__ == '__main__': unittest.main()
