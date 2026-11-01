//! MIR 基本合法性回归。
use super::*;
use crate::mir::{BasicBlock, Function, MirType, Terminator};
use la_arena::Arena;

#[test]
fn catches_unreachable_terminator() {
    let mut functions = Arena::default();
    let mut blocks = Arena::default();
    let entry = blocks.alloc(BasicBlock {
        stmts: Vec::new(),
        terminator: Terminator::Unreachable,
    });
    let func = Function {
        name: "main".to_string(),
        codegen_name: "main".to_string(),
        source_file: "t.py".to_string(),
        params: Vec::new(),
        ret: MirType::Unit,
        body: crate::mir::Body {
            locals: Arena::default(),
            blocks,
            entry,
            regions: Vec::new(),
        },
        param_meta: Vec::new(),
        function_global: None,
        specialization_identity_global: None,
        defaults_global: None,
        signature_global: None,
        env: None,
    };
    let entry_fn = functions.alloc(func);
    let prog = Program {
        functions,
        classes: Arena::default(),
        globals: Arena::default(),
        entry: entry_fn,
        source_name: "t.py".to_string(),
    };
    assert!(validate(&prog).is_err());
}
