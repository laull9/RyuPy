//! 四应用当次CP-first门：固定解释器、手写标签和完整三流，所有临时输入/产物按RAII清理。
use ryupy_driver::{BuildOptions, Session};
use std::path::{Path, PathBuf};
use std::process::{Command, Output};
use std::sync::atomic::{AtomicU64, Ordering};
mod labels;
mod trace;
static NEXT: AtomicU64 = AtomicU64::new(0);

struct Root(PathBuf);
impl Drop for Root {
    fn drop(&mut self) {
        let _ = std::fs::remove_dir_all(&self.0);
    }
}
impl Root {
    fn new(name: &str) -> Self {
        let stamp = std::time::SystemTime::now()
            .duration_since(std::time::UNIX_EPOCH)
            .unwrap()
            .as_nanos();
        let path = std::env::temp_dir().join(format!(
            "ryupy-dynamic-except-{name}-{}-{stamp}-{}",
            std::process::id(),
            NEXT.fetch_add(1, Ordering::Relaxed)
        ));
        std::fs::create_dir(&path).unwrap();
        let root = Self(std::fs::canonicalize(path).unwrap());
        let workspace = std::fs::canonicalize(ryupy_driver::runtime::workspace_root()).unwrap();
        assert!(!root.0.starts_with(workspace), "临时产物必须位于仓库外");
        root
    }
}
fn copy_tree(source: &Path, destination: &Path) {
    std::fs::create_dir(destination).unwrap();
    for entry in std::fs::read_dir(source).unwrap() {
        let entry = entry.unwrap();
        let path = entry.path();
        let target = destination.join(entry.file_name());
        if path.is_dir() {
            copy_tree(&path, &target);
        } else {
            std::fs::copy(path, target).unwrap();
        }
    }
}
fn run(root: &Path, label: &str, command: &mut Command) -> Output {
    let output = command
        .output()
        .unwrap_or_else(|error| panic!("{label}启动失败：{command:?}: {error}"));
    std::fs::write(root.join(format!("{label}.stdout")), &output.stdout).unwrap();
    std::fs::write(root.join(format!("{label}.stderr")), &output.stderr).unwrap();
    std::fs::write(
        root.join(format!("{label}.exit")),
        format!("{:?}\n", output.status.code()),
    )
    .unwrap();
    output
}
fn command(program: &std::ffi::OsStr, cwd: &Path) -> Command {
    let mut command = Command::new(program);
    command
        .current_dir(cwd)
        .env_remove("PYTHONHOME")
        .env_remove("PYTHONPATH")
        .env("LC_ALL", "C")
        .env("TZ", "UTC");
    command
}
fn python_command(python: &std::ffi::OsStr, cwd: &Path) -> Command {
    let mut command = command(python, cwd);
    // -E/-S拒绝环境和site污染；保留脚本目录，identity应用才能导入同次复制的left/right/facade。
    command.args(["-E", "-S", "-B"]);
    command
}

pub(super) fn check(name: &str, entry: &str, expected_exit: i32) {
    let fixture = ryupy_driver::runtime::workspace_root().join("tests/fixtures/dynamic_except");
    let root = Root::new(name);
    let source = root.0.join("source");
    copy_tree(&fixture, &source);
    let cp_cwd = root.0.join("cp");
    let jit_cwd = root.0.join("jit");
    std::fs::create_dir(&cp_cwd).unwrap();
    std::fs::create_dir(&jit_cwd).unwrap();
    let python = std::env::var_os("PYTHON").expect("须通过PYTHON指定固定CPython 3.12.13绝对路径");
    assert!(Path::new(&python).is_absolute(), "PYTHON必须是绝对路径");
    let mut version_command = python_command(&python, &cp_cwd);
    version_command.args(["-c", "import sys; print(sys.implementation.name); print('.'.join(map(str, sys.version_info[:3])))"]);
    let version = run(&root.0, "python-version", &mut version_command);
    assert!(version.status.success() && version.stderr.is_empty());
    assert_eq!(version.stdout, b"cpython\n3.12.13\n");
    let pin = ryupy_driver::runtime::workspace_root().join("tests/pinned-python.txt");
    assert_eq!(std::fs::read_to_string(pin).unwrap().trim(), "3.12.13");
    let input = source.join(entry);
    let expected = run(
        &root.0,
        "cpython",
        python_command(&python, &cp_cwd).arg(&input),
    );
    assert_eq!(
        expected.status.code(),
        Some(expected_exit),
        "CP输入合同失败：{}",
        String::from_utf8_lossy(&expected.stderr)
    );
    let observed: Vec<_> = std::str::from_utf8(&expected.stdout)
        .unwrap()
        .lines()
        .map(|line| line.split_once(' ').expect("每行必须为标签和值").0)
        .collect();
    assert_eq!(observed, labels::expected(name), "公开输出标签/顺序改变");
    assert!(!observed.iter().any(|label| label.starts_with("WRONG")));
    let expected_trace = if expected_exit == 1 {
        Some(trace::checked(&expected.stderr, &source, true))
    } else {
        assert!(expected.stderr.is_empty(), "CP成功应用stderr不为空");
        None
    };
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
    .unwrap_or_else(|error| {
        panic!(
            "动态except编译失败：{error:#}\n{}",
            session.render_diagnostics(false)
        )
    });
    let actual = run(
        &root.0,
        "jit",
        command(ryupy_driver::dotnet::dotnet_exe().as_os_str(), &jit_cwd).arg(&artifact.dll),
    );
    assert_eq!(actual.status.code(), expected.status.code());
    assert_eq!(actual.stdout, expected.stdout);
    if let Some(expected_trace) = expected_trace {
        assert_eq!(
            trace::checked(&actual.stderr, &source, false),
            expected_trace
        );
    } else {
        assert_eq!(actual.stderr, expected.stderr);
    }
}
