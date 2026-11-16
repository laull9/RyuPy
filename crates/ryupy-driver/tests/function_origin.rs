//! 真实函数module前置的三个应用：固定CP-first、原始双流/退出码与完整用户异常链。
//! 集成位置为crates/ryupy-driver/tests/function_origin.rs；所有fixture放tests/fixtures/function_origin。
//! 函数属性差分不替代frame-owner/后续namedtuple端到端验收。
use ryupy_driver::{BuildOptions, Session};
use std::path::PathBuf;
use std::process::{Command, Output};
use std::sync::atomic::{AtomicU64, Ordering};

static NEXT_ROOT: AtomicU64 = AtomicU64::new(0);
const DEFINITIONS: &str = include_str!("../../../tests/fixtures/function_origin/definitions.py");
const MODULE_ENTRY: &str = include_str!("../../../tests/fixtures/function_origin/modules/entry.py");
const PROVIDER: &str =
    include_str!("../../../tests/fixtures/function_origin/modules/origin_provider.py");
const ERRORS: &str = include_str!("../../../tests/fixtures/function_origin/errors.py");

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
        "ryupy-function-origin-{}-{stamp}-{serial}",
        std::process::id()
    ));
    std::fs::create_dir(&path).unwrap();
    eprintln!("function origin evidence: {}", path.display());
    Root(path)
}
// 先落原流再作任何判断；Root由test持有，比较失败或编译panic时仍保留证据。
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
    provider: bool,
) -> (Root, Output, Output) {
    let root = root();
    let python = std::env::var_os("PYTHON").unwrap_or_else(|| "python3".into());
    let version = Command::new(&python)
        .args(["-c", "import sys; assert sys.implementation.name == 'cpython'; print('.'.join(map(str, sys.version_info[:3])))"])
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
    if provider {
        std::fs::write(root.0.join("origin_provider.py"), PROVIDER).unwrap();
    }
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
        "CP fixture本身失败: {}",
        String::from_utf8_lossy(&expected.stderr)
    );
    // 先确认这一次真实CP oracle有效，再启动编译/JIT；不另跑参考脚本。
    if expected_exit == 0 {
        assert!(expected.stderr.is_empty());
    } else {
        assert_eq!(trace_projection(&expected), expected_error_trace());
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
// 与module_function_bindings现有投影相同：只去路径/源码/caret，不删任何File帧或异常链。
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
fn expected_error_trace() -> Vec<&'static str> {
    vec![
        "Traceback (most recent call last):",
        "File errors.py, line 42, in <module>",
        "File errors.py, line 12, in fail",
        "ValueError: final-default-failed",
        "During handling of the above exception, another exception occurred:",
        "Traceback (most recent call last):",
        "File errors.py, line 47, in <module>",
        "File errors.py, line 5, in selected",
        "LookupError: old callable",
        "The above exception was the direct cause of the following exception:",
        "Traceback (most recent call last):",
        "File errors.py, line 49, in <module>",
        "RuntimeError: outer failure",
    ]
}
fn assert_labels(expected: &Output, labels: &[&str]) {
    let output = std::str::from_utf8(&expected.stdout).unwrap();
    for label in labels {
        assert_eq!(
            output
                .lines()
                .filter(|line| line.starts_with(&format!("{label} ")))
                .count(),
            1,
            "CP fixture必须实际输出完整组，不能把被删空的应用计通过: {label}"
        );
    }
}

#[test]
fn fresh_definitions_modules_aliases_and_evaluation_order_match_python() {
    let (_root, expected, actual) = execute(DEFINITIONS, "definitions.py", 0, false);
    assert_labels(
        &expected,
        &[
            "initial",
            "capture",
            "running-field",
            "alias",
            "descriptor",
            "dict-replacement",
            "delete",
            "nonstring-name",
            "none-name",
            "lambda",
            "repeated-def",
            "definition-events",
            "decorated",
        ],
    );
    assert_eq!(
        String::from_utf8_lossy(&expected.stdout)
            .lines()
            .filter(|line| *line == "assignment-identity True")
            .count(),
        5
    );
    assert_success(&expected, &actual);
}

#[test]
fn real_module_exports_closures_and_generator_metadata_match_python() {
    let (_root, expected, actual) = execute(MODULE_ENTRY, "entry.py", 0, true);
    assert_labels(
        &expected,
        &[
            "imported",
            "nested",
            "old-import",
            "replace",
            "return-old-decorator",
            "method",
            "generator-functions",
            "first-yield",
            "second-yield",
            "exhausted",
            "before-throw",
            "throw",
        ],
    );
    assert_success(&expected, &actual);
}

#[test]
fn failed_definition_keeps_old_function_module_and_complete_exception_chain() {
    let (_root, expected, actual) = execute(ERRORS, "errors.py", 1, false);
    assert_labels(
        &expected,
        &[
            "default-preserved",
            "annotation-preserved",
            "decorator-preserved",
            "events",
            "before-def",
            "after-def",
            "saved-before-error",
        ],
    );
    let output = std::str::from_utf8(&expected.stdout).unwrap();
    assert!(
        output
            .lines()
            .any(|line| line == "before-def NameError name 'before' is not defined")
    );
    assert!(output.lines().any(|line| line == "after-def 3"));
    assert_eq!(actual.status.code(), expected.status.code());
    assert_eq!(actual.stdout, expected.stdout);
    assert_eq!(trace_projection(&actual), trace_projection(&expected));
}
