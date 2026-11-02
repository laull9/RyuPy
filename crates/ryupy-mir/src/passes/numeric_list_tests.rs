//! producer/consumer岛必须同时命中；失败预检保原GetItem序列，所有观察/支配风险保普通路径。
use super::MirPass;
use super::float_chain::FloatChain;
use crate::mir::*;

fn local(f: &mut Function) -> LocalId {
    f.body.locals.alloc(LocalDecl {
        ty: MirType::Dynamic,
        name: None,
        is_cell: false,
    })
}
fn call(dest: LocalId, callee: RuntimeFn, args: Vec<Operand>) -> Statement {
    Statement::Assign {
        dest,
        value: Rvalue::Call {
            callee: Callee::Runtime(callee),
            args,
        },
    }
}
fn get(value: LocalId) -> Operand {
    Operand::Local(value)
}
struct Sample {
    f: Function,
    blocks: [BlockId; 5],
    total: LocalId,
    receiver: LocalId,
    root: LocalId,
    leaves: [LocalId; 2],
    child: LocalId,
    keys: [LocalId; 2],
}
fn sample() -> Sample {
    let (mut f, blocks, [a, b, _, total, product, result]) = super::float_loop_tests::sample();
    let [pre, _, body, _, exit] = blocks;
    let root = local(&mut f);
    let receiver = local(&mut f);
    let first = local(&mut f);
    let child = local(&mut f);
    let second = local(&mut f);
    let key = local(&mut f);
    let column = local(&mut f);
    let output = local(&mut f);
    f.params.extend([key, column]);
    f.body.blocks[pre].stmts.splice(
        0..0,
        [
            Statement::Assign {
                dest: root,
                value: Rvalue::CallWithArgs {
                    callee: RuntimeFn::MakeList,
                    prefix: vec![],
                    args: vec![],
                },
            },
            Statement::Assign {
                dest: receiver,
                value: Rvalue::Use(get(root)),
            },
        ],
    );
    f.body.blocks[body].stmts = vec![
        call(first, RuntimeFn::GetItem, vec![get(a), get(key)]),
        call(child, RuntimeFn::GetItem, vec![get(b), get(key)]),
        call(second, RuntimeFn::GetItem, vec![get(child), get(column)]),
        Statement::Assign {
            dest: product,
            value: Rvalue::DynBinary {
                op: DynOp::Mul,
                lhs: get(first),
                rhs: get(second),
                inplace: false,
            },
        },
        Statement::Assign {
            dest: result,
            value: Rvalue::DynBinary {
                op: DynOp::Add,
                lhs: get(total),
                rhs: get(product),
                inplace: true,
            },
        },
        Statement::Assign {
            dest: total,
            value: Rvalue::Use(get(result)),
        },
    ];
    f.body.blocks[exit].stmts = vec![
        Statement::Line(21),
        Statement::Assign {
            dest: output,
            value: Rvalue::DynCallMethod {
                captured: None,
                obj: get(receiver),
                name: "append".into(),
                args: vec![get(total)],
            },
        },
    ];
    f.body.blocks[exit].terminator = Terminator::Return(Some(get(receiver)));
    Sample {
        f,
        blocks,
        total,
        receiver,
        root,
        leaves: [first, second],
        child,
        keys: [key, column],
    }
}
fn calls(f: &Function, callee: RuntimeFn) -> usize {
    f.body.blocks.iter().flat_map(|(_,b)|&b.stmts).filter(|s|matches!(s,
        Statement::Assign{value:Rvalue::Call{callee:Callee::Runtime(found),..},..}|Statement::Eval(Rvalue::Call{callee:Callee::Runtime(found),..}) if *found==callee)).count()
}
#[test]
fn real_two_level_region_shares_arithmetic_and_preserves_original_read_fallback() {
    let mut s = sample();
    FloatChain.run(&mut s.f);
    for (callee, expected) in [
        (RuntimeFn::NumericListCreate, 1),
        (RuntimeFn::NumericListAppendF64, 1),
        (RuntimeFn::NumericListPeekList, 1),
        (RuntimeFn::NumericListCanReadF64, 2),
        (RuntimeFn::NumericListReadCheckedF64, 2),
        (RuntimeFn::NumericListReadF64, 0),
        (RuntimeFn::GetItem, 3),
    ] {
        assert_eq!(calls(&s.f, callee), expected, "{callee:?}");
    }
    let (original_id, original) =
        s.f.body
            .blocks
            .iter()
            .find(|(_, b)| {
                b.stmts.iter().any(|s| {
                    matches!(
                        s,
                        Statement::Assign {
                            value: Rvalue::Call {
                                callee: Callee::Runtime(RuntimeFn::GetItem),
                                ..
                            },
                            ..
                        }
                    )
                })
            })
            .unwrap();
    let ids: Vec<_> = original
        .stmts
        .iter()
        .filter_map(|stmt| match stmt {
            Statement::Assign {
                dest,
                value:
                    Rvalue::Call {
                        callee: Callee::Runtime(RuntimeFn::GetItem),
                        ..
                    },
            } => Some(*dest),
            _ => None,
        })
        .collect();
    assert_eq!(
        ids,
        [s.leaves[0], s.child, s.leaves[1]],
        "失败后原读取依源序一次执行"
    );
    for (_, block) in s.f.body.blocks.iter() {
        if block.stmts.iter().any(|stmt| {
            matches!(
                stmt,
                Statement::Assign {
                    value: Rvalue::Call {
                        callee: Callee::Runtime(RuntimeFn::NumericListCanReadF64),
                        ..
                    },
                    ..
                }
            )
        }) {
            assert!(matches!(block.terminator,Terminator::Branch{else_,..} if else_==original_id));
        }
    }
    let arithmetic: Vec<_> =
        s.f.body
            .blocks
            .iter()
            .filter(|(_, b)| {
                b.stmts.iter().any(|stmt| {
                    matches!(
                        stmt,
                        Statement::Assign {
                            value: Rvalue::Binary { op: BinOp::Mul, .. },
                            ..
                        }
                    )
                })
            })
            .collect();
    assert_eq!(arithmetic.len(), 1, "typed与boxed路径共享算术块");
    assert!(
        s.f.body
            .locals
            .iter()
            .any(|(_, local)| local.name.as_deref() == Some("$float_loop_raw"))
    );
}
#[test]
fn total_observation_or_non_append_sink_does_not_change_list_factory() {
    for case in 0..4 {
        let mut s = sample();
        let exit = s.blocks[4];
        match case {
            0 => s.f.body.blocks[exit].terminator = Terminator::Return(Some(get(s.total))),
            1 => {
                if let Statement::Assign {
                    value: Rvalue::DynCallMethod { name, .. },
                    ..
                } = &mut s.f.body.blocks[exit].stmts[1]
                {
                    *name = "extend".into();
                }
            }
            2 => s.f.body.blocks[exit].stmts.insert(
                0,
                Statement::Eval(Rvalue::Call {
                    callee: Callee::Runtime(RuntimeFn::DynStr),
                    args: vec![get(s.receiver)],
                }),
            ),
            _ => s.f.body.blocks[exit]
                .stmts
                .push(Statement::Eval(Rvalue::CallWithArgs {
                    callee: RuntimeFn::MakeTuple,
                    prefix: vec![],
                    args: vec![Operand::Array(vec![get(s.total)])],
                })),
        }
        FloatChain.run(&mut s.f);
        assert_eq!(calls(&s.f, RuntimeFn::NumericListCreate), 0, "case {case}");
        assert!(
            s.f.body
                .locals
                .iter()
                .any(|(_, l)| l.name.as_deref() == Some("$float_loop_raw")),
            "只退回现FloatLoop"
        );
    }
}
#[test]
fn unique_but_non_dominating_factory_is_not_a_proof_of_list_identity() {
    let mut s = sample();
    let pre = s.blocks[0];
    let factory = s.f.body.blocks[pre].stmts.remove(0);
    s.f.body.blocks.alloc(BasicBlock {
        stmts: vec![factory],
        terminator: Terminator::Return(None),
    });
    FloatChain.run(&mut s.f);
    assert_eq!(calls(&s.f, RuntimeFn::NumericListCreate), 0);
}
#[test]
fn observed_loads_callback_boundaries_and_dependent_indices_reject_island() {
    for case in 0..4 {
        let mut s = sample();
        let body = s.blocks[2];
        let exit = s.blocks[4];
        match case {
            0 => s.f.body.blocks[exit]
                .stmts
                .push(Statement::Eval(Rvalue::Call {
                    callee: Callee::Runtime(RuntimeFn::DynStr),
                    args: vec![get(s.leaves[0])],
                })),
            1 => s.f.body.blocks[body].stmts.insert(2, Statement::Line(9)),
            2 => s.f.body.blocks[body].stmts.insert(
                2,
                Statement::Eval(Rvalue::Call {
                    callee: Callee::Runtime(RuntimeFn::DynStr),
                    args: vec![get(s.keys[0])],
                }),
            ),
            _ => {
                if let Statement::Assign {
                    value: Rvalue::Call { args, .. },
                    ..
                } = &mut s.f.body.blocks[body].stmts[2]
                {
                    args[1] = get(s.leaves[0]);
                }
            }
        }
        FloatChain.run(&mut s.f);
        assert_eq!(calls(&s.f, RuntimeFn::NumericListCreate), 0, "case {case}");
    }
}
#[test]
fn excess_depth_and_partial_budget_never_rewrite_the_factory() {
    for case in 0..2 {
        let mut s = sample();
        let body = s.blocks[2];
        if case == 0 {
            let deeper = local(&mut s.f);
            s.f.body.blocks[body].stmts.insert(
                2,
                call(
                    deeper,
                    RuntimeFn::GetItem,
                    vec![get(s.child), get(s.keys[0])],
                ),
            );
            if let Statement::Assign {
                value: Rvalue::Call { args, .. },
                ..
            } = &mut s.f.body.blocks[body].stmts[3]
            {
                args[0] = get(deeper);
            }
        } else {
            // 普通loop扩张12个局部刚好可用，岛还需4个；失败不得留下半转换的工厂。
            while s.f.body.locals.len() < 500 {
                local(&mut s.f);
            }
        }
        FloatChain.run(&mut s.f);
        assert_eq!(calls(&s.f, RuntimeFn::NumericListCreate), 0);
        assert!(s.f.body.blocks.iter().any(|(_,b)|b.stmts.iter().any(|stmt|matches!(stmt,
            Statement::Assign{dest,value:Rvalue::CallWithArgs{callee:RuntimeFn::MakeList,..}} if *dest==s.root))));
    }
}

/// 捕获只对actualmodule有语义；本岛证明唯一MakeList来源后才允许将结果视为identity。
#[test]
fn module_capture_preserves_proven_list_island_and_rejects_unknown_or_escaped_target() {
    for mode in 0..8 {
        let mut sample = sample();
        let exit = sample.blocks[4];
        let captured = local(&mut sample.f);
        let receiver = local(&mut sample.f);
        let flag = local(&mut sample.f);
        sample.f.body.locals[flag].ty = MirType::Bool;
        let source = if mode == 1 {
            sample.keys[0]
        } else {
            sample.receiver
        };
        let name = if mode == 2 { "other" } else { "append" };
        sample.f.body.blocks[exit].stmts.splice(
            1..1,
            [
                Statement::Assign {
                    dest: receiver,
                    value: Rvalue::Use(get(source)),
                },
                call(flag, RuntimeFn::IsModule, vec![get(receiver)]),
                call(
                    captured,
                    RuntimeFn::CaptureModuleTarget,
                    vec![get(receiver), Operand::Const(Constant::Str(name.into()))],
                ),
            ],
        );
        if let Statement::Assign {
            value:
                Rvalue::DynCallMethod {
                    captured: condition,
                    obj,
                    ..
                },
            ..
        } = &mut sample.f.body.blocks[exit].stmts[4]
        {
            *obj = get(captured);
            *condition = Some(get(flag));
        } else {
            panic!("fixture sink");
        }
        if mode == 3 {
            sample.f.body.blocks[exit].terminator = Terminator::Return(Some(get(captured)));
        }
        if mode == 4 {
            sample.f.body.blocks[exit].stmts.insert(
                1,
                Statement::Assign {
                    dest: captured,
                    value: Rvalue::Use(get(source)),
                },
            );
        }
        if mode == 5 {
            sample.f.body.blocks[exit].stmts.insert(
                4,
                Statement::Assign {
                    dest: flag,
                    value: Rvalue::Use(Operand::Const(Constant::Bool(false))),
                },
            );
        }
        if mode == 6 {
            sample.f.body.blocks[exit].terminator = Terminator::Branch {
                cond: get(flag),
                then_: exit,
                else_: exit,
            };
        }
        if mode == 7 {
            if let Statement::Assign {
                value: Rvalue::Call { args, .. },
                ..
            } = &mut sample.f.body.blocks[exit].stmts[3]
            {
                args[0] = get(source);
            }
        }
        FloatChain.run(&mut sample.f);
        assert_eq!(
            calls(&sample.f, RuntimeFn::NumericListCreate),
            usize::from(mode == 0),
            "case {mode}"
        );
        assert_eq!(
            calls(&sample.f, RuntimeFn::NumericListAppendF64),
            usize::from(mode == 0),
            "case {mode}"
        );
        // 原ordinary路径保捕获；未知/逃逸的pair也不被删掉冒充pure。
        assert_eq!(
            calls(&sample.f, RuntimeFn::CaptureModuleTarget),
            1,
            "case {mode}"
        );
    }
}
