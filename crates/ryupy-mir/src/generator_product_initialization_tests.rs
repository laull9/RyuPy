//! 真实前端/七pass前言为正基底；直接变异初始化合同，负例不得由其它guard或panic代答。
use crate::generator_product_identity::UnchangedHigh;
use crate::*;

#[path = "generator_product_initialization_fixture.rs"]
pub(super) mod fixture;
use fixture::Fixture;

#[test]
fn actual_prefix_preserves_resets_opaque_path_and_optional_metadata() {
    for runtime_file in [false, true] {
        let mut f = Fixture::new(runtime_file);
        assert_eq!(f.exports.iter().any(|e| e.name == "__path__"), runtime_file);
        let slot = f.slot("__name__");
        assert!(f.stmts().iter().any(|s| matches!(s, Statement::SetGlobal {
            dest, value: Operand::Const(Constant::Null) } if *dest == slot)));
        if runtime_file {
            let at = f.publish("__path__");
            assert!(matches!(
                f.stmts()[at],
                Statement::SetGlobal {
                    value: Operand::Local(_),
                    ..
                }
            ));
        }
        for boxed in [false, true] {
            let at = f.publish("__name__");
            let Statement::SetGlobal { value: input, .. } = f.stmts()[at].clone() else {
                panic!("publish")
            };
            let alias = f.local();
            f.stmts().insert(
                at,
                Statement::Assign {
                    dest: alias,
                    value: if boxed {
                        Rvalue::Box {
                            value: input,
                            from: MirType::Dynamic,
                        }
                    } else {
                        Rvalue::Use(input)
                    },
                },
            );
            let Statement::SetGlobal { value, .. } = &mut f.stmts()[at + 1] else {
                panic!("publish")
            };
            *value = Operand::Local(alias);
            f.initialization.inspect(&f.program, &f.exports).unwrap();
        }
    }
}

#[test]
fn missing_wrong_duplicate_and_late_role_publications_are_rejected() {
    for mutation in 0..6 {
        let mut f = Fixture::new(true);
        let at = f.publish("__name__");
        let statement = f.stmts()[at].clone();
        match mutation {
            0 => {
                f.stmts().remove(at);
            }
            1 => {
                f.stmts().insert(at, statement);
            }
            2 => {
                let slot = f.slot("__name__");
                f.stmts().push(Statement::SetGlobal {
                    dest: slot,
                    value: Operand::Const(Constant::Null),
                });
            }
            3 => {
                let Statement::SetGlobal { value, .. } = &mut f.stmts()[at] else {
                    panic!("publish")
                };
                *value = Operand::Const(Constant::None);
                f.reject("producer/helper/key/常量不符");
                continue;
            }
            4 => {
                let other = f.slot("__package__");
                let Statement::SetGlobal { dest, .. } = &mut f.stmts()[at] else {
                    panic!("publish")
                };
                *dest = other;
            }
            _ => {
                let target = f.plan.functions()[0];
                let body = &mut f.program.functions[target].body;
                body.blocks[body.entry].stmts.push(statement);
            }
        }
        f.reject("唯一Null reset随后唯一发布");
    }
    let mut f = Fixture::new(true);
    let at = f.publish("__name__");
    let slot = f.slot("__name__");
    let reset = f
        .stmts()
        .iter()
        .position(|s| matches!(s, Statement::SetGlobal { dest, .. } if *dest == slot))
        .unwrap();
    let statement = f.stmts().remove(reset);
    f.stmts().insert(at, statement);
    f.reject("唯一Null reset随后唯一发布");
}

#[test]
fn helper_keys_modules_source_and_opaque_values_cannot_be_forged_or_escape() {
    for mutation in 0..5 {
        let mut f = Fixture::new(true);
        let at = f.call(RuntimeFn::ModuleMetadata);
        let Statement::Assign {
            value: Rvalue::Call { callee, args },
            ..
        } = &mut f.stmts()[at]
        else {
            panic!("metadata")
        };
        match mutation {
            0 => args[0] = Operand::Const(Constant::Str("other".into())),
            1 => args[1] = Operand::Const(Constant::Str("__name__".into())),
            2 => *callee = Callee::Runtime(RuntimeFn::ModuleFile),
            3 => {
                *callee = Callee::Runtime(RuntimeFn::DynNone);
                args.clear();
            }
            _ => args[1] = Operand::Const(Constant::Str("__path__".into())),
        }
        f.reject(match mutation {
            0 | 1 => "metadata实际module/key错误",
            2 | 3 => "前言真实局部类型不符",
            _ => "producer/helper/key/常量不符",
        });
    }
    let mut f = Fixture::new(true);
    let at = f.publish("__path__");
    let Statement::SetGlobal { value, .. } = &mut f.stmts()[at] else {
        panic!("path")
    };
    *value = Operand::Const(Constant::None);
    f.reject("producer/helper/key/常量不符");
    for key in ["__file__", "__path__"] {
        let mut f = Fixture::new(true);
        let at = f.publish(key);
        let Statement::SetGlobal { value, .. } = f.stmts()[at].clone() else {
            panic!("store")
        };
        f.stmts().push(Statement::Eval(Rvalue::Use(value)));
        f.reject("producer局部向已核store之外外逃");
        let mut f = Fixture::new(true);
        let slot = f.slot(key);
        f.stmts()
            .push(Statement::Eval(Rvalue::Use(Operand::Global(slot))));
        f.reject("opaque file/metadata初值");
    }
    let mut f = Fixture::new(true);
    let at = f.call(RuntimeFn::ModuleExecutionSourceWithCode);
    let Statement::Assign {
        value: Rvalue::Call { args, .. },
        ..
    } = &mut f.stmts()[at]
    else {
        panic!("source")
    };
    args[0] = Operand::Const(Constant::Str("other".into()));
    f.reject("Source真实参数不符");
}

#[test]
fn role_guards_declarations_alias_exports_and_bad_ids_are_errors() {
    for mutation in 0..5 {
        let mut f = Fixture::new(true);
        let slot = f.slot("__name__");
        let export = f.exports.iter_mut().find(|e| e.name == "__name__").unwrap();
        match mutation {
            0 => export.guard = ExportGuard::None,
            1 => export.guard = ExportGuard::BoundFlag(slot),
            2 => export.value = GlobalId::from_raw(999_999.into()),
            3 => f.program.globals[slot].ty = MirType::Str,
            _ => {
                f.exports.push(ModuleExport {
                    name: "alias".into(),
                    value: slot,
                    guard: ExportGuard::ValueSentinel,
                });
                f.reject("角色槽与其它导出/私有角色冲突");
                continue;
            }
        }
        f.reject("角色声明或导出协议改变");
    }
    let mut f = Fixture::new(true);
    f.program.entry = FnId::from_raw(999_999.into());
    f.reject("实际初始化入口或来源文件改变");
    let mut f = Fixture::new(true);
    let at = f.publish("__name__");
    let Statement::SetGlobal { value, .. } = &mut f.stmts()[at] else {
        panic!("store")
    };
    *value = Operand::Local(LocalId::from_raw(999_999.into()));
    f.reject("producer/helper/key/常量不符");
    let mut f = Fixture::new(true);
    let target = f.plan.functions()[0];
    f.program.functions[target].function_global = Some(f.slot("__name__"));
    f.reject("角色槽与其它导出/私有角色冲突");
}

#[test]
fn explicit_reads_definition_freezes_and_implicit_class_reads_require_publication() {
    let mut f = Fixture::new(true);
    let slot = f.slot("__name__");
    let at = f.publish("__name__");
    f.stmts()
        .insert(at, Statement::Eval(Rvalue::Use(Operand::Global(slot))));
    f.reject("真实读取/冻结/MakeClass早于发布");
    let mut f = Fixture::new(true);
    let call = f.call(RuntimeFn::FunctionModule);
    let Statement::Eval(Rvalue::Call { args, .. }) = &f.stmts()[call] else {
        panic!("module")
    };
    let Operand::Local(saved) = args[1] else {
        panic!("saved")
    };
    let read = f
        .stmts()
        .iter()
        .position(|s| matches!(s, Statement::Assign { dest, .. } if *dest == saved))
        .unwrap();
    let statement = f.stmts().remove(read);
    let at = f.publish("__name__");
    f.stmts().insert(at, statement); // 调用仍在发布后，唯独真实冻结点提前。
    f.reject("真实读取/冻结/MakeClass早于发布");
    let mut f = Fixture::new(true);
    let call = f.call(RuntimeFn::FunctionModule);
    let Statement::Eval(Rvalue::Call { args, .. }) = &f.stmts()[call] else {
        panic!("module")
    };
    let Operand::Local(saved) = args[1] else {
        panic!("saved")
    };
    f.stmts().insert(
        call,
        Statement::Assign {
            dest: saved,
            value: Rvalue::Use(Operand::Const(Constant::None)),
        },
    );
    f.reject("冻结/producer局部被覆盖");
    let mut f = Fixture::new(true);
    let call = f.call(RuntimeFn::MakeClass);
    let statement = f.stmts().remove(call);
    let at = f.publish("__name__");
    f.stmts().insert(at, statement);
    f.reject("真实读取/冻结/MakeClass早于发布");
}

#[test]
fn entry_bypass_eh_cycles_and_internal_initializer_calls_are_rejected() {
    for wrapped in [false, true] {
        for module_caller in [false, true] {
            let mut f = Fixture::new(true);
            let entry = f.program.entry;
            let value = if wrapped {
                Rvalue::MakeFunction {
                    func: entry,
                    env: vec![],
                    defaults: vec![],
                    instance_context: None,
                }
            } else {
                Rvalue::Call {
                    callee: Callee::Fn(entry),
                    args: vec![],
                }
            };
            if module_caller {
                f.stmts().push(Statement::Eval(value));
            } else {
                let target = f.plan.functions()[0];
                f.program.functions[target].body.blocks.alloc(BasicBlock {
                    stmts: vec![Statement::Eval(value)],
                    terminator: Terminator::Unreachable,
                });
            }
            f.reject("程序内部调用或包装初始化入口");
        }
    }
    for mutation in 0..4 {
        let mut f = Fixture::new(true);
        let body = &mut f.program.functions[f.program.entry].body;
        match mutation {
            0 => {
                body.blocks[f.block].terminator = Terminator::Goto(f.block);
            }
            1 => {
                body.entry = body.blocks.alloc(BasicBlock {
                    stmts: vec![],
                    terminator: Terminator::Goto(f.block),
                });
                f.reject("角色发布不在entry同块有序前言");
                continue;
            }
            2 => {
                body.regions.push(Region {
                    source_handler_chain: None,
                    exc_local: None,
                    try_start: f.block,
                    try_len: 1,
                    handler: f.block,
                    kind: RegionKind::Catch {
                        type_names: vec!["ValueError".into()],
                    },
                });
                f.reject("初始化入口含异常绕入");
                continue;
            }
            _ => {
                body.blocks[f.block].terminator =
                    Terminator::Goto(BlockId::from_raw(999_999.into()));
                f.reject("控制边目标不在实际blocks");
                continue;
            }
        }
        f.reject("模块控制链回入");
    }
    let mut f = Fixture::new(true);
    let at = f.publish("__name__");
    f.stmts().insert(
        at,
        Statement::Eval(Rvalue::Call {
            callee: Callee::Runtime(RuntimeFn::DynNone),
            args: vec![],
        }),
    );
    f.reject("前言全部发布前存在未知effect");
    let mut f = Fixture::new(true);
    let slot = f.program.globals.alloc(GlobalDecl {
        storage: GlobalStorage::LegacyStatic,
        name: "fake_reset".into(),
        ty: MirType::Dynamic,
    });
    f.stmts().insert(
        0,
        Statement::SetGlobal {
            dest: slot,
            value: Operand::Const(Constant::Null),
        },
    );
    f.reject("前言全部发布前存在未知effect或伪reset");
}

#[test]
fn lowering_seal_preserves_all_non_generators_and_global_export_premises() {
    let mut f = Fixture::new(true);
    let seal = UnchangedHigh::capture(&f.program, &f.exports, f.plan.functions());
    generators::lower_with_trace(&mut f.program, &f.plan.functions, &f.plan.exceptions, true);
    seal.verify(&f.program, &f.exports).unwrap();
    let ids: Vec<_> = f
        .program
        .functions
        .iter()
        .map(|(id, _)| id)
        .filter(|id| !f.plan.functions().contains(id))
        .collect();
    // 仅原非gen，排除新增resume；任一完整原image变动须拒绝。
    let original_count = f.program.functions.len() - f.plan.functions().len();
    for id in ids
        .into_iter()
        .filter(|id| (id.into_raw().into_u32() as usize) < original_count)
    {
        let old = f.program.functions[id].name.clone();
        f.program.functions[id].name.push_str("_changed");
        assert!(
            seal.verify(&f.program, &f.exports)
                .unwrap_err()
                .contains("非生成器实际FnId/image")
        );
        f.program.functions[id].name = old;
    }
    let slot = f.slot("__name__");
    f.program.globals[slot].name.push_str("_changed");
    assert!(
        seal.verify(&f.program, &f.exports)
            .unwrap_err()
            .contains("GlobalDecl/exports")
    );
    let f = Fixture::new(true);
    let seal = UnchangedHigh::capture(&f.program, &f.exports, f.plan.functions());
    let mut exports = f.exports.clone();
    exports[0].guard = ExportGuard::None;
    assert!(
        seal.verify(&f.program, &exports)
            .unwrap_err()
            .contains("GlobalDecl/exports")
    );
}

#[test]
fn linked_registration_capability_and_real_maps_are_checked_before_certification() {
    let f = Fixture::new(true);
    for location in [
        ModuleLocation::LegacyDiagnostic,
        ModuleLocation::SourceAbsolute("/sources/product.py".into()),
        ModuleLocation::ExecutableRelative("sources/product.py".into()),
    ] {
        let mut case = Fixture::new(true);
        let mut meta = case.meta();
        meta.location = location.clone();
        case.initialization.meta(&meta).unwrap();
        generators::lower_with_trace(
            &mut case.program,
            &case.plan.functions,
            &case.plan.exceptions,
            true,
        );
        let (linked, maps) = trace_lowering::link_for_audit(vec![(case.program, meta)], 0);
        case.initialization.linked(&linked, &maps[0]).unwrap();
        assert_eq!(linked.modules[linked.entry].location, location);
    }
    for mutation in 0..7 {
        let mut meta = f.meta();
        match mutation {
            0 => meta.file_backed_by_global = false,
            1 => meta.name = "other".into(),
            2 => meta.file = "other.py".into(),
            3 => meta.is_package = true,
            4 => meta.location = ModuleLocation::SourceAbsolute("relative.py".into()),
            5 => meta.location = ModuleLocation::ExecutableRelative("../outside.py".into()),
            _ => {
                meta.location = ModuleLocation::SourceAbsolute("/sources/product.py".into());
                meta.file_backed_by_global = false;
            }
        }
        assert!(
            f.initialization
                .meta(&meta)
                .unwrap_err()
                .starts_with("模块初始化合同：")
        );
    }
    let mut f = Fixture::new(true);
    generators::lower_with_trace(&mut f.program, &f.plan.functions, &f.plan.exceptions, true);
    let meta = f.meta();
    let (mut linked, mut maps) = trace_lowering::link_for_audit(vec![(f.program, meta)], 0);
    f.initialization.linked(&linked, &maps[0]).unwrap();
    let entry = linked.modules[linked.entry].init;
    let original = *maps[0]
        .functions
        .iter()
        .find(|(_, actual)| **actual == entry)
        .unwrap()
        .0;
    maps[0].functions.remove(&original);
    assert!(
        f.initialization
            .linked(&linked, &maps[0])
            .unwrap_err()
            .contains("init映射错误")
    );
    maps[0].functions.insert(original, entry);
    f.initialization.linked(&linked, &maps[0]).unwrap();
    let slot = f
        .exports
        .iter()
        .find(|e| e.name == "__name__")
        .unwrap()
        .value;
    let mapped = maps[0].globals.remove(&slot).unwrap();
    assert!(
        f.initialization
            .linked(&linked, &maps[0])
            .unwrap_err()
            .contains("缺角色映射")
    );
    maps[0]
        .globals
        .insert(slot, GlobalId::from_raw(999_999.into()));
    assert!(
        f.initialization
            .linked(&linked, &maps[0])
            .unwrap_err()
            .contains("角色/导出/guard映射错误")
    );
    maps[0].globals.insert(slot, mapped);
    linked.modules[linked.entry]
        .exports
        .iter_mut()
        .find(|e| e.name == "__name__")
        .unwrap()
        .guard = ExportGuard::None;
    assert!(
        f.initialization
            .linked(&linked, &maps[0])
            .unwrap_err()
            .contains("角色/导出/guard映射错误")
    );
}
