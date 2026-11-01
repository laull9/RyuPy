//! 捕获调用异常后必须保留处理函数自身帧；正常返回才弹自身帧。
use ryupy_driver::Session;
#[test]
fn caught_call_does_not_remove_the_catching_function_frame() {
    let root = ryupy_driver::runtime::workspace_root();
    let path = std::env::temp_dir().join("ryupy_catch_current_frame.py");
    std::fs::write(&path, "def original():\n    raise ValueError('old')\ndef caught():\n    try:\n        original()\n    except ValueError:\n        pass\n    raise RuntimeError('new')\ncaught()\n").unwrap();
    let mut session = Session::default();
    let mir = ryupy_driver::dump(&mut session, &path, ryupy_driver::Stage::Mir).unwrap();
    insta::assert_snapshot!("catch_restores_current_frame_mir", mir);
    let artifact = ryupy_driver::build(
        &mut session,
        &ryupy_driver::BuildOptions {
            input: path.clone(),
            out_dir: root.join("target/test_catch_current_frame"),
            aot: false,
            search_paths: vec![],
        },
    )
    .unwrap();
    let result = ryupy_driver::dotnet::run_dll(&artifact.dll).unwrap();
    std::fs::remove_file(path).unwrap();
    let stderr = String::from_utf8_lossy(&result.stderr);
    assert_eq!(result.status.code(), Some(1), "{stderr}");
    assert!(
        stderr.contains("line 8, in caught"),
        "当前处理帧不能被catch恢复弹掉：{stderr}"
    );
    assert!(
        stderr.contains("line 9, in <module>"),
        "模块调用者仍须保留：{stderr}"
    );
    assert!(
        !stderr.contains("in original"),
        "已捕获旧异常的被调用帧不能残留：{stderr}"
    );
    assert!(stderr.contains("RuntimeError: new"), "{stderr}");
}
