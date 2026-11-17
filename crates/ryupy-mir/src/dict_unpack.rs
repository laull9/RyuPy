//! 含**字典的分块构造：连续Pair按既有BUILD_MAP规则准备，mapping逐个立即合并。
use crate::expr_builder::ExprBuilder;
use crate::mir::{Callee, MirType, Operand, RuntimeFn, Rvalue, Statement};
use ryupy_hir::{DictItem, ExprId};

impl ExprBuilder<'_> {
    pub(crate) fn build_dict_unpack(&mut self, items: &[DictItem], expr_id: ExprId) -> Operand {
        let mut index = 0;
        let prefix = Self::dict_pair_run(items, &mut index);
        let dictionary = self.build_dict_literal(&prefix, expr_id);
        while index < items.len() {
            let source = if let DictItem::Unpack(source) = items[index] {
                index += 1;
                let value = self.build_expr(source);
                let ty = self.ty_of(source);
                self.coerce(value, &ty, &MirType::Dynamic)
            } else {
                let pairs = Self::dict_pair_run(items, &mut index);
                self.build_dict_literal(&pairs, expr_id)
            };
            // dictionary本身是Dynamic临时；yield/await后由既有活性分析保存并恢复。
            self.push_stmt(Statement::Eval(Rvalue::Call {
                callee: Callee::Runtime(RuntimeFn::DictUnpackMapping),
                args: vec![dictionary.clone(), source],
            }));
        }
        dictionary
    }

    fn dict_pair_run(items: &[DictItem], index: &mut usize) -> Vec<(ExprId, ExprId)> {
        let mut pairs = Vec::new();
        while let Some(DictItem::Pair(key, value)) = items.get(*index) {
            pairs.push((*key, *value));
            *index += 1;
        }
        pairs
    }
}
