//! 链式比较的结果类型：只有全部比较可证明精确 bool 时保留静态路径，任意对象结果保持 Dynamic。
use crate::Ty;
use crate::check::FunctionChecker;
use ryupy_hir::{CmpOp, ExprId};
impl FunctionChecker<'_> {
    pub(crate) fn check_comparison_chain(&mut self, operands: &[ExprId], ops: &[CmpOp]) -> Ty {
        let types: Vec<_> = operands
            .iter()
            .map(|operand| self.check_expr(*operand))
            .collect();
        if operands.len() != ops.len() + 1 || ops.is_empty() {
            self.soft_demote = true;
            return Ty::Dynamic;
        }
        let dynamic = ops.iter().zip(types.windows(2)).any(|(op, pair)| {
            matches!(op, CmpOp::In | CmpOp::NotIn | CmpOp::Is | CmpOp::IsNot)
                || crate::ops::check_cmp_op(*op, &pair[0], &pair[1]) != Some(Ty::Bool)
        });
        if dynamic {
            self.soft_demote = true;
            Ty::Dynamic
        } else {
            Ty::Bool
        }
    }
}

#[cfg(test)]
mod tests {
    use super::*;
    #[test]
    fn comparison_chains_keep_numeric_static_and_unknown_module_results_dynamic() {
        let source = "def numeric(x: int) -> bool:\n    return 0 < x < 10\n\nclass Rich:\n    def __lt__(self, other):\n        return self\n\nleft=Rich()\nright=Rich()\nresult=left < right\nchain=left < right < left\n";
        let mut sources = ryupy_base::SourceDb::default();
        let file = sources.add("comparison.py", source);
        let (parsed, diagnostics) = ryupy_syntax::parse(file, source);
        assert!(diagnostics.is_empty());
        let (module, diagnostics) = ryupy_hir::lower_module(
            &parsed,
            &ryupy_hir::ModuleInfo {
                instance_capabilities: Default::default(),
                instance_globals: false,
                resolve_file_at_runtime: false,
                name: "__main__".into(),
                package: String::new(),
                file: "comparison.py".into(),
            },
            source,
        );
        assert!(diagnostics.is_empty());
        let (types, diagnostics) = crate::check_module(&module);
        assert!(diagnostics.is_empty());
        let (numeric, _) = module
            .functions
            .iter()
            .find(|(_, function)| function.name.as_str() == "numeric")
            .unwrap();
        assert_eq!(types.sigs[numeric].ret, Ty::Bool);
        assert_eq!(types.modes[numeric], crate::FuncMode::Static);
        let variables = crate::module_vars::infer_module_var_types(&module);
        for (id, var) in module
            .vars
            .iter()
            .filter(|(_, var)| matches!(var.name.as_str(), "result" | "chain"))
        {
            assert_eq!(variables[id], Ty::Dynamic, "{}", var.name);
        }
    }
}
