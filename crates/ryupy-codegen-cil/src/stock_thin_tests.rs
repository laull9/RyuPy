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
    assert!(shown.contains("Finish"));
    assert!(
        !shown.contains("CreateOwnedFunction")
            && !shown.contains("FunctionModule(")
            && !shown.contains("FunctionMetadata(")
    );
}
