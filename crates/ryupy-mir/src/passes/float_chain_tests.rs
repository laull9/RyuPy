//! 浮点链只在精确守卫成功时消除中间盒；逃逸、别名与协议边界保留动态路径。
use super::MirPass;
use super::float_chain::FloatChain;
use crate::mir::*;
use la_arena::{Arena, RawIdx};

fn local(locals: &mut Arena<LocalDecl>) -> LocalId {
    locals.alloc(LocalDecl {
        ty: MirType::Dynamic,
        name: None,
        is_cell: false,
    })
}
fn operation(dest: LocalId, op: DynOp, lhs: LocalId, rhs: LocalId, inplace: bool) -> Statement {
    Statement::Assign {
        dest,
        value: Rvalue::DynBinary {
            op,
            lhs: Operand::Local(lhs),
            rhs: Operand::Local(rhs),
            inplace,
        },
    }
}
fn sample() -> (Function, [LocalId; 5]) {
    let mut locals = Arena::default();
    let a = local(&mut locals);
    let b = local(&mut locals);
    let c = local(&mut locals);
    let middle = local(&mut locals);
    let result = local(&mut locals);
    let mut blocks = Arena::default();
    let entry = blocks.alloc(BasicBlock {
        stmts: vec![
            operation(middle, DynOp::Mul, a, b, false),
            operation(result, DynOp::Add, middle, c, true),
        ],
        terminator: Terminator::Return(Some(Operand::Local(result))),
    });
    (
        Function {
            name: "float_chain".into(),
            codegen_name: "float_chain".into(),
            source_file: "float_chain.py".into(),
            params: vec![a, b, c],
            ret: MirType::Dynamic,
            body: Body {
                locals,
                blocks,
                entry,
                regions: vec![],
            },
            param_meta: vec![],
            function_global: None,
            defaults_global: None,
            specialization_identity_global: None,
            signature_global: Some(GlobalId::from_raw(RawIdx::from(0))),
            env: None,
        },
        [a, b, c, middle, result],
    )
}
fn count(f: &Function, predicate: impl Fn(&Rvalue) -> bool) -> usize {
    f.body
        .blocks
        .iter()
        .flat_map(|(_, block)| &block.stmts)
        .filter(|stmt| match stmt {
            Statement::Assign { value, .. } | Statement::Eval(value) => predicate(value),
            _ => false,
        })
        .count()
}
#[test]
fn guarded_chain_keeps_original_fallback_and_boxes_only_final_fast_result() {
    let (mut f, [a, b, c, _, _]) = sample();
    FloatChain.run(&mut f);
    assert_eq!(count(&f, |v| matches!(v, Rvalue::IsBoxedF64 { .. })), 3);
    assert_eq!(count(&f, |v| matches!(v, Rvalue::UnboxExactF64 { .. })), 3);
    assert_eq!(
        count(&f, |v| matches!(
            v,
            Rvalue::Binary {
                op: BinOp::Mul | BinOp::Add,
                ..
            }
        )),
        2
    );
    assert_eq!(
        count(&f, |v| matches!(
            v,
            Rvalue::Box {
                from: MirType::F64,
                ..
            }
        )),
        1
    );
    assert_eq!(count(&f, |v| matches!(v, Rvalue::DynBinary { .. })), 2);
    let snapshots = &f.body.blocks[f.body.entry].stmts;
    for (stmt, input) in snapshots.iter().take(3).zip([a, b, c]) {
        assert!(
            matches!(stmt, Statement::Assign { value: Rvalue::Use(Operand::Local(source)), .. } if *source == input)
        );
    }
}
#[test]
fn escaping_middle_and_redefined_or_aliased_destinations_reject_chain() {
    for case in 0..5 {
        let (mut f, [a, _, c, middle, result]) = sample();
        let entry = f.body.entry;
        match case {
            0 => f.body.blocks[entry].terminator = Terminator::Return(Some(Operand::Local(middle))),
            1 => f.body.locals[middle].name = Some("observed".into()),
            2 => f.body.blocks[entry].stmts.push(Statement::Assign {
                dest: result,
                value: Rvalue::Use(Operand::Local(a)),
            }),
            3 => f.body.blocks[entry].stmts[1] = operation(c, DynOp::Add, middle, c, true),
            _ => f.body.blocks[entry]
                .stmts
                .push(Statement::Eval(Rvalue::CallWithArgs {
                    callee: RuntimeFn::MakeTuple,
                    prefix: vec![],
                    args: vec![Operand::Array(vec![Operand::Local(middle)])],
                })),
        }
        FloatChain.run(&mut f);
        assert_eq!(
            count(&f, |v| matches!(v, Rvalue::IsBoxedF64 { .. })),
            0,
            "case {case}"
        );
    }
}
#[test]
fn callbacks_lines_copies_and_cross_block_chains_are_not_fused() {
    for case in 0..4 {
        let (mut f, [a, _, _, _, result]) = sample();
        let entry = f.body.entry;
        match case {
            0 => f.body.blocks[entry].stmts.insert(1, Statement::Line(10)),
            1 => f.body.blocks[entry].stmts.insert(
                1,
                Statement::Assign {
                    dest: result,
                    value: Rvalue::Use(Operand::Local(a)),
                },
            ),
            2 => f.body.blocks[entry].stmts.insert(
                1,
                Statement::Eval(Rvalue::Call {
                    callee: Callee::Runtime(RuntimeFn::DynStr),
                    args: vec![Operand::Local(a)],
                }),
            ),
            _ => {
                let last = f.body.blocks[entry].stmts.pop().unwrap();
                let next = f.body.blocks.alloc(BasicBlock {
                    stmts: vec![last],
                    terminator: Terminator::Return(Some(Operand::Local(result))),
                });
                f.body.blocks[entry].terminator = Terminator::Goto(next);
            }
        }
        FloatChain.run(&mut f);
        assert_eq!(count(&f, |v| matches!(v, Rvalue::IsBoxedF64 { .. })), 0);
    }
}
#[test]
fn exception_closure_cell_generator_and_intermediate_inplace_are_rejected() {
    for case in 0..6 {
        let (mut f, [a, b, _, middle, _]) = sample();
        let entry = f.body.entry;
        match case {
            0 => f.env = Some(a),
            1 => f.body.locals[a].is_cell = true,
            2 => f.signature_global = None,
            3 => f.body.regions.push(Region {
                kind: RegionKind::CatchAll,
                try_start: entry,
                try_len: 1,
                handler: entry,
                exc_local: None,
            }),
            4 => {
                f.body.blocks[entry].terminator = Terminator::Suspend {
                    value: Operand::Local(a),
                    resume: entry,
                    input: None,
                    delegated: false,
                }
            }
            _ => f.body.blocks[entry].stmts[0] = operation(middle, DynOp::Mul, a, b, true),
        }
        FloatChain.run(&mut f);
        assert_eq!(count(&f, |v| matches!(v, Rvalue::IsBoxedF64 { .. })), 0);
    }
}
#[test]
fn failed_candidates_consume_attempt_budget() {
    let (mut f, [a, b, c, _, _]) = sample();
    let entry = f.body.entry;
    f.body.blocks[entry].stmts.clear();
    for index in 0..17 {
        let middle = local(&mut f.body.locals);
        let result = local(&mut f.body.locals);
        if index < 16 {
            f.body.locals[middle].name = Some("escape".into());
        }
        f.body.blocks[entry].stmts.extend([
            operation(middle, DynOp::Mul, a, b, false),
            operation(result, DynOp::Add, middle, c, true),
            Statement::Line(index),
        ]);
    }
    FloatChain.run(&mut f);
    assert_eq!(count(&f, |v| matches!(v, Rvalue::IsBoxedF64 { .. })), 0);
}

#[test]
fn chain_input_and_function_size_budgets_preserve_original_operations() {
    for case in 0..4 {
        let (mut f, [a, b, _, middle, result]) = sample();
        let entry = f.body.entry;
        match case {
            0 | 1 => {
                f.body.blocks[entry].stmts.clear();
                let count = if case == 0 { 9 } else { 8 };
                let mut previous = a;
                for _ in 0..count {
                    let input = if case == 1 {
                        local(&mut f.body.locals)
                    } else {
                        b
                    };
                    let dest = local(&mut f.body.locals);
                    f.body.blocks[entry].stmts.push(operation(
                        dest,
                        DynOp::Add,
                        previous,
                        input,
                        false,
                    ));
                    previous = dest;
                }
                f.body.blocks[entry].terminator =
                    Terminator::Return(Some(Operand::Local(previous)));
            }
            2 => {
                for _ in 0..512 {
                    local(&mut f.body.locals);
                }
            }
            _ => {
                f.body.blocks[entry].stmts = vec![
                    operation(middle, DynOp::Mul, a, b, false),
                    operation(result, DynOp::Mul, middle, b, true),
                ];
            }
        }
        FloatChain.run(&mut f);
        assert_eq!(
            count(&f, |v| matches!(v, Rvalue::IsBoxedF64 { .. })),
            0,
            "预算/末条操作限制 {case}"
        );
    }
}
