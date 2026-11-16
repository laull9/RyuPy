//! 双局部来源、合流、回边及数组入口的负例；不将普通Bool伪装成已捕获调用。
use crate::mir::*;
fn local(f: &mut Function, ty: MirType) -> LocalId {
    f.body.locals.alloc(LocalDecl {
        ty,
        name: None,
        is_cell: false,
    })
}
fn call(dest: LocalId, runtime: RuntimeFn, args: Vec<Operand>) -> Statement {
    Statement::Assign {
        dest,
        value: Rvalue::Call {
            callee: Callee::Runtime(runtime),
            args,
        },
    }
}
fn get(id: LocalId) -> Operand {
    Operand::Local(id)
}
fn fixture(array: bool) -> (Program, [LocalId; 4]) {
    let mut p = crate::module_capture_tests::program(Statement::Line(1));
    let f = &mut p.functions[p.entry];
    let r = local(f, MirType::Dynamic);
    let m = local(f, MirType::Bool);
    let v = local(f, MirType::Dynamic);
    let result = local(f, MirType::Dynamic);
    let name = Operand::Const(Constant::Str("f".into()));
    let invoke = if array {
        call(
            result,
            RuntimeFn::CallCapturedMethod,
            vec![
                get(m),
                get(v),
                name.clone(),
                Operand::Array(vec![]),
                Operand::Array(vec![]),
                Operand::Array(vec![]),
            ],
        )
    } else {
        Statement::Assign {
            dest: result,
            value: Rvalue::DynCallMethod {
                captured: Some(get(m)),
                obj: get(v),
                name: "f".into(),
                args: vec![],
            },
        }
    };
    f.body.blocks[f.body.entry].stmts = vec![
        Statement::Assign {
            dest: r,
            value: Rvalue::Use(Operand::Const(Constant::Null)),
        },
        call(m, RuntimeFn::IsModule, vec![get(r)]),
        call(v, RuntimeFn::CaptureModuleTarget, vec![get(r), name]),
        invoke,
    ];
    (p, [r, m, v, result])
}
#[test]
fn module_pair_all_consumers_require_unmodified_matching_origin() {
    for array in [false, true] {
        for mode in 0..9 {
            let (mut p, [r, m, v, _]) = fixture(array);
            let f = &mut p.functions[p.entry];
            let b = &mut f.body.blocks[f.body.entry];
            match mode {
                0 => {}
                1 => {
                    b.stmts.insert(
                        3,
                        Statement::Assign {
                            dest: r,
                            value: Rvalue::Use(Operand::Const(Constant::Null)),
                        },
                    );
                }
                2 => {
                    b.stmts.insert(
                        3,
                        Statement::Assign {
                            dest: m,
                            value: Rvalue::Use(Operand::Const(Constant::Bool(true))),
                        },
                    );
                }
                3 => {
                    b.stmts.insert(
                        3,
                        Statement::Assign {
                            dest: v,
                            value: Rvalue::Use(Operand::Const(Constant::None)),
                        },
                    );
                }
                4 => {
                    if let Statement::Assign {
                        value: Rvalue::Call { args, .. },
                        ..
                    } = &mut b.stmts[2]
                    {
                        args[1] = Operand::Const(Constant::Str("other".into()));
                    }
                }
                5 => {
                    b.stmts.remove(0);
                }
                6 => {
                    b.stmts.swap(1, 2);
                }
                7 => {
                    if let Statement::Assign {
                        value: Rvalue::Call { args, .. },
                        ..
                    } = &mut b.stmts[2]
                    {
                        args[0] = get(v);
                    }
                }
                8 => {
                    f.body.locals[r].is_cell = true;
                }
                _ => unreachable!(),
            }
            assert_eq!(
                crate::validate::validate(&p).is_ok(),
                mode == 0,
                "array={array} case={mode}"
            );
        }
    }
}
#[test]
fn module_pair_diamond_and_loop_write_do_not_restore_old_flag() {
    for array in [false, true] {
        for kills in [false, true] {
            for looping in [false, true] {
                let (mut p, [r, _, _, _]) = fixture(array);
                let f = &mut p.functions[p.entry];
                let entry = f.body.entry;
                let invoke = f.body.blocks[entry].stmts.pop().unwrap();
                let left = f.body.blocks.alloc(BasicBlock {
                    stmts: if kills {
                        vec![Statement::Assign {
                            dest: r,
                            value: Rvalue::Use(Operand::Const(Constant::Null)),
                        }]
                    } else {
                        vec![]
                    },
                    terminator: Terminator::Unreachable,
                });
                let right = f.body.blocks.alloc(BasicBlock {
                    stmts: vec![],
                    terminator: Terminator::Unreachable,
                });
                let join = f.body.blocks.alloc(BasicBlock {
                    stmts: vec![invoke],
                    terminator: Terminator::Return(None),
                });
                f.body.blocks[entry].terminator = Terminator::Branch {
                    cond: Operand::Const(Constant::Bool(true)),
                    then_: if looping { join } else { left },
                    else_: right,
                };
                f.body.blocks[left].terminator = Terminator::Goto(join);
                f.body.blocks[right].terminator = Terminator::Goto(join);
                if looping {
                    f.body.blocks[join].terminator = Terminator::Goto(left);
                }
                assert_eq!(
                    crate::validate::validate(&p).is_ok(),
                    !kills,
                    "array={array} loop={looping}"
                );
            }
        }
    }
}
#[test]
fn module_pair_reload_then_rebuild_and_effectful_calls_preserve_valid_pair() {
    let (mut p, [r, m, v, _]) = fixture(false);
    let f = &mut p.functions[p.entry];
    // 模拟恢复入口先写槽，随后本次调用重新建立完整pair；不强求全函数唯一def。
    f.body.blocks[f.body.entry].stmts.splice(
        0..0,
        [
            Statement::Assign {
                dest: r,
                value: Rvalue::Use(Operand::Const(Constant::None)),
            },
            Statement::Assign {
                dest: m,
                value: Rvalue::Use(Operand::Const(Constant::Bool(false))),
            },
            Statement::Assign {
                dest: v,
                value: Rvalue::Use(Operand::Const(Constant::None)),
            },
        ],
    );
    f.body.blocks[f.body.entry].stmts.insert(
        6,
        Statement::Eval(Rvalue::Call {
            callee: Callee::Runtime(RuntimeFn::TracePopTo),
            args: vec![Operand::Const(Constant::I64(0))],
        }),
    );
    assert!(crate::validate::validate(&p).is_ok());
    use crate::passes::MirPass;
    crate::passes::DeadValues.run(&mut p.functions[p.entry]);
    assert!(crate::validate::validate(&p).is_ok());
}
#[test]
fn module_pair_fixed_and_array_abi_forms_are_closed() {
    for mode in 0..5 {
        let (mut p, [_r, m, _v, result]) = fixture(true);
        let f = &mut p.functions[p.entry];
        match mode {
            0 => f.body.locals[result].ty = MirType::I64,
            1 => f.body.locals[m].ty = MirType::Dynamic,
            2 => {
                if let Statement::Assign {
                    value: Rvalue::Call { args, .. },
                    ..
                } = &mut f.body.blocks[f.body.entry].stmts[3]
                {
                    args[3] = Operand::Const(Constant::Null);
                }
            }
            3 => {
                let old = f.body.blocks[f.body.entry].stmts.pop().unwrap();
                if let Statement::Assign { value, .. } = old {
                    f.body.blocks[f.body.entry]
                        .stmts
                        .push(Statement::Eval(value));
                }
            }
            4 => {
                let old = f.body.blocks[f.body.entry].stmts.pop().unwrap();
                if let Statement::Assign {
                    dest,
                    value: Rvalue::Call { args, .. },
                } = old
                {
                    f.body.blocks[f.body.entry].stmts.push(Statement::Assign {
                        dest,
                        value: Rvalue::CallWithArgs {
                            callee: RuntimeFn::CallCapturedMethod,
                            prefix: args,
                            args: vec![],
                        },
                    });
                }
            }
            _ => unreachable!(),
        }
        assert!(crate::validate::validate(&p).is_err(), "case={mode}");
    }
}

#[test]
fn module_pair_optional_operand_is_read_and_survives_link_clone() {
    let (p, [_, flag, target, _]) = fixture(false);
    let f = &p.functions[p.entry];
    let Statement::Assign { value, .. } = &f.body.blocks[f.body.entry].stmts[3] else {
        panic!("fixture")
    };
    let mut reads = rustc_hash::FxHashSet::default();
    crate::passes::dead_values::read_value(value, &mut reads);
    assert!(reads.contains(&flag) && reads.contains(&target));
    let linked = crate::link::link(
        vec![(
            p,
            crate::link::UnitMeta {
                instance_capabilities: Default::default(),
                execution: crate::ModuleExecutionKind::LegacySingleton,
                environment_reason: "existing-contract".into(),
                location: crate::ModuleLocation::LegacyDiagnostic,
                file_backed_by_global: false,
                is_package: false,
                protected_exports: vec![],
                name: "__main__".into(),
                package: String::new(),
                file: "pair.py".into(),
                exports: vec![],
            },
        )],
        0,
    );
    let f = &linked.functions[linked.modules[linked.entry].init];
    assert!(matches!(&f.body.blocks[f.body.entry].stmts[3],
        Statement::Assign { value: Rvalue::DynCallMethod { captured: Some(Operand::Local(actual)), obj: Operand::Local(actual_target), .. }, .. }
            if *actual == flag && *actual_target == target));
}
