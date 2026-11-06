//! 每应用独占Root；成功失败均保原双流/状态/source/hash。外层预算由父拥有。
use ryupy_driver::{BuildOptions, Session};
use sha2::{Digest, Sha256};
use std::path::{Path, PathBuf};
use std::process::{Command, Output};
use std::sync::atomic::{AtomicU64, Ordering};
mod trace;

static NEXT: AtomicU64 = AtomicU64::new(0);
pub(crate) struct Root {
    pub(crate) path: PathBuf,
    success: bool,
}
impl Drop for Root {
    fn drop(&mut self) {
        let state = if self.success && !std::thread::panicking() {
            "成功"
        } else {
            "失败"
        };
        eprintln!("typing-namedtuple{state}证据保留: {}", self.path.display());
    }
}
impl Root {
    pub(crate) fn new(label: &str) -> Self {
        let stamp = std::time::SystemTime::now()
            .duration_since(std::time::UNIX_EPOCH)
            .unwrap()
            .as_nanos();
        let path = std::env::temp_dir().join(format!(
            "ryupy-typing-namedtuple-{label}-{}-{stamp}-{}",
            std::process::id(),
            NEXT.fetch_add(1, Ordering::Relaxed),
        ));
        std::fs::create_dir(&path).unwrap();
        eprintln!("typing-namedtuple证据目录: {}", path.display());
        Self {
            path,
            success: false,
        }
    }
    pub(crate) fn complete(&mut self) {
        self.success = true;
    }
}
fn hash(root: &Root, label: &str, bytes: &[u8]) {
    std::fs::write(
        root.path.join(format!("{label}.sha256")),
        format!("{:x}\n", Sha256::digest(bytes)),
    )
    .unwrap();
}
fn save(root: &Root, label: &str, output: &Output) {
    std::fs::write(root.path.join(format!("{label}.stdout")), &output.stdout).unwrap();
    std::fs::write(root.path.join(format!("{label}.stderr")), &output.stderr).unwrap();
    std::fs::write(
        root.path.join(format!("{label}.exit")),
        format!("{:?}\n", output.status.code()),
    )
    .unwrap();
    hash(root, &format!("{label}.stdout"), &output.stdout);
    hash(root, &format!("{label}.stderr"), &output.stderr);
}
fn run(root: &Root, label: &str, command: &mut Command) -> Output {
    std::fs::write(
        root.path.join(format!("{label}.command")),
        format!("{command:?}\n"),
    )
    .unwrap();
    let start = std::time::Instant::now();
    let result = command.output();
    std::fs::write(
        root.path.join(format!("{label}.elapsed")),
        format!("{:?}\n", start.elapsed()),
    )
    .unwrap();
    match result {
        Ok(output) => {
            save(root, label, &output);
            output
        }
        Err(error) => {
            std::fs::write(
                root.path.join(format!("{label}.launch-error")),
                error.to_string(),
            )
            .unwrap();
            panic!("{label}启动失败: {error}");
        }
    }
}
fn command(python: &Path, cwd: &Path) -> Command {
    let mut command = Command::new(python);
    command
        .args(["-B", "-S"])
        .current_dir(cwd)
        .env_remove("PYTHONHOME")
        .env_remove("PYTHONPATH")
        .env("LC_ALL", "C");
    command
}
fn python(root: &Root, neutral: &Path) -> PathBuf {
    let python = PathBuf::from(std::env::var_os("PYTHON").expect("须指定锁定CP绝对路径"));
    assert!(python.is_absolute());
    let pinned = std::fs::read_to_string(
        ryupy_driver::runtime::workspace_root().join("tests/pinned-python.txt"),
    )
    .unwrap();
    assert_eq!(pinned.trim(), "3.12.13");
    let mut cmd = command(&python, neutral);
    cmd.args(["-c", "import sys; print(sys.implementation.name); print('.'.join(map(str, sys.version_info[:3])))"]);
    let version = run(root, "python-version", &mut cmd);
    assert_eq!(version.status.code(), Some(0));
    assert!(version.stderr.is_empty());
    assert_eq!(version.stdout, b"cpython\n3.12.13\n");
    python
}
fn labels(output: &Output, expected: &[&str]) {
    let text = std::str::from_utf8(&output.stdout).unwrap();
    let actual: Vec<_> = text
        .lines()
        .map(|line| line.split_once(' ').expect("标签和值缺失").0)
        .collect();
    assert_eq!(actual, expected, "CP标签缺失/重复/多余，不进入编译");
    assert!(text.ends_with(".complete True\n"));
}
pub(super) fn success(filename: &str, source: &str, helpers: &[(&str, &str)], expected: &[&str]) {
    check(filename, source, helpers, Some(expected));
}
pub(super) fn uncaught(filename: &str, source: &str) {
    check(filename, source, &[], None);
}
fn check(filename: &str, source: &str, helpers: &[(&str, &str)], expected_labels: Option<&[&str]>) {
    let mut root = Root::new(filename);
    let sources = root.path.join("source");
    let cp_cwd = root.path.join("cp");
    let jit_cwd = root.path.join("jit");
    for path in [&sources, &cp_cwd, &jit_cwd] {
        std::fs::create_dir(path).unwrap();
    }
    assert_eq!(
        Path::new(filename).file_name().and_then(|x| x.to_str()),
        Some(filename)
    );
    let input = sources.join(filename);
    std::fs::write(&input, source).unwrap();
    hash(&root, &format!("source-{filename}"), source.as_bytes());
    for (name, text) in helpers {
        assert_eq!(
            Path::new(name).file_name().and_then(|x| x.to_str()),
            Some(*name)
        );
        std::fs::write(sources.join(name), text).unwrap();
        hash(&root, &format!("source-{name}"), text.as_bytes());
    }
    let python = python(&root, &cp_cwd);
    let mut cmd = command(&python, &cp_cwd);
    cmd.arg(&input);
    let expected = run(&root, "cpython", &mut cmd);
    // 先落原双流/状态，再核预期exit及完整oracle；失败不进入build。
    let expected_exit = if expected_labels.is_some() { 0 } else { 1 };
    assert_eq!(
        expected.status.code(),
        Some(expected_exit),
        "CP先验失败: {}",
        String::from_utf8_lossy(&expected.stderr)
    );
    let cp_trace = if let Some(expected_labels) = expected_labels {
        assert!(expected.stderr.is_empty());
        labels(&expected, expected_labels);
        None
    } else {
        assert!(expected.stdout.is_empty());
        Some(trace::checked(&expected, &input, source, true))
    };
    let mut session = Session::default();
    let options = BuildOptions {
        input: input.clone(),
        out_dir: root.path.join("out"),
        aot: false,
        search_paths: vec![],
    };
    std::fs::write(root.path.join("build.options"), format!("{options:?}\n")).unwrap();
    let start = std::time::Instant::now();
    let built = ryupy_driver::build(&mut session, &options);
    std::fs::write(
        root.path.join("build.elapsed"),
        format!("{:?}\n", start.elapsed()),
    )
    .unwrap();
    std::fs::write(
        root.path.join("build.diagnostics"),
        format!("{:?}\n", session.diagnostics),
    )
    .unwrap();
    let artifact = match built {
        Ok(artifact) => {
            std::fs::write(root.path.join("build.status"), "success\n").unwrap();
            artifact
        }
        Err(error) => {
            std::fs::write(
                root.path.join("build.status"),
                format!("failed: {error:#}\n"),
            )
            .unwrap();
            panic!("build失败，不启动JIT: {error:#}");
        }
    };
    hash(&root, "build.dll", &std::fs::read(&artifact.dll).unwrap());
    // 库build不伪造进程退出码；常规默认优化/sampling完全不变。
    let options = ryupy_driver::dotnet::RunOptions {
        cwd: Some(jit_cwd),
        env: vec![("LC_ALL".into(), "C".into())],
        ..Default::default()
    };
    std::fs::write(
        root.path.join("jit.command"),
        format!(
            "executable={:?}\nargv={:?}\noptions={options:?}\n",
            ryupy_driver::dotnet::dotnet_exe(),
            [&artifact.dll]
        ),
    )
    .unwrap();
    let start = std::time::Instant::now();
    let result = ryupy_driver::dotnet::run_dll_with(&artifact.dll, &options);
    std::fs::write(
        root.path.join("jit.elapsed"),
        format!("{:?}\n", start.elapsed()),
    )
    .unwrap();
    let actual = match result {
        Ok(output) => output,
        Err(error) => {
            std::fs::write(root.path.join("jit.launch-error"), format!("{error:#}\n")).unwrap();
            panic!("JIT启动失败: {error:#}");
        }
    };
    save(&root, "jit", &actual);
    assert_eq!(actual.status.code(), expected.status.code());
    assert_eq!(actual.stdout, expected.stdout);
    if let Some(cp_trace) = cp_trace {
        let jit_trace = trace::checked(&actual, &input, source, false);
        assert_eq!(jit_trace, cp_trace);
    } else {
        assert_eq!(actual.stderr, expected.stderr);
    }
    root.complete();
}
mod shape;
pub(super) fn structure() {
    shape::check();
}
