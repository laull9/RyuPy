//! 类命名空间可选绑定必须先查存在性，再在冷分支读取全局；条件方法按源码range登记。
use ryupy_driver::{Session, Stage};
mod support;
use support::dump;

#[test]
fn snapshots_class_if_source_order_and_lazy_lookup() {
    let path = std::env::temp_dir().join("ryupy_class_if_snapshot.py");
    std::fs::write(&path, "value=7\nclass C:\n    if value:\n        value=8\n        def method(self, x=value):\n            return x\n    else:\n        def method(self, x=value):\n            return x\n    after=value\n").unwrap();
    for (stage, name) in [(Stage::Hir, "class_if_hir"), (Stage::Mir, "class_if_mir")] {
        let mut session = Session::default();
        let output = dump(&mut session, &path, stage).expect("类体条件应保留源序与惰性回退");
        insta::assert_snapshot!(name, output);
    }
    let output = dump(&mut Session::default(), &path, Stage::Cil).unwrap();
    assert_class_if_cil(&output);
    let corrupted = output.replacen(
        "PyOps.ClassNamespaceContains(object, string)",
        "PyOps.ClassNamespaceGet(object, string)",
        1,
    );
    assert_ne!(corrupted, output);
    assert!(std::panic::catch_unwind(|| assert_class_if_cil(&corrupted)).is_err());
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

// 判断后分别读取类命名空间/全局，两臂共享结果local后才写after并Finalize。
fn assert_class_if_cil(output: &str) {
    use support::cil_contract::Cil;
    let cil = Cil::parse(output);
    let methods = cil.methods_named("method");
    assert_eq!(methods.len(), 2);
    assert_ne!(methods[0].name, methods[1].name);
    for (index, core) in methods.iter().enumerate() {
        let thunk = cil.exact(&format!("__thunk_{index}"));
        assert_eq!(thunk.generated_call_count(core), 2);
        assert_eq!(core.opcode_count("Return"), 1);
    }
    let main = cil.exact("m0___module_main");
    for (name, count) in [
        ("PyOps.MakeClass", 1),
        ("PyOps.ClassNamespaceContains", 1),
        ("PyOps.ClassNamespaceGet", 2),
        ("PyErrors.LoadName", 3),
        ("PyPic.SetAttr", 4),
        ("PyOps.FinalizeOrdinaryClass", 1),
    ] {
        assert_eq!(main.runtime_count(name), count, "{name}");
    }
    let ins = &main.instructions;
    let check = ins.iter().position(|i| *i == "Call(static bool [RyuPy.Runtime]RyuPy.Runtime.PyOps.ClassNamespaceContains(object, string))").unwrap();
    let branch_target = |value: &str, prefix: &str| -> usize {
        value
            .strip_prefix(prefix)
            .unwrap()
            .strip_suffix(')')
            .unwrap()
            .parse()
            .unwrap()
    };
    assert_eq!(ins[check - 1], "LoadString(\"value\")");
    let yes = branch_target(ins[check + 3], "BranchTruthy(");
    let no = branch_target(ins[check + 4], "Branch(");
    assert!(check < yes && yes < no);
    assert_eq!(ins[yes + 1], "LoadString(\"value\")");
    assert_eq!(
        ins[yes + 2],
        "Call(static object [RyuPy.Runtime]RyuPy.Runtime.PyOps.ClassNamespaceGet(object, string))"
    );
    assert_eq!(
        ins[no],
        "LoadStaticField(RyuPy.Generated.Module.m0___mv3_value)"
    );
    assert_eq!(ins[no + 1], "LoadString(\"value\")");
    assert_eq!(
        ins[no + 2],
        "Call(static object [RyuPy.Runtime]RyuPy.Runtime.PyErrors.LoadName(object, string))"
    );
    assert_eq!(ins[yes + 5], ins[no + 5]); // 两臂写同一结果槽。
    let join = branch_target(ins[yes + 6], "Branch(");
    assert_eq!(join, branch_target(ins[no + 6], "Branch("));
    assert!(join > no);
    assert_eq!(ins[join + 2], "LoadString(\"after\")");
    assert_eq!(
        ins[join + 4],
        "Call(static void [RyuPy.Runtime]RyuPy.Runtime.PyPic.SetAttr(int, object, string, object))"
    );
    assert_eq!(
        ins[join + 6],
        "Call(static void [RyuPy.Runtime]RyuPy.Runtime.PyOps.FinalizeOrdinaryClass(object))"
    );
}
