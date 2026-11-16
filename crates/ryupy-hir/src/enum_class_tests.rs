//! Enum首切不能静默放行普通类语义；公开绑定与成员名单在HIR显式保存。
use crate::{ClassBase, ModuleInfo, NativeBase, lower_module_with_native_exceptions};
use ryupy_base::SourceDb;
fn lower(source: &str, native: &[&str]) -> (crate::Module, Vec<ryupy_base::Diagnostic>) {
    let mut db = SourceDb::default();
    let file = db.add("enums.py", source);
    let (parsed, errors) = ryupy_syntax::parse(file, source);
    assert!(errors.is_empty());
    lower_module_with_native_exceptions(
        &parsed,
        &ModuleInfo {
            instance_capabilities: Default::default(),
            instance_globals: false,
            resolve_file_at_runtime: false,
            name: "__main__".into(),
            package: String::new(),
            file: "enums.py".into(),
        },
        source,
        native,
    )
}
#[test]
fn native_enum_records_member_aliases_and_requires_delayed_public_binding() {
    let (module, errors) = lower(
        "from enum import Enum as Base\nclass Values(Base):\n    A = 1\n    B = A\n    C = 'text'\n    D = True\n    E = None\n    F = -2\n    def label(self):\n        return self.name\n",
        &["enum"],
    );
    assert!(errors.is_empty(), "{errors:?}");
    let (_, class) = module.classes.iter().next().unwrap();
    assert!(matches!(
        class.base,
        Some(ClassBase::Native {
            kind: NativeBase::Enum,
            ..
        })
    ));
    assert!(class.decorated_binding.is_some());
    assert!(!class.is_exception);
    assert_eq!(
        class
            .enum_members
            .as_ref()
            .unwrap()
            .iter()
            .map(|name| name.as_str())
            .collect::<Vec<_>>(),
        ["A", "B", "C", "D", "E", "F"]
    );
    assert_eq!(module.protected_exports, ["Base"]);
}
#[test]
fn dynamic_members_special_hooks_duplicate_names_and_enum_subclasses_are_rejected() {
    for body in [
        "A = []",
        "__private = 1",
        "mro = 1",
        "def name(self):\n        return 1",
        "@staticmethod\n    def label():\n        return 1",
        "A = 1.5",
        "A = int('1')",
        "A = unknown",
        "_ignore_ = 'A'",
        "A = 1\n    A = 2",
        "A: int = 1",
        "def __new__(cls):\n        pass",
        "A = 1\n    def A(self):\n        pass",
        "def label(self):\n        pass\n    label = 1",
    ] {
        let source = format!("from enum import Enum\nclass Value(Enum):\n    {body}\n");
        assert!(
            lower(&source, &["enum"])
                .1
                .iter()
                .any(|d| d.code == Some("E0100")),
            "{source}"
        );
    }
    let mixed = "from enum import Enum\nfrom dataclasses import dataclass\n@dataclass\nclass C(Enum):\n    A = 1\n";
    assert!(
        lower(mixed, &["enum", "dataclasses"])
            .1
            .iter()
            .any(|d| d.code == Some("E0100"))
    );
    let source =
        "from enum import Enum\nclass First(Enum):\n    A = 1\nclass Second(First):\n    B = 2\n";
    assert!(
        lower(source, &["enum"])
            .1
            .iter()
            .any(|d| d.code == Some("E0100"))
    );
    // 缺少原生身份时只能走普通运行时守卫，不能偷用EnumFinalize。
    let (module, errors) = lower(
        "from enum import Enum\nclass First(Enum):\n    A = 1\n",
        &[],
    );
    assert!(errors.is_empty(), "{errors:?}");
    assert!(module.classes.iter().all(|(_, class)| matches!(
        class.base,
        Some(crate::ClassBase::Runtime(_))
    ) && class.enum_members.is_none()));
}
