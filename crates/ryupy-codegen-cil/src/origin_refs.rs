//! Dynamic函数与模块执行来源的机械CLR引用；Static帧继续使用既有PushFrame未知来源入口。

use dotnetdll::prelude::*;

pub(crate) struct OriginRefs {
    pub push_frame: MethodRefIndex,
    pub module_source: MethodRefIndex,
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
        }
    }
}
