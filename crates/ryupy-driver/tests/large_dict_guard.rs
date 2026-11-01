//! 大字典非标量键是明确未支持边界：不得为了判断可哈希而运行用户协议。
use ryupy_driver::Session;
#[test]
fn unsupported_large_dictionary_key_does_not_call_user_hash_equal_or_repr() {
    let root = ryupy_driver::runtime::workspace_root();
    let input = root.join("tests/e2e/phase3/m3_7_large_dict_custom_pending.py");
    let mut session = Session::default();
    let artifact = ryupy_driver::build(
        &mut session,
        &ryupy_driver::BuildOptions {
            input,
            out_dir: root.join("target/test_large_dict_guard"),
            aot: false,
            search_paths: vec![],
        },
    )
    .unwrap();
    let result = ryupy_driver::dotnet::run_dll(&artifact.dll).unwrap();
    let stderr = String::from_utf8_lossy(&result.stderr);
    assert_eq!(result.status.code(), Some(0), "{stderr}");
    assert!(stderr.is_empty(), "不能泄漏CLR错误：{stderr}");
    assert_eq!(
        String::from_utf8_lossy(&result.stdout),
        "unsupported ['key', 'value']\n",
        "key/value各一次，拒绝前不得hash/eq/repr，拒绝后不得继续求值"
    );
}
