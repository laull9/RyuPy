//! 从实际应用源码收敛Types/default MIR后锁owner；不能用注解或文件名猜分类。
//! 这里只检查结构，不运行CP/SDK；调用者须在有效CP oracle之后才调用。
use ryupy_mir::{Callee, Function, MirType, Operand, RuntimeFn, Rvalue, Statement};
use std::path::Path;
const STATIC_SOURCE: &str =
    include_str!("../../../../tests/fixtures/namedtuple_core/static_boundary.py");

fn rvalues(function: &Function) -> impl Iterator<Item = &Rvalue> {
    function
        .body
        .blocks
        .iter()
        .flat_map(|(_, block)| &block.stmts)
        .filter_map(|statement| match statement {
            Statement::Assign { value, .. } | Statement::Eval(value) => Some(value),
            _ => None,
        })
}

fn traces(function: &Function, expected: RuntimeFn) -> Vec<&[Operand]> {
    rvalues(function)
        .filter_map(|value| match value {
            Rvalue::Call {
                callee: Callee::Runtime(actual),
                args,
            } if *actual == expected => Some(args.as_slice()),
            _ => None,
        })
        .collect()
}

fn verify(
    root: &Path,
    filename: &str,
    module_name: &str,
    source: &str,
    dynamic: &[&str],
    static_names: &[&str],
) {
    let mut db = ryupy_base::SourceDb::default();
    let file = db.add(filename, source);
    let (parsed, diagnostics) = ryupy_syntax::parse(file, source);
    std::fs::write(
        root.join(format!("{filename}.syntax")),
        format!("{diagnostics:?}\n"),
    )
    .unwrap();
    assert!(diagnostics.is_empty(), "{diagnostics:?}");
    let info = ryupy_hir::ModuleInfo {
        instance_globals: false,
        name: module_name.into(),
        file: filename.into(),
        resolve_file_at_runtime: true,
        ..Default::default()
    };
    let (hir, diagnostics) = ryupy_hir::lower_module(&parsed, &info, source);
    std::fs::write(
        root.join(format!("{filename}.hir")),
        format!("{diagnostics:?}\n"),
    )
    .unwrap();
    assert!(diagnostics.is_empty(), "{diagnostics:?}");
    let (types, diagnostics) = ryupy_ty::check_module(&hir);
    std::fs::write(
        root.join(format!("{filename}.types")),
        format!("{diagnostics:?}\n{}", types.dump()),
    )
    .unwrap();
    assert!(diagnostics.is_empty(), "{diagnostics:?}");
    for (names, mode) in [
        (dynamic, ryupy_ty::FuncMode::Dynamic),
        (static_names, ryupy_ty::FuncMode::Static),
    ] {
        for name in names {
            let matches: Vec<_> = hir
                .functions
                .iter()
                .filter(|(_, function)| function.name.as_str() == *name)
                .collect();
            assert_eq!(matches.len(), 1, "源码函数必须唯一: {name}");
            assert_eq!(types.mode(matches[0].0), mode, "真实分类不同: {name}");
        }
    }
    let (mut program, _, diagnostics) = ryupy_mir::build_program(&hir, &types);
    std::fs::write(
        root.join(format!("{filename}.mir-diagnostics")),
        format!("{diagnostics:?}\n"),
    )
    .unwrap();
    assert!(diagnostics.is_empty(), "{diagnostics:?}");
    let result =
        ryupy_mir::passes::run_pipeline(&mut program, &ryupy_mir::passes::default_pipeline());
    std::fs::write(
        root.join(format!("{filename}.pipeline")),
        format!("{result:?}\n"),
    )
    .unwrap();
    std::fs::write(
        root.join(format!("{filename}.mir")),
        ryupy_mir::pretty::dump(&program),
    )
    .unwrap();
    result.unwrap();
    for name in dynamic.iter().chain(static_names.iter()) {
        let matches: Vec<_> = program
            .functions
            .iter()
            .filter(|(_, function)| function.name == *name && function.signature_global.is_some())
            .collect();
        assert_eq!(matches.len(), 1, "MIR真实函数必须唯一: {name}");
        let function = matches[0].1;
        if static_names.contains(name) {
            assert!(function.dynamic_origin.is_none());
            assert_eq!(function.params.len(), 1);
            assert_eq!(function.param_meta.len(), 1);
            assert_eq!(function.body.locals[function.params[0]].ty, MirType::I64);
            assert_eq!(function.ret, MirType::Unit);
            assert!(traces(function, RuntimeFn::TracePushWithOrigin).is_empty());
            let push = traces(function, RuntimeFn::TracePush);
            assert_eq!(push.len(), 1);
            assert_eq!(push[0].len(), 2);
        } else {
            let owner = function.dynamic_origin.expect("Dynamic来源丢失").owner;
            assert_eq!(function.params.last(), Some(&owner));
            assert_eq!(function.body.locals[owner].ty, MirType::Dynamic);
            assert!(traces(function, RuntimeFn::TracePush).is_empty());
            let generators: Vec<_> = rvalues(function)
                .filter_map(|value| match value {
                    Rvalue::MakeGenerator { origin, .. } => Some(origin),
                    _ => None,
                })
                .collect();
            if generators.is_empty() {
                let push = traces(function, RuntimeFn::TracePushWithOrigin);
                assert_eq!(push.len(), 1);
                assert!(matches!(push[0].get(2), Some(Operand::Local(id)) if *id == owner));
            } else {
                assert_eq!(generators.len(), 1);
                assert!(matches!(generators[0], Some(Operand::Local(id)) if *id == owner));
                assert!(traces(function, RuntimeFn::TracePushWithOrigin).is_empty());
            }
        }
    }
}

pub(super) fn origins(root: &Path, filename: &str, source: &str, helpers: &[(&str, &str)]) {
    verify(
        root,
        filename,
        "__main__",
        source,
        &[
            "direct",
            "direct_none",
            "late",
            "selected",
            "replacement",
            "ordered",
        ],
        &[],
    );
    let provider: Vec<_> = helpers
        .iter()
        .filter(|(name, _)| *name == "origin_provider.py")
        .collect();
    assert_eq!(provider.len(), 1);
    verify(
        root,
        "origin_provider.py",
        "origin_provider",
        provider[0].1,
        &[
            "make_record",
            "make_none",
            "make_closure",
            "closure",
            "make_stream",
            "stream",
            "invoke_callback",
        ],
        &[],
    );
    // 仅源码结构准入，不启动第六个CP/JIT应用，也不把这一断言叫运行时边界通过。
    std::fs::write(root.join("static_boundary.py"), STATIC_SOURCE).unwrap();
    static_boundary(root, "static_boundary.py", STATIC_SOURCE);
}

fn static_boundary(root: &Path, filename: &str, source: &str) {
    verify(
        root,
        filename,
        "__main__",
        source,
        &["dynamic_write"],
        &["static_output"],
    );
}
