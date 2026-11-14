//! 内建format四项真实CP差分：协议、绑定、文本整数与完整异常链。
//! 只注册Dynamic内建，不改fstring ABI或共享CALL债。
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
        "ryupy-format-builtin-{}-{stamp}-{serial}",
        std::process::id()
    ));
    // 创建成功后才拥有清理权；失败保原始证据，不能删除碰撞目录。
    std::fs::create_dir(&path).unwrap();
    eprintln!("format evidence: {}", path.display());
    Root(path)
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

// 先验固定CP退出码，之后才编译同一源码；调用者持guard直到全部断言完成。
fn execute(
    source: &str,
    filename: &str,
    expected_exit: i32,
    oracle: fn(&Output),
) -> (Root, Output, Output) {
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
    // 固定CP源码合同先守门，不能让错误参考继续进入构建或放宽用户帧。
    oracle(&expected);
    if filename == "binding.py" {
        check_mir(&root, &input);
    }
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

fn equal(source: &str, filename: &str) {
    let (_root, expected, actual) = execute(source, filename, 0, |output| {
        assert!(output.stderr.is_empty());
    });
    assert!(expected.stderr.is_empty());
    assert_eq!(actual.status.code(), Some(0));
    assert_eq!(actual.stdout, expected.stdout);
    assert_eq!(actual.stderr, expected.stderr);
}

// 仅忽略路径前缀、源码回显及caret；全部用户帧、异常消息和context链标记参与比较。
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

fn function<'a>(dump: &'a str, name: &str) -> &'a str {
    let start = dump.find(&format!("fn {name}(")).expect(name);
    let tail = &dump[start..];
    tail.find("\nfn ").map_or(tail, |end| &tail[..end])
}

// 仍在同一Root生命周期内，且CP真实成功后才检查MIR和构建。
fn check_mir(root: &Root, input: &std::path::Path) {
    let mut session = Session::default();
    let result = ryupy_driver::dump(&mut session, input, Stage::Mir);
    std::fs::write(
        root.0.join("mir-diagnostics.txt"),
        format!("{:?}\n", session.diagnostics),
    )
    .unwrap();
    let mir = result.unwrap();
    std::fs::write(root.0.join("mir.txt"), &mir).unwrap();
    assert!(session.diagnostics.is_empty(), "{:?}", session.diagnostics);
    assert!(
        mir.contains("RuntimeFn::DynBuiltin(const \"format\""),
        "{mir}"
    );
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

#[test]
fn format_protocol_preserves_type_slots_and_identity() {
    equal(
        include_str!("../../../tests/fixtures/format_builtin/protocol.py"),
        "protocol.py",
    );
}

#[test]
fn format_binding_aliases_and_numeric_mir_match_python() {
    equal(
        include_str!("../../../tests/fixtures/format_builtin/binding.py"),
        "binding.py",
    );
}

#[test]
fn format_text_integer_supported_subset_matches_python() {
    equal(
        include_str!("../../../tests/fixtures/format_builtin/text_integer.py"),
        "text_integer.py",
    );
}

// 按冻结源行号预先守门；这是待真实CP确认的合同，不是声称已执行的golden。
fn check_error_reference(expected: &Output) {
    assert_eq!(expected.stdout, b"['slot', 'finally']\n");
    assert_eq!(
        trace_projection(expected),
        vec![
            "Traceback (most recent call last):",
            "File traceback.py, line 5, in <module>",
            "ValueError: format-inner",
            "The above exception was the direct cause of the following exception:",
            "Traceback (most recent call last):",
            "File traceback.py, line 24, in <module>",
            "File traceback.py, line 22, in caller",
            "File traceback.py, line 16, in convert",
            "File traceback.py, line 12, in __format__",
            "RuntimeError: format-outer",
        ]
    );
}

#[test]
fn format_preserves_complete_user_traceback_and_cause() {
    let (_root, expected, actual) = execute(
        include_str!("../../../tests/fixtures/format_builtin/traceback.py"),
        "traceback.py",
        1,
        check_error_reference,
    );
    assert_eq!(actual.status.code(), Some(1));
    assert_eq!(actual.stdout, expected.stdout);
    assert_eq!(trace_projection(&actual), trace_projection(&expected));
}

#[test]
fn format_general_float_matches_python() {
    equal(
        include_str!("../../../tests/fixtures/format_builtin/general_float.py"),
        "general_float.py",
    );
}
