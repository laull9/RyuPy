//! 词法类冷ABI的真实PE引用签名；不依赖HIR或类型层。
use dotnetdll::prelude::*;
#[test]
fn lexical_class_factory_references_have_exact_clr_signatures() {
    let program = crate::exact_i64_tests::program();
    let bytes = crate::emit_assembly(
        &program,
        &crate::EmitOptions {
            assembly_name: "LexicalClassAbiProbe".into(),
            entry_source_path: None,
        },
    )
    .unwrap();
    let res = Resolution::parse(&bytes, ReadOptions::default()).unwrap();
    for (name, expected) in [
        (
            "PrepareOrdinaryMiClass",
            msig! { static object(string, object[]) },
        ),
        (
            "PrepareRuntimeDerivedClass",
            msig! { static object(string, object[]) },
        ),
        (
            "Prepare",
            msig! { static object(string, string, object, object[]) },
        ),
        ("Finish", msig! { static object(object) }),
        ("NamespaceContains", msig! { static bool(object, string) }),
        ("NamespaceGet", msig! { static object(object, string) }),
        (
            "NamespaceSet",
            msig! { static void(object, string, object) },
        ),
    ] {
        assert!(
            res.method_references
                .iter()
                .any(|method| method.name == name && method.signature == expected),
            "{name}"
        );
    }
}
