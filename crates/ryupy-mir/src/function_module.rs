//! 函数定义时冻结真实模块槽并安装Module；缺省调用读取已成功发布的实例数组。
use crate::expr_builder::ExprBuilder;
use crate::mir::{Callee, Constant, MirType, Operand, RuntimeFn, Rvalue, Statement, Terminator};

impl ExprBuilder<'_> {
    /// 定义前尚无数组，先给受控NameError，不能让ArrayGet(null)触发CLR错误。
    /// 这里只封住新空槽边界；完整callee/defaults前置冻结由来源降级统一完成。
    pub(crate) fn load_published_function_defaults(
        &mut self,
        function: ryupy_hir::FuncId,
    ) -> Option<Operand> {
        let global = self.fn_defaults.get(&function).copied().flatten()?;
        let name = self.hir.functions[function].name.as_str().to_owned();
        Some(self.runtime_call(
            RuntimeFn::LoadName,
            vec![Operand::Global(global), Operand::Const(Constant::Str(name))],
            MirType::Dynamic,
        ))
    }

    /// 必须在默认值和非延迟注解完成后调用；类局部同名值不属于函数globals。
    pub(crate) fn capture_definition_context(&mut self) -> Option<Operand> {
        let state = self.module_state?;
        Some(self.runtime_call(
            RuntimeFn::InstanceCaptureDefinitionContext,
            vec![Operand::Local(state)],
            MirType::Dynamic,
        ))
    }

    pub(crate) fn capture_function_module(&mut self) -> Operand {
        if let Some(state) = self.module_state {
            return self.runtime_call(
                RuntimeFn::InstanceDefinitionModule,
                vec![Operand::Local(state)],
                MirType::Dynamic,
            );
        }
        let global = self
            .hir
            .vars
            .iter()
            .filter(|(_, var)| var.name.as_str() == "__name__")
            .find_map(|(id, _)| self.module_vars.get(&id))
            .copied();
        let Some(global) = global else {
            return Operand::Const(Constant::Null);
        };
        let saved = self.alloc_temp(MirType::Dynamic);
        let flag = self.module_bound_flags.get(&global).copied();
        let join = flag.map(|flag| {
            let present = self.new_block();
            let missing = self.new_block();
            let join = self.new_block();
            self.blocks[*self.current_bb].terminator = Terminator::Branch {
                cond: Operand::Global(flag),
                then_: present,
                else_: missing,
            };
            *self.current_bb = missing;
            self.push_stmt(Statement::Assign {
                dest: saved,
                value: Rvalue::Use(Operand::Const(Constant::Null)),
            });
            self.blocks[*self.current_bb].terminator = Terminator::Goto(join);
            *self.current_bb = present;
            join
        });
        let value = Operand::Global(global);
        let ty = self.globals[global].ty.clone();
        let value = if ty == MirType::Dynamic {
            value
        } else {
            self.box_operand(value, &ty)
        };
        self.push_stmt(Statement::Assign {
            dest: saved,
            value: Rvalue::Use(value),
        });
        if let Some(join) = join {
            self.blocks[*self.current_bb].terminator = Terminator::Goto(join);
            *self.current_bb = join;
        }
        Operand::Local(saved)
    }

    /// 构造与此冷写入之间不能执行用户代码；runtime仅写真实成员，不重新读取globals。
    pub(crate) fn install_function_module(&mut self, function: Operand, module: Operand) {
        self.push_stmt(Statement::Eval(Rvalue::Call {
            callee: Callee::Runtime(RuntimeFn::FunctionModule),
            args: vec![function, module],
        }));
    }
}
