//! class-cell冷守卫的精确CIL ABI与惰性引用；后端只消费MIR，不重新分析词法作用域。
use crate::exact_i64_tests::{function, local, single_block};
use dotnetdll::prelude::*;
use la_arena::Arena;
use ryupy_mir::{
    Callee, LinkedProgram, MirType, ModuleDef, ModuleExecutionKind, Operand, RuntimeFn, Rvalue,
    Statement,
};

fn program(guard: bool) -> LinkedProgram {
    let mut functions = Arena::default();
    let mut locals = Arena::default();
    let input = local(&mut locals, MirType::Dynamic);
    let stmts = if guard {
        vec![Statement::Eval(Rvalue::Call {
            callee: Callee::Runtime(RuntimeFn::InstanceClassCellGuard),
            args: vec![Operand::Local(input)],
        })]
    } else {
        vec![]
    };
    functions.alloc(function(
        "probe",
        single_block(locals, stmts, None),
        vec![input],
        MirType::Unit,
    ));
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
        file: "class_cell.py".into(),
        init,
        exports: vec![],
    });
    LinkedProgram {
        external_provider: None,
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
    }
}
#[test]
fn class_cell_guard_emits_exact_abi_without_default_metadata_cost() {
    let options = crate::EmitOptions {
        assembly_name: "class_cell_abi".into(),
        entry_source_path: None,
    };
    for guard in [false, true] {
        let bytes = crate::emit_assembly(&program(guard), &options).unwrap();
        let res = Resolution::parse(&bytes, ReadOptions::default()).unwrap();
        let entries: Vec<_> = res
            .method_references
            .iter()
            .filter(|method| method.name == "ClassCellGuard")
            .collect();
        assert_eq!(entries.len(), usize::from(guard));
        if guard {
            assert_eq!(entries[0].signature, msig! { static void(object) });
            let MethodReferenceParent::Type(parent) = &entries[0].parent else {
                panic!("必须属于实际运行时类型")
            };
            assert_eq!(
                parent.show(&res),
                "[RyuPy.Runtime]RyuPy.Runtime.PyModuleInstances"
            );
            let probe = res
                .type_definitions
                .iter()
                .flat_map(|t| &t.methods)
                .find(|method| method.name == "probe")
                .unwrap();
            let instructions = &probe.body.as_ref().unwrap().instructions;
            assert_eq!(instructions.len(), 3);
            assert!(matches!(instructions[0], Instruction::LoadArgument(0)));
            assert!(matches!(instructions[1], Instruction::Call { .. }));
            assert!(
                instructions[1]
                    .show(&res)
                    .contains("PyModuleInstances.ClassCellGuard(")
            );
            assert!(matches!(instructions[2], Instruction::Return));
        } else {
            assert!(
                !res.type_references
                    .iter()
                    .any(|ty| ty.name == "PyModuleInstances")
            );
        }
    }
}
