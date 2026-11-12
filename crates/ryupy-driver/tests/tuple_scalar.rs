//! 真实源码必须命中 tuple 标量替换，而非只在手工 MIR 上通过；逃逸函数保留构造。
use ryupy_driver::{Session, Stage};
mod support;

const SOURCE: &str = "def swap(a, b):\n    a, b = b, a\n    return a + b\n\ndef first(a, b):\n    return (a, b)[0]\n\ndef escape(a, b):\n    pair = (a, b)\n    print(pair)\n    return pair[0]\n\nprint(swap(1, 2), first(3, 4))\n";

#[test]
fn source_temporary_tuple_is_removed_but_escaping_tuple_survives() {
    let path = std::env::temp_dir().join("ryupy_tuple_scalar.py");
    std::fs::write(&path, SOURCE).unwrap();
    let mut session = Session::default();
    let mir = support::dump(&mut session, &path, Stage::Mir).unwrap();
    assert!(session.diagnostics.is_empty());
    let swap = mir
        .split("fn swap(")
        .nth(1)
        .unwrap()
        .split("fn first(")
        .next()
        .unwrap();
    assert!(
        !swap.contains("MakeTuple") && !swap.contains("GetItem") && !swap.contains("UnpackCheck"),
        "未命中交换解包：{swap}"
    );
    let first = mir
        .split("fn first(")
        .nth(1)
        .unwrap()
        .split("fn escape(")
        .next()
        .unwrap();
    assert!(
        !first.contains("MakeTuple") && !first.contains("GetItem"),
        "未命中固定下标：{first}"
    );
    let escape = mir
        .split("fn escape(")
        .nth(1)
        .unwrap()
        .split("fn main(")
        .next()
        .unwrap();
    assert!(
        escape.contains("MakeTuple") && escape.contains("GetItem"),
        "不能删除逃逸对象：{escape}"
    );
    insta::assert_snapshot!("tuple_scalar_mir", mir);
    let cil = support::dump(&mut session, &path, Stage::Cil).unwrap();
    let code = support::cil_contract::Cil::parse(&cil);
    for name in ["swap", "first"] {
        let method = code.method(name);
        for member in ["PyOps.MakeTuple", "PyOps.GetItem", "PyOps.UnpackCheck"] {
            assert_eq!(method.runtime_count(member), 0, "{name}: {member}");
        }
        for opcode in ["NewArray", "NewObject", "Box", "Unbox"] {
            assert_eq!(method.opcode_count(opcode), 0, "{name}: {opcode}");
        }
        assert_eq!(method.opcode_count("Return"), 1);
    }
    assert_eq!(code.method("swap").opcode_count("StoreArgument"), 2);
    assert_eq!(code.method("swap").runtime_count("PyPic.Binary"), 1);
    let escaped = code.method("escape");
    assert_eq!(escaped.runtime_count("PyOps.MakeTuple"), 1);
    assert_eq!(escaped.runtime_count("PyOps.GetItem"), 1);
    assert_eq!(escaped.opcode_count("NewArray"), 1);
    assert_eq!(escaped.opcode_count("StoreElement"), 2);
    escaped.ordered_calls(&["PyOps.MakeTuple", "Builtins.WriteObject", "PyOps.GetItem"]);
    // 保元组身份逃逸后的打印与索引，而不是仅证明未崩溃。
    assert_eq!(escaped.runtime_count("Builtins.WriteNewline"), 1);
    std::fs::remove_file(path).unwrap();
}
