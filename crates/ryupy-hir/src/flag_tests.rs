//! Flag仅对真实稳定原生导入启用专用构建；有限非负成员表达式不能扩大到任意元类执行。
use crate::{ClassBase, ModuleInfo, NativeBase, lower_module_with_native_exceptions};
fn lower(source: &str, native: &[&str]) -> (crate::Module, Vec<ryupy_base::Diagnostic>) {
    let mut db = ryupy_base::SourceDb::default();
    let file = db.add("flags.py", source);
    let (parsed, errors) = ryupy_syntax::parse(file, source);
    assert!(errors.is_empty(), "{errors:?}");
    lower_module_with_native_exceptions(
        &parsed,
        &ModuleInfo {
            instance_capabilities: Default::default(),
            instance_globals: false,
            resolve_file_at_runtime: false,
            name: "__main__".into(),
            package: String::new(),
            file: "flags.py".into(),
        },
        source,
        native,
    )
}
#[test]
fn flag_aliases_keep_raw_member_order_and_delayed_binding() {
    let (module, errors) = lower(
        "from enum import Flag as Base\nclass Bits(Base):\n    ZERO = 0\n    A = 1\n    B = 2\n    BOTH = A | B\n    COPY = BOTH\n    MASK = (A ^ B) & BOTH\n",
        &["enum"],
    );
    assert!(errors.is_empty(), "{errors:?}");
    let (_, cls) = module.classes.iter().next().unwrap();
    assert!(matches!(
        cls.base,
        Some(ClassBase::Native {
            kind: NativeBase::Flag,
            ..
        })
    ));
    assert!(cls.decorated_binding.is_some());
    assert_eq!(
        cls.enum_members
            .as_ref()
            .unwrap()
            .iter()
            .map(|n| n.as_str())
            .collect::<Vec<_>>(),
        ["ZERO", "A", "B", "BOTH", "COPY", "MASK"]
    );
    assert_eq!(module.protected_exports, ["Base"]);
    let (module, errors) = lower(
        "import enum as enums\nclass Bits(enums.Flag):\n    A = True\n",
        &["enum"],
    );
    assert!(errors.is_empty(), "{errors:?}");
    assert_eq!(module.protected_exports, ["enums"]);
}
#[test]
fn unsupported_flag_members_and_class_options_are_explicit() {
    for body in [
        "A = -1",
        "A = ~0",
        "A = 1 << 2",
        "A = 1 + 2",
        "A = 's'",
        "A = None",
        "A = 1.0",
        "A = auto()",
        "A = Missing | 1",
        "A = 1\n    A = 2",
    ] {
        let (_, errors) = lower(
            &format!("from enum import Flag, auto\nclass Bits(Flag):\n    {body}\n"),
            &["enum"],
        );
        assert!(
            errors.iter().any(|e| e.code == Some("E0100")),
            "{body}: {errors:?}"
        );
    }
    for header in ["Flag, boundary=1", "int, Flag", "Flag, int"] {
        let (_, errors) = lower(
            &format!("from enum import Flag\nclass Bits({header}):\n    A = 1\n"),
            &["enum"],
        );
        assert!(
            errors.iter().any(|e| e.code == Some("E0100")),
            "{header}: {errors:?}"
        );
    }
}
#[test]
fn local_module_or_rebound_alias_never_selects_flag_protocol() {
    for (source, native) in [
        (
            "from enum import Flag\nclass Bits(Flag):\n    A = 1\n",
            &[][..],
        ),
        (
            "from enum import Flag\nFlag = object\nclass Bits(Flag):\n    A = 1\n",
            &["enum"][..],
        ),
    ] {
        let (module, errors) = lower(source, native);
        assert!(errors.is_empty(), "{errors:?}");
        assert!(module.classes.iter().all(
            |(_, c)| matches!(c.base, Some(ClassBase::Runtime(_))) && c.enum_members.is_none()
        ));
        assert!(module.protected_exports.is_empty());
    }
}
