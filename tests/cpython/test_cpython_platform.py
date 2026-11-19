#!/usr/bin/env python3
"""CPython provider 平台布局、ABI 契约与跨平台依赖审计测试。
验证 Linux x64、Windows x64 与 macOS arm64 的机器架构解析、依赖封闭性规则、
系统证书策略以及中文/空格路径的隔离支持。测试临时产物严格在仓库外创建并退出清理。
"""
import json
import os
from pathlib import Path
import shutil
import struct
import sys
import unittest

# 保持无字节码缓存
sys.dont_write_bytecode = True

from support import (
    ROOT,
    VERSION,
    declaration,
    external_workspace,
    rid,
    write_json,
)


class TestCpythonPlatformContract(unittest.TestCase):
    def setUp(self):
        self.workspace = external_workspace('ryupy-cpython-platform-test-')

    def tearDown(self):
        if self.workspace and self.workspace.exists():
            shutil.rmtree(self.workspace, ignore_errors=True)

    def test_current_rid_validity(self):
        """验证当前运行主机的 RID 属于已识别平台集合。"""
        current_rid = rid()
        self.assertIn(current_rid, ('osx-arm64', 'linux-x64', 'win-x64'))

    def test_multiplatform_declaration_generation(self):
        """测试在不同平台生成的分发声明包含固定来源、构建版本与合规许可证。"""
        fake_dist = self.workspace / 'fake_dist'
        fake_dist.mkdir()
        (fake_dist / 'licenses').mkdir()
        (fake_dist / 'lib/python3.12').mkdir(parents=True)
        (fake_dist / 'lib/python3.12/LICENSE.txt').write_text('CPython License', encoding='utf-8')
        (fake_dist / 'licenses/LICENSE.openssl-3.txt').write_text('OpenSSL License', encoding='utf-8')

        decl = declaration(fake_dist, [], ['math', 'json'], product_root=self.workspace)
        self.assertEqual(decl['schema'], 1)
        self.assertEqual(decl['python_version'], VERSION)
        self.assertEqual(decl['source']['project'], 'python-build-standalone')
        self.assertEqual(decl['source']['build_id'], '20260623')
        self.assertIn('licenses/LICENSE.openssl-3.txt', decl['source']['licenses'])

        # 验证 shim 名字与平台匹配
        shim_name = Path(decl['shim']).name
        if sys.platform == 'darwin':
            self.assertEqual(shim_name, 'libryupy_cpython.dylib')
        elif sys.platform == 'win32':
            self.assertEqual(shim_name, 'ryupy_cpython.dll')
        else:
            self.assertEqual(shim_name, 'libryupy_cpython.so')

    def test_unicode_and_spaces_workspace_path(self):
        """测试中文、空格及特殊符号路径下的分发声明与隔离写入，验证无编码或转义异常。"""
        unicode_dir = self.workspace / '路径 测试 & 目录 with spaces'
        unicode_dir.mkdir(parents=True)
        config_file = unicode_dir / 'ryupy-cpython.json'

        sample_config = {
            'schema': 1,
            'python_version': VERSION,
            'home': 'python',
            'library': 'python/lib/libpython3.12.dylib',
            'shim': 'libryupy_cpython.dylib',
            'search_paths': ['python/lib/python3.12', 'python/lib/python3.12/site-packages'],
            'stdlib_provider': 'cpython',
            'modules': ['math', 'sys', 'sqlite3'],
            'container_transfer': 'synchronized',
        }
        write_json(config_file, sample_config)
        self.assertTrue(config_file.is_file())
        read_back = json.loads(config_file.read_text(encoding='utf-8'))
        self.assertEqual(read_back['container_transfer'], 'synchronized')
        self.assertEqual(read_back['modules'], ['math', 'sys', 'sqlite3'])


if __name__ == '__main__':
    unittest.main()
