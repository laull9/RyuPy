//! 生成器构造 ABI 的 CIL 引用。恢复委托只接收 MIR 已准备好的堆帧。

use dotnetdll::prelude::*;

pub(crate) struct GeneratorRefs {
    pub state_ctor: MethodRefIndex,
    pub sum_state_ctor: MethodRefIndex,
    pub sum_resume_ctor: MethodRefIndex,
    pub sum_try_consume: MethodRefIndex,
    pub sum_range_guard: MethodRefIndex,
    pub state_data: MethodRefIndex,
    pub state_pc: MethodRefIndex,
    pub state_set_pc: MethodRefIndex,
    pub resume_ctor: MethodRefIndex,
    pub ctor: MethodRefIndex,
    pub ctor_with_origin: MethodRefIndex,
    pub input: MethodRefIndex,
    pub retry: MethodRefIndex,
}

impl GeneratorRefs {
    pub(crate) fn build(res: &mut Resolution<'static>, runtime: AssemblyRefIndex) -> Self {
        let resume = res.push_type_reference(type_ref! { RyuPy.Runtime.PyResume in #runtime });
        let generator =
            res.push_type_reference(type_ref! { RyuPy.Runtime.PyGenerator in #runtime });
        let sum_resume = res
            .push_type_reference(type_ref! { RyuPy.Runtime.PyResumeSumCompiled in #runtime });
        let sum_resume: MethodType = BaseType::class(sum_resume).into();
        let resume: MethodType = BaseType::class(resume).into();
        let generator: MethodType = BaseType::class(generator).into();
        let ops = res.push_type_reference(type_ref! { RyuPy.Runtime.PyOps in #runtime });
        let ops: MethodType = BaseType::class(ops).into();
        Self {
            state_ctor: res.push_method_reference(method_ref! {
                void @generator::.ctor(@resume, object, long, string, string, object)
            }),
            sum_state_ctor: res.push_method_reference(method_ref! {
                void @generator::.ctor(@resume, object, long, string, string, object, @sum_resume)
            }),
            sum_resume_ctor: res.push_method_reference(
                method_ref! { void @sum_resume::.ctor(object, nint) },
            ),
            sum_try_consume: res.push_method_reference(
                method_ref! { static bool @ops::GeneratorSumTryConsume(object, object) },
            ),
            sum_range_guard: res.push_method_reference(
                method_ref! { static bool @ops::GeneratorSumRangeGuard(object) },
            ),
            state_data: res.push_method_reference(
                method_ref! { static object @ops::GeneratorStateData(object) },
            ),
            state_pc: res.push_method_reference(
                method_ref! { static long @ops::GeneratorStateGetPC(object) },
            ),
            state_set_pc: res.push_method_reference(
                method_ref! { static void @ops::GeneratorStateSetPC(object, long) },
            ),
            resume_ctor: res
                .push_method_reference(method_ref! { void @resume::.ctor(object, nint) }),
            // 无来源的手工MIR保留既有四参ABI，真实工厂显式传同一个owner引用。
            ctor: res.push_method_reference(method_ref! {
                void @generator::.ctor(@resume, object[], string, string)
            }),
            ctor_with_origin: res.push_method_reference(method_ref! {
                void @generator::.ctor(@resume, object[], string, string, object)
            }),
            input: res.push_method_reference(method_ref! {
                static object @ops::GeneratorInput(object)
            }),
            retry: res.push_method_reference(method_ref! { static object @ops::GeneratorRetry() }),
        }
    }
}

/// yield-from的四个静态协议入口，不在后端解释委托语义。
pub(crate) struct DelegateRefs {
    pub start: MethodRefIndex,
    pub step: MethodRefIndex,
    pub current: MethodRefIndex,
    pub result: MethodRefIndex,
}
impl DelegateRefs {
    pub(crate) fn build(res: &mut Resolution<'static>, runtime: AssemblyRefIndex) -> Self {
        let ops = res.push_type_reference(type_ref! { RyuPy.Runtime.PyOps in #runtime });
        let ops: MethodType = BaseType::class(ops).into();
        Self {
            start: res.push_method_reference(
                method_ref! { static object @ops::GeneratorDelegateStart(object) },
            ),
            step: res.push_method_reference(
                method_ref! { static bool @ops::GeneratorDelegateStep(object, object) },
            ),
            current: res.push_method_reference(
                method_ref! { static object @ops::GeneratorDelegateCurrent(object) },
            ),
            result: res.push_method_reference(
                method_ref! { static object @ops::GeneratorDelegateResult(object) },
            ),
        }
    }
}
