//! 向量仅存在受硬件真边支配的私有局部；错误通道/类型/逃逸在MIR阻断。
use crate::mir::*;
use la_arena::Arena;

fn sample() -> (Program, [BlockId; 3], [LocalId; 4]) {
    let mut locals = Arena::default();
    let mut local = |ty| {
        locals.alloc(LocalDecl {
            ty,
            name: None,
            is_cell: false,
        })
    };
    let guard = local(MirType::Bool);
    let first = local(MirType::V128F64);
    let second = local(MirType::V128F64);
    let result = local(MirType::F64);
    let mut blocks = Arena::default();
    let entry = blocks.alloc(BasicBlock {
        stmts: vec![Statement::Assign {
            dest: guard,
            value: Rvalue::IsVector128Supported,
        }],
        terminator: Terminator::Unreachable,
    });
    let yes = blocks.alloc(BasicBlock {
        stmts: vec![
            Statement::Assign {
                dest: first,
                value: Rvalue::VectorPackF64 {
                    low: Operand::Const(Constant::F64(1.0)),
                    high: Operand::Const(Constant::F64(-2.0)),
                },
            },
            Statement::Assign {
                dest: second,
                value: Rvalue::VectorBinaryF64 {
                    op: BinOp::Add,
                    lhs: Operand::Local(first),
                    rhs: Operand::Local(first),
                },
            },
            Statement::Assign {
                dest: result,
                value: Rvalue::VectorLaneF64 {
                    value: Operand::Local(second),
                    lane: 1,
                },
            },
        ],
        terminator: Terminator::Return(Some(Operand::Local(result))),
    });
    let no = blocks.alloc(BasicBlock {
        stmts: vec![],
        terminator: Terminator::Return(Some(Operand::Const(Constant::F64(0.0)))),
    });
    blocks[entry].terminator = Terminator::Branch {
        cond: Operand::Local(guard),
        then_: yes,
        else_: no,
    };
    let mut functions = Arena::default();
    let main = functions.alloc(Function {
        module_state: None,
        instance_capabilities: Default::default(),
        module_state_source: None,
        name: "vector_test".into(),
        codegen_name: "vector_test".into(),
        source_file: "vector.py".into(),
        definition: None,
        params: vec![],
        ret: MirType::F64,
        body: Body {
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
        signature_global: None,
        env: None,
        dynamic_origin: None,
    });
    (
        Program {
            functions,
            classes: Arena::default(),
            globals: Arena::default(),
            entry: main,
            source_name: "vector.py".into(),
        },
        [entry, yes, no],
        [guard, first, second, result],
    )
}
fn rejected(program: &Program) {
    assert!(
        crate::validate::validate(program)
            .unwrap_err()
            .iter()
            .any(|message| message.contains("向量"))
    );
}
#[test]
fn guarded_private_vector_and_copy_pass() {
    let (mut p, [_, yes, _], [_, first, second, _]) = sample();
    crate::validate::validate(&p).unwrap();
    p.functions[p.entry].body.blocks[yes].stmts.insert(
        1,
        Statement::Assign {
            dest: second,
            value: Rvalue::Use(Operand::Local(first)),
        },
    );
    crate::validate::validate(&p).unwrap();
}
#[test]
fn missing_guard_overwrite_and_unguarded_diamond_fail() {
    let (mut p, [entry, yes, _], _) = sample();
    p.functions[p.entry].body.blocks[entry].terminator = Terminator::Goto(yes);
    rejected(&p);
    let (mut p, [entry, _, _], [guard, ..]) = sample();
    p.functions[p.entry].body.blocks[entry]
        .stmts
        .push(Statement::Assign {
            dest: guard,
            value: Rvalue::Use(Operand::Const(Constant::Bool(true))),
        });
    rejected(&p);
    let (mut p, [_, yes, no], _) = sample();
    p.functions[p.entry].body.blocks[no].terminator = Terminator::Goto(yes);
    rejected(&p);
}
#[test]
fn private_vector_rejects_lane_opcode_pack_and_result_type_errors() {
    for case in 0..4 {
        let (mut p, [_, yes, _], [_, first, second, result]) = sample();
        let f = &mut p.functions[p.entry];
        match case {
            0 => {
                f.body.blocks[yes].stmts[2] = Statement::Assign {
                    dest: result,
                    value: Rvalue::VectorLaneF64 {
                        value: Operand::Local(second),
                        lane: 2,
                    },
                }
            }
            1 => {
                f.body.blocks[yes].stmts[1] = Statement::Assign {
                    dest: second,
                    value: Rvalue::VectorBinaryF64 {
                        op: BinOp::Div,
                        lhs: Operand::Local(first),
                        rhs: Operand::Local(first),
                    },
                }
            }
            2 => {
                f.body.blocks[yes].stmts[0] = Statement::Assign {
                    dest: first,
                    value: Rvalue::VectorPackF64 {
                        low: Operand::Const(Constant::I64(1)),
                        high: Operand::Const(Constant::F64(2.0)),
                    },
                }
            }
            _ => f.body.locals[result].ty = MirType::Dynamic,
        }
        rejected(&p);
    }
}
#[test]
fn vector_cannot_escape_through_box_call_array_return_or_parameter() {
    for case in 0..6 {
        let (mut p, [_, yes, _], [_, first, _, result]) = sample();
        let f = &mut p.functions[p.entry];
        match case {
            0 => f.body.blocks[yes].stmts.push(Statement::Eval(Rvalue::Box {
                value: Operand::Local(first),
                from: MirType::V128F64,
            })),
            1 => f.body.blocks[yes].stmts.push(Statement::Eval(Rvalue::Call {
                callee: Callee::Runtime(RuntimeFn::MakeList),
                args: vec![Operand::Array(vec![Operand::Local(first)])],
            })),
            2 => {
                f.ret = MirType::V128F64;
                f.body.blocks[yes].terminator = Terminator::Return(Some(Operand::Local(first)));
            }
            3 => f.params.push(first),
            4 => f.body.locals[first].is_cell = true,
            _ => f.body.blocks[yes].stmts.push(Statement::Assign {
                dest: result,
                value: Rvalue::Use(Operand::Local(first)),
            }),
        }
        rejected(&p);
    }
}
