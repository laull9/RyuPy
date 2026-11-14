//! 浮点循环状态策略的结构验收：每次重入重置、原盒/慢路保留、严格图与观察约束。
use super::MirPass;
use super::float_chain::FloatChain;
use crate::mir::*;
use la_arena::{Arena, RawIdx};

fn local(locals: &mut Arena<LocalDecl>, ty: MirType) -> LocalId {
    locals.alloc(LocalDecl {
        ty,
        name: None,
        is_cell: false,
    })
}
fn assign(dest: LocalId, value: Rvalue) -> Statement {
    Statement::Assign { dest, value }
}
pub(super) fn sample() -> (Function, [BlockId; 5], [LocalId; 6]) {
    let mut locals = Arena::default();
    let a = local(&mut locals, MirType::Dynamic);
    let b = local(&mut locals, MirType::Dynamic);
    let initial = local(&mut locals, MirType::Dynamic);
    let total = local(&mut locals, MirType::Dynamic);
    let product = local(&mut locals, MirType::Dynamic);
    let result = local(&mut locals, MirType::Dynamic);
    locals[total].name = Some("total".into());
    let mut blocks = Arena::default();
    let ids: Vec<_> = (0..5)
        .map(|_| {
            blocks.alloc(BasicBlock {
                stmts: vec![],
                terminator: Terminator::Unreachable,
            })
        })
        .collect();
    let [pre, header, body, latch, exit] = ids.try_into().unwrap();
    blocks[pre].stmts = vec![
        assign(
            initial,
            Rvalue::Box {
                value: Operand::Const(Constant::F64(0.0)),
                from: MirType::F64,
            },
        ),
        assign(total, Rvalue::Use(Operand::Local(initial))),
    ];
    blocks[pre].terminator = Terminator::Goto(header);
    blocks[header].terminator = Terminator::Branch {
        cond: Operand::Const(Constant::Bool(true)),
        then_: body,
        else_: exit,
    };
    blocks[body].stmts = vec![
        assign(
            product,
            Rvalue::DynBinary {
                op: DynOp::Mul,
                lhs: Operand::Local(a),
                rhs: Operand::Local(b),
                inplace: false,
            },
        ),
        assign(
            result,
            Rvalue::DynBinary {
                op: DynOp::Add,
                lhs: Operand::Local(total),
                rhs: Operand::Local(product),
                inplace: true,
            },
        ),
        assign(total, Rvalue::Use(Operand::Local(result))),
    ];
    blocks[body].terminator = Terminator::Goto(latch);
    blocks[latch].terminator = Terminator::Goto(header);
    blocks[exit].terminator = Terminator::Return(Some(Operand::Local(total)));
    (
        Function {
            module_state: None,
            instance_capabilities: Default::default(),
            module_state_source: None,
            name: "float_loop".into(),
            codegen_name: "float_loop".into(),
            source_file: "float_loop.py".into(),
            definition: None,
            params: vec![a, b],
            ret: MirType::Dynamic,
            body: Body {
                direct_globals_contracts: Vec::new(),
                source_handler_contracts: Vec::new(),
                locals,
                blocks,
                entry: pre,
                regions: vec![],
            },
            param_meta: vec![],
            function_global: None,
            defaults_global: None,
            specialization_identity_global: None,
            signature_global: Some(GlobalId::from_raw(RawIdx::from(0))),
            env: None,
            dynamic_origin: None,
        },
        [pre, header, body, latch, exit],
        [a, b, initial, total, product, result],
    )
}
fn optimized(f: &Function) -> bool {
    f.body
        .locals
        .iter()
        .any(|(_, l)| l.name.as_deref() == Some("$float_loop_raw"))
}
fn count(f: &Function, predicate: impl Fn(&Rvalue) -> bool) -> usize {
    f.body
        .blocks
        .iter()
        .flat_map(|(_, b)| &b.stmts)
        .filter(|s| match s {
            Statement::Assign { value, .. } | Statement::Eval(value) => predicate(value),
            _ => false,
        })
        .count()
}
#[test]
fn raw_state_has_only_boundary_boxes_and_keeps_original_dynamic_chain() {
    let (mut f, [pre, _, _, _, exit], [_, _, _, total, _, _]) = sample();
    FloatChain.run(&mut f);
    assert!(optimized(&f));
    assert_eq!(
        count(&f, |v| matches!(v, Rvalue::DynBinary { .. })),
        2,
        "原慢路不应再被短链guard包装"
    );
    assert_eq!(
        count(&f, |v| matches!(
            v,
            Rvalue::Box {
                from: MirType::F64,
                ..
            }
        )),
        3,
        "初值盒+慢路物化+出口物化"
    );
    assert_eq!(
        count(&f, |v| matches!(v, Rvalue::IsBoxedF64 { .. })),
        4,
        "两个普通输入与初始化/慢路模式检查"
    );
    assert!(matches!(
        f.body.blocks[pre].terminator,
        Terminator::Branch { .. }
    ));
    assert!(matches!(
        f.body.blocks[exit].terminator,
        Terminator::Branch { .. }
    ));
    let fast = f
        .body
        .blocks
        .iter()
        .find(|(_, b)| {
            b.stmts.iter().any(|s| {
                matches!(s,
        Statement::Assign{dest,value:Rvalue::Use(Operand::Const(Constant::Null))} if *dest==total)
            })
        })
        .unwrap()
        .1;
    assert!(!fast.stmts.iter().any(|s| matches!(
        s,
        Statement::Assign {
            value: Rvalue::Box { .. },
            ..
        }
    )));
    assert!(
        f.body.blocks[pre].stmts.iter().any(|s| matches!(s,
        Statement::Assign{dest,value:Rvalue::Use(Operand::Local(_))} if *dest==total)),
        "零轮保原对象写入"
    );
}
#[test]
fn outer_reentry_is_allowed_only_with_a_new_preheader_reset() {
    let (mut f, [pre, _, _, _, exit], _) = sample();
    let outer = f.body.blocks.alloc(BasicBlock {
        stmts: vec![],
        terminator: Terminator::Goto(pre),
    });
    f.body.entry = outer;
    f.body.blocks[exit].terminator = Terminator::Goto(pre);
    FloatChain.run(&mut f);
    assert!(optimized(&f));
    assert!(f.body.blocks[pre].stmts.iter().any(|s|matches!(s,
        Statement::Assign{value:Rvalue::Use(Operand::Const(Constant::F64(value))),..} if *value==0.0)),"每次pre重入重置raw再检查真实初值");
}
#[test]
fn non_float_initial_value_and_missing_or_extra_resets_keep_short_chain_strategy() {
    for case in 0..4 {
        let (mut f, [pre, _, _, _, _], [a, _, initial, total, _, _]) = sample();
        match case {
            0 => {
                f.body.blocks[pre].stmts[0] = assign(
                    initial,
                    Rvalue::Box {
                        value: Operand::Const(Constant::I64(0)),
                        from: MirType::I64,
                    },
                )
            }
            1 => f.body.blocks[pre].stmts[1] = assign(total, Rvalue::Use(Operand::Local(a))),
            2 => f.body.blocks[pre]
                .stmts
                .push(assign(total, Rvalue::Use(Operand::Local(initial)))),
            _ => {
                f.body.blocks[pre].stmts.remove(1);
                f.params.push(total);
            }
        }
        FloatChain.run(&mut f);
        assert!(!optimized(&f), "case {case}");
        assert!(
            count(&f, |v| matches!(v, Rvalue::IsBoxedF64 { .. })) > 0,
            "不匹配loop仍沿原短链"
        );
    }
}
#[test]
fn extra_incoming_edges_breaks_and_accumulator_observation_prevent_loop_strategy() {
    for case in 0..5 {
        let (mut f, [pre, header, body, latch, exit], [_, _, _, total, _, result]) = sample();
        match case {
            0 => {
                f.body.blocks.alloc(BasicBlock {
                    stmts: vec![],
                    terminator: Terminator::Goto(exit),
                });
            }
            1 => {
                f.body.blocks[body].terminator = Terminator::Branch {
                    cond: Operand::Const(Constant::Bool(true)),
                    then_: latch,
                    else_: exit,
                }
            }
            2 => f.body.blocks[header]
                .stmts
                .push(Statement::Eval(Rvalue::Call {
                    callee: Callee::Runtime(RuntimeFn::DynStr),
                    args: vec![Operand::Local(total)],
                })),
            3 => f.body.blocks[latch]
                .stmts
                .push(Statement::Eval(Rvalue::Call {
                    callee: Callee::Runtime(RuntimeFn::DynStr),
                    args: vec![Operand::Local(total)],
                })),
            _ => f.body.blocks[pre].stmts.push(Statement::Eval(Rvalue::Call {
                callee: Callee::Runtime(RuntimeFn::DynStr),
                args: vec![Operand::Local(result)],
            })),
        }
        FloatChain.run(&mut f);
        assert!(!optimized(&f), "case {case}");
    }
}
#[test]
fn observing_total_earlier_in_the_chain_and_escaping_final_temporary_are_rejected() {
    for case in 0..2 {
        let (mut f, [_, _, body, _, exit], [a, _, _, total, product, result]) = sample();
        if case == 0 {
            f.body.blocks[body].stmts[0] = assign(
                product,
                Rvalue::DynBinary {
                    op: DynOp::Mul,
                    lhs: Operand::Local(total),
                    rhs: Operand::Local(a),
                    inplace: false,
                },
            );
        } else {
            f.body.blocks[exit].terminator = Terminator::Return(Some(Operand::Local(result)));
        }
        FloatChain.run(&mut f);
        assert!(!optimized(&f));
    }
}

#[test]
fn no_loop_internal_block_may_be_the_function_entry() {
    for position in 1..5 {
        let (mut f, blocks, _) = sample();
        f.body.entry = blocks[position];
        FloatChain.run(&mut f);
        assert!(!optimized(&f));
    }
}

#[test]
fn no_more_than_four_loops_use_raw_state_in_one_function() {
    let (mut f, _, [a, b, ..]) = sample();
    f.body.blocks = Arena::default();
    let mut previous_exit: Option<BlockId> = None;
    for index in 0..5 {
        let initial = local(&mut f.body.locals, MirType::Dynamic);
        let total = local(&mut f.body.locals, MirType::Dynamic);
        let product = local(&mut f.body.locals, MirType::Dynamic);
        let result = local(&mut f.body.locals, MirType::Dynamic);
        let ids: Vec<_> = (0..5)
            .map(|_| {
                f.body.blocks.alloc(BasicBlock {
                    stmts: vec![],
                    terminator: Terminator::Unreachable,
                })
            })
            .collect();
        let [pre, header, body, latch, exit] = ids.try_into().unwrap();
        if let Some(previous) = previous_exit {
            f.body.blocks[previous].terminator = Terminator::Goto(pre);
        } else {
            f.body.entry = pre;
        }
        f.body.blocks[pre].stmts = vec![
            assign(
                initial,
                Rvalue::Box {
                    value: Operand::Const(Constant::F64(index as f64)),
                    from: MirType::F64,
                },
            ),
            assign(total, Rvalue::Use(Operand::Local(initial))),
        ];
        f.body.blocks[pre].terminator = Terminator::Goto(header);
        f.body.blocks[header].terminator = Terminator::Branch {
            cond: Operand::Const(Constant::Bool(true)),
            then_: body,
            else_: exit,
        };
        f.body.blocks[body].stmts = vec![
            assign(
                product,
                Rvalue::DynBinary {
                    op: DynOp::Mul,
                    lhs: Operand::Local(a),
                    rhs: Operand::Local(b),
                    inplace: false,
                },
            ),
            assign(
                result,
                Rvalue::DynBinary {
                    op: DynOp::Add,
                    lhs: Operand::Local(total),
                    rhs: Operand::Local(product),
                    inplace: true,
                },
            ),
            assign(total, Rvalue::Use(Operand::Local(result))),
        ];
        f.body.blocks[body].terminator = Terminator::Goto(latch);
        f.body.blocks[latch].terminator = Terminator::Goto(header);
        f.body.blocks[exit].terminator = Terminator::Return(Some(Operand::Local(total)));
        previous_exit = Some(exit);
    }
    FloatChain.run(&mut f);
    assert_eq!(
        f.body
            .locals
            .iter()
            .filter(|(_, local)| local.name.as_deref() == Some("$float_loop_raw"))
            .count(),
        4
    );
    assert!(f.body.blocks.len() <= 128 && f.body.locals.len() <= 512);
    assert_eq!(
        count(&f, |v| matches!(v, Rvalue::DynBinary { .. })),
        10,
        "每个动态原链只留一份，不重复特化"
    );
}
