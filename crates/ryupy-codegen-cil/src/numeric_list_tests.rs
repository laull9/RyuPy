//! 数值列表ABI的真实PE验证：F64参数/返回保留原生double，None结果仍是object。
use super::exact_i64_tests::{function, local, program, single_block};
use dotnetdll::prelude::*;
use la_arena::Arena;
use ryupy_mir::{Callee, MirType, Operand, RuntimeFn, Rvalue, Statement};

#[test]
fn numeric_list_abi_preserves_unboxed_double_and_object_results() {
    let mut program = program();
    let cases = [
        (
            "ReadCheckedF64",
            RuntimeFn::NumericListReadCheckedF64,
            vec![MirType::Dynamic, MirType::Dynamic],
            MirType::F64,
            "double",
        ),
        (
            "Create",
            RuntimeFn::NumericListCreate,
            vec![],
            MirType::Dynamic,
            "object",
        ),
        (
            "AppendF64",
            RuntimeFn::NumericListAppendF64,
            vec![MirType::Dynamic, MirType::F64],
            MirType::Dynamic,
            "object",
        ),
        (
            "PeekList",
            RuntimeFn::NumericListPeekList,
            vec![MirType::Dynamic, MirType::Dynamic],
            MirType::Dynamic,
            "object",
        ),
        (
            "CanReadF64",
            RuntimeFn::NumericListCanReadF64,
            vec![MirType::Dynamic, MirType::Dynamic],
            MirType::Bool,
            "bool",
        ),
        (
            "ReadF64",
            RuntimeFn::NumericListReadF64,
            vec![MirType::Dynamic, MirType::Dynamic],
            MirType::F64,
            "double",
        ),
    ];
    for (name, callee, types, ret, _) in &cases {
        let mut locals = Arena::default();
        let params: Vec<_> = types
            .iter()
            .map(|ty| local(&mut locals, ty.clone()))
            .collect();
        let result = local(&mut locals, ret.clone());
        let call = Statement::Assign {
            dest: result,
            value: Rvalue::Call {
                callee: Callee::Runtime(*callee),
                args: params.iter().map(|id| Operand::Local(*id)).collect(),
            },
        };
        program.functions.alloc(function(
            name,
            single_block(locals, vec![call], Some(Operand::Local(result))),
            params,
            ret.clone(),
        ));
    }
    let bytes = crate::emit_assembly(
        &program,
        &crate::EmitOptions {
            assembly_name: "numeric_list_abi".into(),
            entry_source_path: None,
        },
    )
    .unwrap();
    let res = Resolution::parse(&bytes, ReadOptions::default()).unwrap();
    for (name, _, _, _, return_name) in &cases {
        let method = res
            .type_definitions
            .iter()
            .flat_map(|ty| &ty.methods)
            .find(|method| method.name == *name)
            .unwrap();
        let instructions = &method.body.as_ref().unwrap().instructions;
        let calls: Vec<_> = instructions
            .iter()
            .filter_map(|instruction| {
                let text = instruction.show(&res);
                text.contains("PyNumericListOps").then_some(text)
            })
            .collect();
        assert_eq!(calls.len(), 1);
        assert!(calls[0].contains(name), "{}", calls[0]);
        assert!(calls[0].contains(return_name), "{}", calls[0]);
        assert!(
            !instructions
                .iter()
                .any(|instruction| matches!(instruction, Instruction::BoxValue(_)))
        );
        if *name == "AppendF64" {
            assert!(calls[0].contains("double"));
        }
    }
}
