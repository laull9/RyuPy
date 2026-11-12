"""纯检查器反例不计作产品或Native通过。"""
import importlib.util
import json
import shutil
import tempfile
import unittest
from pathlib import Path

HERE = Path(__file__).resolve().parent
spec = importlib.util.spec_from_file_location('tzinfo_native_gate', HERE/'run.py')
gate = importlib.util.module_from_spec(spec); spec.loader.exec_module(gate)


class ContractTests(unittest.TestCase):
    def test_deadline(self):
        self.assertEqual(gate.remaining(1500, 100), 1370)
        for now in (1470, 1490, 1501):
            with self.assertRaises(ValueError): gate.remaining(1500, now)

    def test_fixture_rejects_changes(self):
        self.assertTrue(gate.fixture(HERE/'fixtures').endswith(b'tzinfo-native-complete\n'))
        for change in ('body', 'extra', 'missing', 'duplicate', 'python'):
            with self.subTest(change=change), tempfile.TemporaryDirectory() as folder:
                dest = Path(folder)/'fixtures'; shutil.copytree(HERE/'fixtures', dest)
                manifest = dest/'SOURCE.json'; data = json.loads(manifest.read_text())
                if change == 'body': (dest/'zones.py').write_text('# changed\n')
                if change == 'extra': (dest/'datetime.py').write_text('')
                if change == 'missing': (dest/'entry.py').unlink()
                if change == 'duplicate': data['files'].append(data['files'][0])
                if change == 'python': data['python_sha256'] = '0'*64
                manifest.write_text(json.dumps(data))
                with self.assertRaises(ValueError): gate.fixture(dest)

    def test_reuses_native_contract_and_separate_counts(self):
        source = (HERE/'run.py').read_text()
        self.assertIn("r.phase('native', base.native)", source)
        self.assertIn("result.get('native_executions') == 2", source)
        self.assertIn("min(timeout, remaining(self.deadline, time.time()))", source)
        self.assertNotIn("'cargo'", source)
        self.assertNotIn("'runtime-build'", source)
        workflow = (gate.ROOT/'.github/workflows/decimal-finite-native.yml').read_text()
        self.assertIn('timeout-minutes: 25', workflow)
        self.assertIn('timeout-minutes: 40', workflow)
        self.assertLess(workflow.index(' -B tests/aot/decimal_finite_native/run.py'),
                        workflow.index(' -B tests/aot/tzinfo_native/run.py'))
        self.assertIn('name: tzinfo-native-evidence-', workflow)
        self.assertEqual(len((gate.ROOT/'.github/workflows/wcwidth-legacy-aot.yml').read_text().splitlines()), 600)


if __name__ == '__main__': unittest.main()
