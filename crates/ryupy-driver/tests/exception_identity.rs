//! 跨模块异常引用必须使用内部限定身份，不修改 Python 可见类名。
use ryupy_driver::{Session, Stage};
mod support;
use support::dump;

#[test]
fn builtin_and_user_exception_names_do_not_share_global_cil_key() {
    let root = std::path::Path::new(env!("CARGO_MANIFEST_DIR"))
        .join("../../tests/cases/phase3/m3_7_exception_identity");
    // dump 是单模块视图；三模块实际链接与执行由同名目录 e2e 验收。
    for (file, snapshot) in [
        ("entry.py", "exception_identity_cil"),
        ("left.py", "exception_identity_left_cil"),
        ("right.py", "exception_identity_right_cil"),
    ] {
        let mut session = Session::default();
        let output = dump(&mut session, &root.join(file), Stage::Cil)
            .expect("同名异常应保持模块限定的静态捕获身份");
        insta::assert_snapshot!(snapshot, output);
    }
}
