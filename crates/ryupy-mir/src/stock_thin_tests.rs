//! 真实HIR→MIR薄函数合同：读名、lease、真实体、工厂和超域诊断均检查结构。
use crate::*;
#[path = "stock_thin_expression_tests.rs"]
mod expressions;
fn compile(source: &str) -> (Program, Vec<ModuleExport>) {
    let mut db = ryupy_base::SourceDb::default();
    let file = db.add("thin.py", source);
    let (parsed, diagnostics) = ryupy_syntax::parse(file, source);
    assert!(diagnostics.is_empty(), "{diagnostics:?}");
    let info = ryupy_hir::ModuleInfo {
        stock_thin_functions: true,
        instance_globals: true,
        instance_capabilities: ryupy_hir::stock_entry_capabilities(),
        name: "__main__".into(),
        file: "thin.py".into(),
        ..Default::default()
    };
    let (hir, diagnostics) = ryupy_hir::lower_module(&parsed, &info, source);
    assert!(diagnostics.is_empty(), "{diagnostics:?}");
    assert!(hir.info.stock_thin_functions && hir.info.instance_capabilities.entry_namespace);
    assert!(!ryupy_hir::stock_entry_supported(&parsed));
    let (types, diagnostics) = ryupy_ty::check_module(&hir);
    assert!(diagnostics.is_empty(), "{diagnostics:?}");
    let (mut program, exports, diagnostics) = build_program(&hir, &types);
    assert!(diagnostics.is_empty(), "{diagnostics:?}");
    passes::run_pipeline(&mut program, &passes::default_pipeline()).unwrap();
    source_provenance::require_program(&program).unwrap();
    (program, exports)
}
#[test]
fn stock_thin_real_body_uses_lease_for_every_global_name() {
    for returned in ["x", "value", "abs", "len", "f", "other", "missing"] {
        let source = format!(
            "value = 1\ndef other(x):\n    return x\ndef f(x):\n    return {returned}\nprint(f(None))\n"
        );
        let (program, _) = compile(&source);
        let (id, f) = program
            .functions
            .iter()
            .find(|(_, f)| f.name == "f")
            .unwrap();
        assert_eq!(
            f.module_state_source,
            Some(InstanceStateSource::StockThinLease)
        );
        assert_eq!(f.params.len(), 2);
        assert_eq!(f.params[1], f.module_state.unwrap());
        assert!(f.dynamic_origin.is_none() && f.signature_global.is_none() && f.env.is_none());
        let mut loads = vec![];
        for (_, block) in f.body.blocks.iter() {
            for stmt in &block.stmts {
                if let Statement::Assign { value, .. } | Statement::Eval(value) = stmt {
                    if let Rvalue::Call { callee, args } = value {
                        assert_eq!(
                            *callee,
                            Callee::Runtime(RuntimeFn::StockThinLoadGlobal),
                            "薄体不得携带旧Context/owner helper: {value:?}"
                        );
                        assert_eq!(args[0], Operand::Local(f.params[1]));
                        loads.push(args[1].clone());
                    }
                    assert!(!matches!(value, Rvalue::MakeFunction { .. }));
                }
            }
        }
        assert_eq!(
            loads,
            if returned == "x" {
                vec![]
            } else {
                vec![Operand::Const(Constant::Str(returned.into()))]
            }
        );
        let main = &program.functions[program.entry];
        assert!(
            main.body
                .blocks
                .iter()
                .any(|(_, b)| b.stmts.iter().any(|s| matches!(s,
            Statement::Assign { value: Rvalue::MakeStockThinFunction { func, module_context }, .. }
            if *func == id && *module_context == Operand::Local(main.module_state.unwrap()))))
        );
    }
}

#[test]
fn stock_thin_local_identity_bodies_use_static_cfg_and_neutral_values() {
    for source in [
        include_str!("../../../tests/cases/stock_thin_compiler/local_identity.py"),
        include_str!("../../../tests/cases/stock_thin_compiler/local_unbound.py"),
        include_str!("../../../tests/cases/stock_thin_compiler/identity_order.py"),
    ] {
        let (program, _) = compile(source);
        validate::validate(&program).unwrap();
        for (id, function) in program
            .functions
            .iter()
            .filter(|(id, _)| *id != program.entry)
        {
            assert_ne!(id, program.entry);
            assert_eq!(function.core, FunctionCore::StockThinStatic);
            assert!(function.env.is_none() && function.body.regions.is_empty());
        }
    }
    let (program, _) = compile(
        "shadow = None\ndef f(x):\n    result = shadow\n    shadow = x\n    return result\n",
    );
    let (_, function) = program
        .functions
        .iter()
        .find(|(_, f)| f.name == "f")
        .unwrap();
    let calls: Vec<_> = function
        .body
        .blocks
        .iter()
        .flat_map(|(_, b)| &b.stmts)
        .filter_map(|s| match s {
            Statement::Assign {
                value: Rvalue::Call { callee, .. },
                ..
            } => Some(*callee),
            _ => None,
        })
        .collect();
    assert!(calls.contains(&Callee::Runtime(RuntimeFn::LoadChecked)));
    assert!(!calls.contains(&Callee::Runtime(RuntimeFn::StockThinLoadGlobal)));

    let (program, _) = compile("def f(x):\n    return left is right\n");
    let (_, function) = program
        .functions
        .iter()
        .find(|(_, f)| f.name == "f")
        .unwrap();
    let loads: Vec<_> = function
        .body
        .blocks
        .iter()
        .flat_map(|(_, b)| &b.stmts)
        .filter_map(|s| match s {
            Statement::Assign {
                value:
                    Rvalue::Call {
                        callee: Callee::Runtime(RuntimeFn::StockThinLoadGlobal),
                        args,
                    },
                ..
            } => Some(args[1].clone()),
            _ => None,
        })
        .collect();
    assert_eq!(
        loads,
        vec![
            Operand::Const(Constant::Str("left".into())),
            Operand::Const(Constant::Str("right".into()))
        ]
    );
}
#[test]
fn stock_thin_rejects_each_shape_before_lowering() {
    let fixture = include_str!("../../../tests/cases/stock_thin_compiler/reject_shapes.py");
    let mut cases: Vec<_> = fixture
        .split("# case:")
        .skip(1)
        .map(|s| s.split_once('\n').unwrap().1.to_string())
        .collect();
    cases.extend(
        [
            "def f(y):\n    return y\n",
            "def f(x, /):\n    return x\n",
            "def f(*, x):\n    return x\n",
            "def f(*x):\n    return x\n",
            "def f(**x):\n    return x\n",
            "def f[T](x):\n    return x\n",
            "if True:\n    def f(x):\n        return x\n",
            "f = lambda x: x\n",
            "def f(x):\n    if x.attr:\n        return x\n",
            "def f(x):\n    if x is None:\n        return x\n    else:\n        return x @ x\n",
            "def f(x):\n    alias = x(value=x)\n    return alias\n",
            "def f(x):\n    return x if x is None else __debug__\n",
            "def f(x):\n    if x is None:\n        return x\n    elif __debug__ is x:\n        return x\n",
            "def f(x):\n    return x is None is x\n",
            "def f(x):\n    x.attr = x\n    return x\n",
            "def f(x):\n    global value\n    value = x\n    return value\n",
            "def f(x):\n    return x\n    while x is None:\n        pass\n",
            "def f(x):\n    return x(*x)\n",
            "def f(x):\n    return x(**x)\n",
            "def f(x):\n    return x(unknown=x)\n",
            "def f(x):\n    x += x\n    return x\n",
            "def f(x):\n    return 1j\n",
            "def f(x):\n    return b'bytes'\n",
            "def f(x):\n    return (value := x)\n",
        ]
        .map(str::to_string),
    );
    for source in cases {
        let mut db = ryupy_base::SourceDb::default();
        let file = db.add("negative.py", &source);
        let (parsed, errors) = ryupy_syntax::parse(file, &source);
        if !errors.is_empty() {
            assert!(source.contains("[T]"), "{source}: {errors:?}");
            continue;
        }
        let (_, errors) = ryupy_hir::lower_module(
            &parsed,
            &ryupy_hir::ModuleInfo {
                stock_thin_functions: true,
                instance_globals: true,
                ..Default::default()
            },
            &source,
        );
        assert!(!errors.is_empty(), "必须诊断: {source}");
    }
}

#[test]
fn stock_thin_rejects_old_context_helpers_and_lease_escape() {
    for mode in 0..3 {
        let (mut program, _) = compile("def f(x):\n    return x\n");
        let (_, function) = program
            .functions
            .iter_mut()
            .find(|(_, f)| f.name == "f")
            .unwrap();
        let lease = function.module_state.unwrap();
        let x = function.params[0];
        let block = &mut function.body.blocks[function.body.entry];
        match mode {
            0 => block.stmts.insert(
                0,
                Statement::Assign {
                    dest: x,
                    value: Rvalue::Call {
                        callee: Callee::Runtime(RuntimeFn::InstanceExecutionSource),
                        args: vec![Operand::Local(lease)],
                    },
                },
            ),
            1 => block.stmts.insert(
                0,
                Statement::Assign {
                    dest: x,
                    value: Rvalue::Use(Operand::Local(lease)),
                },
            ),
            _ => block.terminator = Terminator::Return(Some(Operand::Local(lease))),
        }
        assert!(validate::validate(&program).is_err());
    }
}
