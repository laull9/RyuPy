//! 特化身份锚点的真实 PE 回归：构造一次函数对象、双存同一引用、先于模块入口初始化。
use dotnetdll::prelude::*;
use la_arena::Arena;
use ryupy_mir::{
    BasicBlock, Body, Function, GlobalDecl, LinkedProgram, MirType, ModuleDef, Terminator,
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
        params: vec![],
        ret: MirType::Unit,
        body: Body {
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
    }
}

#[test]
fn original_function_identity_is_duplicated_before_module_execution() {
    let mut globals = Arena::default();
    let public = globals.alloc(GlobalDecl {
        name: "original_function".into(),
        ty: MirType::Dynamic,
    });
    let defaults = globals.alloc(GlobalDecl {
        name: "original_defaults".into(),
        ty: MirType::Dynamic,
    });
    let signature = globals.alloc(GlobalDecl {
        name: "original_signature".into(),
        ty: MirType::Dynamic,
    });
    let identity = globals.alloc(GlobalDecl {
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
    functions.alloc(target);
    let mut modules = Arena::default();
    let entry = modules.alloc(ModuleDef {
        location: Default::default(),
        file_backed_by_global: false,
        protected_exports: vec![],
        name: "__main__".into(),
        package: String::new(),
        file: "identity.py".into(),
        init,
        exports: vec![],
    });
    let program = LinkedProgram {
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
    let body = &generated
        .methods
        .iter()
        .find(|method| method.name == "__init_functions")
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
    assert!(matches!(
        body[constructor + 3],
        Instruction::StoreStaticField { .. }
    ));
    assert!(
        body[constructor + 3]
            .show(&res)
            .contains("original_function")
    );
    assert_eq!(
        body.iter()
            .filter(|ins| matches!(ins, Instruction::NewObject(_))
                && ins.show(&res).contains("PyFunction."))
            .count(),
        1,
        "锚点必须共享原对象，不能另建一个可调用对象"
    );
    // 所有方法中只有初始化器写这个私有槽，模块属性入口没有暴露它。
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
    let main = &generated
        .methods
        .iter()
        .find(|method| method.name == "Main")
        .unwrap()
        .body
        .as_ref()
        .unwrap()
        .instructions;
    let initialization = main
        .iter()
        .position(|ins| ins.show(&res).contains("__init_functions"))
        .unwrap();
    let run = main
        .iter()
        .position(|ins| ins.show(&res).contains("__run_main"))
        .unwrap();
    assert!(initialization < run, "用户模块入口前必须已固定原函数身份");
}
