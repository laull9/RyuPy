//! Double原语的真实PE契约：isinst/unbox.any与独立mul/add，不引入Python转换或融合算术助手。
use super::exact_i64_tests::{function, local, program, single_block};
use dotnetdll::prelude::*;
use la_arena::Arena;
use ryupy_mir::{
    BasicBlock, BinOp, Body, Constant, MirType, Operand, Rvalue, Statement, Terminator,
};

#[test]
fn exact_double_guard_unbox_and_arithmetic_are_mechanical_clr_operations() {
    let mut program = program();
    let mut locals = Arena::default();
    let input = local(&mut locals, MirType::Dynamic);
    let guard = local(&mut locals, MirType::Bool);
    let value = local(&mut locals, MirType::F64);
    let mut blocks = Arena::default();
    let entry = blocks.alloc(BasicBlock {
        stmts: vec![Statement::Assign {
            dest: guard,
            value: Rvalue::IsBoxedF64 {
                value: Operand::Local(input),
            },
        }],
        terminator: Terminator::Unreachable,
    });
    let yes = blocks.alloc(BasicBlock {
        stmts: vec![Statement::Assign {
            dest: value,
            value: Rvalue::UnboxExactF64 {
                value: Operand::Local(input),
            },
        }],
        terminator: Terminator::Return(Some(Operand::Local(value))),
    });
    let no = blocks.alloc(BasicBlock {
        stmts: vec![],
        terminator: Terminator::Return(Some(Operand::Const(Constant::F64(0.0)))),
    });
    blocks[entry].terminator = Terminator::Branch {
        cond: Operand::Local(guard),
        then_: yes,
        else_: no,
    };
    program.functions.alloc(function(
        "guarded_double",
        Body {
            direct_globals_contracts: Vec::new(),
            source_handler_contracts: Vec::new(),
            locals,
            blocks,
            entry,
            regions: vec![],
        },
        vec![input],
        MirType::F64,
    ));
    let mut locals = Arena::default();
    let input = local(&mut locals, MirType::Dynamic);
    let value = local(&mut locals, MirType::F64);
    program.functions.alloc(function(
        "converting_double",
        single_block(
            locals,
            vec![Statement::Assign {
                dest: value,
                value: Rvalue::Unbox {
                    value: Operand::Local(input),
                    to: MirType::F64,
                },
            }],
            Some(Operand::Local(value)),
        ),
        vec![input],
        MirType::F64,
    ));
    let mut locals = Arena::default();
    let a = local(&mut locals, MirType::F64);
    let b = local(&mut locals, MirType::F64);
    let c = local(&mut locals, MirType::F64);
    let product = local(&mut locals, MirType::F64);
    let result = local(&mut locals, MirType::F64);
    program.functions.alloc(function(
        "double_chain",
        single_block(
            locals,
            vec![
                Statement::Assign {
                    dest: product,
                    value: Rvalue::Binary {
                        op: BinOp::Mul,
                        lhs: Operand::Local(a),
                        rhs: Operand::Local(b),
                    },
                },
                Statement::Assign {
                    dest: result,
                    value: Rvalue::Binary {
                        op: BinOp::Add,
                        lhs: Operand::Local(product),
                        rhs: Operand::Local(c),
                    },
                },
            ],
            Some(Operand::Local(result)),
        ),
        vec![a, b, c],
        MirType::F64,
    ));
    let bytes = crate::emit_assembly(
        &program,
        &crate::EmitOptions {
            assembly_name: "exact_f64".into(),
            entry_source_path: None,
        },
    )
    .unwrap();
    let res = Resolution::parse(&bytes, ReadOptions::default()).unwrap();
    let instructions = |name: &str| {
        &res.type_definitions
            .iter()
            .flat_map(|t| &t.methods)
            .find(|m| m.name == name)
            .unwrap()
            .body
            .as_ref()
            .unwrap()
            .instructions
    };
    let exact = instructions("guarded_double");
    let guard = exact
        .iter()
        .position(|i| matches!(i, Instruction::IsInstance(_)))
        .unwrap();
    assert!(exact[guard].show(&res).contains("double"));
    assert!(matches!(exact[guard + 1], Instruction::LoadNull));
    assert!(matches!(exact[guard + 2], Instruction::CompareEqual));
    assert!(matches!(
        exact[guard + 3],
        Instruction::LoadConstantInt32(0)
    ));
    assert!(matches!(exact[guard + 4], Instruction::CompareEqual));
    assert!(
        exact
            .iter()
            .find(|i| matches!(i, Instruction::UnboxIntoValue(_)))
            .unwrap()
            .show(&res)
            .contains("double")
    );
    assert!(!exact.iter().any(|i| i.show(&res).contains("PyCast")));
    assert!(
        instructions("converting_double")
            .iter()
            .any(|i| i.show(&res).contains("ToFloat64"))
    );
    let chain = instructions("double_chain");
    assert_eq!(
        chain
            .iter()
            .filter(|i| matches!(i, Instruction::Multiply))
            .count(),
        1
    );
    assert_eq!(
        chain
            .iter()
            .filter(|i| matches!(i, Instruction::Add))
            .count(),
        1
    );
    assert!(!chain.iter().any(|i| matches!(
        i,
        Instruction::AddOverflow(_) | Instruction::MultiplyOverflow(_)
    )));
    assert!(
        !chain
            .iter()
            .any(|i| i.show(&res).contains("PyPic") || i.show(&res).contains("FusedMultiply"))
    );
}
