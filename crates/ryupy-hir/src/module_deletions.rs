//! 删除的词法归属与有限builtin回退：名字可删除不等于值需要全量装箱。
use crate::hir::{ModuleVar, Name};
use rustc_hash::FxHashSet;
use ryupy_base::{Diagnostic, FileId, Span};
use ryupy_syntax::ast;
use ryupy_syntax::ast::visitor::{Visitor, walk_stmt};

/// 只认当前作用域自己的global；类namespace和父函数声明不能污染子函数。
#[derive(Default)]
pub(crate) enum DeleteScope {
    #[default]
    Module,
    Function(Vec<Name>),
    Class,
}

impl DeleteScope {
    pub(crate) fn owns_module_name(&self, name: &str) -> bool {
        match self {
            Self::Module => true,
            Self::Function(globals) => globals.iter().any(|global| global.as_str() == name),
            Self::Class => false,
        }
    }

    pub(crate) fn enter(statement: &ast::Stmt) -> Option<Self> {
        match statement {
            ast::Stmt::FunctionDef(def) => Some(Self::Function(
                crate::scope::collect_global_names(&def.body),
            )),
            ast::Stmt::ClassDef(_) => Some(Self::Class),
            _ => None,
        }
    }
}

pub(crate) fn collect(statements: &[ast::Stmt]) -> FxHashSet<Name> {
    #[derive(Default)]
    struct Scan {
        scope: DeleteScope,
        names: FxHashSet<Name>,
    }
    impl<'a> Visitor<'a> for Scan {
        fn visit_stmt(&mut self, statement: &'a ast::Stmt) {
            let saved = DeleteScope::enter(statement)
                .map(|scope| std::mem::replace(&mut self.scope, scope));
            if let ast::Stmt::Delete(delete) = statement {
                for target in &delete.targets {
                    if let ast::Expr::Name(name) = target
                        && self.scope.owns_module_name(name.id.as_str())
                    {
                        self.names.insert(Name::from(name.id.as_str()));
                    }
                }
            }
            walk_stmt(self, statement);
            if let Some(scope) = saved {
                self.scope = scope;
            }
        }
    }
    let mut scan = Scan::default();
    for statement in statements {
        scan.visit_stmt(statement);
    }
    scan.names
}

/// 选择已有真实builtin对象的冷回退：删除名字，以及可能由兼容分支遮蔽的map。
/// 保留旧函数名以兼容调用方；map不伪造may_delete，类型与MIR共同消费同一选择。
pub fn uses_deleted_builtin_fallback(var: &ModuleVar) -> bool {
    (var.may_delete && supported_builtin(var.name.as_str())) || var.name.as_str() == "map"
}

fn supported_builtin(name: &str) -> bool {
    crate::scope::resolve_builtin(name).is_some()
        || matches!(name, "__name__" | "IOError" | "EnvironmentError")
}

/// 默认静态模块没有真实frame-builtins；环境替换与初始元数据删除仍明确拒绝。
pub(crate) fn validate_default_builtins(
    vars: &la_arena::Arena<ModuleVar>,
    instance_globals: bool,
    file: FileId,
    diagnostics: &mut Vec<Diagnostic>,
) {
    if instance_globals {
        return;
    }
    // 函数定义、类体与模块属性读取尚未共享“缺__name__”协议，不能泄漏PyUnbound；
    // __doc__的首docstring或初始None也尚未发布为默认模块槽，不能误报NameError。
    // __annotations__普通注解初始化与删除后的懒建协议仍未闭合，不能伪造字典。
    // may_delete包含函数global和不可达语句，不能用定义顺序绕过此能力边界。
    for (_, var) in vars.iter() {
        if var.may_delete
            && matches!(
                var.name.as_str(),
                "__name__" | "__doc__" | "__annotations__"
            )
        {
            diagnostics.push(
                Diagnostic::error(
                    Span::new(file, var.range),
                    format!("默认静态模块暂不支持删除{}的初始绑定与元数据协议", var.name),
                )
                .with_code("E0100"),
            );
        }
    }
    let environment = vars
        .iter()
        .find(|(_, var)| var.name.as_str() == "__builtins__");
    if let Some((_, environment)) = environment
        // 用户builtins可以包含任意普通名字，不能仅按编译器已知builtin筛选。
        && vars.iter().any(|(_, var)| var.may_delete || uses_deleted_builtin_fallback(var))
    {
        diagnostics.push(
            Diagnostic::error(
                Span::new(file, environment.range),
                "默认静态模块暂不支持删除或替换__builtins__后进行名称删除或map回退；请使用显式实例能力",
            )
            .with_code("E0100"),
        );
    }
}
