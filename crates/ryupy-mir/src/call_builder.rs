//! 函数调用与构造器调用的 MIR 降级构建器。
//!
//! 调用分三条路径：
//!
//! 1. **直调**：被调对象静态解析为某个 `def`、实参里没有 `*` / `**`、且能按 Python 规则
//!    完整绑定。这时按参数顺序生成实参（缺省值从模块静态数组读取），直接 `call` 对应方法。
//!    静态特化与动态模式的函数都适用（后者的形参 / 返回值是装箱 `dyn`）。
//! 2. **运行时绑定**：动态函数、带 `*` / `**` 的调用、或绑定会失败（重复绑定、未知关键字、
//!    缺少必需参数）的调用。把实参与关键字打包成数组交给 `PyOps.CallValue`，由
//!    `PyBinder` 统一完成绑定并抛出与 CPython 一致的 `TypeError`。
//! 3. **内建 / 方法 / 类构造**：内建走 `PyOps.CallBuiltin`，方法走 `PyOps.CallMethod`，
//!    dataclass 构造按字段名绑定后 `newobj`。
//!
//! 为什么绑定错误不在编译期报：Python 的绑定失败是**运行时** `TypeError`，用户可以
//! `try/except` 捕获。编译期只做“能不能走快路径”的判断，绝不因此拒绝程序。

use ryupy_hir::{CallArg, ExprId, ExprKind, FuncId, ParamKind};
use ryupy_ty::{Ty, type_ref_to_ty};

use crate::build::ty_to_mir;
use crate::expr_builder::ExprBuilder;
use crate::mir::{Callee, Constant, MirType, Operand, RuntimeFn, Rvalue, Statement};

/// 一个实参在绑定计划中的去向。
#[derive(Clone, Copy, Debug)]
enum ArgSlot {
    /// 绑定到某个形参（具体下标记录在 `BindPlan::assigned`）。
    Param,
    /// 多出来的位置实参，进 `*args`。
    ExtraPositional,
    /// 多出来的关键字实参，进 `**kwargs`。
    ExtraKeyword,
}

/// 编译期绑定计划：每个实参去哪个形参，以及每个形参由哪个实参提供。
struct BindPlan {
    slots: Vec<ArgSlot>,
    /// 形参下标 → 提供它的实参下标。
    assigned: Vec<Option<usize>>,
}

impl ExprBuilder<'_> {
    pub(crate) fn build_call(
        &mut self,
        callee: ExprId,
        args: &[CallArg],
        call_expr: ExprId,
    ) -> Operand {
        let call_ty = self.ty_of(call_expr);
        let callee_kind = self.hir.functions[self.current_hir_fn].body.exprs[callee]
            .kind
            .clone();
        match callee_kind {
            ExprKind::Func(fn_id) => self.build_user_call(fn_id, args, &call_ty),
            ExprKind::Class(cls_id) => self.build_class_call(cls_id, args),
            ExprKind::Builtin(builtin) => self.build_builtin_call(builtin, args, &call_ty),
            ExprKind::Attribute { base, name } => {
                self.build_method_call(base, name.as_str().to_owned(), args, &call_ty)
            }
            _ => {
                let callee_op = self.build_expr(callee);
                let callee_ty = self.ty_of(callee);
                let callable = self.coerce(callee_op, &callee_ty, &MirType::Dynamic);
                let result = self.call_value(callable, args);
                self.coerce(result, &Ty::Dynamic, &ty_to_mir(&call_ty))
            }
        }
    }

    // ───────────────────────── 用户函数 ─────────────────────────

    fn build_user_call(&mut self, fn_id: FuncId, args: &[CallArg], call_ty: &Ty) -> Operand {
        let has_star = args
            .iter()
            .any(|arg| matches!(arg, CallArg::Starred(_) | CallArg::DoubleStarred(_)));
        // 动态函数同样可以直调：被调对象在 HIR 里已静态解析为同一个 `def`（与静态直调相同的
        // 名字绑定规则），参数与返回值在 MIR 签名里都是 `dyn`，实参装箱后直接 `call`，
        // 省掉实参数组、PIC、`PyFunction.Invoke` 与 thunk。只有 `*` / `**` 或编译期绑定
        // 不成立时才走运行时 binder。
        if has_star {
            let Some(global) = self.fn_globals.get(&fn_id).copied() else {
                return self.none_value();
            };
            let result = self.call_value(Operand::Global(global), args);
            return self.coerce(result, &Ty::Dynamic, &ty_to_mir(call_ty));
        }

        let Some(plan) = self.plan_binding(fn_id, args) else {
            // 绑定会失败：交给运行时，得到带准确消息的 TypeError
            let global = self.fn_globals[&fn_id];
            let result = self.call_value(Operand::Global(global), args);
            return self.coerce(result, &Ty::Dynamic, &ty_to_mir(call_ty));
        };

        let call_args = self.build_static_arguments(fn_id, args, &plan);
        let mir_fn = self.fn_map[&fn_id];
        let ret_ty = self.types.sigs[fn_id].ret.clone();
        let call = Rvalue::Call {
            callee: Callee::Fn(mir_fn),
            args: call_args,
        };
        if matches!(ret_ty, Ty::None) {
            // 返回 None 的函数是 void 方法：没有返回值可存
            self.push_stmt(Statement::Eval(call));
            return self.none_value();
        }
        let dest = self.alloc_temp(ty_to_mir(&ret_ty));
        self.push_stmt(Statement::Assign { dest, value: call });
        self.coerce(Operand::Local(dest), &ret_ty, &ty_to_mir(call_ty))
    }

    /// 纯编译期绑定：只读 HIR 元数据，不求值任何表达式。
    ///
    /// 返回 `None` 表示这组实参无法按 Python 规则绑定（过多 / 缺少 / 重复 / 未知关键字，
    /// 或位置-only 参数被按关键字传入），调用方应改走运行时 binder。
    fn plan_binding(&self, fn_id: FuncId, args: &[CallArg]) -> Option<BindPlan> {
        let func = &self.hir.functions[fn_id];
        let params = &func.params;
        let positional_slots: Vec<usize> = params
            .iter()
            .enumerate()
            .filter(|(_, param)| {
                matches!(
                    param.kind,
                    ParamKind::PositionalOnly | ParamKind::PositionalOrKeyword
                )
            })
            .map(|(index, _)| index)
            .collect();
        let var_positional = params
            .iter()
            .position(|param| param.kind == ParamKind::VarPositional);
        let var_keyword = params
            .iter()
            .position(|param| param.kind == ParamKind::VarKeyword);

        let mut assigned: Vec<Option<usize>> = vec![None; params.len()];
        let mut slots = Vec::with_capacity(args.len());
        let mut position = 0usize;

        for (arg_index, arg) in args.iter().enumerate() {
            match arg {
                CallArg::Positional(_) => {
                    if position < positional_slots.len() {
                        let param_index = positional_slots[position];
                        if assigned[param_index].is_some() {
                            return None;
                        }
                        assigned[param_index] = Some(arg_index);
                        slots.push(ArgSlot::Param);
                        position += 1;
                    } else if var_positional.is_some() {
                        slots.push(ArgSlot::ExtraPositional);
                    } else {
                        return None;
                    }
                }
                CallArg::Keyword(name, _) => {
                    let matched = params.iter().position(|param| {
                        matches!(
                            param.kind,
                            ParamKind::PositionalOrKeyword | ParamKind::KeywordOnly
                        ) && func.body.locals[param.local].name.as_str() == name.as_str()
                    });
                    if let Some(param_index) = matched {
                        if assigned[param_index].is_some() {
                            return None;
                        }
                        assigned[param_index] = Some(arg_index);
                        slots.push(ArgSlot::Param);
                    } else if var_keyword.is_some() {
                        slots.push(ArgSlot::ExtraKeyword);
                    } else {
                        return None;
                    }
                }
                CallArg::Starred(_) | CallArg::DoubleStarred(_) => return None,
            }
        }

        // 缺少必需参数同样交给运行时（错误消息需要参数名与种类）
        for (index, param) in params.iter().enumerate() {
            let is_variadic =
                matches!(param.kind, ParamKind::VarPositional | ParamKind::VarKeyword);
            if !is_variadic && assigned[index].is_none() && param.default.is_none() {
                return None;
            }
        }

        Some(BindPlan { slots, assigned })
    }

    /// 按参数顺序生成直调实参：已提供的实参、`*args` 收集、`**kwargs` 收集、默认值。
    fn build_static_arguments(
        &mut self,
        fn_id: FuncId,
        args: &[CallArg],
        plan: &BindPlan,
    ) -> Vec<Operand> {
        // 先按源码顺序求值全部实参（Python 的求值顺序），再分配
        let mut values: Vec<(Operand, Ty)> = Vec::with_capacity(args.len());
        for arg in args {
            let expr = arg.expr();
            let op = self.build_expr(expr);
            let ty = self.ty_of(expr);
            values.push((op, ty));
        }

        let func = &self.hir.functions[fn_id];
        let mut extra_positional: Vec<Operand> = Vec::new();
        let mut extra_keyword: Vec<(String, Operand)> = Vec::new();

        // 额外的位置 / 关键字实参先装箱
        for (arg_index, slot) in plan.slots.iter().enumerate() {
            let (op, ty) = values[arg_index].clone();
            match slot {
                ArgSlot::Param => {}
                ArgSlot::ExtraPositional => {
                    extra_positional.push(self.coerce(op, &ty, &MirType::Dynamic));
                }
                ArgSlot::ExtraKeyword => {
                    if let CallArg::Keyword(name, _) = &args[arg_index] {
                        let boxed = self.coerce(op, &ty, &MirType::Dynamic);
                        extra_keyword.push((name.as_str().to_owned(), boxed));
                    }
                }
            }
        }

        let defaults_global = self.fn_defaults.get(&fn_id).copied().flatten();
        let mut call_args = Vec::with_capacity(func.params.len());
        for (index, param) in func.params.iter().enumerate() {
            match param.kind {
                ParamKind::VarPositional => {
                    let tuple = self.call_with_args(
                        RuntimeFn::MakeTuple,
                        Vec::new(),
                        extra_positional.clone(),
                    );
                    call_args.push(tuple);
                }
                ParamKind::VarKeyword => {
                    let dict = self.runtime_call(RuntimeFn::MakeDict, Vec::new(), MirType::Dynamic);
                    for (name, value) in extra_keyword.drain(..) {
                        self.push_stmt(Statement::Eval(Rvalue::Call {
                            callee: Callee::Runtime(RuntimeFn::SetItem),
                            args: vec![dict.clone(), Operand::Const(Constant::Str(name)), value],
                        }));
                    }
                    call_args.push(dict);
                }
                _ => {
                    let expected = ty_to_mir(&self.types.sigs[fn_id].params[index]);
                    if let Some(arg_index) = plan.assigned[index] {
                        let (op, ty) = values[arg_index].clone();
                        call_args.push(self.coerce(op, &ty, &expected));
                    } else if let Some(global) = defaults_global {
                        // 默认值在模块初始化时写进静态数组，这里只读取（保持对象身份）
                        let raw = self.runtime_call(
                            RuntimeFn::ArrayGet,
                            vec![
                                Operand::Global(global),
                                Operand::Const(Constant::I64(index as i64)),
                            ],
                            MirType::Dynamic,
                        );
                        call_args.push(self.coerce(raw, &Ty::Dynamic, &expected));
                    } else {
                        // plan_binding 已保证不会到这里；用 0 占位避免生成非法 MIR
                        call_args.push(Operand::Const(Constant::I64(0)));
                    }
                }
            }
        }
        call_args
    }

    // ───────────────────────── 运行时调用 ─────────────────────────

    /// 生成一次 `PyOps.CallValue(callable, args, kwNames, kwValues)`。
    ///
    /// 纯位置、参数不超过 3 个时改用 [`Rvalue::DynCallValue`]：codegen 会走
    /// `PyPic.CallValueN`，运行时复用调用点上的参数数组。
    pub(crate) fn call_value(&mut self, callable: Operand, args: &[CallArg]) -> Operand {
        if is_small_positional_call(args) {
            let values: Vec<Operand> = args
                .iter()
                .map(|arg| {
                    let expr = arg.expr();
                    let op = self.build_expr(expr);
                    let ty = self.ty_of(expr);
                    self.coerce(op, &ty, &MirType::Dynamic)
                })
                .collect();
            let dest = self.alloc_temp(MirType::Dynamic);
            self.push_stmt(Statement::Assign {
                dest,
                value: Rvalue::DynCallValue {
                    callee: callable,
                    args: values,
                },
            });
            return Operand::Local(dest);
        }
        let (positional, kw_names, kw_values) = self.build_call_arrays(args);
        let dest = self.alloc_temp(MirType::Dynamic);
        self.push_stmt(Statement::Assign {
            dest,
            value: Rvalue::Call {
                callee: Callee::Runtime(RuntimeFn::CallValue),
                args: vec![
                    callable,
                    Operand::Array(positional),
                    Operand::Array(kw_names),
                    Operand::Array(kw_values),
                ],
            },
        });
        Operand::Local(dest)
    }

    /// 把实参拆成 `(位置值, 关键字名, 关键字值)` 三个数组。
    ///
    /// `*iterable` 用 `PyOps.StarArg` 打上标记，`**mapping` 用 `None` 作为关键字名；
    /// 展开发生在运行时（长度只有运行时才知道）。
    pub(crate) fn build_call_arrays(
        &mut self,
        args: &[CallArg],
    ) -> (Vec<Operand>, Vec<Operand>, Vec<Operand>) {
        let mut positional = Vec::new();
        let mut kw_names = Vec::new();
        let mut kw_values = Vec::new();
        for arg in args {
            match arg {
                CallArg::Positional(expr) => {
                    let op = self.build_expr(*expr);
                    let ty = self.ty_of(*expr);
                    positional.push(self.coerce(op, &ty, &MirType::Dynamic));
                }
                CallArg::Starred(expr) => {
                    let op = self.build_expr(*expr);
                    let ty = self.ty_of(*expr);
                    let boxed = self.coerce(op, &ty, &MirType::Dynamic);
                    positional.push(self.runtime_call(
                        RuntimeFn::StarArg,
                        vec![boxed],
                        MirType::Dynamic,
                    ));
                }
                CallArg::Keyword(name, expr) => {
                    let op = self.build_expr(*expr);
                    let ty = self.ty_of(*expr);
                    kw_names.push(Operand::Const(Constant::Str(name.as_str().to_owned())));
                    kw_values.push(self.coerce(op, &ty, &MirType::Dynamic));
                }
                CallArg::DoubleStarred(expr) => {
                    let op = self.build_expr(*expr);
                    let ty = self.ty_of(*expr);
                    kw_names.push(Operand::Const(Constant::None));
                    kw_values.push(self.coerce(op, &ty, &MirType::Dynamic));
                }
            }
        }
        (positional, kw_names, kw_values)
    }

    // ───────────────────────── 方法调用 ─────────────────────────

    fn build_method_call(
        &mut self,
        base: ExprId,
        name: String,
        args: &[CallArg],
        call_ty: &Ty,
    ) -> Operand {
        let obj_op = self.build_expr(base);
        let obj_ty = self.ty_of(base);
        let obj = self.coerce(obj_op, &obj_ty, &MirType::Dynamic);
        if is_small_positional_call(args) {
            let values: Vec<Operand> = args
                .iter()
                .map(|arg| {
                    let expr = arg.expr();
                    let op = self.build_expr(expr);
                    let ty = self.ty_of(expr);
                    self.coerce(op, &ty, &MirType::Dynamic)
                })
                .collect();
            let dest = self.alloc_temp(MirType::Dynamic);
            self.push_stmt(Statement::Assign {
                dest,
                value: Rvalue::DynCallMethod {
                    obj,
                    name,
                    args: values,
                },
            });
            return self.coerce(Operand::Local(dest), &Ty::Dynamic, &ty_to_mir(call_ty));
        }
        let (positional, kw_names, kw_values) = self.build_call_arrays(args);
        let dest = self.alloc_temp(MirType::Dynamic);
        self.push_stmt(Statement::Assign {
            dest,
            value: Rvalue::Call {
                callee: Callee::Runtime(RuntimeFn::CallMethod),
                args: vec![
                    obj,
                    Operand::Const(Constant::Str(name)),
                    Operand::Array(positional),
                    Operand::Array(kw_names),
                    Operand::Array(kw_values),
                ],
            },
        });
        self.coerce(Operand::Local(dest), &Ty::Dynamic, &ty_to_mir(call_ty))
    }

    // ───────────────────────── 类构造 ─────────────────────────

    fn build_class_call(&mut self, cls_id: ryupy_hir::ClassId, args: &[CallArg]) -> Operand {
        let is_dataclass = self.hir.classes[cls_id].is_dataclass;
        if !is_dataclass {
            // 普通 class：构造是运行时 `PyClass.Call`（查找 `__init__`、绑定参数、
            // 异常类还要通过工厂委托创建 CLR 异常对象）。
            let Some(global) = self.class_globals.get(&cls_id).copied() else {
                return self.none_value();
            };
            return self.call_value(Operand::Global(global), args);
        }
        let cls = &self.hir.classes[cls_id];
        let field_count = cls.fields.len();
        let mut assigned: Vec<Option<usize>> = vec![None; field_count];
        let mut position = 0usize;
        let mut ok = true;

        for (arg_index, arg) in args.iter().enumerate() {
            match arg {
                CallArg::Positional(_) => {
                    if position < field_count {
                        assigned[position] = Some(arg_index);
                        position += 1;
                    } else {
                        ok = false;
                    }
                }
                CallArg::Keyword(name, _) => {
                    match cls.fields.iter().position(|field| field.name == *name) {
                        Some(field_index) if assigned[field_index].is_none() => {
                            assigned[field_index] = Some(arg_index);
                        }
                        _ => ok = false,
                    }
                }
                CallArg::Starred(_) | CallArg::DoubleStarred(_) => ok = false,
            }
        }
        if assigned.iter().any(Option::is_none) {
            ok = false;
        }

        // 先按源码顺序求值实参（即使最终报错，副作用顺序也要正确）
        let mut values = Vec::with_capacity(args.len());
        for arg in args {
            let expr = arg.expr();
            let op = self.build_expr(expr);
            let ty = self.ty_of(expr);
            values.push((op, ty));
        }

        if !ok {
            self.push_stmt(Statement::Eval(Rvalue::Call {
                callee: Callee::Runtime(RuntimeFn::ArgError),
                args: vec![
                    Operand::Const(Constant::I64(0)),
                    Operand::Const(Constant::Str(format!("{}() 参数绑定失败", cls.name))),
                ],
            }));
        }

        let mir_cls_id = la_arena::Idx::from_raw(cls_id.into_raw());
        let mut args_ops = Vec::with_capacity(field_count);
        for (index, field) in cls.fields.iter().enumerate() {
            let field_ty = ty_to_mir(&type_ref_to_ty(&field.annotation));
            match assigned[index] {
                Some(arg_index) => {
                    let (op, ty) = values[arg_index].clone();
                    args_ops.push(self.coerce(op, &ty, &field_ty));
                }
                None => args_ops.push(default_value_for(&field_ty)),
            }
        }
        let dest = self.alloc_temp(MirType::Class(mir_cls_id));
        self.push_stmt(Statement::Assign {
            dest,
            value: Rvalue::NewObject {
                class: mir_cls_id,
                args: args_ops,
            },
        });
        Operand::Local(dest)
    }
}

/// 生成一个类型正确的占位值；只在“绑定失败、随后必定抛异常”的不可达路径上使用。
fn default_value_for(ty: &MirType) -> Operand {
    match ty {
        MirType::I64 => Operand::Const(Constant::I64(0)),
        MirType::F64 => Operand::Const(Constant::F64(0.0)),
        MirType::Bool => Operand::Const(Constant::Bool(false)),
        MirType::Str => Operand::Const(Constant::Str(String::new())),
        MirType::Class(_) => Operand::Const(Constant::Null),
        _ => Operand::Const(Constant::None),
    }
}

/// 纯位置、无关键字、无 `*` / `**` 且参数不超过 3 个的调用可以走专用快速入口。
fn is_small_positional_call(args: &[CallArg]) -> bool {
    args.len() <= 3 && args.iter().all(|arg| matches!(arg, CallArg::Positional(_)))
}
