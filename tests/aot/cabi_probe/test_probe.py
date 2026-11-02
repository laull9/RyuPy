"""纯 archive 合同负例；不调用编译器、SDK、被测程序或网络。"""
from pathlib import Path
import tempfile
import unittest
from probe import archive_objects, EXPECTED


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


if __name__ == '__main__':
    unittest.main()
