//! KeyError默认域读取与完整调用的结构门；遮蔽/实例owner、固定ABI和可观察性不能退化。
use crate::module_delete_tests::compile;
use crate::*;

fn lookups(program: &Program) -> usize {
    program
        .functions
        .iter()
        .flat_map(|(_, f)| f.body.blocks.iter())
        .flat_map(|(_, b)| &b.stmts)
        .filter(|stmt| {
            matches!(
                stmt,
                Statement::Assign {
                    value: Rvalue::Call {
                        callee: Callee::Runtime(RuntimeFn::LoadDefaultBuiltin),
                        ..
                    },
                    ..
                }
            )
        })
        .count()
}

#[test]
fn direct_keyerror_and_saved_value_keep_observable_default_lookups() {
    let mut program = compile("a = KeyError('x')\nb = KeyError\n", false);
    assert_eq!(lookups(&program), 2);
    assert!(!pretty::dump(&program).contains("MakeException"));
    passes::run_pipeline(&mut program, &passes::default_pipeline()).unwrap();
    assert_eq!(lookups(&program), 2);
    assert!(validate::validate(&program).is_ok());

    let mut unused = compile("KeyError\n", false);
    assert_eq!(lookups(&unused), 1);
    passes::run_pipeline(
        &mut unused,
        &[passes::PipelinePass::Function(Box::new(passes::DeadValues))],
    )
    .unwrap();
    assert_eq!(lookups(&unused), 1, "字典读取不可因结果未使用而当纯值删除");
    assert!(validate::validate(&unused).is_ok());
}

#[test]
fn keyerror_lookup_is_frozen_before_argument_effects() {
    let program = compile("KeyError(print('argument'))\n", false);
    let main = &program.functions[program.entry];
    let block = &main.body.blocks[main.body.entry];
    let (lookup_index, captured) = block
        .stmts
        .iter()
        .enumerate()
        .find_map(|(i, stmt)| match stmt {
            Statement::Assign {
                dest,
                value:
                    Rvalue::Call {
                        callee: Callee::Runtime(RuntimeFn::LoadDefaultBuiltin),
                        ..
                    },
            } => Some((i, *dest)),
            _ => None,
        })
        .unwrap();
    let argument_index = block
        .stmts
        .iter()
        .position(|stmt| {
            matches!(
                stmt,
                Statement::Assign {
                    value: Rvalue::Call {
                        callee: Callee::Runtime(RuntimeFn::DynBuiltin),
                        ..
                    },
                    ..
                }
            )
        })
        .unwrap();
    assert!(lookup_index < argument_index);
    assert!(block.stmts[argument_index + 1..].iter().any(|stmt| {
        matches!(stmt, Statement::Assign {
            value: Rvalue::DynCallValue { callee: Operand::Local(local), args }, ..
        } if *local == captured && args.len() == 1)
    }));
}

#[test]
fn full_positionals_use_existing_calls_without_flattening_into_messages() {
    let program = compile(
        "a = KeyError()\nb = KeyError(None)\nc = KeyError('')\nd = KeyError(1, 2)\ne = KeyError(1, 2, 3)\nf = KeyError(1, 2, 3, 4)\n",
        false,
    );
    assert_eq!(lookups(&program), 6);
    let dump = pretty::dump(&program);
    assert!(!dump.contains("MakeException"));
    assert!(
        program
            .functions
            .iter()
            .any(|(_, f)| f.body.blocks.iter().any(|(_, b)| {
                b.stmts.iter().any(|stmt| {
                    matches!(stmt, Statement::Assign {
                    value: Rvalue::Call { callee: Callee::Runtime(RuntimeFn::CallValue), args }, ..
                } if matches!(&args[1], Operand::Array(values) if values.len() == 4))
                })
            }))
    );
}

#[test]
fn shadowed_instance_and_other_exception_paths_are_not_replaced() {
    for source in [
        "def f(KeyError):\n    return KeyError('x')\n",
        "def replacement(value):\n    return value\nKeyError = replacement\nKeyError('x')\n",
        "def outer(KeyError):\n    def inner():\n        return KeyError('x')\n    return inner\n",
        "a = ValueError('x')\nb = RuntimeError\n",
    ] {
        assert_eq!(lookups(&compile(source, false)), 0, "{source}");
    }
    // 整数遮蔽不是合法的MIR输入；保留原源码，在类型边界验证明确拒绝。
    let source = "KeyError = 7\nKeyError('x')\n";
    let mut db = ryupy_base::SourceDb::default();
    let file = db.add("keyerror_not_callable.py", source);
    let (parsed, diagnostics) = ryupy_syntax::parse(file, source);
    assert!(diagnostics.is_empty(), "{source}\n{diagnostics:?}");
    let (hir, diagnostics) =
        ryupy_hir::lower_module(&parsed, &ryupy_hir::ModuleInfo::default(), source);
    assert!(diagnostics.is_empty(), "{source}\n{diagnostics:?}");
    let (_, diagnostics) = ryupy_ty::check_module(&hir);
    assert!(
        matches!(diagnostics.as_slice(), [diagnostic]
            if diagnostic.code == Some("E0200") && diagnostic.is_error()),
        "类型检查应只报告非可调用对象E0200：{source}\n{diagnostics:?}"
    );

    let instance = compile("a = KeyError('x')\nb = KeyError\n", true);
    assert_eq!(lookups(&instance), 0);
    assert!(pretty::dump(&instance).contains("InstanceLoadName"));
    assert_eq!(lookups(&compile("raise KeyError\n", false)), 1);
    assert_eq!(
        lookups(&compile(
            "try:\n    pass\nexcept KeyError:\n    pass\n",
            false
        )),
        0,
        "既有静态except目标仍由原类型分派，不冒称动态异常匹配迁移"
    );
}

fn append_lookup(program: &mut Program, args: Vec<Operand>, result: Option<MirType>) {
    let main = &mut program.functions[program.entry];
    let value = Rvalue::Call {
        callee: Callee::Runtime(RuntimeFn::LoadDefaultBuiltin),
        args,
    };
    let statement = if let Some(ty) = result {
        let dest = main.body.locals.alloc(LocalDecl {
            ty,
            name: None,
            is_cell: false,
        });
        Statement::Assign { dest, value }
    } else {
        Statement::Eval(value)
    };
    main.body.blocks[main.body.entry].stmts.push(statement);
}

#[test]
fn default_builtin_fixed_abi_rejects_wrong_arity_argument_result_and_array_call() {
    let name = Operand::Const(Constant::Str("KeyError".into()));
    let mut good = compile("pass\n", false);
    append_lookup(&mut good, vec![name.clone()], Some(MirType::Dynamic));
    assert!(validate::validate(&good).is_ok());
    for args in [
        vec![],
        vec![name.clone(), name.clone()],
        vec![Operand::Const(Constant::Null)],
    ] {
        let mut bad = compile("pass\n", false);
        append_lookup(&mut bad, args, Some(MirType::Dynamic));
        assert!(validate::validate(&bad).is_err());
    }
    for result in [None, Some(MirType::I64)] {
        let mut bad = compile("pass\n", false);
        append_lookup(&mut bad, vec![name.clone()], result);
        assert!(validate::validate(&bad).is_err());
    }
    let mut bad = compile("pass\n", false);
    let main = &mut bad.functions[bad.entry];
    main.body.blocks[main.body.entry]
        .stmts
        .push(Statement::Eval(Rvalue::CallWithArgs {
            callee: RuntimeFn::LoadDefaultBuiltin,
            prefix: vec![name],
            args: vec![],
        }));
    assert!(
        validate::validate(&bad)
            .unwrap_err()
            .iter()
            .any(|e| e.contains("固定Call ABI"))
    );
}

#[test]
fn conditional_map_binding_uses_shared_dynamic_default_lookup_not_unbound_guard() {
    let program = compile(
        "if False:\n    from itertools import imap as map\ndef f():\n    return map(str, [1])\n",
        false,
    );
    let dump = pretty::dump(&program);
    assert!(dump.contains("LoadDeletedModuleName"), "{dump}");
    assert!(!dump.contains("CheckGlobalBound"), "{dump}");
    assert!(
        program
            .globals
            .iter()
            .any(|(_, g)| g.name.ends_with("_map") && g.ty == MirType::Dynamic)
    );
    let instance = compile(
        "if False:\n    map = None\ndef f():\n    return map(str, [1])\n",
        true,
    );
    assert!(!pretty::dump(&instance).contains("LoadDeletedModuleName"));
}
