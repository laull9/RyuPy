//! 普通运行时基类保真实绑定；未实现的元类/特殊布局在类体之前受控拒绝。
use ryupy_driver::{Session, Stage};
mod support;
use support::dump;

#[test]
fn snapshot_runtime_base_and_atomic_public_binding() {
    let path = std::env::temp_dir().join("ryupy_runtime_base_snapshot.py");
    std::fs::write(&path, "from package import Base as Alias\nclass Child(Alias):\n    pass\nclass Grandchild(Child):\n    pass\n").unwrap();
    for (stage, name) in [
        (Stage::Hir, "runtime_base_hir"),
        (Stage::Types, "runtime_base_types"),
        (Stage::Mir, "runtime_base_mir"),
    ] {
        let mut session = Session::default();
        let output = dump(&mut session, &path, stage).unwrap();
        assert!(session.diagnostics.is_empty());
        insta::assert_snapshot!(name, output);
    }
    std::fs::remove_file(path).unwrap();
}

#[test]
fn unsupported_metaclass_is_rejected_before_body_without_rebinding() {
    let root = ryupy_driver::runtime::workspace_root().join("target/test_runtime_base_guard");
    std::fs::create_dir_all(&root).unwrap();
    let input = root.join("main.py");
    std::fs::write(&input, "class Holder:\n    pass\nholder = Holder()\nholder.base = 7\nExisting = 'old'\nlog = []\ntry:\n    class Existing(holder.base):\n        log.append('body')\nexcept NotImplementedError:\n    print(Existing, log)\n").unwrap();
    let mut session = Session::default();
    let artifact = ryupy_driver::build(
        &mut session,
        &ryupy_driver::BuildOptions {
            input,
            out_dir: root.join("out"),
            aot: false,
            search_paths: vec![],
        },
    )
    .unwrap();
    let result = ryupy_driver::dotnet::run_dll(&artifact.dll).unwrap();
    assert_eq!(
        result.status.code(),
        Some(0),
        "{}",
        String::from_utf8_lossy(&result.stderr)
    );
    assert!(result.stderr.is_empty());
    assert_eq!(String::from_utf8_lossy(&result.stdout), "old []\n");
}

#[test]
fn rebound_enum_import_does_not_select_native_enum_lowering() {
    // 旧HIR诊断案例迁移为实际运行时拒绝：重绑后绝不能继续按Enum成员转换。
    let root =
        ryupy_driver::runtime::workspace_root().join("target/test_runtime_base_rebound_enum");
    std::fs::create_dir_all(&root).unwrap();
    let input = root.join("main.py");
    std::fs::write(&input, "from enum import Enum as Base\nBase = object\nC = 'old'\ntry:\n    class C(Base):\n        A = 1\nexcept NotImplementedError:\n    print(C)\n").unwrap();
    let mut session = Session::default();
    let artifact = ryupy_driver::build(
        &mut session,
        &ryupy_driver::BuildOptions {
            input,
            out_dir: root.join("out"),
            aot: false,
            search_paths: vec![],
        },
    )
    .unwrap();
    let result = ryupy_driver::dotnet::run_dll(&artifact.dll).unwrap();
    assert_eq!(
        result.status.code(),
        Some(0),
        "{}",
        String::from_utf8_lossy(&result.stderr)
    );
    assert!(result.stderr.is_empty());
    assert_eq!(String::from_utf8_lossy(&result.stdout), "old\n");
}
