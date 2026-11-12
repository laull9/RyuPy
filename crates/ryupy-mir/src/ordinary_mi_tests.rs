//! 普通MI真实HIR→MIR：只新族延后发布，冷入口固定签名与Finalize顺序不能省略。
use crate::*;
fn compile(source: &str) -> Program {
    let mut db = ryupy_base::SourceDb::default();
    let file = db.add("mi.py", source);
    let (parsed, diagnostics) = ryupy_syntax::parse(file, source);
    assert!(diagnostics.is_empty(), "{diagnostics:?}");
    let (hir, diagnostics) = ryupy_hir::lower_module(&parsed, &Default::default(), source);
    assert!(diagnostics.is_empty(), "{diagnostics:?}");
    let (types, diagnostics) = ryupy_ty::check_module(&hir);
    assert!(diagnostics.is_empty(), "{diagnostics:?}");
    let (program, _, diagnostics) = build_program(&hir, &types);
    assert!(diagnostics.is_empty(), "{diagnostics:?}");
    validate::validate(&program).unwrap();
    program
}
#[test]
fn mi_and_single_descendant_publish_only_after_finalization_and_guard() {
    let mut program = compile(
        "class A:\n    pass\nclass B:\n    pass\nclass D(A,B):\n    value=1\nclass E(D):\n    pass\n",
    );
    let mut prepared = 0;
    for (_, block) in program.functions[program.entry].body.blocks.iter() {
        for (index, statement) in block.stmts.iter().enumerate() {
            let Statement::Assign {
                dest,
                value:
                    Rvalue::Call {
                        callee: Callee::Runtime(RuntimeFn::PrepareOrdinaryMiClass),
                        args,
                    },
            } = statement
            else {
                continue;
            };
            prepared += 1;
            assert_eq!(args.len(), 2);
            let finalized = block.stmts.iter().position(|statement| matches!(statement,
                Statement::Eval(Rvalue::Call {callee: Callee::Runtime(RuntimeFn::FinalizeOrdinaryClass), args})
                if args == &vec![Operand::Local(*dest)])).unwrap();
            let guard = block.stmts.iter().position(|statement| matches!(statement,
                Statement::Eval(Rvalue::Call {callee: Callee::Runtime(RuntimeFn::InstanceClassCellGuard), args})
                if args == &vec![Operand::Local(*dest)])).unwrap();
            let published = block
                .stmts
                .iter()
                .position(|statement| {
                    matches!(statement,
                Statement::SetGlobal {value: Operand::Local(local), ..} if local == dest)
                })
                .unwrap();
            assert!(index < finalized && finalized < guard && guard < published);
        }
    }
    assert_eq!(prepared, 2);
    passes::run_pipeline(&mut program, &passes::default_pipeline()).unwrap();
    validate::validate(&program).unwrap();
}

#[test]
fn runtime_single_base_uses_separate_cold_abi_and_deferred_publication() {
    let mut program = compile(
        "from elsewhere import Base\nclass Derived(Base):\n    seen=Derived\nclass Child(Derived):\n    pass\n",
    );
    let mut prepared = 0;
    for (_, block) in program.functions[program.entry].body.blocks.iter() {
        for (index, statement) in block.stmts.iter().enumerate() {
            let Statement::Assign {
                dest,
                value:
                    Rvalue::Call {
                        callee: Callee::Runtime(RuntimeFn::PrepareRuntimeDerivedClass),
                        args,
                    },
            } = statement
            else {
                continue;
            };
            prepared += 1;
            assert_eq!(args.len(), 2);
            assert!(matches!(&args[1], Operand::Array(bases) if bases.len() == 1));
            let finalized = block.stmts.iter().position(|statement| matches!(statement,
                Statement::Eval(Rvalue::Call {callee: Callee::Runtime(RuntimeFn::FinalizeOrdinaryClass), args})
                if args == &vec![Operand::Local(*dest)])).unwrap();
            let guarded = block.stmts.iter().position(|statement| matches!(statement,
                Statement::Eval(Rvalue::Call {callee: Callee::Runtime(RuntimeFn::InstanceClassCellGuard), args})
                if args == &vec![Operand::Local(*dest)])).unwrap();
            let published = block
                .stmts
                .iter()
                .position(|statement| {
                    matches!(statement,
                Statement::SetGlobal {value: Operand::Local(local), ..} if local == dest)
                })
                .unwrap();
            assert!(index < finalized && finalized < guarded && guarded < published);
        }
    }
    assert_eq!(prepared, 2);
    assert!(!format!("{program:?}").contains("PrepareOrdinaryMiClass"));
    passes::run_pipeline(&mut program, &passes::default_pipeline()).unwrap();
    validate::validate(&program).unwrap();
}
