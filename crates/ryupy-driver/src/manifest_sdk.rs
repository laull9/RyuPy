//! 只解析本次 MSBuild 写出的 SDK 单行版本；不另跑 dotnet --version，也不收集环境/日志。
use crate::manifest_files::plain_metadata;
use anyhow::{Context, Result, ensure};
use std::io::Read;
use std::path::Path;

pub(crate) fn read_version(path: &Path) -> Result<String> {
    let metadata = plain_metadata(path)?;
    ensure!(
        metadata.is_file() && metadata.len() <= 256,
        "SDK 版本记录不是有界普通文件"
    );
    let mut bytes = Vec::new();
    std::fs::File::open(path)?
        .take(257)
        .read_to_end(&mut bytes)?;
    ensure!(bytes.len() <= 256, "SDK 版本记录过大");
    parse_version(std::str::from_utf8(&bytes).context("SDK 版本记录不是 UTF-8")?)
}

pub(crate) fn parse_version(text: &str) -> Result<String> {
    let text = text.strip_prefix('\u{feff}').unwrap_or(text);
    let version = text
        .strip_suffix("\r\n")
        .or_else(|| text.strip_suffix('\n'))
        .unwrap_or(text);
    ensure!(
        !version.is_empty()
            && version.len() <= 128
            && version
                .bytes()
                .all(|c| c.is_ascii_alphanumeric() || matches!(c, b'.' | b'-')),
        "SDK 版本必须恰好一行合法版本号"
    );
    let (core, prerelease) = version
        .split_once('-')
        .map_or((version, None), |(a, b)| (a, Some(b)));
    let pieces: Vec<_> = core.split('.').collect();
    ensure!(
        pieces.len() == 3
            && pieces
                .iter()
                .all(|s| !s.is_empty() && s.bytes().all(|b| b.is_ascii_digit())),
        "SDK 版本缺少三段数字主体"
    );
    if let Some(pre) = prerelease {
        ensure!(
            !pre.is_empty() && pre.split('.').all(|part| !part.is_empty()),
            "SDK 预览版本后缀无效"
        );
    }
    Ok(version.to_owned())
}
