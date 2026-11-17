//! 薄函数实际PE结构门；检查静态委托、真实体调用、env/x换序与新启动入口。
use crate::exact_i64_tests::{function, local, program, single_block};
use dotnetdll::prelude::*;
use ryupy_mir::*;

#[test]
fn stock_thin_pe_has_static_typed_adapter_and_cold_factory() {
    let mut program = program();
    let mut locals = Default::default();
    let x = local(&mut locals, MirType::Dynamic);
    let lease = local(&mut locals, MirType::Dynamic);
    let loaded = local(&mut locals, MirType::Dynamic);
    let mut core = function(
        "thin_real_body",
        single_block(
            locals,
            vec![Statement::Assign {
                dest: loaded,
                value: Rvalue::Call {
                    callee: Callee::Runtime(RuntimeFn::StockThinLoadGlobal),
                    args: vec![
                        Operand::Local(lease),
                        Operand::Const(ryupy_mir::Constant::Str("abs".into())),
                    ],
                },
            }],
            Some(Operand::Local(loaded)),
        ),
        vec![x, lease],
        MirType::Dynamic,
    );
    // 扩围后的局部别名和身份分支直接落到同一静态body的CFG，adapter不增加业务胶水。
    let alias = local(&mut core.body.locals, MirType::Dynamic);
    let same = local(&mut core.body.locals, MirType::Bool);
    let applied = local(&mut core.body.locals, MirType::Dynamic);
    let sum = local(&mut core.body.locals, MirType::Dynamic);
    let entry = core.body.entry;
    core.body.blocks[entry].stmts.extend([
        Statement::Assign {
            dest: alias,
            value: Rvalue::Use(Operand::Local(x)),
        },
        Statement::Assign {
            dest: same,
            value: Rvalue::Call {
                callee: Callee::Runtime(RuntimeFn::IsSame),
                args: vec![Operand::Local(alias), Operand::Local(loaded)],
            },
        },
        Statement::Assign {
            dest: applied,
            value: Rvalue::DynCallValue {
                callee: Operand::Local(loaded),
                args: vec![Operand::Local(x)],
            },
        },
        Statement::Assign {
            dest: sum,
            value: Rvalue::DynBinary {
                op: DynOp::Add,
                lhs: Operand::Local(applied),
                rhs: Operand::Local(x),
                inplace: false,
            },
        },
    ]);
    let then_ = core.body.blocks.alloc(BasicBlock {
        stmts: vec![],
        terminator: Terminator::Return(Some(Operand::Local(alias))),
    });
    let else_ = core.body.blocks.alloc(BasicBlock {
        stmts: vec![],
        terminator: Terminator::Return(Some(Operand::Local(loaded))),
    });
    core.body.blocks[entry].terminator = Terminator::Branch {
        cond: Operand::Local(same),
        then_,
        else_,
    };
    core.core = FunctionCore::StockThinStatic;
    core.module_state = Some(lease);
    core.module_state_source = Some(InstanceStateSource::StockThinLease);
    core.param_meta = vec![ParamMeta {
        name: "x".into(),
        kind: ParamKind::PositionalOrKeyword,
        has_default: false,
        ty: MirType::Dynamic,
    }];
    core.definition = Some(DefinitionInfo {
        lexical_qualname: "f".into(),
        firstlineno: 7,
        introspection_kind: None,
    });
    let target = program.functions.alloc(core);
    let module = &mut program.modules[program.entry];
    module.execution = ModuleExecutionKind::InstanceGlobals;
    let main = &mut program.functions[module.init];
    let context = local(&mut main.body.locals, MirType::Dynamic);
    let result = local(&mut main.body.locals, MirType::Dynamic);
    main.params = vec![context];
    main.module_state = Some(context);
    main.module_state_source = Some(InstanceStateSource::ModuleEntryParameter);
    main.body.blocks[main.body.entry].stmts = vec![Statement::Assign {
        dest: result,
        value: Rvalue::MakeStockThinFunction {
            func: target,
            module_context: Operand::Local(context),
        },
    }];
    program.external_provider = Some("python/provider.json".into());
    let bytes = crate::emit_assembly(
        &program,
        &crate::EmitOptions {
            assembly_name: "ThinStatic".into(),
            entry_source_path: None,
        },
    )
    .unwrap();
    let res = Resolution::parse(&bytes, ReadOptions::default()).unwrap();
    let methods: Vec<_> = res
        .type_definitions
        .iter()
        .flat_map(|t| &t.methods)
        .collect();
    let adapter = methods
        .iter()
        .find(|m| m.name.starts_with("__stock_thin_adapter_"))
        .unwrap();
    assert!(!adapter.signature.instance);
    assert_eq!(adapter.signature.parameters.len(), 2);
    assert!(
        adapter.signature.parameters[0]
            .show(&res)
            .contains("IPyStockThinEnvironment")
    );
    let ins = &adapter.body.as_ref().unwrap().instructions;
    assert!(matches!(
        ins.as_slice(),
        [
            Instruction::LoadArgument(1),
            Instruction::LoadArgument(0),
            Instruction::Call { .. },
            Instruction::Return
        ]
    ));
    assert!(ins[2].show(&res).contains("thin_real_body"));
    let core = methods.iter().find(|m| m.name == "thin_real_body").unwrap();
    assert_eq!(core.signature.parameters.len(), 2);
    assert!(core.body.as_ref().unwrap().instructions.iter().any(|i| {
        let text = i.show(&res);
        text.contains("PyStockThinFunctions") && text.contains("LoadGlobal")
    }));
    let core_text = core
        .body
        .as_ref()
        .unwrap()
        .instructions
        .iter()
        .map(|i| i.show(&res))
        .collect::<Vec<_>>()
        .join("\n");
    assert!(
        core_text.contains("PyOps::Is") || core_text.contains("PyOps.Is"),
        "{core_text}"
    );
    for method in ["Binary", "CallValue1"] {
        assert!(
            core_text.contains(&format!("PyPic::{method}"))
                || core_text.contains(&format!("PyPic.{method}")),
            "{core_text}"
        );
    }
    assert!(!core_text.contains("DynamicMethod") && !core_text.contains("Reflection.Emit"));
    assert_eq!(
        core.body
            .as_ref()
            .unwrap()
            .instructions
            .iter()
            .filter(|i| matches!(i, Instruction::Return))
            .count(),
        2
    );
    assert!(
        core.body
            .as_ref()
            .unwrap()
            .instructions
            .iter()
            .any(|i| matches!(i, Instruction::BranchTruthy(_)))
    );
    let main = methods.iter().find(|m| m.name == "__main__").unwrap();
    let ins = &main.body.as_ref().unwrap().instructions;
    assert!(ins.windows(3).any(|w| matches!(
        w,
        [
            Instruction::LoadNull,
            Instruction::LoadMethodPointer(_),
            Instruction::NewObject(_)
        ]
    ) && w[1].show(&res).contains("__stock_thin_adapter_")
        && w[2].show(&res).contains("PyStockThinBody")));
    let shown = methods
        .iter()
        .flat_map(|m| m.body.iter())
        .flat_map(|b| &b.instructions)
        .map(|i| i.show(&res))
        .collect::<Vec<_>>()
        .join("\n");
    assert!(shown.contains("CreateStockThinFunction"));
    assert!(shown.contains("StartStockThin"));
    assert!(shown.contains("FinishStockThin"));
    let wrapper = methods.iter().find(|m| m.name == "Main").unwrap();
    let wrapper = wrapper.body.as_ref().unwrap();
    for name in ["StartStockThin", "FinishStockThin"] {
        assert_eq!(
            wrapper
                .instructions
                .iter()
                .filter(|i| bootstrap_call(i, &res, name))
                .count(),
            1
        );
    }
    // 初始化仍走原Main；仅__run_main内部实际执行改由Host包住PyModules.RunMain。
    let definition_calls: Vec<_> = wrapper
        .instructions
        .iter()
        .filter_map(|i| match i {
            Instruction::Call {
                param0: MethodSource::User(UserMethod::Definition(target)),
                ..
            } => Some(res[*target].name.as_ref()),
            _ => None,
        })
        .collect();
    assert_eq!(
        definition_calls,
        ["__init_functions", "__init_class_factories", "__run_main"]
    );
    let run = methods.iter().find(|m| m.name == "__run_main").unwrap();
    let run = &run.body.as_ref().unwrap().instructions;
    assert_eq!(run.len(), 2);
    assert!(bootstrap_call(&run[0], &res, "RunStockThinMain"));
    assert!(matches!(run[1], Instruction::Return));
    // 成功、SystemExit、Python异常、CLR异常共享同一专用finish汇合点。
    let finish = wrapper.instructions.len() - 3;
    assert!(matches!(
        wrapper.instructions[finish],
        Instruction::LoadLocal(0)
    ));
    assert!(bootstrap_call(
        &wrapper.instructions[finish + 1],
        &res,
        "FinishStockThin"
    ));
    let leaves: Vec<_> = wrapper
        .instructions
        .iter()
        .filter_map(|i| match i {
            Instruction::Leave(target) => Some(*target),
            _ => None,
        })
        .collect();
    assert_eq!(leaves, vec![finish; 4]);
    assert!(
        !shown.contains("CreateOwnedFunction")
            && !shown.contains("FunctionModule(")
            && !shown.contains("FunctionMetadata(")
    );
}

/// 必须是真实call到精确Bootstrap静态签名，单有MemberRef不能通过此门。
fn bootstrap_call(instruction: &Instruction, res: &Resolution<'_>, name: &str) -> bool {
    let Instruction::Call {
        param0: MethodSource::User(UserMethod::Reference(id)),
        ..
    } = instruction
    else {
        return false;
    };
    let method = &res[*id];
    if method.name != name {
        return false;
    }
    let MethodReferenceParent::Type(parent) = &method.parent else {
        panic!("Bootstrap必须是类型静态方法");
    };
    assert!(parent.show(res).contains("RyuPy.CPython.Bootstrap"));
    assert!(
        !method.signature.instance
            && !method.signature.explicit_this
            && method.signature.varargs.is_none()
    );
    match name {
        "StartStockThin" | "Start" => {
            assert_eq!(method.signature, msig! { static void (string, string[]) })
        }
        "RunStockThinMain" => assert_eq!(method.signature, msig! { static void () }),
        "FinishStockThin" | "Finish" => assert_eq!(method.signature, msig! { static int (int) }),
        _ => panic!("未登记Bootstrap合同"),
    }
    true
}

#[test]
fn ordinary_provider_keeps_original_start_run_and_finish() {
    let mut program = program();
    program.external_provider = Some("python/provider.json".into());
    let bytes = crate::emit_assembly(
        &program,
        &crate::EmitOptions {
            assembly_name: "OrdinaryProvider".into(),
            entry_source_path: None,
        },
    )
    .unwrap();
    let res = Resolution::parse(&bytes, ReadOptions::default()).unwrap();
    let methods: Vec<_> = res
        .type_definitions
        .iter()
        .flat_map(|ty| &ty.methods)
        .collect();
    let wrapper = methods.iter().find(|m| m.name == "Main").unwrap();
    let instructions = &wrapper.body.as_ref().unwrap().instructions;
    for name in ["Start", "Finish"] {
        assert_eq!(
            instructions
                .iter()
                .filter(|i| bootstrap_call(i, &res, name))
                .count(),
            1
        );
    }
    let run = methods.iter().find(|m| m.name == "__run_main").unwrap();
    let instructions = &run.body.as_ref().unwrap().instructions;
    let Instruction::Call {
        param0: MethodSource::User(UserMethod::Reference(id)),
        ..
    } = instructions[0]
    else {
        panic!("默认RunMain必须直接静态call");
    };
    assert_eq!(res[id].name, "RunMain");
    assert!(!res[id].signature.instance);
    assert!(instructions[0].show(&res).contains("PyModules"));
    assert!(res.method_references.iter().all(|m| !matches!(
        m.name.as_ref(),
        "StartStockThin" | "RunStockThinMain" | "FinishStockThin"
    )));
}
