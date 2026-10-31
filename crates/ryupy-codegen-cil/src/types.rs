//! `MirType` → CIL 类型。
//!
//! | MirType | CIL |
//! | --- | --- |
//! | `I64` | `int64` |
//! | `F64` | `float64` |
//! | `Bool` | `bool` |
//! | `Str` | `string` |
//! | `Unit` | `void`（仅返回类型） |
//! | `Class(id)` | 生成的 `class` / `valuetype` |

use dotnetdll::prelude::*;
use ryupy_mir::MirType;

pub fn mir_type_to_base_type(ty: &MirType) -> Option<BaseType<MethodType>> {
    match ty {
        MirType::I64 => Some(BaseType::Int64),
        MirType::F64 => Some(BaseType::Float64),
        MirType::Bool => Some(BaseType::Boolean),
        MirType::Str => Some(BaseType::String),
        MirType::Unit => None,
        MirType::Class(_) => None,
    }
}

pub fn mir_type_to_local_var(ty: &MirType) -> Option<LocalVariable> {
    mir_type_to_base_type(ty).map(|b| LocalVariable::new(b.into()))
}
