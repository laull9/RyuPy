//! 原生基类身份识别：只接受driver确认、未改写且已经执行的直接导入。
use crate::hir::{Name, NativeBase};
use crate::scope::GlobalScope;
use ryupy_base::Ranged;
use ryupy_syntax::ast;

pub(crate) fn resolve<'a>(
    class: &'a ast::StmtClassDef,
    globals: &GlobalScope,
) -> Option<(NativeBase, &'a ast::Expr)> {
    let args = class.arguments.as_ref()?;
    if args.args.len() != 1 || !args.keywords.is_empty() {
        return None;
    }
    let expr = &args.args[0];
    let (binding, path) = crate::exception_imports::attribute_path(expr)?;
    let kind = globals
        .exception_imports
        .get(&Name::from(binding))?
        .resolve_native_base(&path, expr.range().start())?;
    Some((kind, expr))
}

/// 有限模块异常基类；仍要求driver确认的稳定原生导入，不接受任意属性类。
pub(crate) fn io_exception(
    class: &ast::StmtClassDef,
    globals: &GlobalScope,
) -> Option<&'static str> {
    let args = class.arguments.as_ref()?;
    if args.args.len() != 1 || !args.keywords.is_empty() {
        return None;
    }
    let expr = &args.args[0];
    let (name, path) = crate::exception_imports::attribute_path(expr)?;
    globals
        .exception_imports
        .get(&Name::from(name))?
        .resolve(&path, expr.range().start())
        .filter(|name| matches!(*name, "io.UnsupportedOperation" | "zipfile.BadZipFile"))
}

/// 原生自定义初始化异常尚无用户派生ABI；仅拒已证身份，不按类名猜测。
pub(crate) fn unsupported_custom_exception_base(
    class: &ast::StmtClassDef,
    globals: &GlobalScope,
) -> bool {
    let Some(args) = &class.arguments else {
        return false;
    };
    args.args.iter().any(|expr| {
        let Some((name, path)) = crate::exception_imports::attribute_path(expr) else {
            return false;
        };
        globals
            .exception_imports
            .get(&Name::from(name))
            .and_then(|binding| binding.resolve(&path, expr.range().start()))
            .is_some_and(|name| {
                name.starts_with("optparse.")
                    || name == "getopt.GetoptError"
                    || name == "xml.etree.ElementTree.ParseError"
            })
    })
}
