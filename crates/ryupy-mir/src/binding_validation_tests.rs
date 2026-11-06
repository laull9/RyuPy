//! 模块绑定位 ABI 与导出元数据负例，不允许无效 IR 漏到 CIL 后端。
use crate::*;
use la_arena::Arena;

fn program(statement: Statement) -> Program {
    let mut blocks = Arena::default();
    let entry = blocks.alloc(BasicBlock {
        stmts: vec![statement],
        terminator: Terminator::Return(None),
    });
    let mut functions = Arena::default();
    let entry = functions.alloc(Function {
        module_state: None,
        module_state_source: None,
        name: "main".into(),
        codegen_name: "main".into(),
        source_file: "print.py".into(),
        definition: None,
        params: vec![],
        ret: MirType::Unit,
        body: Body {
            locals: Arena::default(),
            blocks,
            entry,
            regions: vec![],
        },
        param_meta: vec![],
        function_global: None,
        specialization_identity_global: None,
        defaults_global: None,
        signature_global: None,
        env: None,
        dynamic_origin: None,
    });
    Program {
        functions,
        classes: Arena::default(),
        globals: Arena::default(),
        entry,
        source_name: "print.py".into(),
    }
}

#[test]
fn bound_check_rejects_wrong_arity_types_and_assigned_void() {
    for args in [
        vec![],
        vec![Operand::Const(Constant::Bool(true))],
        vec![
            Operand::Const(Constant::I64(1)),
            Operand::Const(Constant::Str("x".into())),
        ],
        vec![
            Operand::Const(Constant::Bool(true)),
            Operand::Const(Constant::Null),
        ],
    ] {
        let input = program(Statement::Eval(Rvalue::Call {
            callee: Callee::Runtime(RuntimeFn::CheckGlobalBound),
            args,
        }));
        assert!(
            crate::validate::validate(&input)
                .unwrap_err()
                .iter()
                .any(|error| error.contains("模块绑定位ABI"))
        );
    }
    let call = || Rvalue::Call {
        callee: Callee::Runtime(RuntimeFn::CheckGlobalBound),
        args: vec![
            Operand::Const(Constant::Bool(true)),
            Operand::Const(Constant::Str("x".into())),
        ],
    };
    let mut input = program(Statement::Eval(call()));
    assert!(crate::validate::validate(&input).is_ok());
    let local = input.functions[input.entry].body.locals.alloc(LocalDecl {
        ty: MirType::Dynamic,
        name: None,
        is_cell: false,
    });
    let body = &mut input.functions[input.entry].body;
    body.blocks[body.entry].stmts = vec![Statement::Assign {
        dest: local,
        value: call(),
    }];
    assert!(
        crate::validate::validate(&input)
            .unwrap_err()
            .iter()
            .any(|error| error.contains("无返回值"))
    );
}

#[test]
fn linked_export_guards_reject_invalid_slots_and_types() {
    let input = program(Statement::Eval(Rvalue::Use(Operand::Const(Constant::None))));
    let mut linked = crate::link(
        vec![(
            input,
            UnitMeta {
                execution: crate::ModuleExecutionKind::LegacySingleton,
                location: Default::default(),
                file_backed_by_global: false,
                is_package: false,
                name: "test".into(),
                package: "".into(),
                file: "test.py".into(),
                exports: vec![],
                protected_exports: vec![],
            },
        )],
        0,
    );
    let value = linked.globals.alloc(GlobalDecl {
        storage: crate::GlobalStorage::LegacyStatic,
        name: "value".into(),
        ty: MirType::I64,
    });
    let flag = linked.globals.alloc(GlobalDecl {
        storage: crate::GlobalStorage::LegacyStatic,
        name: "bound".into(),
        ty: MirType::Bool,
    });
    let invalid = GlobalId::from_raw(la_arena::RawIdx::from_u32(999));
    for (slot, guard) in [
        (invalid, ExportGuard::None),
        (value, ExportGuard::ValueSentinel),
        (value, ExportGuard::BoundFlag(invalid)),
        (flag, ExportGuard::BoundFlag(flag)),
        (flag, ExportGuard::BoundFlag(value)),
    ] {
        linked.modules[linked.entry].exports = vec![ModuleExport {
            name: "x".into(),
            value: slot,
            guard,
        }];
        assert!(crate::validate::validate_linked_exports(&linked).is_err());
    }
    linked.modules[linked.entry].exports = vec![ModuleExport {
        name: "x".into(),
        value,
        guard: ExportGuard::BoundFlag(flag),
    }];
    assert!(crate::validate::validate_linked_exports(&linked).is_ok());
}

#[test]
fn class_namespace_contains_checks_arguments_and_boolean_destination() {
    let call = |args| Rvalue::Call {
        callee: Callee::Runtime(RuntimeFn::ClassNamespaceContains),
        args,
    };
    let valid = || {
        vec![
            Operand::Const(Constant::Null),
            Operand::Const(Constant::Str("name".into())),
        ]
    };
    for arguments in [
        vec![],
        vec![
            Operand::Const(Constant::I64(1)),
            Operand::Const(Constant::Str("name".into())),
        ],
    ] {
        let input = program(Statement::Eval(call(arguments)));
        assert!(
            crate::validate::validate(&input)
                .unwrap_err()
                .iter()
                .any(|error| error.contains("类命名空间存在性ABI"))
        );
    }
    let mut input = program(Statement::Eval(call(valid())));
    assert!(crate::validate::validate(&input).is_err());
    let local = input.functions[input.entry].body.locals.alloc(LocalDecl {
        ty: MirType::Bool,
        name: None,
        is_cell: false,
    });
    let body = &mut input.functions[input.entry].body;
    body.blocks[body.entry].stmts = vec![Statement::Assign {
        dest: local,
        value: call(valid()),
    }];
    assert!(crate::validate::validate(&input).is_ok());
    input.functions[input.entry].body.locals[local].ty = MirType::Dynamic;
    assert!(
        crate::validate::validate(&input)
            .unwrap_err()
            .iter()
            .any(|error| error.contains("必须写入bool临时"))
    );
}
