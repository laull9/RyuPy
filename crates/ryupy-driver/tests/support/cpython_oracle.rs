//! 固定 CPython 的同次行为预期：保留公开标签、完整标准输出及异常链结构，所有产物使用外部临时目录。
use ryupy_driver::{BuildOptions, Session};
use std::path::{Path, PathBuf};
use std::process::{Command, Output};
use std::sync::atomic::{AtomicU64, Ordering};

static NEXT: AtomicU64 = AtomicU64::new(0);
struct Root(PathBuf);
impl Drop for Root {
    fn drop(&mut self) {
        let _ = std::fs::remove_dir_all(&self.0);
    }
}

/// CPython 先确认版本、标签和成功/失败合同；产品结果与本次预期逐项比较。
pub(super) fn check(filename: &str, source: &str, labels: &[&str], failure: bool) {
    let serial = NEXT.fetch_add(1, Ordering::Relaxed);
    let stamp = std::time::SystemTime::now()
        .duration_since(std::time::UNIX_EPOCH)
        .unwrap()
        .as_nanos();
    let root = Root(std::env::temp_dir().join(format!(
        "ryupy-oracle-{}-{stamp}-{serial}",
        std::process::id()
    )));
    std::fs::create_dir(&root.0).unwrap();
    let cp_cwd = root.0.join("cp");
    let jit_cwd = root.0.join("jit");
    std::fs::create_dir(&cp_cwd).unwrap();
    std::fs::create_dir(&jit_cwd).unwrap();
    let input = root.0.join(filename);
    std::fs::write(&input, source).unwrap();
    let python =
        std::env::var_os("PYTHON").expect("须通过 PYTHON 指定固定 CPython 3.12.13 绝对路径");
    assert!(Path::new(&python).is_absolute(), "PYTHON 必须是绝对路径");
    let expected = command(&python, &cp_cwd).args(["-c", "import sys; print(sys.implementation.name); print('.'.join(map(str,sys.version_info[:3])))"]).output().unwrap();
    assert!(expected.status.success(), "固定 CPython 无法启动");
    assert_eq!(expected.stdout, b"cpython\n3.12.13\n");
    assert!(expected.stderr.is_empty());
    let expected = command(&python, &cp_cwd).arg(&input).output().unwrap();
    assert_eq!(
        expected.status.code(),
        Some(i32::from(failure)),
        "CPython 输入合同失败：{}",
        String::from_utf8_lossy(&expected.stderr)
    );
    let text = std::str::from_utf8(&expected.stdout).unwrap();
    let actual_labels: Vec<_> = text
        .lines()
        .map(|line| line.split_whitespace().next().expect("应用输出不得有空行"))
        .collect();
    if !labels.is_empty() {
        assert_eq!(actual_labels, labels, "输入公开标签改变");
    }
    if !failure {
        assert!(expected.stderr.is_empty());
    }
    let mut session = Session::default();
    let artifact = ryupy_driver::build(
        &mut session,
        &BuildOptions {
            input,
            out_dir: root.0.join("out"),
            aot: false,
            search_paths: vec![],
        },
    )
    .unwrap_or_else(|error| panic!("编译失败：{error:#}\n{}", session.render_diagnostics(false)));
    let actual = ryupy_driver::dotnet::run_dll_with(
        &artifact.dll,
        &ryupy_driver::dotnet::RunOptions {
            cwd: Some(root.0.clone()),
            env: vec![("LC_ALL".into(), "C".into())],
            ..Default::default()
        },
    )
    .unwrap();
    assert_eq!(actual.status.code(), expected.status.code());
    assert_eq!(actual.stdout, expected.stdout);
    if failure {
        assert_eq!(
            project(&actual, filename, source),
            project(&expected, filename, source)
        );
    } else {
        assert_eq!(actual.stderr, expected.stderr);
    }
}

fn command(python: &std::ffi::OsStr, cwd: &Path) -> Command {
    let mut command = Command::new(python);
    command
        .args(["-I", "-B", "-S"])
        .current_dir(cwd)
        .env_remove("PYTHONHOME")
        .env_remove("PYTHONPATH")
        .env("LC_ALL", "C")
        .env("TZ", "UTC");
    command
}

/// 只规整本次源文件回显和路径，保留用户帧、cause/context、异常类型、消息及未知输出。
fn project(output: &Output, filename: &str, source: &str) -> Vec<String> {
    let text = std::str::from_utf8(&output.stderr).unwrap();
    assert!(!text.contains("Unhandled exception"), "{text}");
    let source_lines: Vec<_> = source.lines().collect();
    let mut result = Vec::new();
    let mut echo = None;
    let mut caret_allowed = false;
    for raw in text.lines() {
        let line = raw.trim();
        if let Some(file) = line.strip_prefix("File \"") {
            let (path, position) = file.rsplit_once("\", line ").expect(line);
            let basename = path.rsplit(['/', '\\']).next().unwrap();
            result.push(format!("File {basename}, line {position}"));
            echo = if basename == filename {
                position
                    .split_once(", in ")
                    .and_then(|(number, _)| number.parse::<usize>().ok())
                    .and_then(|number| number.checked_sub(1))
                    .and_then(|index| source_lines.get(index))
                    .map(|line| line.trim())
            } else {
                None
            };
            caret_allowed = false;
            continue;
        }
        if raw.starts_with(char::is_whitespace) && echo == Some(line) {
            echo = None;
            caret_allowed = true;
            continue;
        }
        echo = None;
        if caret_allowed
            && raw.starts_with(char::is_whitespace)
            && !line.is_empty()
            && line.chars().all(|ch| matches!(ch, '^' | '~' | ' '))
        {
            caret_allowed = false;
            continue;
        }
        caret_allowed = false;
        if !line.is_empty() {
            result.push(line.to_owned());
        }
    }
    result
}
