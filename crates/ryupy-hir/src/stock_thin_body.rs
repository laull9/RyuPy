//! 薄函数局部绑定与身份选择的封闭AST合同；递归审查不可达分支和所有子表达式。
//! 值仅来自名称、None、身份比较和条件选择，赋值只写单个局部，不引入动态业务回退。
use ryupy_base::{Diagnostic, FileId, Ranged, Span};
use ryupy_syntax::ast;

pub(super) fn validate(body: &[ast::Stmt], file: FileId) -> Vec<Diagnostic> {
    let mut check = Check {
        file,
        errors: vec![],
    };
    check.suite(body);
    check.errors
}

struct Check {
    file: FileId,
    errors: Vec<Diagnostic>,
}

impl Check {
    fn reject(&mut self, node: &impl Ranged, detail: &str) {
        self.errors.push(
            Diagnostic::error(
                Span::new(self.file, node.range()),
                format!("薄函数体只支持局部名称绑定、None和身份条件选择：{detail}"),
            )
            .with_code("E0100"),
        );
    }

    fn suite(&mut self, body: &[ast::Stmt]) {
        for stmt in body {
            match stmt {
                ast::Stmt::Return(ret) => {
                    if let Some(value) = &ret.value {
                        self.value(value);
                    }
                }
                ast::Stmt::Assign(assign) => {
                    // 单目标避免提前承诺拆包或链式写入的求值/所有权合同。
                    if let [ast::Expr::Name(target)] = assign.targets.as_slice() {
                        self.name(target);
                    } else {
                        self.reject(stmt, "赋值目标必须是一个局部名称");
                    }
                    self.value(&assign.value);
                }
                ast::Stmt::If(branch) => {
                    self.condition(&branch.test);
                    self.suite(&branch.body);
                    for clause in &branch.elif_else_clauses {
                        if let Some(test) = &clause.test {
                            self.condition(test);
                        }
                        self.suite(&clause.body);
                    }
                }
                ast::Stmt::Pass(_) => {}
                _ => self.reject(stmt, "该语句形状未准入"),
            }
        }
    }

    fn name(&mut self, name: &ast::ExprName) {
        // __debug__按编译选项冻结，不能伪装成lease的可遮蔽全局。
        if name.id.as_str() == "__debug__" {
            self.reject(name, "语言常量 `__debug__` 暂未准入");
        }
    }

    fn condition(&mut self, expr: &ast::Expr) {
        if matches!(expr, ast::Expr::Compare(cmp)
            if cmp.ops.len() == 1 && matches!(cmp.ops[0], ast::CmpOp::Is | ast::CmpOp::IsNot))
        {
            self.value(expr);
        } else {
            self.reject(expr, "条件必须是单个is/is not比较");
        }
    }

    fn value(&mut self, expr: &ast::Expr) {
        match expr {
            ast::Expr::Name(name) => self.name(name),
            ast::Expr::NoneLiteral(_) => {}
            ast::Expr::Compare(cmp)
                if cmp.ops.len() == 1
                    && matches!(cmp.ops[0], ast::CmpOp::Is | ast::CmpOp::IsNot) =>
            {
                for operand in &cmp.operands {
                    self.value(operand);
                }
            }
            ast::Expr::If(choice) => {
                self.condition(&choice.test);
                self.value(&choice.body);
                self.value(&choice.orelse);
            }
            _ => self.reject(expr, "该表达式形状未准入"),
        }
    }
}
