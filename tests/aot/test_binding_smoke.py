"""仅工具轻测，不执行合成native，不构建C/CLI/SDK。"""
import ast
from pathlib import Path
import subprocess
import tempfile
import unittest
from binding_smoke import published, failure_signature, normal_stdout


class BindingSmokeTests(unittest.TestCase):
    def test_unique_cli_paths_and_owned_nested_layout(self):
        with tempfile.TemporaryDirectory() as temporary:
            out = Path(temporary); bundle = out/'ryupy-binding-new'/'bundle'; bundle.mkdir(parents=True)
            dll = bundle/'app.dll'; dll.write_bytes(b'not executed')
            manifest = bundle/'ryupy-artifact.json'; manifest.write_bytes(b'{}')
            raw = f'已生成 {dll}\n产物清单：{manifest}\n'.encode()
            self.assertEqual(published(raw,out,False),dll)
            native = bundle/'ryupy-aot-new'/'publish'/'host'; native.parent.mkdir(parents=True); native.write_bytes(b'not executed')
            manifest = native.parent/'ryupy-artifact.json'; manifest.write_bytes(b'{}')
            raw = f'已生成 {dll}\n已生成 {native}\nAOT 清单：{manifest}\n'.encode()
            self.assertEqual(published(raw,out,True),native)
            with self.assertRaises(AssertionError): published(raw+f'已生成 {native}\n'.encode(),out,True)
            other = out/'other'; other.mkdir()
            with self.assertRaises(AssertionError): published(raw,other,True)

    def test_error_frames_and_exit_are_not_normalized_away(self):
        final = "ImportError: cannot load declared native library 'ryupy_cabi_probe' symbol 'ryupy_demo_add_i32'"
        raw = ('Traceback (most recent call last):\n  File "/private/binding_app.py", line 15, in <module>\n'
               '  File "/private/binding_app.py", line 11, in fail_call\n'+final+'\n').encode()
        self.assertEqual(len(failure_signature(subprocess.CompletedProcess([],1,b'',raw),'load')['frames']),2)
        for bad in (subprocess.CompletedProcess([],0,b'',raw), subprocess.CompletedProcess([],1,b'decoy',raw),
                    subprocess.CompletedProcess([],1,b'',raw.replace(b'line 11',b'line 12')),
                    subprocess.CompletedProcess([],1,b'',raw.replace(b'fail_call',b'fail_call\n  File "extra.py", line 1, in extra'))):
            with self.assertRaises(AssertionError): failure_signature(bad,'load')
        type_raw = raw.replace(b'line 11',b'line 9').replace(final.encode(),
            b"TypeError: add() argument 'left' must be an actual integer, not bool or a converted value")
        failure_signature(subprocess.CompletedProcess([],1,b'',type_raw),'type')
        with self.assertRaises(AssertionError): failure_signature(subprocess.CompletedProcess([],1,b'',type_raw),'load')

    def test_expected_keeps_numeric_raw_bytes_and_fixture_error_lines(self):
        self.assertIn(b'0\r\n3\r\n',normal_stdout(b'0\r\n3\r\n'))
        source = Path(__file__).with_name('binding_fixtures')/'binding_app.py'
        tree = ast.parse(source.read_text(encoding='utf-8'))
        calls = [node for node in ast.walk(tree) if isinstance(node,ast.Call) and isinstance(node.func,ast.Name)]
        self.assertTrue(any(n.func.id=='fail_call' and n.lineno==15 for n in calls))
        for line in (9,11): self.assertTrue(any(n.func.id=='saved' and n.lineno==line for n in calls))


if __name__ == '__main__': unittest.main()
