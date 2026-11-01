//! TestCase原生基类身份只来自driver确认且未被改写的真实模块导入。
use crate::{ModuleInfo, lower_module_with_native_exceptions};
use ryupy_base::SourceDb;

fn lower(source: &str, native: &[&str]) -> (crate::Module, Vec<ryupy_base::Diagnostic>) {
    let mut db = SourceDb::default();
    let file = db.add("native_base.py", source);
    let (parsed, errors) = ryupy_syntax::parse(file, source);
    assert!(errors.is_empty());
    lower_module_with_native_exceptions(
        &parsed,
        &ModuleInfo {
            name: "__main__".into(),
            package: String::new(),
            file: "native_base.py".into(),
        },
        source,
        native,
    )
}
#[test]
fn stable_testcase_aliases_are_real_native_bases() {
    let (module, errors) = lower(
        "import unittest as testing\nfrom unittest import TestCase as Base\nclass First(testing.TestCase):\n    pass\nclass Second(Base):\n    pass\n",
        &["unittest"],
    );
    assert!(errors.is_empty(), "{errors:?}");
    assert_eq!(module.classes.len(), 2);
    assert_eq!(module.protected_exports, ["Base", "testing"]);
    assert!(module.classes.iter().all(|(_, cls)| !cls.is_exception));
}
#[test]
fn missing_native_identity_uses_guarded_runtime_base_not_native_assumptions() {
    for source in [
        "import unittest\nclass C(unittest.TestCase):\n    pass\n",
        "from unittest import TestCase as Base\nclass C(Base):\n    pass\n",
    ] {
        let (module, errors) = lower(source, &[]);
        assert!(errors.is_empty(), "{errors:?}");
        assert!(
            module
                .classes
                .iter()
                .all(|(_, class)| matches!(class.base, Some(crate::ClassBase::Runtime(_))))
        );
        assert!(module.protected_exports.is_empty());
    }
    for source in [
        "import unittest\nunittest = 1\nclass C(unittest.TestCase):\n    pass\n",
        "from unittest import TestCase as Base\nBase = object\nclass C(Base):\n    pass\n",
        "from unittest import TestCase as Base\ndef change():\n    global Base\n    Base = object\nclass C(Base):\n    pass\n",
        "import unittest\ndel unittest\nclass C(unittest.TestCase):\n    pass\n",
        "class C(unittest.TestCase):\n    pass\nimport unittest\n",
        "if True:\n    import unittest\nclass C(unittest.TestCase):\n    pass\n",
        "import unittest\nclass C(unittest.Other):\n    pass\n",
        "import other as unittest\nclass C(unittest.TestCase):\n    pass\n",
    ] {
        let (module, errors) = lower(source, &["unittest"]);
        if source.contains("del unittest") {
            // 删除名字仍未实现；未知读取转运行时不能掩盖该独立语法诊断。
            assert!(errors.iter().any(|error| error.code == Some("E0100")));
            continue;
        }
        assert!(errors.is_empty(), "{source}: {errors:?}");
        assert!(
            module
                .classes
                .iter()
                .all(|(_, class)| matches!(class.base, Some(crate::ClassBase::Runtime(_))))
        );
        assert!(module.protected_exports.is_empty());
    }
}

#[test]
fn direct_factory_base_is_still_explicitly_unsupported() {
    let (_, errors) = lower(
        "import unittest\nclass C(getattr(unittest, 'TestCase')):\n    pass\n",
        &["unittest"],
    );
    assert!(errors.iter().any(|d| d.code == Some("E0100")));
}
