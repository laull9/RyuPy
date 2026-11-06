//! 实例零参值调用的冷来源描述；不制造frame、不保存入口self，也不修改函数对象布局。
//! 后端只传这些已验证的真实槽，运行时先判断callee身份，再读取cell内容。
use crate::expr_builder::ExprBuilder;
use crate::{LocalId, MirType, Operand, Rvalue, Statement};

#[derive(Debug, Clone, Copy, PartialEq, Eq)]
pub enum SuperClassSource {
    /// 当前函数没有自由__class__；普通同名local/cell不能冒充它。
    Missing,
    /// 本函数真实闭包Env中的隐式定义类cell；槽号由S0来源计划交叉校验。
    Lexical { environment: LocalId, index: usize },
    /// 普通外层局部__class__的freecell尚未纳入此切来源证明，明确能力拒绝。
    UnsupportedFree,
}

#[derive(Debug, Clone, Copy, PartialEq, Eq)]
pub struct ZeroCallFrame {
    pub class: SuperClassSource,
    /// 真实第一个位置形参槽；值/cell由LocalDecl判定，不存调用入口的值快照。
    pub first_parameter: Option<LocalId>,
}

impl ExprBuilder<'_> {
    pub(crate) fn supports_zero_call_frame(&self) -> bool {
        let function = &self.hir.functions[self.current_hir_fn];
        self.hir.info.instance_globals
            && function.kind != ryupy_hir::FunctionKind::ModuleMain
            && !function.is_generator
    }

    pub(crate) fn call_zero_with_frame(&mut self, callee: Operand) -> Operand {
        let function = &self.hir.functions[self.current_hir_fn];
        let first_parameter = function
            .params
            .iter()
            .find(|parameter| {
                matches!(
                    parameter.kind,
                    ryupy_hir::ParamKind::PositionalOnly
                        | ryupy_hir::ParamKind::PositionalOrKeyword
                )
            })
            .map(|parameter| self.local_map[&parameter.local]);
        let class = match function
            .closure
            .free
            .iter()
            .position(|name| name.as_str() == "__class__")
        {
            None => SuperClassSource::Missing,
            Some(index) if function.closure.class_cell.is_some() => {
                let Some(environment) = self.env else {
                    // 内部缺源也按已有诊断协议退出，不把非法前端产物变成panic。
                    let _ = self.env_operand();
                    return self.none_value();
                };
                SuperClassSource::Lexical { environment, index }
            }
            Some(_) => SuperClassSource::UnsupportedFree,
        };
        let dest = self.alloc_temp(MirType::Dynamic);
        self.push_stmt(Statement::Assign {
            dest,
            value: Rvalue::InstanceZeroCall {
                callee,
                frame: ZeroCallFrame {
                    class,
                    first_parameter,
                },
            },
        });
        Operand::Local(dest)
    }
}
