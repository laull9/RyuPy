//! 异步迭代固定ABI；后端和验证器只处理object协议，不读取HIR或Python对象结构。
use crate::{MirType, RuntimeFn};
pub(crate) fn signature(runtime: RuntimeFn) -> Option<(&'static [MirType], MirType)> {
    match runtime {
        RuntimeFn::AsyncIterStart
        | RuntimeFn::AsyncIterNext
        | RuntimeFn::AsyncIterIsStop
        | RuntimeFn::CoroutineNextAwaitStart => Some((&[MirType::Dynamic], MirType::Dynamic)),
        _ => None,
    }
}
