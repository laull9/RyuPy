//! HIR资格充分条件由真正的初始模式实现交叉验证，避免前端复制类型策略。
#[test]
fn ordinary_dynamic_proof_matches_actual_initial_modes() {
    for source in [
        "def f():\n    return 1\n",
        "def f(x: int):\n    return x\n",
        "def f(x) -> int:\n    return x\n",
        "def f(*x: int) -> int:\n    return len(x)\n",
        "def outer():\n    def inner(x):\n        return x\n    return inner\n",
        "def f():\n    yield 1\nx = (a for a in [1])\n",
        "def f(x=lambda y: y):\n    return x\n",
        "@(lambda f: f)\ndef f():\n    return 1\n",
    ] {
        let mut db = ryupy_base::SourceDb::default();
        let file = db.add("ordinary.py", source);
        let (parsed, diagnostics) = ryupy_syntax::parse(file, source);
        assert!(diagnostics.is_empty(), "{diagnostics:?}");
        assert!(matches!(
            ryupy_hir::ordinary_dynamic_eligibility(&parsed),
            ryupy_hir::OrdinaryDynamicEligibility::Eligible { .. }
        ));
        let (hir, diagnostics) = ryupy_hir::lower_module(&parsed, &Default::default(), source);
        assert!(diagnostics.is_empty(), "{diagnostics:?}");
        assert!(!hir.info.instance_globals);
        for (_, function) in hir.functions.iter() {
            if function.kind != ryupy_hir::FunctionKind::ModuleMain {
                assert_eq!(
                    crate::initial_mode::initial_mode(&hir, function),
                    crate::FuncMode::Dynamic,
                    "{source}: {}",
                    function.name.as_str()
                );
            }
        }
    }
}
