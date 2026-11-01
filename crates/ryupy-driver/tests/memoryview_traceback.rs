//! memoryview数值转换替换异常时不能残留已吞回调的 traceback。
use ryupy_driver::{BuildOptions, Session};

#[test]
fn memoryview_conversion_preserves_only_live_traceback_frames() {
    let root = ryupy_driver::runtime::workspace_root();
    let directory = std::env::temp_dir().join(format!("ryupy_memoryview_{}", std::process::id()));
    std::fs::create_dir_all(&directory).unwrap();
    let artifact = ryupy_driver::build(
        &mut Session::default(),
        &BuildOptions {
            input: root.join("tests/fixtures/memoryview_traceback.py"),
            out_dir: directory.join("output"),
            aot: false,
            search_paths: vec![],
        },
    )
    .unwrap();
    for mode in ["type", "value", "overflow", "runtime"] {
        let output = std::process::Command::new(ryupy_driver::dotnet::dotnet_exe())
            .arg(&artifact.dll)
            .arg(mode)
            .output()
            .unwrap();
        let stderr = String::from_utf8_lossy(&output.stderr);
        assert_eq!(output.status.code(), Some(1), "{stderr}");
        assert!(stderr.contains("in <module>"), "{stderr}");
        assert!(!stderr.contains("Unhandled exception"), "{stderr}");
        if mode != "runtime" {
            assert!(stderr.contains("memoryview: invalid"), "{stderr}");
            assert!(!stderr.contains("in __index__"), "{stderr}");
            assert!(!stderr.contains("原始"), "{stderr}");
        } else {
            assert!(stderr.contains("RuntimeError: 原样运行错误"), "{stderr}");
            assert!(stderr.contains("in __index__"), "{stderr}");
        }
    }
    std::fs::remove_dir_all(directory).unwrap();
}
