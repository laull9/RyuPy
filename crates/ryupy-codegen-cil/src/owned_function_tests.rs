//! 实例函数thunk隐藏参数的真实PE回归：owner、self与cells各自保持独立。
use crate::exact_i64_tests::{function, local, program, single_block};
use dotnetdll::prelude::*;
use ryupy_mir::{
    DynamicFunctionOrigin, GlobalDecl, GlobalStorage, MirType, Operand, ParamKind, ParamMeta,
};

#[test]
fn owned_thunk_excludes_context_from_binding_and_passes_hidden_arguments() {
    let mut program = program();
    let mut locals = Default::default();
    let arg = local(&mut locals, MirType::Dynamic);
    let env = local(&mut locals, MirType::Dynamic);
    let context = local(&mut locals, MirType::Dynamic);
    let owner = local(&mut locals, MirType::Dynamic);
    let mut core = function(
        "owned_core",
        single_block(locals, vec![], Some(Operand::Local(arg))),
        vec![arg, env, context, owner],
        MirType::Dynamic,
    );
    core.env = Some(env);
    core.module_state = Some(context);
    core.module_state_source = Some(ryupy_mir::InstanceStateSource::FunctionParameter);
    core.dynamic_origin = Some(DynamicFunctionOrigin {
        owner,
        source_global: None,
    });
    core.param_meta.push(ParamMeta {
        name: "arg".into(),
        kind: ParamKind::PositionalOrKeyword,
        has_default: false,
        ty: MirType::Dynamic,
    });
    core.signature_global = Some(program.globals.alloc(GlobalDecl {
        name: "owned_signature".into(),
        storage: GlobalStorage::LegacyStatic,
        ty: MirType::Dynamic,
    }));
    let core = program.functions.alloc(core);
    let module = &mut program.modules[program.entry];
    module.execution = ryupy_mir::ModuleExecutionKind::InstanceGlobals;
    let main = &mut program.functions[module.init];
    let caller = local(&mut main.body.locals, MirType::Dynamic);
    let captured = local(&mut main.body.locals, MirType::Dynamic);
    let result = local(&mut main.body.locals, MirType::Dynamic);
    main.params = vec![caller];
    main.module_state = Some(caller);
    main.module_state_source = Some(ryupy_mir::InstanceStateSource::ModuleEntryParameter);
    main.body.blocks[main.body.entry].stmts.clear();
    main.body.blocks[main.body.entry]
        .stmts
        .push(ryupy_mir::Statement::Assign {
            dest: captured,
            value: ryupy_mir::Rvalue::Call {
                callee: ryupy_mir::Callee::Runtime(
                    ryupy_mir::RuntimeFn::InstanceCaptureDefinitionContext,
                ),
                args: vec![Operand::Local(caller)],
            },
        });
    for _ in 0..2 {
        main.body.blocks[main.body.entry]
            .stmts
            .push(ryupy_mir::Statement::Assign {
                dest: result,
                value: ryupy_mir::Rvalue::MakeFunction {
                    func: core,
                    env: vec![Operand::Local(captured)],
                    defaults: vec![],
                    instance_context: Some(Operand::Local(captured)),
                },
            });
    }
    let bytes = crate::emit_assembly(
        &program,
        &crate::EmitOptions {
            assembly_name: "owner_thunk".into(),
            entry_source_path: None,
        },
    )
    .unwrap();
    let res = Resolution::parse(&bytes, ReadOptions::default()).unwrap();
    let thunk = res
        .type_definitions
        .iter()
        .flat_map(|t| &t.methods)
        .find(|m| m.name == "__thunk_0")
        .unwrap();
    assert_eq!(thunk.signature.parameters.len(), 5);
    let methods: Vec<_> = res
        .type_definitions
        .iter()
        .flat_map(|t| &t.methods)
        .collect();
    let main = methods.iter().find(|m| m.name == "__main__").unwrap();
    let main_ins = &main.body.as_ref().unwrap().instructions;
    assert_eq!(
        main_ins
            .iter()
            .filter(|i| i.show(&res).contains("CreateOwnedFunction("))
            .count(),
        2
    );
    assert_eq!(
        main_ins
            .iter()
            .filter(|i| i.show(&res).contains("CaptureDefinitionContext("))
            .count(),
        1
    );
    assert!(
        !main_ins
            .iter()
            .any(|i| matches!(i, Instruction::LoadMethodPointer(_)))
    );
    assert_eq!(
        main_ins
            .iter()
            .filter(|i| matches!(i, Instruction::LoadStaticField { .. })
                && i.show(&res).contains("__owned_code_"))
            .count(),
        2
    );
    let init = methods
        .iter()
        .find(|m| m.name == "__init_functions")
        .unwrap();
    assert_eq!(
        init.body
            .as_ref()
            .unwrap()
            .instructions
            .iter()
            .filter(
                |i| matches!(i, Instruction::NewObject(_)) && i.show(&res).contains("PyOwnedCall")
            )
            .count(),
        1
    );
    let create = res
        .method_references
        .iter()
        .find(|m| m.name == "CreateOwnedFunction")
        .unwrap();
    assert_eq!(create.signature.parameters.len(), 7);
    assert!(
        create.signature.parameters[3]
            .show(&res)
            .contains("PyOwnedCall")
    );
    let ins = &thunk.body.as_ref().unwrap().instructions;
    assert!(matches!(ins[3], Instruction::LoadConstantInt32(1)));
    let calls: Vec<_> = ins
        .iter()
        .enumerate()
        .filter_map(|(i, op)| op.show(&res).contains("owned_core(").then_some(i))
        .collect();
    assert_eq!(calls.len(), 2);
    for i in calls {
        assert!(matches!(ins[i - 1], Instruction::LoadArgument(1)));
        assert!(matches!(ins[i - 2], Instruction::LoadArgument(0)));
        assert!(ins[i - 3].show(&res).contains("get_Env"));
        assert!(matches!(ins[i - 4], Instruction::LoadArgument(1)));
    }
}

#[test]
fn legacy_program_has_no_owned_function_references_or_cache() {
    let bytes = crate::emit_assembly(
        &program(),
        &crate::EmitOptions {
            assembly_name: "legacy_owner_isolation".into(),
            entry_source_path: None,
        },
    )
    .unwrap();
    let res = Resolution::parse(&bytes, ReadOptions::default()).unwrap();
    assert!(!res.type_references.iter().any(|t| t.name == "PyOwnedCall"));
    assert!(!res.method_references.iter().any(|m| matches!(
        m.name.as_ref(),
        "CaptureDefinitionContext"
            | "CreateOwnedFunction"
            | "DefinitionModule"
            | "ClassGuard"
            | "CreateClass"
    )));
    assert!(
        !res.type_definitions
            .iter()
            .flat_map(|t| &t.fields)
            .any(|f| f.name.starts_with("__owned_code_"))
    );
}

#[test]
fn generator_resume_restores_context_without_owned_thunk_or_recapture() {
    use ryupy_mir::{Callee, Constant, InstanceStateSource, RuntimeFn, Rvalue, Statement};
    let mut program = program();
    let module = &mut program.modules[program.entry];
    module.execution = ryupy_mir::ModuleExecutionKind::InstanceGlobals;
    let main = &mut program.functions[module.init];
    let caller = local(&mut main.body.locals, MirType::Dynamic);
    main.params = vec![caller];
    main.module_state = Some(caller);
    main.module_state_source = Some(InstanceStateSource::ModuleEntryParameter);
    let mut locals = Default::default();
    let frame = local(&mut locals, MirType::Dynamic);
    let context = local(&mut locals, MirType::Dynamic);
    let result = local(&mut locals, MirType::Dynamic);
    let mut resume = function(
        "owned_resume",
        single_block(
            locals,
            vec![
                Statement::Assign {
                    dest: context,
                    value: Rvalue::Call {
                        callee: Callee::Runtime(RuntimeFn::ArrayGet),
                        args: vec![Operand::Local(frame), Operand::Const(Constant::I64(5))],
                    },
                },
                Statement::Assign {
                    dest: result,
                    value: Rvalue::Call {
                        callee: Callee::Runtime(RuntimeFn::InstanceLoadName),
                        args: vec![
                            Operand::Local(context),
                            Operand::Const(Constant::Str("value".into())),
                        ],
                    },
                },
            ],
            Some(Operand::Local(result)),
        ),
        vec![frame],
        MirType::Dynamic,
    );
    resume.module_state = Some(context);
    resume.module_state_source = Some(InstanceStateSource::GeneratorFrame { frame, slot: 5 });
    program.functions.alloc(resume);
    let bytes = crate::emit_assembly(
        &program,
        &crate::EmitOptions {
            assembly_name: "resume_owner".into(),
            entry_source_path: None,
        },
    )
    .unwrap();
    let res = Resolution::parse(&bytes, ReadOptions::default()).unwrap();
    let methods: Vec<_> = res
        .type_definitions
        .iter()
        .flat_map(|t| &t.methods)
        .collect();
    assert!(!methods.iter().any(|m| m.name.starts_with("__thunk_")));
    assert!(!res.type_references.iter().any(|t| t.name == "PyOwnedCall"));
    let resume = methods.iter().find(|m| m.name == "owned_resume").unwrap();
    assert_eq!(resume.signature.parameters.len(), 1);
    let ins = &resume.body.as_ref().unwrap().instructions;
    assert_eq!(
        ins.iter()
            .filter(|i| i.show(&res).contains("ArrayGet("))
            .count(),
        1
    );
    assert_eq!(
        ins.iter()
            .filter(|i| i.show(&res).contains("LoadName("))
            .count(),
        1
    );
    assert!(
        !ins.iter()
            .any(|i| i.show(&res).contains("CaptureDefinitionContext"))
    );
}

#[test]
fn instance_class_helpers_have_explicit_context_and_exact_abi() {
    let mut res = Resolution::new(Module::new("instance_class_refs.dll"));
    let runtime = res.push_assembly_reference(ExternalAssemblyReference::new("RyuPy.Runtime"));
    let _refs = crate::instance_refs::InstanceRefs::build(&mut res, runtime);
    for (name, signature) in [
        ("ClassGuard", msig! { static void(object) }),
        (
            "CreateClass",
            msig! { static object(object, string, object[]) },
        ),
    ] {
        assert!(
            res.method_references
                .iter()
                .any(|m| m.name == name && m.signature == signature),
            "缺少准确类context ABI {name}"
        );
    }
}

#[test]
fn instance_class_cil_passes_captured_context_and_keeps_class_local() {
    use ryupy_mir::{Callee, Constant, InstanceStateSource, RuntimeFn, Rvalue, Statement};
    let mut program = program();
    let module = &mut program.modules[program.entry];
    module.execution = ryupy_mir::ModuleExecutionKind::InstanceGlobals;
    let main = &mut program.functions[module.init];
    let outer = local(&mut main.body.locals, MirType::Dynamic);
    let captured = local(&mut main.body.locals, MirType::Dynamic);
    let class = local(&mut main.body.locals, MirType::Dynamic);
    main.params = vec![outer];
    main.module_state = Some(outer);
    main.module_state_source = Some(InstanceStateSource::ModuleEntryParameter);
    main.body.blocks[main.body.entry].stmts = vec![
        Statement::Eval(Rvalue::Call {
            callee: Callee::Runtime(RuntimeFn::InstanceClassGuard),
            args: vec![Operand::Local(outer)],
        }),
        Statement::Assign {
            dest: captured,
            value: Rvalue::Call {
                callee: Callee::Runtime(RuntimeFn::InstanceCaptureDefinitionContext),
                args: vec![Operand::Local(outer)],
            },
        },
        Statement::Assign {
            dest: class,
            value: Rvalue::Call {
                callee: Callee::Runtime(RuntimeFn::InstanceMakeClass),
                args: vec![
                    Operand::Local(captured),
                    Operand::Const(Constant::Str("C".into())),
                    Operand::Array(vec![]),
                ],
            },
        },
    ];
    let bytes = crate::emit_assembly(
        &program,
        &crate::EmitOptions {
            assembly_name: "class_context".into(),
            entry_source_path: None,
        },
    )
    .unwrap();
    let res = Resolution::parse(&bytes, ReadOptions::default()).unwrap();
    let main = res
        .type_definitions
        .iter()
        .flat_map(|t| &t.methods)
        .find(|m| m.name == "__main__")
        .unwrap();
    let ins = &main.body.as_ref().unwrap().instructions;
    let calls: Vec<_> = ins
        .iter()
        .filter_map(|i| {
            let text = i.show(&res);
            (matches!(i, Instruction::Call { .. }) && text.contains("PyModuleInstances."))
                .then_some(text)
        })
        .collect();
    assert_eq!(calls.len(), 3);
    assert!(calls[0].contains("ClassGuard("));
    assert!(calls[1].contains("CaptureDefinitionContext("));
    assert!(calls[2].contains("CreateClass("));
    assert!(
        !ins.iter()
            .any(|i| matches!(i, Instruction::StoreStaticField { .. }))
    );
    assert!(
        !res.type_definitions
            .iter()
            .flat_map(|t| &t.fields)
            .any(|f| f.name.contains("__class"))
    );
}
