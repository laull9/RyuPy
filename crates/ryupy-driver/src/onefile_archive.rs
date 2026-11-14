//! onefile 私有有界归档写入器；拒绝链接、平台歧义名和非普通文件，按路径排序。
use anyhow::{Context, Result, ensure};
use std::collections::HashSet;
use std::fs::{File, Metadata};
use std::io::{Read, Write};
use std::path::{Path, PathBuf};
const MAX_TOTAL: u64 = 16 * 1024 * 1024 * 1024;
const MAX_FILE: u64 = 4 * 1024 * 1024 * 1024;

pub(super) fn write(root: &Path, executable: &Path, output: &Path) -> Result<()> {
    ensure!(plain(root)?.is_dir(), "onefile 根必须为普通目录");
    let entry = executable
        .strip_prefix(root)
        .context("onefile 入口必须位于 dist 内")?;
    let entry = portable(entry)?;
    let mut nodes = Vec::new();
    collect(root, root, &mut nodes, &mut HashSet::new())?;
    nodes.sort_by(|a, b| a.0.cmp(&b.0));
    ensure!(
        nodes
            .iter()
            .any(|(name, _, meta)| name == &entry && meta.is_file()),
        "onefile 入口不是载荷普通文件"
    );
    let mut out = File::create(output)?;
    out.write_all(b"RYUPYOF1")?;
    string(&mut out, &entry)?;
    out.write_all(&(nodes.len() as u32).to_le_bytes())?;
    let mut total = 0_u64;
    for (name, path, metadata) in nodes {
        let directory = metadata.is_dir();
        let length = if directory { 0 } else { metadata.len() };
        total = total.checked_add(length).context("onefile 总大小溢出")?;
        ensure!(
            length <= MAX_FILE && total <= MAX_TOTAL,
            "onefile 载荷大小超出限制"
        );
        string(&mut out, &name)?;
        out.write_all(&[u8::from(!directory)])?;
        #[cfg(unix)]
        let mode = {
            use std::os::unix::fs::PermissionsExt;
            metadata.permissions().mode() & 0o777
        };
        #[cfg(not(unix))]
        let mode = if directory { 0o700_u32 } else { 0o600_u32 };
        out.write_all(&mode.to_le_bytes())?;
        out.write_all(&length.to_le_bytes())?;
        if !directory {
            let before = plain(&path)?;
            ensure!(
                before.is_file() && before.len() == length,
                "onefile 输入在打包期间变化"
            );
            let input = File::open(&path)?;
            // 多读一个字节即可检测增长，禁止并发写入把有界归档拖成无界流。
            let copied = std::io::copy(&mut input.take(length + 1), &mut out)?;
            ensure!(
                copied == length && plain(&path)?.len() == length,
                "onefile 输入大小在读取期间变化"
            );
        }
    }
    out.write_all(b"ENDOF001")?;
    Ok(())
}

fn collect(
    root: &Path,
    dir: &Path,
    nodes: &mut Vec<(String, PathBuf, Metadata)>,
    seen: &mut HashSet<String>,
) -> Result<()> {
    for child in std::fs::read_dir(dir)? {
        let path = child?.path();
        let meta = plain(&path)?;
        ensure!(
            meta.is_file() || meta.is_dir(),
            "onefile 拒绝特殊文件 {}",
            path.display()
        );
        let name = portable(path.strip_prefix(root)?)?;
        ensure!(
            seen.insert(name.to_lowercase()),
            "onefile 路径大小写重复：{name}"
        );
        ensure!(nodes.len() < 100_000, "onefile 文件数超过限制");
        nodes.push((name, path.clone(), meta));
        if nodes.last().is_some_and(|n| n.2.is_dir()) {
            collect(root, &path, nodes, seen)?;
        }
    }
    Ok(())
}

fn plain(path: &Path) -> Result<Metadata> {
    let meta = std::fs::symlink_metadata(path)?;
    ensure!(
        !meta.file_type().is_symlink(),
        "onefile 拒绝符号链接 {}",
        path.display()
    );
    #[cfg(windows)]
    {
        use std::os::windows::fs::MetadataExt;
        ensure!(
            meta.file_attributes() & 0x400 == 0,
            "onefile 拒绝 reparse point"
        );
    }
    Ok(meta)
}

fn portable(path: &Path) -> Result<String> {
    let name = path
        .to_str()
        .context("onefile 路径须为 UTF-8")?
        .replace(std::path::MAIN_SEPARATOR, "/");
    ensure!(
        !name.is_empty() && name.len() <= 4096,
        "onefile 路径长度无效"
    );
    for part in name.split('/') {
        ensure!(
            !part.is_empty() && part != "." && part != ".." && !part.ends_with(['.', ' ']),
            "onefile 路径组件无效"
        );
        ensure!(
            !part
                .chars()
                .any(|c| c.is_control() || "\\:*?\"<>|".contains(c)),
            "onefile 路径含平台歧义字符"
        );
        let base = part
            .split('.')
            .next()
            .unwrap_or_default()
            .to_ascii_uppercase();
        ensure!(
            !matches!(base.as_str(), "CON" | "PRN" | "AUX" | "NUL")
                && !(base.len() == 4
                    && (base.starts_with("COM") || base.starts_with("LPT"))
                    && matches!(base.as_bytes()[3], b'1'..=b'9')),
            "onefile 路径含 Windows 保留名"
        );
    }
    Ok(name)
}
fn string(out: &mut File, value: &str) -> Result<()> {
    out.write_all(&(value.len() as u32).to_le_bytes())?;
    out.write_all(value.as_bytes())?;
    Ok(())
}

#[cfg(test)]
mod tests {
    use super::*;
    #[test]
    fn rejects_ambiguous_names() {
        for name in [
            "../x", "/x", "a//b", "a/./b", "a\\b", "CON.txt", "x:", "a. ", "LPT1",
        ] {
            assert!(portable(Path::new(name)).is_err(), "{name}");
        }
        assert_eq!(
            portable(Path::new("中文 空格/a.txt")).unwrap(),
            "中文 空格/a.txt"
        );
    }
}
