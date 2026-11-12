//! build/dump共用认证的编译门；只检查IR/诊断与未发PE，不启动CPython或.NET。
use super::*;
use crate::{BuildOptions, Stage};

struct Root(PathBuf);
impl Root {
    fn new() -> Self {
        let path = crate::test_temp::directory("ryupy-native-identity");
        Self(path)
    }
    fn write(&self, path: &str, source: &str) -> PathBuf {
        let path = self.0.join(path);
        std::fs::create_dir_all(path.parent().unwrap()).unwrap();
        std::fs::write(&path, source).unwrap();
        path
    }
}
impl Drop for Root {
    fn drop(&mut self) {
        if !std::thread::panicking() {
            let _ = std::fs::remove_dir_all(&self.0);
        }
    }
}

#[test]
fn build_and_dump_reject_original_fixtures_and_all_stable_xml_forms() {
    let sources = [
        include_str!("../../../tests/fixtures/treebuilder/subclass_syntax.py"),
        include_str!("../../../tests/fixtures/treebuilder/subclass_parse.py"),
        include_str!("../../../tests/fixtures/treebuilder/subclass_imported.py"),
        "import xml.etree.ElementTree as tree\nclass Child(tree.ParseError): pass\n",
        "from xml.etree.ElementTree import ParseError\nclass Child(ParseError): pass\n",
    ];
    for source in sources {
        let root = Root::new();
        let input = root.write("main.py", source);
        let mut dump = Session::default();
        assert!(
            crate::dump(&mut dump, &input, Stage::Mir).is_err(),
            "{source}"
        );
        let mut build = Session::default();
        let out_dir = root.0.join("out");
        assert!(
            crate::build(
                &mut build,
                &BuildOptions {
                    input,
                    out_dir: out_dir.clone(),
                    aot: false,
                    search_paths: vec![],
                }
            )
            .is_err(),
            "{source}"
        );
        for session in [&dump, &build] {
            let expected_start = if source.contains("class Child(SyntaxError)") {
                source.rfind("SyntaxError").unwrap()
            } else {
                source.find("class Child").unwrap()
            };
            let rejection = session
                .diagnostics
                .iter()
                .find(|diagnostic| {
                    diagnostic.code == Some("E0100") && diagnostic.message.contains("用户派生")
                })
                .expect("必须是基类身份拒绝，不能用无关错误代替");
            assert_eq!(
                usize::from(rejection.primary.span.range.start()),
                expected_start
            );
            assert!(!rejection.primary.span.range.is_empty());
            let diagnostic = session.render_diagnostics(false);
            assert!(
                diagnostic.contains("E0100") && diagnostic.contains("用户派生"),
                "{source}: {diagnostic}"
            );
        }
        assert!(!out_dir.exists(), "编译拒绝不能写PE");
    }
}

#[test]
fn source_child_and_parent_shadow_disable_both_certification_paths() {
    for (path, source) in [
        ("xml/etree/ElementTree.py", "class ParseError: pass\n"),
        ("xml/__init__.py", "marker = 1\n"),
        ("xml/etree/__init__.py", "marker = 1\n"),
        ("xml.py", "marker = 1\n"),
    ] {
        let root = Root::new();
        let input = root.write(
            "main.py",
            "import xml.etree.ElementTree as ET\nclass Child(ET.ParseError): pass\n",
        );
        root.write(path, source);
        let mut session = Session::default();
        let (sources, _) = load_program(&mut session, &input, &[]).unwrap();
        assert!(!native_exception_modules(&input).contains(&"xml.etree.ElementTree"));
        assert!(
            !native_exception_modules_for_build(&input, &[], &sources)
                .contains(&"xml.etree.ElementTree")
        );
        let mut dump = Session::default();
        crate::dump(&mut dump, &input, Stage::Mir).unwrap();
        assert!(!dump.diagnostics.has_errors());
        let mut build = Session::default();
        crate::compile_linked(&mut build, &input, &[], false, None).unwrap();
        assert!(!build.diagnostics.has_errors());
        // 仅编译结构通过；父包遮蔽的运行结果不在这里假定为成功。
    }
}

#[test]
fn explicit_search_roots_and_resolved_sources_both_cancel_native_origin() {
    let root = Root::new();
    let input = root.write("entry/main.py", "import xml.etree.ElementTree as ET\n");
    root.write("search/xml.py", "marker = 1\n");
    let paths = [root.0.join("search")];
    assert!(native_exception_modules(&input).contains(&"xml.etree.ElementTree"));
    assert!(
        !native_exception_modules_for_build(&input, &paths, &[]).contains(&"xml.etree.ElementTree")
    );
    let mut session = Session::default();
    let (sources, _) = load_program(&mut session, &input, &paths).unwrap();
    // 即使不再次传搜索根，已加载父源码也必须阻断认证。
    assert!(
        !native_exception_modules_for_build(&input, &[], &sources)
            .contains(&"xml.etree.ElementTree")
    );
}

#[test]
fn unaliased_xml_chain_remains_runtime_even_with_native_module_origin() {
    // 002收窄证明域：此形式不是第六条静态拒绝，不改前三条原fixture契约。
    let root = Root::new();
    let source =
        "import xml.etree.ElementTree\nclass Child(xml.etree.ElementTree.ParseError): pass\n";
    let input = root.write("main.py", source);
    let mut session = Session::default();
    let loaded = load_single(&mut session, &input).unwrap();
    let native = native_exception_modules(&input);
    assert!(native.contains(&"xml.etree.ElementTree"));
    let (hir, diagnostics) = ryupy_hir::lower_module_with_native_exceptions(
        &loaded.parsed,
        &ryupy_hir::ModuleInfo::default(),
        source,
        &native,
    );
    assert!(diagnostics.is_empty(), "{diagnostics:?}");
    assert_eq!(hir.classes.len(), 1, "必须实际保留待Runtime验证的类");
    assert_eq!(hir.classes.iter().next().unwrap().1.name.as_str(), "Child");
    assert!(
        hir.classes
            .iter()
            .all(|(_, class)| matches!(class.base, Some(ryupy_hir::ClassBase::Runtime(_))))
    );
    assert!(hir.protected_exports.is_empty());
    crate::dump(&mut Session::default(), &input, Stage::Mir).unwrap();
    crate::compile_linked(&mut Session::default(), &input, &[], false, None).unwrap();
}

#[test]
fn source_evidence_is_negative_only_and_validates_whole_name() {
    let root = Root::new();
    let input = root.write("first/main.py", "import xml.etree.ElementTree\n");
    let first = root.0.join("first");
    let second = root.0.join("second");
    std::fs::create_dir_all(first.join("xml/etree")).unwrap();
    assert!(!source_evidence(
        std::slice::from_ref(&first),
        "xml.etree.ElementTree"
    ));
    assert!(native_exception_modules(&input).contains(&"xml.etree.ElementTree"));
    root.write("outside.py", "marker = 1\n");
    for name in [
        "",
        ".outside",
        "..outside",
        "../outside",
        "xml/etree",
        "xml\\etree",
        "/outside",
        "xml.",
    ] {
        assert!(
            !source_evidence(std::slice::from_ref(&first), name),
            "{name}"
        );
    }
    root.write("first/xml/__init__.py", "");
    root.write("first/xml/etree/__init__.py", "");
    root.write(
        "second/xml/etree/ElementTree.py",
        "class ParseError: pass\n",
    );
    let roots = [first.clone(), second.clone()];
    assert!(source_evidence(&roots, "xml.etree.ElementTree"));
    assert!(resolve_in(&roots, "xml.etree.ElementTree").is_none());
    assert!(
        !native_exception_modules_for_build(&input, &[second], &[])
            .contains(&"xml.etree.ElementTree")
    );
    // 文件父不把实际子文件变成可加载源码；仍保守取消native资格。
    root.write("first/xml.py", "marker = 1\n");
    root.write("first/xml/etree/ElementTree.py", "class ParseError: pass\n");
    assert!(source_evidence(&roots, "xml.etree.ElementTree"));
    assert!(resolve_in(&roots, "xml.etree.ElementTree").is_none());
    assert!(!native_exception_modules(&input).contains(&"xml.etree.ElementTree"));
}

#[test]
fn collections_abc_cold_candidate_requires_unshadowed_native_module_and_parent() {
    let clean = Root::new();
    let input = clean.write(
        "main.py",
        "from collections.abc import Sequence\nclass Child(Sequence): pass\n",
    );
    assert!(native_exception_modules(&input).contains(&"collections.abc"));
    for (path, source) in [
        ("collections.py", "Sequence = object\n"),
        ("collections/__init__.py", ""),
        ("collections/abc.py", "class Sequence: pass\n"),
    ] {
        let root = Root::new();
        let input = root.write(
            "main.py",
            "from collections.abc import Sequence\nclass Child(Sequence): pass\n",
        );
        root.write(path, source);
        assert!(
            !native_exception_modules(&input).contains(&"collections.abc"),
            "{path}"
        );
    }
    let extra = Root::new();
    extra.write("collections/abc.py", "class Sequence: pass\n");
    assert!(
        !native_exception_modules_for_build(&input, std::slice::from_ref(&extra.0), &[])
            .contains(&"collections.abc")
    );
}
