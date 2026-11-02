"""编排纯函数轻测；不启动编译器/SDK/应用。"""
import subprocess
import unittest
from unittest.mock import patch
from resource_smoke import expected_stdout, missing_signature, equal


class ResourceSmokeTests(unittest.TestCase):
    def test_platform_expected_newlines_do_not_normalize_actual_streams(self):
        for newline in ('\n', '\r\n'):
            with patch('resource_smoke.os.linesep', newline):
                expected = ('hello'+newline).encode()
                self.assertEqual(expected_stdout('hello\n'), expected)
                reference = subprocess.CompletedProcess([],0,expected,b'')
                equal(reference, reference)
                changed = subprocess.CompletedProcess([],0,b'hello\r\n' if newline == '\n' else b'hello\n',b'')
                with self.assertRaises(AssertionError):
                    equal(reference,changed)

    def test_missing_data_keeps_every_frame_and_platform_stdout(self):
        stderr = (b'Traceback (most recent call last):\n'
            b'  File "C:\\source\\main.py", line 11, in <module>\n'
            b'  File "C:\\source\\greetings\\english.py", line 14, in load_text\n'
            b'FileNotFoundError: no data\n')
        for newline in ('\n','\r\n'):
            with patch('resource_smoke.os.linesep',newline):
                result = subprocess.CompletedProcess([],1,('hello'+newline).encode(),stderr)
                self.assertEqual(missing_signature(result),
                    ([('main.py',11,'<module>'),('english.py',14,'load_text')],'FileNotFoundError'))
        with self.assertRaises(AssertionError):
            missing_signature(subprocess.CompletedProcess([],0,b'hello\n',stderr))


class FixtureCheckoutTests(unittest.TestCase):
    def test_hash_failure_names_relative_expected_and_actual_without_normalization(self):
        import hashlib
        from pathlib import Path
        import tempfile
        from resource_smoke import check_fixture_sha
        with tempfile.TemporaryDirectory() as temporary:
            path = Path(temporary)/'data.txt'
            original = b'line one\nline two\n'
            expected = hashlib.sha256(original).hexdigest()
            path.write_bytes(original)
            check_fixture_sha(path, 'greetings/data.txt', expected)
            path.write_bytes(original.replace(b'\n', b'\r\n'))
            actual = hashlib.sha256(path.read_bytes()).hexdigest()
            with self.assertRaises(AssertionError) as caught:
                check_fixture_sha(path, 'greetings/data.txt', expected)
            message = str(caught.exception)
            for value in ('greetings/data.txt', expected, actual):
                self.assertIn(value, message)
            self.assertNotIn(temporary, message)

    def test_git_windows_autocrlf_filter_keeps_all_resource_inputs_lf(self):
        from pathlib import Path
        import os
        import shutil
        import tempfile
        git = shutil.which('git')
        self.assertIsNotNone(git, '此永久轻测需要Git执行真实checkout filter')
        attributes = (Path(__file__).resolve().parents[2]/'.gitattributes').read_bytes()
        with tempfile.TemporaryDirectory() as temporary:
            root = Path(temporary)
            env = {**os.environ, 'GIT_CONFIG_NOSYSTEM':'1', 'GIT_CONFIG_GLOBAL':os.devnull}
            def command(*args, input=None):
                return subprocess.run([git, '-c', 'core.autocrlf=true', '-c', 'core.eol=crlf',
                    *args], cwd=root, env=env, input=input, capture_output=True, check=True, timeout=15).stdout
            command('init', '--quiet')
            raw = b'fixed input\nsecond line\n'
            blob = command('hash-object', '-w', '--stdin', input=raw).decode().strip()
            # 无规则确实发生CRLF转换，确保不是仅在Linux跑了一个无效模拟。
            control = command('cat-file','--filters','--path=uncontrolled.py',blob)
            self.assertEqual(control, raw.replace(b'\n',b'\r\n'))
            (root/'.gitattributes').write_bytes(attributes)
            for relative in ('main.py', 'greetings/__init__.py', 'greetings/english.py',
                             'greetings/french.py', 'greetings/data.txt', 'resources.json', 'expected.json'):
                with self.subTest(relative=relative):
                    output = command('cat-file','--filters',
                        '--path=tests/aot/resource_fixtures/'+relative,blob)
                    self.assertEqual(output, raw)


if __name__ == '__main__':
    unittest.main()
