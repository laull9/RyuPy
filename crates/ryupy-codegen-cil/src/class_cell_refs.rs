//! 仅在MIR实际调用class-cell冷守卫时发射精确ABI引用；默认和无cell产物不增加引用。
use dotnetdll::prelude::*;
use ryupy_mir::{Callee, LinkedProgram, RuntimeFn, Rvalue, Statement};
pub(crate) fn for_program(
    res: &mut Resolution<'static>,
    program: &LinkedProgram,
    runtime: AssemblyRefIndex,
) -> Option<MethodRefIndex> {
    let used = program.functions.iter().any(|(_, function)| {
        function.body.blocks.iter().any(|(_, block)| {
            block.stmts.iter().any(|statement| {
                matches!(
                    statement,
                    Statement::Eval(Rvalue::Call {
                        callee: Callee::Runtime(RuntimeFn::InstanceClassCellGuard),
                        ..
                    })
                )
            })
        })
    });
    used.then(|| {
        let owner =
            res.push_type_reference(type_ref! { RyuPy.Runtime.PyModuleInstances in #runtime });
        let owner: MethodType = BaseType::class(owner).into();
        res.push_method_reference(method_ref! { static void @owner::ClassCellGuard(object) })
    })
}
