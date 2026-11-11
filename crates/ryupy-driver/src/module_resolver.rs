//! 普通包来源选择：首个顶层身份固定后，子模块只能沿该普通父包目录查找。
//! 不把后续root的同名子级补进已选父包，也不把普通模块当namespace包。
use crate::modules::SourceKind;
use std::path::{Path, PathBuf};

pub(super) fn resolve(roots: &[PathBuf], name: &str) -> Option<(PathBuf, SourceKind)> {
    let mut parts = name.split('.');
    let top = parts.next()?;
    if top.is_empty() {
        return None;
    }
    let mut selected = roots.iter().find_map(|root| child(root, top))?;
    for part in parts {
        if selected.1 != SourceKind::Package || part.is_empty() {
            return None;
        }
        selected = child(selected.0.parent()?, part)?;
    }
    Some(selected)
}
fn child(root: &Path, name: &str) -> Option<(PathBuf, SourceKind)> {
    // 保留现有file优先约定；后续兼容性变更不能暗含在catalog部署中。
    let file = root.join(format!("{name}.py"));
    if file.is_file() {
        return Some((file, SourceKind::ModuleFile));
    }
    let init = root.join(name).join("__init__.py");
    init.is_file().then_some((init, SourceKind::Package))
}

#[cfg(test)]
mod tests {
    use super::*;
    #[test]
    fn default_source_resolver_never_borrows_another_root_child() {
        let stamp = std::time::SystemTime::now()
            .duration_since(std::time::UNIX_EPOCH)
            .unwrap()
            .as_nanos();
        let root =
            std::env::temp_dir().join(format!("ryupy-resolver-{}-{stamp}", std::process::id()));
        let first = root.join("first");
        let second = root.join("second");
        for path in [&first, &second] {
            std::fs::create_dir_all(path.join("pkg")).unwrap();
            std::fs::write(path.join("pkg/__init__.py"), "").unwrap();
        }
        std::fs::write(second.join("pkg/child.py"), "").unwrap();
        assert!(resolve(&[first.clone(), second.clone()], "pkg.child").is_none());
        assert_eq!(
            resolve(&[second.clone(), first.clone()], "pkg.child")
                .unwrap()
                .0,
            second.join("pkg/child.py")
        );
        std::fs::write(first.join("pkg.py"), "").unwrap();
        assert!(resolve(&[first, second], "pkg.child").is_none());
        std::fs::remove_dir_all(root).unwrap();
    }
}
