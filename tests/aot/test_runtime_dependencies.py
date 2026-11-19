"""固定 provider 的角色合同测试：RID、许可证、托管闭包以及未知 sidecar。"""
import unittest
from manifest_files import ManifestError
from runtime_dependencies import MANAGED, NATIVE, validate_runtime_dependencies


class RuntimeDependenciesTests(unittest.TestCase):
    def rows(self, rid, jit):
        pairs = {NATIVE[rid]: "runtime_native_dependency",
                 "ThirdPartyNotices.sqlite3.txt": "runtime_dependency_license"}
        if jit:
            pairs.update({name: "runtime_managed_dependency" for name in MANAGED})
            pairs["RuntimeDependencies.props"] = "runtime_dependency_contract"
        return [{"relative_path": name, "role": role} for name, role in pairs.items()]

    def test_host_specific_complete_sets(self):
        for rid in NATIVE:
            for jit in (False, True):
                rows = self.rows(rid, jit)
                validate_runtime_dependencies(rows, rid)
                for index in range(len(rows)):
                    with self.assertRaises(ManifestError):
                        validate_runtime_dependencies(rows[:index] + rows[index + 1:], rid)

    def test_wrong_rid_or_role_or_extra_provider(self):
        for name, role in [("libe_sqlite3.dylib", "runtime_native_dependency"),
                           ("custom.dll", "runtime_managed_dependency"),
                           ("e_sqlite3.dll", "runtime_dependency_license")]:
            rows = self.rows("linux-x64", True)
            rows.append({"relative_path": name, "role": role})
            with self.assertRaises(ManifestError):
                validate_runtime_dependencies(rows, "linux-x64")


if __name__ == "__main__":
    unittest.main()
