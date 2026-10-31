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
//!
//! TODO(M1.0)：实现到 `dotnetdll::prelude::MethodType` / `ReturnType` / `LocalVariable` 的转换。
