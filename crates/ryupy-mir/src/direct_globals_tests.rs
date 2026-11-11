//! 新冷路必须消费能力与真实context；旧热call保持不变。
use crate::*;
pub(super) fn compile() -> Program {
    compile_changed(false)
}
fn compile_changed(copy_call: bool) -> Program {
    let source = "def f(globals):\n    return globals()\ndef g():\n    return globals()\ndef h(callback):\n    return callback()\n";
    let mut db = ryupy_base::SourceDb::default();
    let file = db.add("globals.py", source);
    let (parsed, errors) = ryupy_syntax::parse(file, source);
    assert!(errors.is_empty());
    let mut info = ryupy_hir::ModuleInfo {
        instance_globals: true,
        ..Default::default()
    };
    info.instance_capabilities.direct_globals = true;
    let (mut hir, errors) = ryupy_hir::lower_module(&parsed, &info, source);
    assert!(errors.is_empty(), "{errors:?}");
    if copy_call {
        for (_, function) in hir.functions.iter_mut() {
            let facts = function.body.direct_globals_calls.clone();
            for (call, _) in facts {
                // 保持相同range/同callee，复制出的synthetic调用也不能借原证书。
                let duplicate = function.body.exprs.alloc(function.body.exprs[call].clone());
                for (_, stmt) in function.body.stmts.iter_mut() {
                    if let ryupy_hir::StmtKind::Return(Some(value)) = &mut stmt.kind
                        && *value == call
                    {
                        *value = duplicate;
                    }
                }
            }
        }
    }
    let (types, errors) = ryupy_ty::check_module(&hir);
    assert!(errors.is_empty(), "{errors:?}");
    let (program, _, errors) = build_program(&hir, &types);
    assert!(errors.is_empty(), "{errors:?}");
    program
}
#[test]
fn direct_globals_requires_capability() {
    let mut program = compile();
    assert!(
        validate::validate(&program).is_ok(),
        "{:?}",
        validate::validate(&program)
    );
    assert_eq!(
        pretty::dump(&program)
            .matches("InstanceDirectGlobals")
            .count(),
        2
    );
    for (_, function) in program.functions.iter_mut() {
        function.instance_capabilities.direct_globals = false;
    }
    assert!(validate::validate(&program).is_err());
}
#[test]
fn direct_globals_rejects_forged_context() {
    let mut program = compile();
    let mut changed = 0;
    for (_, function) in program.functions.iter_mut() {
        for (_, block) in function.body.blocks.iter_mut() {
            for statement in &mut block.stmts {
                if let Statement::Assign { value, .. } | Statement::Eval(value) = statement
                    && let Rvalue::Call {
                        callee: Callee::Runtime(RuntimeFn::InstanceDirectGlobals),
                        args,
                    } = value
                {
                    args[0] = Operand::Const(Constant::None);
                    changed += 1;
                }
            }
        }
    }
    assert_eq!(changed, 2);
    assert!(validate::validate(&program).is_err());
}

#[test]
fn same_range_duplicate_does_not_borrow_direct_call_fact() {
    let program = compile_changed(true);
    assert!(!pretty::dump(&program).contains("InstanceDirectGlobals"));
}
