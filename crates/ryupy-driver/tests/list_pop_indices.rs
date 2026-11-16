//! exact list.pop索引协议：三个真实CP/JIT应用，保留首次基线与完整用户异常链。
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

// 每次应用独占目录；失败保留原source、双流、退出码及build诊断，不覆盖首次证据。
fn root() -> Root {
    let stamp = std::time::SystemTime::now()
        .duration_since(std::time::UNIX_EPOCH)
        .unwrap()
        .as_nanos();
    let serial = NEXT_ROOT.fetch_add(1, Ordering::Relaxed);
    let path = std::env::temp_dir().join(format!(
        "ryupy-list-pop-indices-{}-{stamp}-{serial}",
        std::process::id()
    ));
    std::fs::create_dir(&path).unwrap();
    eprintln!("list pop indices evidence: {}", path.display());
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

fn execute(
    source: &str,
    filename: &str,
    expected_exit: i32,
    expected_trace: Option<&[&str]>,
) -> (Root, Output, Output) {
    let root = root();
    let input = root.0.join(filename);
    std::fs::write(&input, source).unwrap();
    let python = std::env::var_os("PYTHON").unwrap_or_else(|| "python3".into());
    let version = Command::new(&python)
        .args([
            "-c",
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
    if let Some(trace) = expected_trace {
        assert_eq!(trace_projection(&expected), trace);
        assert_eq!(expected.stdout, b"callback ['index']\n");
    }
    // 正常driver默认优化流水线，不手造MIR/PE，不改变泛用编译或运行时配置。
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

// 仅去掉路径、每个File帧后的源码展示及caret；未知行保留，不能吞掉额外用户帧。
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
fn wide_indices_and_protocol_match_python_without_accidental_removal() {
    let (_root, expected, actual) = execute(
        include_str!("../../../tests/fixtures/list_pop_indices/wide_and_protocol.py"),
        "list_pop_wide.py",
        0,
        None,
    );
    assert!(expected.stdout.ends_with(b"WIDE_PROTOCOL_COMPLETED\n"));
    assert_success(&expected, &actual);
}

#[test]
fn index_order_mutation_identity_and_prepared_yield_match_python() {
    let (_root, expected, actual) = execute(
        include_str!("../../../tests/fixtures/list_pop_indices/order_and_yield.py"),
        "list_pop_order.py",
        0,
        None,
    );
    assert!(expected.stdout.ends_with(b"ORDER_YIELD_COMPLETED\n"));
    assert_success(&expected, &actual);
}

#[test]
fn index_callback_keeps_complete_user_context_chain() {
    let (_root, expected, actual) = execute(
        include_str!("../../../tests/fixtures/list_pop_indices/error_chain.py"),
        "list_pop_error.py",
        1,
        Some(EXPECTED_TRACE),
    );
    assert_eq!(actual.status.code(), expected.status.code());
    assert_eq!(actual.stdout, expected.stdout);
    // CP在build前先核完整冻结链；JIT必须保留相同全部用户帧。
    let projected = trace_projection(&expected);
    assert_eq!(trace_projection(&actual), projected);
}

// 行号取自固定fixture；首次真实driver执行先核CP，不相等即保留证据并停止，绝不接受JIT结果。
const EXPECTED_TRACE: &[&str] = &[
    "Traceback (most recent call last):",
    "File list_pop_error.py, line 21, in <module>",
    "ValueError: outer context",
    "During handling of the above exception, another exception occurred:",
    "Traceback (most recent call last):",
    "File list_pop_error.py, line 23, in <module>",
    "File list_pop_error.py, line 17, in outer",
    "File list_pop_error.py, line 13, in middle",
    "File list_pop_error.py, line 9, in __index__",
    "LookupError: index chain failed",
];
