"""符号独审轻量合同：不运行调试器，不替代真实搬迁断点与回溯。"""
import struct
import tempfile
import unittest
from pathlib import Path
from verify_binaryornot import debuglink_from_readelf, build_id_from_readelf, quote_gdb, safe_child, DEBUG_INPUT, SYMBOL, verify_debugger_transcript


def dump(data):
    rows = ["Hex dump of section '.gnu_debuglink':"]
    for offset in range(0, len(data), 16):
        chunk = data[offset:offset+16]
        words = ' '.join(chunk[n:n+4].hex() for n in range(0, len(chunk), 4))
        rows.append(f'  0x{offset:08x} {words:<35} deadbeef')
    return '\n'.join(rows)


class ContractTests(unittest.TestCase):
    def test_debugger_actual_hits_with_or_without_thread_prefix(self):
        tail = f"\n#0  {SYMBOL} (receiver=...)\nFalse\n[Inferior 1 exited normally]\n"
        for prefix in ['', 'Thread 1 "scanner" hit ']:
            verify_debugger_transcript(f'{prefix}Breakpoint 1, {SYMBOL} (receiver=...)'+tail)
        for bad in [f'Breakpoint 1 at 0xc5b20: {SYMBOL}'+tail,
                    f'Thread 1 "scanner" hit Breakpoint 1, wrong_function'+tail,
                    f'Breakpoint 1, {SYMBOL}_other'+tail,
                    f'Breakpoint 1, {SYMBOL}\n#0 wrong\nFalse\nexited normally',
                    f'Breakpoint 1, {SYMBOL}'+tail.replace('False', 'True'),
                    f'Breakpoint 1, {SYMBOL}'+tail.replace('exited normally', 'SIGSEGV')]:
            with self.assertRaises(ValueError):
                verify_debugger_transcript(bad)

    def test_debugger_input_hits_non_ascii_decode_path(self):
        self.assertEqual(DEBUG_INPUT, "utf16.txt")

    def test_debuglink_crc_and_ascii_column(self):
        for length in range(1, 70):
            name = b'x'*length+b'.dbg'
            data = name+b'\0'
            data += b'\0' * (-len(data) % 4)
            data += struct.pack('<I', 0xdeadbeef)
            self.assertEqual(debuglink_from_readelf(dump(data)), (name, 0xdeadbeef))

    def test_debuglink_rejects_missing_truncated_and_discontinuous(self):
        for text in ['', dump(b'abcd'), dump(b'a\0\0\0'),
                     dump(b'a\0\0\0'+b'\0'*4).replace('0x00000000','0x00000010'),
                     dump(b'a\0\x01\0'+b'\0'*4)]:
            with self.assertRaises(ValueError):
                debuglink_from_readelf(text)

    def test_build_id_unique(self):
        self.assertEqual(build_id_from_readelf('    Build ID: 01abcdef\n'), '01abcdef')
        for text in ['', 'Build ID: abc\n', 'Build ID: 00\nBuild ID: 01\n']:
            with self.assertRaises(ValueError):
                build_id_from_readelf(text)

    def test_gdb_quote_keeps_one_argument(self):
        self.assertEqual(quote_gdb('/tmp/a "b"'), '"/tmp/a \\"b\\""')
        with self.assertRaises(ValueError):
            quote_gdb('/tmp/a\nrun')

    def test_retained_path_rejects_escape_and_symlink(self):
        with tempfile.TemporaryDirectory() as name:
            root = Path(name)
            (root/'a').mkdir()
            (root/'a/file').write_bytes(b'x')
            self.assertEqual(safe_child(root, 'a/file'), root/'a/file')
            for path in ['../a/file', '/a/file', 'a/../a/file', 'a\\file']:
                with self.assertRaises(ValueError):
                    safe_child(root, path)
            (root/'link').symlink_to(root/'a', target_is_directory=True)
            with self.assertRaises(ValueError):
                safe_child(root, 'link/file')


if __name__ == '__main__':
    unittest.main()
