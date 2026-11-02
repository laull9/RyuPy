"""只造普通字节文件验清单，不把伪文件声称为NativeAOT。"""
import hashlib
import json
import os
from pathlib import Path
import tempfile
import unittest
from types import SimpleNamespace
from manifest_files import (ManifestError, check_artifacts, relative_path,
                            check_read_snapshots, digest, stat_snapshot)


class FileManifestTests(unittest.TestCase):
    def prepare(self, directory):
        root = Path(directory)
        (root/'ryupy-artifact.json').write_text('{}')
        (root/'program').write_bytes(b'not a native executable')
        (root/'symbols').mkdir()
        (root/'symbols/program.dbg').write_bytes(b'debug bytes')
        entries = [{'relative_path':p, 'size':len((root/p).read_bytes()),
                    'sha256':hashlib.sha256((root/p).read_bytes()).hexdigest()}
                   for p in ('program', 'symbols/program.dbg')]
        return root, entries

    def test_exact_files_include_debug_sidecar_and_reject_extra_or_missing(self):
        with tempfile.TemporaryDirectory() as directory:
            root, entries = self.prepare(directory)
            self.assertEqual(len(check_artifacts(root, entries, 'ryupy-artifact.json')), 3)
            (root/'extra').write_bytes(b'new')
            with self.assertRaises(ManifestError):
                check_artifacts(root, entries, 'ryupy-artifact.json')
            (root/'extra').unlink()
            (root/'symbols/program.dbg').unlink()
            with self.assertRaises(ManifestError):
                check_artifacts(root, entries, 'ryupy-artifact.json')

    def test_same_size_mutation_and_wrong_size_rejected(self):
        with tempfile.TemporaryDirectory() as directory:
            root, entries = self.prepare(directory)
            original = (root/'program').read_bytes()
            (root/'program').write_bytes(b'x'*len(original))
            with self.assertRaises(ManifestError):
                check_artifacts(root, entries, 'ryupy-artifact.json')
            (root/'program').write_bytes(original)
            entries[0]['size'] += 1
            with self.assertRaises(ManifestError):
                check_artifacts(root, entries, 'ryupy-artifact.json')

    def test_path_normalization_duplicate_self_digest_and_bad_hash(self):
        for value in ('', '/x', 'C:/x', r'C:\x', '../x', 'a/../x', 'a//x', './x', 'a/', 'a:b', '\0'):
            with self.subTest(value=value), self.assertRaises(ManifestError):
                relative_path(value)
        with tempfile.TemporaryDirectory() as directory:
            root, entries = self.prepare(directory)
            for extra in ({**entries[0]}, {**entries[0], 'relative_path':'ryupy-artifact.json'},
                          {**entries[0], 'relative_path':'PROGRAM'},
                          {**entries[0], 'relative_path':'RYUPY-ARTIFACT.JSON'},
                          {**entries[0], 'relative_path':'.ryupy-artifact.json.tmp'}):
                with self.subTest(extra=extra), self.assertRaises(ManifestError):
                    check_artifacts(root, entries+[extra], 'ryupy-artifact.json', windows=True)
            entries[0]['sha256'] = '0'*63
            with self.assertRaises(ManifestError):
                check_artifacts(root, entries, 'ryupy-artifact.json')

    @unittest.skipUnless(os.name == 'posix', 'POSIX真实链接/设备类型边界')
    def test_symlink_and_non_regular_file_rejected(self):
        with tempfile.TemporaryDirectory() as directory:
            root, entries = self.prepare(directory)
            (root/'link').symlink_to('program')
            with self.assertRaises(ManifestError):
                check_artifacts(root, entries, 'ryupy-artifact.json')
            (root/'link').unlink()
            os.mkfifo(root/'fifo')
            with self.assertRaises(ManifestError):
                check_artifacts(root, entries, 'ryupy-artifact.json')


class ReadSnapshotTests(unittest.TestCase):
    def snapshots(self, windows):
        values = dict(st_dev=1, st_ino=2, st_size=3, st_mtime_ns=4,
                      st_ctime_ns=5, st_birthtime_ns=5)
        result = [SimpleNamespace(**values) for _ in range(4)]
        if windows:
            result[1].st_ctime_ns = result[2].st_ctime_ns = 9
        return result

    def test_windows_ctime_api_difference_is_not_a_mutation(self):
        values = self.snapshots(True)
        check_read_snapshots(*values, 3, windows=True)
        with self.assertRaises(ManifestError):
            check_read_snapshots(*values, 3, windows=False)
        check_read_snapshots(*self.snapshots(False), 3, windows=False)

    def test_each_real_identity_time_size_change_and_short_read_rejected(self):
        for windows in (False, True):
            fields = ['st_dev', 'st_ino', 'st_size', 'st_mtime_ns', 'st_ctime_ns']
            if windows:
                fields.append('st_birthtime_ns')
            for position in range(4):
                for field in fields:
                    with self.subTest(windows=windows, position=position, field=field):
                        values = self.snapshots(windows)
                        setattr(values[position], field, getattr(values[position], field) + 1)
                        with self.assertRaises(ManifestError) as caught:
                            check_read_snapshots(*values, 3, windows=windows)
                        self.assertIn('before_path', str(caught.exception))
                        self.assertIn('after_read_fd', str(caught.exception))
            with self.assertRaises(ManifestError):
                check_read_snapshots(*self.snapshots(windows), 2, windows=windows)
        values = self.snapshots(True)
        for info in values:
            del info.st_birthtime_ns
        with self.assertRaises(ManifestError):
            check_read_snapshots(*values, 3, windows=True)

    @unittest.skipUnless(os.name == 'nt', '只在真实 Windows 记录 path/fd 时间语义')
    def test_windows_written_file_and_utime_digest(self):
        with tempfile.TemporaryDirectory() as directory:
            path = Path(directory) / 'written.bin'
            data = b'actual Windows file, not a synthetic native binary'
            path.write_bytes(b'first')
            path.write_bytes(data)
            # 明确变更写时间；ctime 差异是否可见由实际文件系统值报告，不猜精度。
            os.utime(path, ns=(946684800000000000, 946684800000000000))
            before = path.lstat()
            with path.open('rb') as stream:
                opened = os.fstat(stream.fileno())
                actual = stream.read()
                after_read = os.fstat(stream.fileno())
            after = path.lstat()
            print('windows-stat-evidence=' + json.dumps({
                name: stat_snapshot(info) for name, info in zip(
                    ('before_path', 'opened_fd', 'after_read_fd', 'after_path'),
                    (before, opened, after_read, after))}, sort_keys=True))
            self.assertEqual(actual, data)
            check_read_snapshots(before, opened, after_read, after, len(actual), windows=True)
            self.assertEqual(digest(path), (len(data), hashlib.sha256(data).hexdigest()))


if __name__ == '__main__':
    unittest.main()
