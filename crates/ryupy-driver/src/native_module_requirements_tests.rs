//! 需求来自真实闭图和保守动态入口；显式include、传递及未分析外部provider不能被裁掉。
use super::*;

fn parsed(source: &str) -> ryupy_syntax::ParsedModule {
    let mut db = ryupy_base::SourceDb::default();
    let file = db.add("requirements.py", source);
    let (parsed, errors) = ryupy_syntax::parse(file, source);
    assert!(errors.is_empty(), "{errors:?}");
    parsed
}

#[test]
fn actual_closed_graph_keeps_transitive_and_explicit_include_requirements() {
    let tree = crate::test_temp::Root::new("native-requirements");
    std::fs::write(tree.join("entry.py"), "print(42)\n").unwrap();
    std::fs::write(
        tree.join("helper.py"),
        "from inspect import signature as reflect\n",
    )
    .unwrap();
    let build = |include: bool| {
        let mut sess = Session {
            include_modules: if include {
                vec!["helper".into()]
            } else {
                vec![]
            },
            ..Default::default()
        };
        compile_linked(&mut sess, &tree.join("entry.py"), &[], false, None)
            .unwrap()
            .0
    };
    assert!(!build(false).native_modules.inspect);
    assert!(build(true).native_modules.inspect);
    std::fs::write(tree.join("entry.py"), "import helper\nprint(42)\n").unwrap();
    assert!(build(false).native_modules.inspect);
}

#[test]
fn dynamic_import_aliases_and_reflective_access_remain_conservative() {
    for source in [
        "import importlib as dispatch\nloader = dispatch.import_module\nloader(name)\n",
        "from builtins import __import__ as loader\nloader(name)\n",
        "loader = __import__\nloader(name)\n",
        "fn.__globals__[key][key](name)\n",
        "system.modules[key].__dict__[key][key](name)\n",
        "getattr(value, key)(name)\n",
        "def globals():\n    return 3\nprint(globals())\n", // 遮蔽不做值流证明，宁可保留。
    ] {
        assert!(
            native_module_requirements::freeze_parsed(&Session::default(), [&parsed(source)], true)
                .inspect,
            "{source}"
        );
    }
    let simple = parsed(
        "def count(n):\n    i = 0\n    while i < n:\n        yield i\n        i += 1\nprint(sum(count(3)))\n",
    );
    assert!(
        !native_module_requirements::freeze_parsed(&Session::default(), [&simple], true).inspect
    );
    let external = Session {
        cpython_provider: Some("unused-config.json".into()),
        ..Default::default()
    };
    assert!(native_module_requirements::freeze_parsed(&external, [&simple], true).inspect);
}

#[test]
fn incomplete_dump_graph_preserves_unknown_static_dependencies() {
    let source = parsed("import helper\n");
    assert!(
        native_module_requirements::freeze_parsed(&Session::default(), [&source], false).inspect
    );
    let source = parsed("print(42)\n");
    assert!(
        !native_module_requirements::freeze_parsed(&Session::default(), [&source], false).inspect
    );
}

#[test]
fn bare_importer_keeps_its_existing_instance_admission_boundary() {
    let tree = crate::test_temp::Root::new("native-importer-boundary");
    std::fs::write(tree.join("entry.py"), "loader = __import__\n").unwrap();
    let mut session = Session {
        instance_modules: vec!["__main__".into()],
        ..Default::default()
    };
    assert!(compile_linked(&mut session, &tree.join("entry.py"), &[], false, None).is_err());
    assert!(
        session
            .diagnostics
            .iter()
            .any(|error| error.code == Some("E0100")
                && error.message == "实例模块首切暂不支持环境名字 `__import__` 的读写")
    );
}
