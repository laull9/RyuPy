//! 空桥修复正例与失败见证反例；原诊断的合法桥误拒断言/原流已外置冻结保留。
use super::*;
use crate::generator_product_trace::Contract;
use crate::generator_product_tests::frontend;
use crate::passes::MirPass;
use crate::*;
use la_arena::Arena;

fn block(value: u32) -> BlockId {
    BlockId::from_raw(value.into())
}

fn graph() -> Program {
    let (hir, types) = frontend("pass\n");
    let mut plan = generator_product::DeferredGenerators::default();
    let (mut program, _, diagnostics) =
        build::build_program_mode(&hir, &types, None, Some(&mut plan));
    assert!(diagnostics.is_empty());
    assert!(plan.functions().is_empty());
    let function = &mut program.functions[program.entry];
    let is_pop = |statement: &Statement| {
        matches!(
            statement,
            Statement::Eval(Rvalue::Call {
                callee: Callee::Runtime(RuntimeFn::TracePopTo),
                ..
            })
        )
    };
    let pop = function
        .body
        .blocks
        .iter()
        .flat_map(|(_, b)| &b.stmts)
        .find(|s| is_pop(s))
        .expect("实际模块正常出口的Pop")
        .clone();
    let mut entry = function.body.blocks[function.body.entry].stmts.clone();
    entry.retain(|s| !is_pop(s));
    // 保留真实Push/来源定义及出口Pop，只建立可独立观察的三个块P→E→T。
    let mut blocks = Arena::default();
    blocks.alloc(BasicBlock {
        stmts: entry,
        terminator: Terminator::Goto(block(1)),
    });
    blocks.alloc(BasicBlock {
        stmts: vec![],
        terminator: Terminator::Goto(block(2)),
    });
    blocks.alloc(BasicBlock {
        stmts: vec![pop],
        terminator: Terminator::Return(None),
    });
    function.body.blocks = blocks;
    function.body.entry = block(0);
    assert!(function.body.regions.is_empty());
    validate::validate_high(&program, &[]).unwrap();
    program
}

#[test]
fn cleared_empty_bridge_is_accepted_using_capture_time_canonical_edge() {
    let mut program = graph();
    let contract = Contract::capture(&program, &[]).unwrap();
    contract.verify(&program, &[]).unwrap();
    let entry = program.entry;
    passes::SimplifyCfg.run(&mut program.functions[entry]);
    validate::validate_high(&program, &[]).unwrap();
    assert_eq!(
        program.functions[entry].body.blocks[block(1)].terminator,
        Terminator::Unreachable
    );
    contract.verify(&program, &[]).unwrap();
}

#[test]
fn real_redirect_still_rejects_and_is_not_described_as_erased_bridge() {
    let mut program = graph();
    let contract = Contract::capture(&program, &[]).unwrap();
    let entry = program.entry;
    program.functions[entry].body.blocks[block(0)].terminator = Terminator::Goto(block(0));
    validate::validate_high(&program, &[]).unwrap();
    let error = contract
        .verify(&program, &[])
        .expect_err("错误真实目标必须仍拒绝");
    assert!(error.contains("captured.terminator=Goto(bb1)"));
    assert!(error.contains("final.terminator=Goto(bb0)"));
    assert!(error.contains("old-target-in-final.chain[0] target=bb1 stop=non-empty"));
    assert!(!error.contains("empty=true Unreachable"));
}

#[test]
fn witness_bounds_chains_utf8_and_redacts_literal_contents() {
    let mut body = Body {
        source_handler_contracts: vec![],
        locals: Arena::default(),
        blocks: Arena::default(),
        entry: block(0),
        regions: vec![],
    };
    for index in 0..10 {
        body.blocks.alloc(BasicBlock {
            stmts: vec![],
            terminator: Terminator::Goto(block(index + 1)),
        });
    }
    body.blocks.alloc(BasicBlock {
        stmts: vec![],
        terminator: Terminator::Return(None),
    });
    // 这里只测试摘要器输入，不把此字符串条件当作合法MIR或产品正例。
    let edge = Terminator::Branch {
        cond: Operand::Const(Constant::Str("/private/environment/SECRET_SENTINEL".into())),
        then_: block(0),
        else_: block(1),
    };
    let witness = EdgeWitness::capture(&body, &edge);
    assert_eq!(witness.chains.len(), 2);
    assert!(
        witness
            .chains
            .iter()
            .all(|c| c.nodes.len() == MAX_CHAIN_NODES)
    );
    let function = FnId::from_raw(0.into());
    let error = failure(function, block(0), &witness, &body, &edge, &edge);
    assert!(error.contains("chain_truncated=true"));
    assert!(error.contains("const(str,redacted)"));
    assert!(!error.contains("SECRET_SENTINEL"));
    assert!(!error.contains("/private"));
    assert!(error.len() <= MAX_ERROR_BYTES);
    let bounded = bound_error("界".repeat(MAX_ERROR_BYTES));
    assert!(bounded.len() <= MAX_ERROR_BYTES);
    assert!(bounded.ends_with(TRUNCATED));
    assert!(bounded.is_char_boundary(bounded.len()));
}

#[test]
fn dead_values_keep_original_nonempty_anchor_but_bypassing_it_rejects() {
    for boxed in [false, true] {
        let mut program = graph();
        let function = &mut program.functions[program.entry];
        let dest = function.body.locals.alloc(LocalDecl {
            ty: if boxed { MirType::Dynamic } else { MirType::I64 },
            name: None,
            is_cell: false,
        });
        let value = Operand::Const(Constant::I64(7));
        function.body.blocks[block(1)].stmts.push(Statement::Assign {
            dest,
            value: if boxed {
                Rvalue::Box {
                    value,
                    from: MirType::I64,
                }
            } else {
                Rvalue::Use(value)
            },
        });
        validate::validate_high(&program, &[]).unwrap();
        let contract = Contract::capture(&program, &[]).unwrap();
        let function = &mut program.functions[program.entry];
        passes::SimplifyCfg.run(function);
        passes::DeadValues.run(function);
        assert!(function.body.blocks[block(1)].stmts.is_empty());
        validate::validate_high(&program, &[]).unwrap();
        contract.verify(&program, &[]).unwrap();
        program.functions[program.entry].body.blocks[block(0)].terminator =
            Terminator::Goto(block(2));
        assert!(contract.verify(&program, &[]).is_err());
    }
}

#[test]
fn contract_rejects_bad_ids_before_stack_walk_and_arena_deletion() {
    let source = graph();
    let contract = Contract::capture(&source, &[]).unwrap();
    for mutation in 0..4 {
        let mut changed = graph();
        let body = &mut changed.functions[changed.entry].body;
        match mutation {
            0 => body.entry = block(99),
            1 => body.blocks[block(0)].terminator = Terminator::Goto(block(99)),
            2 => {
                body.regions.push(Region {
                    source_handler_chain: None,
                    kind: RegionKind::CatchAll,
                    try_start: block(1),
                    try_len: 1,
                    handler: block(99),
                    exc_local: None,
                });
            }
            _ => {
                let mut shorter = Arena::default();
                for (_, block) in body.blocks.iter().take(2) {
                    shorter.alloc(block.clone());
                }
                body.blocks = shorter;
            }
        }
        // 直接调用Contract，不能依靠测试外层validate或catch_unwind把panic算拒绝。
        assert!(Contract::capture(&changed, &[]).is_err());
        assert!(contract.verify(&changed, &[]).is_err());
    }
}

#[test]
fn bypassing_an_original_line_or_trace_effect_is_rejected() {
    for trace in [false, true] {
        let mut program = graph();
        let body = &mut program.functions[program.entry].body;
        let statement = if trace {
            Statement::Eval(Rvalue::Call {
                callee: Callee::Runtime(RuntimeFn::TraceLine),
                args: vec![Operand::Const(Constant::I64(9))],
            })
        } else {
            Statement::Line(9)
        };
        body.blocks[block(1)].stmts.push(statement);
        validate::validate_high(&program, &[]).unwrap();
        let contract = Contract::capture(&program, &[]).unwrap();
        let body = &mut program.functions[program.entry].body;
        body.blocks[block(0)].terminator = Terminator::Goto(block(2));
        body.blocks[block(1)].stmts.clear();
        body.blocks[block(1)].terminator = Terminator::Unreachable;
        assert!(contract.verify(&program, &[]).is_err());
    }
}

#[test]
fn simplify_cfg_clears_both_ordered_empty_branch_bridges() {
    let mut program = graph();
    let body = &mut program.functions[program.entry].body;
    let condition = body.locals.alloc(LocalDecl {
        ty: MirType::Bool,
        name: None,
        is_cell: false,
    });
    body.blocks[block(0)].stmts.push(Statement::Assign {
        dest: condition,
        value: Rvalue::Use(Operand::Const(Constant::Bool(true))),
    });
    let second = body.blocks.alloc(BasicBlock {
        stmts: vec![],
        terminator: Terminator::Goto(block(4)),
    });
    body.blocks.alloc(BasicBlock {
        stmts: vec![Statement::Line(8)],
        terminator: Terminator::Goto(block(2)),
    });
    body.blocks[block(0)].terminator = Terminator::Branch {
        cond: Operand::Local(condition),
        then_: block(1),
        else_: second,
    };
    validate::validate_high(&program, &[]).unwrap();
    let contract = Contract::capture(&program, &[]).unwrap();
    passes::SimplifyCfg.run(&mut program.functions[program.entry]);
    validate::validate_high(&program, &[]).unwrap();
    let body = &program.functions[program.entry].body;
    for bridge in [block(1), second] {
        assert_eq!(body.blocks[bridge].terminator, Terminator::Unreachable);
    }
    contract.verify(&program, &[]).unwrap();
}
