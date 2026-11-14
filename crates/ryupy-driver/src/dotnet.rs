//! 调用 .NET SDK。
//!
//! 先用子进程方式（`dotnet x.dll`）跑通，简单可靠；`netcorehost` 进程内托管留到确有性能需要时再引入。

use std::io::Write;
use std::path::{Path, PathBuf};
use std::process::{Command, Output, Stdio};

use anyhow::{Context, Result};
#[path = "dotnet_discovery.rs"]
mod discovery;

/// `dotnet` 可执行文件，可用环境变量 `DOTNET` 覆盖。
pub fn dotnet_exe() -> PathBuf {
    discovery::executable()
}

/// 原生发布开始前确认已安装可用的.NET SDK。
pub fn require_sdk() -> Result<()> {
    discovery::require_sdk()
}
/// 托管应用运行前确认已安装目标.NET Runtime。
pub fn require_runtime() -> Result<()> {
    discovery::require_runtime()
}

/// 运行生成程序集时的进程环境。
///
/// e2e 的目录用例需要传命令行参数、环境变量、工作目录与标准输入；这些都只影响**进程**，
/// 不影响编译，所以单独定义而不放进 [`crate::BuildOptions`]。
#[derive(Debug, Default, Clone)]
pub struct RunOptions {
    pub args: Vec<String>,
    pub env: Vec<(String, String)>,
    pub cwd: Option<PathBuf>,
    /// 写入子进程 stdin 的完整内容（None 表示不重定向）。
    pub stdin: Option<String>,
}

/// 用共享框架运行一个已生成的程序集，捕获输出。
pub fn run_dll(dll: &Path) -> Result<Output> {
    run_dll_with(dll, &RunOptions::default())
}

/// 带参数 / 环境 / 工作目录 / 标准输入地运行程序集。
pub fn run_dll_with(dll: &Path, opts: &RunOptions) -> Result<Output> {
    let mut cmd = Command::new(dotnet_exe());
    cmd.arg(dll).args(&opts.args);
    if let Some(cwd) = &opts.cwd {
        cmd.current_dir(cwd);
    }
    for (k, v) in &opts.env {
        cmd.env(k, v);
    }
    if opts.stdin.is_some() {
        cmd.stdin(Stdio::piped());
    }
    cmd.stdout(Stdio::piped()).stderr(Stdio::piped());

    let mut child = cmd
        .spawn()
        .with_context(|| format!("无法启动 dotnet 运行 {}", dll.display()))?;
    if let Some(text) = &opts.stdin {
        if let Some(mut stdin) = child.stdin.take() {
            // 子进程提前退出时写入会失败，此时以子进程输出为准。
            let _ = stdin.write_all(text.as_bytes());
        }
    }
    child
        .wait_with_output()
        .with_context(|| format!("等待 dotnet 进程失败 {}", dll.display()))
}

/// Native AOT 发布：生成薄宿主并返回本次独立 publish 目录内的真实可执行文件。
/// 宿主直接返回生成的 int Main(string[])，失败不返回或复用旧原生产物。
pub fn publish_aot(dll: &Path, out_dir: &Path) -> Result<PathBuf> {
    crate::aot::publish(dll, out_dir)
}
