//! 隐式None落尾的CPython差分与完整返回函数的MIR快路合同。
use ryupy_driver::{Session, Stage};
#[path = "support/process_oracle.rs"]
mod process_oracle;
mod support;
use process_oracle::{TempRoot, bounded, pinned_python};
fn source() -> std::path::PathBuf {
    ryupy_driver::runtime::workspace_root()
        .join("tests/e2e/phase2/m2_10_static_fallthrough_none.py")
}
#[test]
fn fallthrough_demotes_only_partial_return_paths() {
    let mut session = Session::default();
    let mir = support::dump(&mut session, &source(), Stage::Mir).unwrap();
    assert!(session.diagnostics.is_empty());
    for name in [
        "conditional",
        "caller",
        "loop_partial",
        "loop_break",
        "loop_else",
        "final_override",
    ] {
        let body = mir
            .split("fn ")
            .find(|b| b.starts_with(&format!("{name}(")))
            .unwrap();
        assert!(body.lines().next().unwrap().ends_with("-> dyn {"), "{body}");
        if name != "caller" {
            assert!(body.contains("RuntimeFn::DynNone()"), "{body}");
        }
    }
    for name in ["complete", "final_complete"] {
        let body = mir
            .split("fn ")
            .find(|b| b.starts_with(&format!("{name}(")))
            .unwrap();
        assert!(body.lines().next().unwrap().ends_with("-> i64 {"), "{body}");
        assert!(!body.contains("RuntimeFn::DynNone()"), "{body}");
    }
}
#[test]
fn fallthrough_matches_pinned_python() {
    let root = TempRoot::new("static-fallthrough");
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
