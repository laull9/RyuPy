//! ABC冷合同的正负域；测试不把运行时未验证的导入值断言成静态native布局。
use crate::{Module, ModuleInfo, lower_module_with_native_exceptions};

fn lower(source: &str, native: &[&str], instance: bool) -> (Module, Vec<ryupy_base::Diagnostic>) {
    let mut db = ryupy_base::SourceDb::default();
    let file = db.add("abc_bases.py", source);
    let (parsed, errors) = ryupy_syntax::parse(file, source);
    assert!(errors.is_empty(), "{errors:?}");
    lower_module_with_native_exceptions(
        &parsed,
        &ModuleInfo {
            instance_globals: instance,
            ..Default::default()
        },
        source,
        native,
    )
}
fn selected(source: &str, native: &[&str]) -> bool {
    let (module, errors) = lower(source, native, false);
    assert!(errors.is_empty(), "{source}: {errors:?}");
    let class = &module
        .classes
        .iter()
        .find(|(_, class)| class.name.as_str() == "Target")
        .unwrap()
        .1;
    assert!(module.protected_exports.is_empty(), "冷入口不冻结ABC导出");
    class.ordinary_mi_bases.is_some()
}
#[test]
fn canonical_names_match_shared_runtime_contract_fixture() {
    let expected: Vec<_> = include_str!("../../../tests/fixtures/abc/canonical_names.txt")
        .lines()
        .collect();
    assert_eq!(super::NAMES, expected);
}
#[test]
fn authenticated_abc_single_bases_and_local_descendants_use_deferred_publication() {
    let source = "import collections.abc as c\nfrom collections.abc import MutableSequence as M\nclass Target(M): pass\nclass Child(Target): pass\nclass View(c.ItemsView, c.Sequence): pass\nclass Leaf(View): pass\nclass Old: pass\nclass OldChild(Old): pass\n";
    let (module, errors) = lower(source, &["collections.abc"], false);
    assert!(errors.is_empty(), "{errors:?}");
    for (_, class) in module.classes.iter() {
        let chosen = !matches!(class.name.as_str(), "Old" | "OldChild");
        assert_eq!(class.ordinary_mi_bases.is_some(), chosen, "{}", class.name);
        assert_eq!(class.decorated_binding.is_some(), chosen, "{}", class.name);
    }
    for name in super::NAMES {
        assert!(selected(
            &format!("from collections.abc import {name} as Base\nclass Target(Base): pass\n"),
            &["collections.abc"]
        ));
    }
}
#[test]
fn sortedcontainers_exact_fallback_shape_is_a_cold_candidate() {
    let source = "try:\n    from collections.abc import Sequence, MutableSequence\nexcept ImportError:\n    from collections import Sequence, MutableSequence\nclass Target(MutableSequence): pass\nclass Child(Target): pass\n";
    assert!(selected(source, &["collections.abc"]));
    assert!(!selected(source, &[]));
    assert!(selected(
        "try:\n    from collections.abc import Sequence as S\nexcept ImportError:\n    from collections import Sequence as S\nclass Target(S): pass\n",
        &["collections.abc"]
    ));
}
#[test]
fn module_source_shadow_rebinding_deletion_position_and_wrong_member_do_not_select() {
    assert!(!selected(
        "from collections.abc import Sequence as Base\nclass Target(Base): pass\n",
        &[]
    ));
    for source in [
        "from other import Sequence as Base\nclass Target(Base): pass\n",
        "from collections.abc import Sequence as Base\nBase=object\nclass Target(Base): pass\n",
        "from collections.abc import Sequence as Base\ndef change():\n    global Base\n    Base=object\nclass Target(Base): pass\n",
        "from collections.abc import Sequence as Base\ndel Base\nclass Target(Base): pass\n",
        "class Target(Base): pass\nfrom collections.abc import Sequence as Base\n",
        "import collections.abc as c\nc.Sequence=object\nclass Target(c.Sequence): pass\n",
        "import collections.abc as c\nclass Target(c.Unknown): pass\n",
        "if True:\n    from collections.abc import Sequence as Base\nclass Target(Base): pass\n",
        "import collections.abc\nclass Target(collections.abc.Sequence): pass\n",
    ] {
        assert!(!selected(source, &["collections.abc"]), "{source}");
    }
}
#[test]
fn fallback_must_be_pure_and_match_imported_and_bound_names() {
    for source in [
        "try:\n    from collections.abc import Sequence as Base\n    touch()\nexcept ImportError:\n    from collections import Sequence as Base\nclass Target(Base): pass\n",
        "try:\n    from collections.abc import Sequence as Base\nexcept ImportError:\n    from collections import MutableSequence as Base\nclass Target(Base): pass\n",
        "try:\n    from collections.abc import Sequence as Base\nexcept ImportError:\n    Base=object\nclass Target(Base): pass\n",
        "try:\n    from collections.abc import Sequence as Base\nexcept Exception:\n    from collections import Sequence as Base\nclass Target(Base): pass\n",
        "try:\n    from collections.abc import Sequence as Base\nexcept ImportError:\n    from collections import Sequence as Other\nclass Target(Base): pass\n",
        "try:\n    from collections.abc import Sequence as Base\nexcept ImportError:\n    from collections import Sequence as Base\nfinally:\n    Base=object\nclass Target(Base): pass\n",
        "try:\n    from collections.abc import Sequence as Base\nexcept ImportError:\n    from collections import Sequence as Base\nBase=object\nclass Target(Base): pass\n",
    ] {
        assert!(!selected(source, &["collections.abc"]), "{source}");
    }
}
#[test]
fn unrelated_attribute_write_does_not_cancel_finite_abc_candidate() {
    assert!(selected(
        "import collections.abc as c\nc.unrelated=1\nclass Target(c.Sequence): pass\n",
        &["collections.abc"]
    ));
}
#[test]
fn native_layout_keyword_and_instance_guards_are_not_relaxed() {
    for source in [
        "from collections.abc import Sequence\nclass Target(Sequence, metaclass=type): pass\n",
        "from collections.abc import Sequence\nfrom enum import Flag\nclass Target(Sequence, Flag): pass\n",
    ] {
        let (_, errors) = lower(source, &["collections.abc", "enum"], false);
        assert!(!errors.is_empty(), "{source}");
    }
    let (_, errors) = lower(
        "from collections.abc import Sequence\nclass Target(Sequence): pass\n",
        &["collections.abc"],
        true,
    );
    assert!(!errors.is_empty());
}
