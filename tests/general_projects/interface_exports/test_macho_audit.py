"""Mach-O审计器的真实跨后缀合同：.so dylib、bundle、普通dylib及真正未闭合加载边。"""
from pathlib import Path
import subprocess
import sys
import tempfile
import unittest

ROOT = Path(__file__).resolve().parents[3]
sys.path.insert(0, str(ROOT/'tests/cpython'))
from support import audit_native_paths


@unittest.skipUnless(sys.platform == 'darwin', '此门实际验证macOS Mach-O')
class MachOAuditTests(unittest.TestCase):
    def build(self, root, name, kind, source, dependencies=()):
        text = root/(name+'.c')
        text.write_text(source)
        output = root/name
        subprocess.run(['cc', kind, text, *dependencies, '-o', output], check=True, capture_output=True)
        return output

    def test_real_identity_is_independent_of_suffix(self):
        with tempfile.TemporaryDirectory(prefix='ryupy-macho-audit-', dir='/tmp') as temporary:
            root = Path(temporary)
            for name, kind in [('extension.so', '-dynamiclib'), ('ordinary.dylib', '-dynamiclib'), ('module_bundle.so', '-bundle')]:
                image = self.build(root, name, kind, 'int actual_export(void) { return 7; }\n')
                if kind == '-dynamiclib':
                    subprocess.run(['install_name_tool', '-id', '@rpath/'+name, image], check=True, capture_output=True)
            audit_native_paths(root)

    def test_real_loading_edges_remain_strict_for_dylib_and_bundle(self):
        for kind in ['-dynamiclib', '-bundle']:
            with self.subTest(kind=kind), tempfile.TemporaryDirectory(prefix='ryupy-macho-edge-', dir='/tmp') as temporary:
                root = Path(temporary)
                dependency = self.build(root, 'dependency.dylib', '-dynamiclib', 'int dependency(void) { return 9; }\n')
                subprocess.run(['install_name_tool', '-id', '@rpath/dependency.dylib', dependency], check=True, capture_output=True)
                self.build(root, 'consumer.so', kind, 'extern int dependency(void); int consume(void) { return dependency(); }\n', [dependency])
                with self.assertRaisesRegex(AssertionError, '尚未解析的rpath加载边'):
                    audit_native_paths(root)


if __name__ == '__main__':
    unittest.main()
