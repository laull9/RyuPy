//! 持有实际打开对象并有界冻结。SDK 只读冻结副本，不跟随链接，也不覆写既有目标。
use super::{INPUT_LIMIT, Platform, images};
use crate::manifest_files::{FileDigest, plain_metadata};
use anyhow::{Context, Result, ensure};
use same_file::Handle;
use std::fs::{File, Metadata, OpenOptions};
use std::io::{Read, Seek, SeekFrom, Write};
use std::path::{Component, Path, PathBuf};
pub(crate) fn checked_relative(root: &Path, text: &str) -> Result<PathBuf> {
    ensure!(text.len() <= 4096, "原生输入路径超出预算");
    crate::manifest_files::safe_relative(text)?;
    let path = root.join(text);
    check_ancestors(&path)?;
    Ok(path)
}
fn check_ancestors(path: &Path) -> Result<()> {
    let mut current = PathBuf::new();
    for part in path.components() {
        ensure!(
            !matches!(part, Component::ParentDir),
            "原生输入拒绝父目录组件"
        );
        current.push(part);
        if matches!(part, Component::Prefix(_)) {
            continue;
        }
        plain_metadata(&current)?;
    }
    Ok(())
}
fn stable(a: &Metadata, b: &Metadata) -> Result<bool> {
    #[cfg(unix)]
    {
        use std::os::unix::fs::MetadataExt;
        if a.dev() != b.dev()
            || a.ino() != b.ino()
            || a.ctime() != b.ctime()
            || a.ctime_nsec() != b.ctime_nsec()
        {
            return Ok(false);
        }
    }
    Ok(a.len() == b.len() && a.modified()? == b.modified()?)
}
fn open(path: &Path) -> Result<Handle> {
    check_ancestors(path)?;
    ensure!(plain_metadata(path)?.is_file(), "原生输入不是普通文件");
    let h = Handle::from_file(File::open(path)?)?;
    ensure!(
        h.as_file().metadata()?.is_file() && h == Handle::from_path(path)?,
        "原生输入打开时变化"
    );
    Ok(h)
}
fn stream(h: &Handle, path: &Path, limit: u64, mut output: Option<&mut File>) -> Result<u64> {
    let before = h.as_file().metadata()?;
    ensure!(before.len() <= limit, "原生输入超出读取预算");
    check_ancestors(path)?;
    ensure!(*h == Handle::from_path(path)?, "原生输入路径身份变化");
    let mut f = h.as_file();
    f.seek(SeekFrom::Start(0))?;
    let mut size = 0u64;
    let mut buffer = [0u8; 65536];
    loop {
        let room = usize::try_from((limit - size).saturating_add(1).min(buffer.len() as u64))?;
        let n = f.read(&mut buffer[..room])?;
        if n == 0 {
            break;
        }
        size = size.checked_add(n as u64).context("原生输入长度溢出")?;
        ensure!(size <= limit, "原生输入读取期间增长/超额");
        if let Some(out) = output.as_mut() {
            out.write_all(&buffer[..n])?;
        }
    }
    ensure!(
        size == before.len() && stable(&before, &h.as_file().metadata()?)?,
        "原生输入读取期间变化"
    );
    check_ancestors(path)?;
    ensure!(*h == Handle::from_path(path)?, "原生输入读取后身份变化");
    Ok(size)
}
pub(crate) fn read_small(path: &Path, limit: u64) -> Result<Vec<u8>> {
    let h = open(path)?;
    let before = h.as_file().metadata()?;
    let size = stream(&h, path, limit, None)?;
    let mut f = h.as_file();
    f.seek(SeekFrom::Start(0))?;
    let mut bytes = Vec::with_capacity(usize::try_from(size)?);
    f.take(size + 1).read_to_end(&mut bytes)?;
    ensure!(
        bytes.len() as u64 == size && stable(&before, &h.as_file().metadata()?)?,
        "声明读取期间变化"
    );
    Ok(bytes)
}
pub(crate) struct NativeInput {
    handle: Handle,
    path: PathBuf,
    pub digest: FileDigest,
    metadata: Metadata,
}
pub(crate) struct FrozenInput {
    pub path: PathBuf,
    pub digest: FileDigest,
    metadata: Metadata,
}
impl NativeInput {
    pub(crate) fn open(path: &Path, platform: Platform, aot: bool) -> Result<Self> {
        let handle = open(path)?;
        let metadata = handle.as_file().metadata()?;
        let size = stream(&handle, path, INPUT_LIMIT, None)?;
        let mut f = handle.as_file();
        if aot {
            images::archive(&mut f, size, platform)?
        } else {
            images::shared(&mut f, size, platform)?
        }
        ensure!(
            stable(&metadata, &handle.as_file().metadata()?)?,
            "原生输入验证期间变化"
        );
        Ok(Self {
            handle,
            path: path.to_owned(),
            metadata,
            digest: FileDigest {
                relative_path: "selected-native-input".into(),
                role: if aot {
                    "native_static_archive"
                } else {
                    "native_shared"
                }
                .into(),
                size,
                sha256: String::new(),
            },
        })
    }
    pub(crate) fn freeze(&self, target: &Path) -> Result<FrozenInput> {
        let mut out = OpenOptions::new()
            .create_new(true)
            .write(true)
            .open(target)?;
        ensure!(
            stable(&self.metadata, &self.handle.as_file().metadata()?)?,
            "原生输入冻结前变化"
        );
        let size = stream(&self.handle, &self.path, self.digest.size, Some(&mut out))?;
        ensure!(size == self.digest.size, "原生输入冻结前变化");
        out.flush()?;
        out.sync_all()?;
        drop(out);
        let frozen = FrozenInput {
            path: target.to_owned(),
            digest: self.digest.clone(),
            metadata: std::fs::metadata(target)?,
        };
        frozen.verify()?;
        Ok(frozen)
    }
}
impl FrozenInput {
    pub(crate) fn verify(&self) -> Result<()> {
        let h = open(&self.path)?;
        let size = h.as_file().metadata()?.len();
        ensure!(
            size == self.digest.size && stable(&self.metadata, &h.as_file().metadata()?)?,
            "原生冻结副本变化"
        );
        Ok(())
    }
}
