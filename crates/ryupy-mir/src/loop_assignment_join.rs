//! 动态while的确定赋值出口：循环可能零次执行，也可能break；删除/except清理不能被循环体的后续构建覆盖。
use crate::build::FunctionBuilder;
use rustc_hash::FxHashSet;
use ryupy_hir::{Binding, Body, ExprKind, LocalId, StmtId, StmtKind};

impl FunctionBuilder<'_> {
    pub(crate) fn restore_loop_assignments(
        &mut self,
        before: &FxHashSet<LocalId>,
        sections: &[&[StmtId]],
    ) {
        // 循环内仍使用原快速事实；只在else入口/退出合流，生成代码不增加逐迭代 bookkeeping。
        let body = &self.hir.functions[self.current_hir_fn].body;
        let mut unbound = FxHashSet::default();
        for section in sections {
            collect_unbinding(body, section, &mut unbound);
        }
        self.assigned_locals = before
            .iter()
            .filter(|id| !unbound.contains(id))
            .copied()
            .collect();
    }
}

fn collect_unbinding(body: &Body, statements: &[StmtId], found: &mut FxHashSet<LocalId>) {
    for id in statements {
        match &body.stmts[*id].kind {
            StmtKind::Delete(values) => {
                for value in values {
                    if let ExprKind::Local(local) = body.exprs[*value].kind {
                        found.insert(local);
                    }
                }
            }
            StmtKind::If { then_, else_, .. } => {
                collect_unbinding(body, then_, found);
                collect_unbinding(body, else_, found);
            }
            StmtKind::While {
                body: inner,
                orelse,
                ..
            }
            | StmtKind::ForEach {
                body: inner,
                orelse,
                ..
            }
            | StmtKind::ForRange {
                body: inner,
                orelse,
                ..
            } => {
                collect_unbinding(body, inner, found);
                collect_unbinding(body, orelse, found);
            }
            StmtKind::Try {
                body: inner,
                handlers,
                orelse,
                finalbody,
            } => {
                collect_unbinding(body, inner, found);
                collect_unbinding(body, orelse, found);
                collect_unbinding(body, finalbody, found);
                for handler in handlers {
                    let handler = &body.handlers[*handler];
                    // except E as x在所有退出路径自动清除x，包括已有的同名参数/局部。
                    if let Some(Binding::Local(local)) = handler.name {
                        found.insert(local);
                    }
                    collect_unbinding(body, &handler.body, found);
                }
            }
            _ => {} // 嵌套函数/类体有独立词法作用域；cell读取始终走运行时检查。
        }
    }
}
