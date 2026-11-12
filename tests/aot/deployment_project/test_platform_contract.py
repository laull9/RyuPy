"""平台消费者格式负例；合成头不是实际Native/跨平台通过证据。"""
import inspect
from pathlib import Path
import struct
import sys
import tempfile
import unittest

HERE=Path(__file__).resolve().parent
sys.path.insert(0,str(HERE.parent));sys.path.insert(0,str(HERE.parent/'standalone_app'))
from manifest_verify import image_header
from platform_support import native_format
from validate_artifact import validate_app_artifact
from platform_project import platform_golden


class PlatformContractTests(unittest.TestCase):
    def test_raw_rendering_and_text_list_have_distinct_newlines(self):
        raw=b'first\nsecond\n'
        for name in ('standard','slant'):
            self.assertEqual(platform_golden(name,raw,'\r\n'),raw)
        self.assertEqual(platform_golden('fonts',raw,'\r\n'),b'first\r\nsecond\r\n')
        self.assertEqual(platform_golden('fonts',raw,'\n'),raw)
        with self.assertRaises(ValueError):platform_golden('unknown',raw,'\r\n')

    def test_rust_override_matches_repository_build_context(self):
        workflow=(HERE.parents[2]/'.github/workflows/deployment-project-platforms.yml').read_text()
        self.assertIn("RUSTUP_TOOLCHAIN: '1.99.1'",workflow)
        self.assertIn("toolchain: '1.99.1'",workflow)
        source=(HERE/'platform_project.py').read_text()
        self.assertIn("run('rustc-info',['rustc','-vV'],cwd=REPO",source)

    def test_linux_default_and_unknown_platform_rejected(self):
        self.assertEqual(inspect.signature(validate_app_artifact).parameters['expected_rid'].default,'linux-x64')
        with self.assertRaises(ValueError):
            validate_app_artifact(None,None,{},case='cli',python=None,dotnet=None,cli=None,runtime=None,
                                  oracle_dir=None,output=None,expected_rid='osx-x64')

    def test_macho_and_pe_are_not_linux_or_managed(self):
        with tempfile.TemporaryDirectory() as folder:
            p=Path(folder)/'app'
            macho=bytearray(64);struct.pack_into('<IIII',macho,0,0xfeedfacf,0x100000c,0,2)
            p.write_bytes(macho);image_header(p,'osx-arm64');native_format(p,'osx-arm64')
            with self.assertRaises(ValueError):image_header(p,'win-x64')
            struct.pack_into('<I',macho,4,0x1000007);p.write_bytes(macho)
            with self.assertRaises(ValueError):image_header(p,'osx-arm64')
            pe=bytearray(328);pe[:2]=b'MZ';struct.pack_into('<I',pe,60,64);pe[64:68]=b'PE\0\0'
            struct.pack_into('<H',pe,68,0x8664);struct.pack_into('<HH',pe,84,240,2)
            struct.pack_into('<H',pe,88,0x20b);struct.pack_into('<I',pe,196,16)
            p.write_bytes(pe);image_header(p,'win-x64');native_format(p,'win-x64')
            struct.pack_into('<II',pe,312,4096,72);p.write_bytes(pe)
            with self.assertRaises(ValueError):image_header(p,'win-x64')
            with self.assertRaises(AssertionError):native_format(p,'win-x64')


if __name__=='__main__':unittest.main()
