"""目录形状和明确标记的轻测；真正加载、发布和搬迁由产品驱动验证。"""
import json
from pathlib import Path
import tempfile
import unittest

from application_layout import CANONICAL, MARKER, application_layout
from manifest_files import ManifestError
from runtime_dependencies import validate_runtime_dependencies
from manifest_options import validate_options, BOOL_OPTIONS


class ApplicationLayoutTests(unittest.TestCase):
    def test_current_cold_options_keep_unknown_keys_and_types_rejected(self):
        options={key:False for key in BOOL_OPTIONS}
        options.update(instance_modules=[],module_environment_policy='catalog-only',
                       experimental_records_increment=False,experimental_stock_thin_functions=False)
        validate_options(options)
        for key,value in [('unknown',False),('experimental_stock_module_namespaces',1),
                          ('distribution_metadata','not-a-report')]:
            with self.assertRaises(ManifestError):validate_options({**options,key:value})

    def product(self, root, entry='中文应用', kind='native_aot'):
        marker=root/MARKER
        marker.parent.mkdir()
        marker.write_text(json.dumps({**CANONICAL,'entry':entry,'entry_kind':kind}))
        (root/entry).write_text('手写入口占位，不执行')
        return marker

    def test_exact_public_root_and_internal_manifest(self):
        with tempfile.TemporaryDirectory(prefix='ryupy-layout-') as directory:
            root=Path(directory); self.product(root)
            layout=application_layout(root,require_canonical=True)
            self.assertEqual(layout.relative_manifest,'internal/ryupy-artifact.json')
            layout.check_public_root(root/'中文应用')
            marker={'relative_path':MARKER,'role':'application_layout'}
            layout.check_artifact_paths([marker,{'relative_path':'internal/python/data','role':'unclassified_sidecar'}])
            for extra in ('RyuPy.Runtime.dll','cpython-provider.json'):
                path=root/extra; path.touch()
                with self.assertRaises(ManifestError):layout.check_public_root(root/'中文应用')
                path.unlink()
                with self.assertRaises(ManifestError):
                    layout.check_artifact_paths([marker,{'relative_path':extra,'role':'unclassified_sidecar'}])

    def test_missing_unknown_invalid_and_duplicate_marker_never_guess_layout(self):
        with tempfile.TemporaryDirectory() as directory:
            root=Path(directory)
            self.assertFalse(application_layout(root).canonical)
            with self.assertRaises(ManifestError):application_layout(root,require_canonical=True)
            marker=self.product(root)
            valid=json.loads(marker.read_text())
            cases=[{**valid,'schema_version':True},{**valid,'entry':'../escape'},
                   {**valid,'entry_kind':'unknown'},{**valid,'extra':1},
                   {**valid,'payload_mode':'other'}]
            for value in cases:
                marker.write_text(json.dumps(value))
                with self.assertRaises(ManifestError):application_layout(root)
            marker.write_text(json.dumps(valid)[:-1]+',"entry":"中文应用"}')
            with self.assertRaises(ManifestError):application_layout(root)

    def test_marker_role_and_native_runtime_paths_are_explicit(self):
        with tempfile.TemporaryDirectory() as directory:
            root=Path(directory);self.product(root)
            layout=application_layout(root)
            marker={'relative_path':MARKER,'role':'application_layout'}
            for records in ([],[{**marker,'role':'unclassified_sidecar'}],[marker,marker]):
                with self.assertRaises(ManifestError):layout.check_artifact_paths(records)
            library={'relative_path':'internal/libe_sqlite3.dylib','role':'runtime_native_dependency'}
            notice={'relative_path':'internal/ThirdPartyNotices.sqlite3.txt','role':'runtime_dependency_license'}
            validate_runtime_dependencies([marker,library,notice],'osx-arm64')
            with self.assertRaises(ManifestError):
                validate_runtime_dependencies([marker,{**library,'relative_path':'libe_sqlite3.dylib'},notice],'osx-arm64')


if __name__=='__main__':unittest.main()
