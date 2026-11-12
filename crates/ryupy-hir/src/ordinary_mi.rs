//! 普通MI定义的有限同模块发布合同；此表只选择冷入口，不替代真实基值/C3验证。
use crate::hir::Name;
use rustc_hash::FxHashSet;
use ryupy_base::TextRange;
use ryupy_syntax::ast;

pub(crate) fn definitions(body: &[ast::Stmt]) -> FxHashSet<TextRange> {
    let definitions = crate::module_definitions::definitions(body);
    let mut names = FxHashSet::<Name>::default();
    let mut selected = FxHashSet::default();
    for _ in 0..=definitions.len() {
        let mut changed = false;
        for statement in &definitions {
            let ast::Stmt::ClassDef(class) = statement else {
                continue;
            };
            let Some(arguments) = &class.arguments else {
                continue;
            };
            let family = arguments.args.len() > 1 || arguments.args.iter().any(|base| {
                matches!(base, ast::Expr::Name(name) if names.contains(&Name::from(name.id.as_str())))
            });
            if family {
                changed |= selected.insert(class.range);
                names.insert(Name::from(class.name.as_str()));
            }
        }
        if !changed {
            break;
        }
    }
    selected
}

#[cfg(test)]
#[path = "ordinary_mi_tests.rs"]
mod tests;
