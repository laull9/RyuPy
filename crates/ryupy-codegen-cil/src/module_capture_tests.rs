//! 双局部的真实PE协议：bool在独立局部、固定/数组入口参数顺序正确，不生成捕获对象。
use dotnetdll::prelude::*;
use ryupy_mir::{Callee, Constant, MirType, Operand, RuntimeFn, Rvalue, Statement};

#[test]
fn module_capture_pair_uses_exact_abis_and_no_token_allocation() {
    for arity in 0..=4 {
        let mut program = crate::exact_i64_tests::program();
        let function = &mut program.functions[program.modules[program.entry].init];
        let receiver = crate::exact_i64_tests::local(&mut function.body.locals, MirType::Dynamic);
        let flag = crate::exact_i64_tests::local(&mut function.body.locals, MirType::Bool);
        let target = crate::exact_i64_tests::local(&mut function.body.locals, MirType::Dynamic);
        let result = crate::exact_i64_tests::local(&mut function.body.locals, MirType::Dynamic);
        let call = if arity < 4 {
            Rvalue::DynCallMethod {
                captured: Some(Operand::Local(flag)),
                obj: Operand::Local(target),
                name: "f".into(),
                args: vec![Operand::Const(Constant::Null); arity],
            }
        } else {
            Rvalue::Call {
                callee: Callee::Runtime(RuntimeFn::CallCapturedMethod),
                args: vec![
                    Operand::Local(flag),
                    Operand::Local(target),
                    Operand::Const(Constant::Str("f".into())),
                    Operand::Array(vec![]),
                    Operand::Array(vec![]),
                    Operand::Array(vec![]),
                ],
            }
        };
        function.body.blocks[function.body.entry].stmts = vec![
            Statement::Assign {
                dest: receiver,
                value: Rvalue::Use(Operand::Const(Constant::Null)),
            },
            Statement::Assign {
                dest: flag,
                value: Rvalue::Call {
                    callee: Callee::Runtime(RuntimeFn::IsModule),
                    args: vec![Operand::Local(receiver)],
                },
            },
            Statement::Assign {
                dest: target,
                value: Rvalue::Call {
                    callee: Callee::Runtime(RuntimeFn::CaptureModuleTarget),
                    args: vec![
                        Operand::Local(receiver),
                        Operand::Const(Constant::Str("f".into())),
                    ],
                },
            },
            Statement::Assign {
                dest: result,
                value: call,
            },
        ];
        let bytes = crate::emit_assembly(
            &program,
            &crate::EmitOptions {
                assembly_name: "module_pair".into(),
                entry_source_path: None,
            },
        )
        .unwrap();
        let res = Resolution::parse(&bytes, ReadOptions::default()).unwrap();
        let signature = |name: &str| {
            res.method_references
                .iter()
                .find(|m| m.name == name)
                .unwrap()
                .signature
                .clone()
        };
        assert_eq!(signature("IsModule"), msig! {static bool(object)});
        assert_eq!(
            signature("CaptureModuleTarget"),
            msig! {static object(object,string)}
        );
        assert_eq!(
            signature("CallCapturedMethod"),
            msig! {static object(bool,object,string,object[],object[],object[])}
        );
        assert_eq!(
            signature("CallCapturedMethod0"),
            msig! {static object(int,bool,object,string)}
        );
        assert_eq!(
            signature("CallCapturedMethod1"),
            msig! {static object(int,bool,object,string,object)}
        );
        assert_eq!(
            signature("CallCapturedMethod2"),
            msig! {static object(int,bool,object,string,object,object)}
        );
        assert_eq!(
            signature("CallCapturedMethod3"),
            msig! {static object(int,bool,object,string,object,object,object)}
        );
        assert!(
            !res.method_references
                .iter()
                .any(|m| m.name == "CaptureModuleMethod")
        );
        let main = res
            .type_definitions
            .iter()
            .flat_map(|t| &t.methods)
            .find(|m| m.name == "__main__")
            .unwrap();
        let instructions = &main.body.as_ref().unwrap().instructions;
        let is_module = instructions
            .iter()
            .position(|i| i.show(&res).contains("IsModule("))
            .unwrap();
        let capture = instructions
            .iter()
            .position(|i| i.show(&res).contains("CaptureModuleTarget("))
            .unwrap();
        let call_name = if arity < 4 {
            format!("CallCapturedMethod{arity}(")
        } else {
            "CallCapturedMethod(".into()
        };
        let invoke = instructions
            .iter()
            .position(|i| i.show(&res).contains(&call_name))
            .unwrap();
        assert!(is_module < capture && capture < invoke);
        let Instruction::StoreLocal(flag_slot) = instructions[is_module + 1] else {
            panic!("flag not stored")
        };
        let Instruction::StoreLocal(target_slot) = instructions[capture + 1] else {
            panic!("target not stored")
        };
        let loads: Vec<_> = instructions[capture + 2..invoke]
            .iter()
            .filter_map(|i| match i {
                Instruction::LoadLocal(id) => Some(*id),
                _ => None,
            })
            .collect();
        assert_eq!(loads, vec![flag_slot, target_slot]);
        assert!(
            !instructions
                .iter()
                .any(|i| matches!(i, Instruction::NewObject(_)))
        );
    }
}
