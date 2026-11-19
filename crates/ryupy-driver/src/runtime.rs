//! 从当前编译器内嵌字节准备C#运行时；显式RYUPY_RUNTIME_DIR保留开发覆盖入口。
//! 默认不读取源码仓库或可执行文件旁的旁置目录，完整模块组来自同次编译器构建。

use std::path::{Path, PathBuf};

use anyhow::{Context, Result};

pub(crate) use crate::runtime_assets::DEPENDENCIES;
pub use crate::runtime_assets::RUNTIME_DLL;
use crate::runtime_assets::{LICENSE, MANAGED_DEPENDENCIES};

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

pub fn runtime_dir() -> Result<PathBuf> {
    if let Some(dir) = std::env::var_os("RYUPY_RUNTIME_DIR") {
        return Ok(PathBuf::from(dir));
    }
    crate::embedded_runtime::prepare()
}

/// 内嵌桥与Runtime同组；不接受可执行文件旁的未登记旁置程序集。
pub(crate) fn cpython_bridge_dir() -> Result<PathBuf> {
    let runtime = runtime_dir()?;
    anyhow::ensure!(
        runtime.join(crate::runtime_assets::BRIDGE_DLL).is_file(),
        "运行时缺少RyuPy.CPython.dll：{}",
        runtime.display()
    );
    Ok(runtime)
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
    copy_runtime_subset(&runtime_dir()?, out_dir, false)
}

/// 显式组件从同一构建输出根部署完整托管/原生依赖闭包，避免混用两个开发bin来源。
pub(crate) fn copy_runtime_from(source: &Path, out_dir: &Path) -> Result<()> {
    copy_runtime_subset(source, out_dir, true)
}

fn copy_runtime_subset(source: &Path, out_dir: &Path, bridge: bool) -> Result<()> {
    let src = source.join(RUNTIME_DLL);
    std::fs::copy(&src, out_dir.join(RUNTIME_DLL)).with_context(|| {
        format!(
            "运行时组件缺失：{}；请检查显式RYUPY_RUNTIME_DIR或重新安装编译器",
            src.display()
        )
    })?;
    copy_dependencies_selected(source, out_dir, bridge)?;
    Ok(())
}

/// .NET 未提供 deps.json 的生成入口仍从应用目录解析托管 DLL；native 资产选择同宿主 RID。
#[cfg(test)]
fn copy_dependencies(source: &Path, output: &Path) -> Result<()> {
    copy_dependencies_selected(source, output, false)
}

fn copy_dependencies_selected(source: &Path, output: &Path, bridge: bool) -> Result<()> {
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
        if !bridge
            && path
                .file_name()
                .is_some_and(|name| name == crate::runtime_assets::BRIDGE_DLL)
        {
            continue;
        }
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
