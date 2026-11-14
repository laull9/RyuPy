//! TextWrapper四个真实应用：CP-first、原值/调用顺序/list别名和完整用户链；pending不注册。
#[path = "textwrapper_support/mod.rs"]
mod support;

#[test]
fn a_class_initialization_real_inheritance_and_partial_setters() {
    support::check(
        "class_init.py",
        include_str!("../../../tests/fixtures/textwrapper/class_init.py"),
        &[
            "T1.defaults",
            "T1.ten-positional",
            "T1.kwonly",
            "T1.duplicate",
            "T1.unknown",
            "T1.raw-identity",
            "T1.repeat",
            "T1.independent",
            "T1.setter-partial",
            "T1.duck-init",
            "T1.real-inheritance",
        ],
        None,
    );
}

#[test]
fn b_exact_chunks_default_algorithms_and_live_configuration() {
    support::check(
        "default_algorithms.py",
        include_str!("../../../tests/fixtures/textwrapper/default_algorithms.py"),
        &[
            "T2.split.00.true",
            "T2.split.00.false",
            "T2.split.00.one",
            "T2.split.01.true",
            "T2.split.01.false",
            "T2.split.01.one",
            "T2.split.02.true",
            "T2.split.02.false",
            "T2.split.02.one",
            "T2.split.03.true",
            "T2.split.03.false",
            "T2.split.03.one",
            "T2.split.04.true",
            "T2.split.04.false",
            "T2.split.04.one",
            "T2.split.05.true",
            "T2.split.05.false",
            "T2.split.05.one",
            "T2.split.06.true",
            "T2.split.06.false",
            "T2.split.06.one",
            "T2.split.07.true",
            "T2.split.07.false",
            "T2.split.07.one",
            "T2.split.08.true",
            "T2.split.08.false",
            "T2.split.08.one",
            "T2.split.09.true",
            "T2.split.09.false",
            "T2.split.09.one",
            "T2.normal.00",
            "T2.normal.01",
            "T2.normal.02",
            "T2.normal.03",
            "T2.normal.04",
            "T2.normal.05",
            "T2.normal.06",
            "T2.normal.07",
            "T2.normal.08",
            "T2.normal.09",
            "T2.normal.10",
            "T2.normal.11",
            "T2.live-config",
        ],
        None,
    );
}

#[test]
fn c_dynamic_hooks_duck_receivers_and_visible_chunk_aliases() {
    support::check(
        "hooks_aliases.py",
        include_str!("../../../tests/fixtures/textwrapper/hooks_aliases.py"),
        &[
            "T3.pipeline",
            "T3.split-chunks",
            "T3.root-dispatch",
            "T3.duck-wrap",
            "T3.late-method",
            "T3.late-width",
            "T3.consume-alias",
            "T3.longword-break",
            "T3.longword-preserve",
            "T3.dynamic-longword",
            "T3.raise-partial",
            "T3.rebind-next-hook",
        ],
        None,
    );
}

#[test]
fn d_current_factory_root_rebinding_and_complete_user_context() {
    support::check(
        "factory_context.py",
        include_str!("../../../tests/fixtures/textwrapper/factory_context.py"),
        &[
            "T4.module-factory",
            "T4.module-method",
            "T4.module-inner-hook",
            "T4.deleted-width",
            "T4.zero-empty",
            "T4.text-before-width",
            "T4.disabled-text",
            "T4.unused-tabsize",
            "T4.unused-placeholder",
            "T4.chain",
        ],
        Some(EXPECTED_TRACE),
    );
}

// 仅CP固定库路径384/359两帧被单独记录后投影；以下每一条用户帧不可删除。
const EXPECTED_TRACE: &[&str] = &[
    "Traceback (most recent call last):",
    "File factory_context.py, line 114, in <module>",
    "ValueError: TextWrapper outer context",
    "During handling of the above exception, another exception occurred:",
    "Traceback (most recent call last):",
    "File factory_context.py, line 116, in <module>",
    "File factory_context.py, line 108, in outer",
    "File factory_context.py, line 104, in middle",
    "File factory_context.py, line 100, in _wrap_chunks",
    "File factory_context.py, line 95, in hook_leaf",
    "LookupError: TextWrapper hook chain failed",
];
