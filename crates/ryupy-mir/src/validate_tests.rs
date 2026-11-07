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
        module_state: None,
        instance_capabilities: Default::default(),
        module_state_source: None,
        name: "main".to_string(),
        codegen_name: "main".to_string(),
        source_file: "t.py".to_string(),
        definition: None,
        params: Vec::new(),
        ret: MirType::Unit,
        body: crate::mir::Body {
            source_handler_contracts: Vec::new(),
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
        dynamic_origin: None,
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

#[path = "validate_local_lookup_tests.rs"]
mod local_lookup_tests;
