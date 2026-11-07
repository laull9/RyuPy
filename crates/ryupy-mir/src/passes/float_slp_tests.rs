//! SLP只打包两个独立新鲜结果：有序拓扑、尾部副作用、身份观察与标量fallback均锁定。
use super::float_slp::FloatSlp;
use super::{FloatChain, MirPass};
use crate::mir::*;
use la_arena::{Arena, RawIdx};
fn local(locals: &mut Arena<LocalDecl>) -> LocalId {
    locals.alloc(LocalDecl {
        ty: MirType::Dynamic,
        name: None,
        is_cell: false,
    })
}
fn binary(dest: LocalId, op: DynOp, lhs: LocalId, rhs: LocalId) -> Statement {
    Statement::Assign {
        dest,
        value: Rvalue::DynBinary {
            op,
            lhs: Operand::Local(lhs),
            rhs: Operand::Local(rhs),
            inplace: false,
        },
    }
}
fn sample() -> (Function, [LocalId; 11]) {
    let mut locals = Arena::default();
    let ids: Vec<_> = (0..11).map(|_| local(&mut locals)).collect();
    let [a, b, c, d, e, f, l0, l1, r0, r1, sink] = ids.try_into().unwrap();
    let mut blocks = Arena::default();
    let entry = blocks.alloc(BasicBlock {
        stmts: vec![
            binary(l0, DynOp::Mul, a, b),
            binary(l1, DynOp::Add, l0, c),
            binary(r0, DynOp::Mul, d, e),
            binary(r1, DynOp::Add, r0, f),
            Statement::Assign {
                dest: sink,
                value: Rvalue::CallWithArgs {
                    callee: RuntimeFn::MakeList,
                    prefix: vec![],
                    args: vec![Operand::Local(l1), Operand::Local(r1)],
                },
            },
        ],
        terminator: Terminator::Return(Some(Operand::Local(sink))),
    });
    (
        Function {
            module_state: None,
            instance_capabilities: Default::default(),
            module_state_source: None,
            name: "slp".into(),
            codegen_name: "slp".into(),
            source_file: "slp.py".into(),
            definition: None,
            params: vec![a, b, c, d, e, f],
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
            defaults_global: None,
            specialization_identity_global: None,
            signature_global: Some(GlobalId::from_raw(RawIdx::from(0))),
            env: None,
            dynamic_origin: None,
        },
        [a, b, c, d, e, f, l0, l1, r0, r1, sink],
    )
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
fn paired_chain_has_isa_guard_two_final_boxes_and_optimizable_scalar_fallback() {
    let (mut f, [_, _, _, _, _, _, _, left, _, right, _]) = sample();
    FloatSlp { scalar: false }.run(&mut f);
    assert_eq!(count(&f, |v| matches!(v, Rvalue::IsVector128Supported)), 1);
    assert_eq!(count(&f, |v| matches!(v, Rvalue::VectorPackF64 { .. })), 3);
    assert_eq!(
        count(&f, |v| matches!(v, Rvalue::VectorBinaryF64 { .. })),
        2
    );
    assert_eq!(count(&f, |v| matches!(v, Rvalue::VectorLaneF64 { .. })), 2);
    let fast = f
        .body
        .blocks
        .iter()
        .find(|(_, b)| {
            b.stmts.iter().any(|s| {
                matches!(
                    s,
                    Statement::Assign {
                        value: Rvalue::VectorLaneF64 { .. },
                        ..
                    }
                )
            })
        })
        .unwrap()
        .1;
    let finals: Vec<_> = fast
        .stmts
        .iter()
        .filter_map(|s| match s {
            Statement::Assign {
                dest,
                value: Rvalue::Box {
                    from: MirType::F64, ..
                },
            } => Some(*dest),
            _ => None,
        })
        .collect();
    assert_eq!(finals, [left, right]);
    FloatChain.run(&mut f);
    assert_eq!(
        count(&f, |v| matches!(v, Rvalue::VectorBinaryF64 { .. })),
        2
    );
    assert_eq!(
        count(&f, |v| matches!(
            v,
            Rvalue::Binary {
                op: BinOp::Add | BinOp::Mul,
                ..
            }
        )),
        4,
        "两个slow lane仍可各自走scalar F64"
    );
}
#[test]
fn unchanged_third_item_call_stays_after_both_lanes_and_before_container() {
    let (mut f, [a, ..]) = sample();
    let entry = f.body.entry;
    let tail = local(&mut f.body.locals);
    f.body.blocks[entry].stmts.insert(
        4,
        Statement::Assign {
            dest: tail,
            value: Rvalue::Call {
                callee: Callee::Runtime(RuntimeFn::DynStr),
                args: vec![Operand::Local(a)],
            },
        },
    );
    if let Statement::Assign {
        value: Rvalue::CallWithArgs { args, .. },
        ..
    } = &mut f.body.blocks[entry].stmts[5]
    {
        args.push(Operand::Local(tail));
    }
    FloatSlp { scalar: false }.run(&mut f);
    assert_eq!(
        count(&f, |v| matches!(v, Rvalue::VectorBinaryF64 { .. })),
        2
    );
    let tail_block = f
        .body
        .blocks
        .iter()
        .find(|(_, b)| {
            b.stmts
                .iter()
                .any(|s| matches!(s,Statement::Assign{dest,..}if *dest==tail))
        })
        .unwrap()
        .1;
    assert!(matches!(&tail_block.stmts[0],Statement::Assign{dest,..}if *dest==tail));
    assert!(matches!(
        &tail_block.stmts[1],
        Statement::Assign {
            value: Rvalue::CallWithArgs {
                callee: RuntimeFn::MakeList,
                ..
            },
            ..
        }
    ));
    assert_eq!(
        count(&f, |v| matches!(
            v,
            Rvalue::Call {
                callee: Callee::Runtime(RuntimeFn::DynStr),
                ..
            }
        )),
        1
    );
}
#[test]
fn shared_output_observation_and_cross_lane_dependency_are_rejected() {
    for case in 0..4 {
        let (mut f, [a, _, _, _, _, last, _, left, middle, right, _]) = sample();
        let entry = f.body.entry;
        match case {
            0 => {
                if let Statement::Assign {
                    value: Rvalue::CallWithArgs { args, .. },
                    ..
                } = &mut f.body.blocks[entry].stmts[4]
                {
                    args[1] = Operand::Local(left);
                }
            }
            1 => f.body.blocks[entry]
                .stmts
                .push(Statement::Eval(Rvalue::CallWithArgs {
                    callee: RuntimeFn::MakeTuple,
                    prefix: vec![],
                    args: vec![Operand::Array(vec![Operand::Local(left)])],
                })),
            2 => f.body.blocks[entry].stmts[3] = binary(right, DynOp::Add, left, last),
            _ => {
                f.body.locals[middle].name = Some("named".into());
                f.body.blocks[entry].stmts[2] = binary(middle, DynOp::Mul, a, last);
            }
        }
        FloatSlp { scalar: false }.run(&mut f);
        assert_eq!(
            count(&f, |v| matches!(v, Rvalue::IsVector128Supported)),
            0,
            "case {case}"
        );
    }
}
#[test]
fn ordered_dependency_topology_and_contiguous_regions_are_required() {
    for case in 0..4 {
        let (mut f, [a, _, _, d, _, last, _, _, middle, right, _]) = sample();
        let entry = f.body.entry;
        match case {
            0 => f.body.blocks[entry].stmts[3] = binary(right, DynOp::Add, last, middle),
            1 => f.body.blocks[entry].stmts.insert(2, Statement::Line(2)),
            2 => f.body.blocks[entry].stmts.insert(
                2,
                Statement::Eval(Rvalue::Call {
                    callee: Callee::Runtime(RuntimeFn::DynStr),
                    args: vec![Operand::Local(a)],
                }),
            ),
            _ => f.body.blocks[entry].stmts[3] = binary(right, DynOp::Sub, middle, d),
        }
        FloatSlp { scalar: false }.run(&mut f);
        assert_eq!(
            count(&f, |v| matches!(v, Rvalue::VectorBinaryF64 { .. })),
            0
        );
    }
}
#[test]
fn shared_readonly_external_keeps_distinct_fresh_results() {
    let (mut f, [a, b, c, _, _, _, _, _, middle, right, _]) = sample();
    let entry = f.body.entry;
    f.body.blocks[entry].stmts[2] = binary(middle, DynOp::Mul, a, b);
    f.body.blocks[entry].stmts[3] = binary(right, DynOp::Add, middle, c);
    FloatSlp { scalar: false }.run(&mut f);
    assert_eq!(count(&f, |v| matches!(v, Rvalue::IsBoxedF64 { .. })), 3);
    assert_eq!(
        count(&f, |v| matches!(
            v,
            Rvalue::Box {
                from: MirType::F64,
                ..
            }
        )),
        2
    );
}

#[test]
fn scalar_control_arm_keeps_identical_guards_boxes_and_original_lane_order() {
    let (mut f, [_, _, _, _, _, _, _, left, _, right, _]) = sample();
    FloatSlp { scalar: true }.run(&mut f);
    assert_eq!(count(&f, |v| matches!(v, Rvalue::IsVector128Supported)), 1);
    assert_eq!(count(&f, |v| matches!(v, Rvalue::IsBoxedF64 { .. })), 6);
    assert_eq!(
        count(&f, |v| matches!(
            v,
            Rvalue::VectorPackF64 { .. }
                | Rvalue::VectorBinaryF64 { .. }
                | Rvalue::VectorLaneF64 { .. }
        )),
        0
    );
    assert_eq!(
        count(&f, |v| matches!(
            v,
            Rvalue::Box {
                from: MirType::F64,
                ..
            }
        )),
        2
    );
    let fast = f
        .body
        .blocks
        .iter()
        .find(|(_, body)| {
            body.stmts.iter().any(|stmt| {
                matches!(
                    stmt,
                    Statement::Assign {
                        value: Rvalue::UnboxExactF64 { .. },
                        ..
                    }
                )
            })
        })
        .unwrap()
        .1;
    let ops: Vec<_> = fast
        .stmts
        .iter()
        .filter_map(|stmt| match stmt {
            Statement::Assign {
                value: Rvalue::Binary { op, .. },
                ..
            } => Some((*op, None)),
            Statement::Assign {
                dest,
                value: Rvalue::Box {
                    from: MirType::F64, ..
                },
            } => Some((BinOp::Eq, Some(*dest))),
            _ => None,
        })
        .collect();
    assert_eq!(
        ops,
        [
            (BinOp::Mul, None),
            (BinOp::Add, None),
            (BinOp::Eq, Some(left)),
            (BinOp::Mul, None),
            (BinOp::Add, None),
            (BinOp::Eq, Some(right))
        ]
    );
}

#[test]
fn rejected_pairs_consume_attempt_budget_and_success_budget_is_four() {
    for failed_prefix in [true, false] {
        let (mut f, ids) = sample();
        let [a, b, c, d, e, last, ..] = ids;
        let entry = f.body.entry;
        f.body.blocks[entry].stmts.clear();
        f.body.blocks[entry].terminator = Terminator::Return(None);
        let pairs = if failed_prefix { 17 } else { 5 };
        for index in 0..pairs {
            let l0 = local(&mut f.body.locals);
            let l1 = local(&mut f.body.locals);
            let r0 = local(&mut f.body.locals);
            let r1 = local(&mut f.body.locals);
            let sink = local(&mut f.body.locals);
            if failed_prefix && index < 16 {
                f.body.locals[l0].name = Some("observed".into());
            }
            f.body.blocks[entry].stmts.extend([
                binary(l0, DynOp::Mul, a, b),
                binary(l1, DynOp::Add, l0, c),
                binary(r0, DynOp::Mul, d, e),
                binary(r1, DynOp::Add, r0, last),
                Statement::Assign {
                    dest: sink,
                    value: Rvalue::CallWithArgs {
                        callee: RuntimeFn::MakeList,
                        prefix: vec![],
                        args: vec![Operand::Local(l1), Operand::Local(r1)],
                    },
                },
            ]);
        }
        FloatSlp { scalar: false }.run(&mut f);
        assert_eq!(
            count(&f, |v| matches!(v, Rvalue::IsVector128Supported)),
            if failed_prefix { 0 } else { 4 }
        );
        assert!(f.body.locals.len() <= 512 && f.body.blocks.len() <= 128);
    }
}

#[test]
fn no_hardware_path_jumps_to_original_scalar_operations_without_vector_instructions() {
    let (mut f, _) = sample();
    FloatSlp { scalar: false }.run(&mut f);
    let hardware = f
        .body
        .blocks
        .iter()
        .find(|(_, block)| {
            block.stmts.iter().any(|stmt| {
                matches!(
                    stmt,
                    Statement::Assign {
                        value: Rvalue::IsVector128Supported,
                        ..
                    }
                )
            })
        })
        .unwrap()
        .1;
    let Terminator::Branch { else_, .. } = hardware.terminator else {
        panic!("ISA必须显式分支")
    };
    let fallback = &f.body.blocks[else_];
    assert_eq!(
        fallback
            .stmts
            .iter()
            .filter(|stmt| matches!(
                stmt,
                Statement::Assign {
                    value: Rvalue::DynBinary { .. },
                    ..
                }
            ))
            .count(),
        4
    );
    assert!(!fallback.stmts.iter().any(|stmt| matches!(
        stmt,
        Statement::Assign {
            value: Rvalue::VectorPackF64 { .. }
                | Rvalue::VectorBinaryF64 { .. }
                | Rvalue::VectorLaneF64 { .. },
            ..
        }
    )));
}

#[test]
fn pipeline_keeps_both_slp_arms_explicit_and_scalar_takes_precedence() {
    assert!(
        super::default_pipeline()
            .iter()
            .all(|pass| pass.name() != "float_slp")
    );
    for direct in [false, true] {
        for leaf in [false, true] {
            assert!(
                super::pipeline_with_options(direct, leaf)
                    .iter()
                    .all(|pass| pass.name() != "float_slp")
            );
        }
    }
    for (scalar, packed) in [(false, true), (true, false), (true, true)] {
        let passes = super::pipeline_with_slp_options(false, false, scalar, packed);
        assert_eq!(
            passes
                .iter()
                .filter(|pass| pass.name() == "float_slp")
                .count(),
            1
        );
        let slp_index = passes
            .iter()
            .position(|pass| pass.name() == "float_slp")
            .unwrap();
        let chain_index = passes
            .iter()
            .position(|pass| pass.name() == "float_chain")
            .unwrap();
        assert!(slp_index < chain_index);
        let (mut function, _) = sample();
        // 单独执行流水线实际选择的SLP，排除后续FloatChain对控制臂结构的影响。
        if let super::PipelinePass::Function(pass) = &passes[slp_index] {
            pass.run(&mut function);
        } else {
            panic!("SLP必须为函数局部pass");
        }
        let vector_count = function
            .body
            .blocks
            .iter()
            .flat_map(|(_, block)| &block.stmts)
            .filter(|stmt| {
                matches!(
                    stmt,
                    Statement::Assign {
                        value: Rvalue::VectorBinaryF64 { .. },
                        ..
                    }
                )
            })
            .count();
        assert_eq!(vector_count, if scalar { 0 } else { 2 });
        assert!(
            function
                .body
                .blocks
                .iter()
                .flat_map(|(_, block)| &block.stmts)
                .any(|stmt| matches!(
                    stmt,
                    Statement::Assign {
                        value: Rvalue::IsVector128Supported,
                        ..
                    }
                ))
        );
    }
}
