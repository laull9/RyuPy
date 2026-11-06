//! exact list四方法绑定：三个真实CP/JIT应用，保留首次基线与完整用户异常链。
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
        "ryupy-list-bindings-{}-{stamp}-{serial}",
        std::process::id()
    ));
    std::fs::create_dir(&path).unwrap();
    eprintln!("list bindings evidence: {}", path.display());
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

fn execute(source: &str, filename: &str, expected_exit: i32) -> (Root, Output, Output) {
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
fn original_active_34_groups_keep_the_first_cpython_results() {
    let (_root, expected, actual) = execute(
        include_str!("../../../tests/fixtures/list_method_bindings/active.py"),
        "list_active.py",
        0,
    );
    // 只剔除两个明确域外boundary行；34组中的yield有两行，另有完成标志。
    let historical =
        include_str!("../../../tests/fixtures/list_method_bindings/baseline.cpython.stdout");
    let active: String = historical
        .lines()
        .filter(|line| !line.starts_with("scope=boundary "))
        .map(|line| format!("{line}\n"))
        .collect();
    assert_eq!(active.lines().count(), 36);
    assert_eq!(
        active
            .lines()
            .filter(|line| line.starts_with("scope=active "))
            .count(),
        35
    );
    assert_eq!(expected.stdout, active.as_bytes());
    assert_success(&expected, &actual);
}

#[test]
fn small_successes_argument_effects_and_suspended_receivers_match_python() {
    let (_root, expected, actual) = execute(
        include_str!("../../../tests/fixtures/list_method_bindings/success_and_order.py"),
        "list_success.py",
        0,
    );
    assert_success(&expected, &actual);
}

#[test]
fn binding_errors_keep_the_complete_user_context_chain() {
    let (_root, expected, actual) = execute(
        include_str!("../../../tests/fixtures/list_method_bindings/error_chain.py"),
        "list_error.py",
        1,
    );
    assert_eq!(actual.status.code(), expected.status.code());
    assert_eq!(actual.stdout, expected.stdout);
    assert_eq!(
        expected.stdout,
        concat!(
            "caught clear TypeError list.clear() takes no arguments (1 given) True [10, 20]\n",
            "caught reverse TypeError list.reverse() takes no keyword arguments True [10, 20]\n",
            "caught copy TypeError list.copy() takes no arguments (2 given) True [10, 20]\n",
        )
        .as_bytes()
    );
    // 行号按固定fixture逐行冻结；必须先由父实际CP执行核验，不能预接受快照。
    let projected = trace_projection(&expected);
    assert_eq!(projected.len(), 8);
    assert_eq!(
        projected,
        vec![
            "Traceback (most recent call last):",
            "File list_error.py, line 28, in <module>",
            "ValueError: outer context",
            "During handling of the above exception, another exception occurred:",
            "Traceback (most recent call last):",
            "File list_error.py, line 30, in <module>",
            "File list_error.py, line 17, in pop_error",
            "TypeError: list.pop() takes no keyword arguments",
        ]
    );
    assert_eq!(trace_projection(&actual), projected);
}
