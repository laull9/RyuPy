//! 有界、确定的显式资源树展开；结果仍交给原claims与stage冻结，不打开第二条部署链。
use super::paths;
use anyhow::{Context, Result, ensure};
use serde::Deserialize;
use std::collections::BTreeSet;
use std::path::{Path, PathBuf};

pub(super) const SCAN_LIMIT: usize = 100_000;
const DEPTH_LIMIT: usize = 64;
const FILE_COUNT_LIMIT: usize = 1024;

#[derive(Clone, Deserialize)]
#[serde(deny_unknown_fields)]
pub(super) struct TreeRequest {
    directory: String,
    suffixes: Vec<String>,
}

pub(super) struct Selection {
    root: PathBuf,
    request: TreeRequest,
    pub(super) files: Vec<String>,
}
impl Selection {
    pub(super) fn read(root: &Path, request: &TreeRequest, budget: &mut usize) -> Result<Self> {
        Ok(Self {
            root: root.to_owned(),
            request: request.clone(),
            files: expand(root, request, budget)?,
        })
    }
    pub(super) fn verify(&self, budget: &mut usize) -> Result<()> {
        ensure!(
            expand(&self.root, &self.request, budget)? == self.files,
            "资源目录选择在构建期间变化，请重新构建"
        );
        Ok(())
    }
}

fn expand(root: &Path, request: &TreeRequest, budget: &mut usize) -> Result<Vec<String>> {
    if request.directory != "." {
        paths::portable(&request.directory)?;
    }
    ensure!(
        !request.suffixes.is_empty() && request.suffixes.len() <= 64,
        "资源后缀数须为1至64"
    );
    let mut suffixes = BTreeSet::new();
    for suffix in &request.suffixes {
        ensure!(
            suffix.starts_with('.')
                && suffix.len() > 1
                && suffix.len() <= 255
                && suffix
                    .bytes()
                    .all(|b| b.is_ascii_alphanumeric() || b == b'.' || b == b'_' || b == b'-')
                && !suffix.ends_with('.'),
            "资源后缀须为以点开头的非空ASCII显式后缀，不能使用glob"
        );
        ensure!(!python_file(suffix), "资源后缀不能选择Python源码/字节码");
        ensure!(suffixes.insert(suffix), "资源后缀重复");
    }
    // 逐级检查，绝不借canonicalize悄悄跟随目录链接。
    let mut directory = root.to_owned();
    ensure!(
        crate::manifest_files::plain_metadata(&directory)?.is_dir(),
        "资源根不是目录"
    );
    if request.directory != "." {
        for component in request.directory.split('/') {
            directory.push(component);
            ensure!(
                crate::manifest_files::plain_metadata(&directory)?.is_dir(),
                "资源树不是目录"
            );
        }
    }
    let prefix = if request.directory == "." {
        ""
    } else {
        &request.directory
    };
    let mut files = BTreeSet::new();
    walk(root, prefix, request, budget, 0, &mut files)?;
    ensure!(!files.is_empty(), "资源目录选择未匹配任何文件");
    Ok(files.into_iter().collect())
}
fn python_file(file: &str) -> bool {
    let lower = file.to_ascii_lowercase();
    [".py", ".pyc", ".pyo"]
        .iter()
        .any(|suffix| lower.ends_with(suffix))
}
fn walk(
    root: &Path,
    relative: &str,
    request: &TreeRequest,
    budget: &mut usize,
    depth: usize,
    files: &mut BTreeSet<String>,
) -> Result<()> {
    ensure!(depth <= DEPTH_LIMIT, "资源目录深度超过64层");
    let directory = root.join(relative);
    ensure!(
        crate::manifest_files::plain_metadata(&directory)?.is_dir(),
        "资源树不是目录"
    );
    let mut entries = Vec::new();
    for entry in std::fs::read_dir(&directory).context("无法扫描资源目录")? {
        ensure!(*budget > 0, "资源目录扫描超过100000项");
        *budget -= 1;
        let name = entry?
            .file_name()
            .into_string()
            .map_err(|_| anyhow::anyhow!("资源路径不是ASCII"))?;
        let file = if relative.is_empty() {
            name
        } else {
            format!("{relative}/{name}")
        };
        paths::portable(&file)?;
        entries.push(file);
    }
    entries.sort();
    for file in entries {
        let metadata = crate::manifest_files::plain_metadata(&root.join(&file))?;
        if metadata.is_dir() {
            walk(root, &file, request, budget, depth + 1, files)?;
        } else if request.suffixes.iter().any(|suffix| file.ends_with(suffix)) {
            ensure!(!python_file(&file), "资源不能部署Python源码/字节码");
            files.insert(file);
            ensure!(files.len() <= FILE_COUNT_LIMIT, "资源目录选择超过1024项");
        }
    }
    Ok(())
}

#[cfg(test)]
#[path = "resources_select_tests.rs"]
mod tests;
