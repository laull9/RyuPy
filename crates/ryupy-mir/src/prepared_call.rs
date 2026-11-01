//! 实参可能暂停时，先绑定调用目标，避免恢复后重新读取已变更的类/模块槽位。

use crate::build::ty_to_mir;
use crate::expr_builder::ExprBuilder;
use crate::mir::{Constant, MirType, Operand, RuntimeFn, Rvalue, Statement};
use ryupy_hir::{Builtin, CallArg, ExprId, ExprKind};
use ryupy_ty::Ty;

impl ExprBuilder<'_> {
    pub(crate) fn build_prepared_call(
        &mut self,
        callee: ExprId,
        args: &[CallArg],
        expression: ExprId,
    ) -> Operand {
        let call_ty = self.ty_of(expression);
        let has_star = args.iter().any(|arg| matches!(arg, CallArg::Starred(_)));
        let kind = self.hir.functions[self.current_hir_fn].body.exprs[callee]
            .kind
            .clone();
        if let ExprKind::Class(class) = kind
            && self.hir.classes[class].is_dataclass
        {
            let range = self.hir.functions[self.current_hir_fn].body.exprs[expression].range;
            self.diagnostics.push(
                ryupy_base::Diagnostic::error(
                    ryupy_base::Span::new(self.hir.file, range),
                    "dataclass 跨暂停构造尚未接入专用字段构造 ABI",
                )
                .with_code("E0305"),
            );
            return self.none_value();
        }
        // 真正的内建绑定不会被修改；同名用户变量已经解析成普通名字路径。
        if let ExprKind::Builtin(builtin) = kind {
            let name = match builtin {
                Builtin::Print => "print",
                Builtin::Int => "int",
                Builtin::Float => "float",
                Builtin::Str => "str",
                Builtin::Abs => "abs",
                Builtin::Dynamic(name) => name,
                Builtin::Type | Builtin::Exception(_) => {
                    return self.build_builtin_call(builtin, args, &call_ty);
                }
            };
            // 避开只读取首参的静态快路径，确保全部实参在调用前逐个恢复求值。
            if has_star {
                let (positional, names, values) = self.build_prepared_call_arrays(args);
                return self.runtime_call(
                    RuntimeFn::DynBuiltin,
                    vec![
                        Operand::Const(Constant::Str(name.to_owned())),
                        Operand::Array(positional),
                        Operand::Array(names),
                        Operand::Array(values),
                    ],
                    MirType::Dynamic,
                );
            }
            return self.call_builtin_by_name(name, args);
        }
        let callable = if let ExprKind::Attribute { base, name } = kind {
            let raw = self.build_expr(base);
            let ty = self.ty_of(base);
            let receiver = self.coerce(raw, &ty, &MirType::Dynamic);
            self.runtime_call(
                RuntimeFn::PrepareMethod,
                vec![receiver, Operand::Const(Constant::Str(name.to_string()))],
                MirType::Dynamic,
            )
        } else {
            let raw = self.build_expr(callee);
            let ty = self.ty_of(callee);
            self.coerce(raw, &ty, &MirType::Dynamic)
        };
        // 即使是 Global 操作数也立即取值，不能等参数恢复后才加载函数/类对象。
        let saved = self.alloc_temp(MirType::Dynamic);
        self.push_stmt(Statement::Assign {
            dest: saved,
            value: Rvalue::Use(callable),
        });
        let result = if has_star {
            let (positional, names, values) = self.build_prepared_call_arrays(args);
            self.runtime_call(
                RuntimeFn::CallValue,
                vec![
                    Operand::Local(saved),
                    Operand::Array(positional),
                    Operand::Array(names),
                    Operand::Array(values),
                ],
                MirType::Dynamic,
            )
        } else {
            self.call_value(Operand::Local(saved), args)
        };
        self.coerce(result, &Ty::Dynamic, &ty_to_mir(&call_ty))
    }
}
