//! TypedDict候选语法与能力边界：导入可能条件执行、也可能被改绑，真实身份由运行时验证。
//! 不折叠TYPE_CHECKING，不把TypedDict根factory伪装成普通PyClass。
use crate::hir::Name;
use crate::scope::GlobalScope;
use ryupy_base::{Diagnostic, FileId, Ranged, Span};
use ryupy_syntax::ast;

pub(crate) fn register(
    parsed: &ryupy_syntax::ParsedModule,
    info: &crate::ModuleInfo,
    native: &[&str],
    globals: &mut GlobalScope,
) {
    let stmts = &parsed.module.body;
    let package = &info.package;
    if !native.contains(&"typing") {
        return;
    }
    for import in crate::imports::collect_imports(stmts, package) {
        if import.module != "typing" {
            continue;
        }
        match import.name.as_deref() {
            Some("TypedDict") => {
                globals
                    .typed_dict_roots
                    .insert(Name::from(import.bind.as_str()));
            }
            None => {
                globals
                    .typed_dict_modules
                    .insert(Name::from(import.bind.as_str()));
            }
            _ => {}
        }
    }
    let definitions = crate::module_definitions::definitions(stmts);
    // 只闭合本模块的候选语法，不证明执行/身份；确保后续解析新增的候选也有公开动态槽。
    // 前向读取仍在运行时NameError，不能因预登记提前创建类型。
    loop {
        let mut changed = false;
        for stmt in &definitions {
            if let ast::Stmt::ClassDef(class) = stmt
                && candidate(class, globals).is_some()
            {
                let name = Name::from(class.name.as_str());
                changed |= globals.typed_dict_classes.insert(name.clone());
                globals.dynamic_names.insert(name);
            }
        }
        if !changed {
            break;
        }
    }
}

pub(crate) fn candidate<'a>(
    class: &'a ast::StmtClassDef,
    globals: &GlobalScope,
) -> Option<&'a ast::Expr> {
    let expr = class.arguments.as_ref()?.args.first()?;
    candidate_expr(expr, globals).then_some(expr)
}

/// 只复用现有TypedDict候选语法；实际身份仍由专用运行时验证。
pub(crate) fn candidate_expr(expr: &ast::Expr, globals: &GlobalScope) -> bool {
    match expr {
        ast::Expr::Name(name) => {
            let name = Name::from(name.id.as_str());
            globals.typed_dict_roots.contains(&name) || globals.typed_dict_classes.contains(&name)
        }
        ast::Expr::Attribute(attr) if attr.attr.as_str() == "TypedDict" => {
            matches!(attr.value.as_ref(), ast::Expr::Name(name)
                if globals.typed_dict_modules.contains(&Name::from(name.id.as_str())))
        }
        _ => false,
    }
}

pub(crate) fn validate(
    class: &ast::StmtClassDef,
    dataclass: bool,
    file: FileId,
    diags: &mut Vec<Diagnostic>,
) -> bool {
    let mut total = true;
    let mut valid = !dataclass;
    if let Some(args) = &class.arguments {
        valid &= args.args.len() == 1;
        let mut seen = false;
        for keyword in &args.keywords {
            if keyword
                .arg
                .as_ref()
                .is_some_and(|name| name.as_str() == "total")
                && !seen
                && let ast::Expr::BooleanLiteral(value) = &keyword.value
            {
                total = value.value;
                seen = true;
            } else {
                valid = false;
            }
        }
    }
    for (index, stmt) in class.body.iter().enumerate() {
        valid &= match stmt {
            ast::Stmt::Pass(_) => true,
            ast::Stmt::AnnAssign(a) => {
                a.simple && a.value.is_none() && matches!(a.target.as_ref(), ast::Expr::Name(_))
            }
            ast::Stmt::Expr(e) if index == 0 => {
                matches!(e.value.as_ref(), ast::Expr::StringLiteral(_))
            }
            _ => false,
        };
    }
    if !valid {
        diags.push(
            Diagnostic::error(
                Span::new(file, class.range()),
                "TypedDict目前仅支持单父、字面量布尔total及无默认值字段注解",
            )
            .with_code("E0100")
            .with_primary_label("方法、字段默认值、dataclass与复杂类关键字暂不支持"),
        );
    }
    total
}
