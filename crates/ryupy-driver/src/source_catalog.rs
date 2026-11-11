//! 编译器自有的锁源目录；令牌仅由固定字节、许可与最终部署位置的联合复核签发。
//! 此目录不参与普通搜索根，不按用户模块名授予实例能力，也不执行任何Python。
use crate::modules::SourceKind;
#[path = "source_catalog_archive.rs"]
mod archive;
use anyhow::{Context, Result, ensure};
use std::path::{Path, PathBuf};

struct Entry {
    name: &'static str,
    path: &'static str,
    digest: &'static str,
    kind: SourceKind,
}
const ENTRIES: &[Entry] = &[
    Entry {
        name: "_markupbase",
        path: "_markupbase.py",
        digest: "cb14dd6f2e2439eb70b806cd49d19911363d424c2b6b9f4b73c9c08022d47030",
        kind: SourceKind::ModuleFile,
    },
    Entry {
        name: "html",
        path: "html/__init__.py",
        digest: "923d82d821e75e8d235392c10c145ab8587927b3faf9c952bbd48081eebd8522",
        kind: SourceKind::Package,
    },
    Entry {
        name: "html.entities",
        path: "html/entities.py",
        digest: "d9c65fb2828dbc1f3e399058a341d51e9375ec5bca95a8e92599c41bd5b78bde",
        kind: SourceKind::ModuleFile,
    },
    Entry {
        name: "html.parser",
        path: "html/parser.py",
        digest: "952f1d7dd5be98b6f78801c6904a38e3e2beceb4783efb76c5e22948f0a58092",
        kind: SourceKind::ModuleFile,
    },
];
const LICENSE_DIGEST: &str = "3b2f81fe21d181c499c59a256c8e1968455d6689d269aa85373bfb6af41da3bf";

/// 私有字段防止名字、路径或相同用户字节自行创建编译器能力。
#[derive(Debug, Clone)]
pub(crate) struct Token {
    index: usize,
    deployed: PathBuf,
}
impl Token {
    pub(crate) fn authenticates(&self, path: &Path, bytes: &[u8]) -> bool {
        path == self.deployed
            && crate::manifest_files::hash_bytes(bytes) == ENTRIES[self.index].digest
    }
}

/// 单组目录以全部成员与许可内容寻址；尚未实际使用的成员也不能改变族身份。
fn family_digest() -> Result<String> {
    let mut identity = Vec::new();
    for entry in ENTRIES {
        ensure!(
            crate::manifest_files::hash_bytes(archive::member(entry.path)?) == entry.digest,
            "catalog源码完整性损坏：{}",
            entry.name
        );
        identity.extend_from_slice(entry.path.as_bytes());
        identity.push(0);
        identity.extend_from_slice(entry.digest.as_bytes());
    }
    ensure!(
        crate::manifest_files::hash_bytes(archive::member("LICENSE.txt")?) == LICENSE_DIGEST,
        "catalog许可完整性损坏"
    );
    identity.extend_from_slice(LICENSE_DIGEST.as_bytes());
    Ok(crate::manifest_files::hash_bytes(&identity))
}

/// 只部署实际所需成员；部署位置确定后才允许调用方加入SourceDb。
pub(crate) fn resolve(out: &Path, name: &str) -> Result<Option<(PathBuf, SourceKind, Token)>> {
    let Some(index) = ENTRIES.iter().position(|entry| entry.name == name) else {
        return Ok(None);
    };
    let root = absolute(out)?
        .join("compiler-sources")
        .join(family_digest()?);
    let entry = &ENTRIES[index];
    let path = root.join(entry.path);
    crate::source_deployment::materialize(&path, archive::member(entry.path)?)?;
    crate::source_deployment::materialize(
        &root.join("LICENSE.txt"),
        archive::member("LICENSE.txt")?,
    )?;
    crate::source_deployment::materialize(
        &root.join("manifest.json"),
        archive::member("manifest.json")?,
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

#[cfg(test)]
mod tests {
    use super::*;
    #[test]
    fn default_source_embedded_catalog_and_license_are_locked() {
        assert_eq!(family_digest().unwrap().len(), 64);
        assert_eq!(ENTRIES.len(), 4);
        for entry in ENTRIES {
            assert!(std::str::from_utf8(archive::member(entry.path).unwrap()).is_ok());
        }
    }
}

/// 普通native父包不是独立catalog触发目标；此角色事实来自声明表。
pub(crate) fn direct_request(name: &str) -> bool {
    ENTRIES
        .iter()
        .any(|entry| entry.name == name && entry.kind != SourceKind::Package)
}
