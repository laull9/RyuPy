"""通用共享归属红绿合同：两组无关联发行、不同源冲突、内部重复及普通文件边界。"""
import csv
import importlib.util
import io
import os
from pathlib import Path
import tempfile
import unittest
from types import SimpleNamespace

ROOT = Path(__file__).resolve().parents[3]
SPEC = importlib.util.spec_from_file_location(
    'dependency_collector', ROOT / 'crates/ryupy-driver/src/cpython_dependencies.py')
COLLECTOR = importlib.util.module_from_spec(SPEC)
SPEC.loader.exec_module(COLLECTOR)


class SharedOwnershipContracts(unittest.TestCase):
    def test_two_independent_distribution_groups_share_one_payload_each(self):
        with tempfile.TemporaryDirectory(prefix='ryupy-shared-source-') as temporary:
            root = Path(temporary).resolve()
            for owners, target in [(('copper_gate', 'harbor_widget'), 'granite_mesh/source.py'),
                                   (('quartz_relay', 'marsh_engine'), 'cobalt_basin/assets/payload.dat')]:
                with self.subTest(owners=owners):
                    source = root / target
                    source.parent.mkdir(parents=True, exist_ok=True)
                    source.write_text('同一已安装文件，允许不同发行共同登记。')
                    shared = COLLECTOR.SharedFiles()
                    for owner in owners:
                        shared.add(target, COLLECTOR.validated_source(root, target, owner), owner)
                    self.assertEqual(shared.payloads, {target: str(source)})
                    self.assertEqual(shared.shared_files(), [
                        {'destination': target, 'source': str(source), 'owners': sorted(owners)}])

    def test_different_real_sources_cannot_claim_one_destination(self):
        with tempfile.TemporaryDirectory() as temporary:
            root = Path(temporary)
            first, second = root / 'first', root / 'second'
            first.write_text('同文也不改变源身份')
            second.write_text('同文也不改变源身份')
            shared = COLLECTOR.SharedFiles()
            shared.add('joint/value.bin', first, 'sunrise_member')
            with self.assertRaisesRegex(ValueError, '不同源'):
                shared.add('joint/value.bin', second, 'evening_member')

    def test_one_distribution_cannot_claim_one_file_twice(self):
        with tempfile.TemporaryDirectory() as temporary:
            source = Path(temporary) / 'source'
            source.write_text('fixture')
            shared = COLLECTOR.SharedFiles()
            shared.add('joint/value.bin', source, 'single_owner')
            with self.assertRaisesRegex(ValueError, '重复'):
                shared.add('joint/value.bin', source, 'single_owner')

    def test_traversal_and_absolute_targets_fail(self):
        with tempfile.TemporaryDirectory() as temporary:
            root = Path(temporary)
            for target in ('../escape', '/absolute', 'pkg/../x', 'pkg\\x', ''):
                with self.subTest(target=target), self.assertRaisesRegex(ValueError, 'RECORD路径'):
                    COLLECTOR.validated_source(root, target, 'path_owner')

    @unittest.skipIf(os.name == 'nt', '本机普通用户符号链接权限另由平台验收')
    def test_safe_internal_file_and_directory_symlinks_share_canonical_source(self):
        with tempfile.TemporaryDirectory() as temporary:
            root = Path(temporary)
            (root / 'actual').mkdir()
            (root / 'actual/file').write_text('fixture')
            (root / 'alias').symlink_to(root / 'actual/file')
            (root / 'directory').symlink_to(root / 'actual', target_is_directory=True)
            for target in ('alias', 'directory/file'):
                with self.subTest(target=target):
                    source = COLLECTOR.validated_source(root, target, 'link_owner')
                    self.assertEqual(source, (root / 'actual/file').resolve())
                    shared = COLLECTOR.SharedFiles()
                    shared.add(target, source, 'frost_member')
                    shared.add(target, source, 'mist_member')
                    self.assertEqual(len(shared.payloads), 1)

    @unittest.skipIf(os.name == 'nt', '本机普通用户符号链接权限另由平台验收')
    def test_escaping_and_cyclic_symlinks_are_rejected(self):
        with tempfile.TemporaryDirectory() as temporary:
            root = Path(temporary)
            site = root / 'site'
            site.mkdir()
            outside = root / 'outside'
            outside.write_text('site外文件')
            (site / 'escape').symlink_to(outside)
            (site / 'cycle').symlink_to(site / 'cycle')
            for target in ('escape', 'cycle'):
                with self.subTest(target=target), self.assertRaisesRegex(ValueError, 'RECORD'):
                    COLLECTOR.validated_source(site, target, 'bad_link_owner')

    def test_missing_files_and_directories_are_rejected(self):
        with tempfile.TemporaryDirectory() as temporary:
            root = Path(temporary)
            (root / 'directory').mkdir()
            for target in ('missing', 'directory'):
                with self.subTest(target=target), self.assertRaises(ValueError):
                    COLLECTOR.validated_source(root, target, 'missing_owner')

    def test_internal_duplicate_record_rows_are_rejected(self):
        stream = io.StringIO()
        writer = csv.writer(stream)
        writer.writerows([['namespace/data.dat', '', ''], ['namespace/data.dat', '', '']])
        dist = SimpleNamespace(metadata={'Name': 'independent_duplicate'},
                               read_text=lambda name: stream.getvalue())
        with self.assertRaisesRegex(ValueError, 'RECORD.*重复'):
            COLLECTOR.record_files(dist)

    def test_equivalent_record_spellings_are_internal_duplicates(self):
        dist = SimpleNamespace(metadata={'Name': 'independent_spelling'},
                               read_text=lambda name: 'namespace\\data.dat,,\nnamespace/./data.dat,,\n')
        with self.assertRaisesRegex(ValueError, 'RECORD.*重复'):
            COLLECTOR.record_files(dist)

    def test_unselected_inventory_scan_does_not_validate_duplicate_ownership(self):
        dist = SimpleNamespace(metadata={'Name': 'unrelated_drifter'},
                               read_text=lambda name: 'unrelated_item.py,,\nunrelated_item.py,,\n')
        self.assertEqual(COLLECTOR.record_files(dist, strict=False), ['unrelated_item.py'])


if __name__ == '__main__':
    unittest.main()
