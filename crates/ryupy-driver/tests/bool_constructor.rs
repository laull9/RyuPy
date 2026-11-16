//! bool完整绑定与共享真值协议；编译生产路径不变。
use ryupy_driver::{BuildOptions, Session};
use std::path::PathBuf;
use std::process::{Command, Output};
use std::sync::atomic::{AtomicU64, Ordering};

static NEXT_ROOT: AtomicU64 = AtomicU64::new(0);

struct Root(PathBuf);
impl Drop for Root {
    fn drop(&mut self) {
        if !std::thread::panicking() {
            let _ = std::fs::remove_dir_all(&self.0);
        }
    }
}
fn execute(source: &str, filename: &str, reference: bool) -> (Option<Output>, Output) {
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
        "ryupy-bool-constructor-{}-{unique}-{serial}",
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
    (expected, actual)
}

#[test]
fn bool_constructor_matches_fixed_python() {
    let (expected, actual) = execute(
        include_str!("../../../tests/fixtures/bool_constructor/core.py"),
        "bool_core.py",
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
fn bool_errors_preserve_complete_context_and_source_frames() {
    for (source, filename, original_line, caller_line, inner_line, original, failure) in [
        (
            include_str!("../../../tests/fixtures/bool_constructor/error.py"),
            "bool_error.py",
            5,
            7,
            3,
            "ValueError: original",
            "TypeError: bool expected at most 1 argument, got 2",
        ),
        (
            include_str!("../../../tests/fixtures/bool_constructor/length_error.py"),
            "length_error.py",
            7,
            9,
            5,
            "TypeError: original",
            "ValueError: __len__() should return >= 0",
        ),
    ] {
        let (expected, actual) = execute(source, filename, true);
        let expected = expected.unwrap();
        assert_eq!(expected.status.code(), Some(1));
        assert_eq!(actual.status.code(), Some(1));
        assert_eq!(actual.stdout, expected.stdout);
        let project = |output: &Output| -> Vec<String> {
            let text = String::from_utf8_lossy(&output.stderr);
            assert!(!text.contains("Unhandled exception"), "{text}");
            text.lines()
                .filter_map(|raw| {
                    let line = raw.trim();
                    if let Some(file) = line.strip_prefix("File \"") {
                        let (path, position) = file.rsplit_once("\", line ").expect(line);
                        let basename = path.rsplit(['/', '\\']).next().unwrap();
                        Some(format!("File {basename}, line {position}"))
                    } else if !line.is_empty() && !raw.starts_with(char::is_whitespace) {
                        Some(line.to_owned())
                    } else {
                        None
                    }
                })
                .collect()
        };
        let fixed = vec![
            "Traceback (most recent call last):".to_owned(),
            format!("File {filename}, line {original_line}, in <module>"),
            original.to_owned(),
            "During handling of the above exception, another exception occurred:".to_owned(),
            "Traceback (most recent call last):".to_owned(),
            format!("File {filename}, line {caller_line}, in <module>"),
            format!("File {filename}, line {inner_line}, in convert"),
            failure.to_owned(),
        ];
        assert_eq!(project(&expected), fixed);
        assert_eq!(project(&actual), fixed);
    }
}

#[test]
fn bool_mir_keeps_complete_calls_and_static_numeric_conditions() {
    let serial = NEXT_ROOT.fetch_add(1, Ordering::Relaxed);
    let path = std::env::temp_dir().join(format!(
        "ryupy-bool-shapes-{}-{serial}-{}",
        std::process::id(),
        std::time::SystemTime::now()
            .duration_since(std::time::UNIX_EPOCH)
            .unwrap()
            .as_nanos()
    ));
    std::fs::create_dir(&path).unwrap();
    let root = Root(path);
    let input = root.0.join("shapes.py");
    std::fs::write(&input, "def mark(label, value):\n    print(label)\n    return value\ndef pair():\n    return bool(mark('first-input', 0), mark('second-input', 1))\ndef many():\n    return bool(mark('first-extra', 0), mark('second-extra', 1), mark('third-extra', 2))\ndef zero():\n    return bool()\ndef single(value):\n    return bool(value)\ndef star(value):\n    return bool(*value)\ndef keyword(value):\n    return bool(x=value)\ndef double(value):\n    return bool(value, **{})\ndef alias():\n    return bool\ndef numeric(n: int) -> int:\n    if n:\n        return 1\n    return 0\ndef floating(n: float) -> int:\n    if n:\n        return 1\n    return 0\ndef boolean(n: bool) -> int:\n    if n:\n        return 1\n    return 0\n").unwrap();
    let mut session = Session::default();
    let mir = ryupy_driver::dump(&mut session, &input, ryupy_driver::Stage::Mir).unwrap();
    assert!(session.diagnostics.is_empty());
    let function = |name: &str| -> &str {
        let start = mir.find(&format!("fn {name}(")).unwrap();
        let tail = &mir[start..];
        &tail[..tail.find("\n}\n").unwrap() + 3]
    };
    for (name, labels) in [
        ("pair", vec!["first-input", "second-input"]),
        ("many", vec!["first-extra", "second-extra", "third-extra"]),
    ] {
        let body = function(name);
        let binder = body
            .find("RuntimeFn::DynBuiltin(const \"bool\"")
            .expect(body);
        let mut previous = 0;
        for label in labels {
            assert_eq!(body.matches(label).count(), 1, "{body}");
            let offset = body.find(label).unwrap();
            assert!(offset >= previous && offset < binder, "{body}");
            previous = offset;
        }
        assert!(!body.contains("RuntimeFn::DynTruthy"), "{body}");
    }
    for name in ["zero", "single", "star", "keyword", "double"] {
        let body = function(name);
        assert!(
            body.contains("RuntimeFn::DynBuiltin(const \"bool\""),
            "{body}"
        );
    }
    assert!(function("alias").contains("RuntimeFn::TypeObject(const \"bool\""));
    for (name, signature) in [
        ("numeric", "fn numeric(_0: i64) -> i64"),
        ("floating", "fn floating(_0: f64) -> i64"),
        ("boolean", "fn boolean(_0: bool) -> i64"),
    ] {
        let body = function(name);
        assert!(body.starts_with(signature), "{body}");
        assert!(!body.contains("RuntimeFn::DynTruthy"), "{body}");
        assert!(!body.contains("RuntimeFn::DynBuiltin"), "{body}");
    }
    assert!(function("numeric").contains("Ne("));
    assert!(function("floating").contains("Ne("));
}
