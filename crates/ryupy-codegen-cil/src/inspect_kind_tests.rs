//! 真实PE冷登记门：每次new canonical之后只消费Duplicate；普通无profile不增加调用。
use dotnetdll::prelude::*;
use la_arena::Arena;
use ryupy_mir::*;

#[test]
fn inspect_kind_registers_actual_new_function_and_leaves_original_on_stack() {
    for kind in [
        None,
        Some(PythonFunctionKind::OrdinaryModule),
        Some(PythonFunctionKind::Generator),
    ] {
        let mut program = crate::exact_i64_tests::program();
        let signature = program.globals.alloc(GlobalDecl {
            storage: GlobalStorage::LegacyStatic,
            name: "signature".into(),
            ty: MirType::Dynamic,
        });
        let mut function = crate::exact_i64_tests::function(
            "public",
            crate::exact_i64_tests::single_block(
                Arena::default(),
                vec![],
                Some(Operand::Const(ryupy_mir::Constant::None)),
            ),
            vec![],
            MirType::Dynamic,
        );
        function.signature_global = Some(signature);
        function.definition = Some(DefinitionInfo {
            lexical_qualname: "public".into(),
            firstlineno: 1,
            introspection_kind: kind,
        });
        let target = program.functions.alloc(function);
        let init = program.modules[program.entry].init;
        let main = &mut program.functions[init];
        let result = main.body.locals.alloc(LocalDecl {
            ty: MirType::Dynamic,
            name: None,
            is_cell: false,
        });
        main.body.blocks[main.body.entry]
            .stmts
            .push(Statement::Assign {
                dest: result,
                value: Rvalue::MakeFunction {
                    func: target,
                    instance_context: None,
                    env: vec![],
                    defaults: vec![],
                },
            });
        let bytes = crate::emit_assembly(
            &program,
            &crate::EmitOptions {
                assembly_name: "InspectKindPe".into(),
                entry_source_path: None,
            },
        )
        .unwrap();
        let resolution = Resolution::parse(&bytes, ReadOptions::default()).unwrap();
        let mut registrations = Vec::new();
        for ty in &resolution.type_definitions {
            for method in &ty.methods {
                let Some(body) = &method.body else {
                    continue;
                };
                for (index, instruction) in body.instructions.iter().enumerate() {
                    if let Instruction::Call {
                        param0: MethodSource::User(UserMethod::Reference(reference)),
                        ..
                    } = instruction
                        && resolution[*reference].name == "FunctionIntrospectionKind"
                    {
                        assert!(index >= 3);
                        assert_eq!(body.instructions[index - 2], Instruction::Duplicate);
                        assert!(matches!(
                            body.instructions[index - 3],
                            Instruction::NewObject(_)
                        ));
                        assert_eq!(
                            resolution[*reference].signature,
                            msig! { static void(object, long) }
                        );
                        let Instruction::LoadConstantInt64(kind) = body.instructions[index - 1]
                        else {
                            panic!("kind必须编译期常量");
                        };
                        assert!(
                            !method.name.contains("__init_functions"),
                            "不能按共享签名登记"
                        );
                        assert!(matches!(
                            body.instructions[index + 1],
                            Instruction::StoreLocal(_)
                        ));
                        registrations.push(kind);
                    }
                }
            }
        }
        assert_eq!(
            registrations,
            kind.map(|k| vec![k as i64]).unwrap_or_default()
        );
    }
}
