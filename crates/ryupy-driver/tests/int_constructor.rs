//! int绑定/文本解析真实差分与完整实参求值结构；不扩其它构造器。
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
        "ryupy-int-constructor-{}-{unique}-{serial}",
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
fn int_constructor_matches_fixed_python() {
    let (expected, actual) = execute(
        include_str!("../../../tests/fixtures/int_constructor/core.py"),
        "int_core.py",
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
fn int_constructor_error_preserves_context_and_source_frames() {
    let (expected, actual) = execute(
        include_str!("../../../tests/fixtures/int_constructor/error.py"),
        "int_error.py",
        true,
    );
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
    let expected_trace = project(&expected);
    assert_eq!(
        expected_trace,
        vec![
            "Traceback (most recent call last):",
            "File int_error.py, line 5, in <module>",
            "ValueError: original",
            "During handling of the above exception, another exception occurred:",
            "Traceback (most recent call last):",
            "File int_error.py, line 7, in <module>",
            "File int_error.py, line 3, in convert",
            "ValueError: int() base must be >= 2 and <= 36, or 0",
        ]
    );
    assert_eq!(project(&actual), expected_trace);
}

#[test]
fn int_nonunary_mir_keeps_every_argument_before_runtime_binding() {
    let serial = NEXT_ROOT.fetch_add(1, Ordering::Relaxed);
    let path = std::env::temp_dir().join(format!(
        "ryupy-int-shapes-{}-{serial}-{}",
        std::process::id(),
        std::time::SystemTime::now()
            .duration_since(std::time::UNIX_EPOCH)
            .unwrap()
            .as_nanos()
    ));
    std::fs::create_dir(&path).unwrap();
    let root = Root(path);
    let input = root.0.join("shapes.py");
    std::fs::write(&input, "def mark(label, value):\n    print(label)\n    return value\ndef pair():\n    return int(mark('first-input', '10'), mark('second-input', 16))\ndef many():\n    return int(mark('first-extra', '10'), mark('second-extra', 16), mark('third-extra', 8))\ndef zero():\n    return int()\ndef single_text() -> int:\n    return int('12')\ndef single_dynamic(value):\n    return int(value)\ndef numeric(n: int) -> int:\n    return int(n) + 1\n").unwrap();
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
            .find("RuntimeFn::DynBuiltin(const \"int\"")
            .expect(body);
        let mut previous = 0;
        for label in labels {
            assert_eq!(body.matches(label).count(), 1, "{body}");
            let offset = body.find(label).unwrap();
            assert!(offset >= previous && offset < binder, "{body}");
            previous = offset;
        }
        assert!(!body.contains("RuntimeFn::DynToInt"), "{body}");
    }
    assert!(function("zero").contains("RuntimeFn::DynBuiltin(const \"int\""));
    for name in ["single_text", "single_dynamic"] {
        let body = function(name);
        assert!(body.contains("RuntimeFn::DynToInt"), "{body}");
    }
    let numeric = function("numeric");
    assert!(
        numeric.starts_with("fn numeric(_0: i64) -> i64"),
        "{numeric}"
    );
    assert!(!numeric.contains("RuntimeFn::DynBuiltin"), "{numeric}");
    assert!(!numeric.contains("RuntimeFn::DynToInt"), "{numeric}");
}
