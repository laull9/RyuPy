import tempfile
import unittest
from pathlib import Path
from aot_layout import reported_aot
from smoke import published_native
from platform_smoke import published_platform_native

class LayoutTests(unittest.TestCase):
    def fixture(self, root, binding=False, windows=False, stem='hello & 中文'):
        build=root/(stem+'.build'); attempt=build/'attempt-1-2-3'
        managed_dir=attempt/'ryupy-binding-1-2-3'/'bundle' if binding else attempt
        managed_dir.mkdir(parents=True); managed=managed_dir/(stem+'.dll'); managed.touch()
        sdk=managed_dir/'ryupy-aot-1-2-3'; (sdk/'host').mkdir(parents=True); (sdk/'publish').mkdir()
        dist=root/(stem+'.dist'); dist.mkdir(); native=dist/(stem+('.exe' if windows else '')); native.touch()
        manifest=dist/'ryupy-artifact.json'; manifest.write_text('{}')
        report=f'已生成 {managed}\n已生成 {native}\nAOT 清单：{manifest}\n'.encode()
        return stem,report,managed,sdk,native
    def test_plain_and_binding_layouts_and_windows_identity(self):
        for binding in (False,True):
            for windows in (False,True):
                with tempfile.TemporaryDirectory() as tmp:
                    root=Path(tmp); stem,report,managed,sdk,native=self.fixture(root,binding,windows)
                    got=reported_aot(report,root,stem,'win-x64' if windows else 'linux-x64')
                    self.assertEqual((got.managed,got.sdk_root,got.native),(managed,sdk,native))
    def test_duplicate_missing_wrong_root_and_old_attempt_not_selected(self):
        with tempfile.TemporaryDirectory() as tmp:
            root=Path(tmp); stem,report,managed,sdk,native=self.fixture(root)
            for bad in (report+f'已生成 {native}\n'.encode(),report.split(b'AOT')[0]):
                with self.assertRaises(AssertionError): reported_aot(bad,root,stem,'linux-x64')
            other=root/'other'; other.mkdir()
            with self.assertRaises((AssertionError,FileNotFoundError)): reported_aot(report,other,stem,'linux-x64')
            (managed.parent/'ryupy-aot-extra').mkdir()
            with self.assertRaises(AssertionError): reported_aot(report,root,stem,'linux-x64')
    def test_wrong_managed_name_manifest_parent_and_no_sdk_are_rejected(self):
        with tempfile.TemporaryDirectory() as tmp:
            root=Path(tmp); stem,report,managed,sdk,native=self.fixture(root)
            wrong=managed.parent/'other.dll'; wrong.touch()
            with self.assertRaises(AssertionError):
                reported_aot(report.replace(str(managed).encode(),str(wrong).encode()),root,stem,'linux-x64')
            outside=root/'ryupy-artifact.json'; outside.write_text('{}')
            with self.assertRaises(AssertionError):
                reported_aot(report.replace(str(native.parent/'ryupy-artifact.json').encode(),str(outside).encode()),root,stem,'linux-x64')
            sdk.rename(sdk.with_name('not-an-aot-root'))
            with self.assertRaises(AssertionError): reported_aot(report,root,stem,'linux-x64')

    def test_linux_native_stem_ending_dll_is_not_the_managed_entry(self):
        with tempfile.TemporaryDirectory() as tmp:
            root=Path(tmp); stem,report,managed,_,native=self.fixture(root,stem='demo.dll')
            layout=reported_aot(report,root,stem,'linux-x64')
            self.assertEqual(layout.native.name,'demo.dll')
            self.assertEqual(layout.managed.name,'demo.dll.dll')
            self.assertEqual((layout.managed,layout.native),(managed,native))

    def test_old_helpers_reject_new_dist_contract(self):
        with tempfile.TemporaryDirectory() as tmp:
            root=Path(tmp); _,report,_,_,_=self.fixture(root)
            with self.assertRaises(AssertionError): published_native(report,root)
            with self.assertRaises(AssertionError): published_platform_native(report,root,'win-x64')

if __name__=='__main__': unittest.main()
