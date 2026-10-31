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
use rustc_hash::FxHashMap;
use ryupy_mir::{ClassDefId, MirType};

pub type ClassTypeMap = FxHashMap<ClassDefId, (TypeIndex, bool)>;

pub fn mir_type_to_base_type(
    ty: &MirType,
    class_types: &ClassTypeMap,
) -> Option<BaseType<MethodType>> {
    match ty {
        MirType::I64 => Some(BaseType::Int64),
        MirType::F64 => Some(BaseType::Float64),
        MirType::Bool => Some(BaseType::Boolean),
        MirType::Str => Some(BaseType::String),
        MirType::Unit => None,
        MirType::Class(id) => {
            let (type_idx, is_value_type) = class_types[id];
            if is_value_type {
                Some(BaseType::valuetype(type_idx))
            } else {
                Some(BaseType::class(type_idx))
            }
        }
    }
}

pub fn mir_type_to_member_type(ty: &MirType, class_types: &ClassTypeMap) -> Option<MemberType> {
    match ty {
        MirType::I64 => Some(BaseType::Int64.into()),
        MirType::F64 => Some(BaseType::Float64.into()),
        MirType::Bool => Some(BaseType::Boolean.into()),
        MirType::Str => Some(BaseType::String.into()),
        MirType::Unit => None,
        MirType::Class(id) => {
            let (type_idx, is_value_type) = class_types[id];
            if is_value_type {
                Some(BaseType::valuetype(type_idx).into())
            } else {
                Some(BaseType::class(type_idx).into())
            }
        }
    }
}

pub fn mir_type_to_method_type(ty: &MirType, class_types: &ClassTypeMap) -> Option<MethodType> {
    mir_type_to_base_type(ty, class_types).map(Into::into)
}

pub fn mir_type_to_return_type(ty: &MirType, class_types: &ClassTypeMap) -> ReturnType<MethodType> {
    match mir_type_to_method_type(ty, class_types) {
        Some(mt) => ReturnType::value(mt),
        None => ReturnType::VOID,
    }
}

pub fn mir_type_to_parameter(
    ty: &MirType,
    class_types: &ClassTypeMap,
) -> Option<Parameter<MethodType>> {
    mir_type_to_method_type(ty, class_types).map(Parameter::value)
}

pub fn mir_type_to_local_var(ty: &MirType, class_types: &ClassTypeMap) -> Option<LocalVariable> {
    mir_type_to_base_type(ty, class_types).map(|b| LocalVariable::new(b.into()))
}
