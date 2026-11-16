//! len普通类冷长度协议：固定CP双流/完整用户链与原MIR结构，不扩AOT或性能验收。
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
        "ryupy-len-protocol-{}-{stamp}-{serial}",
        std::process::id()
    ));
    // 创建成功才拥有清理权；失败后保留原始证据，不删除碰撞目录。
    std::fs::create_dir(&path).unwrap();
    eprintln!("len protocol evidence: {}", path.display());
    Root(path)
}
fn function<'a>(dump: &'a str, name: &str) -> &'a str {
    let start = dump.find(&format!("fn {name}(")).expect(name);
    let tail = &dump[start..];
    tail.find("\nfn ").map_or(tail, |end| &tail[..end])
}
fn save_output(root: &Root, label: &str, output: &Output) {
    std::fs::write(root.0.join(format!("{label}.stdout")), &output.stdout).unwrap();
    std::fs::write(root.0.join(format!("{label}.stderr")), &output.stderr).unwrap();
    std::fs::write(
        root.0.join(format!("{label}.exit")),
        format!("{:?}\n", output.status.code()),
    )
    .unwrap();
}

// 调用者持有Root直到所有断言完成；CP退出码先守门，再进入真实编译/JIT。
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

// 只投影路径前缀、源码行及caret；全部用户帧、异常消息和链标记都保留。
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
fn len_values_callbacks_and_context_match_python() {
    let (_root, expected, actual) = execute(
        include_str!("../../../tests/fixtures/len_protocol/core.py"),
        "len_core.py",
        0,
    );
    assert!(expected.stderr.is_empty());
    assert_eq!(actual.status.code(), Some(0));
    assert_eq!(actual.stdout, expected.stdout);
    assert_eq!(actual.stderr, expected.stderr);
}

#[test]
fn len_index_callback_preserves_complete_exception_chain() {
    let (_root, expected, actual) = execute(
        include_str!("../../../tests/fixtures/len_protocol/error_boundary.py"),
        "len_error.py",
        1,
    );
    assert_eq!(actual.status.code(), Some(1));
    assert_eq!(actual.stdout, expected.stdout);
    let projected = trace_projection(&expected);
    // 固定完整CP链；len返回后才转换index，不凭空保留已经返回的__len__帧。
    assert_eq!(
        projected,
        vec![
            "Traceback (most recent call last):",
            "File len_error.py, line 17, in <module>",
            "ValueError: outer",
            "During handling of the above exception, another exception occurred:",
            "Traceback (most recent call last):",
            "File len_error.py, line 19, in <module>",
            "File len_error.py, line 15, in measure",
            "File len_error.py, line 9, in __index__",
            "File len_error.py, line 5, in callback",
            "RuntimeError: index failed",
        ]
    );
    assert_eq!(trace_projection(&actual), projected);
}

#[test]
fn len_mir_keeps_complete_calls_and_numeric_loop() {
    let root = root();
    let input = root.0.join("len_shapes.py");
    std::fs::write(
        &input,
        include_str!("../../../tests/fixtures/len_protocol/shapes.py"),
    )
    .unwrap();
    let mut session = Session::default();
    let result = ryupy_driver::dump(&mut session, &input, Stage::Mir);
    std::fs::write(
        root.0.join("diagnostics.txt"),
        format!("{:?}\n", session.diagnostics),
    )
    .unwrap();
    let mir = result.unwrap();
    std::fs::write(root.0.join("mir.txt"), &mir).unwrap();
    assert!(session.diagnostics.is_empty(), "{:?}", session.diagnostics);
    for name in [
        "pair",
        "zero",
        "single",
        "star",
        "keyword",
        "double",
        "exact_list",
        "exact_text",
    ] {
        let body = function(&mir, name);
        assert_eq!(
            body.matches("RuntimeFn::DynBuiltin(const \"len\"").count(),
            1,
            "{body}"
        );
    }
    let pair = function(&mir, "pair");
    let first = pair.find("const \"first\"").unwrap();
    let second = pair.find("const \"second\"").unwrap();
    let binding = pair.find("RuntimeFn::DynBuiltin(const \"len\"").unwrap();
    assert!(first < second && second < binding, "{pair}");
    assert_eq!(pair.matches("const \"first\"").count(), 1, "{pair}");
    assert_eq!(pair.matches("const \"second\"").count(), 1, "{pair}");
    assert!(function(&mir, "keyword").contains("const \"obj\""));
    let literal = function(&mir, "exact_list");
    assert_eq!(literal.matches("MakeList").count(), 1, "{literal}");
    assert!(!literal.contains("ListExtend"), "{literal}");
    assert!(function(&mir, "exact_text").contains("const \"a🙂\""));
    let numeric = function(&mir, "numeric");
    assert!(
        numeric.starts_with("fn numeric(_0: i64) -> i64"),
        "{numeric}"
    );
    assert!(numeric.contains(": i64; // total"), "{numeric}");
    assert!(numeric.contains("branch "), "{numeric}");
    for forbidden in ["dyn.Add", "DynIter", "DynTruthy", "RuntimeFn::DynBuiltin"] {
        assert!(!numeric.contains(forbidden), "{numeric}");
    }
}
