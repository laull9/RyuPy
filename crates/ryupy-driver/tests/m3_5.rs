//! M3.5 模块异常的层间结构合同与源码遮蔽，所有临时源码均自建并清理。
use ryupy_driver::{Session, Stage};
mod support;
use support::dump;
#[path = "m3_5_dynamic_support/mod.rs"]
mod dynamic_support;

#[test]
fn snapshot_native_exception_aliases() {
    let scope = support::temp_scope::TempScope::new("m3_5");
    let root = scope.root();

    let path = root.join("exceptions.py");
    std::fs::write(&path, "import json as codec\nfrom re import error as PatternError\ntry:\n    codec.loads('[')\nexcept (codec.JSONDecodeError, PatternError):\n    print('caught')\n").unwrap();
    for stage in [Stage::Hir, Stage::Mir, Stage::Cil] {
        let mut session = Session::default();
        let output = dump(&mut session, &path, stage).expect("原生异常应正确降级");
        let required: &[&str] = match stage {
            Stage::Hir => &[
                "import json as global.codec",
                "from re import error as global.PatternError",
                "except json.JSONDecodeError | re.error:",
            ],
            Stage::Mir => &[
                "ImportModule(const \"json\"",
                "ImportAttr(const \"re\", const \"error\")",
                "catch json.JSONDecodeError",
                "catch re.error",
            ],
            Stage::Cil => &[
                "PyModules.Import",
                "PyModules.ImportAttr",
                "PyJSONDecodeError handler",
                "PyRegexError handler",
            ],
            _ => unreachable!("测试只列出已约定阶段"),
        };
        for token in required {
            assert!(output.contains(token), "缺少结构 {token}：{output}");
        }
    }
    std::fs::remove_file(path).unwrap();
    std::fs::remove_dir(root).unwrap();
}

#[test]
fn local_source_named_json_is_not_treated_as_native_exception_module() {
    let scope = support::temp_scope::TempScope::new("m3_5");
    let root = scope.root();

    let path = root.join("main.py");
    let shadow = root.join("json.py");
    std::fs::write(&shadow, "JSONDecodeError = ValueError\n").unwrap();
    std::fs::write(
        &path,
        "import json\ntry:\n    pass\nexcept json.JSONDecodeError:\n    pass\n",
    )
    .unwrap();
    let mut session = Session::default();
    dump(&mut session, &path, Stage::Mir).expect("本地来源保动态目标，不再拒绝合法Attribute");
    assert!(!session.diagnostics.has_errors());
    dynamic_support::check(
        &path,
        Some(("json", "JSONDecodeError = ValueError\n")),
        &["global.json.JSONDecodeError"],
    );
    let mut build_session = Session::default();
    let output = root.join("output");
    let result = ryupy_driver::build(
        &mut build_session,
        &ryupy_driver::BuildOptions {
            input: path.clone(),
            out_dir: output.clone(),
            aot: false,
            search_paths: Vec::new(),
        },
    );
    result.expect("实际模块图允许动态handler，但本地来源必须保留");
    assert!(!build_session.diagnostics.has_errors());
    if output.exists() {
        std::fs::remove_dir_all(output).unwrap();
    }
    std::fs::remove_file(path).unwrap();
    std::fs::remove_file(shadow).unwrap();
    std::fs::remove_file(root.join("dynamic.hir")).unwrap();
    std::fs::remove_file(root.join("dynamic.mir")).unwrap();
    std::fs::remove_dir(root).unwrap();
}

#[test]
fn dynamic_exception_aliases_preserve_runtime_targets() {
    let scope = support::temp_scope::TempScope::new("m3_5");
    let root = scope.root();

    let path = root.join("dynamic.py");
    std::fs::write(&path, "import json\nfrom re import error as E\ndef replace():\n    global E\n    E = ValueError\ndef local(json):\n    try:\n        pass\n    except json.JSONDecodeError:\n        pass\ntry:\n    pass\nexcept (ValueError, json.unknown):\n    pass\ntry:\n    pass\nexcept E:\n    pass\n").unwrap();
    let mut session = Session::default();
    dump(&mut session, &path, Stage::Mir).expect("新增Name/Attribute/tuple范围可降级");
    assert!(!session.diagnostics.has_errors());
    dynamic_support::check(
        &path,
        None,
        &[
            "local.json.JSONDecodeError",
            "(builtin.ValueError,global.json.unknown)",
            "global.E",
        ],
    );
    std::fs::remove_file(path).unwrap();
    std::fs::remove_file(root.join("dynamic.hir")).unwrap();
    std::fs::remove_file(root.join("dynamic.mir")).unwrap();
    std::fs::remove_dir(root).unwrap();
}

#[test]
fn protected_exception_alias_rebinding_is_explicit_and_other_exports_stay_mutable() {
    let scope = support::temp_scope::TempScope::new("m3_5");
    let root = scope.root();

    let main = root.join("main.py");
    let handlers = root.join("handlers.py");
    // codec 与 E 实际用于 handler；ordinary 虽同样导入 json，但不能被误保护。
    std::fs::write(&handlers, "import json as codec\nimport json as ordinary\nfrom re import error as E\ndef run():\n    try:\n        codec.loads('[')\n    except (codec.JSONDecodeError, E):\n        print('native caught')\n").unwrap();
    std::fs::write(&main, "import handlers\nfor name in ['codec', 'E']:\n    try:\n        setattr(handlers, name, ValueError)\n    except NotImplementedError:\n        print('protected', name)\nhandlers.ordinary = 7\nprint(handlers.ordinary)\nhandlers.run()\n").unwrap();
    let mut session = Session::default();
    let artifact = ryupy_driver::build(
        &mut session,
        &ryupy_driver::BuildOptions {
            input: main,
            out_dir: root.join("output"),
            aot: false,
            search_paths: Vec::new(),
        },
    )
    .unwrap_or_else(|error| panic!("{error}: {}", session.render_diagnostics(false)));
    let output = std::process::Command::new(ryupy_driver::dotnet::dotnet_exe())
        .arg(artifact.dll)
        .output()
        .unwrap();
    assert_eq!(
        output.status.code(),
        Some(0),
        "{}",
        String::from_utf8_lossy(&output.stderr)
    );
    assert_eq!(
        String::from_utf8_lossy(&output.stdout),
        "protected codec\nprotected E\n7\nnative caught\n"
    );
    assert!(output.stderr.is_empty());
    std::fs::remove_dir_all(root).unwrap();
}

#[test]
fn local_source_named_re_is_not_treated_as_native_exception_module() {
    let scope = support::temp_scope::TempScope::new("m3_5");
    let root = scope.root();

    let main = root.join("main.py");
    std::fs::write(root.join("re.py"), "error = ValueError\n").unwrap();
    std::fs::write(
        &main,
        "from re import error as PatternError\ntry:\n    pass\nexcept PatternError:\n    pass\n",
    )
    .unwrap();
    let mut session = Session::default();
    dump(&mut session, &main, Stage::Mir).expect("本地re来源保真实导入值");
    assert!(!session.diagnostics.has_errors());
    dynamic_support::check(
        &main,
        Some(("re", "error = ValueError\n")),
        &["global.PatternError"],
    );
    let mut session = Session::default();
    ryupy_driver::build(
        &mut session,
        &ryupy_driver::BuildOptions {
            input: main,
            out_dir: root.join("output"),
            aot: false,
            search_paths: Vec::new(),
        },
    )
    .expect("实际模块图允许本地导入异常值");
    assert!(!session.diagnostics.has_errors());
    std::fs::remove_dir_all(root).unwrap();
}
