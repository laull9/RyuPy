//! ADR0222真实产品与独立宿主；同次CP预期、仓库外构建，AOT删input/build后搬迁执行。
use ryupy_driver::{BuildOptions, Session, dotnet, runtime};
use std::{
    path::{Path, PathBuf},
    process::Command,
    sync::atomic::{AtomicU64, Ordering},
};

struct Root(PathBuf);
impl Root {
    fn new() -> Self {
        static NEXT: AtomicU64 = AtomicU64::new(0);
        let path = std::env::temp_dir().join(format!(
            "ryupy-native-startup-{}-{}",
            std::process::id(),
            NEXT.fetch_add(1, Ordering::Relaxed)
        ));
        assert!(!path.starts_with(runtime::workspace_root()));
        std::fs::create_dir(&path).unwrap();
        Self(path)
    }
}
impl Drop for Root {
    fn drop(&mut self) {
        let _ = std::fs::remove_dir_all(&self.0);
    }
}

fn python() -> String {
    let python = std::env::var("PYTHON").expect("显式指定固定CPython3.12.13");
    let result = Command::new(&python)
        .args([
            "-B",
            "-c",
            "import sys; assert sys.version_info[:3] == (3,12,13), sys.version",
        ])
        .output()
        .unwrap();
    assert!(result.status.success(), "{:?}", result);
    python
}

fn object_evidence(root: &Path) -> (usize, bool) {
    let mut count = 0;
    let mut inspect = false;
    for entry in std::fs::read_dir(root).unwrap() {
        let path = entry.unwrap().path();
        if path.is_dir() {
            let (nested_count, nested_inspect) = object_evidence(&path);
            count += nested_count;
            inspect |= nested_inspect;
        } else if path
            .extension()
            .is_some_and(|ext| ext == "o" || ext == "obj")
        {
            count += 1;
            inspect |= std::fs::read(path)
                .unwrap()
                .windows(b"PyInspect".len())
                .any(|value| value == b"PyInspect");
        }
    }
    (count, inspect)
}

fn differential(files: &[&str], includes: &[&str], instance: bool, aot: bool, inspect: bool) {
    let root = Root::new();
    let input = root.0.join("input");
    std::fs::create_dir(&input).unwrap();
    for name in files {
        let source = runtime::workspace_root().join(name);
        std::fs::copy(&source, input.join(source.file_name().unwrap())).unwrap();
    }
    let entry = input.join(Path::new(files[0]).file_name().unwrap());
    let oracle = Command::new(python())
        .arg("-B")
        .arg(&entry)
        .current_dir(&input)
        .output()
        .unwrap();
    assert!(
        oracle.status.success() && oracle.stderr.is_empty(),
        "{:?}",
        oracle
    );
    let mut session = Session {
        include_modules: includes.iter().map(|value| (*value).into()).collect(),
        ..Default::default()
    };
    if instance {
        session.instance_modules = vec!["__main__".into()];
    }
    let product = root.0.join("product");
    let artifacts = ryupy_driver::build(
        &mut session,
        &BuildOptions {
            input: entry,
            out_dir: product.clone(),
            aot,
            search_paths: vec![],
        },
    )
    .unwrap_or_else(|error| panic!("{error}: {}", session.render_diagnostics(false)));
    assert!(session.cpython_provider.is_none());
    std::fs::remove_dir_all(input).unwrap();
    let actual = if let Some(executable) = artifacts.native_exe {
        // 读取真实ILC对象符号，不依赖代码文本或整个文件摘要；删除构建树前完成裁图断言。
        let (count, found) = object_evidence(&product);
        assert!(
            count > 0,
            "必须读取当次真实ILC对象，不能把缺对象当作裁剪成功"
        );
        assert_eq!(found, inspect);
        let moved = root.0.join("中文 空格搬迁");
        std::fs::rename(executable.parent().unwrap(), &moved).unwrap();
        std::fs::remove_dir_all(product).unwrap();
        Command::new(moved.join(executable.file_name().unwrap()))
            .current_dir(&moved)
            .output()
            .unwrap()
    } else {
        dotnet::run_dll_with(
            &artifacts.dll,
            &dotnet::RunOptions {
                cwd: Some(root.0.clone()),
                ..Default::default()
            },
        )
        .unwrap()
    };
    assert!(actual.status.success(), "{:?}", actual);
    assert_eq!(
        (actual.stdout, actual.stderr),
        (oracle.stdout, oracle.stderr)
    );
}

#[test]
fn static_transitive_dynamic_and_included_inspect_match_cpython() {
    differential(
        &[
            "tests/cases/native_inspect_startup/entry.py",
            "tests/cases/native_inspect_startup/helper.py",
        ],
        &[],
        false,
        false,
        true,
    );
    differential(
        &["tests/e2e/phase3/native_inspect_dynamic_startup.py"],
        &[],
        false,
        false,
        true,
    );
    differential(
        &["tests/native_startup/builtin_loader.py"],
        &[],
        true,
        false,
        true,
    );
    differential(
        &[
            "tests/cases/native_inspect_include/entry.py",
            "tests/cases/native_inspect_include/runtime_extra.py",
        ],
        &["runtime_extra"],
        false,
        false,
        true,
    );
}

#[test]
fn existing_native_inspect_five_groups_keep_their_actual_behavior() {
    for source in ["binding", "callables", "metadata", "values", "wrapped"] {
        differential(
            &[&format!(
                "tests/e2e/phase3/c2_inspect_signature_{source}.py"
            )],
            &[],
            false,
            false,
            true,
        );
    }
}

#[test]
fn legacy_host_apis_upgrade_base_startup_without_resetting_inspect() {
    let root = Root::new();
    let project = root.0.join("project");
    std::fs::create_dir(&project).unwrap();
    for file in ["Program.cs", "Startup.csproj"] {
        std::fs::copy(
            runtime::workspace_root()
                .join("tests/runtime_hosts/compiled_startup")
                .join(file),
            project.join(file),
        )
        .unwrap();
    }
    let runtime_dir = root.0.join("runtime");
    std::fs::create_dir(&runtime_dir).unwrap();
    runtime::copy_runtime_to(&runtime_dir).unwrap();
    let out = root.0.join("out");
    let build = Command::new(dotnet::dotnet_exe())
        .args(["build", "-c", "Release", "--artifacts-path"])
        .arg(root.0.join("artifacts"))
        .arg(project.join("Startup.csproj"))
        .arg(format!("-p:RuntimeDir={}", runtime_dir.display()))
        .arg("-o")
        .arg(&out)
        .output()
        .unwrap();
    assert!(
        build.status.success(),
        "{}",
        String::from_utf8_lossy(&build.stdout)
    );
    for mode in [
        "initialize",
        "register",
        "legacy-initialize",
        "legacy-register",
        "order-initialize",
        "order-register",
    ] {
        let result = Command::new(dotnet::dotnet_exe())
            .arg(out.join("Startup.dll"))
            .arg(mode)
            .output()
            .unwrap();
        assert!(result.status.success(), "{:?}", result);
        assert_eq!(result.stdout, b"startup-upgrade-ok\n");
        assert!(result.stderr.is_empty());
    }
}

#[test]
#[ignore = "显式NativeAOT裁图/删除输入和build/中文空格搬迁门"]
fn native_startup_aot_trims_only_unused_inspect() {
    if std::env::var("E2E_PYTHON_ONLY").as_deref() == Ok("1") {
        eprintln!(
            "Python用例自检跳过显式NativeAOT裁图与搬迁；移除E2E_PYTHON_ONLY后使用--ignored单独执行此门。"
        );
        return;
    }
    differential(&["tests/native_startup/plain.py"], &[], false, true, false);
    differential(
        &[
            "tests/cases/native_inspect_startup/entry.py",
            "tests/cases/native_inspect_startup/helper.py",
        ],
        &[],
        false,
        true,
        true,
    );
    differential(
        &[
            "tests/cases/native_inspect_include/entry.py",
            "tests/cases/native_inspect_include/runtime_extra.py",
        ],
        &["runtime_extra"],
        false,
        true,
        true,
    );
}
