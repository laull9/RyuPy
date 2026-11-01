//! Double证明不能与Int64/bool混淆；同样遵守写kill、合流及循环回边的must事实契约。
use crate::mir::*;
fn sample() -> (Program, [BlockId; 3], [LocalId; 3]) {
    let (mut p, blocks, locals) = super::tests::sample();
    let [entry, yes, no] = blocks;
    let [subject, cond, result] = locals;
    let f = &mut p.functions[p.entry];
    f.ret = MirType::F64;
    f.body.locals[result].ty = MirType::F64;
    f.body.blocks[entry].stmts = vec![Statement::Assign {
        dest: cond,
        value: Rvalue::IsBoxedF64 {
            value: Operand::Local(subject),
        },
    }];
    f.body.blocks[yes].stmts = vec![Statement::Assign {
        dest: result,
        value: Rvalue::UnboxExactF64 {
            value: Operand::Local(subject),
        },
    }];
    f.body.blocks[no].terminator = Terminator::Return(Some(Operand::Const(Constant::F64(0.0))));
    (p, blocks, locals)
}
fn rejected(p: &Program) {
    assert!(
        crate::validate::validate(p)
            .unwrap_err()
            .iter()
            .any(|error| error.contains("精确") || error.contains("拆箱"))
    );
}
fn box_value(dest: LocalId, value: Constant, from: MirType) -> Statement {
    Statement::Assign {
        dest,
        value: Rvalue::Box {
            value: Operand::Const(value),
            from,
        },
    }
}
#[test]
fn guarded_double_and_preserving_alias_are_valid() {
    let (mut p, [_, yes, _], [subject, _, _]) = sample();
    crate::validate::validate(&p).unwrap();
    p.functions[p.entry].body.blocks[yes].stmts.insert(
        0,
        Statement::Assign {
            dest: subject,
            value: Rvalue::Use(Operand::Local(subject)),
        },
    );
    crate::validate::validate(&p).unwrap();
}
#[test]
fn integer_guard_and_writes_cannot_prove_double() {
    for case in 0..4 {
        let (mut p, [entry, yes, _], [subject, cond, _]) = sample();
        let f = &mut p.functions[p.entry];
        match case {
            0 => {
                f.body.blocks[entry].stmts[0] = Statement::Assign {
                    dest: cond,
                    value: Rvalue::IsBoxedI64 {
                        value: Operand::Local(subject),
                    },
                }
            }
            1 => f.body.blocks[yes]
                .stmts
                .insert(0, box_value(subject, Constant::I64(1), MirType::I64)),
            2 => f.body.blocks[yes]
                .stmts
                .insert(0, box_value(subject, Constant::Bool(true), MirType::Bool)),
            _ => f.body.blocks[entry].stmts.push(Statement::Assign {
                dest: cond,
                value: Rvalue::Use(Operand::Const(Constant::Bool(true))),
            }),
        }
        rejected(&p);
    }
}
#[test]
fn mixed_kind_join_and_backedge_invalidate_double_fact() {
    for mixed in [false, true] {
        let (mut p, [entry, yes, no], [subject, cond, result]) = sample();
        let f = &mut p.functions[p.entry];
        f.body.blocks[entry].stmts = vec![Statement::Assign {
            dest: cond,
            value: Rvalue::Use(Operand::Const(Constant::Bool(true))),
        }];
        let join = f.body.blocks.alloc(BasicBlock {
            stmts: vec![Statement::Assign {
                dest: result,
                value: Rvalue::UnboxExactF64 {
                    value: Operand::Local(subject),
                },
            }],
            terminator: Terminator::Return(Some(Operand::Local(result))),
        });
        f.body.blocks[yes].stmts = vec![box_value(subject, Constant::F64(1.0), MirType::F64)];
        f.body.blocks[no].stmts = if mixed {
            vec![box_value(subject, Constant::I64(1), MirType::I64)]
        } else {
            vec![box_value(subject, Constant::F64(2.0), MirType::F64)]
        };
        f.body.blocks[yes].terminator = Terminator::Goto(join);
        f.body.blocks[no].terminator = Terminator::Goto(join);
        if mixed {
            rejected(&p);
        } else {
            crate::validate::validate(&p).unwrap();
        }
    }
    let (mut p, [_, yes, no], [subject, _, _]) = sample();
    let f = &mut p.functions[p.entry];
    f.body.blocks[yes]
        .stmts
        .push(box_value(subject, Constant::I64(1), MirType::I64));
    f.body.blocks[yes].terminator = Terminator::Branch {
        cond: Operand::Const(Constant::Bool(true)),
        then_: yes,
        else_: no,
    };
    rejected(&p);
}
#[test]
fn double_boxes_do_not_qualify_as_integer_leaf_arguments() {
    let (mut p, [entry, yes, _], [subject, cond, result]) = sample();
    let f = &mut p.functions[p.entry];
    let output = f.body.locals.alloc(LocalDecl {
        ty: MirType::Dynamic,
        name: None,
        is_cell: false,
    });
    f.body.blocks[entry].stmts = vec![
        box_value(subject, Constant::F64(-0.0), MirType::F64),
        Statement::Assign {
            dest: cond,
            value: Rvalue::Use(Operand::Const(Constant::Bool(true))),
        },
    ];
    f.body.blocks[yes].stmts.push(Statement::Assign {
        dest: output,
        value: Rvalue::DynCallValue {
            callee: Operand::Const(Constant::None),
            args: vec![Operand::Local(subject)],
        },
    });
    assert!(super::exact_call_sites(f).is_empty());
    f.body.blocks[yes].stmts.pop();
    crate::validate::validate(&p).unwrap();
    // 相反方向同样不能用Double证明Int64拆箱。
    let f = &mut p.functions[p.entry];
    f.ret = MirType::I64;
    f.body.locals[result].ty = MirType::I64;
    f.body.blocks[yes].stmts[0] = Statement::Assign {
        dest: result,
        value: Rvalue::UnboxExactI64 {
            value: Operand::Local(subject),
        },
    };
    rejected(&p);
}
#[test]
fn exact_double_requires_dynamic_input_f64_destination_and_no_dropped_value() {
    for case in 0..3 {
        let (mut p, [entry, yes, _], [subject, _, result]) = sample();
        let f = &mut p.functions[p.entry];
        match case {
            0 => f.body.locals[result].ty = MirType::I64,
            1 => {
                f.body.blocks[yes].stmts[0] = Statement::Eval(Rvalue::UnboxExactF64 {
                    value: Operand::Local(subject),
                })
            }
            _ => {
                f.body.blocks[entry].stmts[0] = Statement::Eval(Rvalue::IsBoxedF64 {
                    value: Operand::Const(Constant::F64(1.0)),
                })
            }
        }
        rejected(&p);
    }
}
