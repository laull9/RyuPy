"""无Docker执行的镜像准备合同与所有消费者参数回归。"""
import ast
import copy
import json
from pathlib import Path
import subprocess
import tempfile
import unittest
from docker_image import IMAGE, prepare_image
from resource_smoke import equal


IDENTITY = {'id': 'sha256:'+'a'*64, 'repo_digests': [IMAGE.rsplit(':', 1)[0]+'@sha256:'+'b'*64],
            'os': 'linux', 'architecture': 'amd64'}


class FakeRunner:
    def __init__(self, logs, identity=None, fail=None, stderr=b''):
        self.logs, self.calls, self.fail, self.stderr = logs, [], fail, stderr
        self.identity = IDENTITY if identity is None else identity

    def run(self, label, command, **kwargs):
        self.calls.append((label, command, kwargs))
        if label == self.fail:
            raise RuntimeError(label+'失败')
        output = self.identity if isinstance(self.identity, bytes) else json.dumps(self.identity).encode()
        return subprocess.CompletedProcess(command, 0, output if label == 'docker-inspect' else b'pull',
                                           self.stderr if label == 'docker-inspect' else b'pull progress')


class DockerImageTests(unittest.TestCase):
    def test_preparation_records_identity_not_program_output(self):
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory); run = FakeRunner(root)
            self.assertEqual(prepare_image(run, '/docker', root), IDENTITY['id'])
            self.assertEqual([x[0] for x in run.calls], ['docker-pull', 'docker-inspect'])
            self.assertEqual(run.calls[0][1], ['/docker', 'pull', IMAGE])
            self.assertTrue(all(x[2]['check'] for x in run.calls))
            record = json.loads((root/'docker-image.json').read_text())
            self.assertEqual(record, {**IDENTITY, 'requested_tag': IMAGE})

    def test_preparation_fail_closed(self):
        cases = [('docker-pull', None, b''), ('docker-inspect', None, b''),
                 (None, b'not json', b''), (None, None, b'warning')]
        for key, value in [('os', 'windows'), ('architecture', 'arm64'), ('id', 'tag'),
                           ('repo_digests', []), ('repo_digests', ['other/repo@sha256:'+'b'*64]),
                           ('repo_digests', [7])]:
            identity = copy.deepcopy(IDENTITY); identity[key] = value
            cases.append((None, identity, b''))
        for fail, identity, stderr in cases:
            with self.subTest(fail=fail, identity=identity, stderr=stderr), tempfile.TemporaryDirectory() as directory:
                root = Path(directory); run = FakeRunner(root, identity, fail, stderr)
                with self.assertRaises((RuntimeError, ValueError, AssertionError)):
                    prepare_image(run, '/docker', root)
                self.assertFalse((root/'docker-image.json').exists())
                self.assertEqual(len(run.calls), 1 if fail == 'docker-pull' else 2)

    def test_all_docker_consumers_use_prepared_id_without_pull(self):
        for name, count in [('smoke.py', 2), ('resource_smoke.py', 1), ('binding_smoke.py', 1)]:
            with self.subTest(name=name):
                tree = ast.parse(Path(__file__).with_name(name).read_text())
                commands = [n for n in ast.walk(tree) if isinstance(n, ast.List)
                            and len(n.elts) > 1 and isinstance(n.elts[1], ast.Constant)
                            and n.elts[1].value == 'run']
                self.assertEqual(len(commands), count)
                for command in commands:
                    values = [n.value for n in command.elts if isinstance(n, ast.Constant)]
                    self.assertIn('--pull=never', values)
                    self.assertIn('--read-only', values)
                    self.assertIn('--network', values); self.assertIn('none', values)
                    self.assertEqual(sum(isinstance(n, ast.Name) and n.id == 'image' for n in command.elts), 1)
                    self.assertNotIn(IMAGE, values)

    def test_program_stderr_exit_and_stdout_remain_strict(self):
        reference = subprocess.CompletedProcess([], 0, b'hello\n', b'')
        for code, out, err in [(0, b'hello\n', b'pull progress'), (1, b'hello\n', b''),
                               (0, b'other', b'')]:
            with self.assertRaises(AssertionError):
                equal(reference, subprocess.CompletedProcess([], code, out, err))


if __name__ == '__main__':
    unittest.main()
