//! 定位并复制 `RyuPy.Runtime.dll`。
//!
//! 查找顺序：
//! 1. 环境变量 `RYUPY_RUNTIME_DIR`（指向包含 `RyuPy.Runtime.dll` 的目录）；
//! 2. 开发模式默认路径 `<workspace>/runtime/RyuPy.Runtime/bin/Release/net10.0/`
//!    （由 `make runtime` 即 `dotnet build -c Release` 生成）。

use std::path::{Path, PathBuf};

use anyhow::{Context, Result};

pub const RUNTIME_DLL: &str = "RyuPy.Runtime.dll";

pub fn runtime_dir() -> PathBuf {
    if let Some(dir) = std::env::var_os("RYUPY_RUNTIME_DIR") {
        return PathBuf::from(dir);
    }
    workspace_root().join("runtime/RyuPy.Runtime/bin/Release/net10.0")
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
