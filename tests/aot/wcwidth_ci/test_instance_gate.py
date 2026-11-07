"""离线脚本契约测试；不启动编译器、SDK或容器。"""
import ast
import json
from pathlib import Path
import tempfile
import unittest
from unittest.mock import patch
import instance_gate as gate


class InstanceGateTests(unittest.TestCase):
    def test_manifest_requires_every_selected_module_and_no_source_payload(self):
        value = {'modules': [{'name': '__main__', 'environment_reason': 'policy-disabled'},
                 {'name': 'subject', 'environment_reason': 'explicit-instance',
                  'getter': None, 'setter': None, 'exports': []}],
                 'module_locations': [{'module_name': 'subject', 'kind': 'executable_relative'}],
                 'scope': {'resources_included': False}, 'resources': []}
        with tempfile.TemporaryDirectory() as temporary:
            root = Path(temporary)
            (root / 'ryupy-artifact.json').write_text(json.dumps(value))
            gate.check_manifest(root, ['subject'])
            with self.assertRaisesRegex(RuntimeError, '缺少显式'):
                gate.check_manifest(root, ['missing'])
            (root / 'subject.py').write_text('pass\n')
            with self.assertRaisesRegex(RuntimeError, 'Python'):
                gate.check_manifest(root, ['subject'])

    def test_fixtures_are_parseable_and_only_empty_resource_anchors(self):
        for path in gate.FIXTURES.glob('*.py'):
            ast.parse(path.read_text(), filename=str(path))
        for name in ['original', 'combined']:
            request = json.loads((gate.FIXTURES / (name + '-resources.json')).read_text())
            self.assertEqual(request['schema_version'], 1)
            self.assertTrue(all(item['files'] == [] for item in request['modules']))
            names = [item['module'] for item in request['modules']]
            self.assertEqual(len(names), len(set(names)))
            self.assertIn('wcwidth.wcwidth', names)
        self.assertEqual(len((gate.FIXTURES / 'combined.stdout').read_bytes()), 549)

    def test_zero_tests_or_wrong_named_result_cannot_open_gate(self):
        with tempfile.TemporaryDirectory() as temporary:
            root = Path(temporary)
            with patch.object(gate.h, 'OUT', root), patch.object(gate.h, 'run'):
                (root / 'guard.stdout').write_text('running 0 tests\ntest result: ok. 0 passed\n')
                with self.assertRaisesRegex(RuntimeError, '具名'):
                    gate.tested('guard', ['cargo'], ['required'])
                (root / 'guard.stdout').write_text('test suite::required ... ignored\n')
                with self.assertRaisesRegex(RuntimeError, '具名'):
                    gate.tested('guard', ['cargo'], ['required'])
                (root / 'guard.stdout').write_text('test suite::required ... ok\n')
                gate.tested('guard', ['cargo'], ['required'])

    def test_cross_stage_tools_require_fixed_python_and_previous_hashes(self):
        with tempfile.TemporaryDirectory() as temporary:
            python = Path(temporary) / 'python'
            python.touch()
            old = {'python_sha256': gate.FIXED_PYTHON_SHA256,
                   'python_path': str(python), 'ci_commit': 'candidate'}
            hashes = {'cli': 'old-cli', 'runtime': 'old-runtime', python: gate.FIXED_PYTHON_SHA256}
            with patch.dict(gate.os.environ, PYTHON=str(python), GITHUB_SHA='candidate'), \
                 patch.object(gate.h, 'sha', side_effect=lambda p: hashes[p]):
                gate.verify_tools('cli', 'runtime', python, ('old-cli', 'old-runtime'), old)
                for target in ['cli', 'runtime', python]:
                    previous = hashes[target]
                    hashes[target] = 'changed'
                    with self.assertRaisesRegex(RuntimeError, '身份不符'):
                        gate.verify_tools('cli', 'runtime', python, ('old-cli', 'old-runtime'), old)
                    hashes[target] = previous
                old['python_sha256'] = 'newly-refrozen'
                with self.assertRaisesRegex(RuntimeError, '身份不符'):
                    gate.verify_tools('cli', 'runtime', python, ('old-cli', 'old-runtime'), old)

    def test_product_gate_is_before_publish_loop(self):
        source = Path(gate.__file__).read_text()
        self.assertLess(source.index("tested('real-source-pe-gate'"),
                        source.index("for name in ['original', 'combined']"))
        for required in ["'--pull=never'", "'--read-only'", "'none'", "preserve_dist(dist, h.OUT)"]:
            self.assertIn(required, source)


if __name__ == '__main__':
    unittest.main()
