//! 条件类绑定的惰性回退：只在 own namespace 缺值时读取模块或内建。
use crate::expr_builder::ExprBuilder;
use crate::mir::{Constant, MirType, Operand, RuntimeFn, Terminator};
impl ExprBuilder<'_> {
    pub(crate) fn build_class_lookup(
        &mut self,
        name: &str,
        fallback: ryupy_hir::ExprId,
    ) -> Operand {
        let namespace = self
            .class_namespace
            .clone()
            .unwrap_or(Operand::Const(Constant::Null));
        let args = vec![namespace, Operand::Const(Constant::Str(name.to_owned()))];
        let present = self.runtime_call(
            if self.lexical_namespace {
                RuntimeFn::LexicalNamespaceContains
            } else {
                RuntimeFn::ClassNamespaceContains
            },
            args.clone(),
            MirType::Bool,
        );
        let yes = self.new_block();
        let no = self.new_block();
        let join = self.new_block();
        let dest = self.alloc_temp(MirType::Dynamic);
        self.blocks[*self.current_bb].terminator = Terminator::Branch {
            cond: present,
            then_: yes,
            else_: no,
        };
        *self.current_bb = yes;
        let value = self.runtime_call(
            if self.lexical_namespace {
                RuntimeFn::LexicalNamespaceGet
            } else {
                RuntimeFn::ClassNamespaceGet
            },
            args,
            MirType::Dynamic,
        );
        self.assign_value(dest, value, &ryupy_ty::Ty::Dynamic);
        self.blocks[*self.current_bb].terminator = Terminator::Goto(join);
        *self.current_bb = no;
        let value = self.build_expr(fallback);
        let ty = self.ty_of(fallback);
        self.assign_value(dest, value, &ty);
        self.blocks[*self.current_bb].terminator = Terminator::Goto(join);
        *self.current_bb = join;
        Operand::Local(dest)
    }
}
