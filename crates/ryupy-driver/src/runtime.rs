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
    Ok(())
}
