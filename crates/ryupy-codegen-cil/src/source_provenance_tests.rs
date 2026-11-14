//! 独立读取实际PE中的来源字段/调用操作数；期望来自两份手工MIR单元，不从待验所属表反推。
use crate::exact_i64_tests::{function, local, single_block};
use dotnetdll::prelude::*;
use la_arena::Arena;
use ryupy_mir::{
    Callee, Constant, GlobalDecl, GlobalStorage, LegacySourceEntry, LinkedProgram, MirType,
    ModuleExport, Operand, RuntimeFn, Rvalue, Statement,
};

fn sample() -> LinkedProgram {
    let mut program = crate::exact_i64_tests::program();
    let template = program.modules[program.entry].clone();
    program.functions = Arena::default();
    program.globals = Arena::default();
    program.modules = Arena::default();
    for prefix in ["left", "right"] {
        let name = program.globals.alloc(GlobalDecl {
            name: format!("{prefix}_name"),
            ty: MirType::Str,
            storage: GlobalStorage::LegacyStatic,
        });
        let bound = program.globals.alloc(GlobalDecl {
            name: format!("{prefix}_bound"),
            ty: MirType::Bool,
            storage: GlobalStorage::LegacyStatic,
        });
        let signature = program.globals.alloc(GlobalDecl {
            name: format!("{prefix}_signature"),
            ty: MirType::Dynamic,
            storage: GlobalStorage::LegacyStatic,
        });
        let mut entry = function(
            &format!("{prefix}_main"),
            single_block(Arena::default(), vec![], None),
            vec![],
            MirType::Unit,
        );
        entry.source_file = "shared/__init__.py".into();
        let entry_id = program.functions.alloc(entry);
        let mut locals = Arena::default();
        let depth = local(&mut locals, MirType::I64);
        let mut fixed = function(
            &format!("{prefix}_fixed"),
            single_block(
                locals,
                vec![
                    Statement::Assign {
                        dest: depth,
                        value: Rvalue::Call {
                            callee: Callee::Runtime(RuntimeFn::TracePushWithOrigin),
                            args: vec![
                                Operand::Const(Constant::Str("shared/__init__.py".into())),
                                Operand::Const(Constant::Str(format!("{prefix}_fixed"))),
                                Operand::Global(signature),
                            ],
                        },
                    },
                    Statement::Eval(Rvalue::Call {
                        callee: Callee::Runtime(RuntimeFn::TracePopTo),
                        args: vec![Operand::Local(depth)],
                    }),
                ],
                None,
            ),
            vec![],
            MirType::Unit,
        );
        fixed.signature_global = Some(signature);
        fixed.source_file = "shared/__init__.py".into();
        let fixed_id = program.functions.alloc(fixed);
        let source = program.globals.alloc(GlobalDecl {
            name: format!("{prefix}_source"),
            ty: MirType::Dynamic,
            storage: GlobalStorage::CodeSource {
                entry: entry_id,
                functions: vec![fixed_id],
                legacy_entry: Some(LegacySourceEntry {
                    initial_name: prefix.into(),
                    name_global: name,
                }),
            },
        });
        let mut locals = Arena::default();
        let marker = local(&mut locals, MirType::Dynamic);
        let depth = local(&mut locals, MirType::I64);
        program.functions[entry_id].body = single_block(
            locals,
            vec![
                Statement::SetGlobal {
                    dest: bound,
                    value: Operand::Const(Constant::Bool(false)),
                },
                Statement::Assign {
                    dest: marker,
                    value: Rvalue::Call {
                        callee: Callee::Runtime(RuntimeFn::ModuleExecutionSourceWithCode),
                        args: vec![Operand::Global(source), Operand::Const(Constant::Null)],
                    },
                },
                Statement::Assign {
                    dest: depth,
                    value: Rvalue::Call {
                        callee: Callee::Runtime(RuntimeFn::TracePushWithOrigin),
                        args: vec![
                            Operand::Const(Constant::Str("shared/__init__.py".into())),
                            Operand::Const(Constant::Str("<module>".into())),
                            Operand::Local(marker),
                        ],
                    },
                },
                Statement::SetGlobal {
                    dest: name,
                    value: Operand::Const(Constant::Str(prefix.into())),
                },
                Statement::SetGlobal {
                    dest: bound,
                    value: Operand::Const(Constant::Bool(true)),
                },
                Statement::Eval(Rvalue::Call {
                    callee: Callee::Runtime(RuntimeFn::TracePopTo),
                    args: vec![Operand::Local(depth)],
                }),
            ],
            None,
        );
        let mut module = template.clone();
        module.name = prefix.into();
        module.init = entry_id;
        module.file = "shared/__init__.py".into();
        module.file_backed_by_global = true;
        module.exports = vec![ModuleExport {
            name: "__name__".into(),
            value: name,
            guard: ryupy_mir::ExportGuard::BoundFlag(bound),
        }];
        let module_id = program.modules.alloc(module);
        if prefix == "left" {
            program.entry = module_id;
        }
    }
    program
}

fn emitted(program: &LinkedProgram) -> Vec<u8> {
    // 只序列化待解析字节，不装载或执行程序集。
    crate::emit_source_assembly(program, &options()).unwrap()
}

fn options() -> crate::EmitOptions {
    crate::EmitOptions {
        assembly_name: "source_probe".into(),
        entry_source_path: None,
    }
}

// 查询真实Call操作数，不依赖ResolvedDebug的方法显示分隔符；类型名及ABI来自固定Runtime合同。
fn type_name(ty: &MethodType, res: &Resolution<'_>) -> String {
    let MethodType::Base(base) = ty else {
        panic!("不允许泛型参数");
    };
    match base.as_ref() {
        BaseType::String => "string".into(),
        BaseType::Object => "object".into(),
        BaseType::Int64 => "long".into(),
        BaseType::Type {
            source: TypeSource::User(UserType::Reference(id)),
            value_kind,
        } => {
            assert!(!matches!(value_kind, Some(ValueKind::ValueType)));
            let reference = &res[*id];
            let ResolutionScope::Assembly(assembly) = reference.scope else {
                panic!("必须引用Runtime程序集");
            };
            assert_eq!(res[assembly].name, "RyuPy.Runtime");
            reference.type_name()
        }
        _ => panic!("不支持的来源ABI类型"),
    }
}

fn runtime_call(instruction: &Instruction, res: &Resolution<'_>, owner: &str, name: &str) -> bool {
    let Instruction::Call {
        param0: MethodSource::User(UserMethod::Reference(id)),
        ..
    } = instruction
    else {
        return false;
    };
    let method = &res[*id];
    if method.name != name {
        return false;
    }
    let MethodReferenceParent::Type(parent) = &method.parent else {
        panic!("必须是类型方法");
    };
    assert_eq!(type_name(parent, res), format!("RyuPy.Runtime.{owner}"));
    let signature = &method.signature;
    assert!(!signature.instance && !signature.explicit_this && signature.varargs.is_none());
    assert_eq!(signature.calling_convention, CallingConvention::Default);
    let expected: (&str, &[&str]) = match name {
        "CreateCodeSource" | "CreateLegacyCodeSourceV1" => {
            ("object", &["string", "string", "RyuPy.Runtime.PyModuleGet"])
        }
        "PushFrameWithOrigin" => ("long", &["string", "string", "object"]),
        "CreateModuleExecutionSourceWithCode" => ("object", &["object", "object"]),
        "FunctionDefinitionWithSource" => ("void", &["object", "string", "long", "object"]),
        _ => panic!("未列明的来源ABI"),
    };
    assert!(signature.return_type.0.is_empty());
    let result = match &signature.return_type.1 {
        None => "void".into(),
        Some(ParameterType::Value(ty)) => type_name(ty, res),
        _ => panic!("返回值不得是ref/typedref"),
    };
    assert_eq!(result, expected.0);
    let parameters: Vec<_> = signature
        .parameters
        .iter()
        .map(|parameter| {
            assert!(parameter.0.is_empty());
            let ParameterType::Value(ty) = &parameter.1 else {
                panic!("参数不得是ref/typedref");
            };
            type_name(ty, res)
        })
        .collect();
    assert_eq!(parameters, expected.1);
    true
}

#[test]
fn source_helpers_bind_actual_getters_fields_and_signature_operands() {
    let bytes = emitted(&sample());
    let res = Resolution::parse(&bytes, ReadOptions::default()).unwrap();
    let ty = res
        .type_definitions
        .iter()
        .find(|t| t.name == "Module")
        .unwrap();
    let sources = ty
        .methods
        .iter()
        .find(|m| m.name == "__init_code_sources")
        .unwrap()
        .body
        .as_ref()
        .unwrap();
    let instructions = &sources.instructions;
    let calls: Vec<_> = instructions
        .iter()
        .enumerate()
        .filter(|(_, i)| runtime_call(i, &res, "PyTraceback", "CreateLegacyCodeSourceV1"))
        .map(|(at, _)| at)
        .collect();
    assert_eq!(calls.len(), 2);
    for (index, prefix) in ["left", "right"].iter().enumerate() {
        let at = calls[index];
        assert!(matches!(
            instructions[at - 2],
            Instruction::LoadMethodPointer(_)
        ));
        assert!(
            instructions[at - 2]
                .show(&res)
                .contains(&format!("__mod_get_{index}"))
        );
        assert!(matches!(
            instructions[at + 1],
            Instruction::StoreStaticField { .. }
        ));
        assert!(
            instructions[at + 1]
                .show(&res)
                .contains(&format!("{prefix}_source"))
        );
        let fixed = ty
            .methods
            .iter()
            .find(|m| m.name == format!("{prefix}_fixed"))
            .unwrap();
        assert_eq!(fixed.signature, msig! { static void() });
        let code = &fixed.body.as_ref().unwrap().instructions;
        let push = code
            .iter()
            .position(|i| runtime_call(i, &res, "PyTraceback", "PushFrameWithOrigin"))
            .unwrap();
        assert!(matches!(
            code[push - 1],
            Instruction::LoadStaticField { .. }
        ));
        assert!(
            code[push - 1]
                .show(&res)
                .contains(&format!("{prefix}_signature"))
        );
        let main = ty
            .methods
            .iter()
            .find(|m| m.name == format!("{prefix}_main"))
            .unwrap();
        let code = &main.body.as_ref().unwrap().instructions;
        let marker = code
            .iter()
            .position(|i| {
                runtime_call(
                    i,
                    &res,
                    "PyTraceback",
                    "CreateModuleExecutionSourceWithCode",
                )
            })
            .unwrap();
        assert!(matches!(
            code[marker - 2],
            Instruction::LoadStaticField { .. }
        ));
        assert!(
            code[marker - 2]
                .show(&res)
                .contains(&format!("{prefix}_source"))
        );
        assert!(matches!(code[marker - 1], Instruction::LoadNull));
    }
    let init = &ty
        .methods
        .iter()
        .find(|m| m.name == "__init_functions")
        .unwrap()
        .body
        .as_ref()
        .unwrap()
        .instructions;
    assert!(init[0].show(&res).contains("__init_literals"));
    assert!(init[1].show(&res).contains("__init_code_sources"));
    let definitions: Vec<_> = init
        .iter()
        .enumerate()
        .filter(|(_, i)| runtime_call(i, &res, "PyOps", "FunctionDefinitionWithSource"))
        .map(|(at, _)| at)
        .collect();
    assert_eq!(definitions.len(), 2);
    for (index, prefix) in ["left", "right"].iter().enumerate() {
        let at = definitions[index];
        assert!(
            init[at - 4]
                .show(&res)
                .contains(&format!("{prefix}_signature"))
        );
        assert!(matches!(init[at - 3], Instruction::LoadNull));
        assert!(matches!(init[at - 2], Instruction::LoadConstantInt64(0)));
        assert!(
            init[at - 1]
                .show(&res)
                .contains(&format!("{prefix}_source"))
        );
    }
}

#[test]
fn swapped_entry_source_is_rejected_before_pe_emission() {
    let mut program = sample();
    let wrong = program
        .globals
        .iter()
        .find(|(_, g)| g.name == "right_source")
        .unwrap()
        .0;
    let entry = program.modules[program.entry].init;
    for (_, block) in program.functions[entry].body.blocks.iter_mut() {
        for statement in &mut block.stmts {
            if let Statement::Assign {
                value:
                    Rvalue::Call {
                        callee: Callee::Runtime(RuntimeFn::ModuleExecutionSourceWithCode),
                        args,
                    },
                ..
            } = statement
            {
                args[0] = Operand::Global(wrong);
            }
        }
    }
    assert!(crate::emit_source_assembly(&program, &options()).is_err());
}

#[test]
fn source_exports_reject_complete_erasure_without_breaking_legacy_api() {
    let mut program = sample();
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
                {
                    *callee = Callee::Runtime(RuntimeFn::TracePush);
                    args.pop();
                }
            }
        }
    }
    assert!(crate::emit_assembly(&program, &options()).is_ok());
    assert!(crate::emit_source_assembly(&program, &options()).is_err());
    assert!(crate::emit_source_assembly_with_report(&program, &options()).is_err());
    assert!(crate::dump_source_cil(&program, &options()).is_err());
    assert!(crate::SourceModules::capture(&program).is_err());
}

fn append_binding(program: &mut LinkedProgram) -> crate::fixed_bindings::FixedI32Binding {
    let initializer = program.functions.alloc(function(
        "binding_init",
        single_block(Arena::default(), vec![], None),
        vec![],
        MirType::Unit,
    ));
    let export = program.globals.alloc(GlobalDecl {
        name: "binding_export".into(),
        ty: MirType::Dynamic,
        storage: GlobalStorage::LegacyStatic,
    });
    let mut module = program.modules[program.entry].clone();
    module.name = "native_probe".into();
    module.file.clear();
    module.package.clear();
    module.init = initializer;
    module.exports = vec![ModuleExport {
        name: "add".into(),
        value: export,
        guard: ryupy_mir::ExportGuard::ValueSentinel,
    }];
    let module = program.modules.alloc(module);
    crate::fixed_bindings::FixedI32Binding {
        module,
        initializer,
        export,
        module_ref: "probe".into(),
        entry_point: "add".into(),
        signature: None,
    }
}

#[test]
fn fixed_source_report_only_exempts_the_new_validated_binding() {
    let mut program = sample();
    let sources = crate::SourceModules::capture(&program).unwrap();
    let binding = append_binding(&mut program);
    let deployment = crate::fixed_bindings::BindingDeployment {
        relative_file: "_internal/native/v1/b0/library.so".into(),
    };
    let emitted = crate::fixed_bindings::emit_source_with_report(
        &program,
        &options(),
        std::slice::from_ref(&binding),
        &deployment,
        &sources,
    )
    .unwrap();
    let pe = Resolution::parse(&emitted.bytes, ReadOptions::default()).unwrap();
    assert_eq!(emitted.bindings.len(), 1);
    assert!(
        pe.type_definitions
            .iter()
            .flat_map(|t| &t.methods)
            .any(|m| m.name == "__init_code_sources")
    );
    for mutation in 0..3 {
        let mut broken = sample();
        let sources = crate::SourceModules::capture(&broken).unwrap();
        let binding = append_binding(&mut broken);
        match mutation {
            0 => {
                broken.modules[broken.entry].init = binding.initializer;
            }
            1 => {
                let source = ryupy_mir::source_provenance::for_function(
                    &broken.globals,
                    broken.modules[broken.entry].init,
                )
                .unwrap();
                broken.globals[source].storage = GlobalStorage::LegacyStatic;
            }
            2 => {
                broken.modules.alloc(broken.modules[binding.module].clone());
            }
            _ => unreachable!(),
        }
        assert!(
            sources
                .validate_extension(&broken, std::slice::from_ref(&binding))
                .is_err()
        );
    }
    // 即使绑定本身结构合法，也不能把declare前就有的模块改认作新增例外。
    let replacement = crate::SourceModules::capture(&sample()).unwrap();
    let mut binding_on_old = binding.clone();
    binding_on_old.module = program.entry;
    assert!(
        replacement
            .validate_extension(&program, &[binding_on_old])
            .is_err()
    );
}
