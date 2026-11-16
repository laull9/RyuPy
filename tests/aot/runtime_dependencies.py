"""SQLite 固定 provider 的发布角色与完整 JIT 随包合同；不读取普通文件内容或计算摘要。"""
from manifest_files import ManifestError

RUNTIME_ROLES = {'runtime_native_dependency', 'runtime_managed_dependency',
                 'runtime_dependency_contract', 'runtime_dependency_license'}
MANAGED = {'Microsoft.Data.Sqlite.dll', 'SQLitePCLRaw.core.dll',
           'SQLitePCLRaw.batteries_v2.dll', 'SQLitePCLRaw.provider.e_sqlite3.dll'}
NATIVE = {'linux-x64': 'libe_sqlite3.so', 'osx-arm64': 'libe_sqlite3.dylib', 'win-x64': 'e_sqlite3.dll'}


def validate_runtime_dependencies(artifacts, rid):
    """AOT 保留宿主 SQLite 库；JIT 若带版本合同就必须携带全套托管和原生依赖。"""
    selected = {row['relative_path']: row['role'] for row in artifacts if row['role'] in RUNTIME_ROLES}
    if not selected:
        return
    library = NATIVE[rid]
    expected = {library: 'runtime_native_dependency',
                'ThirdPartyNotices.sqlite3.txt': 'runtime_dependency_license'}
    if any(role in {'runtime_managed_dependency', 'runtime_dependency_contract'} for role in selected.values()):
        expected.update({name: 'runtime_managed_dependency' for name in MANAGED})
        expected['RuntimeDependencies.props'] = 'runtime_dependency_contract'
    if selected != expected:
        raise ManifestError('运行时随包依赖的文件名、RID、角色或完整性不符')
