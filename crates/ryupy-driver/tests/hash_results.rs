//! 用户hash返回归约的三项真实CP差分：数值与类型、混键与副作用、完整异常链。
//! 不新增编译ABI，不把Hashable ABC或旧容器hash副作用边界计为本切完成。
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

fn root() -> Root {
    let stamp = std::time::SystemTime::now()
        .duration_since(std::time::UNIX_EPOCH)
        .unwrap()
        .as_nanos();
    let serial = NEXT_ROOT.fetch_add(1, Ordering::Relaxed);
    let path = std::env::temp_dir().join(format!(
        "ryupy-hash-results-{}-{stamp}-{serial}",
        std::process::id()
    ));
    // 创建成功后才拥有清理权；失败保原始证据，不能删除碰撞目录。
    std::fs::create_dir(&path).unwrap();
    eprintln!("hash result evidence: {}", path.display());
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

#[test]
fn hash_result_values_and_rejected_conversions_match_python() {
    equal(
        include_str!("../../../tests/fixtures/hash_slots/hash_result_values.py"),
        "hash_result_values.py",
    );
}

#[test]
fn hash_result_mixed_keys_and_callback_identity_match_python() {
    equal(
        include_str!("../../../tests/fixtures/hash_slots/hash_result_containers.py"),
        "hash_result_containers.py",
    );
}

// 与冻结源码逐行对应；CP先验证全部帧和context，之后才build同一源码。
fn check_error_reference(expected: &Output) {
    assert!(expected.stdout.is_empty());
    assert_eq!(
        trace_projection(expected),
        vec![
            "Traceback (most recent call last):",
            "File hash_result_error.py, line 8, in <module>",
            "ValueError: active",
            "During handling of the above exception, another exception occurred:",
            "Traceback (most recent call last):",
            "File hash_result_error.py, line 10, in <module>",
            "File hash_result_error.py, line 6, in caller",
            "File hash_result_error.py, line 4, in __hash__",
            "RuntimeError: hash-body",
        ]
    );
}

#[test]
fn hash_result_callback_preserves_complete_user_exception_chain() {
    let (_root, expected, actual) = execute(
        include_str!("../../../tests/fixtures/hash_slots/hash_result_error.py"),
        "hash_result_error.py",
        1,
        check_error_reference,
    );
    assert_eq!(actual.status.code(), Some(1));
    assert_eq!(actual.stdout, expected.stdout);
    assert_eq!(trace_projection(&actual), trace_projection(&expected));
}
