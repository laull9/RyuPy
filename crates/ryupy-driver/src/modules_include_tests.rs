//! 显式包含只拓展真实源码依赖图；测试不执行Python、SDK或模块初始化。
use super::*;
use std::sync::atomic::{AtomicU64, Ordering};
static NEXT: AtomicU64 = AtomicU64::new(0);
struct Tree(PathBuf);
impl Tree {
    fn new() -> Self {
        let time = std::time::SystemTime::now()
            .duration_since(std::time::UNIX_EPOCH)
            .unwrap()
            .as_nanos();
        for _ in 0..32 {
            let path = std::env::temp_dir().join(format!(
                "ryupy-include-{}-{time}-{}",
                std::process::id(),
                NEXT.fetch_add(1, Ordering::Relaxed)
            ));
            match std::fs::create_dir(&path) {
                Ok(()) => return Self(path),
                Err(error) if error.kind() == std::io::ErrorKind::AlreadyExists => continue,
                Err(error) => panic!("create owned source tree: {error}"),
            }
        }
        panic!("cannot allocate owned source tree");
    }
    fn write(&self, path: &str, text: &str) {
        let path = self.0.join(path);
        std::fs::create_dir_all(path.parent().unwrap()).unwrap();
        std::fs::write(path, text).unwrap();
    }
    fn load(&self, names: &[&str]) -> Result<Vec<SourceModule>> {
        let mut session = Session::default();
        let result = load_program_including(
            &mut session,
            &self.0.join("main.py"),
            &[],
            &names.iter().map(|s| (*s).to_owned()).collect::<Vec<_>>(),
        )?;
        assert!(session.diagnostics.is_empty());
        Ok(result.0)
    }
}
impl Drop for Tree {
    fn drop(&mut self) {
        let _ = std::fs::remove_dir_all(&self.0);
    }
}
#[test]
fn explicit_include_finds_string_only_anchor_and_transitive_dependencies_once() {
    let tree = Tree::new();
    tree.write("main.py", "anchor = 'pkg.fonts'\n");
    tree.write(
        "pkg/__init__.py",
        "raise RuntimeError('must not execute during compilation')\n",
    );
    tree.write("pkg/fonts/__init__.py", "import helper\n");
    tree.write("helper.py", "value = 1\n");
    assert_eq!(tree.load(&[]).unwrap().len(), 1);
    let modules = tree.load(&["pkg.fonts", "pkg.fonts", "pkg"]).unwrap();
    assert_eq!(
        modules.iter().map(|m| m.name.as_str()).collect::<Vec<_>>(),
        ["__main__", "helper", "pkg", "pkg.fonts"]
    );
    assert_eq!(modules[0].kind, SourceKind::Entry);
    assert_eq!(modules[1].kind, SourceKind::ModuleFile);
    assert!(modules[2..].iter().all(|m| m.kind == SourceKind::Package));
}
#[test]
fn explicit_missing_and_nonpackage_parent_fail_without_native_fallback() {
    let tree = Tree::new();
    tree.write("main.py", "import sys\n");
    for name in ["missing", "sys", "missing.child"] {
        assert!(tree.load(&[name]).is_err(), "{name}");
    }
    tree.write("pkg.py", "value = 1\n");
    tree.write("pkg/child.py", "value = 2\n");
    assert!(tree.load(&["pkg.child"]).is_err());
    assert_eq!(tree.load(&[]).unwrap().len(), 1);
}
#[test]
fn explicit_option_rejects_paths_relative_empty_and_non_ascii_names() {
    let tree = Tree::new();
    tree.write("main.py", "value = 1\n");
    // 文件实际存在也不能注册与固定入口同名的父包。
    tree.write("__main__/__init__.py", "value = 2\n");
    tree.write("__main__/child.py", "value = 3\n");
    for name in [
        "",
        ".pkg",
        "pkg.",
        "pkg..child",
        "../pkg",
        "/pkg",
        "C:\\pkg",
        "pkg/child",
        "pkg\\child",
        "pkg-name",
        "1pkg",
        "__main__",
        "__main__.child",
        "中文",
    ] {
        assert!(tree.load(&[name]).is_err(), "{name:?}");
    }
}
#[test]
fn resolver_branch_not_init_basename_controls_package_identity() {
    let tree = Tree::new();
    tree.write("main.py", "value = 1\n");
    tree.write("pkg/__init__.py", "value = 2\n");
    let modules = tree.load(&["pkg", "pkg.__init__"]).unwrap();
    let package = modules.iter().find(|m| m.name == "pkg").unwrap();
    let module = modules.iter().find(|m| m.name == "pkg.__init__").unwrap();
    assert_eq!(package.resolved_source_path, module.resolved_source_path);
    assert_eq!(package.kind, SourceKind::Package);
    assert_eq!(module.kind, SourceKind::ModuleFile);
}
#[test]
fn empty_include_preserves_existing_search_roots_and_unicode_imports() {
    let tree = Tree::new();
    tree.write("main.py", "import 中文\nimport sys\n");
    tree.write("中文.py", "value = 3\n");
    let mut old_session = Session::default();
    let (old, _) = load_program(&mut old_session, &tree.0.join("main.py"), &[]).unwrap();
    let new = tree.load(&[]).unwrap();
    assert_eq!(
        old.iter()
            .map(|m| (&m.name, &m.text, m.kind))
            .collect::<Vec<_>>(),
        new.iter()
            .map(|m| (&m.name, &m.text, m.kind))
            .collect::<Vec<_>>()
    );
    assert_eq!(new.len(), 2);
}

#[test]
fn linked_metadata_uses_actual_package_kind_and_current_session_input() {
    let tree = Tree::new();
    tree.write("main.py", "value = 1\n");
    tree.write("pkg/__init__.py", "value = 2\n");
    let mut session = Session {
        include_modules: vec!["pkg".into(), "pkg.__init__".into()],
        ..Session::default()
    };
    let (program, _, _) =
        crate::compile_linked(&mut session, &tree.0.join("main.py"), &[], false, None).unwrap();
    assert_eq!(program.modules.len(), 3);
    for (_, module) in program.modules.iter() {
        assert_eq!(module.is_package, module.name == "pkg");
        assert!(matches!(
            module.location,
            ryupy_mir::ModuleLocation::SourceAbsolute(_)
        ));
    }
    // 同一个Session的显式配置可清空，图不会保存上一次include的隐式状态。
    session.include_modules.clear();
    let (program, _, _) =
        crate::compile_linked(&mut session, &tree.0.join("main.py"), &[], false, None).unwrap();
    assert_eq!(program.modules.len(), 1);
    assert!(!program.modules[program.entry].is_package);
}
