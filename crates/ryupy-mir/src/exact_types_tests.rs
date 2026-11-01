//! 精确类型证明必须覆盖每个可达前驱；写入、合流及循环回边不能留下过期predicate。
use crate::mir::*;
use la_arena::Arena;

fn sample() -> (Program, [BlockId; 3], [LocalId; 3]) {
    let mut locals = Arena::default();
    let subject = locals.alloc(LocalDecl {
        ty: MirType::Dynamic,
        name: None,
        is_cell: false,
    });
    let cond = locals.alloc(LocalDecl {
        ty: MirType::Bool,
        name: None,
        is_cell: false,
    });
    let result = locals.alloc(LocalDecl {
        ty: MirType::I64,
        name: None,
        is_cell: false,
    });
    let mut blocks = Arena::default();
    let entry = blocks.alloc(BasicBlock {
        stmts: vec![],
        terminator: Terminator::Unreachable,
    });
    let yes = blocks.alloc(BasicBlock {
        stmts: vec![Statement::Assign {
            dest: result,
            value: Rvalue::UnboxExactI64 {
                value: Operand::Local(subject),
            },
        }],
        terminator: Terminator::Return(Some(Operand::Local(result))),
    });
    let no = blocks.alloc(BasicBlock {
        stmts: vec![],
        terminator: Terminator::Return(Some(Operand::Const(Constant::I64(0)))),
    });
    blocks[entry].stmts.push(Statement::Assign {
        dest: cond,
        value: Rvalue::IsBoxedI64 {
            value: Operand::Local(subject),
        },
    });
    blocks[entry].terminator = Terminator::Branch {
        cond: Operand::Local(cond),
        then_: yes,
        else_: no,
    };
    let mut functions = Arena::default();
    let func = functions.alloc(Function {
        name: "exact".into(),
        codegen_name: "exact".into(),
        source_file: "exact.py".into(),
        params: vec![subject],
        ret: MirType::I64,
        body: Body {
            locals,
            blocks,
            entry,
            regions: vec![],
        },
        param_meta: vec![],
        function_global: None,
        specialization_identity_global: None,
        defaults_global: None,
        signature_global: None,
        env: None,
    });
    (
        Program {
            functions,
            classes: Arena::default(),
            globals: Arena::default(),
            entry: func,
            source_name: "exact.py".into(),
        },
        [entry, yes, no],
        [subject, cond, result],
    )
}
fn use_value(dest: LocalId, value: Operand) -> Statement {
    Statement::Assign {
        dest,
        value: Rvalue::Use(value),
    }
}
#[test]
fn guarded_exact_unbox_is_valid_and_self_copy_keeps_exact_value() {
    let (mut p, [_, yes, _], [subject, _, _]) = sample();
    assert!(crate::validate::validate(&p).is_ok());
    p.functions[p.entry].body.blocks[yes]
        .stmts
        .insert(0, use_value(subject, Operand::Local(subject)));
    assert!(crate::validate::validate(&p).is_ok());
}
#[test]
fn rewritten_subject_condition_join_and_backedge_kill_proof() {
    for variant in 0..6 {
        let (mut p, [entry, yes, no], [subject, cond, _]) = sample();
        let f = &mut p.functions[p.entry];
        match variant {
            0 => f.body.blocks[yes]
                .stmts
                .insert(0, use_value(subject, Operand::Const(Constant::None))),
            1 => f.body.blocks[entry]
                .stmts
                .push(use_value(cond, Operand::Const(Constant::Bool(true)))),
            2 => f.body.blocks[no].terminator = Terminator::Goto(yes),
            3 => {
                f.body.blocks[yes]
                    .stmts
                    .push(use_value(subject, Operand::Const(Constant::None)));
                f.body.blocks[yes].terminator = Terminator::Goto(yes);
            }
            4 => f.body.blocks[entry]
                .stmts
                .push(use_value(cond, Operand::Local(cond))),
            5 => f.body.blocks[entry]
                .stmts
                .push(use_value(subject, Operand::Local(subject))),
            _ => unreachable!(),
        }
        let errors = crate::validate::validate(&p).unwrap_err();
        assert!(
            errors.iter().any(|e| e.contains("boxed Int64真边证明")),
            "{variant}: {errors:?}"
        );
    }
}
#[test]
fn boxing_bool_does_not_prove_exact_i64_and_wrong_destinations_fail() {
    let (mut p, [entry, yes, _], [subject, _, result]) = sample();
    p.functions[p.entry].body.blocks[entry].stmts = vec![Statement::Assign {
        dest: subject,
        value: Rvalue::Box {
            value: Operand::Const(Constant::Bool(true)),
            from: MirType::Bool,
        },
    }];
    p.functions[p.entry].body.blocks[entry].terminator = Terminator::Goto(yes);
    assert!(
        crate::validate::validate(&p)
            .unwrap_err()
            .iter()
            .any(|e| e.contains("boxed Int64真边证明"))
    );
    let (mut p, [entry, _, _], [_, _, _]) = sample();
    p.functions[p.entry].body.locals[result].ty = MirType::Dynamic;
    assert!(crate::validate::validate(&p).is_err());
    p.functions[p.entry].body.blocks[entry]
        .stmts
        .push(Statement::Eval(Rvalue::IsBoxedI64 {
            value: Operand::Const(Constant::None),
        }));
    assert!(
        crate::validate::validate(&p)
            .unwrap_err()
            .iter()
            .any(|e| e.contains("不能丢弃结果"))
    );
}
#[test]
fn wrapping_add_requires_two_i64_operands() {
    let (mut p, [_, yes, _], [_, _, result]) = sample();
    p.functions[p.entry].body.blocks[yes].stmts = vec![Statement::Assign {
        dest: result,
        value: Rvalue::Binary {
            op: BinOp::WrappingAdd,
            lhs: Operand::Const(Constant::F64(1.0)),
            rhs: Operand::Const(Constant::F64(2.0)),
        },
    }];
    assert!(
        crate::validate::validate(&p)
            .unwrap_err()
            .iter()
            .any(|e| e.contains("WrappingAdd只允许i64"))
    );
}

// 冷回退是既有CLR int/object/object ABI，不能把任意i64静默截断成运算码。
fn cold_program(opcode: Operand) -> Program {
    let (mut p, _, [_, _, result]) = sample();
    let f = &mut p.functions[p.entry];
    f.ret = MirType::Dynamic;
    f.body.locals[result].ty = MirType::Dynamic;
    f.body.blocks = Arena::default();
    f.body.entry = f.body.blocks.alloc(BasicBlock {
        stmts: vec![Statement::Assign {
            dest: result,
            value: Rvalue::Call {
                callee: Callee::Runtime(RuntimeFn::DynBinary),
                args: vec![
                    opcode,
                    Operand::Const(Constant::None),
                    Operand::Const(Constant::None),
                ],
            },
        }],
        terminator: Terminator::Return(Some(Operand::Local(result))),
    });
    p
}
#[test]
fn cold_binary_opcode_must_be_supported_compile_time_constant() {
    for code in [1, 12, 257, 268] {
        let p = cold_program(Operand::Const(Constant::I64(code)));
        assert!(crate::validate::validate(&p).is_ok());
    }
    for code in [-1, 0, 13, 256, 269, 1_i64 << 40] {
        let p = cold_program(Operand::Const(Constant::I64(code)));
        assert!(
            crate::validate::validate(&p)
                .unwrap_err()
                .iter()
                .any(|e| e.contains("合法编译期i64常量")),
            "{code}"
        );
    }
    let p = cold_program(Operand::Const(Constant::Bool(true)));
    assert!(crate::validate::validate(&p).is_err());
}
#[test]
fn cold_binary_cannot_discard_result_or_pass_unboxed_python_values() {
    for variant in 0..3 {
        let mut p = cold_program(Operand::Const(Constant::I64(257)));
        let f = &mut p.functions[p.entry];
        let stmt = &mut f.body.blocks[f.body.entry].stmts[0];
        let Statement::Assign {
            dest,
            value: Rvalue::Call { args, .. },
        } = stmt
        else {
            unreachable!()
        };
        match variant {
            0 => f.body.locals[*dest].ty = MirType::I64,
            1 => args[1] = Operand::Const(Constant::I64(1)),
            2 => {
                *stmt = Statement::Eval(Rvalue::Call {
                    callee: Callee::Runtime(RuntimeFn::DynBinary),
                    args: args.clone(),
                });
            }
            _ => unreachable!(),
        }
        assert!(crate::validate::validate(&p).is_err(), "错误ABI {variant}");
    }
}
