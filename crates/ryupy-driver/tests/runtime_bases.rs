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
    // canonical object已支持：保原Enum别名重绑，实际类必须是普通根派生而非Enum成员转换。
    let root =
        ryupy_driver::runtime::workspace_root().join("target/test_runtime_base_rebound_enum");
    std::fs::create_dir_all(&root).unwrap();
    let input = root.join("main.py");
    std::fs::write(&input, "from enum import Enum as Base\nBase = object\nC = 'old'\nclass C(Base):\n    A = 1\nassert type(C.A) is int and C.A == 1\nassert not hasattr(C.A, 'name')\nassert C.__bases__ == (object,)\nassert issubclass(C, object)\ninstance = C()\nassert type(instance) is C and isinstance(instance, object)\nassert instance.A == 1\nprint('ordinary-object-base', C.A)\n").unwrap();
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
    assert_eq!(
        String::from_utf8_lossy(&result.stdout),
        "ordinary-object-base 1\n"
    );
}

// 此负例锁Ryu有限能力诊断，不把CP合法str派生或其它TypeError伪作相同输出。
#[test]
fn unsupported_runtime_base_reports_class_line_and_complete_context() {
    use std::sync::atomic::{AtomicU64, Ordering};
    static NEXT: AtomicU64 = AtomicU64::new(0);
    struct Root(std::path::PathBuf);
    impl Drop for Root {
        fn drop(&mut self) {
            if !std::thread::panicking() {
                let _ = std::fs::remove_dir_all(&self.0);
            }
        }
    }
    let stamp = std::time::SystemTime::now()
        .duration_since(std::time::UNIX_EPOCH)
        .unwrap()
        .as_nanos();
    let path = std::env::temp_dir().join(format!(
        "ryupy-runtime-base-frames-{}-{stamp}-{}",
        std::process::id(),
        NEXT.fetch_add(1, Ordering::Relaxed)
    ));
    std::fs::create_dir(&path).unwrap();
    let root = Root(path);
    let input = root.0.join("runtime_base_frames.py");
    let source = "log = []\ndef choose():\n    log.append('base')\n    return 7\nBase = choose()\nExisting = 'old'\ntry:\n    raise ValueError('outer')\nexcept ValueError:\n    try:\n        class Existing(Base):\n            log.append('body')\n    except NotImplementedError:\n        print(Existing, log)\n        raise\n";
    std::fs::write(&input, source).unwrap();
    // 真正class行必须先SetLine再Validate；本测试仍运行默认优化生成的程序。
    let mut session = Session::default();
    let mir = dump(&mut session, &input, Stage::Mir).unwrap();
    assert!(session.diagnostics.is_empty());
    let guard = mir.find("RuntimeFn::ValidateOrdinaryBase(").unwrap();
    assert_eq!(
        mir[..guard]
            .lines()
            .rev()
            .map(str::trim)
            .find(|line| line.starts_with("// line ")),
        Some("// line 11")
    );
    let artifact = ryupy_driver::build(
        &mut Session::default(),
        &ryupy_driver::BuildOptions {
            input,
            out_dir: root.0.join("out"),
            aot: false,
            search_paths: vec![],
        },
    )
    .unwrap();
    let actual = ryupy_driver::dotnet::run_dll(&artifact.dll).unwrap();
    std::fs::write(root.0.join("stdout"), &actual.stdout).unwrap();
    std::fs::write(root.0.join("stderr"), &actual.stderr).unwrap();
    std::fs::write(root.0.join("exit"), format!("{:?}", actual.status.code())).unwrap();
    assert_eq!(actual.status.code(), Some(1));
    let stdout = if cfg!(windows) {
        "old ['base']\r\n"
    } else {
        "old ['base']\n"
    };
    assert_eq!(actual.stdout, stdout.as_bytes());
    let stderr = "Traceback (most recent call last):\n  File \"runtime_base_frames.py\", line 8, in <module>\nValueError: outer\n\nDuring handling of the above exception, another exception occurred:\n\nTraceback (most recent call last):\n  File \"runtime_base_frames.py\", line 11, in <module>\nNotImplementedError: runtime bases currently require an ordinary user class; special classes and metaclass protocols are not supported\n";
    assert_eq!(actual.stderr, stderr.as_bytes());
}
