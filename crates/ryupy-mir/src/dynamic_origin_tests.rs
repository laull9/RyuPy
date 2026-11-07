//! 小来源方案只验证隐藏ABI和普通定义的私有发布，不把未知来源当作静态纯度证明。
use crate::mir::*;
use la_arena::Arena;

fn empty(name: &str) -> Function {
    let mut blocks = Arena::default();
    let entry = blocks.alloc(BasicBlock {
        stmts: vec![],
        terminator: Terminator::Return(None),
    });
    Function {
        module_state: None,
        instance_capabilities: Default::default(),
        module_state_source: None,
        name: name.into(),
        codegen_name: name.into(),
        source_file: "origin.py".into(),
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
    }
}

fn sample() -> (Program, FnId, GlobalId) {
    let mut globals = Arena::default();
    let mut slot = || {
        globals.alloc(GlobalDecl {
            storage: crate::GlobalStorage::LegacyStatic,
            name: "private".into(),
            ty: MirType::Dynamic,
        })
    };
    let public = slot();
    let signature = slot();
    let source = slot();
    let mut target = empty("target");
    let owner = target.body.locals.alloc(LocalDecl {
        ty: MirType::Dynamic,
        name: None,
        is_cell: false,
    });
    target.params.push(owner);
    target.function_global = Some(public);
    target.signature_global = Some(signature);
    target.dynamic_origin = Some(DynamicFunctionOrigin {
        owner,
        source_global: Some(source),
    });
    let mut functions = Arena::default();
    let target = functions.alloc(target);
    let mut main = empty("main");
    let fresh = main.body.locals.alloc(LocalDecl {
        ty: MirType::Dynamic,
        name: None,
        is_cell: false,
    });
    main.body.blocks[main.body.entry].stmts = vec![
        Statement::Assign {
            dest: fresh,
            value: Rvalue::MakeFunction {
                instance_context: None,
                func: target,
                env: vec![],
                defaults: vec![],
            },
        },
        Statement::Eval(Rvalue::Call {
            callee: Callee::Runtime(RuntimeFn::FunctionModule),
            args: vec![Operand::Local(fresh), Operand::Const(Constant::None)],
        }),
        Statement::SetGlobal {
            dest: source,
            value: Operand::Local(fresh),
        },
        Statement::SetGlobal {
            dest: public,
            value: Operand::Local(fresh),
        },
    ];
    let entry = functions.alloc(main);
    (
        Program {
            functions,
            globals,
            classes: Arena::default(),
            entry,
            source_name: "origin.py".into(),
        },
        target,
        source,
    )
}

#[test]
fn successful_definition_has_private_corresponding_instance() {
    let (p, _, _) = sample();
    crate::validate::validate(&p).unwrap();
}

#[test]
fn private_origin_rejects_arbitrary_write_or_wrong_publication() {
    for mutate in 0..3 {
        let (mut p, _, source) = sample();
        let f = &mut p.functions[p.entry];
        let block = &mut f.body.blocks[f.body.entry];
        match mutate {
            0 => {
                block.stmts[2] = Statement::SetGlobal {
                    dest: source,
                    value: Operand::Const(Constant::Null),
                }
            }
            1 => {
                block.stmts.remove(3);
            }
            _ => block.stmts.insert(
                2,
                Statement::Assign {
                    dest: LocalId::from_raw(0.into()),
                    value: Rvalue::Use(Operand::Const(Constant::None)),
                },
            ),
        }
        assert!(
            crate::validate::validate(&p)
                .unwrap_err()
                .iter()
                .any(|e| e.contains("私有来源"))
        );
    }
}

#[test]
fn owner_requires_unchanged_dynamic_tail_parameter() {
    for mutate in 0..3 {
        let (mut p, target, _) = sample();
        let f = &mut p.functions[target];
        let owner = f.dynamic_origin.unwrap().owner;
        match mutate {
            0 => f.body.locals[owner].ty = MirType::I64,
            1 => f.params.clear(),
            _ => f.body.blocks[f.body.entry].stmts.push(Statement::Assign {
                dest: owner,
                value: Rvalue::Use(Operand::Const(Constant::None)),
            }),
        }
        assert!(
            crate::validate::validate(&p)
                .unwrap_err()
                .iter()
                .any(|e| e.contains("owner"))
        );
    }
}
