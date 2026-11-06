//! 注解object ABI的透传白名单、消费回退和独立数值循环。
use crate::{FuncMode, Ty};
use ryupy_base::SourceDb;

fn check(source: &str) -> (ryupy_hir::Module, crate::ModuleTypes) {
    let mut db = SourceDb::default();
    let file = db.add("opaque.py", source);
    let (parsed, errors) = ryupy_syntax::parse(file, source);
    assert!(errors.is_empty(), "{errors:?}");
    let (hir, errors) = ryupy_hir::lower_module(
        &parsed,
        &ryupy_hir::ModuleInfo {
            name: "__main__".into(),
            package: String::new(),
            file: "opaque.py".into(),
            instance_globals: false,
            resolve_file_at_runtime: false,
        },
        source,
    );
    assert!(errors.is_empty(), "{errors:?}");
    let (types, errors) = crate::check_module(&hir);
    assert!(errors.is_empty(), "{errors:?}");
    (hir, types)
}

#[test]
fn opaque_pass_through_keeps_exact_locals_and_independent_numeric_loop() {
    let source = "def identity(s: str) -> str:\n    exact = 'a' + 'b'\n    number = 4 + 5\n    copy = s\n    return copy\ndef branch(s: str, other: str, flag: bool) -> str:\n    if flag:\n        value = s\n    else:\n        value = other\n    return value\ndef loop(s: str, n: int) -> int:\n    total = 0\n    for i in range(n):\n        total += i\n    return total\ndef literal() -> str:\n    return 'literal'\ndef caller() -> str:\n    return identity(42)\ndef numeric(n: int) -> int:\n    return n + 1\n";
    let (hir, types) = check(source);
    for (id, function) in hir.functions.iter() {
        if id == hir.main {
            continue;
        }
        assert_eq!(types.mode(id), FuncMode::Static, "{}", function.name);
        if function.name.as_str() == "identity" {
            assert_eq!(types.sigs[id].params, [Ty::OpaqueStr]);
            assert_eq!(types.sigs[id].ret, Ty::OpaqueStr);
            for (local, decl) in function.body.locals.iter() {
                let expected = match decl.name.as_str() {
                    "exact" => Ty::Str,
                    "number" => Ty::Int,
                    _ => Ty::OpaqueStr,
                };
                assert_eq!(types.body(id).unwrap().local_ty(local), expected);
            }
        }
        if function.name.as_str() == "loop" {
            assert_eq!(types.sigs[id].params, [Ty::OpaqueStr, Ty::Int]);
            assert_eq!(types.sigs[id].ret, Ty::Int);
        }
        if function.name.as_str() == "literal" || function.name.as_str() == "caller" {
            // 返回标注不是exact证据；调用者不得窃取string签名。
            assert_eq!(types.sigs[id].ret, Ty::OpaqueStr);
        }
    }
}

#[test]
fn opaque_consumers_fall_back_even_when_the_result_is_boolean() {
    let source = "def truth(s: str) -> int:\n    if s:\n        return 1\n    return 0\ndef negative(s: str) -> bool:\n    return not s\ndef equal(s: str) -> bool:\n    return s == 'x'\ndef same(s: str) -> bool:\n    return s is s\ndef both(s: str) -> str:\n    return s and s\ndef select(s: str) -> str:\n    return s if s else s\ndef attribute(s: str) -> int:\n    return s.value\ndef index(s: str) -> str:\n    return s[0]\ndef call(s: str) -> str:\n    return s()\ndef convert(s: str) -> str:\n    return str(s)\ndef formatted(s: str) -> str:\n    return f'{s}'\ndef wrong_return(s: str) -> int:\n    return s\ndef stale(s: str) -> str:\n    value = 'x'\n    earlier = value + 'y'\n    value = s\n    return value\ndef loop_read(s: str, n: int) -> str:\n    for i in range(n):\n        copied = s\n    return s\ndef default(s: str = 'x') -> str:\n    return s\n";
    let (hir, types) = check(source);
    for (id, function) in hir.functions.iter() {
        if id == hir.main {
            continue;
        }
        assert_eq!(types.mode(id), FuncMode::Dynamic, "{}", function.name);
    }
}

#[test]
fn annotated_public_slots_and_dataclass_fields_are_objects_not_exact_strings() {
    let source = "from dataclasses import dataclass\nslot: str = 'x'\n@dataclass\nclass Box:\n    value: str\ndef read(box: Box) -> str:\n    return box.value\ndef make() -> Box:\n    return Box('x')\n";
    let (hir, types) = check(source);
    let make = hir
        .functions
        .iter()
        .find(|(_, f)| f.name.as_str() == "make")
        .unwrap()
        .0;
    // 源码dataclass沿既有运行时装饰路径；合法构造无诊断，不扩大静态布局。
    assert!(
        hir.classes
            .iter()
            .any(|(_, c)| c.name.as_str() == "Box" && c.runtime_dataclass)
    );
    assert_eq!(types.mode(make), FuncMode::Dynamic);
    let slot = hir
        .vars
        .iter()
        .find(|(_, v)| v.name.as_str() == "slot")
        .unwrap()
        .0;
    assert!(matches!(
        types.module_vars[slot],
        Ty::OpaqueStr | Ty::Dynamic
    ));
    assert_eq!(
        crate::type_ref_to_ty(&ryupy_hir::TypeRef::Str),
        Ty::OpaqueStr
    );
    let source = "counter: str = 'x'\nfor counter in range(2):\n    pass\n";
    let (hir, types) = check(source);
    assert_eq!(types.mode(hir.main), FuncMode::Dynamic);
}
