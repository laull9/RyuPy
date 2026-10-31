//! 调用 .NET SDK。
//!
//! 先用子进程方式（`dotnet x.dll`）跑通，简单可靠；`netcorehost` 进程内托管留到确有性能需要时再引入。

use std::path::{Path, PathBuf};
use std::process::{Command, Output};

use anyhow::{Context, Result};

/// `dotnet` 可执行文件，可用环境变量 `DOTNET` 覆盖。
pub fn dotnet_exe() -> PathBuf {
    std::env::var_os("DOTNET")
        .map(PathBuf::from)
        .unwrap_or_else(|| PathBuf::from("dotnet"))
}

/// 用共享框架运行一个已生成的程序集，捕获输出。
pub fn run_dll(dll: &Path) -> Result<Output> {
    Command::new(dotnet_exe())
        .arg(dll)
        .output()
        .with_context(|| format!("无法启动 dotnet 运行 {}", dll.display()))
}

/// Native AOT 发布。
///
/// 计划方案（ADR-0005）：ILCompiler 只接受「项目」作为输入，所以在产物目录下生成一个桩项目：
///
/// ```text
/// <out>/aot/
///   RyuPyHost.csproj   // PublishAot=true，<Reference> 指向 <name>.dll 与 RyuPy.Runtime.dll
///   Program.cs         // static void Main(string[] a) => RyuPy.Generated.Module.Main(a);
/// ```
///
/// 然后执行 `dotnet publish -c Release -r <rid>`，返回生成的原生可执行文件路径。
pub fn publish_aot(_dll: &Path, _out_dir: &Path) -> Result<PathBuf> {
    anyhow::bail!("Native AOT 尚未实现（M5.2，见 TODO.md）")
}
