//! 特化身份锚点的真实PE回归：只在定义执行点创建函数，锚点保存同一原始实例。
use dotnetdll::prelude::*;
use la_arena::Arena;
use ryupy_mir::{
    BasicBlock, Body, Function, GlobalDecl, LinkedProgram, LocalDecl, MirType, ModuleDef, Operand,
    Rvalue, Statement, Terminator,
};

fn empty_function(name: &str) -> Function {
    let mut blocks = Arena::default();
    let entry = blocks.alloc(BasicBlock {
        stmts: vec![],
        terminator: Terminator::Return(None),
    });
    Function {
        name: name.into(),
        codegen_name: name.into(),
        source_file: "identity.py".into(),
        definition: None,
        params: vec![],
        ret: MirType::Unit,
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
        module_state: None,
        instance_capabilities: Default::default(),
        module_state_source: None,
    }
}

#[test]
fn original_function_identity_is_duplicated_at_definition() {
    let mut globals = Arena::default();
    let public = globals.alloc(GlobalDecl {
        storage: ryupy_mir::GlobalStorage::LegacyStatic,
        name: "original_function".into(),
        ty: MirType::Dynamic,
    });
    let defaults = globals.alloc(GlobalDecl {
        storage: ryupy_mir::GlobalStorage::LegacyStatic,
        name: "original_defaults".into(),
        ty: MirType::Dynamic,
    });
    let signature = globals.alloc(GlobalDecl {
        storage: ryupy_mir::GlobalStorage::LegacyStatic,
        name: "original_signature".into(),
        ty: MirType::Dynamic,
    });
    let identity = globals.alloc(GlobalDecl {
        storage: ryupy_mir::GlobalStorage::LegacyStatic,
        name: "private_identity_anchor".into(),
        ty: MirType::Dynamic,
    });
    let mut functions = Arena::default();
    let init = functions.alloc(empty_function("module_entry"));
    let mut target = empty_function("leaf");
    target.function_global = Some(public);
    target.defaults_global = Some(defaults);
    target.signature_global = Some(signature);
    target.specialization_identity_global = Some(identity);
    let target = functions.alloc(target);
    let body = &mut functions[init].body;
    let created = body.locals.alloc(LocalDecl {
        ty: MirType::Dynamic,
        name: None,
        is_cell: false,
    });
    body.blocks[body.entry].stmts = vec![
        Statement::Assign {
            dest: created,
            value: Rvalue::MakeFunction {
                instance_context: None,
                func: target,
                env: vec![],
                defaults: vec![],
            },
        },
        Statement::SetGlobal {
            dest: public,
            value: Operand::Local(created),
        },
    ];
    let mut modules = Arena::default();
    let entry = modules.alloc(ModuleDef {
        instance_capabilities: Default::default(),
        execution: ryupy_mir::ModuleExecutionKind::LegacySingleton,
        environment_reason: "existing-contract".into(),
        location: Default::default(),
        file_backed_by_global: false,
        is_package: false,
        protected_exports: vec![],
        name: "__main__".into(),
        package: String::new(),
        file: "identity.py".into(),
        init,
        exports: vec![],
    });
    let program = LinkedProgram {
        distribution_metadata: None,
        trace_product: None,
        functions,
        modules,
        classes: Arena::default(),
        globals,
        entry,
    };
    let bytes = crate::emit_assembly(
        &program,
        &crate::EmitOptions {
            assembly_name: "function_identity".into(),
            entry_source_path: None,
        },
    )
    .unwrap();
    let res = Resolution::parse(&bytes, ReadOptions::default()).unwrap();
    let generated = res
        .type_definitions
        .iter()
        .find(|ty| ty.name == "Module")
        .unwrap();
    let anchor = generated
        .fields
        .iter()
        .find(|field| field.name == "private_identity_anchor")
        .unwrap();
    assert!(matches!(
        anchor.accessibility,
        MemberAccessibility::Access(Accessibility::Private)
    ));
    let initialization = &generated
        .methods
        .iter()
        .find(|method| method.name == "__init_functions")
        .unwrap()
        .body
        .as_ref()
        .unwrap()
        .instructions;
    assert!(
        initialization.iter().all(|ins| {
            !matches!(ins, Instruction::NewObject(_)) || !ins.show(&res).contains("PyFunction.")
        }),
        "签名初始化不能提前构造Python函数身份"
    );
    let body = &generated
        .methods
        .iter()
        .find(|method| method.name == "module_entry")
        .unwrap()
        .body
        .as_ref()
        .unwrap()
        .instructions;
    let constructor = body
        .iter()
        .position(|ins| {
            matches!(ins, Instruction::NewObject(_)) && ins.show(&res).contains("PyFunction.")
        })
        .unwrap();
    assert!(matches!(body[constructor + 1], Instruction::Duplicate));
    assert!(matches!(
        body[constructor + 2],
        Instruction::StoreStaticField { .. }
    ));
    assert!(
        body[constructor + 2]
            .show(&res)
            .contains("private_identity_anchor")
    );
    let publication = body
        .iter()
        .position(|ins| {
            matches!(ins, Instruction::StoreStaticField { .. })
                && ins.show(&res).contains("original_function")
        })
        .unwrap();
    assert!(publication > constructor + 2);
    assert_eq!(
        body.iter()
            .filter(|ins| matches!(ins, Instruction::NewObject(_))
                && ins.show(&res).contains("PyFunction."))
            .count(),
        1,
        "锚点必须共享原对象，不能另建一个可调用对象"
    );
    // 所有方法中只有MakeFunction机械写这个私有槽，模块属性入口没有暴露它。
    let anchor_writes = generated
        .methods
        .iter()
        .filter_map(|method| method.body.as_ref())
        .flat_map(|body| &body.instructions)
        .filter(|ins| {
            matches!(ins, Instruction::StoreStaticField { .. })
                && ins.show(&res).contains("private_identity_anchor")
        })
        .count();
    assert_eq!(anchor_writes, 1);
}
