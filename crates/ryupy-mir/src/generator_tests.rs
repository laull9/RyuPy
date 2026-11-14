//! 错误的暂停 IR 不能越过 MIR 验证边界进入后端。

use crate::mir::*;
use la_arena::Arena;

fn program() -> Program {
    let mut blocks = Arena::default();
    let entry = blocks.alloc(BasicBlock {
        stmts: vec![],
        terminator: Terminator::Return(Some(Operand::Const(Constant::None))),
    });
    let mut functions = Arena::default();
    let entry = functions.alloc(Function {
        module_state: None,
        instance_capabilities: Default::default(),
        module_state_source: None,
        name: "生成器测试".into(),
        codegen_name: "test".into(),
        source_file: "test.py".into(),
        definition: None,
        params: vec![],
        ret: MirType::Dynamic,
        body: Body {
            direct_globals_contracts: Vec::new(),
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
    Program {
        functions,
        classes: Arena::default(),
        globals: Arena::default(),
        entry,
        source_name: "test.py".into(),
    }
}

#[test]
fn rejects_suspend_before_codegen() {
    let mut program = program();
    let body = &mut program.functions[program.entry].body;
    body.blocks[body.entry].terminator = Terminator::Suspend {
        value: Operand::Const(Constant::None),
        input: None,
        delegated: false,
        resume: body.entry,
    };
    assert!(crate::validate::validate(&program).is_err());
}

#[test]
fn rejects_invalid_resume_and_frame() {
    let mut program = program();
    let entry = program.entry;
    let body = &mut program.functions[entry].body;
    body.blocks[body.entry]
        .stmts
        .push(Statement::Eval(Rvalue::MakeGenerator {
            resume: entry,
            frame: vec![Operand::Const(Constant::I64(0))],
            origin: None,
        }));
    assert!(crate::validate::validate(&program).is_err());
}

#[test]
fn liveness_handles_complex_rvalue_reads() {
    let mut body = Body {
        direct_globals_contracts: Vec::new(),
        source_handler_contracts: Vec::new(),
        locals: Arena::default(),
        blocks: Arena::default(),
        entry: BlockId::from_raw(la_arena::RawIdx::from_u32(0)),
        regions: Vec::new(),
    };

    let loc_named = body.locals.alloc(LocalDecl {
        ty: MirType::Dynamic,
        name: Some("user_var".into()),
        is_cell: false,
    });
    let loc_temp1 = body.locals.alloc(LocalDecl {
        ty: MirType::Dynamic,
        name: None,
        is_cell: false,
    });
    let loc_temp2 = body.locals.alloc(LocalDecl {
        ty: MirType::Dynamic,
        name: None,
        is_cell: false,
    });

    let b_resume = body.blocks.alloc(BasicBlock {
        stmts: vec![
            Statement::Assign {
                dest: loc_temp2,
                value: Rvalue::DynUnary {
                    op: DynOp::Not,
                    operand: Operand::Local(loc_temp1),
                },
            },
            Statement::Eval(Rvalue::CallWithArgs {
                callee: RuntimeFn::ArrayGet,
                prefix: vec![Operand::Local(loc_named)],
                args: vec![Operand::Local(loc_temp2)],
            }),
        ],
        terminator: Terminator::Return(None),
    });

    let b_entry = body.blocks.alloc(BasicBlock {
        stmts: vec![Statement::Assign {
            dest: loc_temp1,
            value: Rvalue::DynTruthy {
                value: Operand::Const(Constant::I64(1)),
            },
        }],
        terminator: Terminator::Suspend {
            value: Operand::Const(Constant::None),
            resume: b_resume,
            input: None,
            delegated: false,
        },
    });
    body.entry = b_entry;

    let res = crate::generators_liveness::analyze_suspend_liveness(&body, &[], false);
    assert!(res.is_some());
    let map = res.unwrap();
    let live = map.get(&b_entry).expect("entry must have liveness info");

    // loc_named 具有名字，mandatory_retain 必定包含；
    assert!(live.contains(&loc_named));
    // loc_temp1 在 resume 块通过 DynUnary 读取，必须判定为活跃；
    assert!(live.contains(&loc_temp1));
}

#[test]
fn liveness_multi_suspend_convergence() {
    let mut body = Body {
        direct_globals_contracts: Vec::new(),
        source_handler_contracts: Vec::new(),
        locals: Arena::default(),
        blocks: Arena::default(),
        entry: BlockId::from_raw(la_arena::RawIdx::from_u32(0)),
        regions: Vec::new(),
    };

    let shared_var = body.locals.alloc(LocalDecl {
        ty: MirType::Dynamic,
        name: None,
        is_cell: false,
    });

    let b_resume = body.blocks.alloc(BasicBlock {
        stmts: vec![Statement::Eval(Rvalue::Use(Operand::Local(shared_var)))],
        terminator: Terminator::Return(None),
    });

    let b_suspend2 = body.blocks.alloc(BasicBlock {
        stmts: vec![],
        terminator: Terminator::Suspend {
            value: Operand::Const(Constant::I64(2)),
            resume: b_resume,
            input: None,
            delegated: false,
        },
    });

    let b_suspend1 = body.blocks.alloc(BasicBlock {
        stmts: vec![],
        terminator: Terminator::Suspend {
            value: Operand::Const(Constant::I64(1)),
            resume: b_resume,
            input: None,
            delegated: false,
        },
    });

    let cond_loc = body.locals.alloc(LocalDecl {
        ty: MirType::Bool,
        name: None,
        is_cell: false,
    });
    let b_entry = body.blocks.alloc(BasicBlock {
        stmts: vec![],
        terminator: Terminator::Branch {
            cond: Operand::Local(cond_loc),
            then_: b_suspend1,
            else_: b_suspend2,
        },
    });
    body.entry = b_entry;

    let res = crate::generators_liveness::analyze_suspend_liveness(&body, &[], false);
    assert!(res.is_some());
    let map = res.unwrap();

    let live1 = map.get(&b_suspend1).unwrap();
    let live2 = map.get(&b_suspend2).unwrap();
    // 两个挂起点合流至同一 resume 块，shared_var 在两个点都必须存活
    assert!(live1.contains(&shared_var));
    assert!(live2.contains(&shared_var));
}

#[test]
fn liveness_iteration_budget_rejection_and_eh_gating() {
    let valid = program();
    let valid_body = &valid.functions[valid.entry].body;
    assert!(crate::generators_liveness::analyze_with_budget(valid_body, &[], false, 0).is_none());
    let mut body = Body {
        direct_globals_contracts: Vec::new(),
        source_handler_contracts: Vec::new(),
        locals: Arena::default(),
        blocks: Arena::default(),
        entry: BlockId::from_raw(la_arena::RawIdx::from_u32(0)),
        regions: Vec::new(),
    };

    // 1. 测试异常处理区域门控（EH 不得优化，必须返回 None）
    body.regions.push(Region {
        source_handler_chain: None,
        kind: RegionKind::CatchAll,
        try_start: BlockId::from_raw(la_arena::RawIdx::from_u32(0)),
        try_len: 1,
        handler: BlockId::from_raw(la_arena::RawIdx::from_u32(0)),
        exc_local: None,
    });
    assert!(crate::generators_liveness::analyze_suspend_liveness(&body, &[], false).is_none());

    // 2. 测试 product 模式门控（必须返回 None）
    body.regions.clear();
    assert!(crate::generators_liveness::analyze_suspend_liveness(&body, &[], true).is_none());
}

#[test]
fn liveness_mandatory_retention_preserves_typed_and_named_slots() {
    let mut body = Body {
        direct_globals_contracts: Vec::new(),
        source_handler_contracts: Vec::new(),
        locals: Arena::default(),
        blocks: Arena::default(),
        entry: BlockId::from_raw(la_arena::RawIdx::from_u32(0)),
        regions: Vec::new(),
    };

    // a. 静态类型局部（即使在后面从不使用，也必须强制保活，防止恢复端 Unbox 产生 null 异常）
    let typed_dead = body.locals.alloc(LocalDecl {
        ty: MirType::I64,
        name: None,
        is_cell: false,
    });
    // b. 用户命名变量（必须强制保活，保持 locals 字典与调试反射观察）
    let named_dead = body.locals.alloc(LocalDecl {
        ty: MirType::Dynamic,
        name: Some("x".into()),
        is_cell: false,
    });
    // c. 闭包 cell（必须强制保活）
    let cell_dead = body.locals.alloc(LocalDecl {
        ty: MirType::Dynamic,
        name: None,
        is_cell: true,
    });
    // d. 真正的未命名 Dynamic 编译器临时（在后续完全不使用）
    let dynamic_dead_temp = body.locals.alloc(LocalDecl {
        ty: MirType::Dynamic,
        name: None,
        is_cell: false,
    });

    let b_resume = body.blocks.alloc(BasicBlock {
        stmts: vec![],
        terminator: Terminator::Return(None),
    });

    let b_suspend = body.blocks.alloc(BasicBlock {
        stmts: vec![],
        terminator: Terminator::Suspend {
            value: Operand::Const(Constant::None),
            resume: b_resume,
            input: None,
            delegated: false,
        },
    });
    body.entry = b_suspend;

    let res = crate::generators_liveness::analyze_suspend_liveness(&body, &[], false);
    assert!(res.is_some());
    let map = res.unwrap();
    let live = map.get(&b_suspend).unwrap();

    // 静态局部、命名局部、cell 必须保留
    assert!(live.contains(&typed_dead));
    assert!(live.contains(&named_dead));
    assert!(live.contains(&cell_dead));
    // 唯有未命名 dynamic dead 临时变量被安全排除
    assert!(!live.contains(&dynamic_dead_temp));
}

#[test]
fn liveness_lowering_clears_only_dead_dynamic_temporary_slots() {
    let mut program = program();
    let id = program.entry;
    let function = &mut program.functions[id];
    let dead = function.body.locals.alloc(LocalDecl {
        ty: MirType::Dynamic,
        name: None,
        is_cell: false,
    });
    let typed = function.body.locals.alloc(LocalDecl {
        ty: MirType::I64,
        name: None,
        is_cell: false,
    });
    let entry = function.body.entry;
    let resume = function.body.blocks.alloc(BasicBlock {
        stmts: vec![],
        terminator: Terminator::Return(Some(Operand::Const(Constant::None))),
    });
    function.body.blocks[entry].stmts = vec![
        Statement::Assign {
            dest: dead,
            value: Rvalue::Use(Operand::Const(Constant::None)),
        },
        Statement::Assign {
            dest: typed,
            value: Rvalue::Use(Operand::Const(Constant::I64(42))),
        },
    ];
    function.body.blocks[entry].terminator = Terminator::Suspend {
        value: Operand::Const(Constant::None),
        resume,
        input: None,
        delegated: false,
    };
    crate::generators::lower_with_liveness_for_test(&mut program, id);
    let mut dead_cleared = false;
    let mut typed_saved = false;
    for (_, function) in program.functions.iter() {
        for (_, block) in function.body.blocks.iter() {
            for statement in &block.stmts {
                if let Statement::Eval(Rvalue::Call {
                    callee: Callee::Runtime(RuntimeFn::ArraySet),
                    args,
                }) = statement
                {
                    if let [_, Operand::Const(Constant::I64(index)), value] = args.as_slice() {
                        if *index == i64::from(dead.into_raw().into_u32()) + 4 {
                            dead_cleared |= matches!(value, Operand::Const(Constant::Null));
                        }
                        if *index == i64::from(typed.into_raw().into_u32()) + 4 {
                            assert!(!matches!(value, Operand::Const(Constant::Null)));
                            typed_saved = true;
                        }
                    }
                }
            }
        }
    }
    assert!(dead_cleared && typed_saved);
    assert!(crate::validate::validate(&program).is_ok());
}
