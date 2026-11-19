//! 产品 V1 的版本化引用。仅含通过整产物校验的证书时建立，不改变旧产物引用顺序。
use dotnetdll::prelude::*;
use ryupy_mir::{LinkedProgram, RuntimeFn};

pub(crate) struct TraceProductRefs {
    pub enter: MethodRefIndex,
    calls: Vec<(RuntimeFn, MethodRefIndex)>,
}

impl TraceProductRefs {
    pub fn for_program(
        res: &mut Resolution<'static>,
        program: &LinkedProgram,
        runtime: AssemblyRefIndex,
    ) -> Option<Self> {
        program.trace_product.as_ref()?;
        let ty = res.push_type_reference(type_ref! { RyuPy.Runtime.PyTraceProduct in #runtime });
        let ty: MethodType = BaseType::class(ty).into();
        Some(Self {
            enter: res.push_method_reference(method_ref! { static void @ty::Enter(long) }),
            calls: vec![
                (
                    RuntimeFn::TraceV1Explicit,
                    res.push_method_reference(method_ref! { static object @ty::Explicit(object) }),
                ),
                (
                    RuntimeFn::TraceV1Park,
                    res.push_method_reference(
                        method_ref! { static object @ty::Park(object, long) },
                    ),
                ),
                (
                    RuntimeFn::TraceV1Match,
                    res.push_method_reference(method_ref! { static void @ty::Match(object) }),
                ),
                (
                    RuntimeFn::TraceV1Abort,
                    res.push_method_reference(method_ref! { static void @ty::Abort(object) }),
                ),
                (
                    RuntimeFn::TraceV1AllMiss,
                    res.push_method_reference(method_ref! { static void @ty::AllMiss(object) }),
                ),
                (
                    RuntimeFn::TraceV1Handle,
                    res.push_method_reference(
                        method_ref! { static void @ty::Handle(object, long) },
                    ),
                ),
                (
                    RuntimeFn::TraceV1Forward,
                    res.push_method_reference(
                        method_ref! { static void @ty::Forward(object, long) },
                    ),
                ),
                (
                    RuntimeFn::TraceV1GeneratorCatch,
                    res.push_method_reference(
                        method_ref! { static void @ty::GeneratorCatch(object) },
                    ),
                ),
                (
                    RuntimeFn::TraceV1GeneratorDepth,
                    res.push_method_reference(method_ref! { static long @ty::GeneratorDepth() }),
                ),
            ],
        })
    }

    pub fn get(&self, runtime: RuntimeFn) -> MethodRefIndex {
        self.calls
            .iter()
            .find(|(kind, _)| *kind == runtime)
            .expect("已验证的产品 trace ABI")
            .1
    }
}
