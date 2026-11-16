//! 实际MIR边mutation及默认优化保持性；不等同Python或Native实测。
use crate::direct_globals_tests::compile;
use crate::*;
fn reject(program: &Program) {
    assert!(
        validate::validate(program)
            .unwrap_err()
            .iter()
            .any(|e| e.contains("直接globals"))
    );
}
#[test]
fn direct_globals_rejects_changed_snapshot_producer() {
    let mut program = compile();
    let mut changed = 0;
    for (_, f) in program.functions.iter_mut() {
        for fact in &f.body.direct_globals_contracts {
            for (_, b) in f.body.blocks.iter_mut() {
                for stmt in &mut b.stmts {
                    if let Statement::Assign { dest, value } = stmt
                        && *dest == fact.snapshot
                    {
                        *value = Rvalue::Use(Operand::Const(Constant::None));
                        changed += 1;
                    }
                }
            }
        }
    }
    assert_eq!(changed, 2);
    reject(&program);
}
#[test]
fn direct_globals_rejects_unproven_or_duplicate_helper() {
    let mut program = compile();
    let (_, f) = program
        .functions
        .iter_mut()
        .find(|(_, f)| !f.body.direct_globals_contracts.is_empty())
        .unwrap();
    let fact = f.body.direct_globals_contracts[0].clone();
    let result = f.body.locals.alloc(LocalDecl {
        ty: MirType::Dynamic,
        name: None,
        is_cell: false,
    });
    let block = &mut f.body.blocks[f.body.entry];
    block.stmts.push(Statement::Assign {
        dest: result,
        value: Rvalue::Call {
            callee: Callee::Runtime(RuntimeFn::InstanceDirectGlobals),
            args: vec![Operand::Local(fact.context), Operand::Local(fact.snapshot)],
        },
    });
    reject(&program);
}
#[test]
fn direct_globals_rejects_transferred_source_and_repeated_writes() {
    for transfer in [false, true] {
        let mut program = compile();
        let (_, f) = program
            .functions
            .iter_mut()
            .find(|(_, f)| !f.body.direct_globals_contracts.is_empty())
            .unwrap();
        let fact = f.body.direct_globals_contracts[0].clone();
        if transfer {
            f.body.direct_globals_contracts[0].result = fact.snapshot;
        } else {
            f.body.blocks[f.body.entry].stmts.push(Statement::Assign {
                dest: fact.snapshot,
                value: Rvalue::Use(fact.callee_value),
            });
        }
        reject(&program);
    }
}
#[test]
fn direct_globals_default_pipeline_keeps_actual_binding() {
    let mut program = compile();
    assert!(validate::validate(&program).is_ok());
    passes::run_pipeline(&mut program, &passes::default_pipeline()).unwrap();
    assert!(validate::validate(&program).is_ok());
    assert_eq!(
        program
            .functions
            .iter()
            .map(|(_, f)| f.body.direct_globals_contracts.len())
            .sum::<usize>(),
        2
    );
}
#[test]
fn direct_globals_link_remaps_global_operand_and_keeps_local_binding() {
    let mut a = compile();
    let b = compile();
    let source = a.globals.alloc(GlobalDecl {
        storage: GlobalStorage::LegacyStatic,
        name: "callee_fixture".into(),
        ty: MirType::Dynamic,
    });
    for (_, f) in a.functions.iter_mut() {
        for fact in &mut f.body.direct_globals_contracts {
            fact.callee_value = Operand::Global(source);
            for (_, block) in f.body.blocks.iter_mut() {
                for stmt in &mut block.stmts {
                    if let Statement::Assign { dest, value } = stmt
                        && *dest == fact.snapshot
                    {
                        *value = Rvalue::Use(Operand::Global(source));
                    }
                }
            }
        }
    }
    let meta = |p: &Program, name: &str| UnitMeta {
        instance_capabilities: p.functions[p.entry].instance_capabilities,
        execution: ModuleExecutionKind::InstanceGlobals,
        environment_reason: "explicit-instance".into(),
        location: ModuleLocation::LegacyDiagnostic,
        file_backed_by_global: true,
        is_package: false,
        protected_exports: vec![],
        name: name.into(),
        package: "".into(),
        file: "fixture.py".into(),
        exports: vec![],
    };
    let am = meta(&a, "a");
    let bm = meta(&b, "b");
    let linked = link(vec![(b, bm), (a, am)], 0);
    let mut errors = vec![];
    crate::direct_globals_contract::validate_functions(&linked.functions, &mut errors);
    assert!(errors.is_empty(), "{errors:?}");
    let mapped: Vec<_> = linked
        .functions
        .iter()
        .flat_map(|(_, f)| &f.body.direct_globals_contracts)
        .filter_map(|c| {
            if let Operand::Global(g) = c.callee_value {
                Some(g)
            } else {
                None
            }
        })
        .collect();
    assert_eq!(mapped.len(), 2);
    assert!(mapped.iter().all(|g| *g != source));
}

#[test]
fn direct_globals_rejects_ordinary_call_promoted_to_helper() {
    let mut program = compile();
    let (_, function) = program
        .functions
        .iter_mut()
        .find(|(_, f)| f.name == "h")
        .unwrap();
    let state = function.module_state.unwrap();
    let mut changed = 0;
    for (_, block) in function.body.blocks.iter_mut() {
        for statement in &mut block.stmts {
            if let Statement::Assign { value, .. } = statement {
                let actual = match value {
                    Rvalue::InstanceZeroCall { callee, .. }
                    | Rvalue::DynCallValue { callee, .. } => Some(callee.clone()),
                    _ => None,
                };
                if let Some(callee) = actual {
                    *value = Rvalue::Call {
                        callee: Callee::Runtime(RuntimeFn::InstanceDirectGlobals),
                        args: vec![Operand::Local(state), callee],
                    };
                    changed += 1;
                }
            }
        }
    }
    assert_eq!(changed, 1);
    reject(&program);
}
