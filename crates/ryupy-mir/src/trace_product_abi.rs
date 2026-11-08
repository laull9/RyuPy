//! 产品 trace ABI 的 MIR 类型合同；固定参数与返回类型不能靠后端调用名猜测。
use crate::{MirType, RuntimeFn};

pub(crate) fn signature(runtime: RuntimeFn) -> Option<(&'static [MirType], MirType)> {
    use MirType::{Dynamic, I64, Unit};
    Some(match runtime {
        RuntimeFn::TraceV1Explicit => (&[Dynamic], Dynamic),
        RuntimeFn::TraceV1Park => (&[Dynamic, I64], Dynamic),
        RuntimeFn::TraceV1Match | RuntimeFn::TraceV1Abort | RuntimeFn::TraceV1AllMiss
            | RuntimeFn::TraceV1GeneratorCatch => (&[Dynamic], Unit),
        RuntimeFn::TraceV1Handle | RuntimeFn::TraceV1Forward => (&[Dynamic, I64], Unit),
        RuntimeFn::TraceV1GeneratorDepth => (&[], I64),
        _ => return None,
    })
}
