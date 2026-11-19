//! 真实PE中的Vector128<double>值类型局部、泛型MethodSpec与独立标量对照。
//! 输出函数只暴露double；后续运行时可逐bit比较不同NaN payload及硬件关闭回退。
use super::exact_i64_tests::{function, local, program};
use dotnetdll::prelude::*;
use la_arena::Arena;
use ryupy_mir::{BasicBlock, BinOp, Body, MirType, Operand, Rvalue, Statement, Terminator};

#[test]
fn vector128_methods_encode_generic_value_locals_and_method_specs() {
    let mut program = program();
    for (name, op) in [
        ("add", BinOp::Add),
        ("sub", BinOp::Sub),
        ("mul", BinOp::Mul),
    ] {
        for lane in 0..2 {
            let mut locals = Arena::default();
            let params: Vec<_> = (0..4).map(|_| local(&mut locals, MirType::F64)).collect();
            let guard = local(&mut locals, MirType::Bool);
            let left = local(&mut locals, MirType::V128F64);
            let right = local(&mut locals, MirType::V128F64);
            let vector = local(&mut locals, MirType::V128F64);
            let result = local(&mut locals, MirType::F64);
            let mut blocks = Arena::default();
            let entry = blocks.alloc(BasicBlock {
                stmts: vec![Statement::Assign {
                    dest: guard,
                    value: Rvalue::IsVector128Supported,
                }],
                terminator: Terminator::Unreachable,
            });
            let yes = blocks.alloc(BasicBlock {
                stmts: vec![
                    Statement::Assign {
                        dest: left,
                        value: Rvalue::VectorPackF64 {
                            low: Operand::Local(params[0]),
                            high: Operand::Local(params[1]),
                        },
                    },
                    Statement::Assign {
                        dest: right,
                        value: Rvalue::VectorPackF64 {
                            low: Operand::Local(params[2]),
                            high: Operand::Local(params[3]),
                        },
                    },
                    Statement::Assign {
                        dest: vector,
                        value: Rvalue::VectorBinaryF64 {
                            op,
                            lhs: Operand::Local(left),
                            rhs: Operand::Local(right),
                        },
                    },
                    Statement::Assign {
                        dest: result,
                        value: Rvalue::VectorLaneF64 {
                            value: Operand::Local(vector),
                            lane,
                        },
                    },
                ],
                terminator: Terminator::Return(Some(Operand::Local(result))),
            });
            let no = blocks.alloc(BasicBlock {
                stmts: vec![Statement::Assign {
                    dest: result,
                    value: Rvalue::Binary {
                        op,
                        lhs: Operand::Local(params[usize::from(lane)]),
                        rhs: Operand::Local(params[usize::from(lane) + 2]),
                    },
                }],
                terminator: Terminator::Return(Some(Operand::Local(result))),
            });
            blocks[entry].terminator = Terminator::Branch {
                cond: Operand::Local(guard),
                then_: yes,
                else_: no,
            };
            program.functions.alloc(function(
                &format!("vector_{name}_{lane}"),
                Body {
                    direct_globals_contracts: Vec::new(),
                    source_handler_contracts: Vec::new(),
                    locals,
                    blocks,
                    entry,
                    regions: vec![],
                },
                params,
                MirType::F64,
            ));
        }
    }
    let bytes = crate::emit_assembly(
        &program,
        &crate::EmitOptions {
            assembly_name: "vector128_contract".into(),
            entry_source_path: None,
        },
    )
    .unwrap();
    let res = Resolution::parse(&bytes, ReadOptions::default()).unwrap();
    for method in res
        .type_definitions
        .iter()
        .flat_map(|ty| &ty.methods)
        .filter(|method| method.name.starts_with("vector_"))
    {
        let body = method.body.as_ref().unwrap();
        let local_types: Vec<_> = body
            .header
            .local_variables
            .iter()
            .map(|local| local.show(&res))
            .collect();
        assert_eq!(
            local_types
                .iter()
                .filter(|ty| ty.contains("Vector128") && ty.contains("double"))
                .count(),
            3
        );
        let text: Vec<_> = body
            .instructions
            .iter()
            .map(|instruction| instruction.show(&res))
            .collect();
        assert_eq!(
            text.iter()
                .filter(|line| line.contains("get_IsHardwareAccelerated"))
                .count(),
            1
        );
        assert_eq!(
            text.iter()
                .filter(|line| line.contains("::Create") || line.contains(".Create"))
                .count(),
            2
        );
        for required in [
            if method.name.contains("_add_") {
                "Add"
            } else if method.name.contains("_sub_") {
                "Subtract"
            } else {
                "Multiply"
            },
            "GetElement",
        ] {
            let call = body
                .instructions
                .iter()
                .find(|instruction| instruction.show(&res).contains(required))
                .unwrap();
            assert!(
                matches!(
                    call,
                    Instruction::Call {
                        param0: MethodSource::Generic(_),
                        ..
                    }
                ),
                "{}",
                call.show(&res)
            );
            assert!(call.show(&res).contains("double"));
        }
        assert!(!text.iter().any(|line| line.contains("Fused")
            || line.contains("PyOps")
            || line.contains("PyPic")));
    }
}
