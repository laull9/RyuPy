//! 同名同签名 core 的真实写入/解析测试：slot 来自报告真实映射，错误 core 不因同名而放行。
use crate::exact_i64_tests::{function, local, program, single_block};
use crate::instance_pe_tests::{Expected, Locator, Owner, Root, verify};
use crate::{DelegateReference, MethodDescriptor};
use dotnetdll::prelude::*;
use ryupy_mir::*;

fn locator(method: &MethodDescriptor, slot: u32) -> Locator {
    Locator {
        owner: method.owner.clone(),
        name: method.name.clone(),
        signature: method.signature.clone(),
        slot,
    }
}
fn root(reference: &DelegateReference) -> Root {
    Root {
        source: locator(&reference.source, reference.source_slot),
        target: locator(
            reference.target.as_ref().unwrap(),
            reference.target_slot.unwrap(),
        ),
        instruction: reference.instruction,
        role: reference.role.clone(),
    }
}
fn example() -> (Vec<u8>, Expected) {
    let mut program = program();
    let module = &mut program.modules[program.entry];
    module.execution = ModuleExecutionKind::InstanceGlobals;
    let init = &mut program.functions[module.init];
    let context = local(&mut init.body.locals, MirType::Dynamic);
    init.params.push(context);
    init.module_state = Some(context);
    init.module_state_source = Some(InstanceStateSource::ModuleEntryParameter);
    for _ in 0..2 {
        let mut locals = Default::default();
        let context = local(&mut locals, MirType::Dynamic);
        let owner = local(&mut locals, MirType::Dynamic);
        let mut value = function(
            "same_lambda",
            single_block(locals, vec![], Some(Operand::Const(ryupy_mir::Constant::Null))),
            vec![context, owner],
            MirType::Dynamic,
        );
        value.module_state = Some(context);
        value.module_state_source = Some(InstanceStateSource::FunctionParameter);
        value.dynamic_origin = Some(DynamicFunctionOrigin {
            owner,
            source_global: None,
        });
        value.signature_global = Some(program.globals.alloc(GlobalDecl {
            name: format!("sig_{}", program.functions.len()),
            storage: GlobalStorage::LegacyStatic,
            ty: MirType::Dynamic,
        }));
        program.functions.alloc(value);
    }
    let artifact = crate::emit_assembly_with_report(
        &program,
        &crate::EmitOptions {
            assembly_name: "same_core".into(),
            entry_source_path: None,
        },
    )
    .unwrap();
    // PE 容器时间戳可不同；完整解析结构不得因请求观察报告而改变。
    let plain = crate::emit_assembly(
        &program,
        &crate::EmitOptions {
            assembly_name: "same_core".into(),
            entry_source_path: None,
        },
    )
    .unwrap();
    let plain = Resolution::parse(&plain, ReadOptions::default()).unwrap();
    let observed = Resolution::parse(&artifact.bytes, ReadOptions::default()).unwrap();
    assert_eq!(format!("{plain:?}"), format!("{observed:?}"));
    let report = artifact.report;
    assert_eq!(report.schema_version, 2);
    let owners: Vec<_> = report
        .delegate_references
        .iter()
        .filter(|reference| reference.role == "callable_thunk")
        .map(|reference| {
            let id = reference.function_id.unwrap();
            let definition = report
                .function_definitions
                .iter()
                .find(|d| d.function_id == id)
                .unwrap();
            Owner {
                id,
                core: locator(&definition.core, definition.core_slot),
                root: root(reference),
                calls: 2,
            }
        })
        .collect();
    assert_eq!(owners.len(), 2);
    assert_ne!(owners[0].core.slot, owners[1].core.slot);
    assert_eq!(owners[0].core.name, owners[1].core.name);
    assert_eq!(owners[0].core.signature, owners[1].core.signature);
    assert!(
        owners
            .iter()
            .any(|owner| owner.root.target.name != format!("__thunk_{}", owner.id))
    );
    let expected = Expected {
        entry: locator(&report.entry, report.entry_slot),
        owners,
        modules: report
            .delegate_references
            .iter()
            .filter(|r| r.role == "module_init")
            .map(root)
            .collect(),
    };
    (artifact.bytes, expected)
}

#[test]
fn instance_pe_duplicate_core_roundtrip_and_wrong_targets() {
    let (bytes, expected) = example();
    verify(&bytes, &expected).unwrap();
    let mut wrong = expected.clone();
    wrong.owners[0].core.slot = wrong.owners[1].core.slot;
    assert!(
        verify(&bytes, &wrong).is_err(),
        "同名同签名错误 core 必须拒绝"
    );
    let mut wrong = expected.clone();
    wrong.owners[0].root.target = wrong.owners[1].root.target.clone();
    assert!(verify(&bytes, &wrong).is_err());
    let mut wrong = expected.clone();
    wrong.owners[0].root.source.slot = u32::MAX;
    assert!(verify(&bytes, &wrong).is_err());
    let mut wrong = expected.clone();
    wrong.owners[0].core.signature.push('!');
    assert!(verify(&bytes, &wrong).is_err());
    let mut wrong = expected.clone();
    wrong.owners[0].root.instruction = u32::MAX;
    assert!(verify(&bytes, &wrong).is_err());
    let mut wrong = expected.clone();
    wrong.owners[0].root.role = "module_init".into();
    assert!(verify(&bytes, &wrong).is_err());
    let mut wrong = expected.clone();
    wrong.owners.push(wrong.owners[0].clone());
    assert!(verify(&bytes, &wrong).is_err());
    let mut wrong = expected.clone();
    wrong.owners.clear();
    assert!(verify(&bytes, &wrong).is_err());
    let mut wrong = expected.clone();
    wrong.owners[0].calls = 1;
    assert!(verify(&bytes, &wrong).is_err());
    let mut wrong = expected.clone();
    wrong.entry = wrong.owners[0].root.source.clone();
    assert!(verify(&bytes, &wrong).is_err());
    let mut wrong = expected.clone();
    wrong.modules[0].source = wrong.owners[0].core.clone();
    assert!(verify(&bytes, &wrong).is_err());
    let mut wrong = expected.clone();
    wrong.modules.push(wrong.modules[0].clone());
    assert!(verify(&bytes, &wrong).is_err());
    let mut parsed = Resolution::parse(&bytes, ReadOptions::default()).unwrap();
    let owner = parsed
        .enumerate_type_definitions()
        .find(|(_, ty)| ty.namespace.as_deref() == Some("RyuPy.Generated") && ty.name == "Module")
        .unwrap()
        .0;
    let core = parsed
        .method_index(owner, expected.owners[0].core.slot as usize)
        .unwrap();
    let mut with_property = parsed.clone();
    with_property[owner].properties.push(Property::new(
        true,
        "unsupported_property",
        Parameter::value(ctype! { object }),
    ));
    let bytes_with_property = with_property
        .write(WriteOptions {
            is_32_bit: true,
            is_executable: false,
        })
        .unwrap();
    Resolution::parse(&bytes_with_property, ReadOptions::default()).unwrap();
    assert_eq!(
        verify(&bytes_with_property, &expected).unwrap_err(),
        "生成类型不唯一或含 property/event"
    );
    let mut with_event = parsed.clone();
    let listener = parsed[core].clone();
    with_event[owner].events.push(Event::new(
        "unsupported_event",
        BaseType::class(owner).into(),
        listener.clone(),
        listener,
    ));
    let bytes_with_event = with_event
        .write(WriteOptions {
            is_32_bit: true,
            is_executable: false,
        })
        .unwrap();
    Resolution::parse(&bytes_with_event, ReadOptions::default()).unwrap();
    assert_eq!(
        verify(&bytes_with_event, &expected).unwrap_err(),
        "生成类型不唯一或含 property/event"
    );
    parsed[core].body = None;
    let missing_body = parsed
        .write(WriteOptions {
            is_32_bit: true,
            is_executable: false,
        })
        .unwrap();
    Resolution::parse(&missing_body, ReadOptions::default()).unwrap();
    assert_eq!(verify(&missing_body, &expected).unwrap_err(), "方法没有 body");
}
