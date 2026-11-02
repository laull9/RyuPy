"""纯 archive 合同负例；不调用编译器、SDK、被测程序或网络。"""
from pathlib import Path
import tempfile
import unittest
from probe import archive_objects, EXPECTED, librarian_version, xcode_version


def member(name, payload):
    header = (name.ljust(16) + '0'.ljust(12) + '0'.ljust(6) + '0'.ljust(6)
              + '100644'.ljust(8) + str(len(payload)).ljust(10) + '`\n').encode('ascii')
    return header + payload + (b'\n' if len(payload) & 1 else b'')


class ArchiveTests(unittest.TestCase):
    def check(self, data, rid):
        with tempfile.TemporaryDirectory() as root:
            p = Path(root)/'probe.a'; p.write_bytes(data)
            return archive_objects(p, rid)

    def test_real_coff_shape_accepted_but_import_header_rejected(self):
        self.assertEqual(len(self.check(b'!<arch>\n'+member('p.obj/', b'\x64\x86'+bytes(20)), 'win-x64')), 1)
        with self.assertRaises(AssertionError):
            self.check(b'!<arch>\n'+member('p.obj/', b'\0\0\xff\xff'+bytes(20)), 'win-x64')

    def test_unix_relocatable_arch_and_kind_required(self):
        elf = bytearray(64); elf[:6] = b'\x7fELF\x02\x01'; elf[16:20] = b'\x01\0\x3e\0'
        self.check(b'!<arch>\n'+member('p.o/', elf), 'linux-x64')
        elf[16] = 3
        with self.assertRaises(AssertionError):
            self.check(b'!<arch>\n'+member('p.o/', elf), 'linux-x64')

    def test_macos_bsd_extended_member_name(self):
        obj = bytes.fromhex('cffaedfe0c0000010000000001000000') + bytes(16)
        self.assertEqual(self.check(b'!<arch>\n'+member('#1/4', b'p.o\0'+obj), 'osx-arm64')[0]['member'], 'p.o')

    def test_negative_symbol_member_length_cannot_repeat_parser_position(self):
        data = bytearray(b'!<arch>\n'+member('/', b''))
        data[56:66] = b'-60'.ljust(10)
        with self.assertRaises(AssertionError):
            self.check(data, 'win-x64')

    def test_no_empty_or_multiple_objects_and_fixed_expected(self):
        for data in [b'!<arch>\n', b'!<arch>\n'+member('a/', b'\x64\x86')+member('b/', b'\x64\x86')]:
            with self.assertRaises(AssertionError):
                self.check(data, 'win-x64')
        self.assertEqual(EXPECTED.splitlines(), [b'0', b'3', b'4294967294', b'-4294967296', b'-1'])


class VersionEvidenceTests(unittest.TestCase):
    def test_windows_observed_help_exit_requires_real_banner(self):
        banner = b'Microsoft (R) Library Manager Version 14.44.35222.0\r\n'
        for code in (0, 1100):
            self.assertEqual(librarian_version(code, banner, b''), '14.44.35222.0')
        for code, output in [(1, banner), (1100, b'usage only'), (0, b'Library Manager Version unknown')]:
            with self.assertRaises(AssertionError):
                librarian_version(code, output, b'')

    def test_xcode_evidence_not_guessed_ar_version(self):
        self.assertEqual(xcode_version(b'Xcode 16.4\nBuild version 16F6\n'), 'Xcode 16.4\nBuild version 16F6')
        for output in (b'usage: ar -r', b'Xcode 16.4', b'Xcode unknown\nBuild version 16F6'):
            with self.assertRaises(AssertionError):
                xcode_version(output)


if __name__ == '__main__':
    unittest.main()
