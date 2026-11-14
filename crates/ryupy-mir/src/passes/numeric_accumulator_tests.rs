//! 累加器只改写入口支配、没有观察或逃逸的唯一自然循环。
use super::{MirPass, numeric_accumulator::DirectNumericAccumulator};
use crate::mir::*;
use la_arena::{Arena, RawIdx};

fn sample() -> (Function, [BlockId; 5], [LocalId; 3]) {
    let mut locals = Arena::default();
    let total = locals.alloc(LocalDecl {
        ty: MirType::Dynamic,
        name: Some("total".into()),
        is_cell: false,
    });
    let temp = locals.alloc(LocalDecl {
        ty: MirType::Dynamic,
        name: None,
        is_cell: false,
    });
    let rhs = locals.alloc(LocalDecl {
        ty: MirType::Dynamic,
        name: Some("rhs".into()),
        is_cell: false,
    });
    let mut blocks = Arena::default();
    let ids: Vec<_> = (0..5)
        .map(|_| {
            blocks.alloc(BasicBlock {
                stmts: vec![],
                terminator: Terminator::Unreachable,
            })
        })
        .collect();
    let [entry, header, body, latch, exit] = ids.try_into().unwrap();
    blocks[entry].stmts.push(Statement::Assign {
        dest: total,
        value: Rvalue::Use(Operand::Local(rhs)),
    });
    blocks[entry].terminator = Terminator::Goto(header);
    blocks[header].terminator = Terminator::Branch {
        cond: Operand::Const(Constant::Bool(true)),
        then_: body,
        else_: exit,
    };
    blocks[body].stmts = vec![
        Statement::Assign {
            dest: temp,
            value: Rvalue::DynBinary {
                op: DynOp::Add,
                lhs: Operand::Local(total),
                rhs: Operand::Local(rhs),
                inplace: true,
            },
        },
        Statement::Assign {
            dest: total,
            value: Rvalue::Use(Operand::Local(temp)),
        },
    ];
    blocks[body].terminator = Terminator::Goto(latch);
    blocks[latch].terminator = Terminator::Goto(header);
    blocks[exit].terminator = Terminator::Return(Some(Operand::Local(total)));
    (
        Function {
            module_state: None,
            instance_capabilities: Default::default(),
            module_state_source: None,
            name: "sum".into(),
            codegen_name: "sum".into(),
            source_file: "sum.py".into(),
            definition: None,
            params: vec![rhs],
            ret: MirType::Dynamic,
            body: Body {
                direct_globals_contracts: Vec::new(),
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
            signature_global: Some(GlobalId::from_raw(RawIdx::from(0))),
            env: None,
            dynamic_origin: None,
        },
        [entry, header, body, latch, exit],
        [total, temp, rhs],
    )
}
fn transformed(function: &Function) -> bool {
    function.body.blocks.iter().any(|(_, b)| {
        b.stmts.iter().any(|s| {
            matches!(
                s,
                Statement::Assign {
                    value: Rvalue::Binary {
                        op: BinOp::WrappingAdd,
                        ..
                    },
                    ..
                }
            )
        })
    })
}
#[test]
fn strict_loop_materializes_only_at_unique_exit() {
    let (mut f, [entry, _, body, _, exit], [total, _, _]) = sample();
    DirectNumericAccumulator.run(&mut f);
    assert!(transformed(&f));
    assert!(matches!(
        f.body.blocks[entry].terminator,
        Terminator::Branch { .. }
    ));
    assert!(matches!(
        f.body.blocks[body].terminator,
        Terminator::Branch { .. }
    ));
    assert!(matches!(
        f.body.blocks[exit].terminator,
        Terminator::Branch { .. }
    ));
    let [
        Statement::Assign {
            dest: empty,
            value:
                Rvalue::Binary {
                    op: BinOp::Eq,
                    lhs: Operand::Local(subject),
                    rhs: Operand::Const(Constant::Null),
                },
        },
        Statement::Assign {
            dest: needed,
            value:
                Rvalue::Binary {
                    op: BinOp::BitAnd,
                    lhs: Operand::Local(mode),
                    rhs: Operand::Local(empty_read),
                },
        },
    ] = f.body.blocks[exit].stmts.as_slice()
    else {
        panic!("物化必须同时检查整数模式和CLR null");
    };
    assert_eq!(*subject, total);
    assert_eq!(empty, empty_read);
    assert_eq!(f.body.locals[*mode].ty, MirType::Bool);
    assert!(
        matches!(f.body.blocks[exit].terminator, Terminator::Branch { cond: Operand::Local(cond), .. } if cond == *needed)
    );
    assert!(f.body.blocks.iter().any(|(_, block)| block.stmts.iter().any(|stmt| matches!(stmt, Statement::Assign { dest, value: Rvalue::IsBoxedI64 { value: Operand::Local(source) } } if dest == mode && *source == total))));
    let mut boxed_total = 0;
    for (_, block) in f.body.blocks.iter() {
        for stmt in &block.stmts {
            if matches!(stmt, Statement::Assign { dest, value: Rvalue::Box { .. } } if *dest == total)
            {
                boxed_total += 1;
            }
            assert!(!matches!(
                stmt,
                Statement::Assign {
                    value: Rvalue::Call {
                        callee: Callee::Runtime(
                            RuntimeFn::NumericAccumulatorCreate
                                | RuntimeFn::NumericAccumulatorAdd
                                | RuntimeFn::NumericAccumulatorValue
                        ),
                        ..
                    },
                    ..
                } | Statement::Eval(Rvalue::Call {
                    callee: Callee::Runtime(RuntimeFn::NumericAccumulatorAdd),
                    ..
                })
            ));
        }
    }
    assert_eq!(boxed_total, 2, "只有慢路入口和唯一出口可物化累加盒");
    let mut functions = Arena::default();
    let entry = functions.alloc(f);
    let program = Program {
        functions,
        classes: Arena::default(),
        globals: Arena::default(),
        entry,
        source_name: "sum.py".into(),
    };
    assert!(
        crate::validate::validate(&program).is_ok(),
        "{:?}",
        crate::validate::validate(&program)
    );
}
#[test]
fn observations_rhs_alias_cell_exception_and_generator_reject() {
    for variant in 0..7 {
        let (mut f, [entry, header, body, _, _], [total, temp, _]) = sample();
        match variant {
            0 => f.body.blocks[header]
                .stmts
                .push(Statement::Eval(Rvalue::Use(Operand::Local(total)))),
            1 => {
                if let Statement::Assign {
                    value: Rvalue::DynBinary { rhs, .. },
                    ..
                } = &mut f.body.blocks[body].stmts[0]
                {
                    *rhs = Operand::Local(total);
                }
            }
            2 => f.body.locals[total].is_cell = true,
            3 => f.body.regions.push(Region {
                source_handler_chain: None,
                kind: RegionKind::CatchAll,
                try_start: entry,
                try_len: 1,
                handler: body,
                exc_local: None,
            }),
            4 => f.signature_global = None,
            5 => f.body.blocks[header]
                .stmts
                .push(Statement::Eval(Rvalue::Use(Operand::Local(temp)))),
            6 => f.body.blocks[entry].stmts.clear(),
            _ => unreachable!(),
        }
        DirectNumericAccumulator.run(&mut f);
        assert!(!transformed(&f), "拒绝边界 {variant}");
    }
}
#[test]
fn nonentry_preheader_entry_backedge_and_exit_bypass_reject() {
    for variant in 0..3 {
        let (mut f, [entry, _, _, _, exit], _) = sample();
        let extra = f.body.blocks.alloc(BasicBlock {
            stmts: vec![],
            terminator: Terminator::Goto(entry),
        });
        match variant {
            0 => f.body.entry = extra,
            1 => {} // 不可达前驱也保守拒绝，入口不得重建累加状态。
            2 => f.body.blocks[extra].terminator = Terminator::Goto(exit),
            _ => unreachable!(),
        }
        DirectNumericAccumulator.run(&mut f);
        assert!(!transformed(&f), "CFG边界 {variant}");
    }
}

#[test]
fn default_pipeline_retains_holder_and_explicit_option_selects_direct() {
    // 后续可以增加其它Pass，但两种累加展开必须互斥且恰好运行一次。
    for experimental in [false, true] {
        let pipeline = super::pipeline_with_direct_accumulator(experimental);
        let count = |name| pipeline.iter().filter(|pass| pass.name() == name).count();
        assert_eq!(count("numeric_accumulator"), usize::from(!experimental));
        assert_eq!(
            count("direct_numeric_accumulator_experimental"),
            usize::from(experimental)
        );
    }
    assert!(
        super::default_pipeline()
            .iter()
            .any(|pass| pass.name() == "numeric_accumulator")
    );
    let (mut f, _, _) = sample();
    super::NumericAccumulator.run(&mut f);
    assert!(!transformed(&f));
    assert!(
        f.body
            .blocks
            .iter()
            .any(|(_, b)| b.stmts.iter().any(|s| matches!(
                s,
                Statement::Eval(Rvalue::Call {
                    callee: Callee::Runtime(RuntimeFn::NumericAccumulatorAdd),
                    ..
                })
            )))
    );
}

#[test]
fn any_function_locals_abi_rejects_both_representations() {
    // 即使不在循环内或没有显式total实参，也拒绝隐式局部观察协议。
    for runtime in [
        RuntimeFn::FunctionLocalsValidate,
        RuntimeFn::FunctionLocalsEnsure,
        RuntimeFn::FunctionLocalsSync,
    ] {
        for assign in [false, true] {
            let (mut f, [entry, _, _, _, _], [_, temp, _]) = sample();
            let call = Rvalue::Call {
                callee: Callee::Runtime(runtime),
                args: vec![],
            };
            f.body.blocks[entry].stmts.push(if assign {
                Statement::Assign {
                    dest: temp,
                    value: call,
                }
            } else {
                Statement::Eval(call)
            });
            let original = format!("{f:?}");
            super::NumericAccumulator.run(&mut f);
            assert_eq!(format!("{f:?}"), original);
            DirectNumericAccumulator.run(&mut f);
            assert_eq!(format!("{f:?}"), original);
        }
    }
}

#[test]
fn source_pipeline_string_holder_acceptance_and_observer_rejection() {
    fn count(source: &str) -> usize {
        let mut db = ryupy_base::SourceDb::default();
        let file = db.add("accumulator.py", source);
        let (parsed, diagnostics) = ryupy_syntax::parse(file, source);
        assert!(diagnostics.is_empty(), "{diagnostics:?}");
        let (hir, diagnostics) =
            ryupy_hir::lower_module(&parsed, &ryupy_hir::ModuleInfo::default(), source);
        assert!(diagnostics.is_empty(), "{diagnostics:?}");
        let (types, diagnostics) = ryupy_ty::check_module(&hir);
        assert!(diagnostics.is_empty(), "{diagnostics:?}");
        let (mut program, _, diagnostics) = crate::build_program(&hir, &types);
        assert!(diagnostics.is_empty(), "{diagnostics:?}");
        super::run_pipeline(&mut program, &super::default_pipeline()).unwrap();
        program
            .functions
            .iter()
            .flat_map(|(_, f)| f.body.blocks.iter())
            .flat_map(|(_, b)| &b.stmts)
            .filter(|stmt| {
                matches!(
                    stmt,
                    Statement::Eval(Rvalue::Call {
                        callee: Callee::Runtime(RuntimeFn::NumericAccumulatorAdd),
                        ..
                    })
                )
            })
            .count()
    }
    assert_eq!(
        count(
            "def f(initial, n, piece):\n    total = initial\n    for i in range(n):\n        total += piece\n    return total\nprint(f('', 3, 'ab'))\n"
        ),
        1
    );
    // 新模块槽字符串 Pass 接纳这个无观察正例，原局部 holder 的拒绝边界继续逐项检查。
    assert_eq!(
        count("total = ''\nfor i in range(3):\n    total += 'ab'\nprint(total)\n"),
        1
    );
    for source in [
        "total = ''\nfor i in range(3):\n    print(total)\n    total += 'ab'\nprint(total)\n",
        "def f(initial, n):\n    total = initial\n    for i in range(n):\n        total += total\n    return total\n",
        "def f(initial, n):\n    total = initial\n    for i in range(n):\n        print(locals())\n        total += 'ab'\n    return total\n",
        "def f(initial, n):\n    total = initial\n    for i in range(n):\n        print(total)\n        total += 'ab'\n    return total\n",
        "def f(initial, n):\n    total = initial\n    for i in range(n):\n        if i == 2:\n            return total\n        total += 'ab'\n    return total\n",
        "def f(initial, n):\n    total = initial\n    try:\n        for i in range(n):\n            total += 'ab'\n    except TypeError:\n        return total\n    return total\n",
        "def f(initial, n):\n    total = initial\n    for i in range(n):\n        yield total\n        total += 'ab'\n",
        "def f(initial, n):\n    total = initial\n    def read():\n        return total\n    for i in range(n):\n        print(read())\n        total += 'ab'\n    return total\n",
    ] {
        assert_eq!(count(source), 0, "观察/逃逸路径不得进入holder: {source}");
    }
}
