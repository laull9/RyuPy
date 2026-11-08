//! 失败见证只解释既有拒绝：空链合法收拢仍拒，错误重定向仍拒，输出有界且不带源内容。
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
fn cleared_empty_bridge_keeps_capture_time_witness_and_rejection() {
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
    let error = contract
        .verify(&program, &[])
        .expect_err("诊断不能修正或放宽原边拒绝");
    assert!(error.contains("[edge-witness-v1 fn=0 block=bb0]"));
    assert!(error.contains("captured.terminator=Goto(bb1)"));
    assert!(error.contains("bb1 statements=0 empty=true Goto(bb2)"));
    assert!(error.contains("final.terminator=Goto(bb2)"));
    assert!(error.contains("old-target-in-final.chain[0] target=bb1 stop=terminal"));
    assert!(error.contains("bb1 statements=0 empty=true Unreachable"));
    assert!(error.len() <= MAX_ERROR_BYTES);
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
