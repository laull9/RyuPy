"""仅自测证据保存的失败边界，不计原生程序验收。"""
from pathlib import Path
import tempfile
import unittest
from unittest.mock import patch
import tarfile
import sys
sys.path.insert(0, str(Path(__file__).resolve().parents[1]))
from native_evidence import preserve_dist, snapshot


class EvidenceTests(unittest.TestCase):
    def test_archive_precedes_failure_and_keeps_manifest(self):
        with tempfile.TemporaryDirectory() as root:
            root = Path(root)
            dist = root / 'program.dist'; dist.mkdir()
            output = root / 'evidence'; output.mkdir()
            (dist / 'program').write_bytes(b'\x7fELFfake')
            (dist / 'ryupy-artifact.json').write_text('{}')
            before = preserve_dist(dist, output)
            # 模拟后续首次执行失败；证据不依赖成功分支。
            with self.assertRaises(RuntimeError):
                raise RuntimeError('native failed')
            self.assertEqual(before, snapshot(dist)[0])
            self.assertTrue((output / 'dist-identity.json').is_file())
            with tarfile.open(output / 'native-dist.tar.gz') as archive:
                self.assertEqual(archive.extractfile('program.dist/program').read(), b'\x7fELFfake')
                self.assertEqual(archive.extractfile('program.dist/ryupy-artifact.json').read(), b'{}')

    def test_limits_and_links_are_rejected(self):
        with tempfile.TemporaryDirectory() as root:
            root = Path(root); (root / 'one').write_bytes(b'12')
            with patch('native_evidence.MAX_BYTES', 1), self.assertRaises(RuntimeError):
                snapshot(root)
            with patch('native_evidence.MAX_FILES', 0), self.assertRaises(RuntimeError):
                snapshot(root)
            (root / 'link').symlink_to(root / 'one')
            with self.assertRaises(RuntimeError):
                snapshot(root)


if __name__ == '__main__':
    unittest.main()
