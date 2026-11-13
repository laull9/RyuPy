//! 装饰器的层间结构合同：公开绑定、默认值时序与类体原始命名空间。
use ryupy_driver::{Session, Stage};
mod support;
use support::dump;

#[test]
fn snapshot_definition_decorators() {
    let scope = support::temp_scope::TempScope::new("m3_3");
    let path = scope.path("ryupy_test_m3_3_decorators.py");
    std::fs::write(&path, "def decorate(f):\n    return f\n@decorate\ndef f(x: int = 3) -> int:\n    return x\nclass C:\n    deco = decorate\n    @deco\n    def method(self):\n        return f(4)\nprint(C().method())\n").unwrap();
    for stage in [Stage::Hir, Stage::Mir] {
        let mut session = Session::default();
        let output = dump(&mut session, &path, stage).expect("装饰器应完成HIR/MIR降级");
        let required: &[&str] = match stage {
            Stage::Hir => &[
                "@decorate",
                "decorated def f",
                "@class_namespace.deco",
                "defaults of decorate",
            ],
            Stage::Mir => &[
                "FunctionDefaults",
                "MakeFunction(f;",
                "ClassNamespaceGet",
                "FinalizeOrdinaryClass",
                "CallValue",
            ],
            _ => unreachable!("测试只列出已约定阶段"),
        };
        for token in required {
            assert!(output.contains(token), "缺少结构 {token}：{output}");
        }
    }
    std::fs::remove_file(path).unwrap();
}
