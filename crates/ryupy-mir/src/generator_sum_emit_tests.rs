//! 编译接线的结构合同：源准入、真实字段守卫、同帧回退及批量入口签名；非运行收益证明。
use super::*;

fn built(source: &str) -> Program {
    let (mut program, deferred) = crate::generator_sum_plan::high_with_sources(source);
    let candidates = prepare(&program, &deferred);
    let mut fallback = vec![];
    for &id in deferred.functions() {
        if !crate::generator_state::lower(&mut program, id) {
            fallback.push(id);
        }
    }
    crate::generators::lower(&mut program, &fallback, &deferred.exceptions);
    finish(&mut program, candidates);
    crate::validate::validate(&program).unwrap();
    program
}

fn pairs(program: &Program) -> Vec<(FnId, FnId)> {
    program
        .functions
        .iter()
        .flat_map(|(_, f)| f.body.blocks.iter())
        .flat_map(|(_, b)| &b.stmts)
        .filter_map(|s| match s {
            Statement::Assign {
                value:
                    Rvalue::MakeStateGenerator {
                        resume,
                        sum_resume: Some(sum),
                        ..
                    },
                ..
            } => Some((*resume, *sum)),
            _ => None,
        })
        .collect()
}

#[test]
fn compiled_batch_has_guard_and_same_frame_fallback() {
    let program = built("g = (x * 2 for x in range(4))\n");
    let pair = pairs(&program);
    assert_eq!(pair.len(), 1);
    let (resume, sum) = pair[0];
    let batch = &program.functions[sum];
    assert_eq!(batch.params.len(), 2);
    // 核查真实yield块的发布顺序与边，不用helper存在性冒充融合闭环。
    let (yield_id, yield_block, consume_args, consume_dest) = batch
        .body
        .blocks
        .iter()
        .find_map(|(id, block)| match block.stmts.last()? {
            Statement::Assign {
                dest,
                value:
                    Rvalue::Call {
                        callee: Callee::Runtime(RuntimeFn::GeneratorSumTryConsume),
                        args,
                    },
            } => Some((id, block, args, *dest)),
            _ => None,
        })
        .unwrap();
    assert!(matches!(&yield_block.stmts[yield_block.stmts.len() - 2],
        Statement::Eval(Rvalue::Call {
            callee: Callee::Runtime(RuntimeFn::GeneratorStateSetPC), args })
            if args.first() == Some(&Operand::Local(batch.params[0]))));
    let Terminator::Branch { cond, then_, else_ } = &yield_block.terminator else {
        panic!("累计后必须显式分开成功与拒绝边");
    };
    assert_eq!(*cond, Operand::Local(consume_dest));
    // PC发布完整语句必须与普通恢复一致，包括数值和frame，不能只检查helper名称。
    assert_eq!(
        &yield_block.stmts[yield_block.stmts.len() - 2],
        program.functions[resume].body.blocks[yield_id]
            .stmts
            .last()
            .unwrap(),
    );
    assert_eq!(*then_, program.functions[resume].body.entry);
    assert_eq!(
        batch.body.blocks[*else_].terminator,
        Terminator::Return(Some(consume_args[1].clone()))
    );
    assert_eq!(
        program.functions[resume].body.blocks[yield_id].terminator,
        Terminator::Return(Some(consume_args[1].clone()))
    );

    let statements: Vec<_> = batch
        .body
        .blocks
        .iter()
        .flat_map(|(_, b)| &b.stmts)
        .collect();
    assert!(statements.iter().any(|s| matches!(
        s,
        Statement::Assign {
            value: Rvalue::Call {
                callee: Callee::Runtime(RuntimeFn::GeneratorSumRangeGuard),
                ..
            },
            ..
        }
    )));
    assert!(statements.iter().any(|s| matches!(s,
        Statement::Assign { value: Rvalue::Call { callee: Callee::Fn(id), args }, .. }
            if *id == resume && args == &[Operand::Local(batch.params[0])])));
    assert!(statements.iter().any(|s| matches!(s,
        Statement::Assign { value: Rvalue::Call {
            callee: Callee::Runtime(RuntimeFn::GeneratorSumTryConsume), args }, .. }
            if args.first() == Some(&Operand::Local(batch.params[1])))));
}

#[test]
fn unknown_callback_closure_and_filter_do_not_get_batch_entry() {
    for source in [
        "def f(x):\n    return x\ng = (f(x) for x in range(4))\n",
        "def f(n):\n    return (x * n for x in range(4))\n",
        "g = (x * 2 for x in range(4) if x)\n",
        "def negative():\n    return (x * -3 for x in range(-2, 5))\n",
    ] {
        assert!(pairs(&built(source)).is_empty());
    }
}

#[test]
fn ordinary_resume_is_not_replaced_or_given_accumulator() {
    let program = built("g = (x * 2 for x in range(4))\n");
    let (resume, sum) = pairs(&program)[0];
    assert_ne!(resume, sum);
    let normal = &program.functions[resume];
    assert_eq!(normal.params.len(), 1);
    assert!(
        !normal
            .body
            .blocks
            .iter()
            .flat_map(|(_, b)| &b.stmts)
            .any(|s| matches!(
                s,
                Statement::Assign {
                    value: Rvalue::Call {
                        callee: Callee::Runtime(RuntimeFn::GeneratorSumTryConsume),
                        ..
                    },
                    ..
                }
            ))
    );
}
