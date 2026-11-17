#!/usr/bin/env python3
"""发行准备的纯合同回归：历史资产命名、真实字段版本、离线缓存与API凭据隔离。"""
import importlib.util
import ast
import json
import os
from pathlib import Path
import sys
import tempfile
import unittest
from unittest import mock

sys.dont_write_bytecode = True
ROOT = Path(__file__).resolve().parents[2]
spec = importlib.util.spec_from_file_location('portable', ROOT / 'crates/ryupy-driver/src/cpython_portable.py')
portable = importlib.util.module_from_spec(spec)
spec.loader.exec_module(portable)


class PortableContracts(unittest.TestCase):
    def test_component_sources_accept_annotated_literal_without_execution(self):
        for source in ["DOWNLOADS = {'zlib-ng': {'version': 'fixture'}}", "DOWNLOADS: dict[str, dict] = {'zlib-ng': {'version': 'fixture'}}"]:
            self.assertEqual(portable.download_definitions(ast.parse(source))['zlib-ng']['version'], 'fixture')
        with self.assertRaisesRegex(ValueError, '字面量定义'):
            portable.download_definitions(ast.parse('UNRELATED = {}'))
        self.assertIsNone(portable.download_definitions(ast.parse('with source.open() as fh:\n    DOWNLOADS = json.load(fh)')))

    @staticmethod
    def ready_tree(root):
        """手写文件只验证缓存事务，实际ABI由版本产品矩阵另验。"""
        metadata = {'version': '7', 'optimizations': 'pgo', 'python_version': '3.9.13',
                    'target_triple': 'x86_64-pc-windows-msvc', 'libpython_link_mode': 'shared',
                    'license_path': 'licenses/LICENSE.cpython.txt'}
        prefix = portable.RELEASES + 'download/20220528/cpython-3.9.13%2B20220528-x86_64-pc-windows-msvc-shared'
        provenance = {'project': 'python-build-standalone', 'release': portable.RELEASES + 'tag/20220528',
                      'install_archive': prefix + '-install_only.tar.gz', 'metadata_archive': prefix + '-pgo-full.tar.zst'}
        root.mkdir(parents=True)
        for name in ['python.exe', 'include/Python.h', 'python39.dll', 'licenses/LICENSE.cpython.txt']:
            path = root / name
            path.parent.mkdir(parents=True, exist_ok=True)
            path.write_text('handwritten cache contract fixture', encoding='utf-8')
        (root / 'PYTHON.json').write_text(json.dumps(metadata), encoding='utf-8')
        (root / 'distribution-source.json').write_text(json.dumps(provenance), encoding='utf-8')
        return metadata, provenance

    def test_api_credential_only_uses_stdin_for_official_api(self):
        def response(command, **kwargs):
            Path(command[command.index('--output') + 1]).write_bytes(b'{}')
        with mock.patch.dict(os.environ, {'GITHUB_TOKEN': 'fixture_not_a_secret'}, clear=True):
            with mock.patch.object(portable.subprocess, 'run', side_effect=response) as call:
                portable.fetch(portable.API + '?per_page=5')
                self.assertNotIn('fixture_not_a_secret', str(call.call_args.args))
                self.assertIn(b'fixture_not_a_secret', call.call_args.kwargs['input'])
            with mock.patch.object(portable.subprocess, 'run', side_effect=response) as call:
                portable.fetch('https://raw.githubusercontent.com/vendor/source')
                self.assertIsNone(call.call_args.kwargs['input'])

    def test_real_curl_retry_discards_interrupted_json_prefix(self):
        """真实HTTP连接提前关闭后重试；只允许解析第二次完整响应。"""
        from http.server import BaseHTTPRequestHandler, HTTPServer
        from threading import Thread
        replies = []
        class Handler(BaseHTTPRequestHandler):
            def do_GET(self):
                replies.append(self.path)
                payload = b'{"result": "complete"}'
                self.send_response(200)
                self.send_header('Content-Length', str(len(payload)))
                self.end_headers()
                self.wfile.write(b'{"broken":' if len(replies) == 1 else payload)
                self.wfile.flush()
                self.close_connection = True
            def log_message(self, *_args):
                pass
        server = HTTPServer(('127.0.0.1', 0), Handler)
        thread = Thread(target=server.serve_forever, daemon=True)
        thread.start()
        try:
            self.assertEqual(portable.fetch(f'http://127.0.0.1:{server.server_port}/fixture'), {'result': 'complete'})
            self.assertEqual(len(replies), 2)
        finally:
            server.shutdown()
            server.server_close()
            thread.join()

    def test_old_and_current_windows_assets_match_exact_version(self):
        triple = 'x86_64-pc-windows-msvc'
        for version, build, shared in [('3.9.13', '20220528', '-shared'), ('3.12.13', '20260623', '')]:
            prefix = f'cpython-{version}+{build}-{triple}{shared}'
            release = {'tag_name': build, 'assets': [
                {'name': prefix + '-pgo-full.tar.zst'},
                {'name': prefix + '-install_only.tar.gz'}]}
            full, install = portable.release_assets(release, version, triple, 'pgo')
            self.assertEqual(full['name'], prefix + '-pgo-full.tar.zst')
            self.assertEqual(install['name'], prefix + '-install_only.tar.gz')
            self.assertIsNone(portable.release_assets(release, '3.9.12', triple, 'pgo'))
            self.assertIsNone(portable.release_assets(release, version, 'i686-pc-windows-msvc', 'pgo'))

    def test_metadata_seven_and_eight_preserve_actual_optimization_fields(self):
        self.assertEqual(portable.metadata_options({'version': '7', 'optimizations': 'pgo'}), 'pgo')
        self.assertEqual(portable.metadata_options({'version': '8', 'build_options': 'pgo+lto'}), 'pgo+lto')
        with self.assertRaises(ValueError):
            portable.metadata_options({'version': '99', 'build_options': 'pgo'})

    def test_prepared_cache_selects_without_network_and_rejects_wrong_metadata(self):
        with tempfile.TemporaryDirectory(prefix='ryupy-portable-contract-') as temporary:
            home = Path(temporary)
            triple, version, build = 'x86_64-pc-windows-msvc', '3.9.13', '20220528'
            cache = home / '.cache/ryupy/cpython-auto'
            root = cache / f'cpython-{version}+{build}-{triple}'
            metadata, provenance = self.ready_tree(root)
            metadata_path = root / 'PYTHON.json'
            metadata_path.write_text(json.dumps(metadata), encoding='utf-8')
            prefix = portable.RELEASES + f'download/{build}/cpython-{version}%2B{build}-{triple}-shared'
            provenance = {'project': 'python-build-standalone', 'release': portable.RELEASES + 'tag/' + build,
                          'install_archive': prefix + '-install_only.tar.gz',
                          'metadata_archive': prefix + '-pgo-full.tar.zst'}
            (root / 'distribution-source.json').write_text(json.dumps(provenance), encoding='utf-8')
            request = {'version': version, 'triple': triple, 'options': 'pgo', 'local': str(home / 'system')}
            with mock.patch.object(portable.Path, 'home', return_value=home):
                with mock.patch.object(portable, 'fetch', side_effect=AssertionError('缓存命中不应联网')):
                    self.assertEqual(portable.prepare(request)['root'], str(root))
                    metadata['python_version'] = '3.9.12'
                    metadata_path.write_text(json.dumps(metadata), encoding='utf-8')
                    with self.assertRaisesRegex(ValueError, '来源或元数据不匹配'):
                        portable.prepare(request)
                    metadata['python_version'] = version
                    metadata_path.write_text(json.dumps(metadata), encoding='utf-8')
                    provenance['install_archive'] = provenance['install_archive'].replace('x86_64', 'i686')
                    (root / 'distribution-source.json').write_text(json.dumps(provenance), encoding='utf-8')
                    with self.assertRaisesRegex(ValueError, '官方归档来源'):
                        portable.prepare(request)

    def test_cache_publication_requires_complete_licenses(self):
        with tempfile.TemporaryDirectory(prefix='ryupy-cache-commit-') as temporary:
            root = Path(temporary)
            candidate, destination = root / 'candidate', root / 'final'
            self.ready_tree(candidate)
            license_file = candidate / 'licenses/LICENSE.cpython.txt'
            license_file.unlink()
            with self.assertRaisesRegex(ValueError, '尚未齐备'):
                portable.publish_cache(candidate, destination, '3.9.13', 'x86_64-pc-windows-msvc', 'pgo')
            self.assertFalse(destination.exists())
            license_file.write_text('fixture license', encoding='utf-8')
            destination.mkdir()
            (destination / 'old-diagnostic.txt').write_text('preserve incomplete tree', encoding='utf-8')
            portable.publish_cache(candidate, destination, '3.9.13', 'x86_64-pc-windows-msvc', 'pgo')
            self.assertTrue(portable.cache_ready(destination, '3.9.13', 'x86_64-pc-windows-msvc', 'pgo'))
            backups = list(root.glob('incomplete-final-*'))
            self.assertEqual(len(backups), 1)
            self.assertEqual((backups[0] / 'old-diagnostic.txt').read_text(), 'preserve incomplete tree')

    def test_interrupted_json_cache_is_prepared_again(self):
        with tempfile.TemporaryDirectory(prefix='ryupy-interrupted-cache-') as temporary:
            cache = Path(temporary)
            root = cache / 'cpython-3.9.13+20220528-x86_64-pc-windows-msvc'
            self.ready_tree(root)
            (root / 'PYTHON.json').write_text('{unfinished', encoding='utf-8')
            self.assertIsNone(portable.cached_distribution(cache, '3.9.13', 'x86_64-pc-windows-msvc', 'pgo'))

    def test_concurrent_cache_publishers_reuse_complete_winner(self):
        from concurrent.futures import ThreadPoolExecutor
        with tempfile.TemporaryDirectory(prefix='ryupy-cache-concurrent-') as temporary:
            root = Path(temporary)
            trees = [root / 'first', root / 'second']
            for tree in trees:
                self.ready_tree(tree)
            destination = root / 'final'
            with ThreadPoolExecutor(max_workers=2) as pool:
                list(pool.map(lambda tree: portable.publish_cache(tree, destination, '3.9.13', 'x86_64-pc-windows-msvc', 'pgo'), trees))
            self.assertTrue(portable.cache_ready(destination, '3.9.13', 'x86_64-pc-windows-msvc', 'pgo'))
            self.assertFalse(list(root.glob('incomplete-final-*')))

    @staticmethod
    def tree_with_missing_licenses(root, missing_names, version='3.14.7'):
        """构造缺失特定上游许可的测试发行树。"""
        metadata = {
            'version': '8', 'build_options': 'pgo+lto', 'python_version': version,
            'target_triple': 'aarch64-apple-darwin', 'libpython_link_mode': 'shared',
            'license_paths': list(missing_names)
        }
        root.mkdir(parents=True, exist_ok=True)
        (root / 'licenses').mkdir(parents=True, exist_ok=True)
        (root / 'PYTHON.json').write_text(json.dumps(metadata), encoding='utf-8')
        return metadata

    def test_supplemental_license_rules_coverage(self):
        """核对已审查补齐规则表：3.14+ zstd 与旧版 zlib-ng。"""
        self.assertEqual(portable.SUPPLEMENTAL_LICENSE_RULES['licenses/LICENSE.zstd.txt'], ('zstd', 'LICENSE'))
        self.assertEqual(portable.SUPPLEMENTAL_LICENSE_RULES['licenses/LICENSE.zlib-ng.txt'], ('zlib-ng', 'LICENSE.md'))

    def test_complete_licenses_success_for_3_14_zstd(self):
        """正例：3.14 准确按上游 downloads 元数据从官方主源补齐 zstd LICENSE。"""
        with tempfile.TemporaryDirectory(prefix='ryupy-license-zstd-') as temporary:
            root = Path(temporary) / 'cpython'
            self.tree_with_missing_licenses(root, ['licenses/LICENSE.zstd.txt'])
            zstd_license_text = 'BSD License\nFor Zstandard software\nCopyright (c) Meta Platforms, Inc.'
            definitions_py = (
                "DOWNLOADS = {\n"
                "    'zstd': {\n"
                "        'url': 'https://github.com/python/cpython-source-deps/archive/refs/tags/zstd-1.5.7.tar.gz',\n"
                "        'version': '1.5.7',\n"
                "        'license_file': 'LICENSE.zstd.txt'\n"
                "    }\n"
                "}\n"
            )
            def mock_fetch(url, output=None):
                if url.endswith('downloads.py'):
                    Path(output).write_text(definitions_py, encoding='utf-8')
                elif url == 'https://raw.githubusercontent.com/python/cpython-source-deps/zstd-1.5.7/LICENSE':
                    Path(output).write_text(zstd_license_text, encoding='utf-8')
                else:
                    raise AssertionError('未知请求 URL：' + url)

            with mock.patch.object(portable, 'fetch', side_effect=mock_fetch):
                result = {'root': str(root), 'url': 'https://example.com/release', 'build': '20260901'}
                portable.complete_licenses(result)
                target_file = root / 'licenses/LICENSE.zstd.txt'
                self.assertTrue(target_file.is_file())
                self.assertEqual(target_file.read_text(encoding='utf-8'), zstd_license_text)
                sources = json.loads((root / 'license-sources.json').read_text(encoding='utf-8'))
                self.assertEqual(sources['licenses/LICENSE.zstd.txt'],
                                 'https://raw.githubusercontent.com/python/cpython-source-deps/zstd-1.5.7/LICENSE')

    def test_complete_licenses_success_for_zlib_ng(self):
        """正例：从 downloads.json 补齐旧版 zlib-ng LICENSE.md。"""
        with tempfile.TemporaryDirectory(prefix='ryupy-license-zlib-') as temporary:
            root = Path(temporary) / 'cpython'
            self.tree_with_missing_licenses(root, ['licenses/LICENSE.zlib-ng.txt'], version='3.12.13')
            zlib_license_text = 'zlib-ng license text'
            definitions_json = json.dumps({
                'zlib-ng': {
                    'url': 'https://github.com/python/cpython-source-deps/archive/refs/tags/zlib-ng-2.2.4.tar.gz',
                    'version': '2.2.4',
                    'license_file': 'LICENSE.zlib-ng.txt'
                }
            })
            def mock_fetch(url, output=None):
                if url.endswith('downloads.py'):
                    Path(output).write_text("import json\nwith open('x') as f: DOWNLOADS = json.load(f)", encoding='utf-8')
                elif url.endswith('downloads.json'):
                    Path(output).write_text(definitions_json, encoding='utf-8')
                elif url == 'https://raw.githubusercontent.com/python/cpython-source-deps/zlib-ng-2.2.4/LICENSE.md':
                    Path(output).write_text(zlib_license_text, encoding='utf-8')
                else:
                    raise AssertionError('未知请求 URL：' + url)

            with mock.patch.object(portable, 'fetch', side_effect=mock_fetch):
                result = {'root': str(root), 'url': 'https://example.com/release', 'build': '20260623'}
                portable.complete_licenses(result)
                target_file = root / 'licenses/LICENSE.zlib-ng.txt'
                self.assertTrue(target_file.is_file())
                self.assertEqual(target_file.read_text(encoding='utf-8'), zlib_license_text)

    def test_complete_licenses_rejects_unreviewed_license(self):
        """负例：未审查许可缺失时明确报错，拒绝静默忽略或放行。"""
        with tempfile.TemporaryDirectory(prefix='ryupy-license-unreviewed-') as temporary:
            root = Path(temporary) / 'cpython'
            self.tree_with_missing_licenses(root, ['licenses/LICENSE.unknown.txt'])
            def mock_fetch(url, output=None):
                if url.endswith('downloads.py'):
                    Path(output).write_text("DOWNLOADS = {}", encoding='utf-8')
                else:
                    raise AssertionError('不应请求：' + url)

            with mock.patch.object(portable, 'fetch', side_effect=mock_fetch):
                with self.assertRaisesRegex(ValueError, '上游组件许可缺失且无已审查补充规则：licenses/LICENSE.unknown.txt'):
                    portable.complete_licenses({'root': str(root), 'url': 'u', 'build': '20260901'})

    def test_complete_licenses_negative_cases_for_zstd(self):
        """负例：3.14 zstd 上游来源不匹配、缺少定义或空响应拒绝。"""
        with tempfile.TemporaryDirectory(prefix='ryupy-license-neg-zstd-') as temporary:
            root = Path(temporary) / 'cpython'
            self.tree_with_missing_licenses(root, ['licenses/LICENSE.zstd.txt'])

            # 1. 官方组件来源表中缺少 zstd 定义
            with mock.patch.object(portable, 'fetch', side_effect=lambda url, output: Path(output).write_text("DOWNLOADS = {}", encoding='utf-8')):
                with self.assertRaisesRegex(ValueError, '官方组件来源表缺少 zstd 定义'):
                    portable.complete_licenses({'root': str(root), 'url': 'u', 'build': '20260901'})

            # 2. 上游 url 不匹配（非官方 cpython-source-deps 源）
            bad_url_py = "DOWNLOADS = {'zstd': {'url': 'https://evil.com/zstd.tar.gz', 'version': '1.5.7', 'license_file': 'LICENSE.zstd.txt'}}"
            with mock.patch.object(portable, 'fetch', side_effect=lambda url, output: Path(output).write_text(bad_url_py, encoding='utf-8')):
                with self.assertRaisesRegex(ValueError, 'zstd 上游源身份不匹配'):
                    portable.complete_licenses({'root': str(root), 'url': 'u', 'build': '20260901'})

            # 3. 上游 license_file 不匹配
            bad_file_py = "DOWNLOADS = {'zstd': {'url': 'https://github.com/python/cpython-source-deps/archive/refs/tags/zstd-1.5.7.tar.gz', 'version': '1.5.7', 'license_file': 'WRONG.txt'}}"
            with mock.patch.object(portable, 'fetch', side_effect=lambda url, output: Path(output).write_text(bad_file_py, encoding='utf-8')):
                with self.assertRaisesRegex(ValueError, 'zstd 上游源身份不匹配'):
                    portable.complete_licenses({'root': str(root), 'url': 'u', 'build': '20260901'})

            # 4. 许可证响应为空文件
            good_py = "DOWNLOADS = {'zstd': {'url': 'https://github.com/python/cpython-source-deps/archive/refs/tags/zstd-1.5.7.tar.gz', 'version': '1.5.7', 'license_file': 'LICENSE.zstd.txt'}}"
            def empty_fetch(url, output=None):
                if url.endswith('downloads.py'):
                    Path(output).write_text(good_py, encoding='utf-8')
                else:
                    Path(output).write_text('', encoding='utf-8')
            with mock.patch.object(portable, 'fetch', side_effect=empty_fetch):
                with self.assertRaisesRegex(ValueError, '上游组件许可证响应为空：licenses/LICENSE.zstd.txt'):
                    portable.complete_licenses({'root': str(root), 'url': 'u', 'build': '20260901'})

    def test_complete_licenses_negative_cases_for_zlib_ng(self):
        """负例：旧版 zlib-ng 上游源身份不匹配。"""
        with tempfile.TemporaryDirectory(prefix='ryupy-license-neg-zlib-') as temporary:
            root = Path(temporary) / 'cpython'
            self.tree_with_missing_licenses(root, ['licenses/LICENSE.zlib-ng.txt'], version='3.12.13')
            bad_zlib_py = "DOWNLOADS = {'zlib-ng': {'url': 'https://evil.com/zlib.tar.gz', 'version': '2.2.4', 'license_file': 'LICENSE.zlib-ng.txt'}}"
            with mock.patch.object(portable, 'fetch', side_effect=lambda url, output: Path(output).write_text(bad_zlib_py, encoding='utf-8')):
                with self.assertRaisesRegex(ValueError, 'zlib-ng 上游源身份不匹配'):
                    portable.complete_licenses({'root': str(root), 'url': 'u', 'build': '20260623'})


from test_product_layout import ProductLayoutTests  # 公共布局门随python-baseline执行。


if __name__ == '__main__':
    unittest.main()
