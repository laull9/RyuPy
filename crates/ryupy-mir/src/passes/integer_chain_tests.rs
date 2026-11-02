//! I64短Add链的守卫/逐步溢出/物化与完整增量预算；只验证新增臂，不放宽原F64门禁。
use super::{FloatChain, MirPass};
use crate::mir::*;
use la_arena::{Arena, RawIdx};

fn local(f: &mut Function, ty: MirType) -> LocalId {
    f.body.locals.alloc(LocalDecl {
        ty,
        name: None,
        is_cell: false,
    })
}
fn add(dest: LocalId, lhs: LocalId, rhs: LocalId, inplace: bool) -> Statement {
    Statement::Assign {
        dest,
        value: Rvalue::DynBinary {
            op: DynOp::Add,
            lhs: Operand::Local(lhs),
            rhs: Operand::Local(rhs),
            inplace,
        },
    }
}
fn sample(steps: usize) -> (Function, LocalId) {
    let mut blocks = Arena::default();
    let entry = blocks.alloc(BasicBlock {
        stmts: Vec::new(),
        terminator: Terminator::Unreachable,
    });
    let mut f = Function {
        name: "integer_chain".into(),
        codegen_name: "integer_chain".into(),
        source_file: "chain.py".into(),
        params: Vec::new(),
        ret: MirType::Dynamic,
        body: Body {
            locals: Arena::default(),
            blocks,
            entry,
            regions: Vec::new(),
        },
        param_meta: Vec::new(),
        function_global: None,
        specialization_identity_global: None,
        defaults_global: None,
        signature_global: Some(GlobalId::from_raw(RawIdx::from(0))),
        env: None,
    };
    let inputs: Vec<_> = (0..=steps)
        .map(|_| local(&mut f, MirType::Dynamic))
        .collect();
    f.params = inputs.clone();
    let mut previous = inputs[0];
    for rhs in inputs.into_iter().skip(1) {
        let dest = local(&mut f, MirType::Dynamic);
        f.body.blocks[entry]
            .stmts
            .push(add(dest, previous, rhs, false));
        previous = dest;
    }
    f.body.blocks[entry].terminator = Terminator::Return(Some(Operand::Local(previous)));
    (f, previous)
}
fn count(f: &Function, predicate: impl Fn(&Rvalue) -> bool) -> usize {
    f.body
        .blocks
        .iter()
        .flat_map(|(_, b)| &b.stmts)
        .filter(|stmt| match stmt {
            Statement::Assign { value, .. } | Statement::Eval(value) => predicate(value),
            _ => false,
        })
        .count()
}
fn has_i64(f: &Function) -> usize {
    count(f, |v| matches!(v, Rvalue::IsBoxedI64 { .. }))
}
fn validate(f: Function) {
    let mut functions = Arena::default();
    let entry = functions.alloc(f);
    let mut globals = Arena::default();
    globals.alloc(GlobalDecl {
        name: "signature".into(),
        ty: MirType::Dynamic,
    });
    let program = Program {
        functions,
        classes: Arena::default(),
        globals,
        entry,
        source_name: "chain.py".into(),
    };
    assert!(
        crate::validate::validate(&program).is_ok(),
        "{:?}",
        crate::validate::validate(&program)
    );
}

#[test]
fn every_add_has_overflow_edge_and_only_final_integer_box_commits() {
    for steps in 2..=4 {
        let (mut f, final_dest) = sample(steps);
        FloatChain.run(&mut f);
        assert_eq!(has_i64(&f), steps + 1);
        assert_eq!(
            count(&f, |v| matches!(v, Rvalue::UnboxExactI64 { .. })),
            steps + 1
        );
        assert_eq!(count(&f, |v| matches!(v, Rvalue::DynBinary { .. })), steps);
        assert_eq!(
            count(&f, |v| matches!(
                v,
                Rvalue::Box {
                    from: MirType::I64,
                    ..
                }
            )),
            1
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
        let fallback = f
            .body
            .blocks
            .iter()
            .find(|(_, b)| {
                b.stmts.iter().any(|stmt| {
                    matches!(
                        stmt,
                        Statement::Assign {
                            value: Rvalue::DynBinary { .. },
                            ..
                        }
                    )
                })
            })
            .unwrap()
            .0;
        let integer_entry = f
            .body
            .blocks
            .iter()
            .find_map(|(_, b)| {
                if matches!(
                    b.stmts.first(),
                    Some(Statement::Assign {
                        value: Rvalue::IsBoxedF64 { .. },
                        ..
                    })
                ) {
                    if let Terminator::Branch { else_, .. } = b.terminator {
                        return Some(else_);
                    }
                }
                None
            })
            .unwrap();
        assert!(matches!(
            f.body.blocks[integer_entry].stmts.first(),
            Some(Statement::Assign {
                value: Rvalue::IsBoxedI64 { .. },
                ..
            })
        ));
        let mut additions = 0;
        for (_, block) in f.body.blocks.iter() {
            if matches!(
                block.stmts.first(),
                Some(Statement::Assign {
                    value: Rvalue::IsBoxedF64 { .. },
                    ..
                })
            ) {
                assert!(
                    matches!(block.terminator, Terminator::Branch { else_, .. } if else_ == integer_entry)
                );
            }
            if matches!(
                block.stmts.first(),
                Some(Statement::Assign {
                    value: Rvalue::IsBoxedI64 { .. },
                    ..
                })
            ) {
                assert!(
                    matches!(block.terminator, Terminator::Branch { else_, .. } if else_ == fallback)
                );
            }
            if matches!(
                block.stmts.first(),
                Some(Statement::Assign {
                    value: Rvalue::Binary {
                        op: BinOp::WrappingAdd,
                        ..
                    },
                    ..
                })
            ) {
                additions += 1;
                assert_eq!(block.stmts.len(), 5);
                for (stmt, op) in block.stmts.iter().zip([
                    BinOp::WrappingAdd,
                    BinOp::BitXor,
                    BinOp::BitXor,
                    BinOp::BitAnd,
                    BinOp::Ge,
                ]) {
                    assert!(
                        matches!(stmt, Statement::Assign { dest, value: Rvalue::Binary { op: actual, .. } } if *actual == op && *dest != final_dest)
                    );
                }
                assert!(
                    matches!(block.terminator, Terminator::Branch { else_, .. } if else_ == fallback)
                );
            }
        }
        assert_eq!(additions, steps);
        validate(f);
    }
}

#[test]
fn full_three_arm_budgets_accept_boundary_and_preserve_float_only_above_it() {
    // N=3,S=2：共新增27locals、13blocks、29statements，原两条动态Add仍保留。
    for dimension in 0..3 {
        for overflow in [false, true] {
            let (mut f, _) = sample(2);
            let extra = usize::from(overflow);
            match dimension {
                0 => {
                    while f.body.locals.len() < 485 + extra {
                        local(&mut f, MirType::Dynamic);
                    }
                }
                1 => {
                    while f.body.blocks.len() < 115 + extra {
                        f.body.blocks.alloc(BasicBlock {
                            stmts: Vec::new(),
                            terminator: Terminator::Unreachable,
                        });
                    }
                }
                _ => {
                    while f.body.blocks[f.body.entry].stmts.len() < 4067 + extra {
                        f.body.blocks[f.body.entry].stmts.push(Statement::Line(99));
                    }
                }
            }
            FloatChain.run(&mut f);
            assert_eq!(
                has_i64(&f),
                if overflow { 0 } else { 3 },
                "dimension={dimension}, overflow={overflow}"
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
            assert!(f.body.locals.len() <= 512);
            assert!(f.body.blocks.len() <= 128);
            assert!(
                f.body
                    .blocks
                    .iter()
                    .map(|(_, b)| b.stmts.len())
                    .sum::<usize>()
                    <= 4096
            );
            validate(f);
        }
    }
}

#[test]
fn only_four_successes_and_add_only_short_shapes_gain_integer_arm() {
    for case in 0..3 {
        let (mut f, _) = sample(if case == 0 { 5 } else { 2 });
        if case > 0 {
            if let Statement::Assign {
                value: Rvalue::DynBinary { op, .. },
                ..
            } = &mut f.body.blocks[f.body.entry].stmts[0]
            {
                *op = if case == 1 { DynOp::Sub } else { DynOp::Mul };
            }
        }
        FloatChain.run(&mut f);
        assert_eq!(has_i64(&f), 0);
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
    }
    let (mut f, _) = sample(2);
    let entry = f.body.entry;
    let a = f.params[0];
    let b = f.params[1];
    let c = f.params[2];
    for _ in 1..5 {
        let middle = local(&mut f, MirType::Dynamic);
        let result = local(&mut f, MirType::Dynamic);
        f.body.blocks[entry].stmts.extend([
            Statement::Line(7),
            add(middle, a, b, false),
            add(result, middle, c, true),
        ]);
    }
    FloatChain.run(&mut f);
    assert_eq!(has_i64(&f), 12);
    assert_eq!(
        count(&f, |v| matches!(
            v,
            Rvalue::Box {
                from: MirType::F64,
                ..
            }
        )),
        5
    );
    assert_eq!(
        count(&f, |v| matches!(v, Rvalue::DynBinary { inplace: true, .. })),
        4
    );
    validate(f);
}

#[test]
fn add_chain_observers_callbacks_and_unsupported_frames_keep_original_path() {
    for case in 0..11 {
        let (mut f, result) = sample(2);
        let entry = f.body.entry;
        let middle = match f.body.blocks[entry].stmts[0] {
            Statement::Assign { dest, .. } => dest,
            _ => unreachable!(),
        };
        match case {
            0 => f.body.locals[middle].name = Some("observed".into()),
            1 => f.body.blocks[entry].terminator = Terminator::Return(Some(Operand::Local(middle))),
            2 => f.body.blocks[entry].stmts.push(Statement::Assign {
                dest: middle,
                value: Rvalue::Use(Operand::Local(f.params[0])),
            }),
            3 => f.body.blocks[entry].stmts.insert(1, Statement::Line(8)),
            4 => f.body.blocks[entry].stmts.insert(
                1,
                Statement::Eval(Rvalue::Call {
                    callee: Callee::Runtime(RuntimeFn::DynStr),
                    args: vec![Operand::Local(middle)],
                }),
            ),
            5 => f.env = Some(f.params[0]),
            6 => f.body.locals[f.params[0]].is_cell = true,
            7 => f.body.regions.push(Region {
                kind: RegionKind::CatchAll,
                try_start: entry,
                try_len: 1,
                handler: entry,
                exc_local: None,
            }),
            8 => {
                f.body.blocks[entry].terminator = Terminator::Suspend {
                    value: Operand::Local(result),
                    resume: entry,
                    input: None,
                    delegated: false,
                }
            }
            9 => {
                if let Statement::Assign {
                    value: Rvalue::DynBinary { inplace, .. },
                    ..
                } = &mut f.body.blocks[entry].stmts[0]
                {
                    *inplace = true;
                }
            }
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
        assert_eq!(has_i64(&f), 0, "case {case}");
        assert_eq!(count(&f, |v| matches!(v, Rvalue::DynBinary { .. })), 2);
    }
}

#[test]
fn overflow_formula_is_wired_to_its_sum_and_matches_checked_add_boundaries() {
    let (mut f, _) = sample(2);
    FloatChain.run(&mut f);
    for (_, b) in f.body.blocks.iter() {
        if !matches!(
            b.stmts.first(),
            Some(Statement::Assign {
                value: Rvalue::Binary {
                    op: BinOp::WrappingAdd,
                    ..
                },
                ..
            })
        ) {
            continue;
        }
        let [
            Statement::Assign {
                dest: sum,
                value:
                    Rvalue::Binary {
                        op: BinOp::WrappingAdd,
                        lhs: Operand::Local(left),
                        rhs: Operand::Local(right),
                    },
            },
            Statement::Assign {
                dest: x,
                value:
                    Rvalue::Binary {
                        op: BinOp::BitXor,
                        lhs: Operand::Local(xleft),
                        rhs: Operand::Local(xsum),
                    },
            },
            Statement::Assign {
                dest: y,
                value:
                    Rvalue::Binary {
                        op: BinOp::BitXor,
                        lhs: Operand::Local(yright),
                        rhs: Operand::Local(ysum),
                    },
            },
            Statement::Assign {
                dest: bits,
                value:
                    Rvalue::Binary {
                        op: BinOp::BitAnd,
                        lhs: Operand::Local(and_left),
                        rhs: Operand::Local(and_right),
                    },
            },
            Statement::Assign {
                dest: safe,
                value:
                    Rvalue::Binary {
                        op: BinOp::Ge,
                        lhs: Operand::Local(compared),
                        rhs: Operand::Const(Constant::I64(0)),
                    },
            },
        ] = b.stmts.as_slice()
        else {
            panic!("overflow formula changed");
        };
        assert_eq!(
            (left, sum, right, sum, x, y, bits),
            (xleft, xsum, yright, ysum, and_left, and_right, compared)
        );
        assert!(
            matches!(b.terminator, Terminator::Branch { cond: Operand::Local(cond), .. } if cond == *safe)
        );
    }
    for a in [i64::MIN, i64::MIN + 1, -1, 0, 1, i64::MAX - 1, i64::MAX] {
        for b in [i64::MIN, i64::MIN + 1, -1, 0, 1, i64::MAX - 1, i64::MAX] {
            let sum = a.wrapping_add(b);
            let safe = ((a ^ sum) & (b ^ sum)) >= 0;
            assert_eq!(safe, a.checked_add(b).is_some(), "{a} + {b}");
        }
    }
}
