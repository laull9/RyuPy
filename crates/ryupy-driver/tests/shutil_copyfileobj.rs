//! copyfileobj真实默认编译差分；只投影已验证CP标准库实现帧，保留全部用户帧和异常链。
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
    let probe = Command::new(&python).args(["-B", "-c", "import sys, shutil; assert sys.implementation.name == 'cpython'; print('.'.join(map(str, sys.version_info[:3]))); print(shutil.__file__)"]).output().unwrap();
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
        "ryupy-copyfileobj-{}-{stamp}-{}",
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
            ..Default::default()
        },
    )
    .unwrap();
    save(&root.0, "jit", &actual);
    // guard交还断言调用者，失败保留原日志，成功仅清理本次独占根。
    (root, expected, actual, input, stdlib)
}
fn success(source: &str, filename: &str) {
    let (_root, expected, actual, _, _) = execute(source, filename);
    assert!(
        expected.status.success(),
        "{}",
        String::from_utf8_lossy(&expected.stderr)
    );
    assert!(expected.stderr.is_empty());
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
fn cached_stream_methods_lengths_keywords_and_buffer_identity() {
    success(
        include_str!("../../../tests/fixtures/shutil_copyfileobj/core.py"),
        "copy_core.py",
    );
}
#[test]
fn binary_text_and_real_file_positions_without_implicit_close() {
    success(
        include_str!("../../../tests/fixtures/shutil_copyfileobj/io_streams.py"),
        "copy_io.py",
    );
}
#[test]
fn caught_user_exception_identity_and_error_side_effects() {
    success(
        include_str!("../../../tests/fixtures/shutil_copyfileobj/errors.py"),
        "copy_errors.py",
    );
}
fn projection(output: &Output, input: &Path, stdlib: &Path, python: bool) -> Vec<String> {
    let text = String::from_utf8(output.stderr.clone()).unwrap();
    assert!(!text.contains("Unhandled exception"), "{text}");
    let mut result = Vec::new();
    let mut library_frames = 0;
    for raw in text.lines() {
        let line = raw.trim();
        if let Some(file) = line.strip_prefix("File \"") {
            let (path, position) = file.rsplit_once("\", line ").unwrap();
            if python && Path::new(path) == stdlib {
                assert_eq!(position, "204, in copyfileobj");
                assert_eq!(
                    result.last().unwrap(),
                    "File copy_traceback.py, line 15, in transfer"
                );
                library_frames += 1;
                continue;
            }
            if python {
                assert_eq!(Path::new(path), input, "unexpected reference frame {path}");
            } else {
                assert!(
                    Path::new(path) == input || path == "copy_traceback.py",
                    "unexpected native frame {path}"
                );
            }
            result.push(format!("File copy_traceback.py, line {position}"));
        } else if !line.is_empty() && !raw.starts_with(char::is_whitespace) {
            result.push(line.to_owned());
        }
    }
    assert_eq!(library_frames, usize::from(python));
    result
}
#[test]
fn complete_context_chain_and_user_caller_frames() {
    let (_root, expected, actual, input, stdlib) = execute(
        include_str!("../../../tests/fixtures/shutil_copyfileobj/traceback.py"),
        "copy_traceback.py",
    );
    assert_eq!(expected.status.code(), Some(1));
    assert_eq!(actual.status.code(), Some(1));
    assert_eq!(actual.stdout, expected.stdout);
    let fixed = [
        "Traceback (most recent call last):",
        "File copy_traceback.py, line 18, in <module>",
        "ValueError: outer-context",
        "During handling of the above exception, another exception occurred:",
        "Traceback (most recent call last):",
        "File copy_traceback.py, line 20, in <module>",
        "File copy_traceback.py, line 15, in transfer",
        "File copy_traceback.py, line 12, in write",
        "CopyError: copy-user-error",
    ];
    assert_eq!(projection(&expected, &input, &stdlib, true), fixed);
    assert_eq!(projection(&actual, &input, &stdlib, false), fixed);
}
