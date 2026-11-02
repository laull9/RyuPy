//! 报告与真实PE逐条交叉，不用方法名字预测委托根，也不把声明但未引用的工厂列为根。
use crate::exact_i64_tests::{function, local, program, single_block};
use dotnetdll::prelude::*;
use la_arena::Arena;
use ryupy_mir::*;

fn global(program: &mut LinkedProgram, name: &str) -> GlobalId {
    program.globals.alloc(GlobalDecl {
        name: name.into(),
        ty: MirType::Dynamic,
    })
}
fn target(program: &mut LinkedProgram, name: &str, nested: bool) -> FnId {
    let mut locals = Arena::default();
    let env = nested.then(|| local(&mut locals, MirType::Dynamic));
    let mut value = function(
        name,
        single_block(
            locals,
            vec![],
            Some(Operand::Const(ryupy_mir::Constant::Null)),
        ),
        env.into_iter().collect(),
        MirType::Dynamic,
    );
    value.env = env;
    value.signature_global = Some(global(program, &format!("{name}_sig")));
    if !nested {
        value.function_global = Some(global(program, &format!("{name}_fn")));
        value.defaults_global = Some(global(program, &format!("{name}_defaults")));
    }
    program.functions.alloc(value)
}
fn options() -> crate::EmitOptions {
    crate::EmitOptions {
        assembly_name: "report".into(),
        entry_source_path: None,
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
fn check_actual(artifact: &crate::EmissionArtifact) {
    let res = Resolution::parse(&artifact.bytes, ReadOptions::default()).unwrap();
    let mut actual = Vec::new();
    for (owner, _) in res.enumerate_type_definitions() {
        for (_, method) in res.enumerate_methods(owner) {
            let Some(body) = &method.body else {
                continue;
            };
            for (index, instruction) in body.instructions.iter().enumerate() {
                if let Instruction::LoadMethodPointer(target) = instruction {
                    actual.push((
                        descriptor(&res, owner, method),
                        index as u32,
                        target.show(&res),
                    ));
                }
            }
        }
    }
    actual.sort();
    let mut reported: Vec<_> = artifact
        .report
        .delegate_references
        .iter()
        .map(|reference| {
            (
                reference.source.clone(),
                reference.instruction,
                reference.target_display.clone(),
            )
        })
        .collect();
    reported.sort();
    assert_eq!(actual, reported);
}

#[test]
fn minimal_report_has_only_actual_module_delegates_and_no_path_leak() {
    let program = program();
    let artifact = crate::emit_assembly_with_report(&program, &options()).unwrap();
    check_actual(&artifact);
    assert_eq!(artifact.report.modules.len(), 1);
    assert_eq!(artifact.report.delegate_references.len(), 3);
    assert_eq!(artifact.report.entry.name, "Main");
    for role in ["module_init", "module_getter", "module_setter"] {
        assert_eq!(
            artifact
                .report
                .delegate_references
                .iter()
                .filter(|site| site.role == role)
                .count(),
            1
        );
    }
    // 请求报告不向IL加注册/根：比较真实方法体而非含随机模块标识的PE字节。
    let legacy = crate::emit_assembly(&program, &options()).unwrap();
    let old = Resolution::parse(&legacy, ReadOptions::default()).unwrap();
    let new = Resolution::parse(&artifact.bytes, ReadOptions::default()).unwrap();
    let bodies = |res: &Resolution<'_>| {
        res.type_definitions
            .iter()
            .flat_map(|ty| &ty.methods)
            .map(|method| {
                (
                    method.name.to_string(),
                    method.body.as_ref().map(|body| {
                        body.instructions
                            .iter()
                            .map(|instruction| instruction.show(res))
                            .collect::<Vec<_>>()
                    }),
                )
            })
            .collect::<Vec<_>>()
    };
    assert_eq!(bodies(&old), bodies(&new));
    assert!(!format!("{:?}", artifact.report).contains("exact_i64.py"));
}

#[test]
fn modules_nested_functions_generators_and_registered_factories_match_real_pe() {
    let mut program = program();
    let init = program.modules[program.entry].init;
    let top = target(&mut program, "same_python_name_left", false);
    let nested = target(&mut program, "same_python_name_right", true);
    program.functions[top].name = "same".into();
    program.functions[nested].name = "same".into();
    let mut locals = Arena::default();
    let frame = local(&mut locals, MirType::Dynamic);
    let resume = program.functions.alloc(function(
        "actual_resume",
        single_block(
            locals,
            vec![],
            Some(Operand::Const(ryupy_mir::Constant::Null)),
        ),
        vec![frame],
        MirType::Dynamic,
    ));
    let body = &mut program.functions[init].body;
    let closure = local(&mut body.locals, MirType::Dynamic);
    let generator = local(&mut body.locals, MirType::Dynamic);
    body.blocks[body.entry].stmts.extend([
        Statement::Assign {
            dest: closure,
            value: Rvalue::MakeFunction {
                func: nested,
                env: vec![],
                defaults: vec![],
            },
        },
        Statement::Assign {
            dest: generator,
            value: Rvalue::MakeGenerator {
                resume,
                frame: vec![],
            },
        },
    ]);
    let second_init = program.functions.alloc(function(
        "second_init",
        single_block(Arena::default(), vec![], None),
        vec![],
        MirType::Unit,
    ));
    program.modules.alloc(ModuleDef {
        protected_exports: vec![],
        name: "other".into(),
        package: "".into(),
        file: "same.py".into(),
        init: second_init,
        exports: vec![],
    });
    for registered in [true, false] {
        let factory_global = registered.then(|| global(&mut program, "actual_factory"));
        program.classes.alloc(ClassDef {
            name: "SameException".into(),
            codegen_name: format!("Exception{registered}"),
            fields: vec![],
            is_value_type: false,
            is_dataclass: false,
            base: None,
            builtin_base: Some("ValueError".into()),
            is_exception: true,
            factory_global,
        });
    }
    let artifact = crate::emit_assembly_with_report(&program, &options()).unwrap();
    check_actual(&artifact);
    assert_eq!(artifact.report.modules.len(), 2);
    for (role, count) in [
        ("module_init", 2),
        ("module_getter", 2),
        ("module_setter", 2),
        ("callable_thunk", 2),
        ("generator_resume", 1),
        ("exception_factory", 1),
    ] {
        assert_eq!(
            artifact
                .report
                .delegate_references
                .iter()
                .filter(|site| site.role == role)
                .count(),
            count,
            "{role}"
        );
    }
    assert!(
        artifact
            .report
            .delegate_references
            .iter()
            .all(|site| site.target.is_some() && site.role != "unclassified")
    );
    let ids: Vec<_> = artifact
        .report
        .modules
        .iter()
        .map(|module| module.module_id)
        .collect();
    assert_eq!(ids, vec![0, 1]);
}

#[test]
fn unknown_pointer_is_reported_explicitly_instead_of_silently_omitted() {
    let mut program = program();
    program.modules = Arena::default();
    let mut res = Resolution::new(Module::new("unknown.dll"));
    let owner = res.push_type_definition(TypeDefinition::new(Some("Example".into()), "Container"));
    let destination = res.push_method(
        owner,
        Method::new(
            Accessibility::Public,
            msig! { static void () },
            "not_a_known_role",
            None,
        ),
    );
    let source = res.push_method(
        owner,
        Method::new(
            Accessibility::Public,
            msig! { static void () },
            "entry",
            Some(body::Method::new(vec![
                Instruction::LoadMethodPointer(destination.into()),
                Instruction::Pop,
                Instruction::Return,
            ])),
        ),
    );
    let report = crate::emission_report::collect(
        &res,
        &program,
        source,
        &Default::default(),
        &Default::default(),
        &Default::default(),
        &crate::modules::ModuleAccessors {
            getters: vec![],
            setters: vec![],
        },
    );
    assert_eq!(report.delegate_references.len(), 1);
    assert_eq!(report.delegate_references[0].role, "unclassified");
    assert_eq!(
        report.delegate_references[0].target.as_ref().unwrap().name,
        "not_a_known_role"
    );
}
