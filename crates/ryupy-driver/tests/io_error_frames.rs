//! 真 JIT 缺数据负例：open 在进入 with 前失败，仍报告模块调用者与加载函数的原始行号。
use ryupy_driver::{BuildOptions, Session};

#[test]
fn io_missing_file_preserves_python_caller_frames() {
    let stamp = std::time::SystemTime::now()
        .duration_since(std::time::UNIX_EPOCH)
        .unwrap()
        .as_nanos();
    let root = std::env::temp_dir().join(format!("ryupy-io-frames-{}-{stamp}", std::process::id()));
    std::fs::create_dir(&root).unwrap();
    let entry = root.join("main.py");
    std::fs::write(
        &entry,
        "from english import load_text\n\n\n\n\n\n\n\n\n\nload_text()\n",
    )
    .unwrap();
    std::fs::write(root.join("english.py"), "import os.path\n\n\n\n\n\n\n\n\n\n\ndef load_text():\n    path = os.path.join(os.path.dirname(__file__), 'missing-data.txt')\n    with open(path, 'r', encoding='utf-8') as stream:\n        return stream.read()\n").unwrap();
    let artifacts = ryupy_driver::build(
        &mut Session::default(),
        &BuildOptions {
            input: entry,
            out_dir: root.join("out"),
            aot: false,
            search_paths: vec![],
        },
    )
    .unwrap();
    let result = ryupy_driver::dotnet::run_dll(&artifacts.dll).unwrap();
    let stderr = String::from_utf8_lossy(&result.stderr);
    assert_eq!(result.status.code(), Some(1), "{stderr}");
    let caller = stderr
        .find("line 11, in <module>")
        .expect("必须保留main调用帧");
    let callee = stderr
        .find("line 14, in load_text")
        .expect("必须保留open失败帧");
    assert!(caller < callee, "{stderr}");
    assert!(stderr.contains("FileNotFoundError:"), "{stderr}");
    std::fs::remove_dir_all(root).unwrap();
}
