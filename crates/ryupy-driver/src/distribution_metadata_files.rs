//! 分发快照的有界普通文件读取；不跟链接、不沿RECORD寻找安装文件。
use super::EnvironmentUnavailable;
use anyhow::{Context, Result};
use serde::Serialize;
use std::fs::{File, Metadata};
use std::io::Read;
use std::path::{Component, Path, PathBuf};
#[derive(Default)]
pub(super) struct Budget {
    bytes: usize,
    files: usize,
}
#[derive(Serialize, PartialEq, Debug)]
pub(super) struct Record {
    pub path: String,
    size: usize,
    sha256: String,
    pub hex: String,
}
pub(super) fn plain_path(path: &Path) -> Result<()> {
    for component in path.components() {
        environment_ensure!(
            !matches!(component, Component::ParentDir),
            "分发路径不接受..越根组件"
        );
    }
    for ancestor in path.ancestors().filter(|p| !p.as_os_str().is_empty()) {
        let meta = std::fs::symlink_metadata(ancestor)?;
        environment_ensure!(!meta.file_type().is_symlink(), "分发路径不接受链接");
        environment_ensure!(meta.is_dir() || meta.is_file(), "分发路径不接受特殊文件");
    }
    Ok(())
}
// 仅列出已核OS/架构ABI；Linux两种架构的NOFOLLOW数值并不相同。
// 常量与libc 0.2.191的linux gnu/musl及bsd声明核对，不新增外部依赖。
fn open_no_follow(path: &Path) -> Result<File> {
    #[cfg(all(
        any(target_os = "linux", target_os = "macos"),
        any(target_arch = "x86_64", target_arch = "aarch64")
    ))]
    {
        use std::os::unix::fs::OpenOptionsExt;
        #[cfg(all(target_os = "linux", target_arch = "x86_64"))]
        const FLAGS: i32 = 0x20000 | 0x800;
        #[cfg(all(target_os = "linux", target_arch = "aarch64"))]
        const FLAGS: i32 = 0x8000 | 0x800;
        #[cfg(target_os = "macos")]
        const FLAGS: i32 = 0x100 | 0x4;
        // NONBLOCK避免检查后末端被替换成FIFO时阻塞；句柄类型仍必须检查。
        Ok(std::fs::OpenOptions::new()
            .read(true)
            .custom_flags(FLAGS)
            .open(path)?)
    }
    #[cfg(not(all(
        any(target_os = "linux", target_os = "macos"),
        any(target_arch = "x86_64", target_arch = "aarch64")
    )))]
    {
        let _ = path;
        anyhow::bail!("此平台未实现分发元数据末端nofollow打开，拒绝该选项")
    }
}
fn unchanged(before: &Metadata, after: &Metadata) -> Result<()> {
    #[cfg(unix)]
    {
        use std::os::unix::fs::MetadataExt;
        environment_ensure!(
            before.dev() == after.dev() && before.ino() == after.ino(),
            "分发文件身份在打开或读取期间改变"
        );
    }
    environment_ensure!(
        before.len() == after.len() && before.modified()? == after.modified()?,
        "分发文件长度或时间在读取期间改变"
    );
    Ok(())
}
// before来自本次lstat；helper保留为私有，测试能直接验证检查后末端替换。
fn open_checked(path: &Path, before: &Metadata, limit: usize) -> Result<File> {
    environment_ensure!(
        before.is_file() && before.len() <= limit as u64,
        "分发文件类型或尺寸超限"
    );
    let file = open_no_follow(path)?;
    let opened = file.metadata()?;
    environment_ensure!(
        opened.is_file() && opened.len() <= limit as u64,
        "打开后的分发对象不是有界普通文件"
    );
    unchanged(before, &opened)?;
    Ok(file)
}
pub(super) fn read(path: &Path, limit: usize) -> Result<Vec<u8>> {
    plain_path(path)?;
    let before = std::fs::symlink_metadata(path)?;
    let mut file = open_checked(path, &before, limit)?;
    let mut bytes = Vec::new();
    file.by_ref()
        .take(limit as u64 + 1)
        .read_to_end(&mut bytes)?;
    environment_ensure!(
        bytes.len() <= limit && bytes.len() as u64 == before.len(),
        "分发文件读取期间截短或增长"
    );
    unchanged(&before, &file.metadata()?)?;
    plain_path(path)?;
    let after = std::fs::symlink_metadata(path)?;
    environment_ensure!(after.is_file(), "分发路径在读取后不再是普通文件");
    unchanged(&before, &after)?;
    Ok(bytes)
}
pub(super) fn children(path: &Path) -> Result<Vec<(String, PathBuf)>> {
    let mut found = Vec::new();
    for entry in std::fs::read_dir(path)? {
        let entry = entry?;
        let name = entry
            .file_name()
            .into_string()
            .map_err(|_| anyhow::anyhow!("分发路径不是UTF-8"))?;
        crate::manifest_files::safe_relative(&name)?;
        // 根也不能藏链接/特殊文件再被静默忽略；普通源码目录无需递归。
        plain_path(&entry.path())?;
        environment_ensure!(found.len() < 4096, "分发目录直接子项超过4096");
        found.push((name, entry.path()));
    }
    found.sort_by(|a, b| a.0.cmp(&b.0));
    Ok(found)
}
pub(super) fn record(path: &Path, name: &str, budget: &mut Budget) -> Result<Record> {
    let bytes = read(path, 1024 * 1024)?;
    budget.files += 1;
    budget.bytes += bytes.len();
    environment_ensure!(
        budget.files <= 2048 && budget.bytes <= 1024 * 1024,
        "完整分发快照超过2048文件或1MiB"
    );
    let hex: String = bytes.iter().map(|b| format!("{b:02x}")).collect();
    Ok(Record {
        path: name.to_owned(),
        size: bytes.len(),
        sha256: crate::manifest_files::hash_bytes(&bytes),
        hex,
    })
}
pub(super) fn walk(
    root: &Path,
    relative: &str,
    output: &mut Vec<Record>,
    directories: &mut Vec<String>,
    budget: &mut Budget,
    depth: usize,
) -> Result<()> {
    environment_ensure!(depth <= 16, "分发目录深度超过16");
    for (name, path) in children(&root.join(relative))? {
        let relative = if relative.is_empty() {
            name
        } else {
            format!("{relative}/{name}")
        };
        if path.is_dir() {
            environment_ensure!(directories.len() < 2048, "分发子目录超过2048");
            directories.push(relative.clone());
            walk(root, &relative, output, directories, budget, depth + 1)?;
        } else {
            output.push(record(&path, &relative, budget)?);
        }
    }
    Ok(())
}
pub(super) fn unhex(hex: &str) -> Result<Vec<u8>> {
    hex.as_bytes()
        .chunks_exact(2)
        .map(|part| {
            u8::from_str_radix(std::str::from_utf8(part).context("hex编码无效")?, 16)
                .context("hex编码无效")
        })
        .collect()
}

#[cfg(all(
    test,
    any(target_os = "linux", target_os = "macos"),
    any(target_arch = "x86_64", target_arch = "aarch64")
))]
mod tests {
    use super::*;
    struct Tree(crate::test_temp::Root);
    impl Tree {
        fn new() -> Self {
            Self(crate::test_temp::Root::new("metadata-open"))
        }
    }
    #[test]
    fn distribution_metadata_open_rejects_final_link_and_replaced_inode() {
        let tree = Tree::new();
        let path = tree.0.join("metadata");
        let retained = tree.0.join("retained");
        std::fs::write(&path, b"same").unwrap();
        let before = std::fs::symlink_metadata(&path).unwrap();
        std::fs::rename(&path, &retained).unwrap();
        // 若普通open跟随此链接，句柄会与before同inode；此例专门证明末端NOFOLLOW。
        std::os::unix::fs::symlink(&retained, &path).unwrap();
        assert!(open_checked(&path, &before, 4).is_err());
        std::fs::remove_file(&path).unwrap();
        // 原inode保持存活，新普通文件内容完全相同也不能冒充原对象。
        std::fs::write(&path, b"same").unwrap();
        assert!(open_checked(&path, &before, 4).is_err());
        assert_eq!(read(&path, 4).unwrap(), b"same");
    }
    #[test]
    fn distribution_metadata_open_checks_handle_type_and_budget() {
        let tree = Tree::new();
        let path = tree.0.join("metadata");
        std::fs::write(&path, b"data").unwrap();
        let before = std::fs::symlink_metadata(&path).unwrap();
        assert!(open_checked(&path, &before, 3).is_err());
        std::fs::rename(&path, tree.0.join("retained")).unwrap();
        std::fs::create_dir(&path).unwrap();
        assert!(open_checked(&path, &before, 4).is_err());
    }
}
