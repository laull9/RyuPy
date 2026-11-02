//! BCL Vector128<double> 的机械引用；泛型方法保留 MethodSpec，不引入Python数值助手。
use dotnetdll::prelude::*;

pub(crate) struct VectorRefs {
    pub ty: TypeRefIndex,
    pub supported: MethodRefIndex,
    pub pack: MethodRefIndex,
    pub add: GenericMethodInstantiation,
    pub subtract: GenericMethodInstantiation,
    pub multiply: GenericMethodInstantiation,
    pub lane: GenericMethodInstantiation,
}
impl VectorRefs {
    pub(crate) fn build(res: &mut Resolution<'static>) -> Self {
        let assembly = res.push_assembly_reference(crate::assembly_refs::intrinsics());
        let ty = res
            .push_type_reference(type_ref! { System.Runtime.Intrinsics.Vector128<1> in #assembly });
        let owner =
            res.push_type_reference(type_ref! { System.Runtime.Intrinsics.Vector128 in #assembly });
        let owner: MethodType = BaseType::class(owner).into();
        let concrete: MethodType =
            BaseType::valuetype(TypeSource::generic(ty, vec![ctype! { double }])).into();
        let generic: MethodType =
            BaseType::valuetype(TypeSource::generic(ty, vec![ctype! { M0 }])).into();
        let supported = res
            .push_method_reference(method_ref! { static bool @owner::get_IsHardwareAccelerated() });
        let pack = res
            .push_method_reference(method_ref! { static @concrete @owner::Create(double, double) });
        let add = res.push_method_reference(
            method_ref! { static @generic @owner::Add<1>(@generic, @generic) },
        );
        let subtract = res.push_method_reference(
            method_ref! { static @generic @owner::Subtract<1>(@generic, @generic) },
        );
        let multiply = res.push_method_reference(
            method_ref! { static @generic @owner::Multiply<1>(@generic, @generic) },
        );
        let lane = res
            .push_method_reference(method_ref! { static M0 @owner::GetElement<1>(@generic, int) });
        let specialize = |method| GenericMethodInstantiation::new(method, vec![ctype! { double }]);
        Self {
            ty,
            supported,
            pack,
            add: specialize(add),
            subtract: specialize(subtract),
            multiply: specialize(multiply),
            lane: specialize(lane),
        }
    }
}

/// 仅真实使用向量节点的程序建立BCL引用；类型本身也纳入检查以覆盖手工MIR测试。
pub(crate) fn is_required(program: &ryupy_mir::LinkedProgram) -> bool {
    use ryupy_mir::{MirType, Rvalue, Statement};
    program.functions.iter().any(|(_, function)| {
        function
            .body
            .locals
            .iter()
            .any(|(_, local)| local.ty == MirType::V128F64)
            || function.body.blocks.iter().any(|(_, block)| {
                block.stmts.iter().any(|stmt| {
                    matches!(
                        stmt,
                        Statement::Assign {
                            value: Rvalue::IsVector128Supported
                                | Rvalue::VectorPackF64 { .. }
                                | Rvalue::VectorBinaryF64 { .. }
                                | Rvalue::VectorLaneF64 { .. },
                            ..
                        } | Statement::Eval(
                            Rvalue::IsVector128Supported
                                | Rvalue::VectorPackF64 { .. }
                                | Rvalue::VectorBinaryF64 { .. }
                                | Rvalue::VectorLaneF64 { .. }
                        )
                    )
                })
            })
    })
}
