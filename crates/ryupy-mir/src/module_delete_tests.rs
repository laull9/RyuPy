//! 删除的typed绑定位、动态哨兵与真实实例state的MIR结构回归。
use crate::*;

pub(super) fn compile(source: &str, instance_globals: bool) -> Program {
    let mut db = ryupy_base::SourceDb::default();
    let file = db.add("delete.py", source);
    let (parsed, diagnostics) = ryupy_syntax::parse(file, source);
    assert!(diagnostics.is_empty());
    let (hir, diagnostics) = ryupy_hir::lower_module(
        &parsed,
        &ryupy_hir::ModuleInfo {
            instance_globals,
            ..Default::default()
        },
        source,
    );
    assert!(diagnostics.is_empty(), "{diagnostics:?}");
    let (types, diagnostics) = ryupy_ty::check_module(&hir);
    assert!(diagnostics.is_empty());
    let (program, _, diagnostics) = build_program(&hir, &types);
    assert!(diagnostics.is_empty(), "{diagnostics:?}");
    assert!(
        validate::validate(&program).is_ok(),
        "{:?}",
        validate::validate(&program)
    );
    program
}

#[test]
fn typed_module_delete_keeps_field_and_checks_before_unbinding() {
    let program = compile("x = 1\ndel x\nx = 2\n", false);
    assert!(
        program
            .globals
            .iter()
            .any(|(_, g)| g.name.ends_with("_x") && g.ty == MirType::I64)
    );
    let dump = pretty::dump(&program);
    assert!(dump.contains("CheckModuleDelete"), "{dump}");
    assert!(!dump.contains("CheckModuleDeleteValue"));
    assert!(!dump.contains("LoadDeletedModuleName"));
    let main = &program.functions[program.entry];
    let writes = main
        .body
        .blocks
        .iter()
        .find_map(|(_, block)| {
            let i = block.stmts.iter().position(|s| {
                matches!(
                    s,
                    Statement::Eval(Rvalue::Call {
                        callee: Callee::Runtime(RuntimeFn::CheckModuleDelete),
                        ..
                    })
                )
            })?;
            Some(&block.stmts[i + 1])
        })
        .unwrap();
    assert!(matches!(
        writes,
        Statement::SetGlobal {
            value: Operand::Const(Constant::Bool(false)),
            ..
        }
    ));
}

#[test]
fn dynamic_delete_checks_value_without_builtin_lookup() {
    let program = compile("x = []\ndel x\n", false);
    let dump = pretty::dump(&program);
    assert!(dump.contains("CheckModuleDeleteValue"), "{dump}");
    assert!(dump.contains("MakeUnbound"), "{dump}");
    assert!(!dump.contains("LoadDeletedModuleName"));
}

#[test]
fn only_deleted_builtin_names_use_new_read_helper() {
    let program = compile("len = 1\ndel len\nprint(len([]))\n", false);
    assert!(pretty::dump(&program).contains("LoadDeletedModuleName"));
    let program = compile("x = 1\nprint(x)\n", false);
    assert!(!pretty::dump(&program).contains("ModuleDelete"));
    assert!(!pretty::dump(&program).contains("LoadDeletedModuleName"));
}

#[test]
fn instance_deletion_uses_explicit_owner_and_survives_pipeline() {
    let mut program = compile(
        "x = 1\ndef erase():\n    global x\n    del x\ndef g():\n    global x\n    yield 1\n    del x\n",
        true,
    );
    passes::run_pipeline(&mut program, &passes::default_pipeline()).unwrap();
    assert!(pretty::dump(&program).contains("InstanceDeleteName"));
    assert!(
        program
            .functions
            .iter()
            .all(|(_, f)| f.body.blocks.iter().all(|(_, b)| b
                .stmts
                .iter()
                .all(|s| !matches!(s, Statement::SetGlobal { .. }))))
    );
}
