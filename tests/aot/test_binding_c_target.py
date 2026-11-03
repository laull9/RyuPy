"""仅模拟命令构造，不执行 C/SDK，不声称 macOS 12 实机支持。"""
import subprocess
import tempfile
import unittest
from pathlib import Path
from unittest.mock import patch
import binding_c


class TargetTests(unittest.TestCase):
    def commands(self, rid):
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            source = root/'probe.c'
            source.write_bytes(b'return (int64_t)left + (int64_t)right;')
            calls = []
            class Runner:
                def run(self, label, argv, **kwargs):
                    calls.append((label, argv))
                    stdout = (str(source)+'\n').encode() if label=='ar-path' else b'fixture tools'
                    return subprocess.CompletedProcess(argv, 0, stdout, b'')
            records = {}
            with patch.object(binding_c,'tool',side_effect=lambda x:x), \
                 patch.object(binding_c,'digest',return_value={}), \
                 patch.object(binding_c,'archive_objects',return_value=[]), \
                 patch.object(binding_c,'xcode_version',return_value='fixture'):
                binding_c.build_c(Runner(),source,root/'products',rid,records)
            return dict(calls),records

    def test_macos_all_owned_c_compile_and_link_commands_share_minimum(self):
        calls,records=self.commands('osx-arm64')
        labels=('c-object','c-reference-build','c-shared','c-missing-symbol','c-decoy')
        for label in labels:
            self.assertEqual(calls[label].count('-mmacosx-version-min=12.0'),1,label)
        self.assertNotIn('-mmacosx-version-min=12.0',calls['c-archive'])
        self.assertEqual(records['fixture_macos_deployment_target'],'12.0')
        self.assertEqual(sum(label in calls for label in labels)+1,6)

    def test_linux_commands_do_not_gain_apple_flags(self):
        calls,records=self.commands('linux-x64')
        self.assertFalse(any('macosx-version' in arg for args in calls.values() for arg in args))
        self.assertNotIn('fixture_macos_deployment_target',records)


if __name__=='__main__': unittest.main()
