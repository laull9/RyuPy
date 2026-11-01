//! M3.4 正常关闭阶段：重绑定stdout的缓冲必须刷新，刷新失败改为退出码120。
use ryupy_driver::{BuildOptions, Session};

#[test]
fn shutdown_flushes_redirected_file_and_reports_flush_failure() {
    let directory = std::env::temp_dir().join(format!("ryupy_shutdown_{}", std::process::id()));
    std::fs::create_dir_all(&directory).unwrap();
    let input = directory.join("shutdown.py");
    let output = directory.join("stdout.txt");
    std::fs::write(
        &input,
        r#"import sys
class BadFlush:
    def write(self, text):
        pass
    def flush(self):
        raise ValueError('shutdown failure')
class NoClose:
    def write(self, text):
        pass
    def flush(self):
        pass
    def close(self):
        raise RuntimeError('must not close redirected stream')
if sys.argv[1] == 'file':
    sys.stdout = open(sys.argv[2], 'w', encoding='utf-8')
    print('退出缓冲')
elif sys.argv[1] == 'both':
    sys.stdout = BadFlush()
    sys.stderr = open(sys.argv[2], 'w', encoding='utf-8')
    sys.stderr.write('错误流仍须落盘')
    sys.exit(7)
elif sys.argv[1] == 'none':
    sys.stdout = None
    sys.exit(7)
elif sys.argv[1] == 'no_close':
    sys.stdout = NoClose()
    sys.stderr = NoClose()
    sys.exit(7)
else:
    if sys.argv[1] == 'stderr':
        sys.stderr = BadFlush()
    else:
        sys.stdout = BadFlush()
    sys.exit(7)
"#,
    )
    .unwrap();
    let mut session = Session::default();
    let artifact = ryupy_driver::build(
        &mut session,
        &BuildOptions {
            input: input.clone(),
            out_dir: directory.join("output"),
            aot: false,
            search_paths: vec![],
        },
    )
    .unwrap();
    let run = |args: &[&str]| {
        std::process::Command::new(ryupy_driver::dotnet::dotnet_exe())
            .arg(&artifact.dll)
            .args(args)
            .output()
            .unwrap()
    };
    let normal = run(&["file", output.to_str().unwrap()]);
    assert_eq!(
        normal.status.code(),
        Some(0),
        "{}",
        String::from_utf8_lossy(&normal.stderr)
    );
    assert_eq!(std::fs::read_to_string(&output).unwrap(), "退出缓冲\n");
    for mode in ["failure", "stderr"] {
        let failed = run(&[mode]);
        assert_eq!(
            failed.status.code(),
            Some(120),
            "{}",
            String::from_utf8_lossy(&failed.stderr)
        );
        let stderr = String::from_utf8_lossy(&failed.stderr);
        assert!(stderr.contains("shutdown failure"), "{stderr}");
        assert!(!stderr.contains("Unhandled exception"), "{stderr}");
    }
    let error_file = directory.join("stderr.txt");
    let both = run(&["both", error_file.to_str().unwrap()]);
    assert_eq!(
        both.status.code(),
        Some(120),
        "{}",
        String::from_utf8_lossy(&both.stderr)
    );
    assert_eq!(
        std::fs::read_to_string(&error_file).unwrap(),
        "错误流仍须落盘",
        "stdout刷新失败不得阻断stderr刷新"
    );
    assert!(!String::from_utf8_lossy(&both.stderr).contains("Unhandled exception"));
    for mode in ["none", "no_close"] {
        let preserved = run(&[mode]);
        assert_eq!(
            preserved.status.code(),
            Some(7),
            "{mode}: {}",
            String::from_utf8_lossy(&preserved.stderr)
        );
        assert!(preserved.stderr.is_empty());
    }
    std::fs::remove_file(input).unwrap();
    std::fs::remove_file(output).unwrap();
    std::fs::remove_file(error_file).unwrap();
    // 产物留在独立目录供失败取证，不删除任何共享输出。
}
