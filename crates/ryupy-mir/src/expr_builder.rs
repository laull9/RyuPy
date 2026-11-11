//! 表达式 HIR → MIR 降级构建器。
//!
//! 分两条路径，由当前函数的 [`FuncMode`] 决定（见 `docs/adr/0010`）：
//!
//! **静态路径**（Phase 1 起）：
//! - `//` `%` `**` 改写为运行时辅助调用；`/` 先把整数提升为 float；
//! - `int` 与 `float` 混合时插入显式 `Cast`；
//! - 比较、分支使用 CIL 原生指令。
//!
//! **动态路径**（M2.1）：
//! - 所有值都是装箱的 `object`：字面量先 `Box`，运算交给 `PyOps`；
//! - 比较返回 CIL `bool`，真值判定走 `PyOps.Truthy`；
//! - 动态值流入静态槽位时插入 `Unbox`（`PyCast.*`），底层类型不符抛 Python `TypeError`；
//! - `and` / `or` 仍用基本块短路，但返回的是**操作数本身**。
//!
//! 两条路径的公共部分（局部变量读取、确定赋值分析、基本块管理）只写一份。

use la_arena::Arena;
use rustc_hash::{FxHashMap, FxHashSet};
use ryupy_base::{Diagnostic, Span};
use ryupy_hir::{Builtin, ExprId, ExprKind, LocalId as HirLocalId, Module};
use ryupy_ty::{ModuleTypes, Ty, TypeckResults, type_ref_to_ty};

use crate::build::ty_to_mir;
use crate::mir::{
    BasicBlock, Callee, Constant, FnId, LocalDecl, LocalId as MirLocalId, MirType, Operand,
    RuntimeFn, Rvalue, Statement,
};

pub struct ExprBuilder<'a> {
    pub direct_globals_contracts: &'a mut Vec<crate::DirectGlobalsContract>,
    pub trace_expressions: Option<&'a mut Vec<crate::trace_lowering::ExpressionOrigin>>,
    pub module_state: Option<MirLocalId>,
    pub hir: &'a Module,
    pub types: &'a ModuleTypes,
    pub fn_map: &'a FxHashMap<ryupy_hir::FuncId, FnId>,
    pub fn_globals: &'a FxHashMap<ryupy_hir::FuncId, crate::mir::GlobalId>,
    pub fn_defaults: &'a FxHashMap<ryupy_hir::FuncId, Option<crate::mir::GlobalId>>,
    pub fn_sources: &'a FxHashMap<ryupy_hir::FuncId, crate::mir::GlobalId>,
    /// HIR 类 → 类对象静态槽位（普通类与 dataclass 类对象都从这里读取）。
    pub class_globals: &'a FxHashMap<ryupy_hir::ClassId, crate::mir::GlobalId>,
    /// HIR 模块变量 → 静态槽位。
    pub module_vars: &'a FxHashMap<ryupy_hir::ModuleVarId, crate::mir::GlobalId>,
    /// 全部静态槽位（读取槽位类型做装箱）。
    pub module_bound_flags: &'a FxHashMap<crate::mir::GlobalId, crate::mir::GlobalId>,
    pub globals: &'a la_arena::Arena<crate::mir::GlobalDecl>,
    pub current_hir_fn: ryupy_hir::FuncId,
    /// 闭包环境形参（M3.0）：`Free` 读取时从这里取外层 cell。
    pub env: Option<MirLocalId>,
    /// 每次函数调用独有的懒分配locals字典槽。
    pub function_locals_cache: &'a mut Option<MirLocalId>,
    /// 类体读取只查当前类的原始命名空间，不绑定描述符。
    pub class_namespace: Option<Operand>,
    pub lexical_namespace: bool,
    /// 类体创建函数时，显式捕获本次定义的隐式 cell，不读取类 namespace。
    pub class_cell: Option<(ryupy_hir::ClassId, Operand)>,
    pub lexical_cell_stack: Vec<(ryupy_hir::ClassId, Operand)>,
    pub locals: &'a mut Arena<LocalDecl>,
    pub blocks: &'a mut Arena<BasicBlock>,
    pub current_bb: &'a mut crate::mir::BlockId,
    pub local_map: &'a FxHashMap<HirLocalId, MirLocalId>,
    pub assigned_locals: &'a mut FxHashSet<HirLocalId>,
    pub reported_unbound: &'a mut FxHashSet<HirLocalId>,
    pub diagnostics: &'a mut Vec<Diagnostic>,
}

impl<'a> ExprBuilder<'a> {
    pub fn push_stmt(&mut self, stmt: Statement) {
        self.blocks[*self.current_bb].stmts.push(stmt);
    }

    pub fn alloc_temp(&mut self, ty: MirType) -> MirLocalId {
        self.locals.alloc(LocalDecl {
            ty,
            name: None,
            is_cell: false,
        })
    }

    /// 当前函数的闭包环境操作数；没有 env（非闭包函数）时报内部错误。
    pub(crate) fn env_operand(&mut self) -> Operand {
        match self.env {
            Some(local) => Operand::Local(local),
            None => {
                let range = self.hir.functions[self.current_hir_fn].range;
                self.diagnostics.push(
                    Diagnostic::error(
                        ryupy_base::Span::new(self.hir.file, range),
                        "内部错误：闭包函数缺少 env 形参",
                    )
                    .with_code("E0101")
                    .with_primary_label("free 变量分析与 MIR 构建不一致"),
                );
                Operand::Const(Constant::Null)
            }
        }
    }

    /// 当前函数的类型旁表；动态函数没有记录，所有查询都返回 `Dynamic`。
    fn body(&self) -> Option<&'a TypeckResults> {
        self.types.body(self.current_hir_fn)
    }

    /// 表达式类型。动态函数一律 `Dynamic`。
    pub fn ty_of(&self, id: ExprId) -> Ty {
        match self.body() {
            Some(b) => b.expr_ty(id),
            None => Ty::Dynamic,
        }
    }

    pub fn build_expr(&mut self, expr_id: ExprId) -> Operand {
        let result = self.build_expr_inner(expr_id);
        if let Some(origins) = self.trace_expressions.as_deref_mut() {
            origins.push(crate::trace_lowering::ExpressionOrigin {
                expression: expr_id,
                block: *self.current_bb,
                source_statement_cursor: self.blocks[*self.current_bb].stmts.len(),
                result: result.clone(),
            });
        }
        result
    }

    fn build_expr_inner(&mut self, expr_id: ExprId) -> Operand {
        let hir_body = &self.hir.functions[self.current_hir_fn].body;
        let expr = &hir_body.exprs[expr_id];
        match &expr.kind {
            ExprKind::ModuleMetadata(module, name) => self.build_module_metadata(module, name),
            ExprKind::ModuleFile(name) => {
                let dest = self.alloc_temp(MirType::Str);
                self.push_stmt(Statement::Assign {
                    dest,
                    value: Rvalue::Call {
                        callee: Callee::Runtime(RuntimeFn::ModuleFile),
                        args: vec![Operand::Const(Constant::Str(name.as_str().into()))],
                    },
                });
                if self.is_dynamic_context() {
                    self.coerce(Operand::Local(dest), &Ty::Str, &MirType::Dynamic)
                } else {
                    Operand::Local(dest)
                }
            }
            ExprKind::Literal(lit) => self.build_literal(lit),
            ExprKind::Yield(value) => self.build_yield_expr(*value),
            ExprKind::YieldFrom(value) => self.build_yield_from(*value),
            ExprKind::ModuleVar(var) => self.build_module_name(*var),
            ExprKind::Local(loc) => {
                let mir_local = self.local_map[loc];
                // cell 变量（M3.0）：槽位里是 `object[1]`，读取经过运行时
                // 未赋值检查（抛 UnboundLocalError）。
                if self.locals[mir_local].is_cell {
                    let var_name = hir_body.locals[*loc].name.to_string();
                    return self.runtime_call(
                        RuntimeFn::CellGet,
                        vec![
                            Operand::Local(mir_local),
                            Operand::Const(Constant::Str(var_name)),
                        ],
                        MirType::Dynamic,
                    );
                }
                // 确定赋值分析（语义陷阱 #10）：
                // - 动态槽位无法用 CLR 默认值区分「未赋值」与「值为 0/None」，
                //   所以对「可能未赋值」的读取插入运行时检查，抛 UnboundLocalError；
                // - 静态槽位（i64/f64）没有额外表示空间，仍作为编译期错误报出。
                if !self.assigned_locals.contains(loc) {
                    let var_name = hir_body.locals[*loc].name.to_string();
                    // 只有动态函数才走运行时检查：静态函数的槽位是 i64/f64，
                    // 类型旁表认为它是静态类型，插入动态检查会让后续运算错误地走动态路径。
                    if self.is_dynamic_context()
                        && matches!(self.locals[mir_local].ty, MirType::Dynamic)
                    {
                        let checked = self.runtime_call(
                            RuntimeFn::LoadChecked,
                            vec![
                                Operand::Local(mir_local),
                                Operand::Const(Constant::Str(var_name)),
                            ],
                            MirType::Dynamic,
                        );
                        return checked;
                    }
                    if !self.reported_unbound.contains(loc) {
                        self.diagnostics.push(
                            Diagnostic::error(
                                Span::new(self.hir.file, expr.range),
                                format!("局部变量 `{var_name}` 在赋值前被读取"),
                            )
                            .with_code("E0301")
                            .with_primary_label("静态槽位无法表示「未赋值」"),
                        );
                        self.reported_unbound.insert(*loc);
                    }
                }
                Operand::Local(mir_local)
            }
            ExprKind::Binary {
                op,
                lhs,
                rhs,
                inplace,
            } => self.build_binary(*op, *lhs, *rhs, *inplace, expr_id),
            ExprKind::Unary { op, operand } => self.build_unary(*op, *operand, expr_id),
            ExprKind::Compare { op, lhs, rhs } => self.build_compare(*op, *lhs, *rhs, expr_id),
            ExprKind::BoolOp { op, lhs, rhs } => self.build_bool_op(*op, *lhs, *rhs, expr_id),
            ExprKind::IfExp { cond, then_, else_ } => {
                self.build_if_exp(*cond, *then_, *else_, expr_id)
            }
            ExprKind::Call { callee, args } => self.build_call(*callee, args, expr_id),
            ExprKind::PreparedCall { callee, args } => {
                self.build_prepared_call(*callee, args, expr_id)
            }
            ExprKind::Attribute { base, name } => {
                let base_op = self.build_expr(*base);
                let base_ty = self.ty_of(*base);
                // 静态字段访问要求操作数是局部变量（codegen 从这里读字段类型）；
                // 模块变量是静态槽位，先搬到临时局部。
                let base_op = if matches!(base_ty, Ty::Class(_)) {
                    self.as_local(base_op)
                } else {
                    base_op
                };
                if let Ty::Class(cls_id) = base_ty {
                    let cls = &self.hir.classes[cls_id];
                    if let Some((field_idx, field)) =
                        cls.fields.iter().enumerate().find(|(_, f)| f.name == *name)
                    {
                        let field_ty = type_ref_to_ty(&field.annotation);
                        let dest = self.alloc_temp(ty_to_mir(&field_ty));
                        self.push_stmt(Statement::Assign {
                            dest,
                            value: Rvalue::GetField {
                                obj: base_op,
                                field: field_idx as u32,
                            },
                        });
                        return Operand::Local(dest);
                    }
                }
                if base_ty.is_dynamic() || matches!(base_ty, Ty::Error) {
                    // 动态属性读取：M2.2 覆盖异常对象与类型对象（args / __cause__ / __name__），
                    // 普通实例属性在 M2.5 由同一个入口扩展。
                    return self.call_runtime_attr(base_op, name.as_str());
                }
                self.diagnostics.push(
                    Diagnostic::error(
                        Span::new(self.hir.file, expr.range),
                        "该类型的属性访问尚未支持（M2.5 普通 class）",
                    )
                    .with_code("E0303")
                    .with_primary_label("需要普通 class 与属性查找"),
                );
                self.none_value()
            }
            ExprKind::List(items) => self.build_sequence_literal(items, RuntimeFn::MakeList),
            ExprKind::Tuple(items) => self.build_sequence_literal(items, RuntimeFn::MakeTuple),
            ExprKind::Set(items) => self.build_sequence_literal(items, RuntimeFn::MakeSet),
            ExprKind::FString(parts) => self.build_fstring(parts),
            ExprKind::Bytes(bytes) => {
                // 每个字节当成一个 latin-1 字符放进 CIL 字符串，运行时再还原成字节
                let text: String = bytes.iter().map(|b| *b as char).collect();
                let dest = self.alloc_temp(MirType::Dynamic);
                self.push_stmt(Statement::Assign {
                    dest,
                    value: Rvalue::Call {
                        callee: Callee::Runtime(RuntimeFn::MakeBytes),
                        args: vec![Operand::Const(Constant::Str(text))],
                    },
                });
                Operand::Local(dest)
            }
            ExprKind::Dict(pairs) => self.build_dict_literal(pairs, expr_id),
            ExprKind::Subscript { base, index } => {
                let base_op = self.build_expr(*base);
                let index_op = self.build_expr(*index);
                let dest = self.alloc_temp(MirType::Dynamic);
                self.push_stmt(Statement::Assign {
                    dest,
                    value: Rvalue::Call {
                        callee: Callee::Runtime(RuntimeFn::GetItem),
                        args: vec![base_op, index_op],
                    },
                });
                Operand::Local(dest)
            }
            ExprKind::Slice { start, stop, step } => {
                let part = |this: &mut Self, e: Option<ExprId>| match e {
                    Some(id) => this.build_expr(id),
                    None => this.runtime_call(RuntimeFn::DynNone, Vec::new(), MirType::Dynamic),
                };
                let start_op = part(self, *start);
                let stop_op = part(self, *stop);
                let step_op = part(self, *step);
                let dest = self.alloc_temp(MirType::Dynamic);
                self.push_stmt(Statement::Assign {
                    dest,
                    value: Rvalue::Call {
                        callee: Callee::Runtime(RuntimeFn::MakeSlice),
                        args: vec![start_op, stop_op, step_op],
                    },
                });
                Operand::Local(dest)
            }
            // 内建构造名作为值（`isinstance(x, int)`、`f = str`）：构造类型对象
            ExprKind::ClassCellRead(class) => self.build_lexical_cell_read(*class, expr.range),
            ExprKind::ClassFreeLocal(local) => self.build_class_free_local(*local, expr.range),
            ExprKind::ClassLookup { name, fallback } => {
                self.build_class_lookup(name.as_str(), *fallback)
            }
            ExprKind::ClassLocal(name) => self.runtime_call(
                RuntimeFn::ClassNamespaceGet,
                vec![
                    self.class_namespace
                        .clone()
                        .unwrap_or(Operand::Const(Constant::Null)),
                    Operand::Const(Constant::Str(name.as_str().to_owned())),
                ],
                MirType::Dynamic,
            ),
            ExprKind::Builtin(builtin) => self.build_builtin_value(*builtin, expr_id),
            // 闭包自由变量（M3.0）：env[idx] 是外层 cell，读取带未赋值检查。
            ExprKind::Free(idx) => {
                let env = self.env_operand();
                let name = self.hir.functions[self.current_hir_fn].closure.free[*idx]
                    .as_str()
                    .to_owned();
                self.runtime_call(
                    RuntimeFn::CellGetFree,
                    vec![
                        env,
                        Operand::Const(Constant::I64(*idx as i64)),
                        Operand::Const(Constant::Str(name)),
                    ],
                    MirType::Dynamic,
                )
            }
            // lambda（M3.0）：在求值位置创建匿名函数对象。
            // 参数默认值此刻在外层作用域求值（每次执行 lambda 都重新求值）。
            ExprKind::Lambda(func_id) => {
                let env = self.build_closure_env(*func_id);
                let mut defaults = Vec::new();
                for param in &self.hir.functions[*func_id].params {
                    if let Some(expr) = param.default {
                        // 默认值表达式属于外层（当前）函数的 body。
                        let ty = self.ty_of(expr);
                        let op = self.build_expr(expr);
                        let boxed = self.coerce(op, &ty, &MirType::Dynamic);
                        let saved = self.alloc_temp(MirType::Dynamic);
                        self.push_stmt(Statement::Assign {
                            dest: saved,
                            value: Rvalue::Use(boxed),
                        });
                        defaults.push(Operand::Local(saved));
                    } else {
                        // 占位：binder 只按 HasDefault 读取对应下标。
                        defaults.push(Operand::Const(Constant::Null));
                    }
                }
                let module = self.capture_function_module();
                let instance_context = self.capture_definition_context();
                let dest = self.alloc_temp(MirType::Dynamic);
                self.push_stmt(Statement::Assign {
                    dest,
                    value: Rvalue::MakeFunction {
                        instance_context,
                        func: self.fn_map[func_id],
                        env,
                        defaults,
                    },
                });
                self.install_function_module(Operand::Local(dest), module);
                Operand::Local(dest)
            }
            // 函数名作为值：加载模块静态字段里的 PyFunction 对象
            ExprKind::Func(fn_id) => match self.fn_globals.get(fn_id).copied() {
                Some(global) => Operand::Global(global),
                None => self.none_value(),
            },
            // 类名作为值：加载类对象。`isinstance(x, Box)`、`Box.named` 都依赖它。
            ExprKind::Class(cls_id) => match self.class_globals.get(cls_id).copied() {
                Some(global) => Operand::Global(global),
                None => self.none_value(),
            },
            // 星号只在容器字面量与赋值目标里有意义；这里落到容器的增量构建
            ExprKind::Starred(inner) => self.build_expr(*inner),
            _ => self.none_value(),
        }
    }

    /// f-string：逐段格式化成字符串，再用 `StrConcat` 拼接。
    ///
    /// 动态函数里结果是装箱的 `object`（与 `str()` 的处理一致），否则是一个 `Str` 临时量。
    fn build_fstring(&mut self, parts: &[ryupy_hir::FStringPart]) -> Operand {
        use ryupy_hir::FStringPart;

        let mut acc: Option<Operand> = None;
        for part in parts {
            let piece = match part {
                FStringPart::Literal(text) => {
                    Operand::Const(Constant::Str(text.as_ref().to_owned()))
                }
                FStringPart::Value {
                    value,
                    conversion,
                    spec,
                } => {
                    let op = self.build_expr(*value);
                    let ty = self.ty_of(*value);
                    let boxed = self.coerce(op, &ty, &MirType::Dynamic);
                    self.runtime_call(
                        RuntimeFn::FormatValue,
                        vec![
                            boxed,
                            Operand::Const(Constant::Str(spec.as_deref().unwrap_or("").to_owned())),
                            Operand::Const(Constant::I64(*conversion as i64)),
                        ],
                        MirType::Str,
                    )
                }
            };
            acc = Some(match acc {
                None => piece,
                Some(previous) => {
                    self.call_runtime_binary(RuntimeFn::StrConcat, previous, piece, MirType::Str)
                }
            });
        }
        let result = acc.unwrap_or(Operand::Const(Constant::Str(String::new())));
        if self.is_dynamic_context() {
            let dest = self.alloc_temp(MirType::Dynamic);
            self.push_stmt(Statement::Assign {
                dest,
                value: Rvalue::Use(result),
            });
            Operand::Local(dest)
        } else {
            result
        }
    }

    /// 默认域内建名称的真实冷读取；实例模块与源码遮蔽不经过这个入口。
    pub(crate) fn default_builtin_value(&mut self, name: &str) -> Operand {
        self.runtime_call(
            RuntimeFn::LoadDefaultBuiltin,
            vec![Operand::Const(Constant::Str(name.to_owned()))],
            MirType::Dynamic,
        )
    }

    /// 内建名字作为**值**使用时的结果。
    ///
    /// 构造函数（`int` / `str` / `list` …）在 Python 里是类型对象，`isinstance`
    /// 和赋值都依赖它。真正调用走 `build_call`，不会到这里。
    fn build_builtin_value(&mut self, builtin: Builtin, _expression: ExprId) -> Operand {
        let name = match builtin {
            // 本切仅迁移KeyError，保保存别名的真实绑定；不扩大其它异常的构造合同。
            Builtin::Exception("KeyError") => return self.default_builtin_value("KeyError"),
            Builtin::Exception("SyntaxError") => return self.default_builtin_value("SyntaxError"),
            // 异常类作为值：`isinstance(e, Exception)` / `raise E` 等。
            Builtin::Exception(type_name) => type_name,
            Builtin::Int => "int",
            Builtin::Float => "float",
            Builtin::Str => "str",
            Builtin::Type => "type",
            Builtin::Dynamic("object") => "object",
            Builtin::Dynamic("list") => "list",
            Builtin::Dynamic("tuple") => "tuple",
            Builtin::Dynamic("dict") => "dict",
            Builtin::Dynamic("set") => "set",
            Builtin::Dynamic("frozenset") => "frozenset",
            Builtin::Dynamic("bytes") => "bytes",
            Builtin::Dynamic("bytearray") => "bytearray",
            Builtin::Dynamic("slice") => "slice",
            Builtin::Dynamic("bool") => "bool",
            Builtin::Print => return self.builtin_function_value("print"),
            Builtin::Abs => return self.builtin_function_value("abs"),
            Builtin::Dynamic(name) => return self.builtin_function_value(name),
        };
        self.runtime_call(
            RuntimeFn::TypeObject,
            vec![Operand::Const(Constant::Str(name.to_owned()))],
            MirType::Dynamic,
        )
    }

    fn builtin_function_value(&mut self, name: &str) -> Operand {
        self.runtime_call(
            RuntimeFn::BuiltinFunction,
            vec![Operand::Const(Constant::Str(name.to_owned()))],
            MirType::Dynamic,
        )
    }

    /// 生成一次带数组实参的运行时调用。
    pub(crate) fn call_with_args(
        &mut self,
        callee: RuntimeFn,
        prefix: Vec<Operand>,
        args: Vec<Operand>,
    ) -> Operand {
        let dest = self.alloc_temp(MirType::Dynamic);
        self.push_stmt(Statement::Assign {
            dest,
            value: Rvalue::CallWithArgs {
                callee,
                prefix,
                args,
            },
        });
        Operand::Local(dest)
    }

    // ───────────────────────── 字面量 ─────────────────────────
}
