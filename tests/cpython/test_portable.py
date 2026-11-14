#!/usr/bin/env python3
"""发行准备的纯合同回归：历史资产命名、真实字段版本、离线缓存与API凭据隔离。"""
import importlib.util
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
    def test_api_credential_only_uses_stdin_for_official_api(self):
        with mock.patch.dict(os.environ, {'GITHUB_TOKEN': 'fixture_not_a_secret'}, clear=True):
            with mock.patch.object(portable.subprocess, 'check_output', return_value=b'{}') as call:
                portable.fetch(portable.API + '?per_page=5')
                self.assertNotIn('fixture_not_a_secret', str(call.call_args.args))
                self.assertIn(b'fixture_not_a_secret', call.call_args.kwargs['input'])
            with mock.patch.object(portable.subprocess, 'check_output', return_value=b'{}') as call:
                portable.fetch('https://raw.githubusercontent.com/vendor/source')
                self.assertIsNone(call.call_args.kwargs['input'])

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
            root.mkdir(parents=True)
            metadata = {'version': '7', 'optimizations': 'pgo', 'python_version': version,
                        'target_triple': triple, 'libpython_link_mode': 'shared'}
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


if __name__ == '__main__':
    unittest.main()
