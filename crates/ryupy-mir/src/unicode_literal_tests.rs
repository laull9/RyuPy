//! 精确Unicode常量及文本比较的HIR→MIR门；真实注解协议和exact事实两条路径都不可发CLR引用比较。
use crate::*;
fn lower(source: &str) -> (ryupy_hir::Module, ryupy_ty::ModuleTypes) {
    let mut db = ryupy_base::SourceDb::default();
    let file = db.add("unicode.py", source);
    let (parsed, diagnostics) = ryupy_syntax::parse(file, source);
    assert!(diagnostics.is_empty());
    let (hir, diagnostics) = ryupy_hir::lower_module(&parsed, &Default::default(), source);
    assert!(diagnostics.is_empty(), "{diagnostics:?}");
    let (types, diagnostics) = ryupy_ty::check_module(&hir);
    assert!(diagnostics.is_empty(), "{diagnostics:?}");
    (hir, types)
}
fn assert_route(
    hir: &ryupy_hir::Module,
    types: &ryupy_ty::ModuleTypes,
    name: &str,
    runtime: RuntimeFn,
) {
    let (program, _, diagnostics) = build_program(hir, types);
    assert!(diagnostics.is_empty(), "{diagnostics:?}");
    validate::validate(&program).unwrap();
    let (_, function) = program
        .functions
        .iter()
        .find(|(_, function)| function.name == name)
        .unwrap();
    assert!(function.body.blocks.iter().flat_map(|(_,block)|&block.stmts).any(|statement|matches!(statement,
        Statement::Assign{value:Rvalue::Call{callee:Callee::Runtime(actual),..},..} if *actual==runtime)));
    assert!(
        !function
            .body
            .blocks
            .iter()
            .flat_map(|(_, block)| &block.stmts)
            .any(|statement| matches!(
                statement,
                Statement::Assign {
                    value: Rvalue::Binary {
                        op: BinOp::Eq | BinOp::Lt,
                        ..
                    },
                    ..
                }
            ))
    );
}
#[test]
fn annotated_text_comparisons_keep_dynamic_protocol_route() {
    let (hir, types) = lower(
        "def equal(a: str, b: str) -> bool:\n    return a == b\nprint(equal('\\ud83d\\ude00', '😀'))\n",
    );
    assert_route(&hir, &types, "equal", RuntimeFn::DynRichCompareValue);
}
#[test]
fn proven_exact_text_comparisons_use_python_value_semantics() {
    let (hir, mut types) =
        lower("def exact() -> bool:\n    return '\\ud83d\\ude00' < '😀'\nprint(exact())\n");
    let (id, function) = hir
        .functions
        .iter()
        .find(|(_, function)| function.name.as_str() == "exact")
        .unwrap();
    // 当前ty可保守选Dynamic；此门显式给出该纯literal体的合法exact事实，保护MIR静态消费者。
    types.modes.insert(id, ryupy_ty::FuncMode::Static);
    types.sigs[id].ret = ryupy_ty::Ty::Bool;
    let mut facts = ryupy_ty::TypeckResults::default();
    for (expr, value) in function.body.exprs.iter() {
        facts.exprs.insert(
            expr,
            match value.kind {
                ryupy_hir::ExprKind::Literal(_) => ryupy_ty::Ty::Str,
                _ => ryupy_ty::Ty::Bool,
            },
        );
    }
    types.bodies.insert(id, facts);
    assert_route(&hir, &types, "exact", RuntimeFn::DynCompare);
}
