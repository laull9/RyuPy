//! 表达式 AST → HIR 降级。
//!
//! 流程与要点：
//! 1. **字面量与变量**：
//!    - 将各类数值、布尔、字符串与 None 转换为 [`Literal`]；
//!    - 按照 LEGB 规则（局部变量 → 全局函数/类 → 内建）解析变量；
//!    - 在函数体内若读取模块级变量，根据语义规范第 5 节报 [`E0105`] 阻断。
//! 2. **运算符与去糖**：
//!    - 二元与一元算符：映射为 [`BinOp`] 与 [`UnaryOp`]；
//!    - 链式比较（`a < b < c`）：去糖展开为二元短路逻辑 `(a < b) and (b < c)`；
//!    - 多元布尔逻辑（`a and b and c`）：展开为左结合的二元 [`BoolOp`] 树；
//!    - 条件表达式（`then_ if cond else else_`）：降级为 [`IfExp`]。
//! 3. **健壮性**：
//!    - 遇到不受支持的语法结构，生成 [`Diagnostic`] 并产出 [`ExprKind::Missing`]，绝不 panic。

use rustc_hash::FxHashMap;
use ryupy_base::{Diagnostic, FileId, Ranged, Span, TextRange};
use ryupy_syntax::ast;

use crate::hir::{
    Body, BoolOp, Expr, ExprId, ExprKind, FuncId, Literal, ModuleVarId, Name, UnaryOp,
};
use crate::operator::{map_bin_op, map_bool_op, map_cmp_op, map_unary_op};
use crate::scope::{FunctionScope, GlobalScope, Resolution, resolve_builtin};

pub struct ExprLowerCtx<'a> {
    /// 模块默认值和类体不能包含 yield；函数体/嵌套默认值可以。
    pub allow_yield: bool,
    pub file: FileId,
    pub global_scope: &'a GlobalScope,
    pub func_scope: &'a FunctionScope,
    /// 模块级变量表；模块作用域里的名字直接解析到这里。
    pub module_vars: &'a FxHashMap<Name, ModuleVarId>,
    /// 嵌套 def / lambda 的 AST 位置 → FuncId（M3.0）。lambda 表达式靠它找到
    /// 分析阶段分配的函数；类体等未分析位置查不到，明确拒绝。
    pub nested_funcs: &'a FxHashMap<TextRange, FuncId>,
    /// lambda 参数默认值：在**外层作用域** lambda 位置求值，先暂存、
    /// 循环结束后回填进对应函数的 `Param::default`（与嵌套 def 一致）。
    pub nested_defaults: &'a mut FxHashMap<FuncId, Vec<Option<ExprId>>>,
    pub body: &'a mut Body,
    pub diagnostics: &'a mut Vec<Diagnostic>,
}

impl<'a> ExprLowerCtx<'a> {
    pub fn lower_expr(&mut self, expr: &ast::Expr) -> ExprId {
        let range = expr.range();
        let kind = match expr {
            ast::Expr::Yield(value) => {
                if self.allow_yield {
                    ExprKind::Yield(value.value.as_deref().map(|value| self.lower_expr(value)))
                } else {
                    self.diagnostics.push(
                        Diagnostic::error(Span::new(self.file, range), "yield 只能出现在函数体内")
                            .with_code("E0100"),
                    );
                    ExprKind::Missing
                }
            }
            ast::Expr::NumberLiteral(num) => match &num.value {
                ast::Number::Int(int_val) => {
                    // 先试 i64 快路径；更大的值仍要进 HIR，由 ty 决定是否降级到动态执行
                    if let Some(val) = int_val.as_i64() {
                        ExprKind::Literal(Literal::Int(i128::from(val)))
                    } else if let Some(val) = int_val.as_u64() {
                        ExprKind::Literal(Literal::Int(i128::from(val)))
                    } else {
                        self.diagnostics.push(
                            Diagnostic::error(
                                Span::new(self.file, num.range),
                                "整数字面量超出支持范围",
                            )
                            .with_code("E0100")
                            .with_primary_label("字面量超过 128 位有符号整数"),
                        );
                        ExprKind::Missing
                    }
                }
                ast::Number::Float(f) => ExprKind::Literal(Literal::Float(*f)),
                _ => {
                    self.diagnostics.push(
                        Diagnostic::error(Span::new(self.file, num.range), "暂不支持该数值字面量")
                            .with_code("E0100")
                            .with_primary_label("暂不支持此类数值"),
                    );
                    ExprKind::Missing
                }
            },
            ast::Expr::BooleanLiteral(b) => ExprKind::Literal(Literal::Bool(b.value)),
            ast::Expr::NoneLiteral(_) => ExprKind::Literal(Literal::None),
            ast::Expr::StringLiteral(s) => ExprKind::Literal(Literal::Str(s.value.to_str().into())),
            ast::Expr::Name(name_expr) => self.lower_name(name_expr),
            ast::Expr::BinOp(bin) => {
                if let Some(op) = map_bin_op(bin.op) {
                    let lhs = self.lower_expr(&bin.left);
                    let rhs = self.lower_expr(&bin.right);
                    ExprKind::Binary {
                        op,
                        lhs,
                        rhs,
                        inplace: false,
                    }
                } else {
                    self.diagnostics.push(
                        Diagnostic::error(Span::new(self.file, bin.range), "暂不支持该二元运算符")
                            .with_code("E0100")
                            .with_primary_label("暂不支持此运算"),
                    );
                    ExprKind::Missing
                }
            }
            ast::Expr::UnaryOp(u) => {
                if let Some(op) = map_unary_op(u.op) {
                    if op == UnaryOp::Pos {
                        // +x 在数值上等价于 x 本身
                        return self.lower_expr(&u.operand);
                    }
                    let operand = self.lower_expr(&u.operand);
                    ExprKind::Unary { op, operand }
                } else {
                    self.diagnostics.push(
                        Diagnostic::error(Span::new(self.file, u.range), "暂不支持该一元运算符")
                            .with_code("E0100")
                            .with_primary_label("暂不支持此一元操作"),
                    );
                    ExprKind::Missing
                }
            }
            ast::Expr::Compare(cmp) => self.lower_compare(cmp),
            ast::Expr::BoolOp(b) => self.lower_bool_op(b),
            ast::Expr::If(if_exp) => {
                let cond = self.lower_expr(&if_exp.test);
                let then_ = self.lower_expr(&if_exp.body);
                let else_ = self.lower_expr(&if_exp.orelse);
                ExprKind::IfExp { cond, then_, else_ }
            }
            ast::Expr::Call(call) => self.lower_call(call),
            ast::Expr::Attribute(attr) => {
                let base = self.lower_expr(&attr.value);
                let name = Name::from(attr.attr.as_str());
                ExprKind::Attribute { base, name }
            }
            ast::Expr::ListComp(_)
            | ast::Expr::DictComp(_)
            | ast::Expr::SetComp(_)
            | ast::Expr::Generator(_) => {
                match crate::comprehension_ast::Comprehension::from_expr(expr) {
                    Some(comp) => self.lower_comprehension(comp),
                    None => ExprKind::Missing,
                }
            }
            ast::Expr::List(list) => ExprKind::List(self.lower_elements(&list.elts)),
            ast::Expr::Tuple(tuple) => ExprKind::Tuple(self.lower_elements(&tuple.elts)),
            ast::Expr::Set(set) => ExprKind::Set(self.lower_elements(&set.elts)),
            ast::Expr::Dict(dict) => self.lower_dict(dict),
            ast::Expr::FString(fstring) => self.lower_fstring(fstring),
            ast::Expr::BytesLiteral(bytes) => {
                ExprKind::Bytes(bytes.value.bytes().collect::<Vec<u8>>().into_boxed_slice())
            }
            ast::Expr::Subscript(sub) => {
                let base = self.lower_expr(&sub.value);
                let index = self.lower_subscript_index(&sub.slice);
                ExprKind::Subscript { base, index }
            }
            ast::Expr::Starred(starred) => {
                let inner = self.lower_expr(&starred.value);
                ExprKind::Starred(inner)
            }
            ast::Expr::Lambda(lambda) => self.lower_lambda(lambda),
            _ => {
                self.diagnostics.push(
                    Diagnostic::error(Span::new(self.file, range), "暂不支持该表达式")
                        .with_code("E0100")
                        .with_primary_label("Phase 1 暂不支持该语法"),
                );
                ExprKind::Missing
            }
        };

        self.body.exprs.alloc(Expr { kind, range })
    }

    /// 容器字面量的元素列表，保留 `*x` 星号标记。
    fn lower_elements(&mut self, elements: &[ast::Expr]) -> Vec<ExprId> {
        elements.iter().map(|e| self.lower_expr(e)).collect()
    }

    /// `{k: v}` 字典字面量。`**other` 需要运行时合并，留到后续竖切。
    fn lower_dict(&mut self, dict: &ast::ExprDict) -> ExprKind {
        // 大字典在 CPython 中分块建表再复用缓存 hash 合并。只对不会执行用户
        // hash/eq 的纯标量字面量键采用直接插入；名字/调用不能仅凭推断类型放行。
        if dict.items.len() > 17
            && !dict.items.iter().all(|item| {
                matches!(
                    item.key.as_ref(),
                    Some(
                        ast::Expr::NumberLiteral(_)
                            | ast::Expr::StringLiteral(_)
                            | ast::Expr::BooleanLiteral(_)
                            | ast::Expr::NoneLiteral(_)
                    )
                )
            })
        {
            self.diagnostics.push(
                Diagnostic::error(
                    Span::new(self.file, dict.range),
                    "超过17项的字典暂仅支持标量字面量键；动态键分块缓存哈希合并尚未支持",
                )
                .with_code("E0100"),
            );
            return ExprKind::Missing;
        }
        let mut pairs = Vec::new();
        for item in &dict.items {
            let Some(key) = &item.key else {
                self.diagnostics.push(
                    Diagnostic::error(
                        Span::new(self.file, item.value.range()),
                        "暂不支持在字典字面量里使用 `**`",
                    )
                    .with_code("E0100")
                    .with_primary_label("不支持字典解包"),
                );
                continue;
            };
            let key_id = self.lower_expr(key);
            let value_id = self.lower_expr(&item.value);
            pairs.push((key_id, value_id));
        }
        ExprKind::Dict(pairs)
    }

    /// 下标表达式：整数下标或切片。
    pub(crate) fn lower_subscript_index(&mut self, slice: &ast::Expr) -> ExprId {
        if let ast::Expr::Slice(slice_expr) = slice {
            let start = slice_expr.lower.as_deref().map(|e| self.lower_expr(e));
            let stop = slice_expr.upper.as_deref().map(|e| self.lower_expr(e));
            let step = slice_expr.step.as_deref().map(|e| self.lower_expr(e));
            return self.body.exprs.alloc(Expr {
                kind: ExprKind::Slice { start, stop, step },
                range: slice_expr.range,
            });
        }
        self.lower_expr(slice)
    }

    /// lambda 表达式（M3.0）：分析阶段已为它分配匿名函数，这里记录引用；
    /// 参数默认值在当前位置（外层作用域）求值——每次执行 lambda 表达式都
    /// 重新求值，与嵌套 def 一致。
    fn lower_lambda(&mut self, lambda: &ast::ExprLambda) -> ExprKind {
        let Some(&func_id) = self.nested_funcs.get(&lambda.range) else {
            self.diagnostics.push(
                Diagnostic::error(
                    Span::new(self.file, lambda.range),
                    "lambda 暂不支持出现在此位置",
                )
                .with_code("E0100")
                .with_primary_label("类体等位置的 lambda 尚未支持"),
            );
            return ExprKind::Missing;
        };
        if let Some(parameters) = &lambda.parameters {
            let mut aligned: Vec<Option<ExprId>> = Vec::new();
            for param_def in parameters.posonlyargs.iter().chain(&parameters.args) {
                aligned.push(
                    param_def
                        .default
                        .as_deref()
                        .map(|expr| self.lower_expr(expr)),
                );
            }
            if parameters.vararg.is_some() {
                aligned.push(None);
            }
            for param_def in &parameters.kwonlyargs {
                aligned.push(
                    param_def
                        .default
                        .as_deref()
                        .map(|expr| self.lower_expr(expr)),
                );
            }
            if parameters.kwarg.is_some() {
                aligned.push(None);
            }
            if aligned.iter().any(Option::is_some) {
                self.nested_defaults.insert(func_id, aligned);
            }
        }
        ExprKind::Lambda(func_id)
    }

    fn lower_name(&mut self, name_expr: &ast::ExprName) -> ExprKind {
        let name = Name::from(name_expr.id.as_str());
        // `global` 声明的名字优先落到模块命名空间（即使同名的局部变量预扫描过）。
        if self.func_scope.is_global(&name)
            && let Some(&var) = self.module_vars.get(&name)
        {
            return ExprKind::ModuleVar(var);
        }
        // 外层 cell（M3.0 闭包）：`nonlocal` 声明的名字与捕获的自由变量。
        // cell 读取在运行时有未赋值检查，返回装箱值。
        if let Some(&idx) = self.func_scope.free.get(&name) {
            return ExprKind::Free(idx);
        }
        // LEGB 查找顺序：Local → 顶层函数/类 → 模块变量 → Builtin。
        if let Some(&loc_id) = self.func_scope.locals.get(&name) {
            ExprKind::Local(loc_id)
        } else if let Some(&res) = self.global_scope.names.get(&name) {
            match res {
                Resolution::Func(fn_id) => ExprKind::Func(fn_id),
                Resolution::Class(cls_id) => ExprKind::Class(cls_id),
                Resolution::Local(_) | Resolution::Builtin(_) => ExprKind::Missing,
            }
        } else if let Some(&var) = self.module_vars.get(&name) {
            // 模块级变量：函数体内读写同一份静态槽位（M2.7）。
            ExprKind::ModuleVar(var)
        } else if let Some(builtin) = resolve_builtin(name.as_str()) {
            ExprKind::Builtin(builtin)
        } else {
            self.diagnostics.push(
                Diagnostic::error(
                    Span::new(self.file, name_expr.range),
                    format!("未定义的变量或名称 `{name}`"),
                )
                .with_code("E0106")
                .with_primary_label("未解析的名字"),
            );
            ExprKind::Missing
        }
    }

    fn lower_compare(&mut self, cmp: &ast::ExprCompare) -> ExprKind {
        if cmp.ops.is_empty() || cmp.operands.len() < 2 {
            return ExprKind::Missing;
        }

        // 简单二元比较：a < b
        if cmp.ops.len() == 1 {
            let op = match map_cmp_op(cmp.ops[0]) {
                Some(o) => o,
                None => {
                    self.diagnostics.push(
                        Diagnostic::error(
                            Span::new(self.file, cmp.range()),
                            "暂不支持该比较运算符",
                        )
                        .with_code("E0100")
                        .with_primary_label("暂不支持的比较操作"),
                    );
                    return ExprKind::Missing;
                }
            };
            let lhs = self.lower_expr(&cmp.operands[0]);
            let rhs = self.lower_expr(&cmp.operands[1]);
            return ExprKind::Compare { op, lhs, rhs };
        }

        // 链式比较去糖：a < b < c => (a < b) and (b < c)
        // 注意：中间表达式求值在没有副作用时可安全复制
        let mut sub_comparisons = Vec::new();
        for (i, op_raw) in cmp.ops.iter().enumerate() {
            let op = match map_cmp_op(*op_raw) {
                Some(o) => o,
                None => {
                    self.diagnostics.push(
                        Diagnostic::error(
                            Span::new(self.file, cmp.range()),
                            "暂不支持该比较运算符",
                        )
                        .with_code("E0100")
                        .with_primary_label("暂不支持的比较操作"),
                    );
                    return ExprKind::Missing;
                }
            };
            let lhs = self.lower_expr(&cmp.operands[i]);
            let rhs = self.lower_expr(&cmp.operands[i + 1]);
            let pair_cmp = self.body.exprs.alloc(Expr {
                kind: ExprKind::Compare { op, lhs, rhs },
                range: cmp.range(),
            });
            sub_comparisons.push(pair_cmp);
        }

        // 用 and 连接各个比较分量
        let mut result = sub_comparisons[0];
        for &next_cmp in &sub_comparisons[1..] {
            result = self.body.exprs.alloc(Expr {
                kind: ExprKind::BoolOp {
                    op: BoolOp::And,
                    lhs: result,
                    rhs: next_cmp,
                },
                range: cmp.range(),
            });
        }

        self.body.exprs[result].kind.clone()
    }

    fn lower_bool_op(&mut self, b: &ast::ExprBoolOp) -> ExprKind {
        let op = map_bool_op(b.op);
        if b.values.is_empty() {
            return ExprKind::Missing;
        }
        if b.values.len() == 1 {
            let single = self.lower_expr(&b.values[0]);
            return self.body.exprs[single].kind.clone();
        }

        // 多元布尔左结合展开：a and b and c => (a and b) and c
        let mut cur = self.lower_expr(&b.values[0]);
        for val_expr in &b.values[1..] {
            let rhs = self.lower_expr(val_expr);
            cur = self.body.exprs.alloc(Expr {
                kind: ExprKind::BoolOp { op, lhs: cur, rhs },
                range: b.range(),
            });
        }
        self.body.exprs[cur].kind.clone()
    }

    /// f-string → 字面量 / 插值段序列。
    ///
    /// 格式说明符本身也可以包含插值（`f"{x:{width}}"`），这需要运行时构造规格字符串；
    /// 当前只支持纯字面量的规格，遇到嵌套插值报诊断。
    fn lower_fstring(&mut self, fstring: &ast::ExprFString) -> ExprKind {
        use crate::hir::FStringPart;

        let mut parts = Vec::new();
        for element in fstring.value.elements() {
            match element {
                ast::InterpolatedStringElement::Literal(literal) => {
                    parts.push(FStringPart::Literal(literal.value.clone()));
                }
                ast::InterpolatedStringElement::Interpolation(item) => {
                    let value = self.lower_expr(&item.expression);
                    let spec = match &item.format_spec {
                        None => None,
                        Some(spec) => {
                            let mut text = String::new();
                            let mut ok = true;
                            for part in spec.elements.iter() {
                                match part {
                                    ast::InterpolatedStringElement::Literal(literal) => {
                                        text.push_str(&literal.value);
                                    }
                                    ast::InterpolatedStringElement::Interpolation(_) => {
                                        ok = false;
                                    }
                                }
                            }
                            if !ok {
                                self.diagnostics.push(
                                    Diagnostic::error(
                                        Span::new(self.file, spec.range),
                                        "f-string 的格式说明符暂不支持嵌套插值",
                                    )
                                    .with_code("E0100")
                                    .with_primary_label("不支持嵌套插值"),
                                );
                            }
                            Some(text.into_boxed_str())
                        }
                    };
                    parts.push(FStringPart::Value {
                        value,
                        conversion: item.conversion.to_byte().unwrap_or(0),
                        spec,
                    });
                }
            }
        }
        ExprKind::FString(parts)
    }

    /// 调用实参完整保留四种形态：位置、`*args`、关键字、`**kwargs`。
    ///
    /// 绑定（谁对应哪个参数、默认值、重复绑定报错）属于语义层，放在 ty / MIR 处理，
    /// HIR 只负责忠实记录源码里写出来的形态。
    fn lower_call(&mut self, call: &ast::ExprCall) -> ExprKind {
        let suspends = crate::generator_scan::call_suspends(call);
        if suspends && call.arguments.keywords.iter().any(|arg| arg.arg.is_none()) {
            self.diagnostics.push(
                Diagnostic::error(
                    Span::new(self.file, call.arguments.range),
                    "调用双星号实参暂未支持跨暂停的映射快照",
                )
                .with_code("E0100")
                .with_primary_label("先把 yield 的接收值赋给局部，再进行调用"),
            );
            return ExprKind::Missing;
        }
        use crate::hir::CallArg;

        let callee = self.lower_expr(&call.func);
        let mut args = Vec::new();
        for arg in &*call.arguments.args {
            match arg {
                ast::Expr::Starred(starred) => {
                    args.push(CallArg::Starred(self.lower_expr(&starred.value)));
                }
                _ => args.push(CallArg::Positional(self.lower_expr(arg))),
            }
        }
        for keyword in &call.arguments.keywords {
            match &keyword.arg {
                Some(name) => {
                    let value = self.lower_expr(&keyword.value);
                    args.push(CallArg::Keyword(Name::from(name.as_str()), value));
                }
                None => {
                    let value = self.lower_expr(&keyword.value);
                    args.push(CallArg::DoubleStarred(value));
                }
            }
        }
        if suspends
            && let ExprKind::Builtin(builtin) = self.body.exprs[callee].kind
            && !crate::generator_scan::prepared_builtin_supported(builtin, &args)
        {
            self.diagnostics.push(
                Diagnostic::error(
                    Span::new(self.file, call.arguments.range),
                    "该内建的多参数或关键字暂停调用尚未支持完整构造协议",
                )
                .with_code("E0100"),
            );
            return ExprKind::Missing;
        }
        if suspends {
            ExprKind::PreparedCall { callee, args }
        } else {
            ExprKind::Call { callee, args }
        }
    }
}
