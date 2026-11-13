//! 裸return的动态返回槽必须装入None单例；静态Unit/int保持独立，真实调用与CPython逐字对照。
use ryupy_driver::{Session, Stage};
#[path = "support/process_oracle.rs"]
mod process_oracle;
mod support;
use process_oracle::{TempRoot, bounded, pinned_python};

fn source() -> std::path::PathBuf {
    ryupy_driver::runtime::workspace_root().join("tests/e2e/phase2/m2_10_bare_return_none.py")
}

#[test]
fn bare_return_dynamic_slot_uses_none_object_not_integer_zero() {
    let mut session = Session::default();
    let mir = support::dump(&mut session, &source(), Stage::Mir).unwrap();
    assert!(session.diagnostics.is_empty());
    for name in ["bare", "annotated_bare", "method_bare"] {
        let body = mir_function(&mir, name);
        assert!(body.lines().next().unwrap().ends_with("-> dyn {"));
        assert!(body.contains("RuntimeFn::DynNone()"), "{name}: {body}");
        assert!(
            !body.contains("const 0_i64"),
            "裸return不能写整数零: {body}"
        );
    }
    let unit = mir_function(&mir, "unit");
    assert!(unit.lines().next().unwrap().ends_with("-> () {"));
    assert!(!unit.contains("RuntimeFn::DynNone()"));
    let integer = mir_function(&mir, "integer");
    assert!(integer.lines().next().unwrap().ends_with("-> i64 {"));
    assert!(integer.contains("const 7_i64"));

    let mut session = Session::default();
    let cil = support::dump(&mut session, &source(), Stage::Cil).unwrap();
    assert!(session.diagnostics.is_empty());
    for name in ["bare", "annotated_bare", "method_bare"] {
        let body = cil_function(&cil, name);
        assert!(body.contains("PyOps.None()"), "{name}: {body}");
        assert!(
            !body.contains("LoadConstantInt64(0)"),
            "不能生成ldc.i8写object返回槽"
        );
    }
    assert!(
        cil_function(&cil, "unit")
            .lines()
            .next()
            .unwrap()
            .starts_with("public static void ")
    );
    assert!(
        cil_function(&cil, "integer")
            .lines()
            .next()
            .unwrap()
            .starts_with("public static long ")
    );
}

#[test]
fn bare_explicit_and_implicit_none_match_pinned_python() {
    let root = TempRoot::new("bare-return-none");
    let python = pinned_python(root.path());
    let cp_cwd = root.path().join("cp-cwd");
    let jit_cwd = root.path().join("jit-cwd");
    std::fs::create_dir(&cp_cwd).unwrap();
    std::fs::create_dir(&jit_cwd).unwrap();
    let expected = bounded(
        root.path(),
        "cp",
        std::process::Command::new(python)
            .args(["-I", "-B", "-S"])
            .arg(source())
            .current_dir(cp_cwd)
            .env("LC_ALL", "C")
            .env("TZ", "UTC"),
        std::time::Duration::from_secs(60),
    )
    .unwrap();
    assert!(expected.status.success() && expected.stderr.is_empty());
    let mut session = Session::default();
    let artifact = ryupy_driver::build(
        &mut session,
        &ryupy_driver::BuildOptions {
            input: source(),
            out_dir: root.path().join("out"),
            aot: false,
            search_paths: vec![],
        },
    )
    .unwrap();
    assert!(session.diagnostics.is_empty());
    let actual = bounded(
        root.path(),
        "jit",
        std::process::Command::new(std::env::var_os("DOTNET").unwrap_or_else(|| "dotnet".into()))
            .arg(artifact.dll)
            .current_dir(jit_cwd)
            .env("LC_ALL", "C")
            .env("TZ", "UTC"),
        std::time::Duration::from_secs(60),
    )
    .unwrap();
    assert!(
        actual.status.success(),
        "{}",
        String::from_utf8_lossy(&actual.stderr)
    );
    assert_eq!(actual.stderr, expected.stderr);
    assert_eq!(actual.stdout, expected.stdout);
}

fn mir_function<'a>(output: &'a str, name: &str) -> &'a str {
    output
        .split("fn ")
        .find(|body| body.starts_with(&format!("{name}(")))
        .unwrap()
}

fn cil_function<'a>(output: &'a str, name: &str) -> &'a str {
    output
        .split("    .method ")
        .find(|body| {
            body.lines().next().is_some_and(|header| {
                header.split_whitespace().any(|word| {
                    word.split_once('(').is_some_and(|(method, _)| {
                        method
                            .split_once('_')
                            .is_some_and(|(_, method)| method == name)
                    })
                })
            })
        })
        .unwrap()
}
