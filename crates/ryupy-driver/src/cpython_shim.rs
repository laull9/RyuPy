//! 从编译器内嵌源码构建目标版本shim；不定位源码仓库，计划释放后清理成功临时目录。
use super::native;
use anyhow::{Context, Result, ensure};
use std::path::{Path, PathBuf};
use std::process::Command;
use std::sync::Arc;

pub(crate) struct BuildRoot(PathBuf);
impl Drop for BuildRoot {
    fn drop(&mut self) {
        let _ = std::fs::remove_dir_all(&self.0);
    }
}

pub(super) fn build(
    python: &Path,
    version: &native::version::PythonVersion,
) -> Result<(PathBuf, Arc<BuildRoot>)> {
    let timestamp = std::time::SystemTime::now()
        .duration_since(std::time::UNIX_EPOCH)?
        .as_nanos();
    let directory = std::env::temp_dir().join(format!(
        "ryupy-cpython-shim-{}-{}-{timestamp}",
        version.version_str(),
        std::process::id()
    ));
    #[cfg(unix)]
    let mut builder = std::fs::DirBuilder::new();
    #[cfg(not(unix))]
    let builder = std::fs::DirBuilder::new();
    #[cfg(unix)]
    {
        use std::os::unix::fs::DirBuilderExt;
        builder.mode(0o700);
    }
    builder.create(&directory)?;
    let result = (|| {
        for (name, source) in [
            (
                "ryupy_cpython.c",
                include_str!("../../../runtime/RyuPy.CPython/native/ryupy_cpython.c"),
            ),
            (
                "build.sh",
                include_str!("../../../runtime/RyuPy.CPython/native/build.sh"),
            ),
            (
                "build.ps1",
                include_str!("../../../runtime/RyuPy.CPython/native/build.ps1"),
            ),
        ] {
            std::fs::write(directory.join(name), source)?;
        }
        let mut command = if cfg!(windows) {
            // 直接启动PowerShell，路径不经过cmd.exe的shell元字符解释。
            let mut command = Command::new("powershell.exe");
            command
                .args(["-NoProfile", "-ExecutionPolicy", "Bypass", "-File"])
                .arg(directory.join("build.ps1"))
                .arg("-PythonHome")
                .arg(python)
                .arg("-Output")
                .arg(&directory)
                .arg("-PythonVersion")
                .arg(version.series());
            command
        } else {
            let mut command = Command::new("sh");
            command
                .arg(directory.join("build.sh"))
                .arg(python)
                .arg(&directory)
                .arg(version.series());
            command
        };
        let output = command.output().context("无法启动CPython shim编译工具链")?;
        let log = format!(
            "{}\n{}",
            String::from_utf8_lossy(&output.stdout),
            String::from_utf8_lossy(&output.stderr)
        );
        std::fs::write(directory.join("build.log"), &log)?;
        ensure!(output.status.success(), "编译CPython shim失败：{log}");
        let path = directory.join(native::shim_name());
        ensure!(path.is_file(), "shim产物未生成：{}", path.display());
        Ok(path)
    })();
    match result {
        Ok(path) => Ok((path, Arc::new(BuildRoot(directory)))),
        Err(error) => Err(error.context(format!("CPython shim失败现场：{}", directory.display()))),
    }
}
