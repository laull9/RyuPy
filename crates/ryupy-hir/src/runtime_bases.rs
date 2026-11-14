//! 普通运行时基类的语法入口：不传播跨模块ClassId，保留实际绑定的读取。
//! 原生特殊基类仍走已验证入口；这里开放模块槽/属性与真实内建str/list/dict/slice入口，布局由运行时验证。
use crate::hir::{Builtin, ModuleVarId, Name};
use crate::scope::{GlobalScope, Resolution};
use rustc_hash::{FxHashMap, FxHashSet};
use ryupy_syntax::ast;

pub(crate) fn candidate<'a>(
    class: &'a ast::StmtClassDef,
    globals: &GlobalScope,
    module_vars: &FxHashMap<Name, ModuleVarId>,
) -> Option<&'a ast::Expr> {
    candidate_inner(class, globals, module_vars, true)
}

/// 定义名尚未完全解析时，只登记公开槽需求，不推断实际类布局。
/// 有限单模块固定点使后代/同名重定义不依赖扫描顺序；不加载其它模块。
pub(crate) fn register(
    statements: &[ast::Stmt],
    globals: &mut GlobalScope,
    module_vars: &FxHashMap<Name, ModuleVarId>,
) {
    let definitions = crate::module_definitions::definitions(statements);
    let declared: FxHashSet<Name> = definitions
        .iter()
        .filter_map(|stmt| match stmt {
            ast::Stmt::ClassDef(class) => Some(Name::from(class.name.as_str())),
            ast::Stmt::FunctionDef(function) => Some(Name::from(function.name.as_str())),
            _ => None,
        })
        .collect();
    // 每次成功迭代至少增加一个定义名；上界只是当前模块定义数，不是全程序分析。
    for _ in 0..=definitions.len() {
        let mut changed = false;
        for statement in &definitions {
            let ast::Stmt::ClassDef(class) = statement else {
                continue;
            };
            let builtin_visible = class
                .arguments
                .as_ref()
                .and_then(|args| args.args.first())
                .is_some_and(|base| match base {
                    ast::Expr::Name(name) => !declared.contains(&Name::from(name.id.as_str())),
                    _ => false,
                });
            if candidate_inner(class, globals, module_vars, builtin_visible).is_some() {
                changed |= globals
                    .dynamic_names
                    .insert(Name::from(class.name.as_str()));
            }
        }
        if !changed {
            break;
        }
    }
}

fn candidate_inner<'a>(
    class: &'a ast::StmtClassDef,
    globals: &GlobalScope,
    module_vars: &FxHashMap<Name, ModuleVarId>,
    allow_builtin: bool,
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
        // 最外层下标按普通表达式求值；注册与正式lower共用候选，不识别Generic文本。
        ast::Expr::Attribute(_) | ast::Expr::Subscript(_) => Some(base),
        ast::Expr::Name(name) => {
            let name = Name::from(name.id.as_str());
            // 已知本地类继续原静态/装饰后路径；导入与普通赋值槽按运行时值处理。
            let runtime_name = (module_vars.contains_key(&name)
                || globals.dynamic_names.contains(&name))
                && !matches!(globals.names.get(&name), Some(Resolution::Class(_)));
            // str/list/dict可派生而slice在运行时按真实类型拒绝；都不写进异常专用ClassBase::Builtin，
            // 也不绕过源码同名定义/赋值或先期定义副作用。
            let builtin_runtime = allow_builtin
                && !globals.names.contains_key(&name)
                && !module_vars.contains_key(&name)
                && matches!(
                    crate::scope::resolve_builtin(name.as_str()),
                    Some(Builtin::Str | Builtin::Dynamic("list" | "dict" | "slice"))
                );
            (runtime_name || builtin_runtime).then_some(base)
        }
        _ => None,
    }
}

#[cfg(test)]
#[path = "list_class_publication_tests.rs"]
mod list_class_publication_tests;

#[cfg(test)]
#[path = "dict_class_publication_tests.rs"]
mod dict_class_publication_tests;

#[cfg(test)]
#[path = "runtime_base_publication_tests.rs"]
mod runtime_base_publication_tests;
