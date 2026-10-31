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
use ryupy_hir::{
    BinOp as HirBinOp, Builtin, CmpOp, ExprId, ExprKind, Literal, LocalId as HirLocalId, Module,
    UnaryOp as HirUnaryOp,
};
use ryupy_ty::{ModuleTypes, Ty, TypeckResults, type_ref_to_ty};

use crate::build::ty_to_mir;
use crate::mir::{
    BasicBlock, BinOp, Callee, CastKind, Constant, DynOp, FnId, LocalDecl, LocalId as MirLocalId,
    MirType, Operand, RuntimeFn, Rvalue, Statement, UnaryOp,
};

pub struct ExprBuilder<'a> {
    pub hir: &'a Module,
    pub types: &'a ModuleTypes,
    pub fn_map: &'a FxHashMap<ryupy_hir::FuncId, FnId>,
    pub fn_globals: &'a FxHashMap<ryupy_hir::FuncId, crate::mir::GlobalId>,
    pub fn_defaults: &'a FxHashMap<ryupy_hir::FuncId, Option<crate::mir::GlobalId>>,
    pub current_hir_fn: ryupy_hir::FuncId,
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
        self.locals.alloc(LocalDecl { ty, name: None })
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
        let hir_body = &self.hir.functions[self.current_hir_fn].body;
        let expr = &hir_body.exprs[expr_id];
        match &expr.kind {
            ExprKind::Literal(lit) => self.build_literal(lit),
            ExprKind::Local(loc) => {
                // 确定赋值分析（语义陷阱 #10）：
                // - 动态槽位无法用 CLR 默认值区分「未赋值」与「值为 0/None」，
                //   所以对「可能未赋值」的读取插入运行时检查，抛 UnboundLocalError；
                // - 静态槽位（i64/f64）没有额外表示空间，仍作为编译期错误报出。
                let mir_local = self.local_map[loc];
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
            ExprKind::Binary { op, lhs, rhs } => self.build_binary(*op, *lhs, *rhs, expr_id),
            ExprKind::Unary { op, operand } => self.build_unary(*op, *operand, expr_id),
            ExprKind::Compare { op, lhs, rhs } => self.build_compare(*op, *lhs, *rhs, expr_id),
            ExprKind::BoolOp { op, lhs, rhs } => self.build_bool_op(*op, *lhs, *rhs, expr_id),
            ExprKind::IfExp { cond, then_, else_ } => {
                self.build_if_exp(*cond, *then_, *else_, expr_id)
            }
            ExprKind::Call { callee, args } => self.build_call(*callee, args, expr_id),
            ExprKind::Attribute { base, name } => {
                let base_op = self.build_expr(*base);
                let base_ty = self.ty_of(*base);
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
            ExprKind::Builtin(builtin) => self.build_builtin_value(*builtin),
            // 函数名作为值：加载模块静态字段里的 PyFunction 对象
            ExprKind::Func(fn_id) => match self.fn_globals.get(fn_id).copied() {
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

    /// 内建名字作为**值**使用时的结果。
    ///
    /// 构造函数（`int` / `str` / `list` …）在 Python 里是类型对象，`isinstance`
    /// 和赋值都依赖它。真正调用走 `build_call`，不会到这里。
    fn build_builtin_value(&mut self, builtin: Builtin) -> Operand {
        let name = match builtin {
            Builtin::Int => "int",
            Builtin::Float => "float",
            Builtin::Str => "str",
            Builtin::Type => "type",
            Builtin::Dynamic("list") => "list",
            Builtin::Dynamic("tuple") => "tuple",
            Builtin::Dynamic("dict") => "dict",
            Builtin::Dynamic("set") => "set",
            Builtin::Dynamic("frozenset") => "frozenset",
            Builtin::Dynamic("bytes") => "bytes",
            Builtin::Dynamic("bytearray") => "bytearray",
            Builtin::Dynamic("bool") => "bool",
            _ => return self.none_value(),
        };
        self.runtime_call(
            RuntimeFn::TypeObject,
            vec![Operand::Const(Constant::Str(name.to_owned()))],
            MirType::Dynamic,
        )
    }

    /// 容器字面量：没有星号时一次构造；有星号时先建空容器再逐个追加。
    fn build_sequence_literal(&mut self, items: &[ExprId], callee: RuntimeFn) -> Operand {
        let has_star = items.iter().any(|id| {
            matches!(
                self.hir.functions[self.current_hir_fn].body.exprs[*id].kind,
                ExprKind::Starred(_)
            )
        });
        if !has_star {
            let mut args = Vec::new();
            for &item in items {
                let op = self.build_expr(item);
                let ty = self.ty_of(item);
                args.push(self.coerce(op, &ty, &MirType::Dynamic));
            }
            return self.call_with_args(callee, vec![], args);
        }

        // 含 `*x`：先建空列表，再 append / extend
        let container = self.call_with_args(RuntimeFn::MakeList, vec![], vec![]);
        for &item in items {
            let starred = match &self.hir.functions[self.current_hir_fn].body.exprs[item].kind {
                ExprKind::Starred(inner) => Some(*inner),
                _ => None,
            };
            match starred {
                Some(inner) => {
                    let value = self.build_expr(inner);
                    let ty = self.ty_of(inner);
                    let boxed = self.coerce(value, &ty, &MirType::Dynamic);
                    self.push_stmt(Statement::Eval(Rvalue::Call {
                        callee: Callee::Runtime(RuntimeFn::ListExtend),
                        args: vec![container.clone(), boxed],
                    }));
                }
                None => {
                    let value = self.build_expr(item);
                    let ty = self.ty_of(item);
                    let boxed = self.coerce(value, &ty, &MirType::Dynamic);
                    self.push_stmt(Statement::Eval(Rvalue::Call {
                        callee: Callee::Runtime(RuntimeFn::ListAppend),
                        args: vec![container.clone(), boxed],
                    }));
                }
            }
        }
        container
    }

    /// 字典字面量：空字典 + 逐项写入。
    fn build_dict_literal(&mut self, pairs: &[(ExprId, ExprId)], _expr_id: ExprId) -> Operand {
        // MakeDict 没有数组形参，走普通调用
        let dict_dest = self.alloc_temp(MirType::Dynamic);
        self.push_stmt(Statement::Assign {
            dest: dict_dest,
            value: Rvalue::Call {
                callee: Callee::Runtime(RuntimeFn::MakeDict),
                args: Vec::new(),
            },
        });
        let dict = Operand::Local(dict_dest);
        for &(key, value) in pairs {
            let key_op = self.build_expr(key);
            let key_ty = self.ty_of(key);
            let key_boxed = self.coerce(key_op, &key_ty, &MirType::Dynamic);
            let val_op = self.build_expr(value);
            let val_ty = self.ty_of(value);
            let val_boxed = self.coerce(val_op, &val_ty, &MirType::Dynamic);
            self.push_stmt(Statement::Eval(Rvalue::Call {
                callee: Callee::Runtime(RuntimeFn::SetItem),
                args: vec![dict.clone(), key_boxed, val_boxed],
            }));
        }
        dict
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

    fn build_literal(&mut self, lit: &Literal) -> Operand {
        match lit {
            Literal::Int(v) => {
                let Ok(small) = i64::try_from(*v) else {
                    // 超出 i64：交给运行时构造 BigInteger
                    let text = v.to_string();
                    return self.runtime_call(
                        RuntimeFn::DynFromDecimal,
                        vec![Operand::Const(Constant::Str(text))],
                        MirType::Dynamic,
                    );
                };
                if self.is_dynamic_context() {
                    self.boxed_const(Constant::I64(small))
                } else {
                    Operand::Const(Constant::I64(small))
                }
            }
            Literal::Float(v) => {
                if self.is_dynamic_context() {
                    self.boxed_const(Constant::F64(*v))
                } else {
                    Operand::Const(Constant::F64(*v))
                }
            }
            Literal::Bool(b) => {
                if self.is_dynamic_context() {
                    self.boxed_const(Constant::Bool(*b))
                } else {
                    Operand::Const(Constant::Bool(*b))
                }
            }
            Literal::Str(s) => {
                if self.is_dynamic_context() {
                    self.boxed_const(Constant::Str(s.to_string()))
                } else {
                    Operand::Const(Constant::Str(s.to_string()))
                }
            }
            Literal::None => {
                // None 在所有路径上都用运行时的 None 单例（装箱 object）表示：
                // 静态路径没有 `Unit` 值，占位 0 会把 `print(None)` 写成 0，
                // 也会让 `x is None` 判错。
                self.none_value()
            }
        }
    }

    /// 当前函数是否走动态路径。
    pub fn is_dynamic_context(&self) -> bool {
        self.types.is_dynamic(self.current_hir_fn)
    }

    /// Python `None` 的值：运行时的 `None` 单例（装箱 `object`）。
    ///
    /// 静态路径没有单独的 `None` 类型，但语义上它必须与动态路径一致——否则
    /// `print(None)`、`x is None`、把 `None` 传进容器都会得到错误结果。
    pub(crate) fn none_value(&mut self) -> Operand {
        self.runtime_call(RuntimeFn::DynNone, Vec::new(), MirType::Dynamic)
    }

    /// 把一个常量装箱成动态值。
    fn boxed_const(&mut self, c: Constant) -> Operand {
        let from = match &c {
            Constant::I64(_) | Constant::BigInt(_) => MirType::I64,
            Constant::F64(_) => MirType::F64,
            Constant::Bool(_) => MirType::Bool,
            Constant::Str(_) => MirType::Str,
            Constant::None | Constant::Null => {
                return self.runtime_call(RuntimeFn::DynNone, Vec::new(), MirType::Dynamic);
            }
        };
        let dest = self.alloc_temp(MirType::Dynamic);
        self.push_stmt(Statement::Assign {
            dest,
            value: Rvalue::Box {
                value: Operand::Const(c),
                from,
            },
        });
        Operand::Local(dest)
    }

    // ───────────────────────── 装箱 / 拆箱 ─────────────────────────

    /// 把 `op`（类型 `from`）转成目标表示 `to`。
    pub fn coerce(&mut self, op: Operand, from: &Ty, to: &MirType) -> Operand {
        // 目标是 `Unit`（无值）：只需要一个可丢弃的占位。若继续往下走会生成
        // `Unbox to Unit` 这种没有 CIL 含义的指令，或者给 Unit 分配局部变量。
        if matches!(to, MirType::Unit) {
            return self.none_value();
        }
        // `Ty::None` 的操作数已经是运行时的 None 单例（装箱 object），
        // 因此按动态值处理；否则会插入一次 `Box from Unit` 的非法装箱指令。
        let from_dynamic = from.is_dynamic() || matches!(from, Ty::None);
        let to_dynamic = matches!(to, MirType::Dynamic);
        match (from_dynamic, to_dynamic) {
            (true, true) | (false, false) => op,
            (false, true) => {
                let dest = self.alloc_temp(MirType::Dynamic);
                self.push_stmt(Statement::Assign {
                    dest,
                    value: Rvalue::Box {
                        value: op,
                        from: ty_to_mir(from),
                    },
                });
                Operand::Local(dest)
            }
            (true, false) => {
                let dest = self.alloc_temp(to.clone());
                self.push_stmt(Statement::Assign {
                    dest,
                    value: Rvalue::Unbox {
                        value: op,
                        to: to.clone(),
                    },
                });
                Operand::Local(dest)
            }
        }
    }

    /// 生成一次运行时调用并把结果存进临时变量。
    pub(crate) fn runtime_call(
        &mut self,
        rt_fn: RuntimeFn,
        args: Vec<Operand>,
        ret: MirType,
    ) -> Operand {
        let dest = self.alloc_temp(ret);
        self.push_stmt(Statement::Assign {
            dest,
            value: Rvalue::Call {
                callee: Callee::Runtime(rt_fn),
                args,
            },
        });
        Operand::Local(dest)
    }

    /// 动态二元运算。
    fn dyn_binary(&mut self, op: DynOp, lhs: Operand, rhs: Operand) -> Operand {
        let dest = self.alloc_temp(MirType::Dynamic);
        self.push_stmt(Statement::Assign {
            dest,
            value: Rvalue::DynBinary { op, lhs, rhs },
        });
        Operand::Local(dest)
    }

    // ───────────────────────── 运算 ─────────────────────────

    fn build_binary(&mut self, op: HirBinOp, lhs: ExprId, rhs: ExprId, expr_id: ExprId) -> Operand {
        let mut lhs_op = self.build_expr(lhs);
        let mut rhs_op = self.build_expr(rhs);

        let lhs_ty = self.ty_of(lhs);
        let rhs_ty = self.ty_of(rhs);
        let expr_ty = self.ty_of(expr_id);

        if expr_ty.is_dynamic() {
            // 动态路径：操作数此时都是装箱值，直接交给运行时
            lhs_op = self.coerce(lhs_op, &lhs_ty, &MirType::Dynamic);
            rhs_op = self.coerce(rhs_op, &rhs_ty, &MirType::Dynamic);
            return self.dyn_binary(dyn_bin_op(op), lhs_op, rhs_op);
        }

        // 隐式提升：若两端类型分别为 int 和 float，将 int 一端提升为 float
        if lhs_ty == Ty::Int && rhs_ty == Ty::Float {
            lhs_op = self.cast_i64_to_f64(lhs_op);
        } else if lhs_ty == Ty::Float && rhs_ty == Ty::Int {
            rhs_op = self.cast_i64_to_f64(rhs_op);
        }

        // 字符串拼接
        if op == HirBinOp::Add && lhs_ty == Ty::Str && rhs_ty == Ty::Str {
            return self.call_runtime_binary(RuntimeFn::StrConcat, lhs_op, rhs_op, MirType::Str);
        }

        // Python 语义改写：向负无穷取整除法、取模、乘方等落入 RuntimeFn
        match op {
            HirBinOp::FloorDiv => {
                let rt_fn = if expr_ty == Ty::Float {
                    RuntimeFn::FloorDivF64
                } else {
                    RuntimeFn::FloorDivI64
                };
                self.call_runtime_binary(rt_fn, lhs_op, rhs_op, ty_to_mir(&expr_ty))
            }
            HirBinOp::Mod => {
                let rt_fn = if expr_ty == Ty::Float {
                    RuntimeFn::ModF64
                } else {
                    RuntimeFn::ModI64
                };
                self.call_runtime_binary(rt_fn, lhs_op, rhs_op, ty_to_mir(&expr_ty))
            }
            HirBinOp::Pow => {
                let rt_fn = if expr_ty == Ty::Float {
                    RuntimeFn::PowF64
                } else {
                    RuntimeFn::PowI64
                };
                self.call_runtime_binary(rt_fn, lhs_op, rhs_op, ty_to_mir(&expr_ty))
            }
            HirBinOp::TrueDiv => {
                if lhs_ty == Ty::Int {
                    lhs_op = self.cast_i64_to_f64(lhs_op);
                }
                if rhs_ty == Ty::Int {
                    rhs_op = self.cast_i64_to_f64(rhs_op);
                }
                // 除数为 0 时 Python 抛 ZeroDivisionError，CIL 的 `div` 只会得到 inf/NaN，
                // 因此真除法统一走运行时检查（语义陷阱 #12）。
                self.call_runtime_binary(RuntimeFn::TrueDivF64, lhs_op, rhs_op, MirType::F64)
            }
            HirBinOp::LShift => {
                // CIL 的 shl 把移位量按 63 取模（1 << 100 会算成 1 << 36）；
                // 运行时入口负责「溢出报 OverflowError」与正确的大移位语义
                self.call_runtime_binary(RuntimeFn::ShlI64, lhs_op, rhs_op, MirType::I64)
            }
            HirBinOp::RShift => {
                // 移位量 ≥ 64 时 Python 的结果是 0 / -1，CIL 的 shr 同样会取模
                self.call_runtime_binary(RuntimeFn::ShrI64, lhs_op, rhs_op, MirType::I64)
            }
            _ => {
                let mir_op = match op {
                    HirBinOp::Add => BinOp::Add,
                    HirBinOp::Sub => BinOp::Sub,
                    HirBinOp::Mul => BinOp::Mul,
                    HirBinOp::BitAnd => BinOp::BitAnd,
                    HirBinOp::BitOr => BinOp::BitOr,
                    HirBinOp::BitXor => BinOp::BitXor,
                    // 移位已在上面改写成运行时调用：CIL 指令的移位量会取模
                    _ => BinOp::Add,
                };
                let dest = self.alloc_temp(ty_to_mir(&expr_ty));
                self.push_stmt(Statement::Assign {
                    dest,
                    value: Rvalue::Binary {
                        op: mir_op,
                        lhs: lhs_op,
                        rhs: rhs_op,
                    },
                });
                Operand::Local(dest)
            }
        }
    }

    /// `in` / `not in` / `is` / `is not`。
    fn build_membership_compare(&mut self, op: CmpOp, lhs: ExprId, rhs: ExprId) -> Operand {
        let lhs_op = self.build_expr(lhs);
        let lhs_ty = self.ty_of(lhs);
        let lhs_value = self.coerce(lhs_op, &lhs_ty, &MirType::Dynamic);
        let rhs_op = self.build_expr(rhs);
        let rhs_ty = self.ty_of(rhs);
        let rhs_value = self.coerce(rhs_op, &rhs_ty, &MirType::Dynamic);

        let negate = matches!(op, CmpOp::NotIn | CmpOp::IsNot);
        let runtime = match op {
            CmpOp::Is | CmpOp::IsNot => RuntimeFn::IsSame,
            _ => RuntimeFn::Contains,
        };
        // `in` 的语义是 `x in container`，运行时签名是 (container, value)
        let (first, second) = match op {
            CmpOp::Is | CmpOp::IsNot => (lhs_value, rhs_value),
            _ => (rhs_value, lhs_value),
        };

        let raw = self.runtime_call(runtime, vec![first, second], MirType::Bool);
        let value = if negate {
            let dest = self.alloc_temp(MirType::Bool);
            self.push_stmt(Statement::Assign {
                dest,
                value: Rvalue::Unary {
                    op: crate::mir::UnaryOp::Not,
                    operand: raw,
                },
            });
            Operand::Local(dest)
        } else {
            raw
        };
        self.bool_to_value(value)
    }

    /// 把 CIL `bool` 装成动态值；静态上下文原样返回。
    fn bool_to_value(&mut self, op: Operand) -> Operand {
        if !self.is_dynamic_context() {
            return op;
        }
        let dest = self.alloc_temp(MirType::Dynamic);
        self.push_stmt(Statement::Assign {
            dest,
            value: Rvalue::Box {
                value: op,
                from: MirType::Bool,
            },
        });
        Operand::Local(dest)
    }

    fn cast_i64_to_f64(&mut self, op: Operand) -> Operand {
        let dest = self.alloc_temp(MirType::F64);
        self.push_stmt(Statement::Assign {
            dest,
            value: Rvalue::Cast {
                kind: CastKind::I64ToF64,
                operand: op,
            },
        });
        Operand::Local(dest)
    }

    fn build_unary(&mut self, op: HirUnaryOp, operand: ExprId, expr_id: ExprId) -> Operand {
        let op_val = self.build_expr(operand);
        let operand_ty = self.ty_of(operand);
        let expr_ty = self.ty_of(expr_id);

        if expr_ty.is_dynamic() || operand_ty.is_dynamic() {
            let value = self.coerce(op_val, &operand_ty, &MirType::Dynamic);
            if op == HirUnaryOp::Not {
                // `not x` 的结果是 CIL bool，需要时再按上下文装箱
                let cond = self.runtime_call(RuntimeFn::DynTruthy, vec![value], MirType::Bool);
                let dest = self.alloc_temp(MirType::Bool);
                self.push_stmt(Statement::Assign {
                    dest,
                    value: Rvalue::Unary {
                        op: UnaryOp::Not,
                        operand: cond,
                    },
                });
                return self.bool_to_value(Operand::Local(dest));
            }
            let dest = self.alloc_temp(MirType::Dynamic);
            self.push_stmt(Statement::Assign {
                dest,
                value: Rvalue::DynUnary {
                    op: dyn_unary_op(op),
                    operand: value,
                },
            });
            return Operand::Local(dest);
        }

        // 简单常量折叠：负整数与负浮点
        if op == HirUnaryOp::Neg {
            if let Operand::Const(Constant::I64(v)) = op_val {
                return Operand::Const(Constant::I64(-v));
            }
            if let Operand::Const(Constant::F64(v)) = op_val {
                return Operand::Const(Constant::F64(-v));
            }
        }
        let mir_op = match op {
            HirUnaryOp::Neg => UnaryOp::Neg,
            HirUnaryOp::Not => UnaryOp::Not,
            HirUnaryOp::Invert => UnaryOp::BitNot,
            HirUnaryOp::Pos => return op_val,
        };
        let dest = self.alloc_temp(ty_to_mir(&expr_ty));
        self.push_stmt(Statement::Assign {
            dest,
            value: Rvalue::Unary {
                op: mir_op,
                operand: op_val,
            },
        });
        Operand::Local(dest)
    }

    fn build_compare(&mut self, op: CmpOp, lhs: ExprId, rhs: ExprId, expr_id: ExprId) -> Operand {
        // `in` / `not in` / `is` / `is not`：走运行时容器协议与身份比较
        if matches!(op, CmpOp::In | CmpOp::NotIn | CmpOp::Is | CmpOp::IsNot) {
            return self.build_membership_compare(op, lhs, rhs);
        }
        let mut lhs_op = self.build_expr(lhs);
        let mut rhs_op = self.build_expr(rhs);
        let lhs_ty = self.ty_of(lhs);
        let rhs_ty = self.ty_of(rhs);

        if self.ty_of(expr_id).is_dynamic() {
            lhs_op = self.coerce(lhs_op, &lhs_ty, &MirType::Dynamic);
            rhs_op = self.coerce(rhs_op, &rhs_ty, &MirType::Dynamic);
            let dest = self.alloc_temp(MirType::Bool);
            self.push_stmt(Statement::Assign {
                dest,
                value: Rvalue::DynCompare {
                    op: dyn_cmp_op(op),
                    lhs: lhs_op,
                    rhs: rhs_op,
                },
            });
            // 动态上下文里比较结果也是 object（Python 的 True/False 是一等值）
            return self.bool_to_value(Operand::Local(dest));
        }

        if lhs_ty == Ty::Int && rhs_ty == Ty::Float {
            lhs_op = self.cast_i64_to_f64(lhs_op);
        } else if lhs_ty == Ty::Float && rhs_ty == Ty::Int {
            rhs_op = self.cast_i64_to_f64(rhs_op);
        }

        let mir_op = match op {
            CmpOp::Eq => BinOp::Eq,
            CmpOp::NotEq => BinOp::Ne,
            CmpOp::Lt => BinOp::Lt,
            CmpOp::LtE => BinOp::Le,
            CmpOp::Gt => BinOp::Gt,
            CmpOp::GtE => BinOp::Ge,
            // 上面已提前返回，这里只是让 match 完整
            CmpOp::In | CmpOp::NotIn | CmpOp::Is | CmpOp::IsNot => BinOp::Eq,
        };

        let dest = self.alloc_temp(MirType::Bool);
        self.push_stmt(Statement::Assign {
            dest,
            value: Rvalue::Binary {
                op: mir_op,
                lhs: lhs_op,
                rhs: rhs_op,
            },
        });
        Operand::Local(dest)
    }

    fn call_runtime_binary(
        &mut self,
        rt_fn: RuntimeFn,
        lhs: Operand,
        rhs: Operand,
        ret_ty: MirType,
    ) -> Operand {
        self.runtime_call(rt_fn, vec![lhs, rhs], ret_ty)
    }

    pub fn new_block(&mut self) -> crate::mir::BlockId {
        self.blocks.alloc(BasicBlock {
            stmts: Vec::new(),
            terminator: crate::mir::Terminator::Unreachable,
        })
    }

    /// 把任意类型的值转成 CIL `bool`（分支条件用）。
    pub fn to_bool_operand(&mut self, op: Operand, ty: &Ty) -> Operand {
        match ty {
            Ty::Bool => op,
            Ty::Int => {
                let dest = self.alloc_temp(MirType::Bool);
                self.push_stmt(Statement::Assign {
                    dest,
                    value: Rvalue::Binary {
                        op: BinOp::Ne,
                        lhs: op,
                        rhs: Operand::Const(Constant::I64(0)),
                    },
                });
                Operand::Local(dest)
            }
            Ty::Float => {
                let dest = self.alloc_temp(MirType::Bool);
                self.push_stmt(Statement::Assign {
                    dest,
                    value: Rvalue::Binary {
                        op: BinOp::Ne,
                        lhs: op,
                        rhs: Operand::Const(Constant::F64(0.0)),
                    },
                });
                Operand::Local(dest)
            }
            Ty::Dynamic => self.runtime_call(RuntimeFn::DynTruthy, vec![op], MirType::Bool),
            // 其它静态类型在静态函数里已经降级，这里兜底按真值处理
            _ => {
                let boxed = self.coerce(op, ty, &MirType::Dynamic);
                self.runtime_call(RuntimeFn::DynTruthy, vec![boxed], MirType::Bool)
            }
        }
    }

    fn build_bool_op(
        &mut self,
        op: ryupy_hir::BoolOp,
        lhs: ExprId,
        rhs: ExprId,
        expr_id: ExprId,
    ) -> Operand {
        let expr_ty = self.ty_of(expr_id);
        let dest = self.alloc_temp(ty_to_mir(&expr_ty));

        let lhs_op = self.build_expr(lhs);
        let lhs_ty = self.ty_of(lhs);
        self.assign_value(dest, lhs_op, &lhs_ty);

        let cond_bool = self.to_bool_operand(Operand::Local(dest), &lhs_ty);

        let rhs_bb = self.new_block();
        let join_bb = self.new_block();

        match op {
            ryupy_hir::BoolOp::And => {
                self.blocks[*self.current_bb].terminator = crate::mir::Terminator::Branch {
                    cond: cond_bool,
                    then_: rhs_bb,
                    else_: join_bb,
                };
            }
            ryupy_hir::BoolOp::Or => {
                self.blocks[*self.current_bb].terminator = crate::mir::Terminator::Branch {
                    cond: cond_bool,
                    then_: join_bb,
                    else_: rhs_bb,
                };
            }
        }

        *self.current_bb = rhs_bb;
        let rhs_op = self.build_expr(rhs);
        let rhs_ty = self.ty_of(rhs);
        self.assign_value(dest, rhs_op, &rhs_ty);
        self.blocks[*self.current_bb].terminator = crate::mir::Terminator::Goto(join_bb);

        *self.current_bb = join_bb;
        Operand::Local(dest)
    }

    fn build_if_exp(
        &mut self,
        cond: ExprId,
        then_: ExprId,
        else_: ExprId,
        expr_id: ExprId,
    ) -> Operand {
        let expr_ty = self.ty_of(expr_id);
        let dest = self.alloc_temp(ty_to_mir(&expr_ty));

        let cond_raw = self.build_expr(cond);
        let cond_ty = self.ty_of(cond);
        let cond_bool = self.to_bool_operand(cond_raw, &cond_ty);

        let then_bb = self.new_block();
        let else_bb = self.new_block();
        let join_bb = self.new_block();

        self.blocks[*self.current_bb].terminator = crate::mir::Terminator::Branch {
            cond: cond_bool,
            then_: then_bb,
            else_: else_bb,
        };

        *self.current_bb = then_bb;
        let then_op = self.build_expr(then_);
        let then_ty = self.ty_of(then_);
        self.assign_value(dest, then_op, &then_ty);
        self.blocks[*self.current_bb].terminator = crate::mir::Terminator::Goto(join_bb);

        *self.current_bb = else_bb;
        let else_op = self.build_expr(else_);
        let else_ty = self.ty_of(else_);
        self.assign_value(dest, else_op, &else_ty);
        self.blocks[*self.current_bb].terminator = crate::mir::Terminator::Goto(join_bb);

        *self.current_bb = join_bb;
        Operand::Local(dest)
    }

    /// 动态属性读取：`PyObject.GetAttr(obj, "name")`。
    fn call_runtime_attr(&mut self, obj: Operand, name: &str) -> Operand {
        let dest = self.alloc_temp(MirType::Dynamic);
        self.push_stmt(Statement::Assign {
            dest,
            value: Rvalue::Call {
                callee: Callee::Runtime(RuntimeFn::DynGetAttr),
                args: vec![obj, Operand::Const(Constant::Str(name.to_string()))],
            },
        });
        Operand::Local(dest)
    }

    /// 把值写进目标局部变量，必要时插入装箱 / 拆箱。
    pub fn assign_value(&mut self, dest: MirLocalId, op: Operand, from: &Ty) {
        let target_ty = self.locals[dest].ty.clone();
        let coerced = self.coerce(op, from, &target_ty);
        self.push_stmt(Statement::Assign {
            dest,
            value: Rvalue::Use(coerced),
        });
    }
}

fn dyn_bin_op(op: HirBinOp) -> DynOp {
    match op {
        HirBinOp::Add => DynOp::Add,
        HirBinOp::Sub => DynOp::Sub,
        HirBinOp::Mul => DynOp::Mul,
        HirBinOp::TrueDiv => DynOp::TrueDiv,
        HirBinOp::FloorDiv => DynOp::FloorDiv,
        HirBinOp::Mod => DynOp::Mod,
        HirBinOp::Pow => DynOp::Pow,
        HirBinOp::BitAnd => DynOp::BitAnd,
        HirBinOp::BitOr => DynOp::BitOr,
        HirBinOp::BitXor => DynOp::BitXor,
        HirBinOp::LShift => DynOp::LShift,
        HirBinOp::RShift => DynOp::RShift,
    }
}

fn dyn_cmp_op(op: CmpOp) -> DynOp {
    match op {
        CmpOp::Eq => DynOp::Eq,
        CmpOp::NotEq => DynOp::Ne,
        CmpOp::Lt => DynOp::Lt,
        CmpOp::LtE => DynOp::Le,
        CmpOp::Gt => DynOp::Gt,
        CmpOp::GtE => DynOp::Ge,
        // 这三个走专用运行时调用，不会进入这里
        CmpOp::In | CmpOp::NotIn | CmpOp::Is | CmpOp::IsNot => DynOp::Eq,
    }
}

fn dyn_unary_op(op: HirUnaryOp) -> DynOp {
    match op {
        HirUnaryOp::Neg => DynOp::Neg,
        HirUnaryOp::Pos => DynOp::Pos,
        HirUnaryOp::Not => DynOp::Not,
        HirUnaryOp::Invert => DynOp::Invert,
    }
}
