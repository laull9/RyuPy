//! 重新实现B后的真实PE验收；元数据/冷顺序证明不冒充JIT或AOT原生调用成功。
use crate::fixed_bindings::{BindingDeployment, emit_with_report};
use dotnetdll::prelude::*;

fn options() -> crate::EmitOptions {
    crate::EmitOptions {
        assembly_name: "binding_deployment".into(),
        entry_source_path: None,
    }
}
fn deployment() -> BindingDeployment {
    BindingDeployment {
        relative_file: "_internal/native/v1/b0/library.so".into(),
    }
}
fn descriptor(
    res: &Resolution<'_>,
    owner: TypeIndex,
    method: &Method<'_>,
) -> crate::MethodDescriptor {
    let ty = &res[owner];
    crate::MethodDescriptor {
        owner: ty
            .namespace
            .as_ref()
            .map_or_else(|| ty.name.to_string(), |ns| format!("{ns}.{}", ty.name)),
        name: method.name.to_string(),
        signature: method.signature.show(res),
    }
}

#[test]
fn report_matches_actual_pinvoke_and_every_delegate_reference() {
    let (program, binding) = crate::fixed_binding_tests::fixture();
    let artifact = emit_with_report(
        &program,
        &options(),
        std::slice::from_ref(&binding),
        &deployment(),
    )
    .unwrap();
    let res = Resolution::parse(&artifact.bytes, ReadOptions::default()).unwrap();
    assert_eq!(artifact.bindings.len(), 1);
    let record = &artifact.bindings[0];
    assert_eq!(record.binding_id, "b0");
    assert_eq!(record.module_id, binding.module.into_raw().into_u32());
    assert_eq!(
        record.initializer_id,
        binding.initializer.into_raw().into_u32()
    );
    assert_eq!(record.export_id, binding.export.into_raw().into_u32());
    assert_eq!(record.native.signature, "static long (int, int)");
    assert_eq!(record.body.signature, "static object (object[])");
    let mut pinvokes = 0;
    let mut delegates = 0;
    for (owner, _) in res.enumerate_type_definitions() {
        for (_, method) in res.enumerate_methods(owner) {
            if let Some(native) = &method.pinvoke {
                pinvokes += 1;
                assert_eq!(record.native, descriptor(&res, owner, method));
                assert_eq!(record.library, res[native.import_scope].name);
                assert_eq!(record.symbol, native.import_name);
                assert_eq!(
                    record.calling_convention,
                    format!("{:?}", native.calling_convention)
                );
                assert_eq!(record.character_set, format!("{:?}", native.character_set));
                assert_eq!(record.no_mangle, native.no_mangle);
                assert_eq!(record.supports_last_error, native.supports_last_error);
                assert_eq!(record.preserve_sig, method.preserve_sig);
                assert_eq!(record.is_static, !method.signature.instance);
                assert_eq!(record.has_body, method.body.is_some());
                assert_eq!(record.calling_convention, "Cdecl");
                assert_eq!(record.character_set, "NotSpecified");
                assert!(
                    record.no_mangle && record.preserve_sig && record.is_static && !record.has_body
                );
                assert!(!record.supports_last_error);
            }
            if let Some(body) = &method.body {
                for (index, instruction) in body.instructions.iter().enumerate() {
                    if let Instruction::LoadMethodPointer(target) = instruction {
                        delegates += 1;
                        let observed = artifact
                            .report
                            .delegate_references
                            .iter()
                            .find(|item| {
                                item.source == descriptor(&res, owner, method)
                                    && item.instruction == index as u32
                            })
                            .expect("each actual ldftn has one report entry");
                        assert_eq!(observed.target_display, target.show(&res));
                        if observed.target.as_ref() == Some(&record.body) {
                            assert_eq!(observed.role, "declared_native_body");
                            assert_eq!(observed.module_id, Some(record.module_id));
                        }
                    }
                }
            }
        }
    }
    assert_eq!(pinvokes, 1);
    assert_eq!(delegates, artifact.report.delegate_references.len());
    assert_eq!(
        artifact
            .report
            .delegate_references
            .iter()
            .filter(|r| r.role == "declared_native_body")
            .count(),
        1
    );
    assert_eq!(artifact.report.modules.len(), program.modules.len());
}

#[test]
fn resolver_uses_generated_owner_after_ensure_before_any_initialization() {
    let (program, binding) = crate::fixed_binding_tests::fixture();
    let artifact = emit_with_report(&program, &options(), &[binding], &deployment()).unwrap();
    let res = Resolution::parse(&artifact.bytes, ReadOptions::default()).unwrap();
    let methods: Vec<_> = res
        .type_definitions
        .iter()
        .flat_map(|ty| &ty.methods)
        .collect();
    let startup = methods
        .iter()
        .find(|method| method.name == "__init_functions")
        .unwrap()
        .body
        .as_ref()
        .unwrap();
    let ins = &startup.instructions;
    assert!(matches!(ins[0], Instruction::LoadString(_)));
    assert!(ins[1].show(&res).contains("EnsureDeclaredBindingAvailable"));
    let Instruction::LoadTokenType(owner) = &ins[2] else {
        panic!("actual generated owner token required")
    };
    assert!(owner.show(&res).contains("RyuPy.Generated.Module"));
    assert!(ins[3].show(&res).contains("System.Type.GetTypeFromHandle"));
    assert!(ins[4].show(&res).contains("System.Type.get_Assembly"));
    assert!(ins[5].show(&res).contains("ryupy_cabi_probe"));
    assert!(
        ins[6]
            .show(&res)
            .contains("_internal/native/v1/b0/library.so")
    );
    assert!(
        ins[7]
            .show(&res)
            .contains("PyNativeBindingLibrary.Register")
    );
    assert!(ins[8].show(&res).contains("__init_literals"));
    assert_eq!(
        ins.iter()
            .filter(|i| i.show(&res).contains("PyNativeBindingLibrary.Register"))
            .count(),
        1
    );
    assert!(
        !ins.iter()
            .any(|i| i.show(&res).contains("IsDynamicCodeSupported"))
    );
    let main = methods
        .iter()
        .find(|method| method.name == "Main")
        .unwrap()
        .body
        .as_ref()
        .unwrap();
    let system = main
        .instructions
        .iter()
        .position(|i| i.show(&res).contains("PySystem.Initialize"))
        .unwrap();
    let init = main
        .instructions
        .iter()
        .position(|i| i.show(&res).contains("__init_functions"))
        .unwrap();
    assert!(system < init);
}

#[test]
fn invalid_deployment_and_empty_binding_fail_before_emission() {
    let (program, binding) = crate::fixed_binding_tests::fixture();
    for path in [
        "",
        "/private/library.so",
        "../library.so",
        "_internal/native/v1/b0/../library.so",
        "_internal/native/v1/b0/other.so",
    ] {
        assert!(
            emit_with_report(
                &program,
                &options(),
                std::slice::from_ref(&binding),
                &BindingDeployment {
                    relative_file: path.into()
                }
            )
            .is_err()
        );
    }
    assert!(emit_with_report(&program, &options(), &[], &deployment()).is_err());
}

#[test]
fn ordinary_and_a_only_emission_have_no_resolver_reference() {
    let program = crate::exact_i64_tests::program();
    let ordinary = crate::emit_assembly_with_report(&program, &options()).unwrap();
    let (program, binding) = crate::fixed_binding_tests::fixture();
    let a_only = crate::fixed_bindings::emit(&program, &options(), &[binding]).unwrap();
    for bytes in [&ordinary.bytes, &a_only] {
        let res = Resolution::parse(bytes, ReadOptions::default()).unwrap();
        assert!(
            !res.type_references
                .iter()
                .any(|ty| ty.name == "PyNativeBindingLibrary")
        );
    }
    assert!(
        !ordinary
            .report
            .delegate_references
            .iter()
            .any(|item| item.role == "declared_native_body")
    );
}

/// 同一模块中同型函数必须得到各自可达方法，所有工厂成功后才发布导出槽。
#[test]
fn multi_export_pe_has_distinct_wrappers_and_one_registration() {
    use ryupy_mir::{ExportGuard, GlobalDecl, GlobalStorage, MirType, ModuleExport};
    let (mut program, first) = crate::fixed_binding_tests::fixture();
    let mut bindings = vec![first.clone()];
    for (name, symbol) in [
        ("subtract", "demo_subtract"),
        ("add_alias", "ryupy_demo_add_i32"),
    ] {
        let export = program.globals.alloc(GlobalDecl {
            storage: GlobalStorage::LegacyStatic,
            name: name.into(),
            ty: MirType::Dynamic,
        });
        program.modules[first.module].exports.push(ModuleExport {
            name: name.into(),
            value: export,
            guard: ExportGuard::ValueSentinel,
        });
        bindings.push(crate::fixed_bindings::FixedI32Binding {
            export,
            entry_point: symbol.into(),
            ..first.clone()
        });
    }
    let artifact = emit_with_report(&program, &options(), &bindings, &deployment()).unwrap();
    let res = Resolution::parse(&artifact.bytes, ReadOptions::default()).unwrap();
    let methods: Vec<_> = res
        .type_definitions
        .iter()
        .flat_map(|t| &t.methods)
        .collect();
    assert_eq!(artifact.bindings.len(), 3);
    let wrappers: std::collections::HashSet<_> =
        artifact.bindings.iter().map(|b| &b.body.name).collect();
    let natives: std::collections::HashSet<_> =
        artifact.bindings.iter().map(|b| &b.native.name).collect();
    assert_eq!(wrappers.len(), 3);
    assert_eq!(natives.len(), 3);
    assert_eq!(methods.iter().filter(|m| m.pinvoke.is_some()).count(), 3);
    for (index, b) in artifact.bindings.iter().enumerate() {
        assert_eq!(b.binding_id, format!("b{index}"));
        assert_eq!(b.symbol, bindings[index].entry_point);
        assert_eq!(b.export_id, bindings[index].export.into_raw().into_u32());
        assert_eq!(
            artifact
                .report
                .delegate_references
                .iter()
                .filter(|r| r.role == "declared_native_body" && r.target.as_ref() == Some(&b.body))
                .count(),
            1
        );
    }
    let init = methods
        .iter()
        .find(|m| m.name == "__declared_init")
        .unwrap()
        .body
        .as_ref()
        .unwrap();
    let last_factory = init
        .instructions
        .iter()
        .rposition(|i| i.show(&res).contains("CreateI32Binary"))
        .unwrap();
    let first_publish = init
        .instructions
        .iter()
        .position(|i| matches!(i, Instruction::StoreStaticField { .. }))
        .unwrap();
    assert!(last_factory < first_publish);
    assert_eq!(
        init.instructions
            .iter()
            .filter(|i| matches!(i, Instruction::StoreStaticField { .. }))
            .count(),
        3
    );
    let startup = methods
        .iter()
        .find(|m| m.name == "__init_functions")
        .unwrap()
        .body
        .as_ref()
        .unwrap();
    for name in [
        "EnsureDeclaredBindingAvailable",
        "RegisterDeclaredBinding",
        "PyNativeBindingLibrary.Register",
    ] {
        assert_eq!(
            startup
                .instructions
                .iter()
                .filter(|i| i.show(&res).contains(name))
                .count(),
            1,
            "{name}"
        );
    }
    // 重复全局槽、漏掉导出、跨库都必须在发射前拒绝。
    let mut bad = bindings.clone();
    bad[1].export = bad[0].export;
    assert!(emit_with_report(&program, &options(), &bad, &deployment()).is_err());
    assert!(emit_with_report(&program, &options(), &bindings[..2], &deployment()).is_err());
    bad = bindings.clone();
    bad[1].module_ref = "another_library".into();
    assert!(emit_with_report(&program, &options(), &bad, &deployment()).is_err());
    bad = bindings.clone();
    bad[1].initializer = program.modules[program.entry].init;
    assert!(emit_with_report(&program, &options(), &bad, &deployment()).is_err());
}
