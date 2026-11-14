"""普通schema1空摘要合同：保留大小/路径门，显式说明等长改写不可检测。"""
import copy
import json
from pathlib import Path
import tempfile
import unittest
from unittest.mock import patch

from manifest_files import ManifestError, check_artifacts, check_file_record
from manifest_verify import MANIFEST_NAME, validate_schema, verify_manifest
from test_manifest_verify import manifest_fixture
from test_resource_manifest import resource_fixture
from test_binding_manifest import sample
from binding_manifest import validate_schema as validate_binding


def omit(data):
    for source in data['sources']:
        source['compiled_text_sha256'] = ''
    for entry in data['build_inputs'] + data['artifacts']:
        entry['sha256'] = ''
    return data


class OmittedDigestTests(unittest.TestCase):
    def test_current_schema1_without_hash_still_verifies_native(self):
        with tempfile.TemporaryDirectory() as temp:
            root = Path(temp) / 'dist'
            data = omit(manifest_fixture(root))
            (root/MANIFEST_NAME).write_text(json.dumps(data))
            validate_schema(data, 'linux-x64', '10.0.403')
            with patch('manifest_files.digest', side_effect=AssertionError('空摘要不应重算hash')):
                verify_manifest(root, root/'program', 'linux-x64', '10.0.403')

    def test_omitted_artifact_requires_explicit_contract(self):
        with tempfile.TemporaryDirectory() as temp:
            root = Path(temp) / 'dist'
            data = omit(manifest_fixture(root))
            with self.assertRaises(ManifestError):
                check_artifacts(root, data['artifacts'], MANIFEST_NAME)
            check_artifacts(root, data['artifacts'], MANIFEST_NAME, allow_omitted=True)
            (root/'extra').write_text('unlisted')
            with self.assertRaises(ManifestError):
                check_artifacts(root, data['artifacts'], MANIFEST_NAME, allow_omitted=True)

    def test_empty_means_size_not_same_length_content_integrity(self):
        with tempfile.TemporaryDirectory() as temp:
            path = Path(temp)/'data'; path.write_bytes(b'abc')
            record = {'size': 3, 'sha256': ''}
            check_file_record(path, record, allow_omitted=True)
            # 这正是生产者取消摘要后的边界，不能宣称已核同一内容。
            path.write_bytes(b'xyz')
            check_file_record(path, record, allow_omitted=True)
            path.write_bytes(b'longer')
            with self.assertRaises(ManifestError):
                check_file_record(path, record, allow_omitted=True)
            path.unlink(); path.symlink_to(Path(temp)/'outside')
            with self.assertRaises(ManifestError):
                check_file_record(path, record, allow_omitted=True)

    def test_nonempty_bad_digest_is_never_ignored(self):
        with tempfile.TemporaryDirectory() as temp:
            root = Path(temp)/'dist'; data = omit(manifest_fixture(root))
            for field in ('source', 'input', 'artifact'):
                for invalid in (None, False, 0, [], ' ', 'a'*63, 'A'*64):
                    changed = copy.deepcopy(data)
                    if field == 'source': changed['sources'][0]['compiled_text_sha256'] = invalid
                    elif field == 'input': changed['build_inputs'][0]['sha256'] = invalid
                    else: changed['artifacts'][0]['sha256'] = invalid
                    with self.subTest(field=field, invalid=invalid), self.assertRaises(ManifestError):
                        validate_schema(changed, 'linux-x64', '10.0.403')
            record = {'size': (root/'program').stat().st_size, 'sha256': '0'*64}
            with self.assertRaises(ManifestError):
                check_file_record(root/'program', record, allow_omitted=True)

    def test_resource_and_binding_contracts_remain_strict(self):
        with tempfile.TemporaryDirectory() as temp:
            root = Path(temp)
            resource = omit(resource_fixture(root/'resource'))
            with self.assertRaises(ManifestError): validate_schema(resource, 'linux-x64', '10.0.403')
            (root/'binding').mkdir()
            binding, _ = sample(root/'binding')
            omit(binding)
            with self.assertRaises(ManifestError): validate_binding(binding, 'linux-x64', 'jit_shared', None)


if __name__ == '__main__': unittest.main()
