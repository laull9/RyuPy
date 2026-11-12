"""无SDK轻测试：Windows使用隔离用户目录，不继承宿主工具与配置。"""
from pathlib import Path
import sys
import tempfile
from types import SimpleNamespace
import unittest
from unittest import mock

sys.path.insert(0, str(Path(__file__).resolve().parents[1]))
from platform_support import clean_environment


class CleanEnvironmentTests(unittest.TestCase):
    def test_windows_profile_and_temporary_directories_are_isolated(self):
        inherited = {key: 'must-not-inherit' for key in (
            'PATH', 'HOME', 'USERPROFILE', 'APPDATA', 'LOCALAPPDATA', 'TEMP',
            'TMP', 'PYTHONPATH', 'PYTHONHOME', 'DOTNET_ROOT', 'DOTNET',
            'RYUPY_RUNTIME_DIR', 'NUGET_PACKAGES')}
        inherited.update(SystemRoot='C:\\Windows', WINDIR='C:\\Windows')
        with tempfile.TemporaryDirectory() as folder:
            work = Path(folder).resolve()
            # 仅替换被测模块的平台信息，避免Linux的Path误造WindowsPath。
            with mock.patch('platform_support.os', SimpleNamespace(
                    name='nt', environ=inherited)):
                env = clean_environment(work)
            self.assertEqual(set(env), {'PATH', 'HOME', 'USERPROFILE', 'APPDATA',
                'LOCALAPPDATA', 'TEMP', 'TMP', 'SystemRoot', 'WINDIR'})
            for key in ('HOME', 'USERPROFILE', 'APPDATA', 'LOCALAPPDATA', 'TEMP', 'TMP'):
                path = Path(env[key])
                self.assertTrue(path.is_dir(), key)
                self.assertTrue(path.is_relative_to(work), key)
                self.assertNotEqual(path, work, key)
            self.assertEqual(env['HOME'], env['USERPROFILE'])
            self.assertEqual(env['TEMP'], env['TMP'])
            self.assertTrue(Path(env['APPDATA']).is_relative_to(Path(env['USERPROFILE'])))
            self.assertTrue(Path(env['LOCALAPPDATA']).is_relative_to(Path(env['USERPROFILE'])))
            for key in ('APPDATA', 'TEMP', 'PATH'):
                self.assertEqual(list(Path(env[key]).iterdir()), [])
            self.assertEqual(env['SystemRoot'], inherited['SystemRoot'])
            self.assertEqual(env['WINDIR'], inherited['WINDIR'])

    def test_posix_contract_is_unchanged(self):
        with tempfile.TemporaryDirectory() as folder:
            work = Path(folder).resolve()
            with mock.patch('platform_support.os', SimpleNamespace(name='posix', environ={})):
                env = clean_environment(work)
            self.assertEqual(env, {'PATH': str(work/'empty-path'), 'HOME': str(work),
                'TMPDIR': str(work), 'LANG': 'en_US.UTF-8', 'LC_ALL': 'en_US.UTF-8'})
            self.assertEqual(list(Path(env['PATH']).iterdir()), [])

    def test_tool_found_in_clean_path_is_rejected(self):
        with tempfile.TemporaryDirectory() as folder:
            with mock.patch('platform_support.shutil.which', return_value='unexpected-tool'):
                with self.assertRaisesRegex(AssertionError, '精简PATH仍包含dotnet'):
                    clean_environment(Path(folder))


if __name__ == '__main__':
    unittest.main()
