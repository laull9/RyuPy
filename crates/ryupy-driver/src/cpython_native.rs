//! CPython 原生分发的平台身份与 macOS 搬迁：只允许发行内依赖及明确系统库，拒绝开发机绝对引用。
use anyhow::{Context, Result, bail, ensure};
use std::path::{Path, PathBuf};
use std::process::Command;
pub(super) fn shim_name() -> &'static str {
    match std::env::consts::OS {
        "macos" => "libryupy_cpython.dylib",
        "windows" => "ryupy_cpython.dll",
        _ => "libryupy_cpython.so",
    }
}
pub(super) fn library_name() -> &'static str {
    match std::env::consts::OS {
        "macos" => "libpython3.12.dylib",
        "windows" => "python312.dll",
        _ => "libpython3.12.so.1.0",
    }
}
pub(super) fn rid() -> Result<&'static str> {
    match (std::env::consts::OS, std::env::consts::ARCH) {
        ("macos", "aarch64") => Ok("osx-arm64"),
        _ => bail!("CPython provider 当前只接受已冻结的 macOS arm64 发行，其他目标待独立验收"),
    }
}
pub(super) fn system_dependencies() -> &'static [&'static str] {
    &["/usr/lib/", "/System/Library/Frameworks/"]
}
pub(super) fn preflight(root: &Path) -> Result<()> {
    rid()?;
    let library = root.join("lib").join(library_name());
    ensure!(
        library.is_file(),
        "CPython 发行缺少 libpython：{}",
        library.display()
    );
    let output = Command::new("lipo")
        .arg("-archs")
        .arg(&library)
        .output()
        .context("无法运行 lipo 检查 CPython 架构")?;
    ensure!(
        output.status.success()
            && String::from_utf8_lossy(&output.stdout)
                .split_whitespace()
                .any(|arch| arch == "arm64"),
        "CPython libpython 架构不是 arm64"
    );
    Ok(())
}
fn run(program: &str, args: &[&std::ffi::OsStr]) -> Result<()> {
    let output = Command::new(program)
        .args(args)
        .output()
        .with_context(|| format!("无法启动 {program}"))?;
    ensure!(
        output.status.success(),
        "{program} 失败：{}",
        String::from_utf8_lossy(&output.stderr)
    );
    Ok(())
}
fn images(root: &Path, found: &mut Vec<PathBuf>) -> Result<()> {
    for entry in std::fs::read_dir(root)? {
        let path = entry?.path();
        if path.is_dir() {
            images(&path, found)?;
        } else if path
            .extension()
            .is_some_and(|ext| ext == "dylib" || ext == "so")
        {
            found.push(path);
        }
    }
    Ok(())
}
/// PBS 安装工具可能改过 libpython ID。逐映像改为相对 loader 的发行内引用并重新签名。
pub(super) fn relocate(output: &Path) -> Result<()> {
    rid()?;
    let mut all = Vec::new();
    images(&output.join("python"), &mut all)?;
    all.push(output.join(shim_name()));
    for image in &all {
        let report = Command::new("otool").arg("-L").arg(image).output()?;
        ensure!(
            report.status.success(),
            "CPython 原生依赖无法读取：{}",
            image.display()
        );
        let text = String::from_utf8(report.stdout)?;
        for line in text.lines().skip(1) {
            let Some(dependency) = line.trim().split(" (compatibility version ").next() else {
                continue;
            };
            if dependency.is_empty()
                || system_dependencies()
                    .iter()
                    .any(|prefix| dependency.starts_with(prefix))
            {
                continue;
            }
            let name = Path::new(dependency)
                .file_name()
                .context("Mach-O 依赖缺少文件名")?;
            let matches: Vec<_> = all
                .iter()
                .filter(|candidate| candidate.file_name() == Some(name))
                .collect();
            ensure!(
                matches.len() == 1,
                "CPython 原生依赖未唯一收集：{} → {dependency}",
                image.display()
            );
            let target = matches[0];
            if target == image {
                continue;
            }
            let relative = relative_path(image.parent().context("原生映像缺少父目录")?, target)?;
            let replacement = format!("@loader_path/{relative}");
            if dependency != replacement {
                run(
                    "install_name_tool",
                    &[
                        "-change".as_ref(),
                        dependency.as_ref(),
                        replacement.as_ref(),
                        image.as_os_str(),
                    ],
                )?;
            }
        }
        if image
            .extension()
            .is_some_and(|extension| extension == "dylib")
        {
            let id = format!("@rpath/{}", image.file_name().unwrap().to_string_lossy());
            run(
                "install_name_tool",
                &["-id".as_ref(), id.as_ref(), image.as_os_str()],
            )?;
        }
        run(
            "codesign",
            &[
                "--force".as_ref(),
                "--sign".as_ref(),
                "-".as_ref(),
                image.as_os_str(),
            ],
        )?;
    }
    Ok(())
}
fn relative_path(base: &Path, target: &Path) -> Result<String> {
    let base: Vec<_> = base.components().collect();
    let target: Vec<_> = target.components().collect();
    let shared = base
        .iter()
        .zip(&target)
        .take_while(|(left, right)| left == right)
        .count();
    ensure!(shared > 0, "原生依赖不属于同一文件系统根");
    let mut result = PathBuf::new();
    for _ in shared..base.len() {
        result.push("..");
    }
    for component in &target[shared..] {
        result.push(component.as_os_str());
    }
    Ok(result.to_str().context("原生库相对路径不是 UTF-8")?.into())
}
