//! html.escape三个应用的CP-first留证；外层统一timeout，不新增隐式应用或重试。
//! 原始双流先落盘；每次新目录、新编译，成功亦保留，待父任务归档。
use ryupy_driver::{BuildOptions, Session};
use std::path::{Path, PathBuf};
use std::process::{Command, Output};
use std::sync::atomic::{AtomicU64, Ordering};
mod trace;

static NEXT: AtomicU64 = AtomicU64::new(0);

fn root(filename: &str) -> PathBuf {
    let stamp = std::time::SystemTime::now()
        .duration_since(std::time::UNIX_EPOCH)
        .unwrap()
        .as_nanos();
    let root = std::env::temp_dir().join(format!(
        "ryupy-html-escape-{filename}-{}-{stamp}-{}",
        std::process::id(),
        NEXT.fetch_add(1, Ordering::Relaxed)
    ));
    std::fs::create_dir(&root).unwrap();
    eprintln!("html.escape evidence: {}", root.display());
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
    match command.output() {
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

// 记录当前宿主的stdlib来源，不硬编码本机工具链；版本不得放宽。
fn pinned_python(root: &Path) -> (PathBuf, PathBuf) {
    let python = PathBuf::from(std::env::var_os("PYTHON").expect("须指定锁定CP绝对路径"));
    assert!(python.is_absolute(), "PYTHON必须为绝对路径");
    let mut command = python_command(&python, root);
    command.args([
        "-c",
        "import sys, html; print(sys.implementation.name); print('.'.join(map(str, sys.version_info[:3]))); print(html.__file__)",
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
    assert_eq!(lines.len(), 3);
    assert_eq!(lines[0], "cpython");
    assert_eq!(lines[1], "3.12.13");
    let html = PathBuf::from(lines[2]);
    assert!(html.is_absolute() && html.is_file());
    assert_eq!(
        html.file_name().and_then(|name| name.to_str()),
        Some("__init__.py")
    );
    assert_eq!(
        html.parent()
            .and_then(Path::file_name)
            .and_then(|name| name.to_str()),
        Some("html")
    );
    let library = std::fs::read_to_string(&html).unwrap();
    assert_eq!(
        library.lines().nth(18),
        Some("    s = s.replace(\"&\", \"&amp;\") # Must be done first!")
    );
    std::fs::write(root.join("cpython-html-source.py"), library).unwrap();
    (python, html)
}

fn labels(output: &Output, expected: &[&str]) {
    let text = std::str::from_utf8(&output.stdout).unwrap();
    let actual: Vec<_> = text
        .lines()
        .map(|line| line.split_once(' ').expect("输出必须有标签和值").0)
        .collect();
    assert_eq!(actual, expected, "CP应用标签缺失、重复或多余");
}

pub(super) fn check(
    filename: &str,
    source: &str,
    expected_labels: &[&str],
    expected_trace: Option<&[&str]>,
) {
    let root = root(filename);
    let input = root.join(filename);
    std::fs::write(&input, source).unwrap();
    let cp_cwd = root.join("cp");
    let jit_cwd = root.join("jit");
    std::fs::create_dir(&cp_cwd).unwrap();
    std::fs::create_dir(&jit_cwd).unwrap();
    let (python, html) = pinned_python(&root);
    let mut command = python_command(&python, &cp_cwd);
    command.arg(&input);
    let expected = run(&root, "cpython", &mut command);
    let expected_exit = if expected_trace.is_some() { 1 } else { 0 };
    assert_eq!(
        expected.status.code(),
        Some(expected_exit),
        "CP fixture失败，不启动目标编译/JIT: {}",
        String::from_utf8_lossy(&expected.stderr)
    );
    labels(&expected, expected_labels);
    if let Some(frozen) = expected_trace {
        let projected = trace::project(&expected, Some(&html));
        std::fs::write(root.join("cpython.trace-projection"), projected.join("\n")).unwrap();
        assert_eq!(projected, frozen);
    } else {
        assert!(expected.stderr.is_empty());
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
            panic!("html.escape编译失败，不启动JIT: {error:#}");
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
    if let Some(frozen) = expected_trace {
        let projected = trace::project(&actual, None);
        std::fs::write(root.join("jit.trace-projection"), projected.join("\n")).unwrap();
        assert_eq!(projected, frozen);
    } else {
        assert_eq!(actual.stderr, expected.stderr);
    }
}
