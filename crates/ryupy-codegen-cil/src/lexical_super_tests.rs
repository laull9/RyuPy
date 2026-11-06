//! 新frame ABI只在实际MIR使用时出现；CIL按site、callee、Env/index、当前首参原槽顺序传递。
use crate::exact_i64_tests::{function, local, program, single_block};
use dotnetdll::prelude::*;
use la_arena::Arena;
use ryupy_mir::{
    Callee, Constant, InstanceStateSource, MirType, Operand, RuntimeFn, Rvalue, Statement,
    SuperClassSource, ZeroCallFrame,
};

#[test]
fn lexical_super_emits_lazy_abi_without_default_reference() {
    for enabled in [false, true] {
        for cell in [false, true] {
            let mut program = program();
            let mut locals = Arena::default();
            let callee = local(&mut locals, MirType::Dynamic);
            let env = local(&mut locals, MirType::Dynamic);
            let receiver = local(&mut locals, MirType::Dynamic);
            locals[receiver].is_cell = cell;
            let output = local(&mut locals, MirType::Dynamic);
            let value = if enabled {
                Rvalue::InstanceZeroCall {
                    callee: Operand::Local(callee),
                    frame: ZeroCallFrame {
                        class: SuperClassSource::Lexical {
                            environment: env,
                            index: 3,
                        },
                        first_parameter: Some(receiver),
                    },
                }
            } else {
                Rvalue::Use(Operand::Local(callee))
            };
            program.functions.alloc(function(
                "probe",
                single_block(
                    locals,
                    vec![Statement::Assign {
                        dest: output,
                        value,
                    }],
                    Some(Operand::Local(output)),
                ),
                vec![callee, env, receiver],
                MirType::Dynamic,
            ));
            let bytes = crate::emit_assembly(
                &program,
                &crate::EmitOptions {
                    assembly_name: "lexical_super_abi".into(),
                    entry_source_path: None,
                },
            )
            .unwrap();
            let res = Resolution::parse(&bytes, ReadOptions::default()).unwrap();
            let refs: Vec<_> = res
                .method_references
                .iter()
                .filter(|method| method.name == "CallZeroWithFrame")
                .collect();
            assert_eq!(refs.len(), usize::from(enabled));
            if !enabled {
                continue;
            }
            assert_eq!(
                refs[0].signature,
                msig! { static object(int, object, object, long, object, long) }
            );
            let probe = res
                .type_definitions
                .iter()
                .flat_map(|ty| &ty.methods)
                .find(|method| method.name == "probe")
                .unwrap();
            let instructions = &probe.body.as_ref().unwrap().instructions;
            assert!(matches!(instructions[0], Instruction::LoadConstantInt32(0)));
            assert!(matches!(instructions[1], Instruction::LoadArgument(0)));
            assert!(matches!(instructions[2], Instruction::LoadArgument(1)));
            assert!(matches!(instructions[3], Instruction::LoadConstantInt64(3)));
            assert!(matches!(instructions[4], Instruction::LoadArgument(2)));
            assert!(
                matches!(instructions[5], Instruction::LoadConstantInt64(kind) if kind == if cell { 2 } else { 1 })
            );
            assert!(
                instructions[6]
                    .show(&res)
                    .contains("PyOps.CallZeroWithFrame(")
            );
            assert!(
                !instructions
                    .iter()
                    .any(|instruction| instruction.show(&res).contains("CellGet"))
            );
        }
    }
}

#[test]
fn lexical_super_preserves_pic_site_sequence() {
    let mut program = program();
    let mut locals = Arena::default();
    let callee = local(&mut locals, MirType::Dynamic);
    let output = local(&mut locals, MirType::Dynamic);
    let ordinary = || Rvalue::DynCallValue {
        callee: Operand::Local(callee),
        args: vec![],
    };
    let frame_call = Rvalue::InstanceZeroCall {
        callee: Operand::Local(callee),
        frame: ZeroCallFrame {
            class: SuperClassSource::Missing,
            first_parameter: None,
        },
    };
    let statements = [ordinary(), frame_call, ordinary()]
        .into_iter()
        .map(|value| Statement::Assign {
            dest: output,
            value,
        })
        .collect();
    program.functions.alloc(function(
        "sequence",
        single_block(locals, statements, Some(Operand::Local(output))),
        vec![callee],
        MirType::Dynamic,
    ));
    let bytes = crate::emit_assembly(
        &program,
        &crate::EmitOptions {
            assembly_name: "lexical_super_sites".into(),
            entry_source_path: None,
        },
    )
    .unwrap();
    let res = Resolution::parse(&bytes, ReadOptions::default()).unwrap();
    let method = res
        .type_definitions
        .iter()
        .flat_map(|ty| &ty.methods)
        .find(|method| method.name == "sequence")
        .unwrap();
    let instructions = &method.body.as_ref().unwrap().instructions;
    let sites: Vec<_> = instructions
        .iter()
        .enumerate()
        .filter_map(|(index, instruction)| {
            let text = instruction.show(&res);
            let offset = if text.contains("PyOps.CallZeroWithFrame(") {
                6
            } else if text.contains("PyPic.CallValue0(") {
                2
            } else {
                return None;
            };
            match instructions[index - offset] {
                Instruction::LoadConstantInt32(site) => Some(site),
                _ => panic!("每个零参调用须先载入一个明确PIC站点"),
            }
        })
        .collect();
    assert_eq!(sites, [0, 1, 2]);
}

#[test]
fn lexical_super_instance_functions_never_request_legacy_tls() {
    let mut locals = Arena::default();
    let output = local(&mut locals, MirType::Dynamic);
    let mut function = function(
        "probe",
        single_block(
            locals,
            vec![Statement::Assign {
                dest: output,
                value: Rvalue::Call {
                    callee: Callee::Runtime(RuntimeFn::DynBuiltin),
                    args: vec![Operand::Const(Constant::Str("super".into()))],
                },
            }],
            Some(Operand::Local(output)),
        ),
        vec![],
        MirType::Dynamic,
    );
    assert!(crate::callables::function_uses_super(&function));
    function.module_state_source = Some(InstanceStateSource::FunctionParameter);
    assert!(!crate::callables::function_uses_super(&function));
}
