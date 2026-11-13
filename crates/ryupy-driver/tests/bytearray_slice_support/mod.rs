//! bytearray切片应用共享CP-first留证：先源、再版本与CP、再构建/JIT；原始输出在独占临时根内采集并统一清理。
//! 原始stdout/stderr/exit先落盘再断言；错误投影仅删冻结源回显，根生命周期由RAII统一清理。
use ryupy_driver::{BuildOptions, Session};
use std::path::{Path, PathBuf};
use std::process::{Command, Output};

#[path = "../../src/test_temp.rs"]
mod test_temp;
mod trace;

// 独占临时根随调用作用域清理，断言展开也不遗留原始流与构建产物。
pub(super) fn create_root(label: &str) -> test_temp::Root {
    test_temp::Root::new(&format!("bytearray-slice-{label}"))
}

fn save_output(root: &Path, label: &str, output: &Output) {
    for (stream, bytes) in [("stdout", &output.stdout), ("stderr", &output.stderr)] {
        std::fs::write(root.join(format!("{label}.{stream}")), bytes).unwrap();
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
    python
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
    expected_trace: Option<&[&str]>,
) {
    let root = create_root(filename);
    let input = root.join(filename);
    // 即使版本probe或CP自证失败，也必须保留这次实际待验证的原source。
    std::fs::write(&input, source).unwrap();
    let cp_cwd = root.join("cp");
    let jit_cwd = root.join("jit");
    std::fs::create_dir(&cp_cwd).unwrap();
    std::fs::create_dir(&jit_cwd).unwrap();
    let python = pinned_python(&root);
    let mut command = python_command(&python, &cp_cwd);
    command.arg(&input);
    let expected = run(&root, "cpython", &mut command);
    let expected_exit = if expected_trace.is_some() { 1 } else { 0 };
    assert_eq!(
        expected.status.code(),
        Some(expected_exit),
        "CP源合同失败，不启动编译/JIT: {}",
        String::from_utf8_lossy(&expected.stderr)
    );
    labels(&expected, expected_labels);
    if let Some(frozen) = expected_trace {
        let projected = trace::project(&expected);
        std::fs::write(root.join("cpython.trace-projection"), projected.join("\n")).unwrap();
        assert_eq!(projected, frozen, "CP完整链不符，不启动编译/JIT");
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
            panic!("编译失败，不启动JIT: {error:#}");
        }
    };
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
    assert_eq!(actual.status.code(), expected.status.code());
    assert_eq!(actual.stdout, expected.stdout);
    if let Some(frozen) = expected_trace {
        let projected = trace::project(&actual);
        std::fs::write(root.join("jit.trace-projection"), projected.join("\n")).unwrap();
        assert_eq!(projected, frozen);
    } else {
        assert_eq!(actual.stderr, expected.stderr);
    }
}
