//! typevar-foundation公开入口应用共享CP-first留证：先源、再版本与CP、再构建/JIT；成功失败均保独占证据。
//! 原始stdout/stderr/exit先落盘再断言；固定golden与源码同时保存，生命周期不靠临时Drop。
use ryupy_driver::{BuildOptions, Session};
use sha2::{Digest, Sha256};
use std::path::{Path, PathBuf};
use std::process::{Command, Output};
use std::sync::atomic::{AtomicU64, Ordering};

static NEXT: AtomicU64 = AtomicU64::new(0);

pub(super) fn create_root(label: &str) -> PathBuf {
    let stamp = std::time::SystemTime::now()
        .duration_since(std::time::UNIX_EPOCH)
        .unwrap()
        .as_nanos();
    let base = std::env::var_os("RYUPY_TYPEVAR_FOUNDATION_EVIDENCE_ROOT")
        .map(PathBuf::from)
        .unwrap_or_else(std::env::temp_dir);
    std::fs::create_dir_all(&base).unwrap();
    let root = base.join(format!(
        "ryupy-typevar-foundation-{label}-{}-{stamp}-{}",
        std::process::id(),
        NEXT.fetch_add(1, Ordering::Relaxed)
    ));
    std::fs::create_dir(&root).unwrap();
    eprintln!("typevar-foundation公开入口结果证据: {}", root.display());
    root
}

fn save_output(root: &Path, label: &str, output: &Output) {
    for (stream, bytes) in [("stdout", &output.stdout), ("stderr", &output.stderr)] {
        std::fs::write(root.join(format!("{label}.{stream}")), bytes).unwrap();
        std::fs::write(
            root.join(format!("{label}.{stream}.sha256")),
            format!("{:x}\n", Sha256::digest(bytes)),
        )
        .unwrap();
    }
    std::fs::write(
        root.join(format!("{label}.exit")),
        format!("{:?}\n", output.status.code()),
    )
    .unwrap();
}

fn run(root: &Path, label: &str, command: &mut Command) -> Output {
    std::fs::write(
        root.join(format!("{label}.command")),
        format!(
            "command={command:?}\nresolved={:?}\nargs={:?}\ncwd={:?}\n",
            std::fs::canonicalize(command.get_program()),
            command.get_args().collect::<Vec<_>>(),
            command.get_current_dir()
        ),
    )
    .unwrap();
    match command.output() {
        Ok(output) => {
            save_output(root, label, &output);
            output
        }
        Err(error) => {
            std::fs::write(
                root.join(format!("{label}.launch-error")),
                error.to_string(),
            )
            .unwrap();
            panic!("{label}未成功启动: {error}");
        }
    }
}

fn python_command(python: &Path, cwd: &Path) -> Command {
    let mut command = Command::new(python);
    command
        .args(["-I", "-B", "-S"])
        .current_dir(cwd)
        .env_remove("PYTHONHOME")
        .env_remove("PYTHONPATH")
        .env("LC_ALL", "C");
    command
}

fn pinned_python(root: &Path) -> PathBuf {
    let python = PathBuf::from(std::env::var_os("PYTHON").expect("须指定固定CP绝对路径"));
    assert!(python.is_absolute(), "PYTHON必须为绝对路径");
    let mut command = python_command(&python, root);
    command.args([
        "-c",
        "import sys; print(sys.implementation.name); print('.'.join(map(str, sys.version_info[:3])))",
    ]);
    let version = run(root, "python-version", &mut command);
    assert!(version.status.success());
    assert!(version.stderr.is_empty());
    let pinned = std::fs::read_to_string(
        ryupy_driver::runtime::workspace_root().join("tests/pinned-python.txt"),
    )
    .unwrap();
    assert_eq!(pinned.trim(), "3.12.13");
    assert_eq!(version.stdout, b"cpython\n3.12.13\n");
    save_hash(root, "python", &python);
    python
}

fn save_hash(root: &Path, label: &str, path: &Path) {
    let bytes = std::fs::read(path).unwrap();
    std::fs::write(
        root.join(format!("{label}.identity")),
        format!(
            "path={}\nsha256={:x}\n",
            path.display(),
            Sha256::digest(bytes)
        ),
    )
    .unwrap();
}

fn labels(output: &Output, expected: &[&str]) {
    let text = std::str::from_utf8(&output.stdout).unwrap();
    let actual: Vec<_> = text
        .lines()
        .map(|line| line.split_whitespace().next().expect("应用输出不得空行"))
        .collect();
    assert_eq!(actual, expected, "CP标签丢失、重复或额外输出");
}

pub(super) fn check(
    filename: &str,
    source: &str,
    expected_labels: &[&str],
    expected_stdout: &[u8],
) {
    let root = create_root(filename);
    let input = root.join(filename);
    // 即使版本probe或CP自证失败，也必须保留这次实际待验证的原source。
    std::fs::write(&input, source).unwrap();
    std::fs::write(
        root.join("source.sha256"),
        format!("{:x}\n", Sha256::digest(source.as_bytes())),
    )
    .unwrap();
    std::fs::write(root.join("frozen.stdout"), expected_stdout).unwrap();
    let cp_cwd = root.join("cp");
    let jit_cwd = root.join("jit");
    std::fs::create_dir(&cp_cwd).unwrap();
    std::fs::create_dir(&jit_cwd).unwrap();
    let python = pinned_python(&root);
    let mut command = python_command(&python, &cp_cwd);
    command.arg(&input);
    let expected = run(&root, "cpython", &mut command);
    let expected_exit = 0;
    assert_eq!(
        expected.status.code(),
        Some(expected_exit),
        "CP源合同失败，不启动编译/JIT: {}",
        String::from_utf8_lossy(&expected.stderr)
    );
    labels(&expected, expected_labels);
    assert_eq!(
        expected.stdout, expected_stdout,
        "CP原始stdout偏离冻结golden"
    );
    assert!(expected.stderr.is_empty());

    // CP冻结阶段明确禁止构建/JIT；原源与原始双流已经完整保存。
    if std::env::var_os("RYUPY_TYPEVAR_FOUNDATION_CP_ONLY").is_some() {
        return;
    }

    // 固定实际选中的运行时与driver；编译仅复制已构建DLL，绝不触发runtime构建。
    let runtime = ryupy_driver::runtime::runtime_dir().join("RyuPy.Runtime.dll");
    save_hash(&root, "runtime-before", &runtime);
    save_hash(&root, "driver", &std::env::current_exe().unwrap());
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
            panic!("编译失败，不启动JIT: {error:#}");
        }
    };
    save_hash(
        &root,
        "runtime-copied",
        &artifact.dll.parent().unwrap().join("RyuPy.Runtime.dll"),
    );
    save_hash(&root, "artifact", &artifact.dll);
    let options = ryupy_driver::dotnet::RunOptions {
        cwd: Some(jit_cwd),
        env: vec![("LC_ALL".to_owned(), "C".to_owned())],
        ..Default::default()
    };
    std::fs::write(
        root.join("jit.command"),
        format!(
            "dotnet={:?}\ndll={}\noptions={options:?}\n",
            ryupy_driver::dotnet::dotnet_exe(),
            artifact.dll.display()
        ),
    )
    .unwrap();
    let actual = match ryupy_driver::dotnet::run_dll_with(&artifact.dll, &options) {
        Ok(output) => output,
        Err(error) => {
            std::fs::write(root.join("jit.launch-error"), format!("{error:#}\n")).unwrap();
            panic!("JIT未成功启动: {error:#}");
        }
    };
    save_output(&root, "jit", &actual);
    save_hash(&root, "runtime-after", &runtime);
    assert_eq!(
        std::fs::read(root.join("runtime-before.identity")).unwrap(),
        std::fs::read(root.join("runtime-after.identity")).unwrap(),
        "运行时在验证期间变化，结果不可作为稳定版本验收"
    );
    assert_eq!(actual.status.code(), expected.status.code());
    assert_eq!(actual.stdout, expected.stdout);
    assert_eq!(actual.stderr, expected.stderr);
}
