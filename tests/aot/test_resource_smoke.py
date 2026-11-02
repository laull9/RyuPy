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


if __name__ == '__main__':
    unittest.main()
