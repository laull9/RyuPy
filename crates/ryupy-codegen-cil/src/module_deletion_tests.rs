//! 模块删除的真实PE契约：四个精确ABI、void求值栈平衡和未选程序零新增元数据。
use crate::exact_i64_tests::{function, local, single_block};
use dotnetdll::prelude::*;
use la_arena::Arena;
use ryupy_mir::{
    Callee, Constant, InstanceStateSource, LinkedProgram, MirType, ModuleDef, ModuleExecutionKind,
    Operand, RuntimeFn, Rvalue, Statement,
};

const ENTRY_NAMES: [&str; 4] = [
    "CheckModuleDelete",
    "CheckModuleDeleteValue",
    "LoadDeletedModuleName",
    "DeleteName",
];

fn plain_program() -> LinkedProgram {
    let mut functions = Arena::default();
    let init = functions.alloc(function(
        "__main__",
        single_block(Arena::default(), vec![], None),
        vec![],
        MirType::Unit,
    ));
    let mut modules = Arena::default();
    let entry = modules.alloc(ModuleDef {
        instance_capabilities: Default::default(),
        execution: ModuleExecutionKind::LegacySingleton,
        environment_reason: "existing-contract".into(),
        location: Default::default(),
        file_backed_by_global: false,
        is_package: false,
        protected_exports: vec![],
        name: "__main__".into(),
        package: String::new(),
        file: "module_deletion.py".into(),
        init,
        exports: vec![],
    });
    LinkedProgram {
        trace_product: None,
        functions,
        modules,
        classes: Arena::default(),
        globals: Arena::default(),
        entry,
    }
}

fn options() -> crate::EmitOptions {
    crate::EmitOptions {
        assembly_name: "module_deletion_abi".into(),
        entry_source_path: None,
    }
}

/// 每次只选一个入口，确保惰性扫描覆盖Assign和Eval且不额外建立其它三个ABI。
fn with_call(callee: RuntimeFn) -> LinkedProgram {
    let mut program = plain_program();
    let mut locals = Arena::default();
    let input = local(
        &mut locals,
        if callee == RuntimeFn::CheckModuleDelete {
            MirType::Bool
        } else {
            MirType::Dynamic
        },
    );
    let call = Rvalue::Call {
        callee: Callee::Runtime(callee),
        args: vec![
            Operand::Local(input),
            Operand::Const(Constant::Str("ordinary_name".into())),
        ],
    };
    let (stmts, ret, ty) = if callee == RuntimeFn::LoadDeletedModuleName {
        let result = local(&mut locals, MirType::Dynamic);
        (
            vec![Statement::Assign {
                dest: result,
                value: call,
            }],
            Some(Operand::Local(result)),
            MirType::Dynamic,
        )
    } else {
        (vec![Statement::Eval(call)], None, MirType::Unit)
    };
    let mut probe = function("probe", single_block(locals, stmts, ret), vec![input], ty);
    if callee == RuntimeFn::InstanceDeleteName {
        // 实例上下文来自真实模块入口参数，不依赖静态字段或ambient上下文。
        probe.module_state = Some(input);
        probe.module_state_source = Some(InstanceStateSource::ModuleEntryParameter);
        let module = &mut program.modules[program.entry];
        module.execution = ModuleExecutionKind::InstanceGlobals;
        module.init = program.functions.alloc(probe);
    } else {
        program.functions.alloc(probe);
    }
    program
}

#[test]
fn module_deletion_emits_only_selected_exact_abi_and_balanced_call_body() {
    for (callee, owner, name, signature, returns_value) in [
        (
            RuntimeFn::CheckModuleDelete,
            "PyOps",
            ENTRY_NAMES[0],
            msig! { static void(bool, string) },
            false,
        ),
        (
            RuntimeFn::CheckModuleDeleteValue,
            "PyOps",
            ENTRY_NAMES[1],
            msig! { static void(object, string) },
            false,
        ),
        (
            RuntimeFn::LoadDeletedModuleName,
            "PyOps",
            ENTRY_NAMES[2],
            msig! { static object(object, string) },
            true,
        ),
        (
            RuntimeFn::InstanceDeleteName,
            "PyModuleInstances",
            ENTRY_NAMES[3],
            msig! { static void(object, string) },
            false,
        ),
    ] {
        let bytes = crate::emit_assembly(&with_call(callee), &options()).unwrap();
        let res = Resolution::parse(&bytes, ReadOptions::default()).unwrap();
        let entries: Vec<_> = res
            .method_references
            .iter()
            .filter(|m| ENTRY_NAMES.contains(&m.name.as_ref()))
            .collect();
        assert_eq!(entries.len(), 1, "仅应建立实际调用的ABI {name}");
        assert_eq!(entries[0].name, name);
        assert_eq!(entries[0].signature, signature);
        let MethodReferenceParent::Type(parent) = &entries[0].parent else {
            panic!("删除ABI必须属于实际运行时类型");
        };
        assert_eq!(
            parent.show(&res),
            format!("[RyuPy.Runtime]RyuPy.Runtime.{owner}")
        );
        let probe = res
            .type_definitions
            .iter()
            .flat_map(|t| &t.methods)
            .find(|m| m.name == "probe")
            .unwrap();
        let ins = &probe.body.as_ref().unwrap().instructions;
        assert!(matches!(ins[0], Instruction::LoadArgument(0)));
        assert!(matches!(&ins[1], Instruction::LoadString(_)));
        assert!(ins[1].show(&res).contains("ordinary_name"));
        assert!(matches!(&ins[2], Instruction::Call { .. }));
        assert!(ins[2].show(&res).contains(&format!("{owner}.{name}(")));
        // void入口恰好消耗两个实参后ret；不能补pop，也不能分配object[]或插装箱。
        if returns_value {
            assert_eq!(ins.len(), 6);
            assert!(matches!(ins[3], Instruction::StoreLocal(0)));
            assert!(matches!(ins[4], Instruction::LoadLocal(0)));
        } else {
            assert_eq!(ins.len(), 4);
        }
        assert!(matches!(ins.last(), Some(Instruction::Return)));
        assert!(res.type_definitions.iter().all(|t| t.fields.is_empty()));
        if owner == "PyOps" {
            assert!(
                !res.type_references
                    .iter()
                    .any(|t| t.name == "PyModuleInstances")
            );
        }
    }
}

#[test]
fn unused_deletion_refs_preserve_real_program_metadata_and_initializers() {
    let program = plain_program();
    let (mut res, _) = crate::assembly::build_resolution(&program, &options());
    assert!(
        !res.type_references
            .iter()
            .any(|t| t.name == "PyModuleInstances")
    );
    assert!(
        !res.method_references
            .iter()
            .any(|m| ENTRY_NAMES.contains(&m.name.as_ref()))
    );
    assert!(res.type_definitions.iter().all(|t| t.fields.is_empty()));
    let methods: Vec<_> = res
        .type_definitions
        .iter()
        .flat_map(|t| &t.methods)
        .map(|m| m.name.as_ref())
        .collect();
    assert_eq!(
        methods,
        [
            "__init_literals",
            "__init_class_factories",
            "__main__",
            "Main",
            "__init_functions",
            "__mod_get_0",
            "__mod_set_0",
            "__run_main",
        ]
    );
    let runtime = res
        .enumerate_assembly_references()
        .find(|(_, assembly)| assembly.name == "RyuPy.Runtime")
        .unwrap()
        .0;
    // 在完整真实程序集上核对全结构，涵盖引用顺序、字段和所有初始化方法的指令。
    let before = format!("{res:?}");
    assert!(
        crate::module_deletion_refs::DeletionRefs::for_program(&mut res, &program, runtime,)
            .is_none()
    );
    assert_eq!(format!("{res:?}"), before);
    let bytes = res
        .write(WriteOptions {
            is_32_bit: true,
            is_executable: false,
        })
        .unwrap();
    Resolution::parse(&bytes, ReadOptions::default()).unwrap();
}
