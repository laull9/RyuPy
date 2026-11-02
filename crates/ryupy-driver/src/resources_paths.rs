//! 资源首切的 ASCII portable 目标策略；绝对宿主路径不套此规则，不把 ASCII fold 冒充 Unicode/文件系统等价。
use crate::manifest_files::plain_metadata;
use anyhow::{Context, Result, ensure};
use std::collections::BTreeMap;
use std::path::Path;

const MAX_PATH: usize = 1024;
const MAX_COMPONENT: usize = 255;
#[derive(Clone, Copy, Debug, PartialEq, Eq)]
pub(super) enum Kind {
    Directory,
    Anchor,
    Resource,
    Reserved,
    Published,
}
#[derive(Default)]
pub(super) struct Targets {
    entries: BTreeMap<String, (String, Kind)>,
}

pub(super) fn portable(text: &str) -> Result<()> {
    ensure!(
        !text.is_empty() && text.len() <= MAX_PATH && text.is_ascii(),
        "资源相对目标首切仅支持有界 ASCII，不能自动改名"
    );
    for component in text.split('/') {
        ensure!(
            !component.is_empty()
                && component.len() <= MAX_COMPONENT
                && component != "."
                && component != "..",
            "资源路径包含空/相对/过长分量"
        );
        ensure!(
            !component.ends_with(['.', ' '])
                && !component
                    .bytes()
                    .any(|b| b.is_ascii_control() || b"\\:<>\"|?*".contains(&b)),
            "资源路径不符合 portable 文件名规则"
        );
        let stem = component
            .split('.')
            .next()
            .unwrap_or("")
            .trim_end_matches(' ')
            .to_ascii_uppercase();
        ensure!(
            !matches!(
                stem.as_str(),
                "CON" | "PRN" | "AUX" | "NUL" | "CONIN$" | "CONOUT$" | "CLOCK$"
            ) && !["COM", "LPT"].iter().any(|prefix| stem
                .strip_prefix(prefix)
                .is_some_and(|s| s.len() == 1 && matches!(s.as_bytes()[0], b'1'..=b'9'))),
            "资源路径包含 Windows 保留设备名"
        );
    }
    Ok(())
}

impl Targets {
    pub(super) fn insert(&mut self, path: &str, kind: Kind) -> Result<()> {
        portable(path)?;
        let mut prefix = String::new();
        let parts: Vec<_> = path.split('/').collect();
        for (index, part) in parts.iter().enumerate() {
            if index != 0 {
                prefix.push('/');
            }
            prefix.push_str(part);
            self.component(
                &prefix,
                if index + 1 == parts.len() {
                    kind
                } else {
                    Kind::Directory
                },
            )?;
        }
        Ok(())
    }
    fn component(&mut self, path: &str, kind: Kind) -> Result<()> {
        // 输入已限 ASCII，fold 不膨胀；原 spelling 保留，用于拒绝不同大小写路径冲突。
        let key = path.to_ascii_lowercase();
        if let Some((original, existing)) = self.entries.get(&key) {
            ensure!(original == path, "资源目标大小写冲突，拒绝改名合并");
            ensure!(
                *existing == kind
                    && matches!(kind, Kind::Directory | Kind::Anchor | Kind::Resource),
                "资源文件/目录/模块锚点/发布保留项冲突"
            );
        } else {
            self.entries.insert(key, (path.to_owned(), kind));
        }
        Ok(())
    }
    pub(super) fn include_publish(&mut self, root: &Path) -> Result<()> {
        ensure!(plain_metadata(root)?.is_dir(), "资源发布根不是普通目录");
        let mut visited = 0;
        self.walk(root, root, 0, &mut visited)
    }
    fn walk(
        &mut self,
        root: &Path,
        directory: &Path,
        depth: usize,
        visited: &mut usize,
    ) -> Result<()> {
        ensure!(depth <= 64, "发布目录深度超出预算");
        for entry in std::fs::read_dir(directory)? {
            ensure!(*visited < 100_000, "发布项数量超出预算");
            *visited += 1;
            let path = entry?.path();
            let logical = path
                .strip_prefix(root)?
                .iter()
                .map(|part| part.to_str().context("发布名不是 UTF-8"))
                .collect::<Result<Vec<_>>>()?
                .join("/");
            let metadata = plain_metadata(&path)?;
            self.insert(
                &logical,
                if metadata.is_dir() {
                    Kind::Directory
                } else {
                    Kind::Published
                },
            )?;
            if metadata.is_dir() {
                self.walk(root, &path, depth + 1, visited)?;
            }
        }
        Ok(())
    }
}

/// 声明根先解析为真实目录；根以下每段检查，不以 canonicalize 掩盖条目链接。
pub(super) fn checked_source(root: &Path, relative: &str) -> Result<std::path::PathBuf> {
    portable(relative)?;
    ensure!(plain_metadata(root)?.is_dir(), "资源声明根不是普通目录");
    let mut path = root.to_owned();
    let parts: Vec<_> = relative.split('/').collect();
    for (index, component) in parts.iter().enumerate() {
        path.push(component);
        let metadata = plain_metadata(&path)?;
        ensure!(
            if index + 1 == parts.len() {
                metadata.is_file()
            } else {
                metadata.is_dir()
            },
            "资源不是普通文件或祖先不是普通目录"
        );
    }
    Ok(path)
}

#[cfg(test)]
#[path = "resources_paths_tests.rs"]
mod tests;
