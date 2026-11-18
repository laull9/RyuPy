//! 权威namespace生成器的实际暂停IR证明：context只从工厂帧恢复，改坏来源不能进入后端。
use crate::*;

fn compile() -> Program {
    let source =
        include_str!("../../../tests/cases/stock_module_namespaces/generators/stream_state.py");
    let mut db = ryupy_base::SourceDb::default();
    let file = db.add("stream_state.py", source);
    let (parsed, diagnostics) = ryupy_syntax::parse(file, source);
    assert!(diagnostics.is_empty(), "{diagnostics:?}");
    let info = ryupy_hir::ModuleInfo {
        name: "stream_state".into(),
        file: "stream_state.py".into(),
        instance_globals: true,
        instance_capabilities: ryupy_hir::provider_namespace_capabilities(),
        ..Default::default()
    };
    let (hir, diagnostics) = ryupy_hir::lower_module(&parsed, &info, source);
    assert!(diagnostics.is_empty(), "{diagnostics:?}");
    let (types, diagnostics) = ryupy_ty::check_module(&hir);
    assert!(diagnostics.is_empty(), "{diagnostics:?}");
    let (program, _, diagnostics) = build_program(&hir, &types);
    assert!(diagnostics.is_empty(), "{diagnostics:?}");
    validate::validate(&program).unwrap();
    program
}

#[test]
fn generated_resumptions_keep_their_authoritative_context_in_the_factory_frame() {
    let program = compile();
    let resumptions: Vec<_> = program
        .functions
        .iter()
        .filter_map(|(_, function)| {
            matches!(
                function.module_state_source,
                Some(InstanceStateSource::GeneratorFrame { .. })
            )
            .then_some(function)
        })
        .collect();
    // 普通yield、yield-from与两个惰性表达式各有真实恢复体，不把genexpr物化成list。
    assert!(resumptions.len() >= 4);
    for function in resumptions {
        assert!(function.instance_capabilities.provider_namespace);
        assert!(
            function
                .body
                .blocks
                .iter()
                .all(|(_, block)| { !matches!(block.terminator, Terminator::Suspend { .. }) })
        );
    }
}

#[test]
fn changing_the_saved_context_to_an_arbitrary_value_is_rejected() {
    let mut program = compile();
    let function = program
        .functions
        .iter_mut()
        .find_map(|(_, function)| {
            matches!(
                function.module_state_source,
                Some(InstanceStateSource::GeneratorFrame { .. })
            )
            .then_some(function)
        })
        .unwrap();
    let context = function.module_state.unwrap();
    let block = &mut function.body.blocks[function.body.entry];
    let restore = block
        .stmts
        .iter_mut()
        .find(|statement| matches!(statement, Statement::Assign { dest, .. } if *dest == context))
        .unwrap();
    *restore = Statement::Assign {
        dest: context,
        value: Rvalue::Use(Operand::Const(Constant::None)),
    };
    assert!(
        validate::validate(&program)
            .unwrap_err()
            .iter()
            .any(|error| { error.contains("实例state") || error.contains("context") })
    );
}

#[test]
fn a_snapshot_saved_to_a_different_frame_slot_is_rejected() {
    let mut program = compile();
    let function = program
        .functions
        .iter_mut()
        .find_map(|(_, function)| {
            (matches!(
                function.module_state_source,
                Some(InstanceStateSource::GeneratorFrame { .. })
            ) && !function.body.direct_globals_contracts.is_empty())
            .then_some(function)
        })
        .unwrap();
    let fact = function.body.direct_globals_contracts[0].clone();
    let mut changed = false;
    for (_, block) in function.body.blocks.iter_mut() {
        for statement in &mut block.stmts {
            if let Statement::Eval(Rvalue::Call {
                callee: Callee::Runtime(RuntimeFn::ArraySet),
                args,
            }) = statement
            {
                if args.get(2) == Some(&Operand::Local(fact.snapshot)) {
                    // 只有原private frame的精确槽位属于机械保存；邻槽也不能借用豁免。
                    args[1] = Operand::Const(Constant::I64(999_999));
                    changed = true;
                    break;
                }
            }
        }
        if changed {
            break;
        }
    }
    assert!(changed, "测试必须改到真实callee快照保存指令");
    assert!(
        validate::validate(&program)
            .unwrap_err()
            .iter()
            .any(|error| { error.contains("直接globals") })
    );
}

#[test]
fn a_result_restored_from_a_different_frame_slot_is_rejected() {
    let mut program = compile();
    let function = program
        .functions
        .iter_mut()
        .find_map(|(_, function)| {
            (matches!(
                function.module_state_source,
                Some(InstanceStateSource::GeneratorFrame { .. })
            ) && !function.body.direct_globals_contracts.is_empty())
            .then_some(function)
        })
        .unwrap();
    let result = function.body.direct_globals_contracts[0].result;
    let restore = function.body.blocks[function.body.entry]
        .stmts
        .iter_mut()
        .find_map(|statement| match statement {
            Statement::Assign {
                dest,
                value:
                    Rvalue::Call {
                        callee: Callee::Runtime(RuntimeFn::ArrayGet),
                        args,
                    },
            } if *dest == result => Some(args),
            _ => None,
        })
        .expect("真实恢复体必须包含result局部槽");
    restore[1] = Operand::Const(Constant::I64(999_999));
    assert!(
        validate::validate(&program)
            .unwrap_err()
            .iter()
            .any(|error| { error.contains("直接globals") })
    );
}
