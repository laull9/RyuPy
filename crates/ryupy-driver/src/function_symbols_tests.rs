//! 真实Python→链接MIR→CIL报告：同签名匿名/多scope同名函数保持唯一方法及引用坐标。
use super::*;
use std::collections::BTreeSet;

#[test]
fn emitted_function_symbols_and_delegate_sites_are_unique() {
    let root = test_temp::directory("function-symbols");
    let source = r#"
def left():
    def same(value):
        return value
    return same

def right():
    def same(value):
        return value
    return same

def __module_main():
    return 1

def __thunk_0():
    return 2

def __init_functions():
    return 3

a = lambda x: x + 1
b = lambda x: x + 2
c = [x + 1 for x in [1, 2]]
d = [x + 2 for x in [1, 2]]
e = (x + 1 for x in [1, 2])
f = (x + 2 for x in [1, 2])
"#;
    let entry = root.join("entry.py");
    std::fs::write(&entry, source).unwrap();
    let mut session = Session::default();
    let (linked, _, _) = compile_linked(&mut session, &entry, &[], false, None)
        .unwrap_or_else(|e| panic!("{e}: {}", session.render_diagnostics(false)));
    let artifact = ryupy_codegen_cil::emit_assembly_with_report(
        &linked,
        &ryupy_codegen_cil::EmitOptions {
            assembly_name: "function_symbols".into(),
            entry_source_path: None,
        },
    )
    .unwrap();
    assert!(artifact.bytes.starts_with(b"MZ"));
    let definitions = &artifact.report.function_definitions;
    assert_eq!(definitions.len(), linked.functions.len());
    assert_eq!(
        definitions.len(),
        definitions
            .iter()
            .map(|f| &f.core)
            .collect::<BTreeSet<_>>()
            .len()
    );
    let mut sites = BTreeSet::new();
    for reference in &artifact.report.delegate_references {
        assert!(
            sites.insert((&reference.source, reference.instruction)),
            "{reference:?}"
        );
    }
    assert!(
        artifact
            .report
            .delegate_references
            .iter()
            .any(|r| r.role == "generator_resume")
    );
    assert!(
        artifact
            .report
            .delegate_references
            .iter()
            .any(|r| r.role == "callable_thunk")
    );
    assert_eq!(
        linked
            .functions
            .iter()
            .filter(|(_, f)| f.name == "same")
            .count(),
        2
    );
    std::fs::remove_dir_all(root).unwrap();
}

#[test]
fn source_aot_pe_kind_preserves_the_same_emission_report() {
    let root = test_temp::directory("source-aot-pe-kind");
    let entry = root.join("真实 entry.py");
    std::fs::write(&entry, "def f(x):\n    return x + 1\nprint(f(2))\n").unwrap();
    let mut session = Session::default();
    let (linked, _, _) = compile_linked(&mut session, &entry, &[], false, None).unwrap();
    let options = ryupy_codegen_cil::EmitOptions {
        assembly_name: "真实 entry".into(),
        entry_source_path: Some(entry.to_string_lossy().into_owned()),
    };
    let dll = ryupy_codegen_cil::emit_source_assembly_with_report(&linked, &options).unwrap();
    let aot = ryupy_codegen_cil::emit_source_aot_assembly_with_report(&linked, &options).unwrap();
    assert_eq!(dll.report, aot.report);
    for (bytes, expected_dll) in [(&dll.bytes, true), (&aot.bytes, false)] {
        let pe = u32::from_le_bytes(bytes[0x3c..0x40].try_into().unwrap()) as usize;
        let flags = u16::from_le_bytes(bytes[pe + 22..pe + 24].try_into().unwrap());
        assert_eq!(flags & 0x2000 != 0, expected_dll);
    }
    assert!(!aot.report.delegate_references.is_empty());
    std::fs::remove_dir_all(root).unwrap();
}
