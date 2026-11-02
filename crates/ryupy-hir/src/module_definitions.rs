//! 模块定义预登记：控制流不创建作用域，类可出现在模块 try/if/循环中。
//! 函数体与类体不递归；函数内类仍由语句降级明确拒绝。

use ryupy_syntax::ast;
use ryupy_syntax::ast::visitor::{Visitor, walk_stmt};

/// 顶层 def 由静态登记处理，控制流中的 def 仍交给既有闭包发现器。
pub(crate) fn definitions(body: &[ast::Stmt]) -> Vec<&ast::Stmt> {
    struct Classes<'a>(Vec<&'a ast::Stmt>);
    impl<'a> Visitor<'a> for Classes<'a> {
        fn visit_stmt(&mut self, stmt: &'a ast::Stmt) {
            match stmt {
                ast::Stmt::ClassDef(_) => self.0.push(stmt),
                ast::Stmt::FunctionDef(_) => {}
                _ => walk_stmt(self, stmt),
            }
        }
    }
    let mut result = Classes(Vec::new());
    for stmt in body {
        if matches!(stmt, ast::Stmt::FunctionDef(_)) {
            result.0.push(stmt);
        } else {
            result.visit_stmt(stmt);
        }
    }
    result.0
}

#[cfg(test)]
mod tests {
    #[test]
    fn module_try_class_is_registered_but_function_class_remains_rejected() {
        let source = "def keep(value):
    return value
@keep
class Ordered:
    pass
try:
    @keep
    class Ordered:
        def method(self):
            return 1
except ValueError:
    pass
";
        let mut db = ryupy_base::SourceDb::default();
        let file = db.add("classes.py", source);
        let (parsed, diagnostics) = ryupy_syntax::parse(file, source);
        assert!(diagnostics.is_empty());
        let info = crate::ModuleInfo {
            resolve_file_at_runtime: false,
            name: "__main__".into(),
            package: String::new(),
            file: "classes.py".into(),
        };
        let (module, diagnostics) = crate::lower_module(&parsed, &info, source);
        assert!(diagnostics.is_empty(), "{diagnostics:?}");
        assert_eq!(module.classes.len(), 2);
        assert!(
            module
                .classes
                .iter()
                .all(|(_, class)| class.decorated_binding.is_some())
        );
        let source = "def factory():
    class Local:
        pass
    return Local
";
        let (parsed, _) = ryupy_syntax::parse(file, source);
        let (_, diagnostics) = crate::lower_module(&parsed, &info, source);
        assert!(
            diagnostics
                .iter()
                .any(|diagnostic| diagnostic.code == Some("E0100"))
        );
    }
}
