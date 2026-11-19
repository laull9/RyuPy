//! 产品 V1 的源级动作构建。分派 token 与 ambient context 分开，不能把 CatchAll 当作 Handle。
use crate::build::FunctionBuilder;
use crate::*;

impl FunctionBuilder<'_> {
    fn product_depth(&mut self) -> Option<LocalId> {
        if self.frame_depth.is_none() {
            self.diagnostics.push(
                ryupy_base::Diagnostic::error(
                    ryupy_base::Span::new(
                        self.hir.file,
                        self.hir.functions[self.current_hir_fn].range,
                    ),
                    "trace V1缺少真实帧depth，不能生成局部旧回退",
                )
                .with_code("E0310"),
            );
        }
        self.frame_depth
    }

    pub(crate) fn emit_product_receive(&mut self, error: LocalId, forward: bool) {
        if !self.trace_product {
            return;
        }
        let Some(depth) = self.product_depth() else {
            return;
        };
        self.push_stmt(Statement::Eval(Rvalue::Call {
            callee: Callee::Runtime(if forward {
                RuntimeFn::TraceV1Forward
            } else {
                RuntimeFn::TraceV1Handle
            }),
            args: vec![Operand::Local(error), Operand::Local(depth)],
        }));
    }

    pub(crate) fn emit_product_park(&mut self, error: LocalId) -> Option<LocalId> {
        if !self.trace_product {
            return None;
        }
        let depth = self.product_depth()?;
        let park = self.alloc_temp(MirType::Dynamic);
        self.push_stmt(Statement::Assign {
            dest: park,
            value: Rvalue::Call {
                callee: Callee::Runtime(RuntimeFn::TraceV1Park),
                args: vec![Operand::Local(error), Operand::Local(depth)],
            },
        });
        Some(park)
    }

    pub(crate) fn emit_product_consume(&mut self, token: Option<LocalId>, action: RuntimeFn) {
        if let Some(token) = token {
            self.push_stmt(Statement::Eval(Rvalue::Call {
                callee: Callee::Runtime(action),
                args: vec![Operand::Local(token)],
            }));
            // guard 覆盖 selector 与正文；正文失败不能重复消费已成功匹配的 park。
            self.push_stmt(Statement::Assign {
                dest: token,
                value: Rvalue::Use(Operand::Const(Constant::None)),
            });
        }
    }
}
