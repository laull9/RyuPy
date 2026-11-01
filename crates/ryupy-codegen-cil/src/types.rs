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
//! | `Dynamic` | `object`（装箱值，见 ADR-0010） |

use dotnetdll::prelude::*;
use rustc_hash::FxHashMap;
use ryupy_mir::{ClassDefId, MirType};

/// 生成的类型索引表。
///
/// `object` 单独放一个字段，而不是塞进 `classes`：动态值的 CIL 类型是
/// `System.Object`，不属于任何用户类（ADR-0010）。
#[derive(Debug)]
pub struct ClassTypeMap {
    /// `System.Object` 的类型引用（外部类型，用 TypeRefIndex 表示）。
    pub object: TypeRefIndex,
    /// BCL私有向量局部的泛型值类型定义，不属于用户class。
    pub vector: Option<TypeRefIndex>,
    pub classes: FxHashMap<ClassDefId, (TypeIndex, bool)>,
}

impl ClassTypeMap {
    pub fn new(object: TypeRefIndex, vector: Option<TypeRefIndex>) -> Self {
        Self {
            object,
            vector,
            classes: FxHashMap::default(),
        }
    }

    pub fn get(&self, id: ClassDefId) -> (TypeIndex, bool) {
        self.classes[&id]
    }
}

pub fn mir_type_to_base_type(
    ty: &MirType,
    class_types: &ClassTypeMap,
) -> Option<BaseType<MethodType>> {
    match ty {
        MirType::I64 => Some(BaseType::Int64),
        MirType::F64 => Some(BaseType::Float64),
        MirType::V128F64 => Some(BaseType::valuetype(TypeSource::generic(
            class_types.vector?,
            vec![BaseType::Float64.into()],
        ))),
        MirType::Bool => Some(BaseType::Boolean),
        MirType::Str => Some(BaseType::String),
        MirType::Unit => None,
        MirType::Dynamic => Some(BaseType::class(class_types.object)),
        MirType::Class(id) => {
            let (type_idx, is_value_type) = class_types.get(*id);
            if is_value_type {
                Some(BaseType::valuetype(type_idx))
            } else {
                Some(BaseType::class(type_idx))
            }
        }
    }
}

// `Dynamic` 分支需要 `.into()` 把 `BaseType<MethodType>` 变成 `MemberType`；
// clippy 会把这次转换误判成恒等转换，故就地允许。
#[allow(clippy::useless_conversion)]
pub fn mir_type_to_member_type(ty: &MirType, class_types: &ClassTypeMap) -> Option<MemberType> {
    match ty {
        MirType::I64 => Some(BaseType::Int64.into()),
        MirType::F64 => Some(BaseType::Float64.into()),
        MirType::V128F64 => Some(
            BaseType::valuetype(TypeSource::generic(
                class_types.vector?,
                vec![BaseType::Float64.into()],
            ))
            .into(),
        ),
        MirType::Bool => Some(BaseType::Boolean.into()),
        MirType::Str => Some(BaseType::String.into()),
        MirType::Unit => None,
        MirType::Dynamic => Some(BaseType::class(class_types.object).into()),
        MirType::Class(id) => {
            let (type_idx, is_value_type) = class_types.get(*id);
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
