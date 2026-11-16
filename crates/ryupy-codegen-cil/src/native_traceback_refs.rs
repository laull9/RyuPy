//! 原生traceback冷传播ABI引用；后端只发射中端已经区分的显式raise/真实接收操作。
use dotnetdll::prelude::*;

pub(crate) struct NativeTracebackRefs {
    pub explicit_raise: MethodRefIndex,
    pub receive: MethodRefIndex,
    pub generator_receive: MethodRefIndex,
    pub of: MethodRefIndex,
    pub handled: MethodRefIndex,
    pub enter_guard: MethodRefIndex,
}
impl NativeTracebackRefs {
    pub(crate) fn build(res: &mut Resolution<'static>, runtime: AssemblyRefIndex) -> Self {
        let ops = res.push_type_reference(type_ref! { RyuPy.Runtime.PyOps in #runtime });
        let ops: MethodType = BaseType::class(ops).into();
        let errors = res.push_type_reference(type_ref! { RyuPy.Runtime.PyErrors in #runtime });
        let errors: MethodType = BaseType::class(errors).into();
        Self {
            handled: res.push_method_reference(method_ref! { static void @ops::TracebackHandled(object) }),
            enter_guard: res.push_method_reference(method_ref! { static object @errors::EnterHandlerGuard(object) }),
            explicit_raise: res.push_method_reference(method_ref! { static object @errors::PrepareExplicitRaise(object) }),
            receive: res.push_method_reference(method_ref! { static void @ops::TracebackReceive(object, long) }),
            generator_receive: res.push_method_reference(method_ref! { static void @ops::GeneratorTracebackReceive(object, object) }),
            of: res.push_method_reference(method_ref! { static object @ops::TracebackOf(object) }),
        }
    }
}
