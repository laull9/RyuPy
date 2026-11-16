# 纯mock反例：不运行CP/CLR/Native，不把模拟结果计产品通过。
import contextlib
import io
import json
from pathlib import Path
import subprocess
import sys
import tempfile
import unittest
from unittest.mock import patch
import product_runner as runner

class RunnerTests(unittest.TestCase):
    def run_case(self, fault=None):
        with tempfile.TemporaryDirectory() as temp:
            root = Path(temp)
            expected = {'functional': [['identity', True]], 'default': [['default-restored', True]]}
            golden = root / 'expected.json'; golden.write_text(json.dumps(expected))
            config = {'files': [{'path': str(golden), 'sha256': runner.digest(golden)}],
                      'cp_expected': str(golden), 'env': {},
                      'backends': [{'name': n, 'argv': ['fake-host'], 'cwd': temp, 'metadata_expected': {'functional': [[['__name__', ['str', 'subject']]]] * 4, 'default': [[['__name__', ['str', 'subject']]]], 'indirect': []}}
                                   for n in ('jit',)]}
            if fault == 'backend': config['backends'].pop()
            if fault == 'identity': config['files'][0]['sha256'] = 'bad'
            config_file = root / 'config.json'; config_file.write_text(json.dumps(config))
            output = root / 'output'
            calls = []
            def fake(argv, **kwargs):
                calls.append(argv)
                if fault == 'launch': raise OSError('mock launch failure')
                if fault == 'timeout': raise subprocess.TimeoutExpired(argv, 60, b'partial', b'partial-error')
                mode = argv[-2]
                count = 4 if mode == 'functional' else 1 if mode == 'default' else 0
                if fault != 'missing-metadata':
                    for i in range(count):
                        Path(argv[-1] + '.' + str(i) + '.json').write_text(json.dumps([['__name__', ['str', 'wrong' if fault == 'wrong-metadata' else 'subject']]]))
                if fault == 'malformed-metadata':
                    Path(argv[-1] + '.0.json').write_bytes(b'{broken')
                value = [['product-indirect-canonical', 'NotImplementedError']] if mode == 'indirect' else expected[mode]
                stream = ('\n'.join(json.dumps(x) for x in value) + '\n').encode()
                return subprocess.CompletedProcess(argv, -11 if fault == 'signal' else 0,
                    b'wrong' if fault == 'stdout' else stream, b'warning' if fault == 'stderr' else b'')
            with patch.object(sys, 'argv', ['runner', '--execute', str(config_file), str(output), 'jit']), patch.object(subprocess, 'run', fake):
                if fault:
                    with self.assertRaises((SystemExit, RuntimeError)): runner.main()
                else: runner.main()
            count = 0 if fault in ('identity', 'backend') else 1 if fault else 3
            self.assertEqual(len(calls), count)
            if count:
                self.assertTrue((output / 'jit-functional.stdout').is_file())
                self.assertTrue((output / 'jit-functional.stderr').is_file())
                records = json.loads((output / 'RUN.json').read_text())
                self.assertEqual(len(records), count)
                if fault: self.assertFalse(records[0]['ok'])
                if fault == 'malformed-metadata':
                    self.assertEqual(records[0]['metadata_error']['type'], 'JSONDecodeError')
                    self.assertEqual((output / 'jit-functional.metadata.0.json').read_bytes(), b'{broken')
                    self.assertEqual(len(records[0]['metadata_files']), 4)
            return count
    def test_success(self): self.run_case()
    def test_bad_stdout(self): self.run_case('stdout')
    def test_nonempty_stderr(self): self.run_case('stderr')
    def test_signal(self): self.run_case('signal')
    def test_launch(self): self.run_case('launch')
    def test_timeout(self): self.run_case('timeout')
    def test_identity(self): self.run_case('identity')
    def test_missing_metadata(self): self.run_case('missing-metadata')
    def test_wrong_metadata(self): self.run_case('wrong-metadata')
    def test_malformed_metadata(self): self.run_case('malformed-metadata')
    def test_missing_backend(self): self.run_case('backend')

if __name__ == '__main__': unittest.main()
