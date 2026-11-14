//! 普通函数locals的三份CP-first完整应用；保源码、版本、原始双流及完整用户错误链。
#[path = "function_locals_support/mod.rs"]
mod support;

#[test]
fn function_locals_state_identity() {
    support::check(
        "state_identity.py",
        include_str!("../../../tests/fixtures/function_locals/state_identity.py"),
        &[
            "locals.parameters",
            "locals.synchronize",
            "locals.cleanup",
            "locals.mapping",
            "locals.parameters",
            "locals.synchronize",
            "locals.cleanup",
            "locals.mapping",
            "locals.invocations",
            "locals.recursive",
            "locals.lambda",
        ],
        None,
    );
}

#[test]
fn function_locals_binding_chain() {
    support::check(
        "binding_chain.py",
        include_str!("../../../tests/fixtures/function_locals/binding_chain.py"),
        &[
            "bind.positional",
            "bind.keyword",
            "bind.expansion",
            "bind.invalid-star",
            "bind.shadowed",
            "bind.chain",
        ],
        Some(&[
            "Traceback (most recent call last):",
            "File binding_chain.py, line 57, in <module>",
            "ValueError: locals-cause",
            "The above exception was the direct cause of the following exception:",
            "Traceback (most recent call last):",
            "File binding_chain.py, line 61, in <module>",
            "RuntimeError: locals-ambient",
            "During handling of the above exception, another exception occurred:",
            "Traceback (most recent call last):",
            "File binding_chain.py, line 64, in <module>",
            "File binding_chain.py, line 54, in outer",
            "File binding_chain.py, line 52, in middle",
            "File binding_chain.py, line 50, in inner",
            "TypeError: locals() takes no arguments (1 given)",
        ]),
    );
}

#[test]
fn function_locals_collision_contract() {
    support::check(
        "collision_contract.py",
        include_str!("../../../tests/fixtures/function_locals/collision_contract.py"),
        &[
            "collision.update-delete",
            "collision.key-error",
            "collision.value-error",
            "collision.declarations",
            "collision.invalid-no-refresh",
            "collision.valid-refresh",
        ],
        None,
    );
}
