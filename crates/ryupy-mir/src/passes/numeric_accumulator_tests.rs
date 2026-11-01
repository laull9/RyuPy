//! 累加器只改写入口支配、没有观察或逃逸的唯一自然循环。
use super::{MirPass, numeric_accumulator::NumericAccumulator};
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
            name: "sum".into(),
            codegen_name: "sum".into(),
            source_file: "sum.py".into(),
            params: vec![rhs],
            ret: MirType::Dynamic,
            body: Body {
                locals,
                blocks,
                entry,
                regions: vec![],
            },
            param_meta: vec![],
            function_global: None,
            defaults_global: None,
            signature_global: Some(GlobalId::from_raw(RawIdx::from(0))),
            env: None,
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
                Statement::Eval(Rvalue::Call {
                    callee: Callee::Runtime(RuntimeFn::NumericAccumulatorAdd),
                    ..
                })
            )
        })
    })
}
#[test]
fn strict_loop_materializes_only_at_unique_exit() {
    let (mut f, [entry, _, body, _, exit], [total, _, _]) = sample();
    NumericAccumulator.run(&mut f);
    assert!(transformed(&f));
    assert_eq!(f.body.blocks[entry].stmts.len(), 2);
    assert_eq!(f.body.blocks[body].stmts.len(), 1);
    assert!(
        matches!(&f.body.blocks[exit].stmts[0], Statement::Assign { dest, value: Rvalue::Call { callee: Callee::Runtime(RuntimeFn::NumericAccumulatorValue), .. } } if *dest == total)
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
        NumericAccumulator.run(&mut f);
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
            1 => {} // 不可达前驱也保守拒绝，入口不得重建holder。
            2 => f.body.blocks[extra].terminator = Terminator::Goto(exit),
            _ => unreachable!(),
        }
        NumericAccumulator.run(&mut f);
        assert!(!transformed(&f), "CFG边界 {variant}");
    }
}
