//! stock真实namespace入口的冻结语义域；只检查源码形状，不按第三方库名选择。
//! callable所有权仍待core/handle产品接线，故连不可达def、方法与lambda也必须排除。
use ryupy_base::InstanceCapabilities;
use ryupy_syntax::ast::visitor::{Visitor, walk_expr, walk_stmt};
use ryupy_syntax::{ParsedModule, ast};

pub fn stock_entry_capabilities() -> InstanceCapabilities {
    InstanceCapabilities {
        entry_namespace: true,
        aot_class_guards: true,
        direct_globals: true,
        ordinary_name_handlers: true,
        relative_from_import: false,
    }
}

/// 旧typed/普通原生路径保持原资格；新域无解释执行、失败重编或业务委派。
pub fn stock_entry_supported(parsed: &ParsedModule) -> bool {
    let mut scan = Scan { supported: true };
    for statement in &parsed.module.body {
        scan.visit_stmt(statement);
    }
    scan.supported
        && crate::instance_globals::validate(
            &parsed.module.body,
            parsed.file,
            stock_entry_capabilities(),
        )
        .is_empty()
}

struct Scan {
    supported: bool,
}
impl<'a> Visitor<'a> for Scan {
    fn visit_stmt(&mut self, statement: &'a ast::Stmt) {
        match statement {
            ast::Stmt::FunctionDef(_) => self.supported = false,
            ast::Stmt::ClassDef(class)
                if class
                    .arguments
                    .as_ref()
                    .is_some_and(|args| !args.keywords.is_empty() || args.args.len() > 1) =>
            {
                self.supported = false
            }
            _ => {}
        }
        walk_stmt(self, statement);
    }
    fn visit_expr(&mut self, expression: &'a ast::Expr) {
        if matches!(expression, ast::Expr::Lambda(_)) {
            self.supported = false;
        }
        walk_expr(self, expression);
    }
}

#[cfg(test)]
mod tests {
    fn parsed(source: &str) -> ryupy_syntax::ParsedModule {
        let mut db = ryupy_base::SourceDb::default();
        let file = db.add("stock_entry.py", source);
        let (parsed, errors) = ryupy_syntax::parse(file, source);
        assert!(errors.is_empty(), "{errors:?}");
        parsed
    }

    #[test]
    fn stock_entry_keeps_class_body_and_decorators_in_cil() {
        let source = "import decorators\n@decorators.define\nclass Point:\n    x: int\n    y: int = 0\npoint = Point(1, 2)\nprint(point)\n";
        let parsed = parsed(source);
        assert!(super::stock_entry_supported(&parsed));
        let (_, diagnostics) = crate::lower_module(
            &parsed,
            &crate::ModuleInfo {
                instance_globals: true,
                instance_capabilities: super::stock_entry_capabilities(),
                foreign_class_construction: true,
                ..Default::default()
            },
            source,
        );
        assert!(diagnostics.is_empty(), "{diagnostics:?}");
        assert!(!crate::instance_ast_supported(&parsed, Default::default()));
    }

    #[test]
    fn stock_entry_excludes_all_unproven_callable_ownership_even_in_dead_code() {
        for source in [
            "def work():\n    return 1\n",
            "if False:\n    def work():\n        return 1\n",
            "class C:\n    def method(self):\n        pass\n",
            "callback = lambda: 1\n",
            "class C(A, B):\n    pass\n",
            "x: int = 1\n",
        ] {
            assert!(!super::stock_entry_supported(&parsed(source)), "{source}");
        }
    }
}
