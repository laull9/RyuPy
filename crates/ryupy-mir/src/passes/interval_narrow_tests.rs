//! 合流、bool排除、溢出和预算负例；证明不能把Top/未访问混为一谈或改变既有盒身份。
use super::{MirPass, interval_facts, interval_narrow::IntervalNarrow};
use crate::mir::*;
use la_arena::{Arena, Idx, RawIdx};
fn local(f: &mut Function, ty: MirType) -> LocalId {
    f.body.locals.alloc(LocalDecl {
        ty,
        name: None,
        is_cell: false,
    })
}
fn boxed(dest: LocalId, value: i64) -> Statement {
    Statement::Assign {
        dest,
        value: Rvalue::Box {
            value: Operand::Const(Constant::I64(value)),
            from: MirType::I64,
        },
    }
}
fn sample() -> (Function, LocalId, BlockId) {
    let mut blocks = Arena::default();
    let entry = blocks.alloc(BasicBlock {
        stmts: vec![],
        terminator: Terminator::Unreachable,
    });
    let yes = blocks.alloc(BasicBlock {
        stmts: vec![],
        terminator: Terminator::Unreachable,
    });
    let no = blocks.alloc(BasicBlock {
        stmts: vec![],
        terminator: Terminator::Unreachable,
    });
    let join = blocks.alloc(BasicBlock {
        stmts: vec![],
        terminator: Terminator::Unreachable,
    });
    let mut f = Function {
        module_state: None,
        instance_capabilities: Default::default(),
        module_state_source: None,
        name: "join".into(),
        codegen_name: "join".into(),
        source_file: "join.py".into(),
        definition: None,
        params: vec![],
        ret: MirType::Dynamic,
        body: Body {
            source_handler_contracts: Vec::new(),
            locals: Arena::default(),
            blocks,
            entry,
            regions: vec![],
        },
        param_meta: vec![],
        function_global: None,
        specialization_identity_global: None,
        defaults_global: None,
        signature_global: Some(Idx::from_raw(RawIdx::from_u32(0))),
        env: None,
        dynamic_origin: None,
    };
    let value = local(&mut f, MirType::Dynamic);
    let two = local(&mut f, MirType::Dynamic);
    let sum = local(&mut f, MirType::Dynamic);
    f.body.blocks[entry].terminator = Terminator::Branch {
        cond: Operand::Const(Constant::Bool(true)),
        then_: yes,
        else_: no,
    };
    f.body.blocks[yes].stmts = vec![boxed(value, 3)];
    f.body.blocks[yes].terminator = Terminator::Goto(join);
    f.body.blocks[no].stmts = vec![boxed(value, 7)];
    f.body.blocks[no].terminator = Terminator::Goto(join);
    f.body.blocks[join].stmts = vec![
        boxed(two, 2),
        Statement::Assign {
            dest: sum,
            value: Rvalue::DynBinary {
                op: DynOp::Add,
                lhs: Operand::Local(value),
                rhs: Operand::Local(two),
                inplace: false,
            },
        },
    ];
    f.body.blocks[join].terminator = Terminator::Return(Some(Operand::Local(sum)));
    (f, value, join)
}
fn unboxes(f: &Function) -> usize {
    f.body
        .blocks
        .iter()
        .flat_map(|(_, b)| &b.stmts)
        .filter(|s| {
            matches!(
                s,
                Statement::Assign {
                    value: Rvalue::UnboxExactI64 { .. },
                    ..
                }
            )
        })
        .count()
}
#[test]
fn hull_drives_automatic_unbox_without_changing_source_boxes() {
    let (mut f, value, join) = sample();
    let states = interval_facts::inputs(&f).unwrap();
    let range = states[&join].ranges[&value];
    assert_eq!((range.lo, range.hi), (3, 7));
    IntervalNarrow.run(&mut f);
    assert_eq!(unboxes(&f), 2);
    // 原分支各自的Box必须保留，不能把其观察身份替成新的求和值盒。
    assert_eq!(
        f.body
            .blocks
            .iter()
            .flat_map(|(_, b)| &b.stmts)
            .filter(|s| matches!(s,Statement::Assign{dest,value:Rvalue::Box{..}}if *dest==value))
            .count(),
        2
    );
}
#[test]
fn bool_missing_and_overflow_branches_stay_dynamic() {
    for case in 0..3 {
        let (mut f, value, _) = sample();
        let no = Idx::from_raw(RawIdx::from_u32(2));
        f.body.blocks[no].stmts = match case {
            0 => vec![Statement::Assign {
                dest: value,
                value: Rvalue::Box {
                    value: Operand::Const(Constant::Bool(true)),
                    from: MirType::Bool,
                },
            }],
            1 => vec![],
            _ => vec![boxed(value, i64::MAX)],
        };
        IntervalNarrow.run(&mut f);
        assert_eq!(unboxes(&f), 0);
    }
}
#[test]
fn growing_backedge_widens_to_top() {
    let (mut f, value, join) = sample();
    let counter = local(&mut f, MirType::I64);
    f.body.blocks[f.body.entry].stmts.push(Statement::Assign {
        dest: counter,
        value: Rvalue::Use(Operand::Const(Constant::I64(0))),
    });
    f.body.blocks[join].stmts = vec![
        Statement::Assign {
            dest: counter,
            value: Rvalue::Binary {
                op: BinOp::Add,
                lhs: Operand::Local(counter),
                rhs: Operand::Const(Constant::I64(1)),
            },
        },
        Statement::Assign {
            dest: value,
            value: Rvalue::Box {
                value: Operand::Local(counter),
                from: MirType::I64,
            },
        },
    ];
    f.body.blocks[join].terminator = Terminator::Goto(join);
    let states = interval_facts::inputs(&f).unwrap();
    assert!(!states[&join].ranges.contains_key(&counter));
}
#[test]
fn over_budget_and_cell_inputs_are_unchanged() {
    for cell in [true, false] {
        let (mut f, value, _) = sample();
        if cell {
            f.body.locals[value].is_cell = true;
        } else {
            for _ in 0..129 {
                f.body.blocks.alloc(BasicBlock {
                    stmts: vec![],
                    terminator: Terminator::Unreachable,
                });
            }
        }
        let before = format!("{:?}", f.body);
        IntervalNarrow.run(&mut f);
        assert_eq!(before, format!("{:?}", f.body));
    }
}

#[test]
fn transformed_join_passes_independent_exact_type_verifier() {
    let (mut f, _, _) = sample();
    IntervalNarrow.run(&mut f);
    let mut functions = Arena::default();
    let entry = functions.alloc(f);
    let mut globals = Arena::default();
    globals.alloc(GlobalDecl {
        storage: crate::GlobalStorage::LegacyStatic,
        name: "signature".into(),
        ty: MirType::Dynamic,
    });
    let program = Program {
        functions,
        classes: Arena::default(),
        globals,
        entry,
        source_name: "join.py".into(),
    };
    crate::validate::validate(&program).unwrap();
}

#[test]
fn unknown_native_local_or_call_result_does_not_invent_interval() {
    let (mut f, value, join) = sample();
    let native = local(&mut f, MirType::I64);
    let no = Idx::from_raw(RawIdx::from_u32(2));
    f.body.blocks[no].stmts = vec![Statement::Assign {
        dest: value,
        value: Rvalue::Box {
            value: Operand::Local(native),
            from: MirType::I64,
        },
    }];
    let states = interval_facts::inputs(&f).unwrap();
    assert!(!states[&join].ranges.contains_key(&value));
    IntervalNarrow.run(&mut f);
    assert_eq!(unboxes(&f), 0);
    let (mut f, value, join) = sample();
    f.body.blocks[join].stmts.insert(
        0,
        Statement::Assign {
            dest: value,
            value: Rvalue::Call {
                callee: Callee::Runtime(RuntimeFn::LoadName),
                args: vec![
                    Operand::Const(Constant::Null),
                    Operand::Const(Constant::Str("unknown".into())),
                ],
            },
        },
    );
    IntervalNarrow.run(&mut f);
    assert_eq!(unboxes(&f), 0);
}
