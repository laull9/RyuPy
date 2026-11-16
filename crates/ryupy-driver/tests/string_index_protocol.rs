//! 字符串下标类型级整数协议、警告和异常与固定CPython逐次比较。
use ryupy_driver::Session;
#[path = "support/process_oracle.rs"]
mod process_oracle;
use process_oracle::{TempRoot, bounded, pinned_python};
fn source() -> std::path::PathBuf {
    ryupy_driver::runtime::workspace_root().join("tests/e2e/phase2/m2_4_string_index_protocol.py")
}
#[test]
fn string_index_protocol_matches_pinned_python() {
    let root = TempRoot::new("string-index-protocol");
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
