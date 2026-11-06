//! 六真实异常逐项发射/读回PE，typed catch必须属于RyuPy.Runtime的对应CLR类型。
use dotnetdll::prelude::*;
use ryupy_mir::{BasicBlock, Region, RegionKind, Rvalue, Statement, Terminator};
#[test]
fn optparse_six_handlers_reference_actual_runtime_exception_types() {
    for name in [
        "OptParseError",
        "OptionError",
        "OptionConflictError",
        "OptionValueError",
        "BadOptionError",
        "AmbiguousOptionError",
    ] {
        let mut program = crate::exact_i64_tests::program();
        let init = program.modules[program.entry].init;
        let body = &mut program.functions[init].body;
        let caught = crate::exact_i64_tests::local(&mut body.locals, ryupy_mir::MirType::Dynamic);
        let protected = body.entry;
        let handler = body.blocks.alloc(BasicBlock {
            stmts: vec![Statement::Assign {
                dest: caught,
                value: Rvalue::CatchException,
            }],
            terminator: Terminator::Unreachable,
        });
        let after = body.blocks.alloc(BasicBlock {
            stmts: vec![],
            terminator: Terminator::Return(None),
        });
        body.blocks[protected].terminator = Terminator::Goto(after);
        body.blocks[handler].terminator = Terminator::Goto(after);
        body.regions.push(Region {
            kind: RegionKind::Catch {
                type_names: vec![format!("optparse.{name}")],
            },
            try_start: protected,
            try_len: 1,
            handler,
            exc_local: Some(caught),
        });
        let bytes = crate::emit_assembly(
            &program,
            &crate::EmitOptions {
                assembly_name: "zip_exception".into(),
                entry_source_path: None,
            },
        )
        .unwrap();
        let res = Resolution::parse(&bytes, ReadOptions::default()).unwrap();
        let reference = res
            .type_references
            .iter()
            .find(|t| t.name == format!("Py{name}"))
            .unwrap();
        assert_eq!(reference.namespace.as_deref(), Some("RyuPy.Runtime"));
        let ResolutionScope::Assembly(assembly) = reference.scope else {
            panic!("真实runtime程序集类型引用")
        };
        assert_eq!(res[assembly].name, "RyuPy.Runtime");
        let expected = format!("[RyuPy.Runtime]RyuPy.Runtime.Py{name}");
        let init_method = res
            .type_definitions
            .iter()
            .flat_map(|t| &t.methods)
            .find(|m| m.name == "__main__")
            .unwrap();
        let clauses: Vec<_> = init_method
            .body
            .as_ref()
            .unwrap()
            .data_sections
            .iter()
            .filter_map(|s| match s {
                body::DataSection::ExceptionHandlers(clauses) => Some(clauses),
                _ => None,
            })
            .flatten()
            .collect();
        assert!(clauses.iter().any(|clause| match &clause.kind {
            body::ExceptionKind::TypedException(ty) => ty.show(&res) == expected,
            _ => false,
        }));
    }
}
