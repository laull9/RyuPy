//! 可选的构建期依赖闭包：固定解释器只运行静态收集工具，结果进入既有provider计划。
use super::{Payload, absolute, relative};
use anyhow::{Context, Result, ensure};
use serde::Deserialize;
use std::path::{Path, PathBuf};
use std::process::{Command, Stdio};

#[derive(Clone, Deserialize)]
#[serde(deny_unknown_fields)]
pub(super) struct Declaration {
    /// 显式安装环境根；支持venv及普通前缀，不在运行时保留环境搜索。
    environment: PathBuf,
    /// 静态入口看不到的插件、动态导入与额外需求，使用PEP508语法。
    #[serde(default)]
    requirements: Vec<String>,
    #[serde(default)]
    dynamic_modules: Vec<String>,
}
#[derive(Default, Deserialize)]
pub(super) struct Closure {
    pub modules: Vec<String>,
    pub payloads: Vec<Payload>,
    pub report: serde_json::Value,
}
pub(super) fn collect(
    declaration: Declaration,
    base: &Path,
    root: &Path,
    entry: &Path,
    provided: &[String],
    stdlib: &std::collections::BTreeSet<String>,
) -> Result<Closure> {
    let environment = absolute(base, &declaration.environment)?
        .canonicalize()
        .context("依赖构建环境不存在，请准备固定CPython环境")?;
    let executable = if cfg!(windows) {
        root.join("python.exe")
    } else {
        root.join("bin/python3.12")
    };
    let request = serde_json::json!({
        "environment": environment, "entry": entry.canonicalize()?,
        "requirements": declaration.requirements, "dynamic_modules": declaration.dynamic_modules,
        "stdlib": stdlib, "provided_modules": provided,
    });
    // -I -S 禁止用户site、.pth与环境变量注入；收集器从不导入应用包。
    let mut child = Command::new(executable)
        .args([
            "-I",
            "-S",
            "-B",
            "-c",
            include_str!("cpython_dependencies.py"),
        ])
        .stdin(Stdio::piped())
        .stdout(Stdio::piped())
        .stderr(Stdio::piped())
        .spawn()
        .context("无法启动固定发行的构建期依赖收集器")?;
    use std::io::Write;
    child
        .stdin
        .take()
        .context("缺少收集器输入管道")?
        .write_all(&serde_json::to_vec(&request)?)?;
    let output = child.wait_with_output()?;
    ensure!(
        output.status.success(),
        "CPython依赖收集失败：{}",
        String::from_utf8_lossy(&output.stderr)
    );
    let closure: Closure =
        serde_json::from_slice(&output.stdout).context("依赖收集器输出格式错误")?;
    for payload in &closure.payloads {
        relative(&payload.destination)?;
        ensure!(
            payload.source.starts_with(&environment),
            "依赖源越过构建环境：{}",
            payload.source.display()
        );
    }
    Ok(closure)
}
