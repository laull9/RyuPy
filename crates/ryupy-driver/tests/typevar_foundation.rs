//! TypeVar公开合同：先取得固定CP同次预期，再独立构建/JIT，所有失败保留证据。
#[path = "typevar_foundation_support/mod.rs"]
mod support;

#[test]
fn typevar_foundation_metadata() {
    support::check(
        "metadata.py",
        include_str!("../../../tests/cases/typevar_foundation/metadata/entry.py"),
        &[
            "identity",
            "metadata",
            "write",
            "delete",
            "write",
            "delete",
            "write",
            "delete",
            "write",
            "delete",
            "write",
            "delete",
            "write",
            "delete",
            "mutation",
            "caller-module",
            "class-write",
            "class-write",
            "class-delete",
            "function-module",
        ],
    );
}

#[test]
fn typevar_foundation_binding() {
    support::check(
        "binding.py",
        include_str!("../../../tests/cases/typevar_foundation/binding/entry.py"),
        &[
            "references",
            "missing-name",
            "bad-name",
            "duplicate-name",
            "unknown-keyword",
            "single-constraint",
            "single-before-conflict",
            "constraint-conflict",
            "bound-before-single",
            "bound-before-conflict",
            "flags-before-bound",
            "infer-conflict",
            "tuple-bound",
            "flag-order",
            "all-flags",
            "conflict-events",
            "name-before-flags",
            "keyword-before-flags",
            "raising-flag",
            "nonbool-flag",
            "flag-errors",
            "bad-name-unknown",
            "missing-unknown",
            "duplicate-name-unknown",
        ],
    );
}

#[test]
fn typevar_foundation_composition() {
    support::check(
        "composition.py",
        include_str!("../../../tests/cases/typevar_foundation/composition/entry.py"),
        &["aliases", "recursive-parameters", "annotations"],
    );
}

#[test]
fn typevar_foundation_protocols() {
    support::check(
        "protocols.py",
        include_str!("../../../tests/cases/typevar_foundation/protocols/entry.py"),
        &[
            "identity-protocols",
            "call",
            "getitem",
            "iter",
            "int",
            "trunc",
            "len",
            "float",
            "isinstance",
            "no-implicit-events",
        ],
    );
}

#[test]
fn typevar_foundation_public_slots() {
    support::check(
        "public_slots.py",
        include_str!("../../../tests/cases/typevar_foundation/public_slots/entry.py"),
        &[
            "fixed-slots",
            "right-reflection",
            "missing-call",
            "missing-getitem",
            "missing-iter",
            "missing-length",
            "list-extend",
            "namedtuple-make",
            "namedtuple-defaults",
            "star-unpack",
            "exact-unpack",
            "no-poison-events",
            "write-delete-key",
        ],
    );
}
