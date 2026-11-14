//! typing.IO真实应用CP-first留证；每调用独占Root，不清成功或失败证据。
//! 沿既有driver库build/JIT流程，不另建golden框架；执行预算与外层timeout由父独占。
use ryupy_driver::{BuildOptions, Session};
use std::path::{Path, PathBuf};
use std::process::{Command, Output};
use std::sync::atomic::{AtomicU64, Ordering};

static NEXT: AtomicU64 = AtomicU64::new(0);
pub(super) struct Root(pub PathBuf);
impl Drop for Root {
    fn drop(&mut self) {
        // guard直到最后断言才释放；包含panic的所有路径保留目录，父负责审阅归档。
        eprintln!("typing-io证据保留: {}", self.0.display());
    }
}
pub(super) fn root(label: &str) -> Root {
    let stamp = std::time::SystemTime::now()
        .duration_since(std::time::UNIX_EPOCH)
        .unwrap()
        .as_nanos();
    let path = std::env::temp_dir().join(format!(
        "ryupy-typing-io-{label}-{}-{stamp}-{}",
        std::process::id(),
        NEXT.fetch_add(1, Ordering::Relaxed)
    ));
    // 不能用create_dir_all吞碰撞；只有创建成功才获得该目录所有权。
    std::fs::create_dir(&path).unwrap();
    eprintln!("typing-io证据目录已创建: {}", path.display());
    Root(path)
}
fn save(root: &Root, label: &str, output: &Output) {
    std::fs::write(root.0.join(format!("{label}.stdout")), &output.stdout).unwrap();
    std::fs::write(root.0.join(format!("{label}.stderr")), &output.stderr).unwrap();
    std::fs::write(
        root.0.join(format!("{label}.exit")),
        format!("{:?}\n", output.status.code()),
    )
    .unwrap();
}
fn run(root: &Root, label: &str, command: &mut Command) -> Output {
    std::fs::write(
        root.0.join(format!("{label}.command")),
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
                root.0.join(format!("{label}.launch-error")),
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
fn python(root: &Root, neutral_cwd: &Path) -> PathBuf {
    let python = PathBuf::from(std::env::var_os("PYTHON").expect("须指定锁定CP绝对路径"));
    assert!(python.is_absolute());
    let pinned = std::fs::read_to_string(
        ryupy_driver::runtime::workspace_root().join("tests/pinned-python.txt"),
    )
    .unwrap();
    assert_eq!(pinned.trim(), "3.12.13");
    // -c的cwd不含fixture辅助模块；版本检查不提前导入typing或辅助模块。
    let mut cmd = command(&python, neutral_cwd);
    cmd.args(["-c", "import sys; print(sys.implementation.name); print('.'.join(map(str, sys.version_info[:3])))"]);
    let version = run(root, "python-version", &mut cmd);
    assert_eq!(version.status.code(), Some(0));
    assert!(version.stderr.is_empty());
    assert_eq!(version.stdout, b"cpython\n3.12.13\n");
    python
}
pub(super) fn check(
    filename: &str,
    source: &str,
    expected_labels: &[&str],
    provider: Option<&str>,
) {
    let root = root(filename);
    let sources = root.0.join("source");
    let cp_cwd = root.0.join("cp");
    let jit_cwd = root.0.join("jit");
    for path in [&sources, &cp_cwd, &jit_cwd] {
        std::fs::create_dir(path).unwrap();
    }
    let input = sources.join(filename);
    std::fs::write(&input, source).unwrap();
    if let Some(text) = provider {
        std::fs::write(sources.join("io_provider.py"), text).unwrap();
    }
    let python = python(&root, &cp_cwd);
    let mut cmd = command(&python, &cp_cwd);
    cmd.arg(&input);
    let expected = run(&root, "cpython", &mut cmd);
    assert_eq!(
        expected.status.code(),
        Some(0),
        "CP原流先验失败，不进入build/JIT: {}",
        String::from_utf8_lossy(&expected.stderr)
    );
    assert!(expected.stderr.is_empty());
    let stdout = std::str::from_utf8(&expected.stdout).unwrap();
    let labels: Vec<_> = stdout
        .lines()
        .map(|line| line.split_once(' ').expect("必须输出完整标签和值").0)
        .collect();
    assert_eq!(
        labels, expected_labels,
        "CP标签缺失/重复/多余，不启动目标编译"
    );
    assert!(stdout.ends_with(".complete True\n"));
    // 每个fixture的内部assert先保证CP正例成立，不能靠两边相同错误或空输出验收。
    let mut session = Session::default();
    let options = BuildOptions {
        input,
        out_dir: root.0.join("out"),
        aot: false,
        search_paths: vec![],
    };
    std::fs::write(root.0.join("build.options"), format!("{options:?}\n")).unwrap();
    let built = ryupy_driver::build(&mut session, &options);
    std::fs::write(
        root.0.join("build.diagnostics"),
        format!("{:?}\n", session.diagnostics),
    )
    .unwrap();
    // build是库调用，不捏造compile进程的exit/stdout/stderr。
    let artifact = match built {
        Ok(artifact) => {
            std::fs::write(root.0.join("build.status"), "success\n").unwrap();
            artifact
        }
        Err(error) => {
            std::fs::write(root.0.join("build.status"), format!("failed: {error:#}\n")).unwrap();
            panic!("编译失败，不启动JIT: {error:#}");
        }
    };
    let options = ryupy_driver::dotnet::RunOptions {
        cwd: Some(jit_cwd),
        env: vec![("LC_ALL".into(), "C".into())],
        ..Default::default()
    };
    std::fs::write(
        root.0.join("jit.command"),
        format!(
            "executable={:?}\nargv={:?}\noptions={options:?}\n",
            ryupy_driver::dotnet::dotnet_exe(),
            [&artifact.dll],
        ),
    )
    .unwrap();
    let actual = match ryupy_driver::dotnet::run_dll_with(&artifact.dll, &options) {
        Ok(output) => output,
        Err(error) => {
            std::fs::write(root.0.join("jit.launch-error"), format!("{error:#}\n")).unwrap();
            panic!("JIT启动失败: {error:#}");
        }
    };
    save(&root, "jit", &actual);
    assert_eq!(actual.status.code(), expected.status.code());
    assert_eq!(actual.stdout, expected.stdout);
    assert_eq!(actual.stderr, expected.stderr);
}
