//! 异常Retry必须先保存原调用帧，暂停后bare raise仍保留最初抛错位置。
use ryupy_driver::Session;

#[test]
fn generator_retry_preserves_original_traceback() {
    let root = ryupy_driver::runtime::workspace_root();
    let input = std::env::temp_dir().join("ryupy_generator_traceback.py");
    std::fs::write(&input, "def original_helper():\n    return [][0]\ndef values():\n    try:\n        original_helper()\n    except IndexError:\n        yield 1\n        raise\nit = values()\nprint(next(it))\nnext(it)\n").unwrap();
    let mut session = Session::default();
    let artifact = ryupy_driver::build(
        &mut session,
        &ryupy_driver::BuildOptions {
            input: input.clone(),
            out_dir: root.join("target/test_generator_traceback"),
            aot: false,
            search_paths: vec![],
        },
    )
    .unwrap();
    let result = ryupy_driver::dotnet::run_dll(&artifact.dll).unwrap();
    std::fs::remove_file(input).unwrap();
    assert_eq!(result.status.code(), Some(1));
    let stderr = String::from_utf8_lossy(&result.stderr);
    assert!(
        stderr.contains("line 2, in original_helper"),
        "原抛错帧丢失：{stderr}"
    );
    assert!(stderr.contains("IndexError"), "异常类型丢失：{stderr}");
    assert!(!stderr.contains("System."), "泄漏CLR错误：{stderr}");
}
