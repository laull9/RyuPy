//! 将已经闭包验收的 dist 树封装为同宿主 NativeAOT 单文件；不重新解析 provider 布局。
//! 成功清理仓库外工作树；失败保留 SDK 日志并在诊断中提供绝对路径。
use anyhow::{Context, Result, bail, ensure};
use std::path::{Path, PathBuf};
use std::process::Command;
#[path = "onefile_archive.rs"]
mod archive;

/// 输出必须尚不存在且位于载荷树外；调用方负责高层发布事务及命名。
/// 原 dist 保持不动，整个树（含许可证、资源、原生扩展）进入唯一原生文件。
pub fn package(dist_root: &Path, native_executable: &Path, output: &Path) -> Result<PathBuf> {
    let dist = dist_root.canonicalize().context("onefile dist 不存在")?;
    let executable = native_executable
        .canonicalize()
        .context("onefile 内层入口不存在")?;
    let parent = output
        .parent()
        .filter(|p| !p.as_os_str().is_empty())
        .unwrap_or(Path::new("."));
    let output = parent
        .canonicalize()?
        .join(output.file_name().context("onefile 缺输出文件名")?);
    ensure!(!output.starts_with(&dist), "onefile 输出不能位于载荷树内");
    ensure!(
        std::fs::symlink_metadata(&output).is_err_and(|e| e.kind() == std::io::ErrorKind::NotFound),
        "onefile 输出已经存在或不可访问"
    );
    let root = work_root()?;
    let result = build(&dist, &executable, &output, &root);
    match result {
        Ok(path) => {
            std::fs::remove_dir_all(&root).context("onefile 已发布但工作目录清理失败")?;
            Ok(path)
        }
        Err(error) => {
            Err(error.context(format!("onefile 构建失败，诊断保留于 {}", root.display())))
        }
    }
}

fn work_root() -> Result<PathBuf> {
    use std::sync::atomic::{AtomicU64, Ordering};
    static NEXT: AtomicU64 = AtomicU64::new(0);
    let time = std::time::SystemTime::now()
        .duration_since(std::time::UNIX_EPOCH)?
        .as_nanos();
    for _ in 0..64 {
        let root = std::env::temp_dir().join(format!(
            "ryupy-onefile-{}-{time}-{}",
            std::process::id(),
            NEXT.fetch_add(1, Ordering::Relaxed)
        ));
        let mut builder = std::fs::DirBuilder::new();
        #[cfg(unix)]
        {
            use std::os::unix::fs::DirBuilderExt;
            builder.mode(0o700);
        }
        match builder.create(&root) {
            Ok(()) => return Ok(root),
            Err(e) if e.kind() == std::io::ErrorKind::AlreadyExists => continue,
            Err(e) => return Err(e.into()),
        }
    }
    bail!("无法创建 onefile 独占工作目录")
}

fn build(dist: &Path, executable: &Path, output: &Path, root: &Path) -> Result<PathBuf> {
    let rid = match (std::env::consts::OS, std::env::consts::ARCH) {
        ("macos", "aarch64") => "osx-arm64",
        ("linux", "x86_64") if cfg!(target_env = "gnu") => "linux-x64",
        ("windows", "x86_64") if cfg!(target_env = "msvc") => "win-x64",
        _ => bail!("onefile 只支持 macOS arm64、Linux glibc x64、Windows MSVC x64 同宿主发布"),
    };
    archive::write(dist, executable, &root.join("payload.raw"))?;
    std::fs::write(
        root.join("Payload.cs"),
        include_str!("../../../runtime/RyuPy.OneFile/Payload.cs"),
    )?;
    std::fs::write(
        root.join("DecodedPayloadStream.cs"),
        include_str!("../../../runtime/RyuPy.OneFile/DecodedPayloadStream.cs"),
    )?;
    std::fs::write(
        root.join("Pack.cs"),
        include_str!("../../../runtime/RyuPy.OneFile/Pack.cs"),
    )?;
    std::fs::write(
        root.join("Program.cs"),
        include_str!("../../../runtime/RyuPy.OneFile/Program.cs"),
    )?;
    std::fs::write(
        root.join("Pack.csproj"),
        include_str!("../../../runtime/RyuPy.OneFile/Pack.csproj"),
    )?;
    std::fs::write(
        root.join("Launcher.csproj"),
        include_str!("../../../runtime/RyuPy.OneFile/Launcher.csproj"),
    )?;
    sdk(
        root,
        "pack",
        &["run", "--project", "Pack.csproj", "-c", "Release"],
    )?;
    std::fs::remove_file(root.join("payload.raw"))?;
    sdk(
        root,
        "publish",
        &[
            "publish",
            "Launcher.csproj",
            "-c",
            "Release",
            "-r",
            rid,
            "--self-contained",
            "true",
            "-o",
            "publish",
        ],
    )?;
    let name = if cfg!(windows) {
        "RyuPyOneFile.exe"
    } else {
        "RyuPyOneFile"
    };
    let source = root.join("publish").join(name);
    ensure!(
        std::fs::metadata(&source)?.len() > 0,
        "SDK 未生成 onefile 原生文件"
    );
    // create_new 拒绝覆盖并发输出及符号链接；失败删除本次拥有的输出。
    let mut target = std::fs::OpenOptions::new()
        .write(true)
        .create_new(true)
        .open(output)?;
    let copied = (|| -> Result<()> {
        std::io::copy(&mut std::fs::File::open(&source)?, &mut target)?;
        target.set_permissions(std::fs::metadata(&source)?.permissions())?;
        target.sync_all()?;
        Ok(())
    })();
    drop(target);
    if let Err(error) = copied {
        let _ = std::fs::remove_file(output);
        return Err(error);
    }
    Ok(output.to_owned())
}

fn sdk(root: &Path, stage: &str, args: &[&str]) -> Result<()> {
    let output = Command::new(crate::dotnet::dotnet_exe())
        .args(args)
        .args([
            "-p:ImportDirectoryBuildProps=false",
            "-p:ImportDirectoryBuildTargets=false",
        ])
        .current_dir(root)
        .output()
        .context("无法启动 onefile .NET SDK")?;
    std::fs::write(root.join(format!("{stage}.stdout.log")), &output.stdout)?;
    std::fs::write(root.join(format!("{stage}.stderr.log")), &output.stderr)?;
    ensure!(
        output.status.success(),
        "onefile SDK {stage} 失败（{}）\n{}\n{}",
        output.status,
        String::from_utf8_lossy(&output.stdout),
        String::from_utf8_lossy(&output.stderr)
    );
    Ok(())
}

#[cfg(test)]
#[path = "onefile_tests.rs"]
mod tests;
