//! SDK 子进程实际双流、关闭 stdin、失败诊断与旧文件保护测试；不启动真实 SDK。
use super::*;
use std::io::{Read, Write};
use std::time::{Duration, Instant};

struct Temp(PathBuf);
impl Temp {
    fn new() -> Self {
        Self(
            crate::aot::owned_root(&std::env::temp_dir(), "process-test")
                .unwrap()
                .0,
        )
    }
    fn path(&self) -> &Path {
        &self.0
    }
}
impl Drop for Temp {
    fn drop(&mut self) {
        let _ = std::fs::remove_dir_all(&self.0);
    }
}

// 仅子进程设置自己的环境，不改测试宿主的全局环境。
fn child(root: &Path, mode: &str) -> Command {
    let mut command = Command::new(std::env::current_exe().unwrap());
    let module = module_path!().split_once("::").unwrap().1;
    command.args([
        "--ignored",
        "--exact",
        &format!("{module}::sdk_child"),
        "--nocapture",
    ]);
    command.env("RYUPY_SDK_PROCESS_TEST_ROOT", root);
    command.env("RYUPY_SDK_PROCESS_TEST_MODE", mode);
    command
}

#[test]
#[ignore = "由独立子进程显式调用"]
fn sdk_child() {
    let Ok(root) = std::env::var("RYUPY_SDK_PROCESS_TEST_ROOT") else {
        return;
    };
    let mode = std::env::var("RYUPY_SDK_PROCESS_TEST_MODE").unwrap();
    assert_eq!(
        std::io::stdin().read(&mut [0_u8; 1]).unwrap(),
        0,
        "SDK stdin 必须为 EOF"
    );
    std::io::stdout().write_all(b"sdk-out-first\n").unwrap();
    std::io::stdout().flush().unwrap();
    std::io::stderr().write_all(b"sdk-err-first\n").unwrap();
    std::io::stderr().flush().unwrap();
    if mode == "wait" {
        let deadline = Instant::now() + Duration::from_secs(20);
        while !Path::new(&root).join("release").exists() {
            assert!(Instant::now() < deadline, "父进程未释放子进程");
            std::thread::sleep(Duration::from_millis(10));
        }
    }
    if mode == "fail" {
        std::process::exit(7);
    }
}

#[test]
fn both_logs_visible_before_child_exits() {
    let dir = Temp::new();
    let root = dir.path().to_owned();
    let worker_root = root.clone();
    let worker = std::thread::spawn(move || {
        run(
            &mut child(&worker_root, "wait"),
            &worker_root,
            &crate::stage_trace::StageTrace::new(false),
        )
    });
    let deadline = Instant::now() + Duration::from_secs(15);
    let visible = loop {
        let out = std::fs::read(root.join("publish.stdout.log")).unwrap_or_default();
        let err = std::fs::read(root.join("publish.stderr.log")).unwrap_or_default();
        if out.windows(13).any(|v| v == b"sdk-out-first")
            && err.windows(13).any(|v| v == b"sdk-err-first")
        {
            break true;
        }
        if worker.is_finished() || Instant::now() >= deadline {
            break false;
        }
        std::thread::sleep(Duration::from_millis(10));
    };
    let waiting = !worker.is_finished();
    // 失败断言前也释放子进程，避免测试卡住。
    std::fs::write(root.join("release"), b"").unwrap();
    let output = worker.join().unwrap().unwrap();
    assert!(visible && waiting);
    assert!(output.status.success());
    assert!(output.diagnostics().contains("sdk-out-first"));
    assert!(output.diagnostics().contains("sdk-err-first"));
}

#[test]
fn nonzero_keeps_status_and_both_streams() {
    let dir = Temp::new();
    let output = run(
        &mut child(dir.path(), "fail"),
        dir.path(),
        &crate::stage_trace::StageTrace::new(false),
    )
    .unwrap();
    assert_eq!(output.status.code(), Some(7));
    assert!(output.diagnostics().contains("sdk-out-first"));
    assert!(output.diagnostics().contains("sdk-err-first"));
    std::fs::remove_file(dir.path().join("publish.stdout.log")).unwrap();
    let diagnostics = output.diagnostics();
    assert!(diagnostics.contains("无法读取 SDK 日志"));
    assert!(diagnostics.contains("publish.stdout.log"));
    assert!(diagnostics.contains("sdk-err-first"));
    assert_eq!(output.status.code(), Some(7));
}

#[test]
fn spawn_failure_reports_owned_log_paths() {
    let dir = Temp::new();
    let error = run(
        &mut Command::new(dir.path().join("missing-sdk")),
        dir.path(),
        &crate::stage_trace::StageTrace::new(false),
    )
    .err()
    .unwrap();
    assert!(error.to_string().contains("publish.stdout.log"));
    assert!(error.to_string().contains("publish.stderr.log"));
    assert!(dir.path().join("publish.stdout.log").is_file());
    assert!(dir.path().join("publish.stderr.log").is_file());
}

#[test]
fn existing_files_are_never_overwritten() {
    for name in ["publish.stdout.log", "publish.stderr.log"] {
        let dir = Temp::new();
        std::fs::write(dir.path().join(name), b"previous").unwrap();
        assert!(
            run(
                &mut child(dir.path(), "fail"),
                dir.path(),
                &crate::stage_trace::StageTrace::new(false)
            )
            .is_err()
        );
        assert_eq!(std::fs::read(dir.path().join(name)).unwrap(), b"previous");
    }
}

#[cfg(unix)]
#[test]
fn existing_symlink_is_rejected_without_touching_target() {
    for name in ["publish.stdout.log", "publish.stderr.log"] {
        let dir = Temp::new();
        let target = dir.path().join("target");
        std::fs::write(&target, b"outside").unwrap();
        std::os::unix::fs::symlink(&target, dir.path().join(name)).unwrap();
        assert!(
            run(
                &mut child(dir.path(), "fail"),
                dir.path(),
                &crate::stage_trace::StageTrace::new(false)
            )
            .is_err()
        );
        assert_eq!(std::fs::read(target).unwrap(), b"outside");
    }
}

#[test]
fn stdin_is_closed_and_success_diagnostics_survive_missing_log() {
    let dir = Temp::new();
    let input = dir.path().join("stdin-sentinel");
    std::fs::write(&input, b"must-not-reach-sdk").unwrap();
    let mut command = child(dir.path(), "success");
    // 明确给命令一个非空 stdin；runner 必须覆盖它，不依赖测试宿主 stdin 状态。
    command.stdin(Stdio::from(std::fs::File::open(input).unwrap()));
    let output = run(
        &mut command,
        dir.path(),
        &crate::stage_trace::StageTrace::new(false),
    )
    .unwrap();
    assert!(output.status.success());
    std::fs::remove_file(dir.path().join("publish.stdout.log")).unwrap();
    let diagnostics = output.diagnostics();
    assert!(diagnostics.contains("无法读取 SDK 日志"));
    assert!(diagnostics.contains("publish.stdout.log"));
    assert!(diagnostics.contains("sdk-err-first"));
    assert!(output.status.success());
}
