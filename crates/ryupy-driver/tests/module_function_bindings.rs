//! 模块函数公开绑定：三个真实应用CP/JIT差分；先固定CP版本/退出码，再启动Ryu。
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
        "ryupy-module-bindings-{}-{stamp}-{serial}",
        std::process::id()
    ));
    std::fs::create_dir(&path).unwrap();
    eprintln!("module bindings evidence: {}", path.display());
    Root(path)
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
fn execute(
    source: &str,
    filename: &str,
    expected_exit: i32,
    modules: bool,
) -> (Root, Output, Output) {
    let root = root();
    let python = std::env::var_os("PYTHON").unwrap_or_else(|| "python3".into());
    let version = Command::new(&python).args(["-c", "import sys; assert sys.implementation.name == 'cpython'; print('.'.join(map(str, sys.version_info[:3])))"]).output().unwrap();
    save_output(&root, "python-version", &version);
    assert!(version.status.success());
    assert!(version.stderr.is_empty());
    let pinned = std::fs::read_to_string(
        ryupy_driver::runtime::workspace_root().join("tests/pinned-python.txt"),
    )
    .unwrap();
    assert_eq!(
        String::from_utf8_lossy(&version.stdout).trim(),
        pinned.trim()
    );
    // 多模块测试使用真实文件，绝不以sys.modules字典伪造导出/循环导入。
    if modules {
        for (name, source) in MODULES {
            std::fs::write(root.0.join(name), source).unwrap();
        }
    }
    assert_eq!(pinned.trim(), "3.12.13");
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

const MODULES: &[(&str, &str)] = &[
    (
        "binding_provider.py",
        include_str!("../../../tests/fixtures/module_function_bindings/binding_provider.py"),
    ),
    (
        "binding_cycle_a.py",
        include_str!("../../../tests/fixtures/module_function_bindings/binding_cycle_a.py"),
    ),
    (
        "binding_cycle_b.py",
        include_str!("../../../tests/fixtures/module_function_bindings/binding_cycle_b.py"),
    ),
];

#[test]
fn selected_definitions_defaults_aliases_and_late_globals_match_python() {
    let (_root, expected, actual) = execute(
        include_str!("../../../tests/fixtures/module_function_bindings/single.py"),
        "single.py",
        0,
        false,
    );
    assert_success(&expected, &actual);
}

#[test]
fn real_exports_setters_and_circular_imports_share_the_public_slot() {
    let (_root, expected, actual) = execute(
        include_str!("../../../tests/fixtures/module_function_bindings/modules.py"),
        "modules.py",
        0,
        true,
    );
    assert_success(&expected, &actual);
}

#[test]
fn failed_publication_preserves_alias_and_complete_exception_chain() {
    let (_root, expected, actual) = execute(
        include_str!("../../../tests/fixtures/module_function_bindings/error.py"),
        "error.py",
        1,
        false,
    );
    assert_eq!(actual.status.code(), expected.status.code());
    assert_eq!(actual.stdout, expected.stdout);
    // 沿用starred driver既有投影：所有帧、异常及链标记保留，只忽略源码/caret/路径。
    let projected = trace_projection(&expected);
    assert_eq!(
        projected,
        vec![
            "Traceback (most recent call last):",
            "File error.py, line 9, in <module>",
            "File error.py, line 7, in failed_default",
            "ValueError: default rejected",
            "During handling of the above exception, another exception occurred:",
            "Traceback (most recent call last):",
            "File error.py, line 14, in <module>",
            "File error.py, line 4, in selected",
            "LookupError: old callable",
            "The above exception was the direct cause of the following exception:",
            "Traceback (most recent call last):",
            "File error.py, line 16, in <module>",
            "RuntimeError: outer failure",
        ]
    );
    assert_eq!(trace_projection(&actual), projected);
}
