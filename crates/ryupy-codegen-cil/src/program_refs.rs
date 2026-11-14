//! 按LinkedProgram实际需要建立可选引用；旧引用建立次序和未选路径metadata保持不变。
use dotnetdll::prelude::*;
use ryupy_mir::LinkedProgram;

pub(crate) fn initialize(
    res: &mut Resolution<'static>,
    program: &LinkedProgram,
    runtime: AssemblyRefIndex,
    refs: &mut crate::stackify_refs::RuntimeRefs,
) {
    refs.trace_product =
        crate::trace_product_refs::TraceProductRefs::for_program(res, program, runtime);
    refs.instances = crate::instance_refs::for_program(res, program, runtime);
    refs.owned_functions =
        crate::owned_function_refs::OwnedFunctionRefs::for_program(res, program, runtime);
    // 纯标量程序不引入向量程序集/泛型引用，避免无关冷模块和快照扩大。
    if crate::vector_refs::is_required(program) {
        refs.vectors = Some(crate::vector_refs::VectorRefs::build(res));
    }
    refs.class_cell_guard = crate::class_cell_refs::for_program(res, program, runtime);
    refs.zero_call_frame = crate::lexical_super::for_program(res, program, runtime);
    refs.deletions = crate::module_deletion_refs::DeletionRefs::for_program(res, program, runtime);
}
