"""仅工具纯测，不运行SDK、CP fixture或伪native。"""
from pathlib import Path
import subprocess
import tempfile
import unittest
from default_layout_smoke import publish_command, same_output, retain_source

class DefaultLayoutTests(unittest.TestCase):
    def test_exact_public_command_has_no_output_override(self):
        source=Path('/owned/source/m3_6_abc.py')
        self.assertEqual(publish_command('/built/ryupy',source),['/built/ryupy','build','--aot',str(source)])
    def test_raw_stream_and_exit_differences_are_not_normalized(self):
        cp=subprocess.CompletedProcess([],0,b'True\n',b'')
        same_output(cp,cp)
        for actual in [subprocess.CompletedProcess([],1,b'True\n',b''),
                       subprocess.CompletedProcess([],0,b'True\r\n',b''),
                       subprocess.CompletedProcess([],0,b'True\n',b'warning')]:
            with self.assertRaises(AssertionError): same_output(cp,actual)
    def test_only_owned_source_is_renamed_and_evidence_never_overwritten(self):
        with tempfile.TemporaryDirectory() as tmp:
            work=Path(tmp); source=work/'source'; source.mkdir(); (source/'m3_6_abc.build').mkdir()
            (source/'m3_6_abc.py').write_bytes(b'unchanged')
            target=retain_source(source,work)
            self.assertFalse(source.exists()); self.assertEqual((target/'m3_6_abc.py').read_bytes(),b'unchanged')
            source.mkdir()
            with self.assertRaises(AssertionError): retain_source(source,work)
            self.assertTrue(source.is_dir())

if __name__=='__main__': unittest.main()
