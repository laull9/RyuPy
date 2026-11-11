//! 词法类工厂的固定CLR ABI；namespace为私有frame，不能与旧PyClass参数混用。
use crate::{MirType, RuntimeFn};
pub(crate) fn signature(runtime: RuntimeFn) -> Option<(&'static [MirType], MirType)> {
    use MirType::{Bool, Dynamic, Str, Unit};
    Some(match runtime {
        RuntimeFn::LexicalClassPrepare => (&[Str, Str, Dynamic, Dynamic], Dynamic),
        RuntimeFn::LexicalClassFinish => (&[Dynamic], Dynamic),
        RuntimeFn::LexicalNamespaceContains => (&[Dynamic, Str], Bool),
        RuntimeFn::LexicalNamespaceGet => (&[Dynamic, Str], Dynamic),
        RuntimeFn::LexicalNamespaceSet => (&[Dynamic, Str, Dynamic], Unit),
        _ => return None,
    })
}
