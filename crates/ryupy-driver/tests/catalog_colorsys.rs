//! 真实 CPython 差分与可搬迁 AOT 验收；编译器始终使用内嵌原源而非宿主模块。
use ryupy_driver::{BuildOptions, ModuleEnvironmentPolicy, Session};
use std::path::PathBuf;
use std::process::Command;

fn tree() -> PathBuf {
    static NEXT: std::sync::atomic::AtomicUsize = std::sync::atomic::AtomicUsize::new(0);
    let stamp = std::time::SystemTime::now()
        .duration_since(std::time::UNIX_EPOCH)
        .unwrap()
        .as_nanos();
    let serial = NEXT.fetch_add(1, std::sync::atomic::Ordering::Relaxed);
    let root = std::env::temp_dir().join(format!(
        "ryupy-colorsys-{}-{stamp}-{serial}",
        std::process::id()
    ));
    std::fs::create_dir(&root).unwrap();
    root
}

fn oracle(root: &std::path::Path) -> Vec<u8> {
    let python = std::env::var_os("RYUPY_FOREACH_PYTHON").expect("需要固定 CPython 3.12.13");
    let version = Command::new(&python)
        .args([
            "-I",
            "-c",
            "import sys; print('.'.join(map(str, sys.version_info[:3])))",
        ])
        .output()
        .unwrap();
    assert!(version.status.success());
    assert_eq!(
        String::from_utf8_lossy(&version.stdout).trim(),
        include_str!("../../../tests/pinned-python.txt").trim()
    );
    let output = Command::new(python)
        .arg("-I")
        .arg("-B")
        .arg(root.join("app.py"))
        .output()
        .unwrap();
    assert!(
        output.status.success(),
        "{}",
        String::from_utf8_lossy(&output.stderr)
    );
    output.stdout
}

fn verify(aot: bool) {
    let root = tree();
    let source = root.join("source");
    std::fs::create_dir(&source).unwrap();
    std::fs::write(
        source.join("app.py"),
        include_str!("../../../tests/e2e/phase3/fixtures/catalog_colorsys_api.py"),
    )
    .unwrap();
    let expected = oracle(&source);
    let mut session = Session {
        module_environment_policy: ModuleEnvironmentPolicy::OrdinaryDynamicPilot,
        ..Default::default()
    };
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
        command.env("PATH", root.join("no-tools"));
        command
    } else {
        let mut command = Command::new(ryupy_driver::dotnet::dotnet_exe());
        command.arg(artifacts.dll);
        command
    };
    let actual = command
        .current_dir(&root)
        .env_remove("PYTHONPATH")
        .env_remove("PYTHONHOME")
        .output()
        .unwrap();
    assert!(
        actual.status.success(),
        "{}",
        String::from_utf8_lossy(&actual.stderr)
    );
    assert_eq!(
        String::from_utf8_lossy(&actual.stdout),
        String::from_utf8_lossy(&expected)
    );
    println!(
        "{}：CPython 3.12.13 的 {} 行逐 API 输出一致",
        if aot { "AOT 搬迁" } else { "JIT" },
        String::from_utf8_lossy(&expected).lines().count()
    );
    std::fs::remove_dir_all(root).unwrap();
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
