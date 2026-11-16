//! 推导式标量区的来源、守卫、活值物化和别名结构证明；全部从手写Python建立MIR。
use super::{MirPass, scalar_region::ScalarRegion};
use crate::mir::*;
fn compile(source: &str) -> Program {
    let mut db = ryupy_base::SourceDb::default();
    let file = db.add("regions.py", source);
    let (ast, e) = ryupy_syntax::parse(file, source);
    assert!(e.is_empty(), "{e:?}");
    let (hir, e) = ryupy_hir::lower_module(&ast, &Default::default(), source);
    assert!(e.is_empty(), "{e:?}");
    let (types, e) = ryupy_ty::check_module(&hir);
    assert!(e.is_empty(), "{e:?}");
    let (p, _, e) = crate::build_program(&hir, &types);
    assert!(e.is_empty(), "{e:?}");
    p
}
fn values(f: &Function) -> impl Iterator<Item = &Rvalue> {
    f.body
        .blocks
        .iter()
        .flat_map(|(_, b)| &b.stmts)
        .filter_map(|s| match s {
            Statement::Assign { value, .. } | Statement::Eval(value) => Some(value),
            _ => None,
        })
}
fn guards(f: &Function) -> usize {
    values(f)
        .filter(|v| matches!(v, Rvalue::IsBoxedI64 { .. }))
        .count()
}
#[test]
fn original_workload_has_explicit_regions_and_native_modulo() {
    let mut p = compile(include_str!(
        "../../../../tests/benchmarks/comprehensions.py"
    ));
    assert_eq!(
        p.functions
            .iter()
            .filter(|(_, f)| f.is_comprehension)
            .count(),
        5
    );
    for (_, f) in p.functions.iter_mut() {
        ScalarRegion.run(f);
    }
    crate::validate::validate(&p).unwrap();
    assert!(p.functions.iter().filter(|(_, f)| guards(f) > 0).count() >= 3);
    assert!(p.functions.iter().any(|(_, f)| values(f).any(|v| matches!(
        v,
        Rvalue::Call {
            callee: Callee::Runtime(RuntimeFn::ModI64),
            ..
        }
    ))));
    let set = p
        .functions
        .iter()
        .find(|(_, f)| f.name == "<setcomp>")
        .unwrap()
        .1;
    assert!(values(set).any(|v| matches!(
        v,
        Rvalue::Call {
            callee: Callee::Runtime(RuntimeFn::SetAdd),
            ..
        }
    )));
    assert!(!values(set).any(|v| matches!(v, Rvalue::DynCallMethod { .. })));
}
#[test]
fn names_cells_and_large_ranges_do_not_invent_proofs() {
    let mut p = compile(
        "def ordinary(x):\n    return x*x\ndef outer(a):\n    return [x*a for x in range(3)]\n",
    );
    for (_, f) in p.functions.iter_mut() {
        f.name = "<listcomp>".into();
        ScalarRegion.run(f);
        assert_eq!(guards(f), 0);
    }
    let mut p = compile("result = [x % 0 for x in [1]]\n");
    for (_, f) in p.functions.iter_mut() {
        ScalarRegion.run(f);
        assert_eq!(guards(f), 0);
    }
}
#[test]
fn fresh_aliases_materialize_one_box_at_the_boundary() {
    let mut p = compile("result = [x*x for x in [1000]]\n");
    let f = p
        .functions
        .iter_mut()
        .find(|(_, f)| f.is_comprehension)
        .unwrap()
        .1;
    let (bb, at, dest) = f
        .body
        .blocks
        .iter()
        .find_map(|(bb, b)| {
            b.stmts.iter().enumerate().find_map(|(at, s)| match s {
                Statement::Assign {
                    dest,
                    value: Rvalue::DynBinary { op: DynOp::Mul, .. },
                } => Some((bb, at, *dest)),
                _ => None,
            })
        })
        .unwrap();
    let alias = f.body.locals.alloc(LocalDecl {
        ty: MirType::Dynamic,
        name: None,
        is_cell: false,
    });
    let tuple = f.body.locals.alloc(LocalDecl {
        ty: MirType::Dynamic,
        name: None,
        is_cell: false,
    });
    f.body.blocks[bb].stmts.splice(
        at + 1..at + 1,
        [
            Statement::Assign {
                dest: alias,
                value: Rvalue::Use(Operand::Local(dest)),
            },
            Statement::Assign {
                dest: tuple,
                value: Rvalue::CallWithArgs {
                    callee: RuntimeFn::MakeTuple,
                    prefix: vec![],
                    args: vec![Operand::Local(dest), Operand::Local(alias)],
                },
            },
        ],
    );
    ScalarRegion.run(f);
    assert!(f.body.blocks.iter().any(|(_,b)|b.stmts.iter().any(|s|matches!(s,Statement::Assign{dest:d,value:Rvalue::Box{from:MirType::I64,..}}if *d==dest))
        &&b.stmts.iter().any(|s|matches!(s,Statement::Assign{dest:d,value:Rvalue::Use(Operand::Local(v))}if *d==alias&&*v==dest))));
    assert!(
        !f.body
            .blocks
            .iter()
            .flat_map(|(_, b)| &b.stmts)
            .any(|s| matches!(s,Statement::Assign{dest:d,value:Rvalue::Box{..}}if *d==alias))
    );
    crate::validate::validate(&p).unwrap();
}

#[test]
fn immediate_enumerate_unpack_is_scalar_but_escaping_pairs_stay_objects() {
    let mut p =
        compile("a = {x:i for i,x in enumerate([3,4])}\nb = [pair for pair in enumerate([3,4])]\n");
    let mut hits = 0;
    for (_, f) in p.functions.iter_mut() {
        super::enumerate_scalar::EnumerateScalar.run(f);
        if values(f).any(|v| {
            matches!(
                v,
                Rvalue::Call {
                    callee: Callee::Runtime(RuntimeFn::IsEnumerateIterator),
                    ..
                }
            )
        }) {
            hits += 1;
            assert!(values(f).any(|v| matches!(
                v,
                Rvalue::Call {
                    callee: Callee::Runtime(RuntimeFn::EnumerateCurrentIndex),
                    ..
                }
            )));
            assert!(
                values(f).any(|v| matches!(
                    v,
                    Rvalue::Call {
                        callee: Callee::Runtime(RuntimeFn::IterValue),
                        ..
                    }
                )),
                "原迭代协议慢路必须保留"
            );
        }
    }
    assert_eq!(hits, 1);
    crate::validate::validate(&p).unwrap();
}

#[test]
fn capacity_requires_single_unfiltered_list_scope() {
    let p = compile(
        "a = [x for x in range(3)]\nb = [x for x in range(3) if x]\nc = [(x,y) for x in range(3) for y in range(2)]\n",
    );
    assert_eq!(
        p.functions
            .iter()
            .map(|(_, f)| values(f)
                .filter(|v| matches!(
                    v,
                    Rvalue::Call {
                        callee: Callee::Runtime(RuntimeFn::MakeComprehensionList),
                        ..
                    }
                ))
                .count())
            .sum::<usize>(),
        1
    );
}
