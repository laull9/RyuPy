//! 模块定义预登记：控制流不创建作用域，类可出现在模块 try/if/循环中。
//! 函数体与类体不递归；受支持的函数局部类由真实定义owner的词法降级处理。

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
    use crate::{Binding, ExprKind, LexicalBinding, StmtKind};

    #[test]
    fn module_try_class_is_registered_but_function_class_stays_lexical() {
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
            instance_capabilities: Default::default(),
            instance_globals: false,
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
        let (parsed, diagnostics) = ryupy_syntax::parse(file, source);
        assert!(diagnostics.is_empty(), "{diagnostics:?}");
        // 预登记只收factory，不得把函数体的Local提升到模块定义。
        let definitions = super::definitions(&parsed.module.body);
        let [ryupy_syntax::ast::Stmt::FunctionDef(factory)] = definitions.as_slice() else {
            panic!("模块预登记必须仅含factory函数");
        };
        assert_eq!(factory.name.as_str(), "factory");
        let (module, diagnostics) = crate::lower_module(&parsed, &info, source);
        assert!(diagnostics.is_empty(), "{diagnostics:?}");
        assert_eq!(module.classes.len(), 1);
        let (class_id, class) = module.classes.iter().next().unwrap();
        assert_eq!(class.name.as_str(), "Local");
        assert!(class.base.is_none() && !class.has_class_cell);
        let lexical = class.lexical.as_ref().expect("必须是词法类模板");
        let owner = lexical.owner.expect("必须有真实定义owner");
        assert_ne!(owner, module.main);
        let function = &module.functions[owner];
        assert_eq!(function.name.as_str(), "factory");
        assert_eq!(module.top_functions, vec![owner]);
        assert_eq!(lexical.qualname, "factory.<locals>.Local");
        let Some(LexicalBinding::Outer(Binding::Local(local))) = &lexical.target else {
            panic!("Local必须发布到factory的真实局部绑定");
        };
        assert_eq!(function.body.locals[*local].name.as_str(), "Local");
        let [definition, returned] = function.body.root.as_slice() else {
            panic!("原函数必须保留类定义及返回两条语句");
        };
        assert!(matches!(
            function.body.stmts[*definition].kind,
            StmtKind::ClassDef(id) if id == class_id
        ));
        let StmtKind::Return(Some(value)) = function.body.stmts[*returned].kind else {
            panic!("原函数必须返回局部类绑定");
        };
        assert!(matches!(function.body.exprs[value].kind, ExprKind::Local(id) if id == *local));
        assert!(module.top_classes.is_empty());
        assert!(
            module
                .vars
                .iter()
                .all(|(_, var)| var.name.as_str() != "Local")
        );
        assert!(
            !module.functions[module.main]
                .body
                .stmts
                .iter()
                .any(|(_, stmt)| { matches!(stmt.kind, StmtKind::ClassDef(id) if id == class_id) })
        );
    }
}
