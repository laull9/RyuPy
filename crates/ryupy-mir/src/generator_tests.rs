//! 错误的暂停 IR 不能越过 MIR 验证边界进入后端。

use crate::mir::*;
use la_arena::Arena;

fn program() -> Program {
    let mut blocks = Arena::default();
    let entry = blocks.alloc(BasicBlock {
        stmts: vec![],
        terminator: Terminator::Return(Some(Operand::Const(Constant::None))),
    });
    let mut functions = Arena::default();
    let entry = functions.alloc(Function {
        module_state: None,
        instance_capabilities: Default::default(),
        module_state_source: None,
        name: "生成器测试".into(),
        codegen_name: "test".into(),
        source_file: "test.py".into(),
        definition: None,
        params: vec![],
        ret: MirType::Dynamic,
        body: Body {
            source_handler_contracts: Vec::new(),
            locals: Arena::default(),
            blocks,
            entry,
            regions: vec![],
        },
        param_meta: vec![],
        function_global: None,
        specialization_identity_global: None,
        defaults_global: None,
        signature_global: None,
        env: None,
        dynamic_origin: None,
    });
    Program {
        functions,
        classes: Arena::default(),
        globals: Arena::default(),
        entry,
        source_name: "test.py".into(),
    }
}

#[test]
fn rejects_suspend_before_codegen() {
    let mut program = program();
    let body = &mut program.functions[program.entry].body;
    body.blocks[body.entry].terminator = Terminator::Suspend {
        value: Operand::Const(Constant::None),
        input: None,
        delegated: false,
        resume: body.entry,
    };
    assert!(crate::validate::validate(&program).is_err());
}

#[test]
fn rejects_invalid_resume_and_frame() {
    let mut program = program();
    let entry = program.entry;
    let body = &mut program.functions[entry].body;
    body.blocks[body.entry]
        .stmts
        .push(Statement::Eval(Rvalue::MakeGenerator {
            resume: entry,
            frame: vec![Operand::Const(Constant::I64(0))],
            origin: None,
        }));
    assert!(crate::validate::validate(&program).is_err());
}
