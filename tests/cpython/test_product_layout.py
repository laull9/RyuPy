"""公开internal布局的静态合同门；手写最小目录，不运行CLI或生成普通文件摘要。"""
import json
from pathlib import Path
import tempfile
import unittest

from support import built_product, product_layout, provider_data


class ProductLayoutTests(unittest.TestCase):
    def tree(self, root, name='entry', kind='apphost'):
        data = root / 'internal'
        data.mkdir()
        (root / name).write_bytes(b'native fixture')
        marker = {'schema_version': 1, 'kind': 'ryupy-application-layout',
                  'payload_mode': 'canonical-v1', 'entry': name, 'entry_kind': kind}
        (data / '.ryupy-layout.json').write_text(json.dumps(marker))
        return marker

    def test_jit_and_native_use_the_marker_entry_and_one_data_root(self):
        for name, kind in [('entry', 'apphost'), ('internal.bin', 'apphost'),
                           ('internal.exe', 'native_aot')]:
            with self.subTest(name=name), tempfile.TemporaryDirectory(prefix='ryupy-layout-static-') as tmp:
                root = Path(tmp).resolve()
                self.tree(root, name, kind)
                layout = product_layout(root)
                self.assertEqual(layout.entry, root / name)
                self.assertEqual(layout.data, root / 'internal')
                self.assertEqual(layout.provider_config, root / 'internal/ryupy-cpython.json')
                self.assertEqual(layout.provider_report, root / 'internal/cpython-provider.json')

    def test_flat_wrong_case_extra_root_files_and_bad_marker_are_rejected(self):
        faults = ('flat', 'case', 'extra', 'schema', 'kind', 'traversal',
                  'entry_kind', 'bool_schema', 'extra_field', 'missing_entry')
        for fault in faults:
            with self.subTest(fault=fault), tempfile.TemporaryDirectory(prefix='ryupy-layout-static-') as tmp:
                root = Path(tmp)
                if fault == 'flat':
                    (root / 'entry.dll').write_bytes(b'old')
                else:
                    marker = self.tree(root)
                    if fault == 'case':
                        (root / 'internal').rename(root / 'Internal')
                    elif fault == 'extra':
                        (root / 'RyuPy.Runtime.dll').write_bytes(b'old root dependency')
                    elif fault == 'missing_entry':
                        (root / 'entry').unlink()
                    else:
                        changes = {'schema': ('schema_version', 2),
                                   'bool_schema': ('schema_version', True),
                                   'kind': ('kind', 'other'),
                                   'traversal': ('entry', '../entry'),
                                   'entry_kind': ('entry_kind', 'managed_dll'),
                                   'extra_field': ('other', 1)}
                        key, value = changes[fault]
                        marker[key] = value
                        (root / 'internal/.ryupy-layout.json').write_text(json.dumps(marker))
                with self.assertRaises(AssertionError):
                    product_layout(root)

    def test_marker_read_is_bounded_and_duplicate_fields_are_rejected(self):
        with tempfile.TemporaryDirectory(prefix='ryupy-layout-static-') as tmp:
            root = Path(tmp)
            self.tree(root)
            marker = root / 'internal/.ryupy-layout.json'
            original = marker.read_text()
            for invalid in [original[:-1] + ', "entry": "entry"}', ' ' * 4097, 'null']:
                marker.write_text(invalid)
                with self.assertRaises(AssertionError):
                    product_layout(root)

    def test_generated_record_must_match_marker_and_onefile_is_explicitly_flat(self):
        with tempfile.TemporaryDirectory(prefix='ryupy-layout-static-') as tmp:
            root = Path(tmp).resolve()
            output = root / 'jit'
            output.mkdir()
            self.tree(output)
            stderr = ('已生成 ' + str(output / 'entry') + '\n').encode()
            self.assertEqual(built_product(stderr, output, 'entry', 'jit').entry, output / 'entry')
            with self.assertRaises(AssertionError):
                built_product('已生成 ' + str(output / 'other'), output, 'entry', 'jit')
            dist = root / 'entry.dist'
            dist.mkdir()
            (dist / 'cpython-provider.json').write_text('{}')
            self.assertEqual(provider_data(root, 'entry', 'onefile'), dist)
            with self.assertRaises(AssertionError):
                provider_data(root, 'entry', 'standalone')


if __name__ == '__main__':
    unittest.main()
