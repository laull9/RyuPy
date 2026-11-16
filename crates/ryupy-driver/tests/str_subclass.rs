//! 字符串子类公共对象结果及精确primitive快路；真实差分待同轮payload齐套。
use ryupy_driver::{BuildOptions, Session, Stage};
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
fn root() -> Root {
    let stamp = std::time::SystemTime::now()
        .duration_since(std::time::UNIX_EPOCH)
        .unwrap()
        .as_nanos();
    let serial = NEXT_ROOT.fetch_add(1, Ordering::Relaxed);
    let path = std::env::temp_dir().join(format!(
        "ryupy-str-subclass-{}-{stamp}-{serial}",
        std::process::id()
    ));
    std::fs::create_dir(&path).unwrap();
    Root(path)
}
fn function<'a>(dump: &'a str, name: &str) -> &'a str {
    let start = dump.find(&format!("fn {name}(")).expect(name);
    let tail = &dump[start..];
    tail.find("\nfn ").map_or(tail, |end| &tail[..end])
}

#[test]
fn str_object_calls_keep_proven_fast_paths_and_complete_unknown_arguments() {
    let root = root();
    let input = root.0.join("str_shapes.py");
    std::fs::write(&input, "def numeric(value: int) -> str:\n    return str(value)\ndef floating(value: float) -> str:\n    return str(value)\ndef boolean(value: bool) -> str:\n    return str(value)\ndef literal() -> str:\n    return str('hello')\ndef none_value() -> str:\n    return str(None)\ndef opaque(value: str) -> str:\n    return str(value)\ndef unknown(value):\n    return str(value)\ndef zero() -> str:\n    return str()\ndef many() -> str:\n    return str(11, 22, 33)\ndef keyword(value: int) -> str:\n    return str(object=value)\ndef star(value: int) -> str:\n    return str(*[value])\ndef double(value: int) -> str:\n    return str(value, **{})\ndef side(value):\n    return value\ndef ordered():\n    return str(side('first'), side('second'))\ndef saved(value):\n    convert = str\n    return convert(value)\ndef literals(unused):\n    return (str(1), str(184467440737095516160), str(2.5), str(True), str('x'), str(None))\n").unwrap();
    let mut session = Session::default();
    let output = ryupy_driver::dump(&mut session, &input, Stage::Mir).unwrap();
    std::fs::write(root.0.join("mir.txt"), &output).unwrap();
    assert!(session.diagnostics.is_empty(), "{:?}", session.diagnostics);
    for name in ["numeric", "floating", "boolean", "literal", "none_value"] {
        let text = function(&output, name);
        assert_eq!(text.matches("RuntimeFn::DynStr(").count(), 1, "{text}");
        assert!(!text.contains("RuntimeFn::DynBuiltin"), "{text}");
        assert!(!text.contains("RuntimeFn::CastToStr"), "{text}");
    }
    assert!(function(&output, "numeric").contains("_0: i64"));
    assert!(function(&output, "floating").contains("_0: f64"));
    assert!(function(&output, "boolean").contains("_0: bool"));
    for name in [
        "opaque", "unknown", "zero", "many", "keyword", "star", "double", "ordered",
    ] {
        let text = function(&output, name);
        assert!(text.contains("-> dyn"), "{text}");
        assert_eq!(
            text.matches("RuntimeFn::DynBuiltin(const \"str\"").count(),
            1,
            "{text}"
        );
        assert!(!text.contains("RuntimeFn::DynStr("), "{text}");
        assert!(!text.contains("RuntimeFn::CastToStr"), "{text}");
    }
    let many = function(&output, "many");
    for value in [11, 22, 33] {
        assert_eq!(
            many.matches(&format!("const {value}_i64")).count(),
            1,
            "{many}"
        );
    }
    assert!(function(&output, "keyword").contains("const \"object\""));
    let ordered = function(&output, "ordered");
    assert!(ordered.find("const \"first\"").unwrap() < ordered.find("const \"second\"").unwrap());
    assert!(
        ordered.find("const \"second\"").unwrap()
            < ordered.find("RuntimeFn::DynBuiltin(const \"str\"").unwrap()
    );
    let saved = function(&output, "saved");
    // Dynamic 的独立 owner 使用槽 2；业务 value/convert 仍固定在槽 0/1。
    assert!(
        saved.contains("let _2: dyn; // __function_owner"),
        "{saved}"
    );
    assert!(
        saved.contains(
            "_3 = RuntimeFn::TracePushWithOrigin(const \"str_shapes.py\", const \"saved\", _2);"
        ),
        "{saved}"
    );
    assert!(
        saved.contains("RuntimeFn::TypeObject(const \"str\")"),
        "{saved}"
    );
    assert!(
        saved.contains("_4 = RuntimeFn::TypeObject(const \"str\");"),
        "{saved}"
    );
    assert!(saved.contains("_1 = _4;"), "{saved}");
    assert_eq!(saved.matches("dyn.call(").count(), 1, "{saved}");
    assert!(saved.contains("dyn.call(_1, [_0]);"), "{saved}");
    assert!(!saved.contains("RuntimeFn::DynStr("), "{saved}");
    // 只有不可覆写的HIR字面量例外；同一Dynamic函数里的未知局部仍走binder。
    let literals = function(&output, "literals");
    assert_eq!(
        literals.matches("RuntimeFn::DynStr(").count(),
        6,
        "{literals}"
    );
    assert!(!literals.contains("RuntimeFn::DynBuiltin"), "{literals}");
}

// 原始双流与退出码先落盘；Root交给调用者持有直到所有比较结束。
fn save_output(root: &Root, label: &str, output: &Output) {
    std::fs::write(root.0.join(format!("{label}.stdout")), &output.stdout).unwrap();
    std::fs::write(root.0.join(format!("{label}.stderr")), &output.stderr).unwrap();
    std::fs::write(
        root.0.join(format!("{label}.exit")),
        format!("{:?}\n", output.status.code()),
    )
    .unwrap();
}
fn execute(source: &str, filename: &str, expected_exit: i32) -> (Root, Output, Output) {
    let root = root();
    let python = std::env::var_os("PYTHON").unwrap_or_else(|| "python3".into());
    let version = Command::new(&python).args(["-c", "import sys; assert sys.implementation.name == 'cpython'; print('.'.join(map(str, sys.version_info[:3])))"]).output().unwrap();
    save_output(&root, "python-version", &version);
    assert!(version.status.success());
    let pinned = std::fs::read_to_string(
        ryupy_driver::runtime::workspace_root().join("tests/pinned-python.txt"),
    )
    .unwrap();
    assert_eq!(
        String::from_utf8_lossy(&version.stdout).trim(),
        pinned.trim()
    );
    let input = root.0.join(filename);
    std::fs::write(&input, source).unwrap();
    let expected = Command::new(&python)
        .arg("-B")
        .arg(&input)
        .current_dir(&root.0)
        .output()
        .unwrap();
    save_output(&root, "cpython", &expected);
    assert_eq!(
        expected.status.code(),
        Some(expected_exit),
        "CP reference failed: {}",
        String::from_utf8_lossy(&expected.stderr)
    );
    let mut session = Session::default();
    let result = ryupy_driver::build(
        &mut session,
        &BuildOptions {
            input,
            out_dir: root.0.join("out"),
            aot: false,
            search_paths: vec![],
        },
    );
    std::fs::write(
        root.0.join("diagnostics.txt"),
        format!("{:?}\n", session.diagnostics),
    )
    .unwrap();
    let artifact = result.unwrap();
    let actual = ryupy_driver::dotnet::run_dll(&artifact.dll).unwrap();
    save_output(&root, "ryupy", &actual);
    (root, expected, actual)
}
fn assert_success(expected: &Output, actual: &Output) {
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
    assert_eq!(actual.status.code(), expected.status.code());
    assert_eq!(actual.stdout, expected.stdout);
    assert_eq!(actual.stderr, expected.stderr);
}

#[test]
fn str_subclass_construction_identity_and_publication_match_python() {
    let (_root, expected, actual) = execute(
        include_str!("../../../tests/fixtures/str_subclass/core.py"),
        "str_core.py",
        0,
    );
    assert_success(&expected, &actual);
}

#[test]
fn str_subclass_methods_and_raw_consumers_match_python() {
    let (_root, expected, actual) = execute(
        include_str!("../../../tests/fixtures/str_subclass/methods.py"),
        "str_methods.py",
        0,
    );
    assert_success(&expected, &actual);
}

// 仅忽略源码展示、caret及路径前缀；全部File帧和非缩进异常/链标记均保留。
fn trace_projection(output: &Output) -> Vec<String> {
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
}

#[test]
fn str_subclass_numeric_binding_and_error_chain_match_python() {
    let (_root, expected, actual) = execute(
        include_str!("../../../tests/fixtures/str_subclass/error_boundary.py"),
        "str_error.py",
        1,
    );
    assert_eq!(expected.status.code(), Some(1));
    assert_eq!(actual.status.code(), Some(1));
    assert_eq!(actual.stdout, expected.stdout);
    let projected = trace_projection(&expected);
    assert_eq!(
        projected,
        vec![
            "Traceback (most recent call last):",
            "File str_error.py, line 71, in <module>",
            "ValueError: outer",
            "During handling of the above exception, another exception occurred:",
            "Traceback (most recent call last):",
            "File str_error.py, line 73, in <module>",
            "File str_error.py, line 67, in convert",
            "TypeError: __str__ returned non-string (type int)",
        ]
    );
    assert_eq!(trace_projection(&actual), projected);
}
