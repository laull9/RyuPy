//! 有限optparse真实默认编译差分；原输出不整形，构造异常保完整用户帧及模块限定名。
use ryupy_driver::{BuildOptions, Session};
use std::path::{Path, PathBuf};
use std::process::{Command, Output};
use std::sync::atomic::{AtomicU64, Ordering};

static NEXT: AtomicU64 = AtomicU64::new(0);
struct Root(PathBuf);
impl Drop for Root {
    fn drop(&mut self) {
        if !std::thread::panicking() {
            let _ = std::fs::remove_dir_all(&self.0);
        }
    }
}
fn save(root: &Path, label: &str, output: &Output) {
    std::fs::write(root.join(format!("{label}.stdout")), &output.stdout).unwrap();
    std::fs::write(root.join(format!("{label}.stderr")), &output.stderr).unwrap();
    std::fs::write(
        root.join(format!("{label}.exit")),
        format!("{:?}", output.status.code()),
    )
    .unwrap();
}
fn execute(source: &str, filename: &str) -> (Root, Output, Output, PathBuf, PathBuf) {
    let python = std::env::var_os("PYTHON").unwrap_or_else(|| "python3".into());
    let probe = Command::new(&python).args(["-B", "-c", "import sys, optparse; assert sys.implementation.name == 'cpython'; print('.'.join(map(str, sys.version_info[:3]))); print(optparse.__file__)"]).output().unwrap();
    assert!(probe.status.success());
    let text = String::from_utf8(probe.stdout).unwrap();
    let lines: Vec<_> = text.lines().collect();
    assert_eq!(lines.len(), 2);
    let pinned = std::fs::read_to_string(
        ryupy_driver::runtime::workspace_root().join("tests/pinned-python.txt"),
    )
    .unwrap();
    assert_eq!(lines[0], pinned.trim());
    let stdlib = PathBuf::from(lines[1]);
    assert!(stdlib.is_absolute() && stdlib.is_file());
    let stamp = std::time::SystemTime::now()
        .duration_since(std::time::UNIX_EPOCH)
        .unwrap()
        .as_nanos();
    let path = std::env::temp_dir().join(format!(
        "ryupy-optparse_core-{}-{stamp}-{}",
        std::process::id(),
        NEXT.fetch_add(1, Ordering::Relaxed)
    ));
    std::fs::create_dir(&path).unwrap();
    let root = Root(path);
    let input = root.0.join(filename);
    std::fs::write(&input, source).unwrap();
    let cp_cwd = root.0.join("cp");
    let jit_cwd = root.0.join("jit");
    std::fs::create_dir(&cp_cwd).unwrap();
    std::fs::create_dir(&jit_cwd).unwrap();
    let expected = Command::new(python)
        .arg("-B")
        .arg(&input)
        .current_dir(&cp_cwd)
        .env("COLUMNS", "80")
        .env("LC_ALL", "C")
        .output()
        .unwrap();
    save(&root.0, "python", &expected);
    let artifacts = ryupy_driver::build(
        &mut Session::default(),
        &BuildOptions {
            input: input.clone(),
            out_dir: root.0.join("out"),
            aot: false,
            search_paths: vec![],
        },
    )
    .unwrap();
    let actual = ryupy_driver::dotnet::run_dll_with(
        &artifacts.dll,
        &ryupy_driver::dotnet::RunOptions {
            cwd: Some(jit_cwd),
            env: vec![
                ("COLUMNS".to_owned(), "80".to_owned()),
                ("LC_ALL".to_owned(), "C".to_owned()),
            ],
            ..Default::default()
        },
    )
    .unwrap();
    save(&root.0, "jit", &actual);
    // guard交还断言调用者，失败保留原日志，成功仅清理本次独占根。
    (root, expected, actual, input, stdlib)
}
fn same_raw(source: &str, filename: &str) {
    let (_root, expected, actual, _, _) = execute(source, filename);
    assert!(
        expected.status.success(),
        "{}",
        String::from_utf8_lossy(&expected.stderr)
    );
    assert_eq!(
        actual.status.code(),
        expected.status.code(),
        "{}",
        String::from_utf8_lossy(&actual.stderr)
    );
    assert_eq!(actual.stdout, expected.stdout);
    assert_eq!(actual.stderr, expected.stderr);
}
#[test]
fn token_grammar_and_interspersed_arguments() {
    same_raw(
        include_str!("../../../tests/fixtures/optparse_core/grammar.py"),
        "opt_grammar.py",
    );
}
#[test]
fn real_objects_defaults_and_values_identity() {
    same_raw(
        include_str!("../../../tests/fixtures/optparse_core/objects.py"),
        "opt_objects.py",
    );
}
#[test]
fn help_exits_with_exact_default_formatter_output() {
    same_raw(
        include_str!("../../../tests/fixtures/optparse_core/help.py"),
        "opt_help.py",
    );
}
#[test]
fn version_and_usage_use_real_stdout_and_exit() {
    same_raw(
        include_str!("../../../tests/fixtures/optparse_core/version.py"),
        "opt_version.py",
    );
}
#[test]
fn parse_errors_keep_exact_stderr_and_context_after_partial_assignment() {
    same_raw(
        include_str!("../../../tests/fixtures/optparse_core/errors.py"),
        "opt_errors.py",
    );
}
fn projection(output: &Output, input: &Path, stdlib: &Path, python: bool) -> Vec<String> {
    let text = String::from_utf8(output.stderr.clone()).unwrap();
    assert!(!text.contains("Unhandled exception"), "{text}");
    let mut result = Vec::new();
    let mut library_frames = Vec::new();
    for raw in text.lines() {
        let line = raw.trim();
        if let Some(file) = line.strip_prefix("File \"") {
            let (path, position) = file.rsplit_once("\", line ").unwrap();
            if python && Path::new(path) == stdlib {
                assert_eq!(
                    result.last().unwrap(),
                    "File opt_constructor.py, line 4, in construct"
                );
                library_frames.push(position.to_owned());
                continue;
            }
            if python {
                assert_eq!(Path::new(path), input, "unexpected reference frame {path}");
            } else {
                assert!(
                    Path::new(path) == input || path == "opt_constructor.py",
                    "unexpected native frame {path}"
                );
            }
            result.push(format!("File opt_constructor.py, line {position}"));
        } else if !line.is_empty() && !raw.starts_with(char::is_whitespace) {
            result.push(line.to_owned());
        }
    }
    if python {
        assert_eq!(library_frames, ["581, in __init__", "657, in _check_type"]);
    } else {
        assert!(library_frames.is_empty());
    }
    result
}
#[test]
fn complete_context_chain_and_user_caller_frames() {
    let (_root, expected, actual, input, stdlib) = execute(
        include_str!("../../../tests/fixtures/optparse_core/constructor_error.py"),
        "opt_constructor.py",
    );
    assert_eq!(expected.status.code(), Some(1));
    assert_eq!(actual.status.code(), Some(1));
    assert_eq!(actual.stdout, expected.stdout);
    let fixed = [
        "Traceback (most recent call last):",
        "File opt_constructor.py, line 7, in <module>",
        "ValueError: outer-context",
        "During handling of the above exception, another exception occurred:",
        "Traceback (most recent call last):",
        "File opt_constructor.py, line 9, in <module>",
        "File opt_constructor.py, line 4, in construct",
        "optparse.OptionError: option -n: invalid option type: 'invalid'",
    ];
    assert_eq!(projection(&expected, &input, &stdlib, true), fixed);
    assert_eq!(projection(&actual, &input, &stdlib, false), fixed);
}
