"""新Native门的检查器反例；检查器通过不是Native产品通过。"""
import copy
import hashlib
import json
from pathlib import Path
import shutil
import sys
import tempfile
import unittest

HERE = Path(__file__).resolve().parent
sys.path.insert(0, str(HERE.parent))
from runtime_single_base import fixture_identity, prepare_calls, native_output


def sha(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


class RuntimeSingleBaseGateTests(unittest.TestCase):
    def test_fixture_identity_and_mutation(self):
        source = HERE/'runtime_single_base_fixtures/ordinary'
        digest = json.loads((source/'SOURCE.json').read_text())['python_sha256']
        self.assertEqual(fixture_identity(source, digest, sha), b'10 7\n[3, 1, 2] 3\n4 2 2\nabc 2 b\n')
        with tempfile.TemporaryDirectory() as tmp:
            root = Path(tmp)/'inputs'; shutil.copytree(source, root)
            (root/'entry.py').write_text('print("fake")\n')
            with self.assertRaises(ValueError): fixture_identity(root, digest, sha)
        with self.assertRaises(ValueError): fixture_identity(source, '0'*64, sha)

    def test_real_calls_not_symbol_presence(self):
        call = {'owner': 'RyuPy.Runtime.PyOps', 'name': 'PrepareRuntimeDerivedClass',
                'signature_hex': '00021c0e1d1c'}
        data = {'sha256': 'a'*64, 'methods': [{'body': {'instructions': [{'target': call} for _ in range(5)]}}]}
        self.assertEqual(prepare_calls(data, 'a'*64)['actual_calls'], 5)
        mutations = []
        for field, value in [('owner', 'Wrong.PyOps'), ('signature_hex', '000001')]:
            changed = copy.deepcopy(data); changed['methods'][0]['body']['instructions'][0]['target'][field] = value
            mutations.append(changed)
        changed = copy.deepcopy(data); changed['methods'][0]['body']['instructions'].pop(); mutations.append(changed)
        changed = copy.deepcopy(data); changed['methods'][0]['body']['instructions'].append({'target': {'name': 'PrepareOrdinaryMiClass'}}); mutations.append(changed)
        changed = copy.deepcopy(data); changed['sha256'] = 'b'*64; mutations.append(changed)
        for changed in mutations:
            with self.subTest(changed=changed), self.assertRaises(ValueError): prepare_calls(changed, 'a'*64)

    def test_native_rejection_is_not_success_or_crash(self):
        good = {'exit': 1, 'failure': None, 'raw_streams_complete': True}
        message = b'NotImplementedError: runtime MI inheritance requires dynamic code support\n'
        native_output(good, b'', message, b'dynamic-mi-created\n', True)
        for record, out, err in [(dict(good, exit=0), b'', message),
                                 (good, b'dynamic-mi-created\n', message),
                                 (good, b'', b'fatal error\n'+message),
                                 (good, b'', b'NotImplementedError: wrong reason\n'),
                                 (dict(good, raw_streams_complete=False), b'', message)]:
            with self.assertRaises(ValueError): native_output(record, out, err, b'', True)

    def test_native_is_mandatory_and_old_rejection_retained(self):
        raw = (HERE/'run.py').read_text()
        self.assertIn("('new-mi-aot-negative',aot_boundary)", raw)
        self.assertIn("('runtime-single-base-native',__import__('runtime_single_base').run)", raw)
        self.assertIn("r.result.get('runtime_single_base_native', {}).get('passed')", raw)
        self.assertIn("'pre_emission_rejection_verified':True", raw)
        step = (HERE.parents[2]/'.github/retired-workflows/ordinary-mi.yml').read_text()
        self.assertIn('timeout-minutes: 40', step)
        self.assertIn('!target/ordinary-mi-evidence/runtime-single-*-native-build/**', step)
        # 拒绝把真实dist归档也排除，失败与成功均可收原件。
        self.assertNotIn('!target/ordinary-mi-evidence/runtime-single-*-native-dist', step)


if __name__ == '__main__': unittest.main()
