//! 真实PE的异常表容量回归：small EH头只有一字节长度，21条子句必须升级fat。
//! 不以CIL文本代替序列化验证，避免正确IR被二进制长度截断后悄然失去catch。
use dotnetdll::prelude::*;
use la_arena::Arena;
use ryupy_mir::{
    BasicBlock, Body, Function, LinkedProgram, LocalDecl, MirType, ModuleDef, Region, RegionKind,
    Rvalue, Statement, Terminator,
};

fn serialize_clauses(count: usize) -> Vec<u8> {
    let mut locals = Arena::default();
    let caught = locals.alloc(LocalDecl {
        ty: MirType::Dynamic,
        name: None,
        is_cell: false,
    });
    let mut blocks = Arena::default();
    let mut regions = Vec::new();
    let mut previous: Option<ryupy_mir::BlockId> = None;
    let mut first = None;
    for _ in 0..count {
        let protected = blocks.alloc(BasicBlock {
            stmts: vec![],
            terminator: Terminator::Unreachable,
        });
        first.get_or_insert(protected);
        if let Some(previous) = previous {
            blocks[previous].terminator = Terminator::Goto(protected);
        }
        let handler = blocks.alloc(BasicBlock {
            stmts: vec![Statement::Assign {
                dest: caught,
                value: Rvalue::CatchException,
            }],
            terminator: Terminator::Unreachable,
        });
        let after = blocks.alloc(BasicBlock {
            stmts: vec![],
            terminator: Terminator::Return(None),
        });
        blocks[protected].terminator = Terminator::Goto(after);
        blocks[handler].terminator = Terminator::Goto(after);
        previous = Some(after);
        regions.push(Region {
            source_handler_chain: None,
            kind: RegionKind::CatchAll,
            try_start: protected,
            try_len: 1,
            handler,
            exc_local: Some(caught),
        });
    }
    let mut functions = Arena::default();
    let init = functions.alloc(Function {
        name: "__main__".into(),
        codegen_name: "eh_capacity".into(),
        source_file: "eh.py".into(),
        definition: None,
        params: vec![],
        ret: MirType::Unit,
        body: Body {
            source_handler_contracts: Vec::new(),
            locals,
            blocks,
            entry: first.unwrap(),
            regions,
        },
        param_meta: vec![],
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
        file: "eh.py".into(),
        init,
        exports: vec![],
    });
    let program = LinkedProgram {
        functions,
        modules,
        classes: Arena::default(),
        globals: Arena::default(),
        entry,
    };
    crate::emit_assembly(
        &program,
        &crate::EmitOptions {
            assembly_name: "eh_capacity".into(),
            entry_source_path: None,
        },
    )
    .unwrap()
}

#[test]
fn serialized_exception_table_keeps_twenty_and_twenty_one_clauses() {
    for count in [20, 21] {
        let bytes = serialize_clauses(count);
        let resolution = Resolution::parse(&bytes, ReadOptions::default()).unwrap();
        let method = resolution
            .type_definitions
            .iter()
            .flat_map(|ty| &ty.methods)
            .find(|method| method.name == "eh_capacity")
            .unwrap();
        let actual: usize = method
            .body
            .as_ref()
            .unwrap()
            .data_sections
            .iter()
            .map(|section| match section {
                body::DataSection::ExceptionHandlers(clauses) => clauses.len(),
                _ => 0,
            })
            .sum();
        assert_eq!(actual, count, "真实PE不得因small EH长度截断丢失处理器");
    }
}
