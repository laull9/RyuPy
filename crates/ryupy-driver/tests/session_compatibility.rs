//! 从外部crate验证公开Session结构更新接口；内部来源权限的不可写性见compile_fail文档。
use ryupy_driver::{DefaultSourceState, Session};

#[test]
fn session_opaque_state_preserves_external_struct_update() {
    let session = Session {
        experimental_packed_slp: true,
        ..Session::default()
    };
    assert!(session.experimental_packed_slp);
    assert!(session.diagnostics.is_empty());
    // 允许显式填入全默认不透明状态，不向调用方暴露output/plan/catalog权限。
    let session = Session {
        default_source_state: DefaultSourceState::default(),
        ..Session::default()
    };
    assert!(session.diagnostics.is_empty());
}
