//! BadZipFile真实类型、完整参数与静态用户派生的CP差分；不执行ZIP解析。
use ryupy_driver::{BuildOptions, Session};
use std::path::PathBuf;
use std::process::{Command, Output};
use std::sync::atomic::{AtomicU64, Ordering};
#[path = "m3_5_dynamic_support/mod.rs"]
mod dynamic_support;

static NEXT_ROOT: AtomicU64 = AtomicU64::new(0);

struct Root(PathBuf);
impl Drop for Root {
    fn drop(&mut self) {
        if !std::thread::panicking() {
            let _ = std::fs::remove_dir_all(&self.0);
        }
    }
}
fn execute(source: &str, filename: &str, reference: bool) -> (Root, Option<Output>, Output) {
    let python = std::env::var_os("PYTHON").unwrap_or_else(|| "python3".into());
    let version = Command::new(&python).args(["-c", "import sys; assert sys.implementation.name == 'cpython'; print('.'.join(map(str, sys.version_info[:3])))"]).output().unwrap();
    assert!(version.status.success());
    let pinned = std::fs::read_to_string(
        ryupy_driver::runtime::workspace_root().join("tests/pinned-python.txt"),
    )
    .unwrap();
    assert_eq!(
        String::from_utf8_lossy(&version.stdout).trim(),
        pinned.trim()
    );
    let unique = std::time::SystemTime::now()
        .duration_since(std::time::UNIX_EPOCH)
        .unwrap()
        .as_nanos();
    let serial = NEXT_ROOT.fetch_add(1, Ordering::Relaxed);
    let path = std::env::temp_dir().join(format!(
        "ryupy-badzip-exception-{}-{unique}-{serial}",
        std::process::id()
    ));
    // 只有create成功后才拥有清理权；碰撞失败不能删除他人或旧产物。
    std::fs::create_dir(&path).unwrap();
    let root = Root(path);
    let input = root.0.join(filename);
    std::fs::write(&input, source).unwrap();
    let expected = reference.then(|| {
        Command::new(&python)
            .arg("-B")
            .arg(&input)
            .current_dir(&root.0)
            .output()
            .unwrap()
    });
    if let Some(output) = &expected {
        save_output(&root.0, "cpython", output);
    }
    let artifact = ryupy_driver::build(
        &mut Session::default(),
        &BuildOptions {
            input,
            out_dir: root.0.join("out"),
            aot: false,
            search_paths: vec![],
        },
    )
    .unwrap();
    let actual = ryupy_driver::dotnet::run_dll(&artifact.dll).unwrap();
    save_output(&root.0, "ryupy", &actual);
    (root, expected, actual)
}

fn save_output(root: &std::path::Path, prefix: &str, output: &Output) {
    std::fs::write(root.join(format!("{prefix}.stdout")), &output.stdout).unwrap();
    std::fs::write(root.join(format!("{prefix}.stderr")), &output.stderr).unwrap();
    std::fs::write(
        root.join(format!("{prefix}.exit")),
        format!("{:?}\n", output.status.code()),
    )
    .unwrap();
}

#[test]
fn bad_zipfile_constructor_alias_and_subclass_match_python() {
    let (_root, expected, actual) = execute(
        include_str!("../../../tests/fixtures/zipfile_exception/core.py"),
        "badzip_core.py",
        true,
    );
    let expected = expected.unwrap();
    assert!(
        expected.status.success(),
        "{}",
        String::from_utf8_lossy(&expected.stderr)
    );
    assert!(
        actual.status.success(),
        "{}",
        String::from_utf8_lossy(&actual.stderr)
    );
    assert_eq!(actual.stdout, expected.stdout);
    assert_eq!(actual.stderr, expected.stderr);
}

#[test]
fn source_zipfile_and_rebound_alias_do_not_gain_static_typed_catch() {
    let unique = std::time::SystemTime::now()
        .duration_since(std::time::UNIX_EPOCH)
        .unwrap()
        .as_nanos();
    let serial = NEXT_ROOT.fetch_add(1, Ordering::Relaxed);
    let path = std::env::temp_dir().join(format!(
        "ryupy-badzip-shadow-{}-{unique}-{serial}",
        std::process::id()
    ));
    std::fs::create_dir(&path).unwrap();
    let root = Root(path);
    let input = root.0.join("main.py");
    std::fs::write(&input, "from zipfile import BadZipFile as Bad\nBad = ValueError\ntry:\n    pass\nexcept Bad:\n    pass\n").unwrap();
    let mut session = Session::default();
    ryupy_driver::dump(&mut session, &input, ryupy_driver::Stage::Mir).expect("重绑Bad是动态目标");
    assert!(!session.diagnostics.has_errors());
    dynamic_support::check(&input, None, &["global.Bad"]);
    for extension in ["hir", "mir"] {
        std::fs::rename(
            root.0.join(format!("dynamic.{extension}")),
            root.0.join(format!("rebound.{extension}")),
        )
        .unwrap();
    }
    std::fs::write(root.0.join("zipfile.py"), "BadZipFile = ValueError\n").unwrap();
    std::fs::write(
        &input,
        "import zipfile\ntry:\n    pass\nexcept zipfile.BadZipFile:\n    pass\n",
    )
    .unwrap();
    let mut session = Session::default();
    ryupy_driver::dump(&mut session, &input, ryupy_driver::Stage::Mir)
        .expect("本地zipfile来源保动态目标");
    assert!(!session.diagnostics.has_errors());
    dynamic_support::check(
        &input,
        Some(("zipfile", "BadZipFile = ValueError\n")),
        &["global.zipfile.BadZipFile"],
    );
}
