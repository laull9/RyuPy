//! 列表推导式编译器私有 collector 的 ListAppend 直调优化。
//!
//! 在 HIR 降级阶段（`comprehensions.rs:lower_comprehension_body`），对于 ListComp：
//! 1. 为推导式结果分配不可被用户源码拼写的局部变量 `.result`；
//! 2. 在 root 首条语句将其初始化为空列表 `[]`（`StmtKind::Assign { target: Local(.result), value: List([]) }`）；
//! 3. 在推导式循环核心生成 `.result.append(elt)` 作为 `StmtKind::Expr`；
//! 4. 在推导式结束时返回 `.result`。
//!
//! 本模块通过全套形式化 Guard 证明：
//! - 当前函数是编译器合成的 `<listcomp>` 隐式嵌套函数（而非用户定义的 def 或 lambda）；
//! - 接收者严格为本地 `.result`，且在 root 首条语句被精确初始化为空列表 `[]`；
//! - 该 `.result` 局部变量未在函数体内其他任何位置重新赋值/解包绑定/循环迭代；
//! - 该 `.result` 未被逃逸/捕获到闭包 cell 或 free 变量中；
//! - 调用方法名恰为 `"append"`，且实参为单一位置参数。
//!
//! 在所有 Guard 严格成立后，按 Python 规范先求值 receiver 再求值实参（各一次），
//! 直接发射 `RuntimeFn::ListAppend`（对应 `PyOps.ListAppend` void），避免通用
//! 方法分派（`IsModule`、`CaptureModuleTarget`、`DynCallMethod`）。
//! 若任何一项 Guard 不通过，直接返回 `None`，不产生任何副作用。

use ryupy_hir::{
    AssignTarget, Binding, CallArg, ExprId, ExprKind, FunctionKind, LocalId, SequenceItem,
    SequenceTarget, StmtKind,
};
use ryupy_ty::Ty;

use crate::build::ty_to_mir;
use crate::expr_builder::ExprBuilder;
use crate::mir::{Callee, MirType, Operand, RuntimeFn, Rvalue, Statement};

impl ExprBuilder<'_> {
    /// 尝试将推导式私有结果收集器的 `append` 方法调用直接降级为 `RuntimeFn::ListAppend`。
    ///
    /// 必须在所有 Guard 形式证明均通过后才进行降级，
    /// 否则返回 `None` 交由通用方法分派逻辑处理，不产生任何副作用。
    pub(crate) fn try_build_private_comp_append(
        &mut self,
        base: ExprId,
        name: &str,
        args: &[CallArg],
        call_ty: &Ty,
    ) -> Option<Operand> {
        // 1. 方法名必须是 "append"
        if name != "append" {
            return None;
        }

        // 2. 实参必须为唯一的单位置实参
        if args.len() != 1 {
            return None;
        }
        let CallArg::Positional(elt_expr) = args[0] else {
            return None;
        };

        // 3. 核对当前函数来源：必须是编译器合成的 <listcomp> 隐式嵌套函数
        let hir_fn = &self.hir.functions[self.current_hir_fn];
        if hir_fn.kind != FunctionKind::Nested {
            return None;
        }
        if hir_fn.name.as_str() != "<listcomp>" {
            return None;
        }
        if hir_fn.definition.is_some() {
            // 用户源码显式定义的 def / lambda 拥有 definition，必须拒绝
            return None;
        }
        if hir_fn.is_generator {
            return None;
        }

        // 4. 核对 base 接收者：必须是当前函数体内的私有局部变量 ".result"
        let ExprKind::Local(local_id) = hir_fn.body.exprs[base].kind else {
            return None;
        };
        let local = &hir_fn.body.locals[local_id];
        if local.name.as_str() != ".result" {
            return None;
        }
        if local.is_param {
            return None;
        }

        // 5. 核对闭包信息：.result 绝不能逃逸为 cell 或 free 变量
        if hir_fn.closure.cells.iter().any(|c| c.as_str() == ".result") {
            return None;
        }
        if hir_fn.closure.free.iter().any(|f| f.as_str() == ".result") {
            return None;
        }

        // 6. 核对函数 root 首条语句：必须将该 local 初始化为精确的空列表 []
        let &first_stmt_id = hir_fn.body.root.first()?;
        let first_stmt = &hir_fn.body.stmts[first_stmt_id];
        let StmtKind::Assign {
            target: AssignTarget::Local(init_local),
            value: init_val,
        } = &first_stmt.kind
        else {
            return None;
        };
        if *init_local != local_id {
            return None;
        }
        let init_expr = &hir_fn.body.exprs[*init_val];
        let ExprKind::List(items) = &init_expr.kind else {
            return None;
        };
        if !items.is_empty() {
            return None;
        }

        // 7. 静态证明单一定义：遍历当前函数内所有语句，确保除首条初始化外无任何对 local_id 的写入/绑定
        for (stmt_id, stmt) in hir_fn.body.stmts.iter() {
            if stmt_id == first_stmt_id {
                continue;
            }
            if stmt_writes_local(&hir_fn.body, stmt, local_id) {
                return None;
            }
        }

        // 8. 确保函数参数列表没有同名/同 local
        if hir_fn
            .params
            .iter()
            .any(|p| hir_fn.body.locals[p.local].name.as_str() == ".result")
        {
            return None;
        }

        // ── Guard 全部通过，开始生成 MIR 指令 ──
        // Python 语义保证：先求值接收者，再求值实参（各一次）
        let receiver_op = self.build_expr(base);
        let receiver_ty = self.ty_of(base);
        let receiver_obj = self.coerce(receiver_op, &receiver_ty, &MirType::Dynamic);

        let elt_op = self.build_expr(elt_expr);
        let elt_ty = self.ty_of(elt_expr);
        let elt_obj = self.coerce(elt_op, &elt_ty, &MirType::Dynamic);

        // 发射 ListAppend 直调语句
        self.push_stmt(Statement::Eval(Rvalue::Call {
            callee: Callee::Runtime(RuntimeFn::ListAppend),
            args: vec![receiver_obj, elt_obj],
        }));

        // 方法调用表达式返回 None
        let none = self.none_value();
        Some(self.coerce(none, &Ty::None, &ty_to_mir(call_ty)))
    }
}

/// 检查单条语句是否尝试向指定的局部变量写入或绑定
fn stmt_writes_local(body: &ryupy_hir::Body, stmt: &ryupy_hir::Stmt, local_id: LocalId) -> bool {
    match &stmt.kind {
        StmtKind::Assign { target, .. } => assign_target_writes_local(target, local_id),
        StmtKind::ForEach { var, .. } | StmtKind::ForRange { var, .. } => {
            binding_writes_local(var, local_id)
        }
        StmtKind::NestedDef { target, .. } | StmtKind::DecoratedDef { target, .. } => {
            assign_target_writes_local(target, local_id)
        }
        StmtKind::Import { bind, .. } => binding_writes_local(bind, local_id),
        StmtKind::ImportFrom { names, .. } => names
            .iter()
            .any(|(_, bind)| binding_writes_local(bind, local_id)),
        StmtKind::Try { handlers, .. } => handlers.iter().any(|handler| {
            body.handlers[*handler]
                .name
                .as_ref()
                .is_some_and(|bind| binding_writes_local(bind, local_id))
        }),
        // 急切推导式的源语法不会产生这些语句；拒绝缺失绑定/删除来源证明的手工HIR。
        StmtKind::Delete(_) | StmtKind::ClassDef(_) | StmtKind::FunctionDefaults(_) => true,
        // 子块语句已在body.stmts中逐项扫描；这些节点自身不写绑定。
        StmtKind::Expr(_)
        | StmtKind::Return(_)
        | StmtKind::Yield(_)
        | StmtKind::If { .. }
        | StmtKind::While { .. }
        | StmtKind::Break
        | StmtKind::Continue
        | StmtKind::Pass
        | StmtKind::Raise { .. } => false,
    }
}

/// 检查赋值绑定是否指向目标局部变量
fn binding_writes_local(binding: &Binding, local_id: LocalId) -> bool {
    matches!(binding, Binding::Local(id) if *id == local_id)
}

/// 检查赋值目标是否包含目标局部变量
fn assign_target_writes_local(target: &AssignTarget, local_id: LocalId) -> bool {
    match target {
        AssignTarget::Local(id) => *id == local_id,
        AssignTarget::Sequence(items) => items
            .iter()
            .any(|item| sequence_item_writes_local(item, local_id)),
        AssignTarget::ModuleVar(_)
        | AssignTarget::Free(_)
        | AssignTarget::Attribute { .. }
        | AssignTarget::Subscript { .. } => false,
    }
}

/// 检查序列解包项是否包含目标局部变量
fn sequence_item_writes_local(item: &SequenceItem, local_id: LocalId) -> bool {
    match item {
        SequenceItem::One(target) | SequenceItem::Rest(target) => match target {
            SequenceTarget::Binding(binding) => binding_writes_local(binding, local_id),
            SequenceTarget::Attribute { .. } | SequenceTarget::Subscript { .. } => false,
        },
    }
}
