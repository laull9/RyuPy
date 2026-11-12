//! 跨模块动态单基与同模块后代的精确发布结构；替代生成MIR golden，不记录编译产物。
use ryupy_mir::{Callee, Constant, Operand, RuntimeFn, Rvalue, Statement};

#[test]
fn imported_base_and_descendant_prepare_once_and_publish_only_after_finalize_guard() {
    let source = "from package import Base as Alias\nclass Child(Alias):\n    pass\nclass Grandchild(Child):\n    pass\n";
    let mut db = ryupy_base::SourceDb::default();
    let (parsed, diagnostics) = ryupy_syntax::parse(db.add("runtime_base.py", source), source);
    assert!(diagnostics.is_empty());
    let (hir, diagnostics) = ryupy_hir::lower_module(&parsed, &Default::default(), source);
    assert!(diagnostics.is_empty(), "{diagnostics:?}");
    let (types, diagnostics) = ryupy_ty::check_module(&hir);
    assert!(diagnostics.is_empty(), "{diagnostics:?}");
    let (program, _, diagnostics) = ryupy_mir::build_program(&hir, &types);
    assert!(diagnostics.is_empty(), "{diagnostics:?}");
    let statements: Vec<_> = program.functions[program.entry]
        .body
        .blocks
        .iter()
        .flat_map(|(_, block)| &block.stmts)
        .collect();
    let calls = |wanted| -> Vec<_> {
        statements
            .iter()
            .enumerate()
            .filter_map(|(index, statement)| {
                let value = match statement {
                    Statement::Assign { value, .. } | Statement::Eval(value) => value,
                    _ => return None,
                };
                match value {
                    Rvalue::Call {
                        callee: Callee::Runtime(function),
                        args,
                    } if *function == wanted => Some((index, args)),
                    _ => None,
                }
            })
            .collect()
    };
    assert!(calls(RuntimeFn::MakeClass).is_empty());
    assert!(calls(RuntimeFn::PrepareOrdinaryMiClass).is_empty());
    let validate = calls(RuntimeFn::ValidateOrdinaryBase);
    let prepare = calls(RuntimeFn::PrepareRuntimeDerivedClass);
    let finalize = calls(RuntimeFn::FinalizeOrdinaryClass);
    let guard = calls(RuntimeFn::InstanceClassCellGuard);
    assert_eq!(
        (validate.len(), prepare.len(), finalize.len(), guard.len()),
        (2, 2, 2, 2)
    );
    for (index, (class_id, class)) in hir.classes.iter().enumerate() {
        assert!(class.deferred_runtime_base);
        let name = ["Child", "Grandchild"][index];
        assert_eq!(class.name.as_str(), name);
        let Statement::Assign {
            dest: validated, ..
        } = statements[validate[index].0]
        else {
            panic!("基值验证必须保存结果")
        };
        let Statement::Assign { dest: prepared, .. } = statements[prepare[index].0] else {
            panic!("准备必须保存原类")
        };
        assert!(
            matches!(&prepare[index].1[..], [Operand::Const(Constant::Str(actual)), Operand::Array(bases)]
            if actual == name && matches!(&bases[..], [Operand::Local(id)] if id == validated))
        );
        for args in [finalize[index].1, guard[index].1] {
            assert!(matches!(&args[..], [Operand::Local(id)] if id == prepared));
        }
        let binding = class.decorated_binding.expect("独立公开槽");
        let public_name = format!("__mv{}_{}", binding.into_raw().into_u32(), name);
        let private_name = format!("__class_{}", class_id.into_raw().into_u32());
        let global = |name: &str| {
            program
                .globals
                .iter()
                .find(|(_, item)| item.name == name)
                .unwrap()
                .0
        };
        let writes = |target| -> Vec<_> {
            statements
                .iter()
                .enumerate()
                .filter_map(|(position, statement)| match statement {
                    Statement::SetGlobal { dest, value }
                        if *dest == target && !matches!(value, Operand::Const(Constant::Null)) =>
                    {
                        assert!(matches!(value, Operand::Local(id) if id == prepared));
                        Some(position)
                    }
                    _ => None,
                })
                .collect()
        };
        let private = writes(global(&private_name));
        let public = writes(global(&public_name));
        assert_eq!((private.len(), public.len()), (1, 1));
        assert!(
            validate[index].0 < prepare[index].0
                && prepare[index].0 < finalize[index].0
                && finalize[index].0 < guard[index].0
                && guard[index].0 < private[0]
                && private[0] < public[0]
        );
        if index == 0 {
            assert!(public[0] < validate[1].0);
        }
    }
}
