//! M3.5 模块异常的层间快照与源码遮蔽，所有临时源码均自建并清理。
use ryupy_driver::{Session, Stage, dump};

#[test]
fn snapshot_native_exception_aliases() {
    let root = std::env::temp_dir().join(format!("ryupy_m3_5_aliases_{}", std::process::id()));
    std::fs::create_dir_all(&root).unwrap();
    let path = root.join("exceptions.py");
    std::fs::write(&path, "import json as codec\nfrom re import error as PatternError\ntry:\n    codec.loads('[')\nexcept (codec.JSONDecodeError, PatternError):\n    print('caught')\n").unwrap();
    for (stage, name) in [
        (Stage::Hir, "module_exceptions_hir"),
        (Stage::Mir, "module_exceptions_mir"),
        (Stage::Cil, "module_exceptions_cil"),
    ] {
        let mut session = Session::default();
        let output = dump(&mut session, &path, stage).expect("原生异常应正确降级");
        insta::assert_snapshot!(
            name,
            output.replace(path.to_str().unwrap(), "exceptions.py")
        );
    }
    std::fs::remove_file(path).unwrap();
    std::fs::remove_dir(root).unwrap();
}

#[test]
fn local_source_named_json_is_not_treated_as_native_exception_module() {
    let root = std::env::temp_dir().join(format!("ryupy_m3_5_shadow_{}", std::process::id()));
    std::fs::create_dir_all(&root).unwrap();
    let path = root.join("main.py");
    let shadow = root.join("json.py");
    std::fs::write(&shadow, "JSONDecodeError = ValueError\n").unwrap();
    std::fs::write(
        &path,
        "import json\ntry:\n    pass\nexcept json.JSONDecodeError:\n    pass\n",
    )
    .unwrap();
    let mut session = Session::default();
    assert!(dump(&mut session, &path, Stage::Mir).is_err());
    let diagnostic = session
        .render_diagnostics(false)
        .replace(path.to_str().unwrap(), "main.py");
    insta::assert_snapshot!("module_exception_source_shadow", diagnostic);
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
    assert!(result.is_err(), "模块图构建也不能把本地源码认作原生异常");
    assert!(build_session.render_diagnostics(false).contains("E0100"));
    if output.exists() {
        std::fs::remove_dir_all(output).unwrap();
    }
    std::fs::remove_file(path).unwrap();
    std::fs::remove_file(shadow).unwrap();
    std::fs::remove_dir(root).unwrap();
}

#[test]
fn snapshot_dynamic_exception_alias_diagnostics() {
    let root = std::env::temp_dir().join(format!("ryupy_m3_5_dynamic_{}", std::process::id()));
    std::fs::create_dir_all(&root).unwrap();
    let path = root.join("dynamic.py");
    std::fs::write(&path, "import json\nfrom re import error as E\ndef replace():\n    global E\n    E = ValueError\ndef local(json):\n    try:\n        pass\n    except json.JSONDecodeError:\n        pass\ntry:\n    pass\nexcept (ValueError, json.unknown):\n    pass\ntry:\n    pass\nexcept E:\n    pass\n").unwrap();
    let mut session = Session::default();
    assert!(dump(&mut session, &path, Stage::Mir).is_err());
    insta::assert_snapshot!(
        "module_exception_dynamic_diagnostics",
        session
            .render_diagnostics(false)
            .replace(path.to_str().unwrap(), "dynamic.py")
    );
    std::fs::remove_file(path).unwrap();
    std::fs::remove_dir(root).unwrap();
}

#[test]
fn protected_exception_alias_rebinding_is_explicit_and_other_exports_stay_mutable() {
    let root = std::env::temp_dir().join(format!("ryupy_m3_5_protected_{}", std::process::id()));
    std::fs::create_dir_all(&root).unwrap();
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
    let root = std::env::temp_dir().join(format!("ryupy_m3_5_re_shadow_{}", std::process::id()));
    std::fs::create_dir_all(&root).unwrap();
    let main = root.join("main.py");
    std::fs::write(root.join("re.py"), "error = ValueError\n").unwrap();
    std::fs::write(
        &main,
        "from re import error as PatternError\ntry:\n    pass\nexcept PatternError:\n    pass\n",
    )
    .unwrap();
    let mut session = Session::default();
    assert!(dump(&mut session, &main, Stage::Mir).is_err());
    assert!(session.render_diagnostics(false).contains("E0100"));
    let mut session = Session::default();
    assert!(
        ryupy_driver::build(
            &mut session,
            &ryupy_driver::BuildOptions {
                input: main,
                out_dir: root.join("output"),
                aot: false,
                search_paths: Vec::new(),
            }
        )
        .is_err()
    );
    assert!(session.render_diagnostics(false).contains("E0100"));
    std::fs::remove_dir_all(root).unwrap();
}
