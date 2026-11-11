//! 警告用户子类的真实PE构造函数必须把原始args交给运行时基类消息策略。

use dotnetdll::prelude::*;
use la_arena::Arena;
use ryupy_mir::{BasicBlock, Body, Function, LinkedProgram, MirType, ModuleDef, Terminator};

#[test]
fn serialized_warning_constructor_defers_message_policy_to_runtime() {
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
    let mut classes = Arena::default();
    classes.alloc(ryupy_mir::ClassDef {
        class_cell: None,
        name: "ProjectWarning".into(),
        codegen_name: "ProjectWarning".into(),
        fields: vec![],
        is_value_type: false,
        is_dataclass: false,
        base: None,
        builtin_base: Some("UserWarning".into()),
        is_exception: true,
        factory_global: None,
    });
    let program = LinkedProgram {
        candidate_modules: Vec::new(),
        metadata_unavailable: None,
        optional_sources: None,
        distribution_metadata: None,
        trace_product: None,
        functions,
        modules,
        classes,
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
    let constructor = resolution
        .type_definitions
        .iter()
        .find(|ty| ty.name == "_ProjectWarning")
        .unwrap()
        .methods
        .iter()
        .find(|method| method.name == ".ctor")
        .unwrap();
    let instructions = &constructor.body.as_ref().unwrap().instructions;
    assert!(instructions[2].show(&resolution).contains("UserWarning"));
    assert!(matches!(instructions[3], Instruction::LoadArgument(2)));
    let message_call = instructions[4].show(&resolution);
    assert!(
        message_call.contains("UserExceptionMessageForBase"),
        "{message_call}"
    );
    assert!(
        matches!(instructions[5], Instruction::LoadArgument(2)),
        "原始args必须继续传给protected基类ctor"
    );
    assert!(instructions[6].show(&resolution).contains("PyUserWarning"));
}
