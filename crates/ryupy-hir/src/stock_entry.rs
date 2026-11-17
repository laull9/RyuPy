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
    let mut scan = Scan {
        supported: true,
        needs_namespace: false,
        loop_depth: 0,
    };
    for statement in &parsed.module.body {
        scan.visit_stmt(statement);
    }
    scan.supported
        && scan.needs_namespace
        && crate::instance_globals::validate(
            &parsed.module.body,
            parsed.file,
            stock_entry_capabilities(),
        )
        .is_empty()
}

struct Scan {
    supported: bool,
    needs_namespace: bool,
    loop_depth: usize,
}
impl<'a> Visitor<'a> for Scan {
    fn visit_stmt(&mut self, statement: &'a ast::Stmt) {
        match statement {
            ast::Stmt::FunctionDef(_) => self.supported = false,
            ast::Stmt::ClassDef(class) => {
                // 首切仅迁移隐式 object 布局。显式基类可能要求 list/dict/str 或异常
                // 的专用布局，须继续交给原路径，不能先准入再在 Runtime 拒绝。
                if self.loop_depth != 0
                    || class
                        .arguments
                        .as_ref()
                        .is_some_and(|args| !args.keywords.is_empty() || !args.args.is_empty())
                {
                    self.supported = false;
                }
                self.needs_namespace |= !class.decorator_list.is_empty();
            }
            ast::Stmt::For(_) | ast::Stmt::While(_) => {
                // 循环建类可经迭代器等普通对象形成跨堆强环；所有权合同完成前
                // 保留旧域，连循环内不可达的类声明也不自动迁移。
                self.loop_depth += 1;
                walk_stmt(self, statement);
                self.loop_depth -= 1;
                return;
            }
            _ => {}
        }
        walk_stmt(self, statement);
    }
    fn visit_expr(&mut self, expression: &'a ast::Expr) {
        if matches!(expression, ast::Expr::Lambda(_)) {
            self.supported = false;
        }
        if let ast::Expr::Call(call) = expression
            && let ast::Expr::Name(name) = call.func.as_ref()
            && matches!(name.id.as_str(), "globals" | "locals" | "vars")
            && call.arguments.args.is_empty()
            && call.arguments.keywords.is_empty()
        {
            // 有真实 namespace 需求才支付动态字典成本；普通数值脚本继续 typed。
            self.needs_namespace = true;
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

    #[test]
    fn stock_entry_preserves_special_layouts_and_loop_class_ownership() {
        for base in ["list", "dict", "str", "ValueError", "object", "Other"] {
            let source = format!("namespace = globals()\nclass C({base}):\n    pass\n");
            assert!(!super::stock_entry_supported(&parsed(&source)), "{source}");
        }
        for source in [
            "namespace = globals()\nfor i in range(2):\n    class C:\n        pass\n    C.keep = iter((C,))\n",
            "namespace = globals()\nwhile False:\n    if False:\n        class C:\n            pass\n",
        ] {
            assert!(!super::stock_entry_supported(&parsed(source)), "{source}");
        }
    }

    #[test]
    fn stock_entry_requires_namespace_demand_to_preserve_typed_globals() {
        for source in [
            "import ssl\ntotal = 0\nfor i in range(50000):\n    total += i\nprint(total)\n",
            "import ssl\nclass C:\n    pass\nprint(C)\n",
        ] {
            assert!(!super::stock_entry_supported(&parsed(source)), "{source}");
        }
        assert!(super::stock_entry_supported(&parsed(
            "namespace = globals()\nprint(namespace is globals())\n"
        )));
    }
}
