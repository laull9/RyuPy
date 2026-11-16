//! CP-first隔离执行与原流留证；不删除成功证据，不执行项目入口或AOT。
//! 每次进程/编译前后保留明确状态，禁止复用旧DLL；固定版本而不硬编码宿主安装根。
use ryupy_driver::{BuildOptions, Session};
use std::path::{Path, PathBuf};
use std::process::{Command, Output};
use std::sync::atomic::{AtomicU64, Ordering};
mod shape;

static NEXT: AtomicU64 = AtomicU64::new(0);

fn root(filename: &str) -> PathBuf {
    let stamp = std::time::SystemTime::now()
        .duration_since(std::time::UNIX_EPOCH)
        .unwrap()
        .as_nanos();
    let root = std::env::temp_dir().join(format!(
        "ryupy-namedtuple-{filename}-{}-{stamp}-{}",
        std::process::id(),
        NEXT.fetch_add(1, Ordering::Relaxed)
    ));
    // 排他创建才取得本次目录；无Drop清理，父任务归档后自行处理。
    std::fs::create_dir(&root).unwrap();
    eprintln!("namedtuple evidence: {}", root.display());
    root
}

fn save(root: &Path, label: &str, output: &Output) {
    std::fs::write(root.join(format!("{label}.stdout")), &output.stdout).unwrap();
    std::fs::write(root.join(format!("{label}.stderr")), &output.stderr).unwrap();
    std::fs::write(
        root.join(format!("{label}.exit")),
        format!("{:?}\n", output.status.code()),
    )
    .unwrap();
}

fn run(root: &Path, label: &str, command: &mut Command) -> Output {
    std::fs::write(
        root.join(format!("{label}.command")),
        format!("{command:?}\n"),
    )
    .unwrap();
    let result = command.output();
    match result {
        Ok(output) => {
            save(root, label, &output);
            output
        }
        Err(error) => {
            std::fs::write(
                root.join(format!("{label}.launch-error")),
                error.to_string(),
            )
            .unwrap();
            panic!("{label}进程未成功启动: {error}");
        }
    }
}

fn python_command(python: &Path, cwd: &Path) -> Command {
    let mut command = Command::new(python);
    command
        .args(["-B", "-S"])
        .current_dir(cwd)
        .env_remove("PYTHONHOME")
        .env_remove("PYTHONPATH")
        .env("LC_ALL", "C");
    command
}

fn pinned_python(root: &Path) -> PathBuf {
    let python = PathBuf::from(std::env::var_os("PYTHON").expect("须指定锁定CP绝对路径"));
    assert!(python.is_absolute(), "PYTHON必须为绝对路径");
    let mut command = python_command(&python, root);
    command.args([
        "-c",
        "import sys, collections; print(sys.implementation.name); print('.'.join(map(str, sys.version_info[:3]))); print(collections.__file__)",
    ]);
    let version = run(root, "python-version", &mut command);
    assert!(version.status.success());
    assert!(version.stderr.is_empty());
    let pinned = std::fs::read_to_string(
        ryupy_driver::runtime::workspace_root().join("tests/pinned-python.txt"),
    )
    .unwrap();
    assert_eq!(pinned.trim(), "3.12.13");
    let output = std::str::from_utf8(&version.stdout).unwrap();
    let lines: Vec<_> = output.lines().collect();
    assert_eq!(lines.len(), 3, "版本探针必须只有实现、版本和实际stdlib路径");
    assert_eq!(lines[0], "cpython");
    assert_eq!(lines[1], "3.12.13");
    // 实际安装根属于宿主环境，不是产品合同；原流仍保留完整路径。
    let collections = Path::new(lines[2]);
    assert!(collections.is_absolute() && collections.is_file());
    assert_eq!(
        collections.file_name().and_then(|name| name.to_str()),
        Some("__init__.py")
    );
    assert_eq!(
        collections
            .parent()
            .and_then(Path::file_name)
            .and_then(|name| name.to_str()),
        Some("collections")
    );
    python
}

// 标签序列连同源内assert守住应用非空；每个错误完整消息仍属于逐字节stdout oracle。
fn labels(output: &Output, expected: &[&str]) {
    let text = std::str::from_utf8(&output.stdout).unwrap();
    let actual: Vec<_> = text
        .lines()
        .map(|line| line.split_once(' ').expect("输出必须有标签和值").0)
        .collect();
    assert_eq!(actual, expected, "CP应用标签缺失、重复或额外输出");
}

pub(super) fn success(
    filename: &str,
    source: &str,
    helpers: &[(&str, &str)],
    expected_labels: &[&str],
) {
    checked_success(filename, source, helpers, expected_labels, false);
}

pub(super) fn origin_success(
    filename: &str,
    source: &str,
    helpers: &[(&str, &str)],
    expected_labels: &[&str],
) {
    checked_success(filename, source, helpers, expected_labels, true);
}

fn checked_success(
    filename: &str,
    source: &str,
    helpers: &[(&str, &str)],
    expected_labels: &[&str],
    check_origin: bool,
) {
    let root = root(filename);
    let input = root.join(filename);
    std::fs::write(&input, source).unwrap();
    for (name, text) in helpers {
        assert!(Path::new(name).file_name().unwrap() == *name);
        std::fs::write(root.join(name), text).unwrap();
    }
    let cp_cwd = root.join("cp");
    let jit_cwd = root.join("jit");
    std::fs::create_dir(&cp_cwd).unwrap();
    std::fs::create_dir(&jit_cwd).unwrap();
    let python = pinned_python(&root);
    let mut command = python_command(&python, &cp_cwd);
    command.arg(&input);
    let expected = run(&root, "cpython", &mut command);
    assert_eq!(
        expected.status.code(),
        Some(0),
        "CP fixture失败，不启动目标编译/JIT: {}",
        String::from_utf8_lossy(&expected.stderr)
    );
    assert!(expected.stderr.is_empty());
    labels(&expected, expected_labels);
    if check_origin {
        shape::origins(&root, filename, source, helpers);
    }

    let mut session = Session::default();
    let options = BuildOptions {
        input,
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
        Ok(artifact) => {
            std::fs::write(root.join("build.status"), "success\n").unwrap();
            artifact
        }
        Err(error) => {
            std::fs::write(root.join("build.status"), format!("failed: {error:#}\n")).unwrap();
            panic!("namedtuple编译失败，不启动JIT: {error:#}");
        }
    };
    let options = ryupy_driver::dotnet::RunOptions {
        cwd: Some(jit_cwd),
        env: vec![("LC_ALL".to_owned(), "C".to_owned())],
        ..Default::default()
    };
    std::fs::write(
        root.join("jit.command"),
        format!("dll={}\noptions={options:?}\n", artifact.dll.display()),
    )
    .unwrap();
    let result = ryupy_driver::dotnet::run_dll_with(&artifact.dll, &options);
    let actual = match result {
        Ok(output) => output,
        Err(error) => {
            std::fs::write(root.join("jit.launch-error"), format!("{error:#}\n")).unwrap();
            panic!("JIT未成功启动: {error:#}");
        }
    };
    save(&root, "jit", &actual);
    assert_eq!(actual.status.code(), expected.status.code());
    assert_eq!(actual.stdout, expected.stdout);
    assert_eq!(actual.stderr, expected.stderr);
}
