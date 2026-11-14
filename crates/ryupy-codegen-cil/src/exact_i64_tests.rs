//! 精确 Int64 原语的真实 PE 回归：直接类型测试/拆箱/回绕加法不调用转换助手。
//!
//! 这些测试只核对机械后端契约；守卫的支配证明和溢出回退属于 MIR 测试。

use dotnetdll::prelude::*;
use la_arena::Arena;
use ryupy_mir::{
    BasicBlock, BinOp, Body, Callee, Constant, Function, LinkedProgram, LocalDecl, LocalId,
    MirType, ModuleDef, Operand, RuntimeFn, Rvalue, Statement, Terminator,
};

pub(super) fn local(locals: &mut Arena<LocalDecl>, ty: MirType) -> LocalId {
    locals.alloc(LocalDecl {
        ty,
        name: None,
        is_cell: false,
    })
}

pub(super) fn function(name: &str, body: Body, params: Vec<LocalId>, ret: MirType) -> Function {
    Function {
        name: name.into(),
        codegen_name: name.into(),
        source_file: "exact_i64.py".into(),
        definition: None,
        params,
        ret,
        body,
        param_meta: Vec::new(),
        function_global: None,
        specialization_identity_global: None,
        defaults_global: None,
        signature_global: None,
        env: None,
        dynamic_origin: None,
        module_state: None,
        instance_capabilities: Default::default(),
        module_state_source: None,
    }
}

pub(super) fn single_block(
    locals: Arena<LocalDecl>,
    stmts: Vec<Statement>,
    ret: Option<Operand>,
) -> Body {
    let mut blocks = Arena::default();
    let entry = blocks.alloc(BasicBlock {
        stmts,
        terminator: Terminator::Return(ret),
    });
    Body {
        direct_globals_contracts: Vec::new(),
        source_handler_contracts: Vec::new(),
        locals,
        blocks,
        entry,
        regions: Vec::new(),
    }
}

pub(super) fn program() -> LinkedProgram {
    let mut functions = Arena::default();
    let init = functions.alloc(function(
        "__main__",
        single_block(Arena::default(), Vec::new(), None),
        Vec::new(),
        MirType::Unit,
    ));
    let mut locals = Arena::default();
    let input = local(&mut locals, MirType::Dynamic);
    let guard = local(&mut locals, MirType::Bool);
    let integer = local(&mut locals, MirType::I64);
    let mut blocks = Arena::default();
    let entry = blocks.alloc(BasicBlock {
        stmts: vec![Statement::Assign {
            dest: guard,
            value: Rvalue::IsBoxedI64 {
                value: Operand::Local(input),
            },
        }],
        terminator: Terminator::Unreachable,
    });
    let fast = blocks.alloc(BasicBlock {
        stmts: vec![Statement::Assign {
            dest: integer,
            value: Rvalue::UnboxExactI64 {
                value: Operand::Local(input),
            },
        }],
        terminator: Terminator::Return(Some(Operand::Local(integer))),
    });
    let slow = blocks.alloc(BasicBlock {
        stmts: Vec::new(),
        terminator: Terminator::Return(Some(Operand::Const(Constant::I64(0)))),
    });
    blocks[entry].terminator = Terminator::Branch {
        cond: Operand::Local(guard),
        then_: fast,
        else_: slow,
    };
    functions.alloc(function(
        "guarded_exact",
        Body {
            direct_globals_contracts: Vec::new(),
            source_handler_contracts: Vec::new(),
            locals,
            blocks,
            entry,
            regions: Vec::new(),
        },
        vec![input],
        MirType::I64,
    ));
    for (name, op) in [
        ("checked_add", BinOp::Add),
        ("wrapping_add", BinOp::WrappingAdd),
    ] {
        let mut locals = Arena::default();
        let left = local(&mut locals, MirType::I64);
        let right = local(&mut locals, MirType::I64);
        let result = local(&mut locals, MirType::I64);
        let body = single_block(
            locals,
            vec![Statement::Assign {
                dest: result,
                value: Rvalue::Binary {
                    op,
                    lhs: Operand::Local(left),
                    rhs: Operand::Local(right),
                },
            }],
            Some(Operand::Local(result)),
        );
        functions.alloc(function(name, body, vec![left, right], MirType::I64));
    }
    let mut locals = Arena::default();
    let input = local(&mut locals, MirType::Dynamic);
    let result = local(&mut locals, MirType::I64);
    functions.alloc(function(
        "converting_unbox",
        single_block(
            locals,
            vec![Statement::Assign {
                dest: result,
                value: Rvalue::Unbox {
                    value: Operand::Local(input),
                    to: MirType::I64,
                },
            }],
            Some(Operand::Local(result)),
        ),
        vec![input],
        MirType::I64,
    ));
    let mut locals = Arena::default();
    let left = local(&mut locals, MirType::Dynamic);
    let right = local(&mut locals, MirType::Dynamic);
    let result = local(&mut locals, MirType::Dynamic);
    functions.alloc(function(
        "direct_binary_abi",
        single_block(
            locals,
            vec![Statement::Assign {
                dest: result,
                value: Rvalue::Call {
                    callee: Callee::Runtime(RuntimeFn::DynBinary),
                    args: vec![
                        Operand::Const(Constant::I64(0x101)),
                        Operand::Local(left),
                        Operand::Local(right),
                    ],
                },
            }],
            Some(Operand::Local(result)),
        ),
        vec![left, right],
        MirType::Dynamic,
    ));
    let mut modules = Arena::default();
    let entry = modules.alloc(ModuleDef {
        instance_capabilities: Default::default(),
        execution: ryupy_mir::ModuleExecutionKind::LegacySingleton,
        environment_reason: "existing-contract".into(),
        location: Default::default(),
        file_backed_by_global: false,
        is_package: false,
        protected_exports: Vec::new(),
        name: "__main__".into(),
        package: String::new(),
        file: "exact_i64.py".into(),
        init,
        exports: Vec::new(),
    });
    LinkedProgram {
        external_provider: None,
        candidate_modules: Vec::new(),
        metadata_unavailable: None,
        optional_sources: None,
        distribution_metadata: None,
        trace_product: None,
        functions,
        modules,
        classes: Arena::default(),
        globals: Arena::default(),
        entry,
    }
}

#[test]
fn serialized_exact_i64_uses_clr_primitives_without_changing_legacy_operations() {
    let bytes = crate::emit_assembly(
        &program(),
        &crate::EmitOptions {
            assembly_name: "exact_i64".into(),
            entry_source_path: None,
        },
    )
    .unwrap();
    let res = Resolution::parse(&bytes, ReadOptions::default()).unwrap();
    let instructions = |name: &str| {
        &res.type_definitions
            .iter()
            .flat_map(|ty| &ty.methods)
            .find(|method| method.name == name)
            .unwrap()
            .body
            .as_ref()
            .unwrap()
            .instructions
    };
    let exact = instructions("guarded_exact");
    let guard = exact
        .iter()
        .position(|ins| matches!(ins, Instruction::IsInstance(_)))
        .unwrap();
    assert!(exact[guard].show(&res).contains("long"));
    assert!(matches!(exact[guard + 1], Instruction::LoadNull));
    assert!(matches!(exact[guard + 2], Instruction::CompareEqual));
    assert!(matches!(
        exact[guard + 3],
        Instruction::LoadConstantInt32(0)
    ));
    assert!(matches!(exact[guard + 4], Instruction::CompareEqual));
    let unbox = exact
        .iter()
        .find(|ins| matches!(ins, Instruction::UnboxIntoValue(_)))
        .unwrap();
    assert!(unbox.show(&res).contains("long"));
    assert!(!exact.iter().any(|ins| ins.show(&res).contains("PyCast")));
    let wrapping = instructions("wrapping_add");
    assert!(wrapping.iter().any(|ins| matches!(ins, Instruction::Add)));
    assert!(
        !wrapping
            .iter()
            .any(|ins| matches!(ins, Instruction::AddOverflow(_)))
    );
    let checked = instructions("checked_add");
    assert!(
        checked
            .iter()
            .any(|ins| matches!(ins, Instruction::AddOverflow(NumberSign::Signed)))
    );
    assert!(!checked.iter().any(|ins| matches!(ins, Instruction::Add)));
    assert!(
        instructions("converting_unbox")
            .iter()
            .any(|ins| ins.show(&res).contains("ToInt64"))
    );
}

#[test]
fn serialized_direct_binary_converts_only_opcode_to_clr_int32() {
    let bytes = crate::emit_assembly(
        &program(),
        &crate::EmitOptions {
            assembly_name: "binary_abi".into(),
            entry_source_path: None,
        },
    )
    .unwrap();
    let res = Resolution::parse(&bytes, ReadOptions::default()).unwrap();
    let instructions = &res
        .type_definitions
        .iter()
        .flat_map(|ty| &ty.methods)
        .find(|method| method.name == "direct_binary_abi")
        .unwrap()
        .body
        .as_ref()
        .unwrap()
        .instructions;
    let call = instructions
        .iter()
        .position(|ins| ins.show(&res).contains("PyOps.Binary"))
        .expect("回退必须调用既有 PyOps.Binary，而不是 PIC");
    assert!(matches!(
        instructions[call - 4],
        Instruction::LoadConstantInt64(0x101)
    ));
    assert!(matches!(
        instructions[call - 3],
        Instruction::Convert(ConversionType::Int32)
    ));
    assert!(matches!(
        instructions[call - 2],
        Instruction::LoadArgument(0)
    ));
    assert!(matches!(
        instructions[call - 1],
        Instruction::LoadArgument(1)
    ));
    assert_eq!(
        instructions
            .iter()
            .filter(|ins| matches!(ins, Instruction::Convert(_)))
            .count(),
        1,
        "动态对象实参不能被窄化"
    );
    assert!(
        !instructions
            .iter()
            .any(|ins| ins.show(&res).contains("PyPic"))
    );
}

#[test]
fn serialized_i64_box_preserves_runtime_integer_cache_contract() {
    // 短链只物化末结果；该Box必须沿整数缓存助手，不能悄换成CLR直接装箱。
    let mut program = program();
    let mut locals = Arena::default();
    let input = local(&mut locals, MirType::I64);
    let result = local(&mut locals, MirType::Dynamic);
    program.functions.alloc(function(
        "materialize_i64",
        single_block(
            locals,
            vec![Statement::Assign {
                dest: result,
                value: Rvalue::Box {
                    value: Operand::Local(input),
                    from: MirType::I64,
                },
            }],
            Some(Operand::Local(result)),
        ),
        vec![input],
        MirType::Dynamic,
    ));
    let bytes = crate::emit_assembly(
        &program,
        &crate::EmitOptions {
            assembly_name: "integer_materialization".into(),
            entry_source_path: None,
        },
    )
    .unwrap();
    let res = Resolution::parse(&bytes, ReadOptions::default()).unwrap();
    let instructions = &res
        .type_definitions
        .iter()
        .flat_map(|ty| &ty.methods)
        .find(|method| method.name == "materialize_i64")
        .unwrap()
        .body
        .as_ref()
        .unwrap()
        .instructions;
    let calls: Vec<_> = instructions
        .iter()
        .enumerate()
        .filter(|(_, instruction)| instruction.show(&res).contains("PyOps.BoxInt"))
        .collect();
    assert_eq!(calls.len(), 1);
    let (index, call) = calls[0];
    assert!(
        call.show(&res)
            .contains("static object [RyuPy.Runtime]RyuPy.Runtime.PyOps.BoxInt(long)"),
        "{}",
        call.show(&res)
    );
    assert!(index > 0);
    assert!(matches!(
        instructions[index - 1],
        Instruction::LoadArgument(0)
    ));
    assert!(
        !instructions
            .iter()
            .any(|instruction| matches!(instruction, Instruction::BoxValue(_)))
    );
}
