//! 模块pair两处查询的真实transfer和诊断回归；保持先kill、后查询及先验证、后transfer。
use super::*;
use la_arena::{Arena, Idx, RawIdx};

fn local(raw: u32) -> LocalId {
    Idx::from_raw(RawIdx::from_u32(raw))
}
fn declaration(ty: MirType) -> LocalDecl {
    LocalDecl {
        ty,
        name: None,
        is_cell: false,
    }
}
fn assign(dest: LocalId) -> Statement {
    Statement::Assign {
        dest,
        value: Rvalue::Use(Operand::Const(Constant::Null)),
    }
}
fn fixture() -> (Program, [LocalId; 4]) {
    let mut p = crate::module_capture_tests::program(Statement::Line(1));
    let f = &mut p.functions[p.entry];
    let receiver = local(0);
    let flag = f.body.locals.alloc(declaration(MirType::Bool));
    let target = f.body.locals.alloc(declaration(MirType::Dynamic));
    let result = f.body.locals.alloc(declaration(MirType::Dynamic));
    f.body.blocks[f.body.entry].stmts = vec![
        assign(receiver),
        Statement::Assign {
            dest: flag,
            value: Rvalue::Call {
                callee: Callee::Runtime(RuntimeFn::IsModule),
                args: vec![Operand::Local(receiver)],
            },
        },
        Statement::Assign {
            dest: target,
            value: Rvalue::Call {
                callee: Callee::Runtime(RuntimeFn::CaptureModuleTarget),
                args: vec![
                    Operand::Local(receiver),
                    Operand::Const(Constant::Str("f".into())),
                ],
            },
        },
        Statement::Assign {
            dest: result,
            value: invocation(flag, target),
        },
    ];
    (p, [receiver, flag, target, result])
}
fn invocation(flag: LocalId, target: LocalId) -> Rvalue {
    Rvalue::DynCallMethod {
        captured: Some(Operand::Local(flag)),
        obj: Operand::Local(target),
        name: "f".into(),
        args: vec![],
    }
}
fn errors(p: &Program) -> Vec<String> {
    let mut errors = vec![];
    validate(p, &mut errors);
    errors
}

#[test]
fn transfer_checks_current_arena_boundaries_and_foreign_raw() {
    let mut p = crate::module_capture_tests::program(Statement::Line(1));
    let f = &mut p.functions[p.entry];
    let last = f.body.locals.alloc(declaration(MirType::Dynamic));
    let mut foreign = Arena::default();
    let foreign_first = foreign.alloc(declaration(MirType::I64));
    for dest in [local(0), last, foreign_first] {
        let mut facts = Facts::default();
        facts.transfer(f, &assign(dest));
        assert!(matches!(facts.0.get(&dest), Some(Fact::Frozen)));
    }
    for dest in [local(f.body.locals.len() as u32), local(u32::MAX)] {
        let mut facts = Facts::default();
        facts.transfer(f, &assign(dest));
        assert!(facts.0.is_empty());
    }
    f.body.locals.clear();
    for dest in [local(0), local(u32::MAX)] {
        let mut facts = Facts::default();
        facts.transfer(f, &assign(dest));
        assert!(facts.0.is_empty());
    }
    f.body.locals.alloc(declaration(MirType::I64));
    let mut facts = Facts::default();
    facts.transfer(f, &assign(local(0)));
    assert!(facts.0.is_empty());
}

#[test]
fn invalid_destination_still_kills_its_key_and_all_dependencies() {
    for empty in [false, true] {
        let mut p = crate::module_capture_tests::program(Statement::Line(1));
        let f = &mut p.functions[p.entry];
        if empty {
            f.body.locals.clear();
        }
        for dest in [local(f.body.locals.len() as u32), local(u32::MAX)] {
            let mut facts = Facts::default();
            facts.0.insert(dest, Fact::Frozen);
            facts.0.insert(local(10), Fact::Flag(dest));
            facts.0.insert(
                local(11),
                Fact::Target {
                    receiver: dest,
                    flag: local(99),
                    name: "f".into(),
                },
            );
            facts.0.insert(
                local(12),
                Fact::Target {
                    receiver: local(99),
                    flag: dest,
                    name: "f".into(),
                },
            );
            facts.0.insert(local(99), Fact::Frozen);
            facts.transfer(f, &assign(dest));
            assert_eq!(facts.0.len(), 1);
            assert!(matches!(facts.0.get(&local(99)), Some(Fact::Frozen)));
        }
    }
}

#[test]
fn transfer_keeps_cell_named_parameter_and_environment_exclusions() {
    for mode in 0..5 {
        let mut p = crate::module_capture_tests::program(Statement::Line(1));
        let f = &mut p.functions[p.entry];
        let dest = local(0);
        match mode {
            1 => f.body.locals[dest].is_cell = true,
            2 => f.body.locals[dest].name = Some("visible".into()),
            3 => f.params.push(dest),
            4 => f.env = Some(dest),
            _ => {}
        }
        let mut facts = Facts::default();
        facts.0.insert(local(10), Fact::Flag(dest));
        facts.transfer(f, &assign(dest));
        assert!(!facts.0.contains_key(&local(10)));
        assert_eq!(matches!(facts.0.get(&dest), Some(Fact::Frozen)), mode == 0);
    }
}

#[test]
fn captured_call_checks_pair_before_result_transfer_kills_it() {
    let (mut p, [_, flag, target, _]) = fixture();
    let f = &mut p.functions[p.entry];
    f.body.blocks[f.body.entry].stmts[3] = Statement::Assign {
        dest: target,
        value: invocation(flag, target),
    };
    assert_eq!(crate::validate::validate(&p), Ok(()));
    // 上一次调用写回target后已经杀掉来源事实，下一次调用必须拒绝。
    let f = &mut p.functions[p.entry];
    f.body.blocks[f.body.entry].stmts.push(Statement::Assign {
        dest: target,
        value: invocation(flag, target),
    });
    assert_eq!(
        errors(&p),
        vec!["函数 `main` 的模块捕获标志/目标缺少同源冻结及支配证明"]
    );
}

#[test]
fn captured_destination_abi_preserves_dynamic_invalid_and_eval_results() {
    for mode in 0..6 {
        let (mut p, [_, flag, target, result]) = fixture();
        let f = &mut p.functions[p.entry];
        let statement = match mode {
            1 => {
                f.body.locals[result].ty = MirType::I64;
                Statement::Assign {
                    dest: result,
                    value: invocation(flag, target),
                }
            }
            2 => Statement::Assign {
                dest: local(f.body.locals.len() as u32),
                value: invocation(flag, target),
            },
            3 => Statement::Assign {
                dest: local(u32::MAX),
                value: invocation(flag, target),
            },
            4 => Statement::Eval(invocation(flag, target)),
            5 => {
                let mut foreign = Arena::default();
                for _ in 0..3 {
                    foreign.alloc(declaration(MirType::I64));
                }
                let dest = foreign.alloc(declaration(MirType::Bool));
                Statement::Assign {
                    dest,
                    value: invocation(flag, target),
                }
            }
            _ => Statement::Assign {
                dest: result,
                value: invocation(flag, target),
            },
        };
        f.body.blocks[f.body.entry].stmts[3] = statement;
        let expected: Vec<String> = if mode == 0 || mode == 5 {
            vec![]
        } else {
            vec!["函数 `main` 的捕获方法固定ABI/结果槽错误".into()]
        };
        assert_eq!(errors(&p), expected);
    }
}

#[test]
fn invalid_destination_keeps_complete_abi_origin_and_membership_error_order() {
    let (mut p, [_, flag, target, _]) = fixture();
    let f = &mut p.functions[p.entry];
    // 删除全部来源建立语句，但保留有效flag/target类型，隔离来源与结果槽诊断。
    f.body.blocks[f.body.entry].stmts = vec![Statement::Assign {
        dest: local(u32::MAX),
        value: invocation(flag, target),
    }];
    assert_eq!(crate::validate::validate(&p), Err(vec![
        "函数 `main` 的捕获方法固定ABI/结果槽错误".into(),
        "函数 `main` 的模块捕获标志/目标缺少同源冻结及支配证明".into(),
        "函数 `main` 基本块 Idx::<BasicBlock>(0) 中的赋值目标 Idx::<LocalDecl>(4294967295) 不在 locals arena 中".into(),
    ]));
}
