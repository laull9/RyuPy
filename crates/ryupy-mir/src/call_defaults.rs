//! 未观察默认值的有守卫直调：callee与实参各冻结一次，守卫失败沿原对象绑定。
//! 仅普通legacy顶层Dynamic函数的至多三个纯位置参数；闭包/实例/状态机保原调用链。
use crate::build::ty_to_mir;
use crate::expr_builder::ExprBuilder;
use crate::mir::*;
use ryupy_hir::{CallArg, FuncId, FunctionKind, ParamKind};
use ryupy_ty::{FuncMode, Ty};

impl ExprBuilder<'_> {
    pub(crate) fn call_defaults_or_published(
        &mut self,
        function: FuncId,
        args: &[CallArg],
        result_ty: &Ty,
    ) -> Operand {
        if !self.unobserved_defaults_candidate(function, args) {
            return self.call_published_function(function, args, result_ty);
        }
        let source = self.fn_sources[&function];
        let published = self.fn_globals[&function];
        let name = self.hir.functions[function].name.to_string();
        let callable = self.runtime_call(
            RuntimeFn::LoadName,
            vec![
                Operand::Global(published),
                Operand::Const(Constant::Str(name)),
            ],
            MirType::Dynamic,
        );
        // 每个实参立即保存，后续实参回调可重绑全局名；快慢两臂均复用这些原对象。
        let mut values = Vec::with_capacity(args.len());
        for arg in args {
            let expression = arg.expr();
            let value = self.build_expr(expression);
            let value = self.coerce(value, &self.ty_of(expression), &MirType::Dynamic);
            let saved = self.alloc_temp(MirType::Dynamic);
            self.push_stmt(Statement::Assign {
                dest: saved,
                value: Rvalue::Use(value),
            });
            values.push(Operand::Local(saved));
        }
        // 锚点只由对应MakeFunction成功发布，签名相同的其它函数不能通过引用身份守卫。
        let guard = self.runtime_call(
            RuntimeFn::FunctionDefaultsUnobserved,
            vec![callable.clone(), Operand::Global(source)],
            MirType::Bool,
        );
        let fast = self.defaults_block();
        let fallback = self.defaults_block();
        let join = self.defaults_block();
        self.blocks[*self.current_bb].terminator = Terminator::Branch {
            cond: guard,
            then_: fast,
            else_: fallback,
        };
        let result = self.alloc_temp(MirType::Dynamic);
        *self.current_bb = fast;
        let defaults = self.runtime_call(
            RuntimeFn::FunctionDefaults,
            vec![callable.clone()],
            MirType::Dynamic,
        );
        let mut bound = values.clone();
        for index in args.len()..self.hir.functions[function].params.len() {
            bound.push(self.runtime_call(
                RuntimeFn::ArrayGet,
                vec![
                    defaults.clone(),
                    Operand::Const(Constant::I64(index as i64)),
                ],
                MirType::Dynamic,
            ));
        }
        // 无env/context，Dynamic尾owner仍是实参之前冻结的真实函数。
        bound.push(callable.clone());
        self.push_stmt(Statement::Assign {
            dest: result,
            value: Rvalue::Call {
                callee: Callee::Fn(self.fn_map[&function]),
                args: bound,
            },
        });
        self.blocks[*self.current_bb].terminator = Terminator::Goto(join);
        *self.current_bb = fallback;
        self.push_stmt(Statement::Assign {
            dest: result,
            value: Rvalue::DynCallValue {
                callee: callable,
                args: values,
            },
        });
        self.blocks[*self.current_bb].terminator = Terminator::Goto(join);
        *self.current_bb = join;
        self.coerce(Operand::Local(result), &Ty::Dynamic, &ty_to_mir(result_ty))
    }

    fn unobserved_defaults_candidate(&self, function: FuncId, args: &[CallArg]) -> bool {
        let target = &self.hir.functions[function];
        self.types.mode(function) == FuncMode::Dynamic
            && target.kind == FunctionKind::User
            && !self.hir.info.instance_globals
            && target.closure.free.is_empty()
            && !target.is_generator
            && !target.is_coroutine
            && self.fn_sources.contains_key(&function)
            && self.fn_globals.contains_key(&function)
            && (1..=3).contains(&target.params.len())
            && args.len() < target.params.len()
            && args.iter().all(|arg| matches!(arg, CallArg::Positional(_)))
            && target.params.iter().all(|param| {
                matches!(
                    param.kind,
                    ParamKind::PositionalOnly | ParamKind::PositionalOrKeyword
                )
            })
            && target.params[args.len()..]
                .iter()
                .all(|param| param.default.is_some())
    }

    fn defaults_block(&mut self) -> BlockId {
        self.blocks.alloc(BasicBlock {
            stmts: Vec::new(),
            terminator: Terminator::Unreachable,
        })
    }
}
