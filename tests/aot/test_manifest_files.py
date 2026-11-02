"""只造普通字节文件验清单，不把伪文件声称为NativeAOT。"""
import hashlib
import os
from pathlib import Path
import tempfile
import unittest
from manifest_files import ManifestError, check_artifacts, relative_path


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


if __name__ == '__main__':
    unittest.main()
