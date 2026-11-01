//! 普通运行时基类的语法入口：不传播跨模块ClassId，保留实际绑定的读取。
//! 原生特殊基类仍走已验证入口；这里只开放模块槽名字或属性表达式。
use crate::hir::{ModuleVarId, Name};
use crate::scope::{GlobalScope, Resolution};
use rustc_hash::FxHashMap;
use ryupy_syntax::ast;

pub(crate) fn candidate<'a>(
    class: &'a ast::StmtClassDef,
    globals: &GlobalScope,
    module_vars: &FxHashMap<Name, ModuleVarId>,
) -> Option<&'a ast::Expr> {
    let args = class.arguments.as_ref()?;
    if args.args.len() != 1
        || !args.keywords.is_empty()
        || crate::native_bases::resolve(class, globals).is_some()
        || crate::native_bases::io_exception(class, globals).is_some()
    {
        return None;
    }
    let base = args.args.first()?;
    match base {
        ast::Expr::Attribute(_) => Some(base),
        ast::Expr::Name(name) => {
            let name = Name::from(name.id.as_str());
            // 已知本地类继续原静态/装饰后路径；导入与普通赋值槽按运行时值处理。
            ((module_vars.contains_key(&name) || globals.dynamic_names.contains(&name))
                && !matches!(globals.names.get(&name), Some(Resolution::Class(_))))
            .then_some(base)
        }
        _ => None,
    }
}
