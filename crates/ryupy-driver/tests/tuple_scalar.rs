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
    insta::assert_snapshot!("tuple_scalar_cil", cil);
    std::fs::remove_file(path).unwrap();
}
