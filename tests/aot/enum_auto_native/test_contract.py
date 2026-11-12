"""检查器反例不能替代真实产品及ELF执行。"""
import importlib.util
import json
import shutil
import tempfile
import unittest
from pathlib import Path

HERE = Path(__file__).resolve().parent
spec = importlib.util.spec_from_file_location('enum_auto_gate', HERE/'run.py')
gate = importlib.util.module_from_spec(spec); spec.loader.exec_module(gate)


class ContractTests(unittest.TestCase):
    def test_fixture_originals_and_rejections(self):
        self.assertTrue(gate.base.fixture(HERE/'fixtures').endswith(b"debug-side-effects ['once']\n"))
        for change in ('body', 'shadow', 'missing', 'duplicate', 'python'):
            with self.subTest(change=change), tempfile.TemporaryDirectory() as folder:
                p = Path(folder)/'fixtures'; shutil.copytree(HERE/'fixtures', p)
                data = json.loads((p/'SOURCE.json').read_text())
                if change == 'body': (p/'values.py').write_text('# changed')
                if change == 'shadow': (p/'enum.py').write_text('')
                if change == 'missing': (p/'entry.py').unlink()
                if change == 'duplicate': data['files'].append(data['files'][0])
                if change == 'python': data['python_sha256'] = '0'*64
                (p/'SOURCE.json').write_text(json.dumps(data))
                with self.assertRaises(ValueError): gate.base.fixture(p)

    def test_raw_stream_rejections(self):
        good = dict(exit=0, failure=None, raw_streams_complete=True)
        gate.base.positive(good, b'ok', b'', b'ok')
        for record, stdout, stderr in [(dict(good, exit=1), b'ok', b''),
                (dict(good, failure='timeout'), b'ok', b''),
                (dict(good, raw_streams_complete=False), b'ok', b''),
                (good, b'wrong', b''), (good, b'ok', b'warning')]:
            with self.assertRaises(ValueError): gate.base.positive(record, stdout, stderr, b'ok')

    def test_real_native_and_default_tools_required(self):
        source = (HERE/'run.py').read_text()
        self.assertIn("r.phase('native', base.native)", source)
        self.assertIn("PYTHONOPTIMIZE='0'", source)
        self.assertIn("self.save('python-mode.json'", source)
        self.assertIn("r.phase('prepare-default-tools', base.prepare)", source)
        shared = (HERE.parent/'decimal_finite_native/run.py').read_text()
        self.assertIn("['cargo', 'build', '--locked', '-p', 'ryupy-cli']", shared)
        self.assertNotIn('ordinary-dynamic-module-pilot', shared)
        for needle in ('manifest.image_header', "'native-original'", "'native-relocated'", 'finally:', 'hidden.rename(r.inputs)'):
            self.assertIn(needle, shared)
        workflow = (gate.base.ROOT/'.github/workflows/decimal-finite-native.yml').read_text()
        job = workflow.split('  enum-auto-native:', 1)[1]
        for needle in ('timeout-minutes: 40', 'timeout-minutes: 25', 'enum-auto-native-evidence-', 'tests/aot/enum_auto_native/run.py'):
            self.assertIn(needle, job)
        self.assertEqual(len((gate.base.ROOT/'.github/workflows/wcwidth-legacy-aot.yml').read_text().splitlines()), 600)


if __name__ == '__main__': unittest.main()
