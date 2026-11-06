//! 四应用CP-first正常门禁：原流先落盘再断言，独占根成功失败均保留。
use ryupy_driver::{BuildOptions, Session};
use std::path::{Path, PathBuf};
use std::process::{Command, Output};
use std::sync::atomic::{AtomicU64, Ordering};
mod trace;
static NEXT: AtomicU64 = AtomicU64::new(0);
fn text(path: impl AsRef<Path>) -> String {
    std::fs::read_to_string(path).unwrap()
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
fn exclusive_root(name: &str) -> PathBuf {
    let stamp = std::time::SystemTime::now()
        .duration_since(std::time::UNIX_EPOCH)
        .unwrap()
        .as_nanos();
    let root = std::env::temp_dir().join(format!(
        "ryupy-dynamic-except-{name}-{}-{stamp}-{}",
        std::process::id(),
        NEXT.fetch_add(1, Ordering::Relaxed)
    ));
    std::fs::create_dir(&root).unwrap();
    eprintln!("动态except证据: {}", root.display());
    root
}
fn record_command(root: &Path, label: &str, command: &Command) {
    let program = PathBuf::from(command.get_program());
    let resolved = if program.components().count() > 1 {
        std::fs::canonicalize(&program).ok()
    } else {
        std::env::var_os("PATH").and_then(|path| {
            std::env::split_paths(&path)
                .map(|directory| directory.join(&program))
                .find(|path| path.is_file())
                .and_then(|path| std::fs::canonicalize(path).ok())
        })
    };
    std::fs::write(root.join(format!("{label}.command")),
        format!("command={command:?}\nprogram={program:?}\nresolved={resolved:?}\nargs={:?}\ncwd={:?}\n",
        command.get_args().collect::<Vec<_>>(), command.get_current_dir())).unwrap();
}

fn run(root: &Path, label: &str, command: &mut Command) -> Output {
    record_command(root, label, command);
    match command.output() {
        Ok(output) => {
            std::fs::write(root.join(format!("{label}.stdout")), &output.stdout).unwrap();
            std::fs::write(root.join(format!("{label}.stderr")), &output.stderr).unwrap();
            std::fs::write(
                root.join(format!("{label}.exit")),
                format!("{:?}\n", output.status.code()),
            )
            .unwrap();
            output
        }
        Err(error) => {
            std::fs::write(
                root.join(format!("{label}.launch-error")),
                error.to_string(),
            )
            .unwrap();
            panic!("{label}启动失败: {error}");
        }
    }
}
fn python_command(python: &std::ffi::OsStr, cwd: &Path) -> Command {
    let mut command = Command::new(python);
    command
        .args(["-S", "-B"])
        .current_dir(cwd)
        .env_remove("PYTHONHOME")
        .env_remove("PYTHONPATH")
        .env("LC_ALL", "C");
    command
}

pub(super) fn check(name: &str, entry: &str, expected_exit: i32) {
    let fixture = ryupy_driver::runtime::workspace_root().join("tests/fixtures/dynamic_except");
    let root = exclusive_root(name);
    copy_tree(&fixture, &root.join("source"));
    let cp_cwd = root.join("cp");
    let cwd = root.join("jit");
    std::fs::create_dir(&cp_cwd).unwrap();
    std::fs::create_dir(&cwd).unwrap();
    let python = std::env::var_os("PYTHON").unwrap_or_else(|| "python3".into());
    let mut command = python_command(&python, &cp_cwd);
    command.args(["-c", "import sys; print(sys.implementation.name); print('.'.join(map(str, sys.version_info[:3])))"]);
    let version = run(&root, "python-version", &mut command);
    assert!(version.status.success() && version.stderr.is_empty());
    assert_eq!(version.stdout, b"cpython\n3.12.13\n");
    assert_eq!(
        text(ryupy_driver::runtime::workspace_root().join("tests/pinned-python.txt")).trim(),
        "3.12.13"
    );
    let mut command = python_command(&python, &cp_cwd);
    command.arg(root.join("source").join(entry));
    let cp = run(&root, "cpython", &mut command);
    assert_eq!(
        cp.status.code(),
        Some(expected_exit),
        "CP意外退出，不build/JIT"
    );
    let stdout = cp.stdout;
    let stderr = cp.stderr;
    assert_eq!(
        stdout,
        std::fs::read(fixture.join(format!("{name}.stdout"))).unwrap(),
        "CP完整值合同不符，不build/JIT"
    );
    let labels = text(fixture.join(format!("{name}.labels")));
    let observed: Vec<_> = std::str::from_utf8(&stdout)
        .unwrap()
        .lines()
        .map(|line| line.split_once(' ').expect("每行必须为标签和值").0)
        .collect();
    assert_eq!(observed, labels.lines().collect::<Vec<_>>());
    assert!(!observed.iter().any(|label| label.starts_with("WRONG")));
    let frozen_trace = if expected_exit == 1 {
        let frozen = trace::EXPECTED;
        let projected = trace::project(&stderr, &root.join("source"));
        std::fs::write(root.join("cpython.trace"), &projected).unwrap();
        assert_eq!(projected, frozen, "CP完整帧链不符，不build/JIT");
        Some(frozen)
    } else {
        assert!(stderr.is_empty(), "CP成功应用stderr不为空，不build");
        None
    };
    let mut session = Session::default();
    let options = BuildOptions {
        input: root.join("source").join(entry),
        out_dir: root.join("out"),
        aot: false,
        search_paths: vec![],
    };
    std::fs::write(root.join("build.options"), format!("{options:?}\n")).unwrap();
    let built = ryupy_driver::build(&mut session, &options);
    std::fs::write(
        root.join("build.diagnostics"),
        format!("{:?}\n", session.diagnostics),
    )
    .unwrap();
    let artifact = match built {
        Ok(value) => {
            std::fs::write(root.join("build.status"), "success\n").unwrap();
            value
        }
        Err(error) => {
            std::fs::write(root.join("build.status"), format!("failed: {error:#}\n")).unwrap();
            panic!("动态except编译失败，不启动JIT: {error:#}");
        }
    };
    let options = ryupy_driver::dotnet::RunOptions {
        cwd: Some(cwd),
        env: vec![("LC_ALL".to_owned(), "C".to_owned())],
        ..Default::default()
    };
    let mut jit_command = Command::new(ryupy_driver::dotnet::dotnet_exe());
    jit_command
        .arg(&artifact.dll)
        .current_dir(root.join("jit"))
        .env("LC_ALL", "C");
    record_command(&root, "jit", &jit_command);
    let actual = match ryupy_driver::dotnet::run_dll_with(&artifact.dll, &options) {
        Ok(value) => value,
        Err(error) => {
            std::fs::write(root.join("jit.launch-error"), format!("{error:#}\n")).unwrap();
            panic!("JIT未成功启动: {error:#}");
        }
    };
    std::fs::write(root.join("jit.stdout"), &actual.stdout).unwrap();
    std::fs::write(root.join("jit.stderr"), &actual.stderr).unwrap();
    std::fs::write(
        root.join("jit.exit"),
        format!("{:?}\n", actual.status.code()),
    )
    .unwrap();
    assert_eq!(actual.status.code(), Some(expected_exit));
    assert_eq!(actual.stdout, stdout);
    if let Some(frozen) = frozen_trace {
        let projected = trace::project(&actual.stderr, &root.join("source"));
        std::fs::write(root.join("jit.trace"), &projected).unwrap();
        assert_eq!(projected, frozen);
    } else {
        assert_eq!(actual.stderr, stderr);
    }
}
