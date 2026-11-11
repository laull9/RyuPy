//! except目标的静态身份证明和有界动态语法准入；探测与导出保护登记分离。
use crate::hir::{Builtin, Name};
use crate::lower::StmtLowerCtx;
use ryupy_base::Ranged;
use ryupy_syntax::ast;

impl StmtLowerCtx<'_> {
    /// 把 `except` 的类型表达式解析成异常类型名列表。
    ///
    /// 接受内建/用户异常类，以及 driver 验证过的稳定原生模块异常导入。
    /// 元组每一项都必须能静态解析；不能丢弃未知项后冒称整条 handler 已支持。
    /// 返回空列表由调用方报诊断，不求值动态类型表达式。
    pub(crate) fn exception_type_names(&self, expr: &ast::Expr, mark_used: bool) -> Vec<String> {
        if let Some((name, path)) = crate::exception_imports::attribute_path(expr) {
            let ident = Name::from(name);
            // local候选失败就拒绝，不能退回同名模块类或builtin。
            if !self.module_scope && self.func_scope.locals.contains_key(&ident) {
                return self
                    .func_scope
                    .exception_imports
                    .get(&ident)
                    .and_then(|binding| {
                        binding.resolve(
                            &path,
                            expr.range().start(),
                            self.direct_stmt_index,
                            mark_used,
                        )
                    })
                    .map(|name| vec![name.to_owned()])
                    .unwrap_or_default();
            }
            let shadowed = !self.module_scope
                && (self.func_scope.locals.contains_key(&ident)
                    || self.func_scope.free.contains_key(&ident)
                    || self.func_scope.nonlocals.contains(&ident));
            if shadowed {
                return Vec::new();
            }
            // 删除使公开身份失效；即使原来是用户异常类或builtin，也须按实际名字匹配。
            if self.global_scope.deleted_names.contains(&ident) {
                return Vec::new();
            }
            if let Some(binding) = self.global_scope.exception_imports.get(&ident) {
                let resolved = if mark_used {
                    binding.resolve(&path, expr.range().start())
                } else {
                    binding.peek(&path, expr.range().start())
                };
                return resolved
                    .map(|name| vec![name.to_owned()])
                    .unwrap_or_default();
            }
        }
        match expr {
            ast::Expr::Name(name) => {
                let ident = Name::from(name.id.as_str());
                match self.global_scope.names.get(&ident) {
                    // 用户类：必须位于 BaseException 家族，否则 `except X` 在 CPython
                    // 里是 TypeError，这里作为暂不支持的能力明确拒绝。
                    Some(crate::scope::Resolution::Class(cls_id)) => {
                        let cls = &self.classes[*cls_id];
                        if cls.is_exception {
                            vec![cls.name.as_str().to_owned()]
                        } else {
                            Vec::new()
                        }
                    }
                    Some(_) => Vec::new(),
                    None => match crate::scope::resolve_builtin(name.id.as_str()) {
                        Some(Builtin::Exception(type_name)) => {
                            vec![type_name.to_owned()]
                        }
                        _ => Vec::new(),
                    },
                }
            }
            ast::Expr::Tuple(tuple) => {
                let mut out = Vec::new();
                for elt in &tuple.elts {
                    let names = self.exception_type_names(elt, mark_used);
                    if names.is_empty() {
                        return Vec::new();
                    }
                    out.extend(names);
                }
                out
            }
            _ => Vec::new(),
        }
    }
}

/// 动态目标仅接Name、以Name起点的属性链和原形tuple；不提升调用或展开副作用。
pub(crate) fn supported_target(expr: &ast::Expr) -> bool {
    match expr {
        ast::Expr::Name(_) => true,
        ast::Expr::Attribute(attr) => name_attribute_chain(&attr.value),
        ast::Expr::Tuple(tuple) => tuple.elts.iter().all(supported_target),
        _ => false,
    }
}

fn name_attribute_chain(expr: &ast::Expr) -> bool {
    match expr {
        ast::Expr::Name(_) => true,
        ast::Expr::Attribute(attr) => name_attribute_chain(&attr.value),
        _ => false,
    }
}
