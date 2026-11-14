//! 定义来源不随链接符号或生成器恢复函数变名，签名/源行号的非法组合必须报错。
use crate::*;
use la_arena::Arena;

fn program() -> Program {
    let mut globals = Arena::default();
    let signature = globals.alloc(GlobalDecl {
        storage: crate::GlobalStorage::LegacyStatic,
        name: "signature".into(),
        ty: MirType::Dynamic,
    });
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
        definition: Some(DefinitionInfo {
            lexical_qualname: "Owner.factory.<locals>.child".into(),
            firstlineno: 37,
        }),
        name: "child".into(),
        codegen_name: "internal_symbol".into(),
        source_file: "definition.py".into(),
        params: vec![],
        ret: MirType::Dynamic,
        body: Body {
            direct_globals_contracts: Vec::new(),
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
        signature_global: Some(signature),
        env: None,
        dynamic_origin: None,
    });
    Program {
        functions,
        globals,
        classes: Arena::default(),
        entry,
        source_name: "definition.py".into(),
    }
}

#[test]
fn function_definition_link_preserves_python_source_without_prefix() {
    let program = program();
    let meta = UnitMeta {
        instance_capabilities: Default::default(),
        execution: crate::ModuleExecutionKind::LegacySingleton,
        environment_reason: "existing-contract".into(),
        location: Default::default(),
        file_backed_by_global: false,
        is_package: false,
        protected_exports: vec![],
        name: "module".into(),
        package: String::new(),
        file: "definition.py".into(),
        exports: vec![],
    };
    let linked = link(vec![(program, meta)], 0);
    let function = &linked.functions[linked.modules[linked.entry].init];
    assert_eq!(function.codegen_name, "m0_internal_symbol");
    assert_eq!(
        function.definition.as_ref().unwrap(),
        &DefinitionInfo {
            lexical_qualname: "Owner.factory.<locals>.child".into(),
            firstlineno: 37,
        }
    );
}

#[test]
fn function_definition_generator_keeps_source_on_factory_only() {
    let mut program = program();
    let id = program.entry;
    let function = &mut program.functions[id];
    let resume = function.body.blocks.alloc(BasicBlock {
        stmts: vec![],
        terminator: Terminator::Return(Some(Operand::Const(Constant::None))),
    });
    function.body.blocks[function.body.entry].terminator = Terminator::Suspend {
        value: Operand::Const(Constant::None),
        input: None,
        delegated: false,
        resume,
    };
    crate::generators::lower(&mut program, &[id], &Default::default());
    crate::validate::validate(&program).unwrap();
    assert_eq!(
        program.functions[id]
            .definition
            .as_ref()
            .unwrap()
            .firstlineno,
        37
    );
    assert_eq!(program.functions.len(), 2);
    for (other, function) in program.functions.iter() {
        if other != id {
            assert!(function.definition.is_none());
            assert!(function.signature_global.is_none());
        }
    }
}

#[test]
fn function_definition_rejects_missing_signature_and_zero_line() {
    for missing in [false, true] {
        let mut program = program();
        let function = &mut program.functions[program.entry];
        if missing {
            function.signature_global = None;
        } else {
            function.definition.as_mut().unwrap().firstlineno = 0;
        }
        assert!(
            crate::validate::validate(&program)
                .unwrap_err()
                .iter()
                .any(|e| e.contains("定义来源"))
        );
    }
}

#[test]
fn function_definition_rejects_signature_shared_by_distinct_definitions() {
    let other_program = program();
    let mut program = program();
    let other = other_program.functions.into_iter().next().unwrap().1;
    program.functions.alloc(other);
    assert!(
        crate::validate::validate(&program)
            .unwrap_err()
            .iter()
            .any(|e| e.contains("定义签名被重复使用"))
    );
}
