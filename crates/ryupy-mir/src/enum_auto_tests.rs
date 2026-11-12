//! Enum auto语法准入不折叠callee；名单在Create冻结，赋值与默认参数读取均先于Finalize。
use crate::*;

#[test]
fn enum_auto_preserves_runtime_calls_namespace_order_and_exact_member_declaration() {
    let source = "from enum import Enum, auto as next_value\nclass C(Enum):\n    A = next_value()\n    B = A\n    C = next_value()\n    def read(self, value=A): return value\n";
    let mut db = ryupy_base::SourceDb::default();
    let file = db.add("enum_auto.py", source);
    let (parsed, errors) = ryupy_syntax::parse(file, source);
    assert!(errors.is_empty());
    let (hir, errors) = ryupy_hir::lower_module_with_native_exceptions(
        &parsed,
        &Default::default(),
        source,
        &["enum"],
    );
    assert!(errors.is_empty(), "{errors:?}");
    let (types, errors) = ryupy_ty::check_module(&hir);
    assert!(errors.is_empty(), "{errors:?}");
    let (mut program, _, errors) = build_program(&hir, &types);
    assert!(errors.is_empty(), "{errors:?}");
    validate::validate(&program).unwrap();
    let stmts: Vec<_> = program.functions[program.entry]
        .body
        .blocks
        .iter()
        .flat_map(|(_, block)| &block.stmts)
        .collect();
    let calls: Vec<_> = stmts
        .iter()
        .enumerate()
        .filter_map(|(index, statement)| {
            let value = match statement {
                Statement::Assign { value, .. } | Statement::Eval(value) => value,
                _ => return None,
            };
            if let Rvalue::Call {
                callee: Callee::Runtime(function),
                args,
            } = value
            {
                Some((index, *function, args))
            } else {
                None
            }
        })
        .collect();
    let creates: Vec<_> = calls
        .iter()
        .filter(|(_, f, _)| *f == RuntimeFn::EnumCreate)
        .collect();
    let finishes: Vec<_> = calls
        .iter()
        .filter(|(_, f, _)| *f == RuntimeFn::EnumFinalize)
        .collect();
    assert_eq!(creates.len(), 1);
    assert_eq!(finishes.len(), 1);
    let (create, _, begin_args) = creates[0];
    let (finish, _, end_args) = finishes[0];
    let names = Operand::Array(
        ["A", "B", "C"]
            .map(|name| Operand::Const(Constant::Str(name.into())))
            .into(),
    );
    assert_eq!(begin_args.len(), 3);
    assert_eq!(begin_args[2], names);
    assert_eq!(end_args[1], names);
    let dynamic: Vec<_> = stmts
        .iter()
        .enumerate()
        .filter_map(|(index, statement)| {
            if let Statement::Assign {
                dest,
                value: Rvalue::DynCallValue { callee, args },
            } = statement
            {
                assert!(args.is_empty());
                Some((index, *dest, callee))
            } else {
                None
            }
        })
        .collect();
    assert_eq!(dynamic.len(), 2);
    assert!(
        dynamic
            .iter()
            .all(|(index, _, _)| create < index && index < finish)
    );
    let writes: Vec<_> = calls.iter().filter(|(_, f, args)| *f == RuntimeFn::SetAttr
        && args.get(1).is_some_and(|a| matches!(a, Operand::Const(Constant::Str(n)) if ["A", "B", "C", "read"].contains(&n.as_str())))).collect();
    assert_eq!(writes.len(), 4);
    for (call, write) in dynamic.iter().zip([writes[0], writes[2]]) {
        assert!(call.0 < write.0);
        assert_eq!(write.2[2], Operand::Local(call.1));
    }

    assert!(
        writes
            .iter()
            .all(|(index, _, _)| create < index && index < finish)
    );
    let reads: Vec<_> = calls
        .iter()
        .filter(|(_, f, args)| {
            *f == RuntimeFn::ClassNamespaceGet
                && args
                    .iter()
                    .any(|a| matches!(a, Operand::Const(Constant::Str(n)) if n == "A"))
        })
        .collect();
    assert_eq!(reads.len(), 2);
    assert!(
        reads
            .iter()
            .all(|(index, _, _)| writes[0].0 < *index && index < finish)
    );
    passes::run_pipeline(&mut program, &passes::default_pipeline()).unwrap();
    validate::validate(&program).unwrap();
}

#[test]
fn failed_enum_body_or_decorator_cannot_replace_existing_public_binding() {
    for decorate in [false, true] {
        let decorator = if decorate { "    @fail\n" } else { "" };
        let source = format!(
            "from enum import Enum, auto\ndef fail(cls):\n    raise RuntimeError('decorator')\nPublished = 'old'\ntry:\n{decorator}    class Published(Enum):\n        A = 10\n        B = 1\n        C = auto()\n        def unreachable(self, value=print('unreachable')): return value\nexcept Exception:\n    print(Published)\n"
        );
        let mut db = ryupy_base::SourceDb::default();
        let (parsed, errors) = ryupy_syntax::parse(db.add("enum_failure.py", &source), &source);
        assert!(errors.is_empty(), "{errors:?}");
        let (hir, errors) = ryupy_hir::lower_module_with_native_exceptions(
            &parsed,
            &Default::default(),
            &source,
            &["enum"],
        );
        assert!(errors.is_empty(), "{errors:?}");
        let (types, errors) = ryupy_ty::check_module(&hir);
        assert!(errors.is_empty(), "{errors:?}");
        let (program, _, errors) = build_program(&hir, &types);
        assert!(errors.is_empty(), "{errors:?}");
        validate::validate(&program).unwrap();
        let class = hir.classes.iter().next().unwrap().1;
        let binding = class
            .decorated_binding
            .expect("Enum必须使用动态公开槽而非私有class槽");
        let name = format!("__mv{}_Published", binding.into_raw().into_u32());
        let target = program
            .globals
            .iter()
            .find(|(_, g)| g.name == name)
            .unwrap()
            .0;
        let stmts: Vec<_> = program.functions[program.entry]
            .body
            .blocks
            .iter()
            .flat_map(|(_, b)| &b.stmts)
            .collect();
        let writes: Vec<_> = stmts.iter().enumerate().filter(|(_,s)| matches!(s, Statement::SetGlobal{dest,value} if *dest == target && !matches!(value,Operand::Const(Constant::Null)))).collect();
        assert_eq!(writes.len(), 2, "旧值及最终成功值各一次；类体不能提前发布");
        let finalization = stmts
            .iter()
            .position(|s| {
                matches!(
                    s,
                    Statement::Assign {
                        value: Rvalue::Call {
                            callee: Callee::Runtime(RuntimeFn::EnumFinalize),
                            ..
                        },
                        ..
                    }
                )
            })
            .unwrap();
        assert!(writes[0].0 < finalization && finalization < writes[1].0);
        if decorate {
            assert!(
                stmts[finalization + 1..writes[1].0]
                    .iter()
                    .any(|s| matches!(
                        s,
                        Statement::Assign {
                            value: Rvalue::DynCallValue { .. },
                            ..
                        } | Statement::Assign {
                            value: Rvalue::Call { .. },
                            ..
                        }
                    ))
            );
        }
    }
}
