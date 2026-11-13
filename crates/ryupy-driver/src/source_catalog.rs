//! 版本化编译器源码目录；令牌绑定内嵌原字节与最终部署位置，不按模块名授权。
//! 此目录不参与普通搜索根，不按用户模块名授予实例能力，也不执行任何Python。
use crate::modules::SourceKind;
#[path = "source_catalog_archive.rs"]
mod archive;
use anyhow::{Context, Result};
use std::path::{Path, PathBuf};

struct Entry {
    name: &'static str,
    path: &'static str,
    kind: SourceKind,
}
const ENTRIES: &[Entry] = &[
    Entry {
        name: "_markupbase",
        path: "_markupbase.py",
        kind: SourceKind::ModuleFile,
    },
    Entry {
        name: "html",
        path: "html/__init__.py",
        kind: SourceKind::Package,
    },
    Entry {
        name: "html.entities",
        path: "html/entities.py",
        kind: SourceKind::ModuleFile,
    },
    Entry {
        name: "html.parser",
        path: "html/parser.py",
        kind: SourceKind::ModuleFile,
    },
    Entry {
        name: "colorsys",
        path: "colorsys.py",
        kind: SourceKind::ModuleFile,
    },
];

pub(crate) const VERSION: &str = "cpython-3.12.13-catalog-2";

/// 已验来源与仅探测来源分别登记；后者永远不能取得编译能力令牌。
pub(crate) fn source(name: &str) -> Result<Option<&'static [u8]>> {
    if name == "colorsys" {
        return Ok(Some(include_bytes!(
            "../catalog/cpython-3.12.13/colorsys.py"
        )));
    }
    if name == "queue" {
        return Ok(Some(include_bytes!("../catalog/cpython-3.12.13/queue.py")));
    }
    ENTRIES
        .iter()
        .find(|entry| entry.name == name)
        .map(|entry| archive::member(entry.path))
        .transpose()
}

pub(crate) fn facts() -> Result<serde_json::Value> {
    Ok(serde_json::from_str(include_str!(
        "../catalog/facts-v2.json"
    ))?)
}

/// 私有字段防止名字、路径或相同用户字节自行创建编译器能力。
#[derive(Debug, Clone)]
pub(crate) struct Token {
    index: usize,
    deployed: PathBuf,
}
impl Token {
    pub(crate) fn authenticates(&self, path: &Path, bytes: &[u8]) -> bool {
        path == self.deployed && source(ENTRIES[self.index].name).ok().flatten() == Some(bytes)
    }
}

/// 只部署实际所需成员；部署位置确定后才允许调用方加入SourceDb。
pub(crate) fn resolve(out: &Path, name: &str) -> Result<Option<(PathBuf, SourceKind, Token)>> {
    let Some(index) = ENTRIES.iter().position(|entry| entry.name == name) else {
        return Ok(None);
    };
    let root = absolute(out)?.join("compiler-sources").join(VERSION);
    let entry = &ENTRIES[index];
    let path = root.join(entry.path);
    crate::source_deployment::materialize(&path, source(name)?.context("catalog源码缺失")?)?;
    crate::source_deployment::materialize(
        &root.join("LICENSE.txt"),
        archive::member("LICENSE.txt")?,
    )?;
    crate::source_deployment::materialize(
        &root.join("legacy-manifest.json"),
        archive::member("manifest.json")?,
    )?;
    crate::source_deployment::materialize(
        &root.join("manifest.json"),
        include_bytes!("../catalog/facts-v2.json"),
    )?;
    let token = Token {
        index,
        deployed: path.clone(),
    };
    Ok(Some((path, entry.kind, token)))
}
fn absolute(path: &Path) -> Result<PathBuf> {
    if path.is_absolute() {
        Ok(path.to_owned())
    } else {
        Ok(std::env::current_dir()
            .context("读取当前目录失败")?
            .join(path))
    }
}

/// 普通native父包不是独立catalog触发目标；此角色事实来自声明表。
pub(crate) fn direct_request(name: &str) -> bool {
    ENTRIES
        .iter()
        .any(|entry| entry.name == name && entry.kind != SourceKind::Package)
}

#[cfg(test)]
mod tests {
    use super::*;
    #[test]
    fn default_source_embedded_catalog_and_license_are_locked() {
        assert_eq!(ENTRIES.len(), 5);
        for entry in ENTRIES {
            assert!(std::str::from_utf8(source(entry.name).unwrap().unwrap()).is_ok());
            assert_ne!(
                facts().unwrap()["modules"][entry.name]["admission"],
                "detected_only"
            );
        }
        assert_eq!(facts().unwrap()["catalog_version"], VERSION);
        for name in facts().unwrap()["modules"].as_object().unwrap().keys() {
            assert!(source(name).unwrap().is_some(), "缺少已登记原源：{name}");
        }
        assert!(!direct_request("queue"));
    }
}
