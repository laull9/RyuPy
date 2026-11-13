//! 正常落尾分析与静态快路的源码回归，不依赖生成快照。
use crate::{FuncMode, check_module};

fn inspect(source: &str, falls: bool, mode: FuncMode) {
    let mut db = ryupy_base::SourceDb::default();
    let file = db.add("fallthrough.py", source);
    let (parsed, diagnostics) = ryupy_syntax::parse(file, source);
    assert!(diagnostics.is_empty(), "{diagnostics:?}");
    let (hir, diagnostics) = ryupy_hir::lower_module(&parsed, &Default::default(), source);
    assert!(diagnostics.is_empty(), "{diagnostics:?}");
    let (types, diagnostics) = check_module(&hir);
    assert!(diagnostics.is_empty(), "{diagnostics:?}");
    let (id, f) = hir
        .functions
        .iter()
        .find(|(_, f)| f.name.as_str() == "f")
        .unwrap();
    assert_eq!(
        super::fallthrough::may_fall_through(&f.body),
        falls,
        "{source}"
    );
    assert_eq!(types.modes[id], mode, "{source}");
}

#[test]
fn partial_returns_demote() {
    for body in [
        "if flag:\n        return 7",
        "for i in range(0):\n        return 7",
        "while flag:\n        return 7",
        "while True:\n        break",
        "try:\n        raise ValueError()\n    except ValueError:\n        pass",
    ] {
        inspect(
            &format!("def f(flag: bool) -> int:\n    {body}\n"),
            true,
            FuncMode::Dynamic,
        );
    }
}

#[test]
fn total_returns_preserve_static_fast_path() {
    for body in [
        "return 7\n    pass",
        "if flag:\n        return 7\n    else:\n        return 9",
        "for i in range(5):\n        pass\n    return 7",
        "while flag:\n        break\n    return 7",
        "while True:\n        return 7",
        "while True:\n        continue",
        "try:\n        return 7\n    finally:\n        pass",
        "try:\n        pass\n    finally:\n        return 7",
        "for i in range(0):\n        return 7\n    else:\n        return 9",
    ] {
        inspect(
            &format!("def f(flag: bool) -> int:\n    {body}\n"),
            false,
            FuncMode::Static,
        );
    }
}

#[test]
fn exception_and_nested_loop_reachability() {
    // 异常构造本身可能触发既有动态策略；这里分别核验可达性与该策略。
    for (body, falls) in [
        ("raise ValueError()", false),
        (
            "try:\n        raise ValueError()\n    finally:\n        pass",
            false,
        ),
        (
            "try:\n        pass\n    except ValueError:\n        pass\n    else:\n        return 7",
            true,
        ),
        (
            "while True:\n        for i in range(1):\n            break",
            false,
        ),
        (
            "while True:\n        try:\n            return 7\n        finally:\n            break",
            true,
        ),
        (
            "while True:\n        try:\n            return 7\n        finally:\n            continue",
            false,
        ),
        (
            "while True:\n        for i in range(0):\n            pass\n        else:\n            break",
            true,
        ),
        ("def inner():\n        return 7", true),
    ] {
        let src = format!("def f(flag: bool) -> int:\n    {body}\n");
        let mut db = ryupy_base::SourceDb::default();
        let file = db.add("flow.py", &src);
        let (parsed, diagnostics) = ryupy_syntax::parse(file, &src);
        assert!(diagnostics.is_empty());
        let (hir, diagnostics) = ryupy_hir::lower_module(&parsed, &Default::default(), &src);
        assert!(diagnostics.is_empty(), "{diagnostics:?}");
        let (_, f) = hir
            .functions
            .iter()
            .find(|(_, f)| f.name.as_str() == "f")
            .unwrap();
        assert_eq!(
            super::fallthrough::may_fall_through(&f.body),
            falls,
            "{src}"
        );
    }
}

#[test]
fn demotion_propagates_to_caller() {
    inspect(
        "def partial(flag: bool) -> int:\n    if flag:\n        return 7\ndef f(flag: bool) -> int:\n    return partial(flag)\n",
        false,
        FuncMode::Dynamic,
    );
    inspect(
        "def f(flag: bool) -> None:\n    pass\n",
        true,
        FuncMode::Static,
    );
}
