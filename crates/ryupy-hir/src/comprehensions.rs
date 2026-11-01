//! 推导式与生成器表达式的独立作用域与去糖。
//!
//! 每次执行创建一个隐式函数，复用已有的共享 cell、动态调用与通用迭代。
//! 最左 iterable 在外层立即转换成迭代器；隐藏参数把它送入推导式作用域。
//! 循环和过滤器只创建 HIR 控制流，不在每个元素处重新创建函数或环境。

use crate::comprehension_ast::{Comprehension, ComprehensionKind};
use crate::hir::*;
use crate::scope::FunctionScope;
use ryupy_base::{Diagnostic, Ranged, Span, TextRange};
use ryupy_syntax::ast;
use ryupy_syntax::ast::visitor::{Visitor, walk_expr};

const ITERATOR: &str = ".0";

/// 登记隐式参数与循环绑定；标识符中含点，用户源码不能与之冲突。
pub(crate) fn prepare_scope(
    comp: Comprehension<'_>,
    params: &mut Vec<Param>,
    scope: &mut FunctionScope,
    body: &mut Body,
) {
    let local = body.locals.alloc(Local {
        name: Name::from(ITERATOR),
        annotation: None,
        is_param: true,
        range: comp.range,
    });
    scope.locals.insert(Name::from(ITERATOR), local);
    params.push(Param {
        local,
        annotation: None,
        default: None,
        kind: ParamKind::PositionalOnly,
        range: comp.range,
    });
    struct Targets<'a> {
        scope: &'a mut FunctionScope,
        body: &'a mut Body,
    }
    impl<'a> Visitor<'a> for Targets<'_> {
        fn visit_expr(&mut self, expr: &'a ast::Expr) {
            if let ast::Expr::Name(name) = expr {
                let name_str = Name::from(name.id.as_str());
                self.scope
                    .locals
                    .entry(name_str.clone())
                    .or_insert_with(|| {
                        self.body.locals.alloc(Local {
                            name: name_str,
                            annotation: None,
                            is_param: false,
                            range: name.range,
                        })
                    });
            } else {
                walk_expr(self, expr);
            }
        }
    }
    let mut targets = Targets { scope, body };
    for generator in comp.generators {
        targets.visit_expr(&generator.target);
    }
}

impl crate::expr::ExprLowerCtx<'_> {
    pub(crate) fn lower_comprehension(&mut self, comp: Comprehension<'_>) -> ExprKind {
        let Some(first) = comp.generators.first() else {
            return ExprKind::Missing;
        };
        if comp.kind == ComprehensionKind::Dict && comp.key.is_none() {
            self.diagnostics.push(
                Diagnostic::error(Span::new(self.file, comp.range), "暂不支持字典推导式解包")
                    .with_code("E0100")
                    .with_primary_label("字典推导式需要显式的 key: value"),
            );
            return ExprKind::Missing;
        }
        if comp.generators.iter().any(|generator| generator.is_async) {
            self.diagnostics.push(
                Diagnostic::error(Span::new(self.file, comp.range), "暂不支持异步推导式")
                    .with_code("E0100")
                    .with_primary_label("不能把 async for 静默降级为同步迭代"),
            );
            return ExprKind::Missing;
        }
        let Some(&func) = self.nested_funcs.get(&comp.range) else {
            self.diagnostics.push(
                Diagnostic::error(
                    Span::new(self.file, comp.range),
                    "推导式暂不支持出现在此位置",
                )
                .with_code("E0100")
                .with_primary_label("类体推导式的作用域尚未接入"),
            );
            return ExprKind::Missing;
        };
        let iter_value = self.lower_expr(&first.iter);
        // 直接引用内建 iter，不受用户同名绑定影响；这对应 Python 的 GET_ITER。
        let iter_fn = self.body.exprs.alloc(Expr {
            kind: ExprKind::Builtin(Builtin::Dynamic("iter")),
            range: first.iter.range(),
        });
        let iterator = self.body.exprs.alloc(Expr {
            kind: ExprKind::Call {
                callee: iter_fn,
                args: vec![CallArg::Positional(iter_value)],
            },
            range: first.iter.range(),
        });
        let callee = self.body.exprs.alloc(Expr {
            kind: ExprKind::Lambda(func),
            range: comp.range,
        });
        ExprKind::Call {
            callee,
            args: vec![CallArg::Positional(iterator)],
        }
    }
}

impl crate::lower::StmtLowerCtx<'_> {
    /// 急切推导式创建结果容器；生成器表达式只发 Yield，复用相同循环和过滤边界。
    pub(crate) fn lower_comprehension_body(&mut self, comp: Comprehension<'_>) {
        let empty_kind = match comp.kind {
            ComprehensionKind::List => Some(ExprKind::List(Vec::new())),
            ComprehensionKind::Dict => Some(ExprKind::Dict(Vec::new())),
            ComprehensionKind::Set => Some(ExprKind::Set(Vec::new())),
            ComprehensionKind::Generator => None,
        };
        let (mut body, result) = if let Some(empty_kind) = empty_kind {
            let result = self.body.locals.alloc(Local {
                name: Name::from(".result"),
                annotation: None,
                is_param: false,
                range: comp.range,
            });
            let empty = self.comp_expr(empty_kind, comp.range);
            let init = self.comp_stmt(
                StmtKind::Assign {
                    target: AssignTarget::Local(result),
                    value: empty,
                },
                comp.range,
            );
            self.body.root.push(init);
            (self.comp_emit(comp, result), Some(result))
        } else {
            // 生成器没有结果容器或隐藏结果槽，每次恢复只产出一个元素。
            let value = self.lower_expr(comp.elt);
            (
                vec![self.comp_stmt(StmtKind::Yield(Some(value)), comp.range)],
                None,
            )
        };
        // 从内向外包装，运行期仍严格按源码的 for / if / 元素顺序求值。
        for (index, generator) in comp.generators.iter().enumerate().rev() {
            for condition in generator.ifs.iter().rev() {
                let cond = self.lower_expr(condition);
                body = vec![self.comp_stmt(
                    StmtKind::If {
                        cond,
                        then_: body,
                        else_: Vec::new(),
                    },
                    condition.range(),
                )];
            }
            let var = match &generator.target {
                ast::Expr::Name(name) => {
                    let Some(var) = self.resolve_binding(&Name::from(name.id.as_str())) else {
                        continue;
                    };
                    var
                }
                ast::Expr::Tuple(tuple) => {
                    let Some(items) = self.lower_sequence_target(&tuple.elts, tuple.range) else {
                        continue;
                    };
                    self.comp_unpack(items, &mut body, tuple.range)
                }
                ast::Expr::List(list) => {
                    let Some(items) = self.lower_sequence_target(&list.elts, list.range) else {
                        continue;
                    };
                    self.comp_unpack(items, &mut body, list.range)
                }
                _ => {
                    self.diagnostics.push(
                        Diagnostic::error(
                            Span::new(self.file, generator.target.range()),
                            "暂不支持该推导式循环目标",
                        )
                        .with_code("E0100")
                        .with_primary_label("只支持名字或序列解包"),
                    );
                    continue;
                }
            };
            let iter = if index == 0 {
                self.comp_expr(
                    ExprKind::Local(self.func_scope.locals[&Name::from(ITERATOR)]),
                    generator.iter.range(),
                )
            } else {
                self.lower_expr(&generator.iter)
            };
            body = vec![self.comp_stmt(
                StmtKind::ForEach {
                    var,
                    iter,
                    body,
                    orelse: Vec::new(),
                },
                generator.range,
            )];
        }
        let value = result.map(|result| self.comp_expr(ExprKind::Local(result), comp.range));
        let ret = self.comp_stmt(StmtKind::Return(value), comp.range);
        self.body.root.extend(body);
        self.body.root.push(ret);
    }

    /// 字典必须先求 key 再求 value：普通下标赋值的规则恰好相反，
    /// 因此先把 key 固定在隐藏局部中，再复用 Assign(Subscript)。
    fn comp_emit(&mut self, comp: Comprehension<'_>, result: LocalId) -> Vec<StmtId> {
        let base = self.comp_expr(ExprKind::Local(result), comp.range);
        if let Some(key) = comp.key {
            let key_local = self.body.locals.alloc(Local {
                name: Name::from(".key"),
                annotation: None,
                is_param: false,
                range: key.range(),
            });
            let key_value = self.lower_expr(key);
            let save_key = self.comp_stmt(
                StmtKind::Assign {
                    target: AssignTarget::Local(key_local),
                    value: key_value,
                },
                key.range(),
            );
            let index = self.comp_expr(ExprKind::Local(key_local), key.range());
            let value = self.lower_expr(comp.elt);
            let insert = self.comp_stmt(
                StmtKind::Assign {
                    target: AssignTarget::Subscript { base, index },
                    value,
                },
                comp.range,
            );
            vec![save_key, insert]
        } else {
            let method = if comp.kind == ComprehensionKind::Set {
                "add"
            } else {
                "append"
            };
            let callee = self.comp_expr(
                ExprKind::Attribute {
                    base,
                    name: Name::from(method),
                },
                comp.range,
            );
            let elt = self.lower_expr(comp.elt);
            let call = self.comp_expr(
                ExprKind::Call {
                    callee,
                    args: vec![CallArg::Positional(elt)],
                },
                comp.range,
            );
            vec![self.comp_stmt(StmtKind::Expr(call), comp.range)]
        }
    }

    fn comp_unpack(
        &mut self,
        items: Vec<SequenceItem>,
        body: &mut Vec<StmtId>,
        range: TextRange,
    ) -> Binding {
        let local = self.body.locals.alloc(Local {
            name: Name::from(format!(".item{}", self.body.locals.len()).as_str()),
            annotation: None,
            is_param: false,
            range,
        });
        let value = self.comp_expr(ExprKind::Local(local), range);
        let assign = self.comp_stmt(
            StmtKind::Assign {
                target: AssignTarget::Sequence(items),
                value,
            },
            range,
        );
        body.insert(0, assign);
        Binding::Local(local)
    }

    fn comp_expr(&mut self, kind: ExprKind, range: TextRange) -> ExprId {
        self.body.exprs.alloc(Expr { kind, range })
    }

    fn comp_stmt(&mut self, kind: StmtKind, range: TextRange) -> StmtId {
        let line = self.line_of(range.start().to_usize());
        self.body.stmts.alloc(Stmt { kind, range, line })
    }
}
