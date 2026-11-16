//! 类构造的透明异常清理：只释放外部 prepared namespace，再原样抛出同一异常。
//! 清理域不是 Python except，不修改 handled exception 或 traceback/frame 状态。
use crate::build::FunctionBuilder;
use crate::mir::{
    BlockId, Callee, Constant, MirType, Operand, Region, RegionKind, RuntimeFn, Rvalue, Statement,
    Terminator,
};

impl FunctionBuilder<'_> {
    // prepare 先执行；LOAD_NAME 对真实 mapping 只查询一次，缺名才访问定义者 globals/builtins。
    pub(crate) fn initialize_foreign_class_metadata(
        &mut self,
        namespace: Operand,
        module_name: ryupy_hir::ModuleVarId,
        qualname: String,
    ) {
        let found = self.expr_builder().runtime_call(
            RuntimeFn::LexicalNamespaceContains,
            vec![
                namespace.clone(),
                Operand::Const(Constant::Str("__name__".into())),
            ],
            MirType::Bool,
        );
        let yes = self.new_block();
        let no = self.new_block();
        let after = self.new_block();
        let module = self.alloc_temp(MirType::Dynamic);
        self.terminate_current(Terminator::Branch {
            cond: found,
            then_: yes,
            else_: no,
        });
        self.current_bb = yes;
        self.push_stmt(Statement::Assign {
            dest: module,
            value: Rvalue::Call {
                callee: Callee::Runtime(RuntimeFn::LexicalNamespaceGet),
                args: vec![
                    namespace.clone(),
                    Operand::Const(Constant::Str("__name__".into())),
                ],
            },
        });
        self.terminate_current(Terminator::Goto(after));
        self.current_bb = no;
        let fallback = self.expr_builder().build_module_name(module_name);
        let ty = if self.module_state.is_some() || self.types.is_dynamic(self.current_hir_fn) {
            MirType::Dynamic
        } else {
            self.globals[self.module_vars[&module_name]].ty.clone()
        };
        let fallback = self.coerce_mir(fallback, &ty, &MirType::Dynamic);
        self.push_stmt(Statement::Assign {
            dest: module,
            value: Rvalue::Use(fallback),
        });
        self.terminate_current(Terminator::Goto(after));
        self.current_bb = after;
        self.emit_set_attr(namespace.clone(), "__module__", Operand::Local(module));
        let qualname = self.coerce_mir(
            Operand::Const(Constant::Str(qualname)),
            &MirType::Str,
            &MirType::Dynamic,
        );
        self.emit_set_attr(namespace, "__qualname__", qualname);
    }

    pub(crate) fn begin_class_cleanup(&mut self) -> BlockId {
        let start = self.new_block();
        self.terminate_current(Terminator::Goto(start));
        self.current_bb = start;
        self.protected_stack.push(self.block_index(start));
        start
    }

    pub(crate) fn end_class_cleanup(&mut self, start: BlockId, target: Operand) {
        let normal = self.if_unterminated_take();
        let end = self.block_index_of_next_block();
        self.protected_stack.pop();
        let exception = self.alloc_temp(MirType::Dynamic);
        let handler = self.new_block();
        self.current_bb = handler;
        self.push_stmt(Statement::Assign {
            dest: exception,
            value: Rvalue::CatchException,
        });
        self.push_stmt(Statement::Eval(Rvalue::Call {
            callee: Callee::Runtime(RuntimeFn::ClassDefinitionAbort),
            args: vec![target],
        }));
        self.blocks[handler].terminator = Terminator::Raise {
            value: Some(Operand::Local(exception)),
            cause: None,
        };
        self.regions.push(Region {
            source_handler_chain: None,
            kind: RegionKind::CatchAll,
            try_start: start,
            try_len: end - self.block_index(start),
            handler,
            exc_local: Some(exception),
        });
        let after = self.new_block();
        if let Some(normal) = normal {
            self.blocks[normal].terminator = Terminator::Goto(after);
        }
        self.current_bb = after;
    }
}
