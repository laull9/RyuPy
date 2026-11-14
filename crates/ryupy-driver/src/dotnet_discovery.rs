//! 按显式覆盖、PATH、架构环境变量与系统安装位置发现.NET；只检查本次操作所需能力。
use anyhow::{Context, Result, ensure};
use std::path::{Path, PathBuf};
use std::process::Command;

fn in_path(name: &Path) -> Option<PathBuf> {
    std::env::var_os("PATH").and_then(|path| {
        std::env::split_paths(&path)
            .map(|directory| directory.join(name))
            .find(|candidate| candidate.is_file())
    })
}

pub(super) fn executable() -> PathBuf {
    if let Some(path) = std::env::var_os("DOTNET") {
        let path = PathBuf::from(path);
        return path
            .canonicalize()
            .ok()
            .or_else(|| in_path(&path))
            .unwrap_or(path);
    }
    let name = Path::new(if cfg!(windows) {
        "dotnet.exe"
    } else {
        "dotnet"
    });
    if let Some(path) = in_path(name) {
        return path.canonicalize().unwrap_or(path);
    }
    let architecture = if cfg!(target_arch = "aarch64") {
        "DOTNET_ROOT_ARM64"
    } else {
        "DOTNET_ROOT_X64"
    };
    let mut roots: Vec<PathBuf> = [architecture, "DOTNET_ROOT"]
        .into_iter()
        .filter_map(std::env::var_os)
        .map(PathBuf::from)
        .collect();
    if let Some(home) = std::env::var_os(if cfg!(windows) { "USERPROFILE" } else { "HOME" }) {
        roots.push(PathBuf::from(home).join(".dotnet"));
    }
    if cfg!(windows) {
        if let Some(programs) = std::env::var_os("ProgramFiles") {
            roots.push(PathBuf::from(programs).join("dotnet"));
        }
    } else {
        roots.extend(
            [
                "/usr/share/dotnet",
                "/usr/lib/dotnet",
                "/usr/local/share/dotnet",
                "/opt/homebrew/share/dotnet",
            ]
            .into_iter()
            .map(PathBuf::from),
        );
    }
    roots
        .into_iter()
        .map(|root| root.join(name))
        .find(|path| path.is_file())
        .unwrap_or_else(|| name.to_owned())
}

fn inspect(argument: &str, need: &str) -> Result<String> {
    let executable = executable();
    let output = Command::new(&executable)
        .arg(argument)
        .output()
        .with_context(|| {
            format!("未找到可用.NET宿主，{need}；请安装.NET 10或用DOTNET/DOTNET_ROOT指定安装位置")
        })?;
    ensure!(
        output.status.success(),
        ".NET环境探测失败（{}）：{}",
        executable.display(),
        String::from_utf8_lossy(&output.stderr)
    );
    Ok(String::from_utf8_lossy(&output.stdout).into_owned())
}

pub(super) fn require_sdk() -> Result<()> {
    let versions = inspect("--list-sdks", "原生发布需要.NET SDK 10及以上")?;
    ensure!(
        versions.lines().any(|line| line
            .split_whitespace()
            .next()
            .is_some_and(|version| !version.contains('-')
                && version
                    .split('.')
                    .next()
                    .and_then(|major| major.parse::<u32>().ok())
                    .is_some_and(|major| major >= 10))),
        "原生发布需要.NET SDK 10及以上；当前检测到：{}",
        versions.trim()
    );
    Ok(())
}

pub(super) fn require_runtime() -> Result<()> {
    let versions = inspect("--list-runtimes", "运行托管产物需要.NET Runtime 10")?;
    ensure!(
        versions.lines().any(|line| {
            let mut words = line.split_whitespace();
            words.next() == Some("Microsoft.NETCore.App")
                && words
                    .next()
                    .is_some_and(|version| version.starts_with("10.") && !version.contains('-'))
        }),
        "运行托管产物需要.NET Runtime 10；当前检测到：{}",
        versions.trim()
    );
    Ok(())
}

#[cfg(test)]
mod tests {
    use super::*;

    #[test]
    fn dotnet_root_discovers_actual_sdk_with_empty_path() {
        if std::env::var_os("RYUPY_DOTNET_ROOT_PROBE").is_some() {
            require_sdk().unwrap();
            return;
        }
        let dotnet = executable()
            .canonicalize()
            .expect("测试构建机需要真实.NET SDK");
        let output = Command::new(std::env::current_exe().unwrap())
            .args([
                "--exact",
                "dotnet::discovery::tests::dotnet_root_discovers_actual_sdk_with_empty_path",
                "--nocapture",
            ])
            .env("RYUPY_DOTNET_ROOT_PROBE", "1")
            .env("PATH", "")
            .env_remove("DOTNET")
            .env_remove("DOTNET_ROOT_X64")
            .env_remove("DOTNET_ROOT_ARM64")
            .env("DOTNET_ROOT", dotnet.parent().unwrap())
            .output()
            .unwrap();
        assert!(
            output.status.success(),
            "真实SDK根目录兜底失败：{}\n{}",
            String::from_utf8_lossy(&output.stdout),
            String::from_utf8_lossy(&output.stderr)
        );
        assert!(
            String::from_utf8_lossy(&output.stdout).contains("1 passed"),
            "SDK子进程必须实际运行指定测试"
        );
    }
}
