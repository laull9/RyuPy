//! memoryview数值转换替换异常时不能残留已吞回调的 traceback。
#[path = "../src/test_temp.rs"]
mod layout_temp;
use ryupy_driver::{BuildOptions, Session};

#[test]
fn memoryview_conversion_preserves_only_live_traceback_frames() {
    let root = ryupy_driver::runtime::workspace_root();
    let directory = layout_temp::Root::new("memoryview-traceback");
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
        let output =
            std::process::Command::new(artifact.native_exe.as_ref().expect("应用必须有公开入口"))
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
