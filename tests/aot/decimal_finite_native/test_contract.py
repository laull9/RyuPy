"""有限Decimal Native检查器反例；只验证检查器，真实产品另行hosted执行。"""
import copy
import hashlib
import json
import os
from pathlib import Path
import shutil
import tempfile
import unittest
from contract import context, fixture, positive, source_index

HERE = Path(__file__).resolve().parent
ROOT = HERE.parents[2]


class DecimalNativeContractTests(unittest.TestCase):
    def test_exact_context(self):
        env = {'GITHUB_ACTIONS': 'true', 'RUNNER_ENVIRONMENT': 'github-hosted', 'GITHUB_EVENT_NAME': 'push',
               'GITHUB_RUN_ATTEMPT': '1', 'GITHUB_REF': 'refs/heads/ci/wcwidth-legacy-aot-2909056', 'GITHUB_SHA': 'a'*40}
        event = {'ref': env['GITHUB_REF'], 'after': env['GITHUB_SHA'],
                 'head_commit': {'id': env['GITHUB_SHA'], 'message': 'validation: ordinary-mi-c3-001'}}
        context(env, event)
        for field, value in [('GITHUB_EVENT_NAME', 'workflow_dispatch'), ('GITHUB_RUN_ATTEMPT', '2'),
                             ('RUNNER_ENVIRONMENT', 'self-hosted'), ('GITHUB_REF', 'refs/heads/main')]:
            with self.subTest(field=field), self.assertRaises(ValueError): context(dict(env, **{field:value}), event)
        for field, value in [('id', 'b'*40), ('message', 'validation: decimal-local')]:
            bad = copy.deepcopy(event); bad['head_commit'][field] = value
            with self.assertRaises(ValueError): context(env, bad)

    def test_raw_exit_and_both_streams(self):
        record = {'exit':0, 'failure':None, 'raw_streams_complete':True}
        positive(record, b'golden\n', b'', b'golden\n')
        for rec, out, err in [(dict(record, exit=1), b'golden\n', b''),
                              (dict(record, failure='timeout'), b'golden\n', b''),
                              (dict(record, raw_streams_complete=False), b'golden\n', b''),
                              (record, b'other\n', b''), (record, b'golden\n', b'warning\n')]:
            with self.assertRaises(ValueError): positive(rec, out, err, b'golden\n')

    def test_fixture_exact_files_and_bytes(self):
        golden = fixture(HERE/'fixtures'); self.assertGreater(len(golden), 500)
        for kind in ('bytes', 'missing', 'duplicate', 'extra', 'physical-extra'):
            with tempfile.TemporaryDirectory() as tmp:
                root = Path(tmp)/'input'; shutil.copytree(HERE/'fixtures', root)
                if kind == 'bytes': (root/'entry.py').write_text('print("fake")\n')
                elif kind == 'physical-extra': (root/'decimal.py').write_text('fake = 1\n')
                else:
                    data = json.loads((root/'SOURCE.json').read_text())
                    if kind == 'missing': data['files'].pop()
                    if kind == 'duplicate': data['files'][1] = data['files'][0]
                    if kind == 'extra': data['files'].append({'path':'../escape'})
                    (root/'SOURCE.json').write_text(json.dumps(data))
                with self.subTest(kind=kind), self.assertRaises(ValueError): fixture(root)

    def test_source_index_is_not_only_names(self):
        with tempfile.TemporaryDirectory() as tmp:
            root = Path(tmp); path = root/'a.py'; path.write_bytes(b'pass\n'); path.chmod(0o644)
            blob = hashlib.sha1(b'blob 5\0pass\n').hexdigest()
            raw = ('100644 '+blob+' 0\ta.py\0').encode()
            self.assertEqual(source_index(root, raw)[0]['path'], 'a.py')
            path.write_bytes(b'fail\n')
            with self.assertRaises(ValueError): source_index(root, raw)
            path.write_bytes(b'pass\n'); path.chmod(0o755)
            with self.assertRaises(ValueError): source_index(root, raw)
        with self.assertRaises(ValueError): source_index(Path('.'), b'')

    def test_independent_route_and_artifact(self):
        controller = (ROOT/'.github/workflows/wcwidth-legacy-aot.yml').read_text()
        workflow = (ROOT/'.github/workflows/decimal-finite-native.yml').read_text()
        self.assertLessEqual(len(controller.splitlines()), 600)
        self.assertIn("  decimal-finite-native:\n    needs: select-mode\n    if: needs.select-mode.outputs.ordinary_mi == 'true'\n", controller)
        self.assertIn('uses: ./.github/workflows/decimal-finite-native.yml', controller)
        self.assertIn('timeout-minutes: 40', workflow); self.assertIn('timeout-minutes: 25', workflow)
        self.assertIn('decimal-finite-native-evidence-${{ github.sha }}-${{ github.run_attempt }}', workflow)
        self.assertNotIn('!target/decimal-finite-native-evidence/native-dist', workflow)
        source = (HERE/'run.py').read_text()
        self.assertIn("r.phase('native', native)", source)
        self.assertNotIn('ordinary-dynamic-module-pilot', source)
        self.assertIn("['cargo', 'build', '--locked', '-p', 'ryupy-cli']", source)
        self.assertIn("snapshot(moved) == before", source)
        self.assertIn("hidden.rename(r.inputs)", source)
        self.assertIn("'source-isolation.json', {'restored': True}", source)


if __name__ == '__main__': unittest.main()
