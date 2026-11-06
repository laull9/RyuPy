//! 已知异常类型的CLR引用表；按MIR公开列表顺序建立，保留既有metadata token。
use dotnetdll::prelude::*;
use rustc_hash::FxHashMap;

pub(crate) fn build(
    res: &mut Resolution<'static>,
    ryupy_runtime: AssemblyRefIndex,
    py_exc_ref: TypeRefIndex,
) -> (
    FxHashMap<String, MethodType>,
    FxHashMap<String, TypeRefIndex>,
) {
    // Python 异常类型名 → CLR 类型：CIL 的类型化 catch 按这些类型匹配。
    // 通常为 `Py<PythonName>`；re.error 显式对应 PyRegexError，不能构造不存在的 Pyerror。
    let mut exception_types: FxHashMap<String, MethodType> = FxHashMap::default();
    let mut exception_type_refs: FxHashMap<String, TypeRefIndex> = FxHashMap::default();
    for type_name in ryupy_mir::EXCEPTION_TYPE_NAMES {
        let type_ref = if *type_name == "BaseException" {
            py_exc_ref
        } else {
            res.push_type_reference(ExternalTypeReference::new(
                Some("RyuPy.Runtime".into()),
                match *type_name {
                    "re.error" => "PyRegexError".to_owned(),
                    "io.UnsupportedOperation" => "PyUnsupportedOperation".to_owned(),
                    "json.JSONDecodeError" => "PyJSONDecodeError".to_owned(),
                    "zipfile.BadZipFile" => "PyBadZipFile".to_owned(),
                    "getopt.GetoptError" => "PyGetoptError".to_owned(),
                    "dataclasses.FrozenInstanceError" => "PyFrozenInstanceError".to_owned(),
                    name => format!("Py{}", name.strip_prefix("optparse.").unwrap_or(name)),
                },
                ResolutionScope::Assembly(ryupy_runtime),
            ))
        };
        exception_types.insert((*type_name).to_string(), BaseType::class(type_ref).into());
        exception_type_refs.insert((*type_name).to_string(), type_ref);
    }
    (exception_types, exception_type_refs)
}
