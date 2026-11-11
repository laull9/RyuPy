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
        definition: None,
        params: Vec::new(),
        ret: MirType::Unit,
        body: Body {
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
        module_state: None,
        instance_capabilities: Default::default(),
        module_state_source: None,
    });
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
        file: "entry.py".into(),
        init,
        exports: Vec::new(),
    });
    let program = LinkedProgram {
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
    };
    let bytes = crate::emit_assembly(
        &program,
        &crate::EmitOptions {
            assembly_name: "entry".into(),
            entry_source_path: Some("/source/真实 entry.py".into()),
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
    assert!(
        matches!(body.instructions[3], Instruction::LoadArgument(0)),
        "入口必须传递真实 Main 参数，不重新解析宿主进程命令行"
    );
    assert!(body.instructions[0].show(&resolution).contains("entry.py"));
    assert!(
        body.instructions[2]
            .show(&resolution)
            .contains("/source/真实 entry.py")
    );
    let initialize = body.instructions[4].show(&resolution);
    assert!(
        initialize.contains("PySystem") && initialize.contains("Initialize"),
        "{initialize}"
    );
    let body::DataSection::ExceptionHandlers(handlers) = &body.data_sections[0] else {
        panic!("入口必须保留异常表");
    };
    assert_eq!(handlers.len(), 3);
    for (handler, (offset, length)) in handlers.iter().zip([(9, 3), (12, 4), (16, 4)]) {
        assert_eq!((handler.try_offset, handler.try_length), (2, 7));
        assert_eq!(
            (handler.handler_offset, handler.handler_length),
            (offset, length)
        );
    }
    assert!(matches!(body.instructions[20], Instruction::LoadLocal(0)));
    assert!(
        body.instructions[21]
            .show(&resolution)
            .contains("FinishExit")
    );
    assert!(matches!(body.instructions[22], Instruction::Return));
    for index in [8, 11, 15, 19] {
        assert!(matches!(body.instructions[index], Instruction::Leave(20)));
    }
}
