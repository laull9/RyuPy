//! 入口真实 PE 方法头回归：成功退出码必须来自已清零的局部变量。

use dotnetdll::prelude::*;
use la_arena::Arena;
use ryupy_mir::{BasicBlock, Body, Function, LinkedProgram, MirType, ModuleDef, Terminator};

#[test]
fn serialized_entry_initializes_exit_code() {
    let mut blocks = Arena::default();
    let entry = blocks.alloc(BasicBlock {
        stmts: Vec::new(),
        terminator: Terminator::Return(None),
    });
    let mut functions = Arena::default();
    let init = functions.alloc(Function {
        name: "__main__".into(),
        codegen_name: "__main__".into(),
        source_file: "entry.py".into(),
        params: Vec::new(),
        ret: MirType::Unit,
        body: Body {
            locals: Arena::default(),
            blocks,
            entry,
            regions: Vec::new(),
        },
        param_meta: Vec::new(),
        function_global: None,
        defaults_global: None,
        signature_global: None,
        env: None,
    });
    let mut modules = Arena::default();
    let entry = modules.alloc(ModuleDef {
        name: "__main__".into(),
        package: String::new(),
        file: "entry.py".into(),
        init,
        exports: Vec::new(),
    });
    let program = LinkedProgram {
        functions,
        modules,
        classes: Arena::default(),
        globals: Arena::default(),
        entry,
    };
    let bytes = crate::emit_assembly(
        &program,
        &crate::EmitOptions {
            assembly_name: "entry".into(),
        },
    )
    .unwrap();
    let resolution = Resolution::parse(&bytes, ReadOptions::default()).unwrap();
    let main = resolution
        .type_definitions
        .iter()
        .flat_map(|ty| &ty.methods)
        .find(|method| method.name == "Main")
        .expect("真实程序集须包含 Main");
    let body = main.body.as_ref().unwrap();
    assert!(
        body.header.initialize_locals,
        "成功路径读取退出码之前必须初始化"
    );
    assert_eq!(body.header.local_variables.len(), 1);
}
