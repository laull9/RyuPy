//! tuple 标量替换的正反结构测试：冻结引用、定义支配、逃逸与失败时序。
use super::{MirPass, TupleScalar};
use crate::mir::*;
use la_arena::{Arena, RawIdx};

fn sample(index: i64) -> (Function, [LocalId; 6]) {
    let mut locals = Arena::default();
    let ids: Vec<_> = (0..6)
        .map(|_| {
            locals.alloc(LocalDecl {
                ty: MirType::Dynamic,
                name: None,
                is_cell: false,
            })
        })
        .collect();
    let [left, right, tuple, key, result, length] = ids.try_into().unwrap();
    locals[length].ty = MirType::I64;
    let mut blocks = Arena::default();
    let entry = blocks.alloc(BasicBlock {
        stmts: vec![
            Statement::Assign {
                dest: tuple,
                value: Rvalue::CallWithArgs {
                    callee: RuntimeFn::MakeTuple,
                    prefix: vec![],
                    args: vec![Operand::Local(left), Operand::Local(right)],
                },
            },
            Statement::Assign {
                dest: length,
                value: Rvalue::Call {
                    callee: Callee::Runtime(RuntimeFn::Len),
                    args: vec![Operand::Local(tuple)],
                },
            },
            check(length, 2),
            Statement::Assign {
                dest: key,
                value: Rvalue::Box {
                    value: Operand::Const(Constant::I64(index)),
                    from: MirType::I64,
                },
            },
            Statement::Assign {
                dest: result,
                value: Rvalue::Call {
                    callee: Callee::Runtime(RuntimeFn::GetItem),
                    args: vec![Operand::Local(tuple), Operand::Local(key)],
                },
            },
        ],
        terminator: Terminator::Return(Some(Operand::Local(result))),
    });
    (
        Function {
            module_state: None,
            instance_capabilities: Default::default(),
            module_state_source: None,
            name: "tuple_probe".into(),
            codegen_name: "tuple_probe".into(),
            source_file: "tuple_probe.py".into(),
            definition: None,
            params: vec![left, right],
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
        [left, right, tuple, key, result, length],
    )
}
fn check(length: LocalId, required: i64) -> Statement {
    Statement::Eval(Rvalue::Call {
        callee: Callee::Runtime(RuntimeFn::UnpackCheck),
        args: vec![
            Operand::Local(length),
            Operand::Const(Constant::I64(required)),
            Operand::Const(Constant::Bool(false)),
        ],
    })
}
fn keeps_tuple(function: &Function) -> bool {
    function.body.blocks.iter().any(|(_, b)| {
        b.stmts.iter().any(|s| {
            matches!(
                s,
                Statement::Assign {
                    value: Rvalue::CallWithArgs {
                        callee: RuntimeFn::MakeTuple,
                        ..
                    },
                    ..
                }
            )
        })
    })
}
#[test]
fn freezes_all_elements_before_rebinding_and_selects_negative_index() {
    let (mut function, [left, right, _, _, result, _]) = sample(-1);
    let block = function.body.entry;
    function.body.blocks[block].stmts.insert(
        1,
        Statement::Assign {
            dest: right,
            value: Rvalue::Use(Operand::Local(left)),
        },
    );
    TupleScalar.run(&mut function);
    assert!(!keeps_tuple(&function));
    let statements = &function.body.blocks[block].stmts;
    let Statement::Assign {
        dest: first,
        value: Rvalue::Use(Operand::Local(source)),
    } = &statements[0]
    else {
        panic!("缺少第一元素快照")
    };
    assert_eq!(*source, left);
    let Statement::Assign {
        dest: second,
        value: Rvalue::Use(Operand::Local(source)),
    } = &statements[1]
    else {
        panic!("缺少第二元素快照")
    };
    assert_eq!(*source, right);
    assert_ne!(first, second);
    assert!(![left, right].contains(first) && ![left, right].contains(second));
    assert!(statements.iter().any(|s| matches!(s, Statement::Assign { dest, value: Rvalue::Use(Operand::Local(source)) } if *dest == result && source == second)));
    assert!(!statements.iter().any(|s| matches!(
        s,
        Statement::Eval(Rvalue::Call {
            callee: Callee::Runtime(RuntimeFn::UnpackCheck),
            ..
        })
    )));
}
#[test]
fn failed_unpack_check_stays_in_place_after_constant_length() {
    let (mut function, [_, _, _, _, _, length]) = sample(0);
    let block = function.body.entry;
    function.body.blocks[block].stmts[2] = check(length, 3);
    TupleScalar.run(&mut function);
    assert!(!keeps_tuple(&function));
    let statements = &function.body.blocks[block].stmts;
    assert!(
        matches!(&statements[2], Statement::Assign { dest, value: Rvalue::Use(Operand::Const(Constant::I64(2))) } if *dest == length)
    );
    assert!(matches!(
        &statements[3],
        Statement::Eval(Rvalue::Call {
            callee: Callee::Runtime(RuntimeFn::UnpackCheck),
            ..
        })
    ));
}
#[test]
fn identity_alias_call_nested_array_and_return_escape_preserve_tuple() {
    for case in 0..5 {
        let (mut function, [left, _, tuple, _, result, _]) = sample(0);
        let block = function.body.entry;
        let observation = match case {
            0 => Statement::Assign {
                dest: left,
                value: Rvalue::Use(Operand::Local(tuple)),
            },
            1 => Statement::Eval(Rvalue::Call {
                callee: Callee::Runtime(RuntimeFn::IsSame),
                args: vec![Operand::Local(tuple), Operand::Local(tuple)],
            }),
            2 => Statement::Eval(Rvalue::CallWithArgs {
                callee: RuntimeFn::MakeList,
                prefix: vec![],
                args: vec![Operand::Array(vec![Operand::Local(tuple)])],
            }),
            3 => Statement::Eval(Rvalue::Call {
                callee: Callee::Runtime(RuntimeFn::WriteObject),
                args: vec![Operand::Local(left), Operand::Local(tuple)],
            }),
            _ => {
                function.body.blocks[block].terminator =
                    Terminator::Return(Some(Operand::Local(tuple)));
                Statement::Assign {
                    dest: result,
                    value: Rvalue::Use(Operand::Local(left)),
                }
            }
        };
        function.body.blocks[block].stmts.push(observation);
        TupleScalar.run(&mut function);
        assert!(keeps_tuple(&function), "错误消除逃逸候选 {case}");
    }
}
#[test]
fn unknown_out_of_bounds_late_and_reassigned_indices_preserve_tuple() {
    for case in 0..5 {
        let (mut function, [left, _, _, key, _, _]) = sample(if case == 0 { 9 } else { 0 });
        let block = function.body.entry;
        match case {
            1 => function.body.blocks[block].stmts.swap(3, 4),
            2 => function.body.blocks[block].stmts.push(Statement::Assign {
                dest: key,
                value: Rvalue::Use(Operand::Local(left)),
            }),
            3 => {
                function.body.blocks[block].stmts[3] = Statement::Assign {
                    dest: key,
                    value: Rvalue::Use(Operand::Local(left)),
                }
            }
            4 => {
                let definition = function.body.blocks[block].stmts.remove(3);
                function.body.blocks.alloc(BasicBlock {
                    stmts: vec![definition],
                    terminator: Terminator::Return(None),
                });
            }
            _ => {}
        }
        TupleScalar.run(&mut function);
        assert!(keeps_tuple(&function), "错误接受下标事实 {case}");
    }
}
#[test]
fn cells_generators_closures_and_exception_regions_are_outside_first_slice() {
    for case in 0..5 {
        let (mut function, [left, _, tuple, _, _, _]) = sample(0);
        let entry = function.body.entry;
        match case {
            0 => function.body.locals[left].is_cell = true,
            1 => function.signature_global = None,
            2 => function.env = Some(left),
            3 => {
                function.body.blocks[entry].terminator = Terminator::Suspend {
                    value: Operand::Local(tuple),
                    resume: entry,
                    input: None,
                    delegated: false,
                }
            }
            _ => function.body.regions.push(Region {
                source_handler_chain: None,
                kind: RegionKind::CatchAll,
                try_start: entry,
                try_len: 1,
                handler: entry,
                exc_local: None,
            }),
        }
        TupleScalar.run(&mut function);
        assert!(keeps_tuple(&function));
    }
}
#[test]
fn oversized_tuple_and_cross_block_read_are_not_rewritten() {
    for case in 0..2 {
        let (mut function, [left, _, _, _, _, _]) = sample(0);
        let entry = function.body.entry;
        if case == 0 {
            if let Statement::Assign {
                value: Rvalue::CallWithArgs { args, .. },
                ..
            } = &mut function.body.blocks[entry].stmts[0]
            {
                *args = vec![Operand::Local(left); 9];
            }
        } else {
            let read = function.body.blocks[entry].stmts.pop().unwrap();
            let next = function.body.blocks.alloc(BasicBlock {
                stmts: vec![read],
                terminator: Terminator::Return(None),
            });
            function.body.blocks[entry].terminator = Terminator::Goto(next);
        }
        TupleScalar.run(&mut function);
        assert!(keeps_tuple(&function));
    }
}

#[test]
fn attempt_budget_counts_failed_candidates_and_never_revisits_them() {
    for mixed in [false, true] {
        let (mut function, [left, right, ..]) = sample(0);
        let entry = function.body.entry;
        function.body.blocks[entry].stmts.clear();
        function.body.blocks[entry].terminator = Terminator::Return(None);
        let mut last_tuple = None;
        for index in 0..33 {
            let mut alloc = || {
                function.body.locals.alloc(LocalDecl {
                    ty: MirType::Dynamic,
                    name: None,
                    is_cell: false,
                })
            };
            let tuple = alloc();
            let key = alloc();
            let result = alloc();
            let statements = &mut function.body.blocks[entry].stmts;
            statements.push(Statement::Assign {
                dest: tuple,
                value: Rvalue::CallWithArgs {
                    callee: RuntimeFn::MakeTuple,
                    prefix: vec![],
                    args: vec![Operand::Local(left), Operand::Local(right)],
                },
            });
            statements.push(Statement::Assign {
                dest: key,
                value: Rvalue::Box {
                    value: Operand::Const(Constant::I64(0)),
                    from: MirType::I64,
                },
            });
            statements.push(Statement::Assign {
                dest: result,
                value: Rvalue::Call {
                    callee: Callee::Runtime(RuntimeFn::GetItem),
                    args: vec![Operand::Local(tuple), Operand::Local(key)],
                },
            });
            // 第 33 个始终合法，但预算已用尽；前 32 个或全部失败，或交替成功。
            if index < 32 && (!mixed || index % 2 == 0) {
                statements.push(Statement::Eval(Rvalue::Call {
                    callee: Callee::Runtime(RuntimeFn::IsSame),
                    args: vec![Operand::Local(tuple), Operand::Local(tuple)],
                }));
            }
            last_tuple = Some(tuple);
        }
        TupleScalar.run(&mut function);
        let remaining: Vec<_> = function.body.blocks[entry]
            .stmts
            .iter()
            .filter_map(|stmt| match stmt {
                Statement::Assign {
                    dest,
                    value:
                        Rvalue::CallWithArgs {
                            callee: RuntimeFn::MakeTuple,
                            ..
                        },
                } => Some(*dest),
                _ => None,
            })
            .collect();
        assert_eq!(remaining.len(), if mixed { 17 } else { 33 });
        assert!(
            remaining.contains(&last_tuple.unwrap()),
            "不能越过总尝试预算"
        );
    }
}

#[test]
fn repeated_lengths_only_remove_checks_after_their_own_unique_definition() {
    let (mut function, [_, _, tuple, _, _, _]) = sample(0);
    let entry = function.body.entry;
    let repeated = function.body.locals.alloc(LocalDecl {
        ty: MirType::I64,
        name: None,
        is_cell: false,
    });
    // 参数模拟已存在但未知的长度；前置 check 绝不能用后面的 Len 事实删除。
    function.params.push(repeated);
    function.body.blocks[entry].stmts.extend([
        check(repeated, 2),
        Statement::Assign {
            dest: repeated,
            value: Rvalue::Call {
                callee: Callee::Runtime(RuntimeFn::Len),
                args: vec![Operand::Local(tuple)],
            },
        },
        check(repeated, 2),
        check(repeated, 3),
    ]);
    TupleScalar.run(&mut function);
    assert!(!keeps_tuple(&function));
    let checks = function.body.blocks[entry]
        .stmts
        .iter()
        .filter(|stmt| {
            matches!(
                stmt,
                Statement::Eval(Rvalue::Call {
                    callee: Callee::Runtime(RuntimeFn::UnpackCheck),
                    ..
                })
            )
        })
        .count();
    assert_eq!(checks, 2, "保留前置未知检查和后置失败检查");
}
