//! 类命名空间可选绑定必须先查存在性，再在冷分支读取全局；条件方法按源码range登记。
use ryupy_driver::{Session, Stage};
mod support;
use support::dump;

#[test]
fn snapshots_class_if_source_order_and_lazy_lookup() {
    let path = std::env::temp_dir().join("ryupy_class_if_snapshot.py");
    std::fs::write(&path, "value=7\nclass C:\n    if value:\n        value=8\n        def method(self, x=value):\n            return x\n    else:\n        def method(self, x=value):\n            return x\n    after=value\n").unwrap();
    for (stage, name) in [
        (Stage::Hir, "class_if_hir"),
        (Stage::Mir, "class_if_mir"),
        (Stage::Cil, "class_if_cil"),
    ] {
        let mut session = Session::default();
        let output = dump(&mut session, &path, stage).expect("类体条件应保留源序与惰性回退");
        match stage {
            Stage::Hir => {
                assert_eq!(
                    output.matches("def method(self, x=...)").count(),
                    2,
                    "{name}"
                );
                assert!(
                    output.contains("after = class_lookup(value, global.value)"),
                    "{name}"
                );
                assert!(output.contains("if global.value:"));
            }
            Stage::Mir => {
                assert_eq!(output.matches("MakeFunction(method;").count(), 2, "{name}");
                let contains = output.find("RuntimeFn::ClassNamespaceContains").unwrap();
                let fallback = output[contains..].find("RuntimeFn::LoadName").unwrap();
                assert!(fallback > 0, "全局回退必须位于存在性检查后的分支");
                assert!(output[..contains].contains("const 8_i64"));
                assert!(output[contains..].contains("RuntimeFn::ClassNamespaceGet"));
            }
            Stage::Cil => {
                assert!(
                    output.contains("PyOps.ClassNamespaceContains(object, string)"),
                    "{name}"
                );
                assert!(output.contains("PyOps.ClassNamespaceGet(object, string)"));
                assert!(output.contains("PyErrors.LoadName(object, string)"));
                assert_eq!(
                    output
                        .matches(".method public static [System.Runtime]System.Object m0_method(")
                        .count(),
                    2
                );
            }
            _ => unreachable!(),
        }
    }
    std::fs::remove_file(path).unwrap();
}

#[test]
fn unsupported_builtin_fallback_is_only_rejected_when_executed() {
    // locals已有真实builtin值；本测试仍以未实现globals验证惰性fallback，不改断言。
    let root = ryupy_driver::runtime::workspace_root().join("target/test_class_if_fallback");
    std::fs::create_dir_all(&root).unwrap();
    let input = root.join("main.py");
    std::fs::write(&input, "flag=True\nclass Present:\n    if flag:\n        globals='bound'\n    value=globals\nprint(Present.value)\nflag=False\ntry:\n    class Absent:\n        if flag:\n            globals='bound'\n        value=globals\nexcept NotImplementedError:\n    print('明确拒绝未实现内建回退')\n").unwrap();
    let artifact = ryupy_driver::build(
        &mut Session::default(),
        &ryupy_driver::BuildOptions {
            input,
            out_dir: root.join("out"),
            aot: false,
            search_paths: vec![],
        },
    )
    .expect("不应提前求值或诊断类命名空间的未执行回退");
    let output = ryupy_driver::dotnet::run_dll(&artifact.dll).unwrap();
    assert!(
        output.status.success(),
        "{}",
        String::from_utf8_lossy(&output.stderr)
    );
    assert_eq!(
        String::from_utf8_lossy(&output.stdout).replace('\r', ""),
        "bound\n明确拒绝未实现内建回退\n"
    );
}
