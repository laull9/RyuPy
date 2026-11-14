//! 四个独立动态except应用，CP原流先验通过后才build/JIT。
#[path = "dynamic_except_support/mod.rs"]
mod support;
#[test]
fn dynamic_except_cross_source_identity() {
    support::check("identity", "identity/main.py", 0);
}
#[test]
fn dynamic_except_order_and_tuple_validation() {
    support::check("dispatch", "dispatch.py", 0);
}
#[test]
fn dynamic_except_cleanup_and_ambient() {
    support::check("cleanup", "cleanup.py", 0);
}
#[test]
fn dynamic_except_complete_error_chain() {
    support::check("chain", "chain.py", 1);
}
