//! 星号解包：固定ABI与引用冻结结构，三个真实CP/JIT双流及完整用户异常链。
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
        "ryupy-starred-unpack-{}-{stamp}-{serial}",
        std::process::id()
    ));
    std::fs::create_dir(&path).unwrap();
    eprintln!("starred unpack evidence: {}", path.display());
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

fn functions<'a>(dump: &'a str, name: &str) -> Vec<&'a str> {
    dump.match_indices(&format!("fn {name}("))
        .map(|(start, _)| {
            let tail = &dump[start..];
            tail.find("\nfn ").map_or(tail, |end| &tail[..end])
        })
        .collect()
}

// 只检查专设站点：GetItem必须读helper tuple，不能全函数禁止合法的用户取项。
fn frozen_slots(text: &str, before: usize, after: usize) -> Vec<String> {
    let helpers: Vec<_> = text
        .lines()
        .filter(|line| line.contains("RuntimeFn::UnpackEx("))
        .collect();
    assert_eq!(helpers.len(), 1, "{text}");
    let helper = helpers[0];
    assert!(
        helper.ends_with(&format!(", const {before}_i64, const {after}_i64);")),
        "{text}"
    );
    let tuple = helper.trim().split_once(" = ").unwrap().0;
    let reads: Vec<_> = text
        .lines()
        .filter(|line| line.contains("RuntimeFn::GetItem("))
        .collect();
    assert_eq!(reads.len(), before + after + 1, "{text}");
    for read in &reads {
        assert!(
            read.contains(&format!("RuntimeFn::GetItem({tuple}, ")),
            "{text}"
        );
    }
    for forbidden in [
        "RuntimeFn::Len(",
        "RuntimeFn::MakeSlice(",
        "RuntimeFn::UnpackCheck(",
        "RuntimeFn::DynBuiltin(const \"list\"",
    ] {
        assert!(!text.contains(forbidden), "{text}");
    }
    reads
        .iter()
        .map(|line| line.trim().split_once(" = ").unwrap().0.to_owned())
        .collect()
}

#[test]
fn starred_unpack_has_fixed_abi_frozen_slots_and_preserved_literal_paths() {
    let root = root();
    let input = root.0.join("starred_shapes.py");
    let source = r#"def swap(a, b):
    a, b = b, a
    return a + b
def direct_list(a, b):
    first, *rest = [a, b]
    return rest
def direct_tuple(a, b):
    first, *rest = (a, b)
    return rest
def plain(values):
    a, b = values
def bind(target, values):
    target().first, *target().rest, target()[0] = values
def all_items(values):
    *rest, = values
def left(values):
    first, *rest = values
def right(values):
    *rest, last = values
def both(values):
    first, *rest, last = values
def annotated(values: tuple):
    first, *rest = values
def constructed(values):
    first, *rest = list(values)
def expanded_tuple(values):
    first, *rest = (*values,)
def expanded_list(values):
    first, *rest = [*values]
def chain(values):
    whole = first, *rest = values
def loop(values):
    for first, *rest in values:
        print(first)
def entered(manager):
    with manager as (first, *rest):
        print(first)
def comprehensions(values):
    a = [first for first, *rest in values]
    b = {first for first, *rest in values}
    c = {first: rest for first, *rest in values}
    d = (first for first, *rest in values)
    return a, b, c, d
def paused(target, values):
    target.first, *target.rest, target[(yield 0)] = values
"#;
    std::fs::write(&input, source).unwrap();
    let mut session = Session::default();
    let mir = ryupy_driver::dump(&mut session, &input, Stage::Mir).unwrap();
    std::fs::write(root.0.join("mir.txt"), &mir).unwrap();
    assert!(session.diagnostics.is_empty());
    for (name, before, after) in [
        ("all_items", 0, 0),
        ("left", 1, 0),
        ("right", 0, 1),
        ("both", 1, 1),
        ("bind", 1, 1),
    ] {
        frozen_slots(function(&mir, name), before, after);
    }
    let bind = function(&mir, "bind");
    let last_read = bind.rfind("RuntimeFn::GetItem(").unwrap();
    let target = bind.find("dyn.call(").unwrap();
    assert!(
        last_read < target && target < bind.find("RuntimeFn::SetAttr(").unwrap(),
        "{bind}"
    );
    for name in [
        "annotated",
        "constructed",
        "expanded_tuple",
        "expanded_list",
        "chain",
        "loop",
        "entered",
    ] {
        assert_eq!(
            function(&mir, name).matches("RuntimeFn::UnpackEx(").count(),
            1,
            "{mir}"
        );
    }
    for name in ["<listcomp>", "<setcomp>", "<dictcomp>", "<genexpr>"] {
        let bodies = functions(&mir, name);
        assert_eq!(
            bodies
                .iter()
                .map(|body| body.matches("RuntimeFn::UnpackEx(").count())
                .sum::<usize>(),
            1,
            "{mir}"
        );
    }
    let paused = functions(&mir, "paused");
    assert_eq!(paused.len(), 2, "{mir}");
    assert!(paused[0].contains("MakeGenerator") && !paused[0].contains("UnpackEx"));
    let slots = frozen_slots(paused[1], 1, 1);
    for slot in slots {
        assert!(
            paused[1]
                .lines()
                .any(|line| line.contains("RuntimeFn::ArraySet(")
                    && line.ends_with(&format!(", {slot});"))),
            "{}",
            paused[1]
        );
        assert!(
            paused[1].contains(&format!("{slot} = RuntimeFn::ArrayGet(")),
            "{}",
            paused[1]
        );
    }
    let swap = function(&mir, "swap");
    for forbidden in [
        "MakeTuple",
        "GetItem",
        "UnpackCheck",
        "UnpackSequence",
        "UnpackEx",
    ] {
        assert!(!swap.contains(forbidden), "{swap}");
    }
    for name in ["direct_list", "direct_tuple"] {
        let body = function(&mir, name);
        assert!(body.contains("RuntimeFn::UnpackCheck("), "{body}");
        assert!(
            !body.contains("UnpackEx") && !body.contains("UnpackSequence"),
            "{body}"
        );
    }
    assert!(function(&mir, "plain").contains("RuntimeFn::UnpackSequence("));
    assert_eq!(mir.matches("RuntimeFn::UnpackEx(").count(), 17, "{mir}");
    let cil = ryupy_driver::dump(&mut Session::default(), &input, Stage::Cil).unwrap();
    std::fs::write(root.0.join("cil.txt"), &cil).unwrap();
    assert!(cil.contains("PyOps.UnpackEx(object, long, long)"), "{cil}");
    for invalid in ["a, (b, c) = [1, [2, 3]]\n", "a, *(b, c) = [1, 2, 3]\n"] {
        std::fs::write(&input, invalid).unwrap();
        let mut session = Session::default();
        assert!(ryupy_driver::dump(&mut session, &input, Stage::Mir).is_err());
        assert!(
            session
                .diagnostics
                .iter()
                .any(|error| error.code == Some("E0100"))
        );
    }
    std::fs::write(&input, "a, *b, *c = [1, 2, 3]\n").unwrap();
    let mut session = Session::default();
    assert!(ryupy_driver::dump(&mut session, &input, Stage::Mir).is_err());
    assert!(!session.diagnostics.is_empty());
}

#[test]
fn starred_iterator_protocol_values_and_hint_boundaries_match_python() {
    let (_root, expected, actual) = execute(
        include_str!("../../../tests/fixtures/starred_iterator_unpack/core.py"),
        "starred_core.py",
        0,
    );
    assert_success(&expected, &actual);
}

#[test]
fn starred_targets_keep_order_aliases_and_suspended_references() {
    let (_root, expected, actual) = execute(
        include_str!("../../../tests/fixtures/starred_iterator_unpack/targets.py"),
        "starred_targets.py",
        0,
    );
    assert_success(&expected, &actual);
}

#[test]
fn starred_tail_callback_preserves_the_complete_user_exception_chain() {
    let (_root, expected, actual) = execute(
        include_str!("../../../tests/fixtures/starred_iterator_unpack/error_boundary.py"),
        "starred_error.py",
        1,
    );
    assert_eq!(actual.status.code(), expected.status.code());
    assert_eq!(actual.stdout, expected.stdout);
    let projected = trace_projection(&expected);
    assert_eq!(
        projected,
        vec![
            "Traceback (most recent call last):",
            "File starred_error.py, line 72, in <module>",
            "ValueError: outer",
            "During handling of the above exception, another exception occurred:",
            "Traceback (most recent call last):",
            "File starred_error.py, line 74, in <module>",
            "File starred_error.py, line 69, in unpack",
            "File starred_error.py, line 66, in __next__",
            "File starred_error.py, line 55, in callback",
            "RuntimeError: tail failed",
        ]
    );
    assert_eq!(trace_projection(&actual), projected);
}

// 历史pending原字节已通过CP/JIT验收，保留原文件名并纳入正常回归。
#[test]
fn original_module_function_collision_matches_python_without_source_reduction() {
    let (_root, expected, actual) = execute(
        include_str!(
            "../../../tests/fixtures/starred_iterator_unpack/module_function_collision.py.pending"
        ),
        "starred_original_collision.py",
        0,
    );
    assert_success(&expected, &actual);
}
