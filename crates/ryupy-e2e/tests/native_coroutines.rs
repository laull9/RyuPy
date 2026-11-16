//! C3 显式候选的真实产品差分；同次固定 CPython，所有产物在仓库外并随测试清理。
use ryupy_driver::{BuildOptions, Session, dotnet, runtime::workspace_root};
use std::path::PathBuf;
use std::process::Command;
use std::sync::atomic::{AtomicU64, Ordering};

struct OutputRoot(PathBuf);
impl OutputRoot {
    fn create() -> Self {
        static SEQUENCE: AtomicU64 = AtomicU64::new(0);
        let root = std::env::temp_dir().join(format!(
            "ryupy-c3-{}-{}",
            std::process::id(),
            SEQUENCE.fetch_add(1, Ordering::Relaxed)
        ));
        std::fs::create_dir(&root).expect("创建仓库外 C3 产物目录");
        assert!(!root.starts_with(workspace_root()), "C3 产物必须放在仓库外");
        Self(root)
    }
}
impl Drop for OutputRoot {
    fn drop(&mut self) {
        let _ = std::fs::remove_dir_all(&self.0);
    }
}

fn differential_mode(name: &str, aot: bool) {
    let output = OutputRoot::create();
    let python = std::env::var("PYTHON").expect("C3 差分必须显式指定 PYTHON=固定 CPython 3.12.13");
    let version = Command::new(&python)
        .args([
            "-B",
            "-c",
            "import sys; assert sys.version_info[:3] == (3, 12, 13), sys.version",
        ])
        .output()
        .unwrap();
    assert!(
        version.status.success(),
        "{}",
        String::from_utf8_lossy(&version.stderr)
    );
    let input = output.0.join("input");
    std::fs::create_dir(&input).unwrap();
    let fixture = workspace_root().join(name);
    let source = input.join(fixture.file_name().unwrap());
    std::fs::copy(fixture, &source).unwrap();
    let oracle = Command::new(python)
        .arg("-B")
        .arg(&source)
        .current_dir(&output.0)
        .output()
        .unwrap();
    assert!(
        oracle.status.success(),
        "{}",
        String::from_utf8_lossy(&oracle.stderr)
    );
    assert!(
        oracle.stderr.is_empty(),
        "协程用例必须完整关闭资源：{}",
        String::from_utf8_lossy(&oracle.stderr)
    );
    let mut session = Session::default();
    let artifacts = ryupy_driver::build(
        &mut session,
        &BuildOptions {
            input: source,
            out_dir: output.0.join("product"),
            aot,
            search_paths: vec![],
        },
    )
    .unwrap_or_else(|error| panic!("{error:#}\n{}", session.render_diagnostics(false)));
    assert!(
        session.cpython_provider.is_none(),
        "C3 产品禁止 CPython provider"
    );
    std::fs::remove_dir_all(input).unwrap();
    let actual = if let Some(executable) = artifacts.native_exe {
        let product = executable.parent().unwrap();
        let moved = output.0.join("中文 搬迁");
        std::fs::rename(product, &moved).unwrap();
        // 完整删除构建目录，再从新位置执行真实原生产物。
        std::fs::remove_dir_all(output.0.join("product")).unwrap();
        Command::new(moved.join(executable.file_name().unwrap()))
            .current_dir(&moved)
            .output()
            .unwrap()
    } else {
        dotnet::run_dll_with(
            &artifacts.dll,
            &dotnet::RunOptions {
                cwd: Some(output.0.clone()),
                ..Default::default()
            },
        )
        .unwrap()
    };
    assert!(
        actual.status.success(),
        "{}",
        String::from_utf8_lossy(&actual.stderr)
    );
    assert_eq!(
        String::from_utf8_lossy(&actual.stdout),
        String::from_utf8_lossy(&oracle.stdout)
    );
    assert!(
        actual.stderr.is_empty(),
        "{}",
        String::from_utf8_lossy(&actual.stderr)
    );
    if !aot {
        assert!(!artifacts.dll.with_extension("py").exists());
    }
}

#[test]
fn coroutine_lifecycle_matches_cpython() {
    differential("lifecycle.py");
}
#[test]
fn coroutine_await_protocol_matches_cpython() {
    differential("protocol.py");
}
#[test]
fn coroutine_cleanup_and_closure_match_cpython() {
    differential("cleanup.py");
}

fn differential(name: &str) {
    differential_mode(&format!("tests/coroutines/{name}"), false);
}

#[test]
#[ignore = "显式 NativeAOT 搬迁门，需要本机 AOT 工具链"]
fn coroutine_aot_relocation_matches_cpython() {
    for source in [
        "cleanup.py",
        "call_preparation.py",
        "async_with_cleanup.py",
        "async_with_traceback_protocol.py",
        "async_for_control.py",
        "async_for_exceptions.py",
        "async_for_lifecycle.py",
    ] {
        differential_mode(&format!("tests/coroutines/{source}"), true);
    }
}

#[test]
fn coroutine_changes_preserve_existing_generator_protocols() {
    for name in [
        "m3_2_generator_basic.py",
        "m3_2_generator_send.py",
        "m3_2_generator_exceptions.py",
        "m3_2_generator_cleanup.py",
        "m3_2_generator_return.py",
        "m3_2_yield_from.py",
    ] {
        differential_mode(&format!("tests/e2e/phase3/{name}"), false);
    }
}

#[test]
fn coroutine_call_preparation_matches_cpython() {
    differential("call_preparation.py");
}

#[test]
fn coroutine_await_kwargs_is_explicitly_rejected() {
    let output = OutputRoot::create();
    let source = workspace_root().join("tests/coroutines/reject_await_kwargs.py");
    let python = std::env::var("PYTHON").expect("显式指定固定 CPython oracle");
    let oracle = Command::new(python)
        .arg("-B")
        .arg(&source)
        .output()
        .unwrap();
    assert!(
        oracle.status.success(),
        "{}",
        String::from_utf8_lossy(&oracle.stderr)
    );
    assert!(oracle.stderr.is_empty());
    let mut session = Session::default();
    let result = ryupy_driver::build(
        &mut session,
        &BuildOptions {
            input: source,
            out_dir: output.0.join("product"),
            aot: false,
            search_paths: vec![],
        },
    );
    assert!(result.is_err(), "含 await 的 **kwargs 调用必须在发射前拒绝");
    assert_eq!(
        session
            .diagnostics
            .iter()
            .filter(|diagnostic| diagnostic.code == Some("E0100")
                && diagnostic.message == "调用双星号实参暂未支持跨暂停的映射快照")
            .count(),
        2
    );
}

#[test]
fn async_with_lifecycle_matches_cpython() {
    differential("async_with_lifecycle.py");
}
#[test]
fn async_with_exceptions_match_cpython() {
    differential("async_with_exceptions.py");
}
#[test]
fn async_with_traceback_matches_cpython() {
    differential("async_with_traceback.py");
}

#[test]
fn async_with_traceback_protocol_matches_cpython() {
    differential("async_with_traceback_protocol.py");
}
#[test]
fn async_with_protocol_matches_cpython() {
    differential("async_with_protocol.py");
}

#[test]
fn async_with_cleanup_matches_cpython() {
    differential("async_with_cleanup.py");
}

#[test]
fn async_for_protocol_matches_cpython() {
    differential("async_for_protocol.py");
}
#[test]
fn async_for_control_matches_cpython() {
    differential("async_for_control.py");
}
#[test]
fn async_for_exceptions_match_cpython() {
    differential("async_for_exceptions.py");
}
#[test]
fn async_for_lifecycle_matches_cpython() {
    differential("async_for_lifecycle.py");
}
#[test]
fn async_for_global_shadow_matches_cpython() {
    differential("async_for_global_shadow.py");
}
#[test]
fn async_for_shared_awaitable_matches_cpython() {
    differential("async_for_shared_awaitable.py");
}

// 这些输入在CPython成功，但跨越本切明确边界；保留源码行为和准确拒绝，不能混入成功计数。
fn async_for_boundary_source(name: &str) -> PathBuf {
    let source = workspace_root().join(format!("tests/e2e/phase3/{name}"));
    let python = std::env::var("PYTHON").expect("边界门也必须显式指定固定CPython");
    let oracle = Command::new(python).args(["-B", "-c",
        "import runpy,sys; assert sys.version_info[:3] == (3,12,13); runpy.run_path(sys.argv[1], run_name='__main__')"])
        .arg(&source).output().unwrap();
    assert!(
        oracle.status.success(),
        "{}",
        String::from_utf8_lossy(&oracle.stderr)
    );
    assert!(oracle.stderr.is_empty());
    source
}

#[test]
fn async_for_keeps_async_generator_and_getattribute_support_boundaries() {
    for (name, expected) in [
        (
            "c3_async_for_async_generator_boundary_pending.py",
            "异步生成器尚未接入原生协程候选",
        ),
        (
            "c3_async_for_getattribute_boundary_pending.py",
            "暂不支持自定义 __getattribute__",
        ),
    ] {
        let source = async_for_boundary_source(name);
        let output = OutputRoot::create();
        let mut session = Session::default();
        let result = ryupy_driver::build(
            &mut session,
            &BuildOptions {
                input: source,
                out_dir: output.0.join("product"),
                aot: false,
                search_paths: vec![],
            },
        );
        assert!(result.is_err());
        assert!(
            session
                .diagnostics
                .iter()
                .any(|e| e.code == Some("E0100") && e.message == expected),
            "{}",
            session.render_diagnostics(false)
        );
        if name.contains("async_generator") {
            assert!(
                session
                    .diagnostics
                    .iter()
                    .filter(|e| e.message == expected)
                    .all(|e| e.deferred_failure
                        == Some(ryupy_base::diagnostic::DeferredFailure::SupportBoundary))
            );
        }
    }
}

#[test]
fn async_for_dynamic_exception_base_alias_keeps_explicit_runtime_boundary() {
    let source = async_for_boundary_source("c3_async_for_exception_alias_boundary_pending.py");
    let output = OutputRoot::create();
    let mut session = Session::default();
    let artifact = ryupy_driver::build(
        &mut session,
        &BuildOptions {
            input: source,
            out_dir: output.0.join("product"),
            aot: false,
            search_paths: vec![],
        },
    )
    .unwrap_or_else(|e| panic!("{e:#}\n{}", session.render_diagnostics(false)));
    assert!(session.cpython_provider.is_none());
    let actual = dotnet::run_dll_with(
        &artifact.dll,
        &dotnet::RunOptions {
            cwd: Some(output.0.clone()),
            ..Default::default()
        },
    )
    .unwrap();
    assert!(!actual.status.success());
    assert!(actual.stdout.is_empty());
    assert!(String::from_utf8_lossy(&actual.stderr).contains(
        "NotImplementedError: runtime bases currently require an ordinary user class; special classes and metaclass protocols are not supported"));
}
