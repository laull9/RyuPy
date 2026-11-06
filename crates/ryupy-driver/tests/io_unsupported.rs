//! io.UnsupportedOperation 仅承认稳定原生导入；双父catch与用户派生类均有真实e2e另验。
use ryupy_driver::{Session, Stage};
mod support;
use support::dump;
#[path = "m3_5_dynamic_support/mod.rs"]
mod dynamic_support;
#[test]
fn stable_io_aliases_and_subclasses_lower_with_secondary_parent_handler() {
    let path = std::env::temp_dir().join("ryupy_io_unsupported_static.py");
    std::fs::write(&path, "import io as stream\nfrom io import UnsupportedOperation as Unsupported\nclass Child(Unsupported):\n    pass\ntry:\n    raise Child('x')\nexcept (ValueError, ValueError, stream.UnsupportedOperation):\n    print('caught')\n").unwrap();
    let mut session = Session::default();
    let output = dump(&mut session, &path, Stage::Mir).expect("稳定原生io异常应降级");
    insta::assert_snapshot!("io_unsupported_mir", output);
    std::fs::remove_file(path).unwrap();
}
#[test]
fn rebinding_or_local_io_source_does_not_gain_static_exception_identity() {
    let directory = std::env::temp_dir().join(format!(
        "ryupy_io_unsupported_shadow_{}",
        std::process::id()
    ));
    std::fs::create_dir_all(&directory).unwrap();
    let path = directory.join("main.py");
    {
        let source =
            "import io\nio = None\ntry:\n    pass\nexcept io.UnsupportedOperation:\n    pass\n";
        std::fs::write(&path, source).unwrap();
        let mut session = Session::default();
        dump(&mut session, &path, Stage::Mir).expect("重绑io保动态属性目标");
        assert!(!session.diagnostics.has_errors());
        dynamic_support::check(&path, None, &["global.io.UnsupportedOperation"]);
        for extension in ["hir", "mir"] {
            std::fs::rename(
                directory.join(format!("dynamic.{extension}")),
                directory.join(format!("rebound.{extension}")),
            )
            .unwrap();
        }
    }
    // 运行时基类增量允许该语法，但真实重绑的异常布局仍须在类体前拒绝。
    std::fs::write(&path, "from io import UnsupportedOperation as U\nU = ValueError\ntry:\n    class Child(U):\n        print('不应执行类体')\nexcept NotImplementedError:\n    print('特殊异常布局拒绝')\n").unwrap();
    let mut session = Session::default();
    let output = dump(&mut session, &path, Stage::Mir).expect("重绑基类使用运行时守卫");
    assert!(output.contains("ValidateOrdinaryBase"));
    let artifact = ryupy_driver::build(
        &mut session,
        &ryupy_driver::BuildOptions {
            input: path.clone(),
            out_dir: directory.join("out"),
            aot: false,
            search_paths: vec![],
        },
    )
    .unwrap();
    let result = ryupy_driver::dotnet::run_dll(&artifact.dll).unwrap();
    assert!(
        result.status.success(),
        "{}",
        String::from_utf8_lossy(&result.stderr)
    );
    assert!(result.stderr.is_empty());
    assert_eq!(
        String::from_utf8_lossy(&result.stdout),
        "特殊异常布局拒绝\n"
    );
    std::fs::write(
        directory.join("io.py"),
        "UnsupportedOperation = ValueError\n",
    )
    .unwrap();
    std::fs::write(
        &path,
        "import io\ntry:\n    pass\nexcept io.UnsupportedOperation:\n    pass\n",
    )
    .unwrap();
    let mut session = Session::default();
    dump(&mut session, &path, Stage::Mir).expect("本地io来源不借native typed身份");
    assert!(!session.diagnostics.has_errors());
    dynamic_support::check(
        &path,
        Some(("io", "UnsupportedOperation = ValueError\n")),
        &["global.io.UnsupportedOperation"],
    );
    std::fs::remove_dir_all(directory).unwrap();
}
