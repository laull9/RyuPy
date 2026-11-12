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
