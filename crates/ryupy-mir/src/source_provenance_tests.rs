//! 来源表必须来自真实前端并随link重定位；负例修改实际压帧/所属关系，不只检查新字段存在。
use crate::*;

fn unit(name: &str) -> (Program, UnitMeta) {
    let source = "def fixed(x: int) -> int:\n    return x + 1\ndef dynamic(x):\n    return x\nprint(fixed(1))\n";
    let mut sources = ryupy_base::SourceDb::default();
    let file = sources.add("__init__.py", source);
    let (parsed, errors) = ryupy_syntax::parse(file, source);
    assert!(errors.is_empty(), "{errors:?}");
    let info = ryupy_hir::ModuleInfo {
        name: name.into(),
        file: "__init__.py".into(),
        resolve_file_at_runtime: true,
        ..Default::default()
    };
    let (hir, errors) = ryupy_hir::lower_module(&parsed, &info, source);
    assert!(errors.is_empty(), "{errors:?}");
    let (types, errors) = ryupy_ty::check_module(&hir);
    assert!(errors.is_empty(), "{errors:?}");
    let (program, exports, errors) = build_program(&hir, &types);
    assert!(errors.is_empty(), "{errors:?}");
    source_provenance::require_program(&program).unwrap();
    (
        program,
        UnitMeta {
            instance_capabilities: Default::default(),
            execution: ModuleExecutionKind::LegacySingleton,
            environment_reason: "source-test".into(),
            location: Default::default(),
            file_backed_by_global: true,
            is_package: false,
            protected_exports: vec![],
            name: name.into(),
            package: "".into(),
            file: "__init__.py".into(),
            exports,
        },
    )
}

#[test]
fn actual_source_has_static_signature_and_dynamic_self() {
    let (program, _) = unit("first");
    let source = source_provenance::for_function(&program.globals, program.entry).unwrap();
    for (id, function) in program.functions.iter() {
        assert_eq!(
            source_provenance::for_function(&program.globals, id),
            Some(source)
        );
        if function.name == "fixed" {
            assert!(function.dynamic_origin.is_none());
        }
        if function.name == "dynamic" {
            assert!(function.dynamic_origin.is_some());
        }
    }
}

#[test]
fn removing_or_replacing_actual_dynamic_push_is_rejected() {
    for erase in [false, true] {
        let (mut program, _) = unit("first");
        let (_, function) = program
            .functions
            .iter_mut()
            .find(|(_, f)| f.name == "dynamic")
            .unwrap();
        for (_, block) in function.body.blocks.iter_mut() {
            for statement in &mut block.stmts {
                if let Statement::Assign {
                    value: Rvalue::Call { callee, args },
                    ..
                } = statement
                    && *callee == Callee::Runtime(RuntimeFn::TracePushWithOrigin)
                {
                    if erase {
                        *callee = Callee::Runtime(RuntimeFn::TracePush);
                        args.pop();
                    } else {
                        args[2] = Operand::Global(function.signature_global.unwrap());
                    }
                }
            }
        }
        assert!(validate::validate(&program).is_err());
    }
}

#[test]
fn missing_source_or_ordinary_source_write_cannot_downgrade() {
    for remove in [false, true] {
        let (mut program, _) = unit("first");
        let source = source_provenance::for_function(&program.globals, program.entry).unwrap();
        if remove {
            program.globals[source].storage = GlobalStorage::LegacyStatic;
        } else {
            let module = &mut program.functions[program.entry];
            module.body.blocks[module.body.entry]
                .stmts
                .push(Statement::SetGlobal {
                    dest: source,
                    value: Operand::Const(Constant::Null),
                });
        }
        assert!(validate::validate(&program).is_err());
    }
}

#[test]
fn link_remaps_actual_unit_members_despite_identical_filenames() {
    let first = unit("first");
    let second = unit("second");
    let first_count = first.0.functions.len();
    let second_name = match &second.0.globals
        [source_provenance::for_function(&second.0.globals, second.0.entry).unwrap()]
    .storage
    {
        GlobalStorage::CodeSource {
            legacy_entry: Some(value),
            ..
        } => value.name_global,
        _ => panic!("真实测试unit必须有Legacy入口"),
    };
    let first_globals = first.0.globals.len();
    let linked = link(vec![first, second], 0);
    validate::validate_linked_exports(&linked).unwrap();
    let second = linked.modules.iter().nth(1).unwrap().1;
    let source = source_provenance::for_function(&linked.globals, second.init).unwrap();
    let GlobalStorage::CodeSource {
        entry,
        functions,
        legacy_entry: Some(initial),
    } = &linked.globals[source].storage
    else {
        panic!("缺真实来源");
    };
    assert_eq!(*entry, second.init);
    assert!(
        functions
            .iter()
            .all(|f| f.into_raw().into_u32() as usize >= first_count)
    );
    assert_eq!(
        initial.name_global.into_raw().into_u32() as usize,
        first_globals + second_name.into_raw().into_u32() as usize
    );
}

#[test]
fn changing_one_signature_to_another_unit_is_rejected() {
    let mut linked = link(vec![unit("first"), unit("second")], 0);
    let sources: Vec<_> = linked
        .globals
        .iter()
        .filter_map(|(id, g)| matches!(g.storage, GlobalStorage::CodeSource { .. }).then_some(id))
        .collect();
    let wrong = match &linked.globals[sources[1]].storage {
        GlobalStorage::CodeSource { functions, .. } => functions[0],
        _ => unreachable!(),
    };
    if let GlobalStorage::CodeSource { functions, .. } = &mut linked.globals[sources[0]].storage {
        functions[0] = wrong;
    }
    assert!(validate::validate_linked_exports(&linked).is_err());
}

#[test]
fn erased_complete_source_chain_only_survives_explicit_legacy_validation() {
    let (mut program, meta) = unit("first");
    for (_, global) in program.globals.iter_mut() {
        if matches!(global.storage, GlobalStorage::CodeSource { .. }) {
            global.storage = GlobalStorage::LegacyStatic;
        }
    }
    for (_, function) in program.functions.iter_mut() {
        for (_, block) in function.body.blocks.iter_mut() {
            for statement in &mut block.stmts {
                let Statement::Assign {
                    value: Rvalue::Call { callee, args },
                    ..
                } = statement
                else {
                    continue;
                };
                if *callee == Callee::Runtime(RuntimeFn::ModuleExecutionSourceWithCode) {
                    *callee = Callee::Runtime(RuntimeFn::ModuleExecutionSource);
                    *args = vec![Operand::Const(Constant::Null)];
                } else if *callee == Callee::Runtime(RuntimeFn::TracePushWithOrigin)
                    && function.signature_global.is_some()
                    && function.dynamic_origin.is_none()
                {
                    *callee = Callee::Runtime(RuntimeFn::TracePush);
                    args.pop();
                }
            }
        }
    }
    // 这确实是旧协议的合法手工MIR；源码调用链必须另有不可由残留表关闭的require。
    validate::validate(&program).unwrap();
    assert!(source_provenance::require_program(&program).is_err());
    let linked = link(vec![(program, meta)], 0);
    validate::validate_linked_exports(&linked).unwrap();
    assert!(source_provenance::require_linked(&linked).is_err());
}

#[test]
fn private_signature_export_and_legacy_metadata_override_are_rejected() {
    for signature_export in [false, true] {
        let mut linked = link(vec![unit("first")], 0);
        if signature_export {
            let signature = linked
                .functions
                .iter()
                .find_map(|(_, f)| f.signature_global)
                .unwrap();
            linked.modules[linked.entry].exports.push(ModuleExport {
                name: "leaked_signature".into(),
                value: signature,
                guard: ExportGuard::ValueSentinel,
            });
        } else {
            linked.modules[linked.entry].file_backed_by_global = false;
        }
        assert!(source_provenance::require_linked(&linked).is_err());
    }
}

#[test]
fn legacy_name_cannot_publish_before_push_or_after_callback() {
    for callback in [false, true] {
        let (mut program, _) = unit("first");
        let function = &mut program.functions[program.entry];
        let statements = &mut function.body.blocks[function.body.entry].stmts;
        let push = statements
            .iter()
            .position(|s| {
                matches!(
                    s,
                    Statement::Assign {
                        value: Rvalue::Call {
                            callee: Callee::Runtime(RuntimeFn::TracePushWithOrigin),
                            ..
                        },
                        ..
                    }
                )
            })
            .unwrap();
        if callback {
            statements.insert(
                push,
                Statement::Eval(Rvalue::Call {
                    callee: Callee::Runtime(RuntimeFn::TracePopTo),
                    args: vec![Operand::Const(Constant::I64(0))],
                }),
            );
        } else {
            let statement = statements.remove(push);
            statements.push(statement);
        }
        assert!(source_provenance::require_program(&program).is_err());
    }
}

#[test]
fn legacy_export_requires_real_reset_and_bound_publication() {
    for remove_publish in [false, true] {
        let mut linked = link(vec![unit("first")], 0);
        let module = &linked.modules[linked.entry];
        let export = module
            .exports
            .iter()
            .find(|e| e.name == "__name__")
            .unwrap();
        let (slot, value) = match export.guard {
            ExportGuard::BoundFlag(flag) => (flag, Constant::Bool(remove_publish)),
            ExportGuard::ValueSentinel => (export.value, Constant::Null),
            _ => panic!("真实入口必须有绑定协议"),
        };
        let function = &mut linked.functions[module.init];
        function.body.blocks[function.body.entry].stmts.retain(|s| {
            s != &Statement::SetGlobal {
                dest: slot,
                value: Operand::Const(value.clone()),
            }
        });
        assert!(source_provenance::require_linked(&linked).is_err());
    }
}
