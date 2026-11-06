//! exact set.clear 有限差分：三应用先核固定 CP，再正常编译/JIT；成功失败均保原流。
//! 完整参考与独立 pending 不进入通过分母，不将只读结构检查算成应用执行。
use ryupy_driver::{BuildOptions, Session};
use std::path::PathBuf;
use std::process::{Command, Output};
use std::sync::atomic::{AtomicU64, Ordering};

static NEXT_ROOT: AtomicU64 = AtomicU64::new(0);

// 不实现 Drop 清理：成功/失败原证据均保留，外部归档之后由证据所有者清理。
struct Root(PathBuf);
fn root() -> Root {
    let stamp = std::time::SystemTime::now()
        .duration_since(std::time::UNIX_EPOCH)
        .unwrap()
        .as_nanos();
    let serial = NEXT_ROOT.fetch_add(1, Ordering::Relaxed);
    let path = std::env::temp_dir().join(format!(
        "ryupy-set-clear-{}-{stamp}-{serial}",
        std::process::id()
    ));
    std::fs::create_dir(&path).unwrap();
    eprintln!("set clear evidence: {}", path.display());
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

// 先比较 CP 的完整输出/退出/链；任一冻结合同失败均不得继续编译目标。
fn execute(
    source: &str,
    filename: &str,
    expected_exit: i32,
    expected_stdout: &[u8],
    expected_trace: Option<&[&str]>,
) -> (Root, Output, Output) {
    let root = root();
    let input = root.0.join(filename);
    std::fs::write(&input, source).unwrap();
    std::fs::write(root.0.join("expected.stdout"), expected_stdout).unwrap();
    if let Some(trace) = expected_trace {
        std::fs::write(root.0.join("expected.trace"), trace.join("\n") + "\n").unwrap();
    }
    let python = std::env::var_os("PYTHON").unwrap_or_else(|| "python3".into());
    std::fs::write(root.0.join("python.path.txt"), format!("{python:?}\n")).unwrap();
    let version = Command::new(&python)
        .args([
            "-I", "-S", "-B", "-c",
            "import sys; assert sys.implementation.name == 'cpython'; print('.'.join(map(str, sys.version_info[:3])))",
        ])
        .output()
        .unwrap();
    save_output(&root, "python-version", &version);
    assert!(version.status.success());
    assert!(version.stderr.is_empty());
    let pinned = std::fs::read_to_string(
        ryupy_driver::runtime::workspace_root().join("tests/pinned-python.txt"),
    )
    .unwrap();
    assert_eq!(pinned.trim(), "3.12.13");
    assert_eq!(
        String::from_utf8_lossy(&version.stdout).trim(),
        pinned.trim()
    );
    let expected = Command::new(&python)
        .args(["-I", "-S", "-B"])
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
    assert_eq!(expected.stdout, expected_stdout);
    if let Some(trace) = expected_trace {
        assert_eq!(trace_projection(&expected), trace);
    } else {
        assert!(expected.stderr.is_empty());
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
    std::fs::write(
        root.0.join("build.result.txt"),
        match &result {
            Ok(_) => "ok\n".to_owned(),
            Err(error) => format!("error: {error:#}\n"),
        },
    )
    .unwrap();
    let artifact = result.unwrap();
    let result = ryupy_driver::dotnet::run_dll(&artifact.dll);
    std::fs::write(
        root.0.join("jit.result.txt"),
        match &result {
            Ok(_) => "started\n".to_owned(),
            Err(error) => format!("error: {error:#}\n"),
        },
    )
    .unwrap();
    let actual = result.unwrap();
    save_output(&root, "ryupy", &actual);
    (root, expected, actual)
}

fn assert_success(expected: &Output, actual: &Output) {
    assert!(
        actual.status.success(),
        "{}",
        String::from_utf8_lossy(&actual.stderr)
    );
    assert_eq!(actual.status.code(), expected.status.code());
    assert_eq!(actual.stdout, expected.stdout);
    assert_eq!(actual.stderr, expected.stderr);
}

// 仅剥路径、各 File 帧后的源码展示、caret 与空行；未知行保留，全部用户帧参与比较。
fn trace_projection(output: &Output) -> Vec<String> {
    let text = String::from_utf8_lossy(&output.stderr);
    assert!(!text.contains("Unhandled exception"), "{text}");
    let mut source_line_allowed = false;
    text.lines()
        .filter_map(|raw| {
            let line = raw.trim();
            if let Some(file) = line.strip_prefix("File \"") {
                let (path, position) = file.rsplit_once("\", line ").expect(line);
                let basename = path.rsplit(['/', '\\']).next().unwrap();
                source_line_allowed = true;
                Some(format!("File {basename}, line {position}"))
            } else if source_line_allowed && raw.starts_with("    ") {
                source_line_allowed = false;
                None
            } else if line.is_empty()
                || (raw.starts_with("    ")
                    && line
                        .chars()
                        .all(|character| matches!(character, '^' | '~' | ' ')))
            {
                None
            } else {
                source_line_allowed = false;
                Some(line.to_owned())
            }
        })
        .collect()
}

#[test]
fn set_clear_binding_identity_and_no_hash_match_python() {
    let (_root, expected, actual) = execute(
        include_str!("../../../tests/fixtures/set_clear/bindings_and_identity.py"),
        "set_clear_bindings.py",
        0,
        include_bytes!("../../../tests/fixtures/set_clear/bindings.reference.stdout"),
        None,
    );
    assert_success(&expected, &actual);
}

#[test]
fn set_clear_evaluation_and_live_iterator_states_match_python() {
    let (_root, expected, actual) = execute(
        include_str!("../../../tests/fixtures/set_clear/evaluation_and_iterators.py"),
        "set_clear_evaluation.py",
        0,
        include_bytes!("../../../tests/fixtures/set_clear/evaluation.reference.stdout"),
        None,
    );
    assert_success(&expected, &actual);
}

#[test]
fn set_clear_binding_error_keeps_complete_user_context_chain() {
    let (_root, expected, actual) = execute(
        include_str!("../../../tests/fixtures/set_clear/error_chain.py"),
        "set_clear_error.py",
        1,
        b"after failure [10, 20]\n",
        Some(EXPECTED_TRACE),
    );
    assert_eq!(actual.status.code(), expected.status.code());
    assert_eq!(actual.stdout, expected.stdout);
    assert_eq!(trace_projection(&actual), trace_projection(&expected));
}

// 新 fixture 行号来自源码；首次真正执行先核 CP，不符即停，不从 JIT 反推预期。
const EXPECTED_TRACE: &[&str] = &[
    "Traceback (most recent call last):",
    "File set_clear_error.py, line 13, in <module>",
    "ValueError: outer context",
    "During handling of the above exception, another exception occurred:",
    "Traceback (most recent call last):",
    "File set_clear_error.py, line 16, in <module>",
    "File set_clear_error.py, line 8, in outer",
    "File set_clear_error.py, line 4, in inner",
    "TypeError: set.clear() takes no keyword arguments",
];
