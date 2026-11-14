//! 真实帧来源的机械CLR引用；新Static传自身signature，旧无来源ABI继续保留。

use dotnetdll::prelude::*;

pub(crate) struct OriginRefs {
    pub push_frame: MethodRefIndex,
    pub module_source: MethodRefIndex,
    pub module_source_with_code: MethodRefIndex,
}

impl OriginRefs {
    pub(crate) fn build(res: &mut Resolution<'static>, traceback: MethodType) -> Self {
        Self {
            push_frame: res.push_method_reference(method_ref! {
                static long @traceback::PushFrameWithOrigin(string, string, object)
            }),
            module_source: res.push_method_reference(method_ref! {
                static object @traceback::CreateModuleExecutionSource(object)
            }),
            module_source_with_code: res.push_method_reference(method_ref! {
                static object @traceback::CreateModuleExecutionSourceWithCode(object, object)
            }),
        }
    }
}
