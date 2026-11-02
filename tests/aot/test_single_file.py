"""仅文件复制/归属校验，不执行伪native，也不启动SDK。"""
import hashlib
import os
from pathlib import Path
import tempfile
import unittest
from unittest.mock import patch
from single_file import prepare_single_file, verify_single_file


class SingleFileTests(unittest.TestCase):
    def setup_files(self, root):
        work = root/'work'; work.mkdir()
        full = work/'relocated'; full.mkdir()
        binary = full/'program.exe'; binary.write_bytes(b'ordinary test bytes')
        if os.name == 'posix':
            binary.chmod(0o755)
        (full/'ryupy-artifact.json').write_text('{}')
        (full/'program.pdb').write_bytes(b'symbols')
        return work, binary, hashlib.sha256(binary.read_bytes()).hexdigest()

    def test_copy_is_exact_single_file_same_hash_and_preserves_full_bundle(self):
        with tempfile.TemporaryDirectory() as temporary:
            work,binary,sha = self.setup_files(Path(temporary))
            copied,record = prepare_single_file(binary,work,[work/'source',work/'il'],sha)
            self.assertEqual(list(copied.parent.iterdir()),[copied])
            self.assertEqual(copied.read_bytes(),binary.read_bytes())
            if os.name == 'posix':
                self.assertEqual(copied.stat().st_mode & 0o777, 0o755)
            self.assertEqual(record['sha256'],sha)
            self.assertTrue((binary.parent/'ryupy-artifact.json').is_file())
            self.assertTrue((binary.parent/'program.pdb').is_file())
            (copied.parent/'empty-directory').mkdir()
            with self.assertRaises(AssertionError):
                verify_single_file(copied,sha)

    def test_live_inputs_foreign_roots_and_wrong_hash_fail_before_copy(self):
        with tempfile.TemporaryDirectory() as temporary:
            root=Path(temporary); work,binary,sha = self.setup_files(root)
            (work/'source').mkdir()
            for removed,expected in (([work/'source',work/'il'],sha),([root/'foreign'],sha),
                                     ([work/'absent'],'0'*64),([],sha)):
                with self.subTest(removed=removed), self.assertRaises(AssertionError):
                    prepare_single_file(binary,work,removed,expected)
                self.assertFalse((work/'single-executable').exists())

    def test_existing_destination_is_not_reused_or_deleted(self):
        with tempfile.TemporaryDirectory() as temporary:
            work,binary,sha = self.setup_files(Path(temporary))
            existing=work/'single-executable';existing.mkdir()
            sentinel=existing/'sentinel';sentinel.write_bytes(b'keep')
            with self.assertRaises(FileExistsError):
                prepare_single_file(binary,work,[work/'source',work/'il'],sha)
            self.assertEqual(sentinel.read_bytes(),b'keep')

    def test_copy_tamper_and_extra_file_are_rejected_without_scanning_other_dirs(self):
        with tempfile.TemporaryDirectory() as temporary:
            work,binary,sha = self.setup_files(Path(temporary))
            def altered(source, target):
                target.write_bytes(b'wrong')
            with patch('single_file.shutil.copy2',side_effect=altered), self.assertRaises(AssertionError):
                prepare_single_file(binary,work,[work/'source',work/'il'],sha)
            self.assertTrue((binary.parent/'ryupy-artifact.json').is_file())
        with tempfile.TemporaryDirectory() as temporary:
            work,binary,sha = self.setup_files(Path(temporary))
            def extra(source,target):
                target.write_bytes(source.read_bytes())
                (target.parent/'extra').write_bytes(b'x')
            with patch('single_file.shutil.copy2',side_effect=extra), self.assertRaises(AssertionError):
                prepare_single_file(binary,work,[work/'source',work/'il'],sha)

    @unittest.skipUnless(os.name == 'posix','真实symlink轻测仅POSIX；Windows实际reparse由plain_stat共享拒')
    def test_symlink_executable_and_dangling_removed_input_rejected(self):
        with tempfile.TemporaryDirectory() as temporary:
            work,binary,sha = self.setup_files(Path(temporary))
            link=work/'link';link.symlink_to(binary)
            with self.assertRaises(ValueError):
                prepare_single_file(link,work,[work/'source',work/'il'],sha)
            (work/'source').symlink_to(work/'missing')
            with self.assertRaises(AssertionError):
                prepare_single_file(binary,work,[work/'source',work/'il'],sha)


if __name__ == '__main__':
    unittest.main()
