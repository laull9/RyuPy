//! 默认管线的字段生成器结构、跨暂停匿名临时与复杂协议拒绝；预期来自源码约束。
use crate::mir::*;

fn compile(source: &str) -> Program {
    let mut db = ryupy_base::SourceDb::default();
    let file = db.add("state.py", source);
    let (ast, errors) = ryupy_syntax::parse(file, source);
    assert!(errors.is_empty(), "{errors:?}");
    let (hir, errors) = ryupy_hir::lower_module(&ast, &Default::default(), source);
    assert!(errors.is_empty(), "{errors:?}");
    let (types, errors) = ryupy_ty::check_module(&hir);
    assert!(errors.is_empty(), "{errors:?}");
    let (p, _, errors) = crate::build_program(&hir, &types);
    assert!(errors.is_empty(), "{errors:?}");
    crate::validate::validate(&p).unwrap();
    p
}
fn states(p: &Program) -> Vec<FnId> {
    p.functions
        .iter()
        .flat_map(|(_, f)| f.body.blocks.iter())
        .flat_map(|(_, b)| &b.stmts)
        .filter_map(|s| match s {
            Statement::Assign {
                value: Rvalue::MakeStateGenerator { resume, .. },
                ..
            } => Some(*resume),
            _ => None,
        })
        .collect()
}
#[test]
fn original_generator_workload_uses_resident_fields() {
    let p = compile(include_str!("../../../tests/benchmarks/generators.py"));
    assert_eq!(
        states(&p).len(),
        2,
        "count_up和genexpr使用字段，yield from保留旧载体"
    );
    for id in states(&p) {
        let f = &p.functions[id];
        assert!(
            f.body
                .blocks
                .iter()
                .flat_map(|(_, b)| &b.stmts)
                .any(|s| matches!(s, Statement::SetField { .. }))
        );
        assert!(
            !f.body
                .blocks
                .iter()
                .flat_map(|(_, b)| &b.stmts)
                .any(|s| matches!(
                    s,
                    Statement::Assign {
                        value: Rvalue::Call {
                            callee: Callee::Runtime(RuntimeFn::ArrayGet | RuntimeFn::ArraySet),
                            ..
                        },
                        ..
                    } | Statement::Eval(Rvalue::Call {
                        callee: Callee::Runtime(RuntimeFn::ArrayGet | RuntimeFn::ArraySet),
                        ..
                    })
                ))
        );
    }
}
#[test]
fn anonymous_expression_value_crossing_yield_has_field() {
    let p = compile("def g():\n    yield 10 + (yield 2)\n");
    let id = states(&p)[0];
    let f = &p.functions[id];
    let class = p
        .classes
        .iter()
        .find(|(_, c)| c.is_generator_state)
        .unwrap()
        .1;
    assert!(
        class.fields.iter().any(|(name, ty)| {
            let index: u32 = name.strip_prefix("slot").unwrap().parse().unwrap();
            let local = LocalId::from_raw(index.into());
            *ty == MirType::Dynamic && f.body.locals[local].name.is_none()
        }),
        "跨暂停的字面量求值对象必须有匿名Dynamic字段"
    );
    assert!(
        f.body
            .blocks
            .iter()
            .flat_map(|(_, b)| &b.stmts)
            .any(|s| matches!(
                s,
                Statement::Assign {
                    value: Rvalue::Call {
                        callee: Callee::Runtime(RuntimeFn::GeneratorInput),
                        ..
                    },
                    ..
                }
            ))
    );
}
#[test]
fn exception_closure_delegation_and_locals_stay_legacy() {
    for source in [
        "def g():\n    try:\n        yield 1\n    finally:\n        print(2)\n",
        "def g():\n    x = 1\n    def inner():\n        return x\n    yield inner\n",
        "def g():\n    yield from [1, 2]\n",
        "def g():\n    yield locals()\n",
    ] {
        assert!(states(&compile(source)).is_empty(), "{source}");
    }
}
#[test]
fn invalid_state_creation_is_rejected() {
    for bad in 0..3 {
        let mut p = compile("def g():\n    yield 1\n");
        let stmt = p
            .functions
            .iter_mut()
            .flat_map(|(_, f)| f.body.blocks.iter_mut())
            .flat_map(|(_, b)| &mut b.stmts)
            .find(|s| {
                matches!(
                    s,
                    Statement::Assign {
                        value: Rvalue::MakeStateGenerator { .. },
                        ..
                    }
                )
            })
            .unwrap();
        let Statement::Assign {
            value:
                Rvalue::MakeStateGenerator {
                    initial_pc,
                    payload,
                    origin,
                    ..
                },
            ..
        } = stmt
        else {
            unreachable!()
        };
        match bad {
            0 => *initial_pc = -2,
            1 => *payload = Operand::Const(Constant::None),
            _ => *origin = Some(Operand::Const(Constant::I64(3))),
        }
        assert!(crate::validate::validate(&p).is_err());
    }
}

#[test]
fn native_range_control_is_stored_without_boxing() {
    let p = compile("def g(n):\n    for x in range(n):\n        yield x\n");
    let f = &p.functions[states(&p)[0]];
    let class = p
        .classes
        .iter()
        .find(|(_, c)| c.is_generator_state)
        .unwrap()
        .1;
    assert!(f.body.blocks.iter().flat_map(|(_, b)| &b.stmts).any(|s|
        matches!(s, Statement::SetField { field, value: Operand::Local(value), .. }
            if class.fields[*field as usize].1 == MirType::I64 && f.body.locals[*value].ty == MirType::I64)));
}
