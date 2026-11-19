//! 有界普通函数locals：保留每调用缓存身份，并按源码槽序直接读取原始值。
//! 不使用LoadChecked，未绑定槽由运行时删键；无直接调用时不增加任何MIR槽。
use crate::expr_builder::ExprBuilder;
use crate::mir::{Callee, Constant, MirType, Operand, RuntimeFn, Rvalue, Statement};
use ryupy_hir::CallArg;

impl ExprBuilder<'_> {
    pub(crate) fn supports_function_locals(&self) -> bool {
        self.class_namespace.is_none()
            && self.hir.functions[self.current_hir_fn]
                .body
                .function_locals
                .is_some()
    }

    pub(crate) fn build_function_locals(&mut self, args: &[CallArg]) -> Operand {
        // 通用参数数组会延后展开；这些形状需要交错求值/合并，不能近似执行。
        let positional = args
            .iter()
            .filter(|arg| matches!(arg, CallArg::Positional(_) | CallArg::Starred(_)))
            .count();
        let mixed_star =
            positional > 1 && args.iter().any(|arg| matches!(arg, CallArg::Starred(_)));
        let mut saw_mapping = false;
        let mut followed_mapping = false;
        for arg in args {
            if matches!(arg, CallArg::Keyword(..) | CallArg::DoubleStarred(_)) {
                followed_mapping |= saw_mapping;
                saw_mapping |= matches!(arg, CallArg::DoubleStarred(_));
            }
        }
        if mixed_star || followed_mapping {
            let range = self.hir.functions[self.current_hir_fn].range;
            self.diagnostics.push(ryupy_base::Diagnostic::error(
                ryupy_base::Span::new(self.hir.file, range),
                "locals()暂不支持混合位置/*实参、多个*实参或**之后仍有关键字组；需保持展开与求值交错顺序",
            ).with_code("E0305"));
            return self.none_value();
        }
        // 无实参的已解析内建无需绑定，热路不分配三个空参数数组。
        if !args.is_empty() {
            let (positional, names, values) = self.build_call_arrays(args);
            self.push_stmt(Statement::Eval(Rvalue::Call {
                callee: Callee::Runtime(RuntimeFn::FunctionLocalsValidate),
                args: vec![
                    Operand::Array(positional),
                    Operand::Array(names),
                    Operand::Array(values),
                ],
            }));
        }
        let cache = match *self.function_locals_cache {
            Some(cache) => cache,
            None => {
                // CLR initlocals使每次调用初始为null；不能在调用点重置递归/循环中的缓存。
                let cache = self.alloc_temp(MirType::Dynamic);
                *self.function_locals_cache = Some(cache);
                cache
            }
        };
        self.push_stmt(Statement::Assign {
            dest: cache,
            value: Rvalue::Call {
                callee: Callee::Runtime(RuntimeFn::FunctionLocalsEnsure),
                args: vec![Operand::Local(cache)],
            },
        });
        let function = &self.hir.functions[self.current_hir_fn];
        let entries: Vec<_> = function
            .body
            .function_locals
            .as_ref()
            .into_iter()
            .flatten()
            .map(|id| {
                (
                    function.body.locals[*id].name.to_string(),
                    self.local_map[id],
                )
            })
            .collect();
        for (name, slot) in entries {
            self.push_stmt(Statement::Eval(Rvalue::Call {
                callee: Callee::Runtime(RuntimeFn::FunctionLocalsSync),
                args: vec![
                    Operand::Local(cache),
                    Operand::Const(Constant::Str(name)),
                    Operand::Local(slot),
                ],
            }));
        }
        Operand::Local(cache)
    }
}
