//! 旧M3.5拒绝门迁到真实来源+动态目标结构，保原源码、不按snapshot空诊断放行。
use ryupy_hir::{Body, ExprId, ExprKind, Module};
use ryupy_mir::{Callee, RegionKind, RuntimeFn, Rvalue, Statement};
use std::path::Path;

fn expression(module: &Module, body: &Body, id: ExprId) -> String {
    match &body.exprs[id].kind {
        ExprKind::Local(id) => format!("local.{}", body.locals[*id].name),
        ExprKind::ModuleVar(id) => format!("global.{}", module.vars[*id].name),
        ExprKind::Builtin(ryupy_hir::Builtin::Exception(name)) => format!("builtin.{name}"),
        ExprKind::Attribute { base, name } => format!("{}.{name}", expression(module, body, *base)),
        ExprKind::Tuple(values) => format!(
            "({})",
            values
                .iter()
                .map(|id| expression(module, body, *id))
                .collect::<Vec<_>>()
                .join(",")
        ),
        other => panic!("预期真实Name/Attribute/tuple，实际{other:?}"),
    }
}

pub(super) fn check(path: &Path, shadow: Option<(&str, &str)>, expected: &[&str]) {
    let mut session = ryupy_driver::Session::default();
    let (sources, entry) = ryupy_driver::modules::load_program(&mut session, path, &[]).unwrap();
    assert!(!session.diagnostics.has_errors());
    if let Some((name, text)) = shadow {
        let found: Vec<_> = sources
            .iter()
            .filter(|source| source.name == name)
            .collect();
        assert_eq!(found.len(), 1, "必须实际找到本地模块而非native注册表");
        assert_eq!(
            found[0].resolved_source_path,
            path.parent().unwrap().join(format!("{name}.py"))
        );
        assert_eq!(found[0].text, text);
    }
    // 与driver真实模块图规则一致：源码存在的模块不能作为native异常模块。
    let native: Vec<_> = [
        "json",
        "zipfile",
        "optparse",
        "getopt",
        "re",
        "dataclasses",
        "unittest",
        "enum",
        "io",
        "typing",
    ]
    .into_iter()
    .filter(|name| !sources.iter().any(|source| source.name == *name))
    .collect();
    let source = &sources[entry];
    let info = ryupy_hir::ModuleInfo {
        instance_globals: false,
        name: source.name.clone(),
        package: source.package.clone(),
        file: source.file.clone(),
        ..Default::default()
    };
    let (hir, diagnostics) = ryupy_hir::lower_module_with_native_exceptions(
        &source.parsed,
        &info,
        &source.text,
        &native,
    );
    assert!(diagnostics.is_empty(), "{diagnostics:?}");
    let mut targets = Vec::new();
    for (_, function) in hir.functions.iter() {
        for (_, handler) in function.body.handlers.iter() {
            assert!(
                handler.types.is_none(),
                "这些新范围不得偷转静态native typed catch"
            );
            targets.push(expression(
                &hir,
                &function.body,
                handler.type_expr.expect("不得变为bare"),
            ));
        }
    }
    targets.sort();
    let mut wanted: Vec<_> = expected.iter().map(|value| value.to_string()).collect();
    wanted.sort();
    assert_eq!(targets, wanted);
    let (types, diagnostics) = ryupy_ty::check_module(&hir);
    assert!(diagnostics.is_empty(), "{diagnostics:?}");
    let (program, _, diagnostics) = ryupy_mir::build_program(&hir, &types);
    assert!(diagnostics.is_empty(), "{diagnostics:?}");
    let mut matches = 0;
    let mut catches = 0;
    for (_, function) in program.functions.iter() {
        for region in &function.body.regions {
            assert!(
                !matches!(region.kind, RegionKind::Catch { .. }),
                "旧native静态身份不可误用于本地/可变目标"
            );
            if matches!(region.kind, RegionKind::CatchAll) {
                catches += 1;
            }
        }
        for (_, block) in function.body.blocks.iter() {
            for stmt in &block.stmts {
                let value = match stmt {
                    Statement::Assign { value, .. } | Statement::Eval(value) => Some(value),
                    _ => None,
                };
                if matches!(
                    value,
                    Some(Rvalue::Call {
                        callee: Callee::Runtime(RuntimeFn::ExceptionMatch),
                        ..
                    })
                ) {
                    matches += 1;
                }
            }
        }
    }
    assert_eq!(matches, expected.len());
    assert_eq!(catches, expected.len());
    let root = path.parent().unwrap();
    std::fs::write(root.join("dynamic.hir"), ryupy_hir::pretty::dump(&hir)).unwrap();
    std::fs::write(root.join("dynamic.mir"), ryupy_mir::pretty::dump(&program)).unwrap();
}
