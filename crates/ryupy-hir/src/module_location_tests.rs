//! 模块冷位置初始化必须早于用户代码；诊断名和旧dump表示保持独立。
use crate::*;

fn lower(enabled: bool) -> Module {
    let source = "import sibling\n__file__ = None\n";
    let mut db = ryupy_base::SourceDb::default();
    let file = db.add("unit.py", source);
    let (parsed, errors) = ryupy_syntax::parse(file, source);
    assert!(errors.is_empty());
    let (module, errors) = lower_module(
        &parsed,
        &ModuleInfo {
            resolve_file_at_runtime: enabled,
            name: "pkg.unit".into(),
            package: "pkg".into(),
            file: "unit.py".into(),
        },
        source,
    );
    assert!(errors.is_empty(), "{errors:?}");
    module
}

#[test]
fn runtime_file_is_the_third_initialization_before_import_or_user_write() {
    let module = lower(true);
    let body = &module.functions[module.main].body;
    let StmtKind::Assign {
        target: AssignTarget::ModuleVar(var),
        value,
    } = &body.stmts[body.root[2]].kind
    else {
        panic!("module file initialization must precede user statements");
    };
    assert_eq!(module.vars[*var].name.as_str(), "__file__");
    assert!(
        matches!(&body.exprs[*value].kind, ExprKind::ModuleFile(name) if name.as_str() == "pkg.unit")
    );
    assert_eq!(
        body.exprs
            .iter()
            .filter(|(_, expr)| matches!(expr.kind, ExprKind::ModuleFile(_)))
            .count(),
        1
    );
    assert_eq!(module.info.file, "unit.py");
    assert_eq!(module.source_name, "unit.py");
    // 后续源码仍有自己的写入，初始化读取不能替换所有__file__使用。
    assert!(body.root.len() > 3);
}

#[test]
fn legacy_dump_keeps_literal_without_machine_specific_location() {
    let module = lower(false);
    let body = &module.functions[module.main].body;
    let StmtKind::Assign { value, .. } = &body.stmts[body.root[2]].kind else {
        panic!()
    };
    assert!(
        matches!(&body.exprs[*value].kind, ExprKind::Literal(Literal::Str(value)) if value.as_ref() == "unit.py")
    );
    assert!(
        !body
            .exprs
            .iter()
            .any(|(_, expr)| matches!(expr.kind, ExprKind::ModuleFile(_)))
    );
}
