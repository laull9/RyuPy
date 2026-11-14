//! 编译器内嵌运行时的固定闭包；构建脚本与运行时定位共同使用此文件。
pub const RUNTIME_DLL: &str = "RyuPy.Runtime.dll";
pub const BRIDGE_DLL: &str = "RyuPy.CPython.dll";
pub const DEPENDENCIES: &str = "RuntimeDependencies.props";
pub const LICENSE: &str = "ThirdPartyNotices.sqlite3.txt";
pub const MANAGED_DEPENDENCIES: &[&str] = &[
    "Microsoft.Data.Sqlite.dll",
    "SQLitePCLRaw.core.dll",
    "SQLitePCLRaw.batteries_v2.dll",
    "SQLitePCLRaw.provider.e_sqlite3.dll",
];
