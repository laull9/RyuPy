//! 捕获调用异常后必须保留处理函数自身帧；正常返回才弹自身帧。
use ryupy_driver::Session;

const SOURCE: &str = "def original():\n    raise ValueError('old')\ndef caught():\n    try:\n        original()\n    except ValueError:\n        pass\n    raise RuntimeError('new')\ncaught()\n";

// 纯Rust快照入口与真实运行回归分开；完整JIT断言仍由原测试独立执行。
#[test]
fn snapshot_catch_restores_current_frame() {
    let directory =
        std::env::temp_dir().join(format!("ryupy_catch_snapshot_{}", std::process::id()));
    std::fs::create_dir_all(&directory).unwrap();
    let path = directory.join("ryupy_catch_current_frame.py");
    std::fs::write(&path, SOURCE).unwrap();
    let mut session = Session::default();
    let mir = ryupy_driver::dump(&mut session, &path, ryupy_driver::Stage::Mir).unwrap();
    let caught = mir
        .split("fn caught(")
        .nth(1)
        .unwrap()
        .split("fn main(")
        .next()
        .unwrap();
    assert!(caught.contains("catch ValueError handler"));
    assert!(caught.contains("catch io.UnsupportedOperation handler"));
    // catch入口保留当前帧，仅回退到base+1；正常退出才移除自身帧。
    let restore = caught.find("Add(_1, const 1_i64)").unwrap();
    let pop = caught.find("RuntimeFn::TracePopTo(_5)").unwrap();
    let enter = caught.find("RuntimeFn::HandlerEnter(_4)").unwrap();
    assert!(restore < pop && pop < enter);
    assert!(
        caught.find("RuntimeFn::HandlerLeave").unwrap()
            < caught.find("MakeException(const \"RuntimeError\"").unwrap()
    );
    assert!(caught.contains("RuntimeFn::TracePopTo(_1)"));
    std::fs::remove_file(path).unwrap();
    std::fs::remove_dir(directory).unwrap();
}

#[test]
fn caught_call_does_not_remove_the_catching_function_frame() {
    let root = ryupy_driver::runtime::workspace_root();
    let path = std::env::temp_dir().join("ryupy_catch_current_frame.py");
    std::fs::write(&path, SOURCE).unwrap();
    let mut session = Session::default();
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
