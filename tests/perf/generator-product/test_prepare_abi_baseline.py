"""只验证驱动拒绝/证据协议；mock构建不代表真实C#或历史ABI通过。"""
import hashlib
import json
from pathlib import Path
import subprocess
import tempfile
import unittest
from unittest.mock import patch
from prepare_abi_baseline import COMMIT, SDK, prepare, verify_checkout


class BaselineDriverTests(unittest.TestCase):
    def tree(self, root):
        source = root / 'fixed dev'
        project = source / 'runtime/RyuPy.Runtime/RyuPy.Runtime.csproj'
        project.parent.mkdir(parents=True)
        project.write_text('<Project/>')
        return source

    def test_wrong_commit_and_dirty_tree_rejected(self):
        with tempfile.TemporaryDirectory() as temp:
            source = self.tree(Path(temp))
            for replies in [('bad',), (COMMIT, ' M runtime/file.cs')]:
                with patch('prepare_abi_baseline.output', side_effect=replies):
                    with self.assertRaises(AssertionError): verify_checkout(source)

    def test_reused_or_in_tree_evidence_rejected(self):
        with tempfile.TemporaryDirectory() as temp:
            source = self.tree(Path(temp))
            existing = Path(temp) / 'existing'; existing.mkdir()
            for dest in [existing, source / 'new-evidence']:
                with patch('prepare_abi_baseline.output', side_effect=[COMMIT, '']):
                    with self.assertRaises(AssertionError): prepare(source, dest)

    def test_wrong_actual_sdk_rejected(self):
        with tempfile.TemporaryDirectory() as temp:
            source = self.tree(Path(temp))
            with patch('prepare_abi_baseline.output', side_effect=[COMMIT, '', '10.0.400']):
                with self.assertRaises(AssertionError): prepare(source, Path(temp) / 'evidence')

    def test_failed_or_missing_build_never_emits_manifest(self):
        for status in [0, 1]:
            with tempfile.TemporaryDirectory() as temp:
                source = self.tree(Path(temp)); evidence = Path(temp) / 'evidence'
                with patch('prepare_abi_baseline.output', side_effect=[COMMIT, '', SDK]), \
                     patch('prepare_abi_baseline.subprocess.run', return_value=subprocess.CompletedProcess([], status)):
                    with self.assertRaises(AssertionError): prepare(source, evidence)
                self.assertFalse((evidence / 'manifest.json').exists())
                self.assertTrue((evidence / 'build.log').exists())

    def test_mock_success_hash_and_paths_are_exact(self):
        with tempfile.TemporaryDirectory() as temp:
            source = self.tree(Path(temp)); evidence = Path(temp) / 'outside evidence'
            data = b'only mocked bytes, not a real PE'
            def run(command, **kwargs):
                self.assertEqual(command[:2], ['dotnet', 'build'])
                self.assertEqual(kwargs['cwd'], evidence)
                dll = evidence / 'build/bin/RyuPy.Runtime/release/RyuPy.Runtime.dll'
                dll.parent.mkdir(parents=True); dll.write_bytes(data)
                return subprocess.CompletedProcess(command, 0)
            with patch('prepare_abi_baseline.output', side_effect=[COMMIT, '', SDK, COMMIT, '']), \
                 patch('prepare_abi_baseline.subprocess.run', side_effect=run):
                manifest = json.loads(prepare(source, evidence).read_text())
            self.assertEqual(manifest['sha256'], hashlib.sha256(data).hexdigest())
            self.assertEqual(manifest['commit'], COMMIT)
            self.assertEqual(manifest['bytes'], len(data))
            self.assertTrue(Path(manifest['dll']).is_absolute())


if __name__ == '__main__': unittest.main()
