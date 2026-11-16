//! 中性固定ABI的统一入口；分域签名留在专属模块，避免验证器继续堆叠并复制匹配表。
use crate::{Constant, MirType, Operand, RuntimeFn};
pub(crate) fn signature(runtime: RuntimeFn) -> Option<(&'static [MirType], MirType)> {
    if runtime == RuntimeFn::FunctionIntrospectionKind {
        return Some((&[MirType::Dynamic, MirType::I64], MirType::Unit));
    }
    crate::trace_product_abi::signature(runtime)
        .or_else(|| crate::lexical_class_abi::signature(runtime))
        .or_else(|| crate::async_iteration_abi::signature(runtime))
}

pub(crate) fn valid_values(runtime: RuntimeFn, args: &[Operand]) -> bool {
    runtime != RuntimeFn::FunctionIntrospectionKind
        || matches!(args.get(1), Some(Operand::Const(Constant::I64(0..=4))))
}

#[cfg(test)]
mod tests {
    use super::*;
    #[test]
    fn function_kind_requires_a_frozen_known_encoding() {
        for kind in 0..=4 {
            assert!(valid_values(
                RuntimeFn::FunctionIntrospectionKind,
                &[
                    Operand::Const(Constant::None),
                    Operand::Const(Constant::I64(kind))
                ]
            ));
        }
        for kind in [-1, 5, i64::MAX] {
            assert!(!valid_values(
                RuntimeFn::FunctionIntrospectionKind,
                &[
                    Operand::Const(Constant::None),
                    Operand::Const(Constant::I64(kind))
                ]
            ));
        }
    }
}
