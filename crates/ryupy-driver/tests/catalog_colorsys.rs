//! 真实 CPython 差分与可搬迁 AOT 验收；编译器始终使用内嵌原源而非宿主模块。
use ryupy_driver::{BuildOptions, Session};
use std::process::{Command, Output};

#[allow(dead_code)]
#[path = "support/temp_scope.rs"]
mod temp_scope;

fn oracle(root: &std::path::Path, cwd: &std::path::Path) -> Output {
    let python = std::env::var_os("PYTHON").expect("需要固定 CPython 3.12.13");
    assert!(
        std::path::Path::new(&python).is_absolute(),
        "PYTHON必须是固定CPython绝对路径"
    );
    let version = clean_command(&python, cwd)
        .args([
            "-I",
            "-B",
            "-S",
            "-c",
            "import sys; print(sys.implementation.name); print('.'.join(map(str, sys.version_info[:3])))",
        ])
        .output()
        .unwrap();
    assert!(version.status.success());
    assert_eq!(
        String::from_utf8_lossy(&version.stdout).trim(),
        format!(
            "cpython\n{}",
            include_str!("../../../tests/pinned-python.txt").trim()
        )
    );
    assert!(version.stderr.is_empty());
    let output = clean_command(&python, cwd)
        .arg("-I")
        .arg("-B")
        .arg("-S")
        .arg(root.join("app.py"))
        .output()
        .unwrap();
    assert!(
        output.status.success(),
        "{}",
        String::from_utf8_lossy(&output.stderr)
    );
    output
}

// 每臂独立cwd；固定环境只作用于子进程，避免并行测试间改变进程全局状态。
fn clean_command(executable: &std::ffi::OsStr, cwd: &std::path::Path) -> Command {
    let mut command = Command::new(executable);
    command
        .current_dir(cwd)
        .env_remove("PYTHONHOME")
        .env_remove("PYTHONPATH")
        .env("LC_ALL", "C")
        .env("TZ", "UTC");
    command
}

fn verify(aot: bool) {
    let scope = temp_scope::TempScope::new("colorsys");
    let root = scope.root().canonicalize().unwrap();
    let cp_cwd = root.join("cp");
    let jit_cwd = root.join("jit");
    std::fs::create_dir(&cp_cwd).unwrap();
    std::fs::create_dir(&jit_cwd).unwrap();
    let source = root.join("source");
    std::fs::create_dir(&source).unwrap();
    std::fs::write(
        source.join("app.py"),
        include_str!("../../../tests/e2e/phase3/fixtures/catalog_colorsys_api.py"),
    )
    .unwrap();
    let expected = oracle(&source, &cp_cwd);
    let mut session = Session::default();
    let artifacts = ryupy_driver::build(
        &mut session,
        &BuildOptions {
            input: source.join("app.py"),
            out_dir: root.join("out"),
            aot,
            search_paths: vec![],
        },
    )
    .unwrap_or_else(|error| panic!("{error:#}\n{}", session.render_diagnostics(false)));
    let report: serde_json::Value = serde_json::from_slice(
        &std::fs::read(artifacts.dll.parent().unwrap().join("app.source-plan.json")).unwrap(),
    )
    .unwrap();
    assert_eq!(report["modules"]["colorsys"]["provider"], "cpython_source");
    assert_eq!(report["static_closure_complete"], true);
    let mut command = if aot {
        let executable = artifacts.native_exe.unwrap();
        let dist = executable.parent().unwrap();
        let moved = root.join("relocated");
        std::fs::rename(dist, &moved).unwrap();
        let name = executable.file_name().unwrap();
        std::fs::remove_dir_all(root.join("out")).unwrap();
        std::fs::remove_dir_all(&source).unwrap();
        let mut command = Command::new(moved.join(name));
        // 搬迁后使用无 Python/dotnet 的 PATH，从另一个 cwd 启动原生文件。
        let empty_path = root.join("no-tools");
        std::fs::create_dir(&empty_path).unwrap();
        command.env("PATH", empty_path);
        command
    } else {
        let mut command = Command::new(ryupy_driver::dotnet::dotnet_exe());
        command.arg(artifacts.dll);
        command
    };
    let actual = command
        .current_dir(&jit_cwd)
        .env_remove("PYTHONPATH")
        .env_remove("PYTHONHOME")
        .env("LC_ALL", "C")
        .env("TZ", "UTC")
        .output()
        .unwrap();
    assert!(
        actual.status.success(),
        "{}",
        String::from_utf8_lossy(&actual.stderr)
    );
    assert_eq!(actual.status.code(), expected.status.code());
    assert_eq!(actual.stdout, expected.stdout);
    assert_eq!(actual.stderr, expected.stderr);
    println!(
        "{}：CPython 3.12.13 的 {} 行逐 API 输出一致",
        if aot { "AOT 搬迁" } else { "JIT" },
        String::from_utf8_lossy(&expected.stdout).lines().count()
    );
}

#[test]
fn catalog_colorsys_six_api_cpython_differential() {
    verify(false);
}

#[test]
#[ignore = "独立真实 ILC 发布与搬迁；先 make runtime，再单独串行执行"]
fn catalog_colorsys_aot_relocated_without_source_or_build() {
    verify(true);
}
