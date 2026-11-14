//! 最终JIT源码部署：拒绝链接/特殊对象，已存在文件只复核，不覆盖用户内容。
//! 这是可信串行构建的冷路径，不声称阻止恶意并发目录替换。
use anyhow::{Result, ensure};
use std::io::Write;
use std::path::{Component, Path};

fn directory(path: &Path) -> Result<()> {
    ensure!(
        !path
            .components()
            .any(|part| matches!(part, Component::ParentDir)),
        "catalog部署路径不接受..组件"
    );
    if let Some(parent) = path
        .parent()
        .filter(|parent| !parent.as_os_str().is_empty())
    {
        directory(parent)?;
    }
    match std::fs::symlink_metadata(path) {
        Ok(metadata) => ensure!(
            metadata.is_dir() && !metadata.file_type().is_symlink(),
            "catalog目录不是普通目录：{}",
            path.display()
        ),
        Err(error) if error.kind() == std::io::ErrorKind::NotFound => std::fs::create_dir(path)?,
        Err(error) => return Err(error.into()),
    }
    Ok(())
}
pub(crate) fn materialize(path: &Path, bytes: &[u8]) -> Result<()> {
    directory(
        path.parent()
            .ok_or_else(|| anyhow::anyhow!("catalog部署缺少父目录"))?,
    )?;
    match std::fs::OpenOptions::new()
        .write(true)
        .create_new(true)
        .open(path)
    {
        Ok(mut output) => {
            output.write_all(bytes)?;
            output.sync_all()?;
        }
        Err(error) if error.kind() == std::io::ErrorKind::AlreadyExists => {}
        Err(error) => return Err(error.into()),
    }
    let actual = read(path, bytes.len())?;
    ensure!(
        actual == bytes,
        "catalog部署内容与锁源不同：{}",
        path.display()
    );
    Ok(())
}
/// 最终位置再次复核后将同一批字节交给SourceDb，不能重新走未认证的普通读取。
pub(crate) fn read(path: &Path, limit: usize) -> Result<Vec<u8>> {
    crate::distribution_metadata::read_source_bytes(path, limit)
}
