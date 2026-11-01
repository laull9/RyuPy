//! 百分号数值转换替换异常时不能残留已吞回调的 traceback。
use ryupy_driver::{BuildOptions, Session};

#[test]
fn percent_integer_conversion_preserves_only_live_traceback_frames() {
    let root = ryupy_driver::runtime::workspace_root();
    let directory = std::env::temp_dir().join(format!("ryupy_percent_{}", std::process::id()));
    std::fs::create_dir_all(&directory).unwrap();
    let artifact = ryupy_driver::build(
        &mut Session::default(),
        &BuildOptions {
            input: root.join("tests/fixtures/percent_traceback.py"),
            out_dir: directory.join("output"),
            aot: false,
            search_paths: vec![],
        },
    )
    .unwrap();
    for mode in ["type", "value"] {
        let output = std::process::Command::new(ryupy_driver::dotnet::dotnet_exe())
            .arg(&artifact.dll)
            .arg(mode)
            .output()
            .unwrap();
        let stderr = String::from_utf8_lossy(&output.stderr);
        assert_eq!(output.status.code(), Some(1), "{stderr}");
        assert!(stderr.contains("in <module>"), "{stderr}");
        assert!(!stderr.contains("Unhandled exception"), "{stderr}");
        if mode == "type" {
            assert!(stderr.contains("TypeError: %d format:"), "{stderr}");
            assert!(!stderr.contains("in __int__"), "{stderr}");
            assert!(!stderr.contains("原始转换错误"), "{stderr}");
        } else {
            assert!(stderr.contains("ValueError: 原始数值错误"), "{stderr}");
            assert!(stderr.contains("in __int__"), "{stderr}");
        }
    }
    std::fs::remove_dir_all(directory).unwrap();
}
