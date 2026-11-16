//! 读回真实PE检查locals ABI和initlocals，缓存初值必须是每次调用独立的null。
use dotnetdll::prelude::*;
use ryupy_mir::{Callee, Constant, MirType, Operand, RuntimeFn, Rvalue, Statement};

#[test]
fn function_locals_emits_exact_abi_and_initialized_per_call_cache() {
    let mut program = crate::exact_i64_tests::program();
    let f = &mut program.functions[program.modules[program.entry].init];
    let cache = crate::exact_i64_tests::local(&mut f.body.locals, MirType::Dynamic);
    f.body.blocks[f.body.entry].stmts = vec![
        Statement::Eval(Rvalue::Call {
            callee: Callee::Runtime(RuntimeFn::FunctionLocalsValidate),
            args: vec![Operand::Array(vec![]); 3],
        }),
        Statement::Assign {
            dest: cache,
            value: Rvalue::Call {
                callee: Callee::Runtime(RuntimeFn::FunctionLocalsEnsure),
                args: vec![Operand::Local(cache)],
            },
        },
        Statement::Eval(Rvalue::Call {
            callee: Callee::Runtime(RuntimeFn::FunctionLocalsSync),
            args: vec![
                Operand::Local(cache),
                Operand::Const(Constant::Str("x".into())),
                Operand::Const(Constant::Null),
            ],
        }),
    ];
    let bytes = crate::emit_assembly(
        &program,
        &crate::EmitOptions {
            assembly_name: "FunctionLocalsProbe".into(),
            entry_source_path: None,
        },
    )
    .unwrap();
    let res = Resolution::parse(&bytes, ReadOptions::default()).unwrap();
    for (name, signature) in [
        (
            "FunctionLocalsValidate",
            msig! { static void(object[], object[], object[]) },
        ),
        ("FunctionLocalsEnsure", msig! { static object(object) }),
        (
            "FunctionLocalsSync",
            msig! { static void(object, string, object) },
        ),
    ] {
        assert_eq!(
            res.method_references
                .iter()
                .find(|m| m.name == name)
                .unwrap()
                .signature,
            signature
        );
        let bodies: Vec<_> = res
            .type_definitions
            .iter()
            .flat_map(|t| &t.methods)
            .filter_map(|m| m.body.as_ref())
            .filter(|body| {
                body.instructions.iter().any(|ins| {
                    matches!(ins, Instruction::Call { .. }) && ins.show(&res).contains(name)
                })
            })
            .collect();
        assert_eq!(bodies.len(), 1);
        assert!(bodies[0].header.initialize_locals);
    }
}
