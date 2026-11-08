//! 生成器异常降级的既有结构测试；从生产实现文件原样迁出。
use super::*;

#[test]
fn exception_retry_has_single_physical_region_and_external_returns() {
    let mut body = Body {
        source_handler_contracts: Vec::new(),
        locals: Arena::default(),
        blocks: Arena::default(),
        entry: block_id(0),
        regions: vec![],
    };
    let caught = local(&mut body.locals, MirType::Dynamic);
    let frame = local(&mut body.locals, MirType::Dynamic);
    let input = local(&mut body.locals, MirType::Dynamic);
    alloc(
        &mut body,
        vec![call(
            input,
            RuntimeFn::GeneratorInput,
            vec![Operand::Local(frame)],
        )],
        Terminator::Return(Some(Operand::Const(Constant::None))),
    );
    let handler = alloc(
        &mut body,
        vec![
            Statement::Assign {
                dest: caught,
                value: Rvalue::CatchException,
            },
            // 普通运行时错误也必须在Retry弹callee帧前记录traceback/当前异常。
            call(
                caught,
                RuntimeFn::RaisePrepare,
                vec![Operand::Local(caught)],
            ),
        ],
        Terminator::Return(Some(Operand::Local(caught))),
    );
    body.regions.push(Region {
        source_handler_chain: None,
        kind: RegionKind::Catch {
            type_names: vec!["ValueError".into()],
        },
        try_start: block_id(0),
        try_len: 1,
        handler,
        exc_local: Some(caught),
    });
    let plan = Plan::take(&mut body);
    // 模拟普通生成器降级追加的恢复入口，验证它被重排到第一个物理块。
    body.entry = alloc(&mut body, vec![], Terminator::Goto(block_id(0)));
    plan.finish(
        &mut body,
        frame,
        &[(caught, MirType::Dynamic), (input, MirType::Dynamic)],
        &Default::default(),
    );
    assert_eq!(body.entry, block_id(0));
    assert_eq!(body.regions.len(), 1);
    let region = &body.regions[0];
    assert_eq!(region.try_start, block_id(0));
    assert!(matches!(region.kind, RegionKind::CatchAll));
    assert!(number(region.handler) as usize >= region.try_len);
    for (_, block) in body.blocks.iter().take(region.try_len) {
        assert!(!matches!(block.terminator, Terminator::Return(_)));
    }
    let catches: Vec<_> = body
        .blocks
        .iter()
        .filter(|(_, block)| {
            block.stmts.iter().any(|stmt| {
                matches!(
                    stmt,
                    Statement::Assign {
                        value: Rvalue::CatchException,
                        ..
                    }
                )
            })
        })
        .map(|(id, _)| id)
        .collect();
    assert_eq!(catches, vec![region.handler]);
    // 原语句块先记录 active，再读取输入；注入异常不能落入无保护区的哨兵位置。
    assert!(matches!(
        body.blocks[block_id(1)].stmts[0],
        Statement::Assign {
            value: Rvalue::Use(Operand::Const(Constant::I64(0))),
            ..
        }
    ));
    assert!(matches!(
        body.blocks[block_id(1)].stmts[1],
        Statement::Assign {
            value: Rvalue::Call {
                callee: Callee::Runtime(RuntimeFn::GeneratorInput),
                ..
            },
            ..
        }
    ));
}

#[test]
fn statement_yield_discards_input_via_local_not_cil_stack() {
    let mut blocks = Arena::default();
    let entry = blocks.alloc(BasicBlock {
        stmts: vec![],
        terminator: Terminator::Unreachable,
    });
    let resume = blocks.alloc(BasicBlock {
        stmts: vec![],
        terminator: Terminator::Return(Some(Operand::Const(Constant::None))),
    });
    blocks[entry].terminator = Terminator::Suspend {
        value: Operand::Const(Constant::None),
        resume,
        input: None,
        delegated: false,
    };
    let mut functions = Arena::default();
    let id = functions.alloc(Function {
        module_state: None,
        instance_capabilities: Default::default(),
        module_state_source: None,
        name: "test".into(),
        codegen_name: "test".into(),
        source_file: "test.py".into(),
        definition: None,
        params: vec![],
        ret: MirType::Dynamic,
        body: Body {
            source_handler_contracts: Vec::new(),
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
    let mut program = Program {
        functions,
        classes: Arena::default(),
        globals: Arena::default(),
        entry: id,
        source_name: "test.py".into(),
    };
    crate::generators::lower(&mut program, &[id], &Default::default());
    let mut input_calls = 0;
    for (_, function) in program.functions.iter() {
        for (_, block) in function.body.blocks.iter() {
            for statement in &block.stmts {
                assert!(!matches!(
                    statement,
                    Statement::Eval(Rvalue::Call {
                        callee: Callee::Runtime(RuntimeFn::GeneratorInput),
                        ..
                    })
                ));
                if matches!(
                    statement,
                    Statement::Assign {
                        value: Rvalue::Call {
                            callee: Callee::Runtime(RuntimeFn::GeneratorInput),
                            ..
                        },
                        ..
                    }
                ) {
                    input_calls += 1;
                }
            }
        }
    }
    assert_eq!(input_calls, 1);
    assert!(crate::validate::validate(&program).is_ok());
}
#[test]
fn delegated_resume_preserves_input_for_delegate_step() {
    let mut locals = Arena::default();
    let condition = local(&mut locals, MirType::Bool);
    let mut blocks = Arena::default();
    let entry = blocks.alloc(BasicBlock {
        stmts: vec![],
        terminator: Terminator::Unreachable,
    });
    let resume = blocks.alloc(BasicBlock {
        stmts: vec![],
        terminator: Terminator::Return(Some(Operand::Const(Constant::None))),
    });
    blocks[entry].stmts.push(Statement::Assign {
        dest: condition,
        value: Rvalue::GeneratorDelegateStep {
            delegate: Operand::Const(Constant::None),
        },
    });
    blocks[entry].terminator = Terminator::Suspend {
        value: Operand::Const(Constant::None),
        resume,
        input: None,
        delegated: true,
    };
    let mut functions = Arena::default();
    let id = functions.alloc(Function {
        module_state: None,
        instance_capabilities: Default::default(),
        module_state_source: None,
        name: "test".into(),
        codegen_name: "test".into(),
        source_file: "test.py".into(),
        definition: None,
        params: vec![],
        ret: MirType::Dynamic,
        body: Body {
            source_handler_contracts: Vec::new(),
            locals,
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
    let mut program = Program {
        functions,
        classes: Arena::default(),
        globals: Arena::default(),
        entry: id,
        source_name: "test.py".into(),
    };
    crate::generators::lower(&mut program, &[id], &Default::default());
    let mut steps = 0;
    for (_, function) in program.functions.iter() {
        for (_, block) in function.body.blocks.iter() {
            for statement in &block.stmts {
                if let Statement::Assign {
                    value: Rvalue::Call { callee, args },
                    ..
                } = statement
                {
                    assert!(!matches!(
                        callee,
                        Callee::Runtime(RuntimeFn::GeneratorInput)
                    ));
                    if matches!(callee, Callee::Runtime(RuntimeFn::GeneratorDelegateStep)) {
                        assert_eq!(args.len(), 2);
                        assert!(matches!(args[0], Operand::Local(_)));
                        steps += 1;
                    }
                }
            }
        }
    }
    assert_eq!(steps, 1);
    assert!(crate::validate::validate(&program).is_ok());
}
