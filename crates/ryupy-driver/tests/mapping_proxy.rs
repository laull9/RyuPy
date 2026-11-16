//! MappingProxyType 四个真实 CP/JIT 应用；CP 原流先守门，再编译并执行同源程序。
#[path = "mapping_proxy_support/mod.rs"]
mod support;

#[test]
fn mapping_proxy_identity_binding() {
    support::check(
        "identity_binding.py",
        include_str!("../../../tests/fixtures/mapping_proxy/identity_binding.py"),
        &[
            "P1.type",
            "P1.enum",
            "P1.subclass",
            "P1.noarg",
            "P1.twoargs",
            "P1.duplicate",
            "P1.badkw",
            "P1.extra",
            "P1.list",
            "P1.tuple",
            "P1.object",
            "P1.expanded",
            "P1.string",
            "P1.none-slot",
            "P1.instance-slot",
            "P1.dynamic",
            "P1.complete",
        ],
        None,
    );
}
#[test]
fn mapping_proxy_live_integration() {
    support::check(
        "live_integration.py",
        include_str!("../../../tests/fixtures/mapping_proxy/live_integration.py"),
        &[
            "P2.live",
            "P2.readonly",
            "P2.iteration",
            "P2.consumers",
            "P2.native",
            "P2.union",
            "P2.complete",
        ],
        None,
    );
}
#[test]
fn mapping_proxy_mapping_protocol() {
    support::check(
        "mapping_protocol.py",
        include_str!("../../../tests/fixtures/mapping_proxy/mapping_protocol.py"),
        &[
            "P3.read",
            "P3.methods",
            "P3.rebind",
            "P3.hash",
            "P3.union",
            "P3.compare",
            "P3.explicit-ne",
            "P3.errors",
            "P3.copy",
            "P3.indexed",
            "P3.hit",
            "P3.miss",
            "P3.disabled",
            "P3.raw",
            "P3.kw-none-duplicate",
            "P3.kw-string-duplicate",
            "P3.kw-before-read",
            "P3.kw-after-merge",
            "P3.kw-read-error",
            "P3.counter-empty",
            "P3.counter-nonempty",
            "P3.counter-subtract",
            "P3.complete",
        ],
        None,
    );
}
#[test]
fn mapping_proxy_source_shadow() {
    support::check(
        "main.py",
        include_str!("../../../tests/fixtures/mapping_proxy/shadow/main.py"),
        &["P4.origin", "P4.rebind", "P4.complete"],
        Some(include_str!(
            "../../../tests/fixtures/mapping_proxy/shadow/types.py"
        )),
    );
}
