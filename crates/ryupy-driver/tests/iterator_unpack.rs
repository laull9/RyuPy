//! 非星号解包：保精确字面量快路，并以固定CP双流和完整异常链约束真实JIT。
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
        "ryupy-iterator-unpack-{}-{stamp}-{serial}",
        std::process::id()
    ));
    std::fs::create_dir(&path).unwrap();
    eprintln!("iterator unpack evidence: {}", path.display());
    Root(path)
}
fn function<'a>(dump: &'a str, name: &str) -> &'a str {
    let start = dump.find(&format!("fn {name}(")).expect(name);
    let tail = &dump[start..];
    tail.find("\nfn ").map_or(tail, |end| &tail[..end])
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
fn literal_fast_paths_and_shared_unpack_structure_remain_bounded() {
    let root = root();
    let input = root.0.join("unpack_shapes.py");
    let source = "def swap(a, b):\n    a, b = b, a\n    return a + b\ndef direct_list(a, b):\n    a, b = [b, a]\n    return a + b\ndef bind(target, values):\n    target.first, target[0] = values\ndef star(values):\n    a, *b = values\n    return b\ndef chain(values):\n    whole = a, b = values\n    return whole\ndef loop(values):\n    for a, b in values:\n        print(a, b)\ndef comprehension(values):\n    return [a + b for a, b in values]\ndef entered(manager):\n    with manager as (a, b):\n        print(a, b)\ndef paused(target, values):\n    target.first, target[(yield 0)] = values\n";
    std::fs::write(&input, source).unwrap();
    let mut session = Session::default();
    let result = ryupy_driver::dump(&mut session, &input, Stage::Mir);
    std::fs::write(
        root.0.join("diagnostics.txt"),
        format!("{:?}", session.diagnostics),
    )
    .unwrap();
    let mir = result.unwrap();
    std::fs::write(root.0.join("mir.txt"), &mir).unwrap();
    assert!(session.diagnostics.is_empty());
    let swap = function(&mir, "swap");
    for forbidden in ["MakeTuple", "GetItem", "UnpackCheck", "UnpackSequence"] {
        assert!(!swap.contains(forbidden), "{swap}");
    }
    let list = function(&mir, "direct_list");
    assert!(!list.contains("UnpackSequence"), "{list}");
    assert!(list.contains("UnpackCheck"), "{list}");
    let star = function(&mir, "star");
    assert!(!star.contains("UnpackSequence"), "{star}");
    assert!(!star.contains("UnpackCheck"), "{star}");
    assert_eq!(star.matches("RuntimeFn::UnpackEx(").count(), 1, "{star}");
    for name in ["bind", "chain", "loop", "entered"] {
        let text = function(&mir, name);
        assert_eq!(
            text.matches("RuntimeFn::UnpackSequence(").count(),
            1,
            "{text}"
        );
        assert!(!text.contains("RuntimeFn::Len("), "{text}");
    }
    let bind = function(&mir, "bind");
    let helper = bind.find("RuntimeFn::UnpackSequence(").unwrap();
    let last_read = bind.rfind("RuntimeFn::GetItem(").unwrap();
    let first_store = bind.find("RuntimeFn::SetAttr(").unwrap();
    let second_store = bind.find("RuntimeFn::SetItem(").unwrap();
    assert!(
        helper < last_read && last_read < first_store && first_store < second_store,
        "{bind}"
    );
    // 暂停工厂与恢复体由既有生成器降级拆分；全文件只增加该站点的一次helper。
    // 推导式隐式函数另有一个站点，不能只数外层函数而漏掉真正循环体。
    assert_eq!(
        mir.matches("RuntimeFn::UnpackSequence(").count(),
        6,
        "{mir}"
    );
    let invalid = root.0.join("nested.py");
    for source in ["a, (b, c) = [1, [2, 3]]\n", "a, *(b, c) = [1, 2, 3]\n"] {
        std::fs::write(&invalid, source).unwrap();
        let mut session = Session::default();
        assert!(ryupy_driver::dump(&mut session, &invalid, Stage::Mir).is_err());
        assert!(session.diagnostics.iter().any(|d| d.code == Some("E0100")));
    }
}

#[test]
fn iterator_values_and_common_targets_match_python() {
    let (_root, expected, actual) = execute(
        include_str!("../../../tests/fixtures/iterator_unpack/values.py"),
        "unpack_values.py",
        0,
    );
    assert_success(&expected, &actual);
}

#[test]
fn iterator_cardinality_state_and_suspended_targets_match_python() {
    let (_root, expected, actual) = execute(
        include_str!("../../../tests/fixtures/iterator_unpack/state.py"),
        "unpack_state.py",
        0,
    );
    assert_success(&expected, &actual);
}

#[test]
fn iterator_callback_preserves_complete_exception_chain() {
    let (_root, expected, actual) = execute(
        include_str!("../../../tests/fixtures/iterator_unpack/error_boundary.py"),
        "unpack_error.py",
        1,
    );
    assert_eq!(expected.status.code(), Some(1));
    assert_eq!(actual.status.code(), Some(1));
    assert_eq!(actual.stdout, expected.stdout);
    let projected = trace_projection(&expected);
    // 完整预期按本fixture的用户帧固定；执行时先守住CP原链再比较JIT，不删中间帧。
    assert_eq!(
        projected,
        vec![
            "Traceback (most recent call last):",
            "File unpack_error.py, line 42, in <module>",
            "ValueError: outer",
            "During handling of the above exception, another exception occurred:",
            "Traceback (most recent call last):",
            "File unpack_error.py, line 44, in <module>",
            "File unpack_error.py, line 39, in unpack",
            "File unpack_error.py, line 36, in __next__",
            "File unpack_error.py, line 30, in callback",
            "RuntimeError: next failed",
        ]
    );
    assert_eq!(trace_projection(&actual), projected);
}
