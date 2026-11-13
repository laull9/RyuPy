//! 内部函数符号由稳定HIR身份区分；同名作用域和匿名函数不能压成同一PE方法。
use crate::*;
use std::collections::BTreeSet;

fn compile(source: &str) -> (ryupy_hir::Module, Program) {
    let mut db = ryupy_base::SourceDb::default();
    let file = db.add("symbols.py", source);
    let (parsed, diagnostics) = ryupy_syntax::parse(file, source);
    assert!(diagnostics.is_empty(), "{diagnostics:?}");
    let (hir, diagnostics) = ryupy_hir::lower_module(&parsed, &Default::default(), source);
    assert!(diagnostics.is_empty(), "{diagnostics:?}");
    let (types, diagnostics) = ryupy_ty::check_module(&hir);
    assert!(diagnostics.is_empty(), "{diagnostics:?}");
    let (program, _, diagnostics) = build_program(&hir, &types);
    assert!(diagnostics.is_empty(), "{diagnostics:?}");
    (hir, program)
}

const SOURCE: &str = r#"
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

a = lambda x: x + 1
b = lambda x: x + 2
c = [x + 1 for x in [1, 2]]
d = [x + 2 for x in [1, 2]]
e = (x + 1 for x in [1, 2])
f = (x + 2 for x in [1, 2])
"#;

#[test]
fn stable_internal_symbols_keep_python_names_and_lexical_identity() {
    let (hir, program) = compile(SOURCE);
    let (_, again) = compile(SOURCE);
    let symbols: Vec<_> = program
        .functions
        .iter()
        .map(|(_, f)| &f.codegen_name)
        .collect();
    assert_eq!(
        symbols,
        again
            .functions
            .iter()
            .map(|(_, f)| &f.codegen_name)
            .collect::<Vec<_>>()
    );
    assert_eq!(
        symbols.len(),
        symbols.iter().copied().collect::<BTreeSet<_>>().len()
    );
    assert_eq!(
        program.functions[program.entry].codegen_name,
        "__module_main"
    );
    for (id, source) in hir.functions.iter() {
        if id == hir.main {
            continue;
        }
        let expected = format!("{}$f{}", source.name, id.into_raw().into_u32());
        let function = program
            .functions
            .iter()
            .find(|(_, f)| f.codegen_name == expected)
            .unwrap()
            .1;
        assert_eq!(function.name, source.name.as_str());
        assert_eq!(
            function
                .definition
                .as_ref()
                .map(|d| d.lexical_qualname.as_str()),
            source
                .definition
                .as_ref()
                .map(|d| d.lexical_qualname.as_str())
        );
    }
    let same: Vec<_> = program
        .functions
        .iter()
        .filter(|(_, f)| f.name == "same")
        .map(|(_, f)| f)
        .collect();
    assert_eq!(same.len(), 2);
    assert_ne!(same[0].codegen_name, same[1].codegen_name);
    assert_ne!(same[0].definition, same[1].definition);
    for name in ["<lambda>", "<listcomp>", "<genexpr>"] {
        assert!(
            program
                .functions
                .iter()
                .filter(|(_, f)| f.name == name)
                .count()
                >= 2
        );
    }
    assert!(
        program
            .functions
            .iter()
            .all(|(_, f)| f.codegen_name != "__thunk_0")
    );
    validate::validate(&program).unwrap();
}
