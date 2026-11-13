//! 资格分析覆盖完整源码及不可达边界，不能靠项目名或缩短失败源取得资格。
use crate::{OrdinaryDynamicEligibility as Eligibility, ordinary_dynamic_eligibility};
fn assess(source: &str) -> Eligibility {
    let mut db = ryupy_base::SourceDb::default();
    let file = db.add("ordinary.py", source);
    let (parsed, errors) = ryupy_syntax::parse(file, source);
    assert!(errors.is_empty(), "{errors:?}");
    ordinary_dynamic_eligibility(&parsed)
}
#[test]
fn ordinary_dynamic_requires_every_definition_to_be_dynamic() {
    for source in [
        "def f():\n    return 1\n",
        "def f(x: int):\n    return x\n",
        "def f(x) -> int:\n    return x\n",
        "def f(*x: int) -> int:\n    return len(x)\n",
        "def outer():\n    def inner(x):\n        return x\n    return inner\n",
        "def f():\n    yield 1\nx = (a for a in [1])\n",
    ] {
        assert!(
            matches!(assess(source), Eligibility::Eligible { .. }),
            "{source}"
        );
    }
    for source in [
        "def f(x: int) -> int:\n    return x\n",
        "def outer():\n    def inner(x: int) -> int:\n        return x\n    return inner\n",
        "def f() -> int:\n    yield 1\n",
    ] {
        assert_eq!(assess(source), Eligibility::CompleteSignature, "{source}");
    }
    assert_eq!(assess("x = 1\n"), Eligibility::NoFunction);
    assert_eq!(
        assess("def f():\n    pass\nclass C:\n    pass\n"),
        Eligibility::ClassDefinition
    );
}
#[test]
fn ordinary_dynamic_reuses_the_complete_instance_boundary() {
    for boundary in [
        "try:\n    pass\nexcept Exception:\n    pass\n",
        "from . import child\n",
        "x: int = 1\n",
        "x = [v for v in []]\n",
        "x = globals()\n",
        "del obj.value\n",
    ] {
        let source = format!(
            "def f():\n    pass\nif False:\n{}",
            boundary
                .lines()
                .map(|s| format!("    {s}\n"))
                .collect::<String>()
        );
        assert_eq!(
            assess(&source),
            Eligibility::InstanceSyntaxBoundary,
            "{source}"
        );
    }
}

#[test]
fn debug_constant_does_not_make_ordinary_function_need_instance_environment() {
    assert!(matches!(
        assess("def f():\n    return __debug__\n"),
        Eligibility::Eligible { .. }
    ));
}
