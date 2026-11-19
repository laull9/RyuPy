//! 原始筛法真实管线命中及循环观察/活性拒绝测试；不保存生成MIR。
use super::{MirPass, guarded_list_loop::GuardedListLoop};
use crate::mir::*;

fn compile(source: &str) -> Program {
    let mut db = ryupy_base::SourceDb::default();
    let file = db.add("guarded_loop.py", source);
    let (parsed, errors) = ryupy_syntax::parse(file, source);
    assert!(errors.is_empty(), "{errors:?}");
    let (hir, errors) = ryupy_hir::lower_module(&parsed, &Default::default(), source);
    assert!(errors.is_empty(), "{errors:?}");
    let (types, errors) = ryupy_ty::check_module(&hir);
    assert!(errors.is_empty(), "{errors:?}");
    let (mut program, _, errors) = crate::build_program(&hir, &types);
    assert!(errors.is_empty(), "{errors:?}");
    for (_, f) in program.functions.iter_mut() {
        super::SimplifyCfg.run(f);
        super::DeadValues.run(f);
    }
    crate::validate::validate(&program).unwrap();
    program
}
fn hits(p: &Program) -> usize {
    p.functions
        .iter()
        .flat_map(|(_, f)| f.body.blocks.iter())
        .flat_map(|(_, b)| &b.stmts)
        .filter(|s| {
            matches!(
                s,
                Statement::Assign {
                    value: Rvalue::Call {
                        callee: Callee::Runtime(RuntimeFn::GuardedListSetFalseLoop),
                        ..
                    },
                    ..
                }
            )
        })
        .count()
}
fn optimize(p: &mut Program) {
    for (_, f) in p.functions.iter_mut() {
        GuardedListLoop.run(f);
    }
    crate::validate::validate(p).unwrap();
}
const SIMPLE: &str = "def mark(flags, j, bound, step):\n    while j <= bound:\n        flags[j] = False\n        j += step\n    return j\n";

#[test]
fn original_sieve_hits_default_pipeline() {
    let mut p = compile(include_str!("../../../../tests/benchmarks/sieve.py"));
    super::run_pipeline(&mut p, &super::default_pipeline()).unwrap();
    assert_eq!(hits(&p), 1);
}
#[test]
fn simple_loop_keeps_fallback() {
    let mut p = compile(SIMPLE);
    optimize(&mut p);
    assert_eq!(hits(&p), 1);
    assert!(
        p.functions.iter().any(
            |(_, f)| f
                .body
                .blocks
                .iter()
                .any(|(_, b)| b.stmts.iter().any(|s| matches!(
                    s,
                    Statement::Eval(Rvalue::Call {
                        callee: Callee::Runtime(RuntimeFn::SetItem),
                        ..
                    })
                )))
        )
    );
}
#[test]
fn rejects_observers_and_other_shapes() {
    let sources = [
        SIMPLE.replace("j += step", "print(j)\n        j += step"),
        SIMPLE.replace("j += step", "step += 1\n        j += step"),
        SIMPLE.replace("False", "True"),
        SIMPLE.replace("j <= bound", "j < bound"),
        SIMPLE.replace("j += step", "j += j"),
        SIMPLE.replace("    while", "    live = locals()\n    while"),
        SIMPLE.replace("    while j <= bound:\n        flags[j] = False\n        j += step", "    try:\n        while j <= bound:\n            flags[j] = False\n            j += step\n    finally:\n        print(j)"),
        SIMPLE.replace("    while", "    def capture():\n        return j\n    while"),
        SIMPLE.replace("j += step", "j += step\n        break"),
        SIMPLE.replace("    while", "    if bound:\n        del flags\n    while"),
    ];
    for source in sources {
        let mut p = compile(&source);
        optimize(&mut p);
        assert_eq!(hits(&p), 0, "错误准入: {source}");
    }
}
#[test]
fn unnamed_temporary_live_out_rejects() {
    let mut p = compile(SIMPLE);
    let (_, f) = p
        .functions
        .iter_mut()
        .find(|(_, f)| f.name == "mark")
        .unwrap();
    let (exit, temporary) = f
        .body
        .blocks
        .iter()
        .find_map(|(_, b)| {
            let Terminator::Branch { else_: exit, .. } = b.terminator else {
                return None;
            };
            b.stmts.iter().find_map(|s| match s {
                Statement::Assign {
                    dest,
                    value:
                        Rvalue::Call {
                            callee: Callee::Runtime(RuntimeFn::DynRichCompareValue),
                            ..
                        },
                } => Some((exit, *dest)),
                _ => None,
            })
        })
        .unwrap();
    f.body.blocks[exit].terminator = Terminator::Return(Some(Operand::Local(temporary)));
    optimize(&mut p);
    assert_eq!(hits(&p), 0);
}

#[test]
fn abi_rejects_bad_arguments_result_and_unconsumed_value() {
    for variant in 0..4 {
        let mut p = compile(SIMPLE);
        optimize(&mut p);
        let (_, f) = p
            .functions
            .iter_mut()
            .find(|(_, f)| f.name == "mark")
            .unwrap();
        let statement = f
            .body
            .blocks
            .iter_mut()
            .flat_map(|(_, b)| &mut b.stmts)
            .find(|s| {
                matches!(
                    s,
                    Statement::Assign {
                        value: Rvalue::Call {
                            callee: Callee::Runtime(RuntimeFn::GuardedListSetFalseLoop),
                            ..
                        },
                        ..
                    }
                )
            })
            .unwrap();
        let Statement::Assign { dest, value } = statement else {
            unreachable!()
        };
        let Rvalue::Call { args, .. } = value else {
            unreachable!()
        };
        match variant {
            0 => {
                args.pop();
            }
            1 => {
                args[0] = Operand::Const(Constant::I64(1));
            }
            2 => {
                f.body.locals[*dest].ty = MirType::I64;
            }
            3 => {
                *statement = Statement::Eval(value.clone());
            }
            _ => unreachable!(),
        }
        assert!(crate::validate::validate(&p).is_err());
    }
}

#[test]
fn product_trace_observer_rejects_entire_function() {
    let mut p = compile(SIMPLE);
    let (_, f) = p
        .functions
        .iter_mut()
        .find(|(_, f)| f.name == "mark")
        .unwrap();
    let depth = f.body.locals.alloc(LocalDecl {
        ty: MirType::I64,
        name: None,
        is_cell: false,
    });
    f.body.blocks[f.body.entry].stmts.insert(
        0,
        Statement::Assign {
            dest: depth,
            value: Rvalue::Call {
                callee: Callee::Runtime(RuntimeFn::TraceV1GeneratorDepth),
                args: vec![],
            },
        },
    );
    optimize(&mut p);
    assert_eq!(hits(&p), 0);
}
