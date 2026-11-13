//! 定位并复制 `RyuPy.Runtime.dll`。
//!
//! 查找顺序：
//! 1. 环境变量 `RYUPY_RUNTIME_DIR`（指向包含 `RyuPy.Runtime.dll` 的目录）；
//! 2. 发行包布局，相对**当前可执行文件**：
//!    - `<prefix>/bin/ryupy` + `<prefix>/runtime/RyuPy.Runtime.dll`（`make build` 的产物）；
//!    - `<prefix>/ryupy` + `<prefix>/runtime/RyuPy.Runtime.dll`（放在同一目录时）；
//! 3. 开发模式默认路径 `<workspace>/runtime/RyuPy.Runtime/bin/Release/net10.0/`
//!    （由 `make runtime` 即 `dotnet build -c Release` 生成）。
//!
//! 发行包必须排在开发路径前面：`workspace_root()` 来自编译期的 `CARGO_MANIFEST_DIR`，
//! 在别的机器上并不存在；相对可执行文件查找才能让打包后的 CLI 真正可搬运。

use std::path::{Path, PathBuf};

use anyhow::{Context, Result};

pub const RUNTIME_DLL: &str = "RyuPy.Runtime.dll";
pub(crate) const DEPENDENCIES: &str = "RuntimeDependencies.props";
const LICENSE: &str = "ThirdPartyNotices.sqlite3.txt";
const MANAGED_DEPENDENCIES: &[&str] = &[
    "Microsoft.Data.Sqlite.dll",
    "SQLitePCLRaw.core.dll",
    "SQLitePCLRaw.batteries_v2.dll",
    "SQLitePCLRaw.provider.e_sqlite3.dll",
];

/// 只分类固定 provider 的确切原生库名，不通过扩展名猜测任意 sidecar 的运行时身份。
pub(crate) fn dependency_role(name: &str) -> Option<&'static str> {
    match name {
        "libe_sqlite3.dylib" | "libe_sqlite3.so" | "e_sqlite3.dll" => {
            Some("runtime_native_dependency")
        }
        name if MANAGED_DEPENDENCIES.contains(&name) => Some("runtime_managed_dependency"),
        DEPENDENCIES => Some("runtime_dependency_contract"),
        LICENSE => Some("runtime_dependency_license"),
        _ => None,
    }
}

pub fn runtime_dir() -> PathBuf {
    if let Some(dir) = std::env::var_os("RYUPY_RUNTIME_DIR") {
        return PathBuf::from(dir);
    }
    if let Some(dir) = packaged_runtime_dir() {
        return dir;
    }
    workspace_root().join("runtime/RyuPy.Runtime/bin/Release/net10.0")
}

/// 可执行文件旁边带运行时的两种布局；都没有时返回 `None`，交给开发路径。
fn packaged_runtime_dir() -> Option<PathBuf> {
    let exe = std::env::current_exe().ok()?;
    let exe_dir = exe.parent()?;
    [exe_dir.join("runtime"), exe_dir.join("..").join("runtime")]
        .into_iter()
        .find(|candidate| candidate.join(RUNTIME_DLL).is_file())
        // 规范化掉 `..`，让错误信息和产物路径更可读；失败时保留原路径即可。
        .map(|candidate| candidate.canonicalize().unwrap_or(candidate))
}

pub fn workspace_root() -> PathBuf {
    // crates/ryupy-driver → 上两级即工作区根目录。
    Path::new(env!("CARGO_MANIFEST_DIR"))
        .ancestors()
        .nth(2)
        .expect("CARGO_MANIFEST_DIR 层级不符合预期")
        .to_path_buf()
}

pub fn copy_runtime_to(out_dir: &Path) -> Result<()> {
    let src = runtime_dir().join(RUNTIME_DLL);
    std::fs::copy(&src, out_dir.join(RUNTIME_DLL)).with_context(|| {
        format!(
            "找不到 {}，请先运行 `make runtime`，或设置 RYUPY_RUNTIME_DIR",
            src.display()
        )
    })?;
    copy_dependencies(&runtime_dir(), out_dir)?;
    Ok(())
}

/// .NET 未提供 deps.json 的生成入口仍从应用目录解析托管 DLL；native 资产选择同宿主 RID。
fn copy_dependencies(source: &Path, output: &Path) -> Result<()> {
    // 先检查完整闭包再复制；明确的缺库错误优于应用启动后才出现 FileNotFoundException。
    anyhow::ensure!(
        source.join(DEPENDENCIES).is_file()
            || !MANAGED_DEPENDENCIES
                .iter()
                .any(|name| source.join(name).exists()),
        "运行时缺少版本合同 {DEPENDENCIES}，请重新构建或安装完整发行包"
    );
    if source.join(DEPENDENCIES).is_file() {
        anyhow::ensure!(
            source.join(LICENSE).is_file(),
            "运行时缺少随包许可证 {LICENSE}，请重新构建或安装完整发行包"
        );
        for name in MANAGED_DEPENDENCIES {
            anyhow::ensure!(
                source.join(name).is_file(),
                "运行时缺少托管依赖 {name}，请重新构建或安装完整发行包"
            );
        }
    }
    let architecture = match std::env::consts::ARCH {
        "aarch64" => "arm64",
        "x86_64" => "x64",
        other => other,
    };
    let platform = match std::env::consts::OS {
        "macos" => "osx",
        "windows" => "win",
        "linux" if cfg!(target_env = "musl") => "linux-musl",
        other => other,
    };
    let native = source.join(format!("runtimes/{platform}-{architecture}/native"));
    if source.join(DEPENDENCIES).is_file() {
        let library = match platform {
            "win" => "e_sqlite3.dll",
            "osx" => "libe_sqlite3.dylib",
            _ => "libe_sqlite3.so",
        };
        anyhow::ensure!(
            native.join(library).is_file(),
            "运行时缺少同宿主原生依赖 {}，请重新构建或安装完整发行包",
            native.join(library).display()
        );
    }
    for entry in std::fs::read_dir(source)? {
        let path = entry?.path();
        if path
            .extension()
            .is_some_and(|extension| extension == "dll" || extension == "props")
            && path.file_name().is_some_and(|name| name != RUNTIME_DLL)
        {
            std::fs::copy(&path, output.join(path.file_name().unwrap()))
                .with_context(|| format!("无法部署运行时依赖 {}", path.display()))?;
        }
    }
    if native.is_dir() {
        for entry in std::fs::read_dir(&native)? {
            let path = entry?.path();
            if path.is_file() {
                std::fs::copy(&path, output.join(path.file_name().unwrap()))?;
            }
        }
    }
    if source.join(LICENSE).is_file() {
        std::fs::copy(source.join(LICENSE), output.join(LICENSE))?;
    }
    Ok(())
}

#[cfg(test)]
#[path = "runtime_tests.rs"]
mod tests;
