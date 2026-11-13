//! SDK 双流直接写入本轮独占文件，不等待子进程退出才保存诊断。
use std::path::{Path, PathBuf};
use std::process::{Command, ExitStatus, Stdio};

use anyhow::{Context, Result};

pub(super) struct SdkOutput {
    pub status: ExitStatus,
    stdout: PathBuf,
    stderr: PathBuf,
}

impl SdkOutput {
    /// 失败诊断按需读取同次原流；读盘失败不能掩盖 SDK 退出状态。
    pub(super) fn diagnostics(&self) -> String {
        fn read(path: &Path) -> String {
            match std::fs::read(path) {
                Ok(bytes) => String::from_utf8_lossy(&bytes).into_owned(),
                Err(error) => format!("无法读取 SDK 日志 {path:?}: {error}"),
            }
        }
        format!(
            "stdout:\n{}\nstderr:\n{}",
            read(&self.stdout),
            read(&self.stderr)
        )
    }
}

pub(super) fn run(
    command: &mut Command,
    root: &Path,
    trace: &crate::stage_trace::StageTrace,
) -> Result<SdkOutput> {
    // create_new 对普通旧文件及链接都拒绝；不覆盖前轮或意外外部目标。
    let stdout = root.join("publish.stdout.log");
    let stderr = root.join("publish.stderr.log");
    let out = std::fs::File::create_new(&stdout)
        .with_context(|| format!("无法创建 SDK 日志 {stdout:?}"))?;
    let err = std::fs::File::create_new(&stderr)
        .with_context(|| format!("无法创建 SDK 日志 {stderr:?}"))?;
    // 旧 Command::output 的 stdin 默认关闭；不能因 status 改成继承交互输入。
    command
        .stdin(Stdio::null())
        .stdout(Stdio::from(out))
        .stderr(Stdio::from(err));
    trace.sdk_logs(&stdout, &stderr);
    trace.mark("begin", "aot-sdk-publish", "-");
    let status = command.status();
    trace.mark(
        if status.as_ref().is_ok_and(ExitStatus::success) {
            "end"
        } else {
            "error"
        },
        "aot-sdk-publish",
        "-",
    );
    let status = status.with_context(|| {
        format!("无法启动或等待 Native AOT dotnet publish（可用 DOTNET 指定 SDK）；日志：{stdout:?}, {stderr:?}")
    })?;
    Ok(SdkOutput {
        status,
        stdout,
        stderr,
    })
}

#[cfg(test)]
#[path = "aot_process_tests.rs"]
mod tests;
