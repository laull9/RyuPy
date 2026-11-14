//! 本次独占 publish 树的安全枚举与文件大小登记；普通文件跳过摘要，不跟随链接。
use anyhow::{Context, Result, bail, ensure};
use serde::Serialize;
use std::fs::{File, Metadata, OpenOptions};
use std::io::Write;
use std::path::{Path, PathBuf};

pub(crate) const MANIFEST_NAME: &str = "ryupy-artifact.json";
const TEMP_NAME: &str = ".ryupy-artifact.json.tmp";
const MAX_FILES: usize = 100_000;
const MAX_DEPTH: usize = 64;

#[derive(Debug, Clone, Serialize, PartialEq, Eq)]
pub(crate) struct FileDigest {
    pub relative_path: String,
    pub role: String,
    pub size: u64,
    pub sha256: String,
}

// 历史报告保留字段形状；普通源码/构建产物按项目约定不计算摘要。
pub(crate) fn hash_bytes(_bytes: &[u8]) -> String {
    String::new()
}

/// 使用平台无关的保守相对名称，拒绝 Windows 盘符/ADS 以及 Unix 上可出现的反斜线。
pub(crate) fn safe_relative(text: &str) -> Result<()> {
    ensure!(
        !text.is_empty() && !text.contains(['\\', ':']),
        "清单路径不是安全相对路径"
    );
    for part in text.split('/') {
        ensure!(
            !part.is_empty() && part != "." && part != "..",
            "清单路径含空/相对组件"
        );
        ensure!(!part.chars().any(char::is_control), "清单路径含控制字符");
    }
    ensure!(!Path::new(text).is_absolute(), "清单路径不能是绝对路径");
    Ok(())
}

pub(crate) fn plain_metadata(path: &Path) -> Result<Metadata> {
    let metadata = std::fs::symlink_metadata(path).context("无法读取清单文件元数据")?;
    ensure!(!metadata.file_type().is_symlink(), "清单拒绝符号链接");
    #[cfg(windows)]
    {
        use std::os::windows::fs::MetadataExt;
        ensure!(
            metadata.file_attributes() & 0x400 == 0,
            "清单拒绝 Windows reparse point"
        );
    }
    ensure!(
        metadata.is_file() || metadata.is_dir(),
        "清单拒绝非普通文件/目录"
    );
    Ok(metadata)
}

pub(crate) fn digest_file(path: &Path, logical: &str, role: &str) -> Result<FileDigest> {
    safe_relative(logical)?;
    let before = plain_metadata(path)?;
    ensure!(before.is_file(), "登记对象不是普通文件");
    let file = File::open(path).context("无法打开本次清单文件")?;
    let opened = file.metadata()?;
    ensure!(
        opened.is_file() && unchanged(&before, &opened)?,
        "清单文件在打开时变化"
    );
    let size = opened.len();
    let after = plain_metadata(path)?;
    ensure!(
        size == before.len()
            && unchanged(&before, &after)?
            && unchanged(&opened, &file.metadata()?)?,
        "清单文件在登记期间变化"
    );
    Ok(FileDigest {
        relative_path: logical.to_owned(),
        role: role.to_owned(),
        size,
        sha256: String::new(),
    })
}

fn unchanged(a: &Metadata, b: &Metadata) -> Result<bool> {
    #[cfg(unix)]
    {
        use std::os::unix::fs::MetadataExt;
        if a.dev() != b.dev() || a.ino() != b.ino() {
            return Ok(false);
        }
    }
    Ok(a.len() == b.len() && a.modified()? == b.modified()?)
}

/// 校验整个 owned 树后排序；只把传入的实际原生文件标 native，其他不猜运行时必需性。
pub(crate) fn collect(publish: &Path, native: &Path) -> Result<Vec<FileDigest>> {
    ensure!(plain_metadata(publish)?.is_dir(), "publish 不是普通目录");
    ensure_absent(&publish.join(MANIFEST_NAME))?;
    ensure_absent(&publish.join(TEMP_NAME))?;
    let mut files = Vec::new();
    let mut visited = 0;
    walk(publish, publish, native, 0, &mut visited, &mut files)?;
    ensure!(
        files.iter().any(|item| item.role == "native_executable"),
        "清单缺本次原生产物"
    );
    files.sort_by(|a, b| a.relative_path.cmp(&b.relative_path));
    Ok(files)
}

fn walk(
    root: &Path,
    directory: &Path,
    native: &Path,
    depth: usize,
    visited: &mut usize,
    files: &mut Vec<FileDigest>,
) -> Result<()> {
    ensure!(depth <= MAX_DEPTH, "publish 目录深度超出清单预算");
    for item in std::fs::read_dir(directory)? {
        ensure!(*visited < MAX_FILES, "publish 文件/目录总数超出清单预算");
        *visited += 1;
        let path = item?.path();
        let relative = path
            .strip_prefix(root)
            .context("清单文件不属于本次 publish")?;
        let components = relative
            .iter()
            .map(|s| s.to_str().context("清单文件名不是 UTF-8"))
            .collect::<Result<Vec<_>>>()?;
        let logical = components.join("/");
        safe_relative(&logical)?;
        let metadata = plain_metadata(&path)?;
        if metadata.is_dir() {
            walk(root, &path, native, depth + 1, visited, files)?;
        } else {
            ensure!(files.len() < MAX_FILES, "publish 文件数超出清单预算");
            let role = if path == native {
                "native_executable"
            } else {
                crate::runtime::dependency_role(&logical).unwrap_or("unclassified_sidecar")
            };
            files.push(digest_file(&path, &logical, role)?);
        }
    }
    Ok(())
}

/// 不包含自身摘要。独占临时文件关闭后同目录 rename；失败由 owned 发布流程统一清理。
pub(crate) fn write_atomic(publish: &Path, bytes: &[u8]) -> Result<PathBuf> {
    ensure!(plain_metadata(publish)?.is_dir(), "清单目录无效");
    let target = publish.join(MANIFEST_NAME);
    let temporary = publish.join(TEMP_NAME);
    ensure_absent(&target)?;
    let mut file = OpenOptions::new()
        .write(true)
        .create_new(true)
        .open(&temporary)
        .context("无法独占创建清单临时文件")?;
    file.write_all(bytes)?;
    file.flush()?;
    file.sync_all()?;
    drop(file);
    // 此目录由本次发布独占。不是对恶意并发替换提供无竞态沙箱。
    ensure_absent(&target)?;
    std::fs::rename(&temporary, &target).context("无法原子发布清单")?;
    Ok(target)
}

/// 不把权限错误或悬空链接当作不存在，写前与 rename 前都使用同一规则。
pub(crate) fn ensure_absent(path: &Path) -> Result<()> {
    match std::fs::symlink_metadata(path) {
        Ok(_) => bail!("本次清单/SDK记录目标已存在，拒绝覆盖"),
        Err(error) if error.kind() == std::io::ErrorKind::NotFound => Ok(()),
        Err(error) => Err(error).context("无法确认本次目标不存在"),
    }
}
