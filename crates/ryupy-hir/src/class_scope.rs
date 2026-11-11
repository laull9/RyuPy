//! 类体源序绑定：条件两侧各自推演，合流区分可能与必定已绑定。
use crate::hir::Name;
use rustc_hash::FxHashSet;

#[derive(Clone, Default)]
pub struct ClassScope {
    pub(crate) lexical_class: Option<crate::ClassId>,
    pub(crate) outer_class_cell: Option<crate::ClassId>,
    pub(crate) legacy_class_body: bool,
    pub(crate) unsupported_outer_cell: bool,
    /// 新工厂按整个suite判定局部绑定；None保持旧源序模型。
    pub(crate) lexical_bindings: Option<FxHashSet<Name>>,
    possible: FxHashSet<Name>,
    definite: FxHashSet<Name>,
}
impl ClassScope {
    pub(crate) fn contains(&self, name: &Name) -> bool {
        self.possible.contains(name)
    }
    pub(crate) fn definite(&self, name: &Name) -> bool {
        self.definite.contains(name)
    }
    pub(crate) fn insert(&mut self, name: Name) {
        self.possible.insert(name.clone());
        self.definite.insert(name);
    }
    pub(crate) fn merge(&mut self, left: Self, right: Self) {
        // 条件不创建词法域；合流即使由空scope开始也必须保留外层cell身份。
        self.lexical_class = left.lexical_class;
        self.outer_class_cell = left.outer_class_cell;
        self.legacy_class_body = left.legacy_class_body;
        self.unsupported_outer_cell = left.unsupported_outer_cell;
        self.lexical_bindings = left.lexical_bindings.clone();
        self.possible = left.possible.union(&right.possible).cloned().collect();
        self.definite = left
            .definite
            .intersection(&right.definite)
            .cloned()
            .collect();
    }
}

impl crate::expr::ExprLowerCtx<'_> {
    /// 新namespace的回退分类按完整静态绑定集，不按执行到当前位置的赋值判断。
    pub(crate) fn lower_lexical_name(
        &mut self,
        source: &ryupy_syntax::ast::ExprName,
    ) -> Option<crate::ExprKind> {
        let bindings = self.class_names?.lexical_bindings.as_ref()?;
        let name = Name::from(source.id.as_str());
        let empty = crate::scope::FunctionScope::default();
        let func_scope = if bindings.contains(&name) {
            &empty
        } else {
            self.func_scope
        };
        let mut fallback = crate::expr::ExprLowerCtx {
            class_names: None,
            allow_yield: false,
            file: self.file,
            global_scope: self.global_scope,
            func_scope,
            module_vars: self.module_vars,
            nested_funcs: self.nested_funcs,
            nested_defaults: self.nested_defaults,
            body: self.body,
            diagnostics: self.diagnostics,
        };
        let kind = if name.as_str() == "__class__" && !bindings.contains(&name) {
            let scope = self.class_names.unwrap();
            if let Some(class) = scope.outer_class_cell {
                fallback.nested_defaults.class_cell_reads.insert(class);
                crate::ExprKind::ClassCellRead(class)
            } else if scope.unsupported_outer_cell {
                fallback.diagnostics.push(
                    ryupy_base::Diagnostic::error(
                        ryupy_base::Span::new(self.file, source.range),
                        "默认顶层旧类到嵌套类体的外层__class__来源暂不支持",
                    )
                    .with_code("E0100"),
                );
                crate::ExprKind::Missing
            } else {
                fallback.lower_namespace_fallback(source)
            }
        } else {
            fallback.lower_namespace_fallback(source)
        };
        let kind = match kind {
            crate::ExprKind::Local(local) => crate::ExprKind::ClassFreeLocal(local),
            kind => kind,
        };
        let fallback = self.body.exprs.alloc(crate::Expr {
            kind,
            range: source.range,
        });
        Some(crate::ExprKind::ClassLookup { name, fallback })
    }
    /// 未执行的fallback不提前触发未支持builtin诊断；真实词法来源仍优先。
    fn lower_namespace_fallback(
        &mut self,
        source: &ryupy_syntax::ast::ExprName,
    ) -> crate::ExprKind {
        let name = Name::from(source.id.as_str());
        if crate::unresolved_globals::unsupported_builtin(name.as_str())
            && crate::scope::resolve_builtin(name.as_str()).is_none()
            && !self.func_scope.locals.contains_key(&name)
            && !self.func_scope.free.contains_key(&name)
            && !self.module_vars.contains_key(&name)
            && !self.global_scope.names.contains_key(&name)
        {
            crate::ExprKind::UnresolvedGlobal(name)
        } else {
            self.lower_name(source)
        }
    }
}
