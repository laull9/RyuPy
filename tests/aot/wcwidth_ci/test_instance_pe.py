"""PE envelope 离线负例：哈希、版本和陈旧输入不能进入实际 PE 验收。"""
import copy
import json
from pathlib import Path
import tempfile
import unittest
from instance_pe import digest, envelope, unchanged


class EnvelopeTests(unittest.TestCase):
    def test_identity_and_rejection(self):
        locator = {'owner': 'RyuPy.Generated.Module', 'name': 'x', 'signature': 'sig', 'slot': 0}
        root = {'source': locator, 'target': locator, 'instruction': 0, 'role': 'callable_thunk'}
        good = {'schema_version': 1, 'report_version': 2, 'case': 'original', 'commit': 'current',
                'pe': {'name': 'original.dll', 'size': 2, 'sha256': digest(b'PE')},
                'entry': locator, 'selected': ['subject'], 'owner_count': 1,
                'sources': [{'module_name': 'subject'}],
                'owners': [{'function_id': 7, 'core': locator, 'root': root, 'calls': 2}],
                'modules': [{'module_id': 1, 'module_name': 'subject', 'root': root}]}
        with tempfile.TemporaryDirectory() as tmp:
            directory = Path(tmp)
            pe = directory / 'original.dll'
            pe.write_bytes(b'PE')
            expected = directory / 'original-expected.json'
            expected.write_text(json.dumps(good))
            identity = envelope(directory, 'original', 'current')
            unchanged(directory, identity)
            self.assertTrue((directory / 'original.pe-envelope').read_bytes().startswith(b'RYUPE002'))
            with self.assertRaises(FileExistsError):
                envelope(directory, 'original', 'current')
            pe.write_bytes(b'XX')
            with self.assertRaises(ValueError):
                unchanged(directory, identity)
            with self.assertRaises(ValueError):
                envelope(directory, 'original', 'current')
            pe.write_bytes(b'PE')
            for key, bad in [('report_version', 1), ('schema_version', 2), ('commit', 'old'),
                             ('case', 'combined'), ('selected', ['missing']), ('owners', []), ('owner_count', 2)]:
                value = copy.deepcopy(good)
                value[key] = bad
                expected.write_text(json.dumps(value))
                with self.subTest(key=key), self.assertRaises(ValueError):
                    envelope(directory, 'original', 'current')
            value = copy.deepcopy(good)
            value['owners'] *= 2
            expected.write_text(json.dumps(value))
            with self.assertRaises(ValueError):
                envelope(directory, 'original', 'current')


if __name__ == '__main__':
    unittest.main()
