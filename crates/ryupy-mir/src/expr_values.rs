//! 字面量、装箱 / 拆箱与运行时调用辅助（[`ExprBuilder`] 的值侧方法）。
//!
//! 动态值表示见 ADR-0010：静态槽位 ↔ 装箱 `object` 之间的转换全部经过
//! [`coerce`](super::ExprBuilder::coerce)，保证身份规则（True / 小整数）一致。

use ryupy_hir::Literal;
use ryupy_ty::Ty;

use crate::build::ty_to_mir;
use crate::expr_builder::ExprBuilder;
use crate::mir::{Callee, Constant, MirType, Operand, RuntimeFn, Rvalue, Statement};

impl<'a> ExprBuilder<'a> {
    pub(crate) fn build_literal(&mut self, lit: &Literal) -> Operand {
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
    pub(crate) fn boxed_const(&mut self, c: Constant) -> Operand {
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
        // 类型旁表在动态函数里一律返回 `Dynamic`，但操作数本身可能是静态的
        // （例如 `NewObject` 的结果）。装箱/拆箱必须以**操作数实际类型**为准，
        // 否则会生成「对静态值拆箱」的非法 MIR。
        let from_mir = self.operand_type(&op).unwrap_or_else(|| ty_to_mir(from));
        // `Ty::None` 的操作数已经是运行时的 None 单例（装箱 object）。
        let from_dynamic = matches!(from_mir, MirType::Dynamic) || matches!(from, Ty::None);
        let to_dynamic = matches!(to, MirType::Dynamic);
        match (from_dynamic, to_dynamic) {
            (true, true) => op,
            (false, false) => {
                if from_mir == *to {
                    op
                } else {
                    // 两个静态表示之间（如 i64 → f64）：先装箱再交给运行时拆箱。
                    let boxed = self.box_operand(op, &from_mir);
                    self.unbox_operand(boxed, to)
                }
            }
            (false, true) => self.box_operand(op, &from_mir),
            (true, false) => self.unbox_operand(op, to),
        }
    }

    pub(crate) fn box_operand(&mut self, op: Operand, from: &MirType) -> Operand {
        let dest = self.alloc_temp(MirType::Dynamic);
        self.push_stmt(Statement::Assign {
            dest,
            value: Rvalue::Box {
                value: op,
                from: from.clone(),
            },
        });
        Operand::Local(dest)
    }

    pub(crate) fn unbox_operand(&mut self, op: Operand, to: &MirType) -> Operand {
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

    /// 把操作数搬进一个局部变量（已经局部时原样返回）。
    pub(crate) fn as_local(&mut self, op: Operand) -> Operand {
        if matches!(op, Operand::Local(_)) {
            return op;
        }
        let ty = self.operand_type(&op).unwrap_or(MirType::Dynamic);
        let dest = self.alloc_temp(ty);
        self.push_stmt(Statement::Assign {
            dest,
            value: Rvalue::Use(op),
        });
        Operand::Local(dest)
    }

    /// 操作数在 CIL 层面的静态类型；无法判断时返回 `None`（按调用方给的类型处理）。
    pub(crate) fn operand_type(&self, op: &Operand) -> Option<MirType> {
        match op {
            Operand::Local(id) => Some(self.locals[*id].ty.clone()),
            Operand::Global(gid) => Some(self.globals[*gid].ty.clone()),
            Operand::Const(constant) => Some(match constant {
                crate::mir::Constant::I64(_) => MirType::I64,
                crate::mir::Constant::F64(_) => MirType::F64,
                crate::mir::Constant::Bool(_) => MirType::Bool,
                crate::mir::Constant::Str(_) => MirType::Str,
                crate::mir::Constant::Null | crate::mir::Constant::None => MirType::Dynamic,
                crate::mir::Constant::BigInt(_) => MirType::Dynamic,
            }),
            Operand::Array(_) => Some(MirType::Dynamic),
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
}
