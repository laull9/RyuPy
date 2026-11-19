//! 可选发现的真实文件图边界；只验证编译准备，不冒JIT/Native或CP差分证据。
use super::*;
use crate::modules::{load_program_including, load_program_with_discovery};
use std::sync::atomic::{AtomicU64, Ordering};
static NEXT: AtomicU64 = AtomicU64::new(0);
struct Tree(PathBuf);
impl Tree {
    fn new() -> Self {
        let root = std::env::temp_dir().join(format!(
            "ryupy-discovery-{}-{}",
            std::process::id(),
            NEXT.fetch_add(1, Ordering::Relaxed)
        ));
        std::fs::create_dir(&root).unwrap();
        Self(root)
    }
    fn write(&self, name: &str, contents: impl AsRef<[u8]>) {
        let path = self.0.join(name);
        std::fs::create_dir_all(path.parent().unwrap()).unwrap();
        std::fs::write(path, contents).unwrap();
    }
    fn setup(&self, call: &str) {
        self.write("entry.py", format!("import pkg\n{call}\n"));
        self.write("pkg/__init__.py", "");
        self.write("pkg/data/__init__.py", "# 数据包\n\n");
    }
    fn load(&self, discover: bool) -> (Session, Vec<SourceModule>) {
        let mut session = Session::default();
        let (sources, _) =
            load_program_with_discovery(&mut session, &self.0.join("entry.py"), &[], &[], discover)
                .unwrap();
        assert!(session.diagnostics.is_empty());
        (session, sources)
    }
}
impl Drop for Tree {
    fn drop(&mut self) {
        let _ = std::fs::remove_dir_all(&self.0);
    }
}
fn names(sources: &[SourceModule]) -> Vec<&str> {
    sources.iter().map(|source| source.name.as_str()).collect()
}
#[test]
fn spelling_hints_preserve_text_and_record_exact_discovery_reason() {
    for call in [
        "import importlib.resources\ndef unused():\n return importlib.resources.files('pkg.data')",
        "import importlib.resources as r\ndef unused():\n return r.files(anchor='pkg.data')",
        "from importlib import resources as r\nr.files('pkg.data')",
        "from importlib.resources import files as f\nf('pkg.data')",
        "from importlib.resources import files\nfiles = lambda name: name\nfiles('pkg.data')",
    ] {
        let tree = Tree::new();
        tree.setup(call);
        let (session, sources) = tree.load(true);
        assert_eq!(names(&sources), ["__main__", "pkg", "pkg.data"]);
        let discovered = &sources[2];
        assert_eq!(discovered.text, "# 数据包\n\n");
        assert_eq!(
            session.sources.get(discovered.parsed.file).text,
            discovered.text
        );
        let report = discovered.resource_discovery.as_ref().unwrap();
        assert_eq!(
            (&*report.source, &*report.anchor, report.reason),
            ("__main__", "pkg.data", "literal-files-empty-direct-package")
        );
        assert_eq!(names(&tree.load(false).1), ["__main__", "pkg"]);
    }
}
#[test]
fn missing_nonempty_invalid_and_encoded_candidates_never_pollute_diagnostics() {
    for bytes in [
        &b"pass\n"[..],
        b"'docstring'\n",
        b"def broken(:\n",
        b"raise RuntimeError('unused')\n",
        b"# coding: unknown-codec\n",
        b"# comment\r\n# coding=utf-8\r\n",
        b" \r# coding: not-a-codec\r",
        b"\xef\xbb\xbf\n",
        b"\xff\xfe\n",
        b"\xff\n",
    ] {
        let tree = Tree::new();
        tree.setup(
            "from importlib.resources import files\ndef unused():\n return files('pkg.data')",
        );
        tree.write("pkg/data/__init__.py", bytes);
        let (session, sources) = tree.load(true);
        assert_eq!(names(&sources), ["__main__", "pkg"]);
        assert_eq!(session.sources.iter().count(), 2);
    }
    let tree = Tree::new();
    tree.setup("from importlib.resources import files\nfiles('pkg.missing')");
    assert_eq!(names(&tree.load(true).1), ["__main__", "pkg"]);
    tree.write(
        "entry.py",
        "import pkg\nfrom importlib.resources import files\nfiles('pkg.data')\n",
    );
    std::fs::remove_file(tree.0.join("pkg/data/__init__.py")).unwrap();
    std::fs::create_dir(tree.0.join("pkg/data/__init__.py")).unwrap();
    assert_eq!(names(&tree.load(true).1), ["__main__", "pkg"]);
}
#[test]
fn discovery_uses_only_original_package_parents_and_does_not_recurse() {
    let tree = Tree::new();
    tree.setup("from importlib.resources import files\nfiles('pkg.data')\nfiles('pkg.data.deep')\nfiles('absent.data')");
    tree.write("pkg/data/deep/__init__.py", "");
    tree.write("absent/__init__.py", "");
    tree.write("absent/data/__init__.py", "");
    assert_eq!(names(&tree.load(true).1), ["__main__", "pkg", "pkg.data"]);
    tree.write("pkg.py", "");
    assert_eq!(names(&tree.load(true).1), ["__main__", "pkg"]);
}
#[test]
fn resolver_file_shadow_and_cross_root_parent_are_not_bypassed() {
    let tree = Tree::new();
    tree.setup("from importlib.resources import files\nfiles('pkg.data')");
    tree.write("pkg/data.py", "");
    assert_eq!(names(&tree.load(true).1), ["__main__", "pkg"]);
    std::fs::remove_file(tree.0.join("pkg/data.py")).unwrap();
    std::fs::remove_file(tree.0.join("pkg/data/__init__.py")).unwrap();
    let other = Tree::new();
    other.write("pkg/data/__init__.py", "");
    let mut session = Session::default();
    let (sources, _) = load_program_with_discovery(
        &mut session,
        &tree.0.join("entry.py"),
        std::slice::from_ref(&other.0),
        &[],
        true,
    )
    .unwrap();
    assert_eq!(names(&sources), ["__main__", "pkg"]);
}
#[test]
fn unsupported_call_forms_and_unrelated_literals_do_not_discover() {
    for call in [
        "from importlib.resources import files\nanchor = 'pkg.data'\nfiles(anchor)",
        "from importlib.resources import files\nfiles(package='pkg.data')",
        "from importlib.resources import files\nfiles('pkg.data', anchor='pkg.data')",
        "from importlib.resources import files\nfiles(*['pkg.data'])",
        "from importlib.resources import files\nfiles(**{'anchor': 'pkg.data'})",
        "def files(name):\n return name\nfiles('pkg.data')",
        "from importlib.resources import files\nfiles('pkg/data')",
    ] {
        let tree = Tree::new();
        tree.setup(call);
        assert_eq!(names(&tree.load(true).1), ["__main__", "pkg"]);
    }
}
#[test]
fn existing_hard_dependency_and_explicit_include_keep_original_contract() {
    let tree = Tree::new();
    tree.setup("from importlib.resources import files\nfiles('pkg.data')");
    tree.write("pkg/data/__init__.py", "import helper\n");
    tree.write("helper.py", "");
    let mut session = Session::default();
    let (sources, _) = load_program_with_discovery(
        &mut session,
        &tree.0.join("entry.py"),
        &[],
        &["pkg.data".into()],
        true,
    )
    .unwrap();
    assert_eq!(names(&sources), ["__main__", "helper", "pkg", "pkg.data"]);
    assert!(
        sources
            .iter()
            .all(|source| source.resource_discovery.is_none())
    );
    assert!(
        load_program_including(
            &mut session,
            &tree.0.join("entry.py"),
            &[],
            &["pkg.missing".into()]
        )
        .is_err()
    );
}
#[test]
fn actual_build_compilation_selects_jit_discovery_but_not_aot_graph() {
    let tree = Tree::new();
    tree.setup("from importlib.resources import files\ndef unused():\n return files('pkg.data')");
    for aot in [false, true] {
        let mut session = Session::default();
        let (linked, _, _) =
            crate::compile_linked(&mut session, &tree.0.join("entry.py"), &[], aot, None).unwrap();
        assert_eq!(linked.modules.len(), if aot { 2 } else { 3 });
        let report: serde_json::Value =
            serde_json::from_slice(&session.module_environment_report_json().unwrap()).unwrap();
        assert_eq!(
            report["modules"]
                .as_array()
                .unwrap()
                .iter()
                .filter(|row| row.get("resource_discovery").is_some())
                .count(),
            usize::from(!aot)
        );
    }
}

#[test]
fn aot_resource_manifest_does_not_turn_optional_anchor_into_include() {
    let tree = Tree::new();
    tree.setup("from importlib.resources import files\nfiles('pkg.data')");
    tree.write(
        "resources.json",
        r#"{"schema_version":1,"modules":[{"module":"pkg.data","files":[]}]}"#,
    );
    let mut session = Session::default();
    assert!(
        crate::compile_linked(
            &mut session,
            &tree.0.join("entry.py"),
            &[],
            true,
            Some(&tree.0.join("resources.json"))
        )
        .is_err()
    );
}
