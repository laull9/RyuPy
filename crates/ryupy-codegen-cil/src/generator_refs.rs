//! 生成器构造 ABI 的 CIL 引用。恢复委托只接收 MIR 已准备好的堆帧。

use dotnetdll::prelude::*;

pub(crate) fn build(
    res: &mut Resolution<'static>,
    runtime: AssemblyRefIndex,
) -> (MethodRefIndex, MethodRefIndex) {
    let resume = res.push_type_reference(type_ref! { RyuPy.Runtime.PyResume in #runtime });
    let generator = res.push_type_reference(type_ref! { RyuPy.Runtime.PyGenerator in #runtime });
    let resume: MethodType = BaseType::class(resume).into();
    let generator: MethodType = BaseType::class(generator).into();
    let delegate_ctor =
        res.push_method_reference(method_ref! { void @resume::.ctor(object, nint) });
    let generator_ctor = res.push_method_reference(method_ref! {
        void @generator::.ctor(@resume, object[], string, string)
    });
    (delegate_ctor, generator_ctor)
}
