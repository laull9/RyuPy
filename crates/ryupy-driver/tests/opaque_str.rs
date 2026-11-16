//! str注解对象ABI：真实多模块身份、保守回退与exact局部/数值循环结构。
use ryupy_driver::{BuildOptions, Session, Stage};
use std::path::PathBuf;
use std::process::{Command, Output};
use std::sync::atomic::{AtomicU64, Ordering};
static NEXT: AtomicU64 = AtomicU64::new(0);
struct Root(PathBuf);
impl Drop for Root {
    fn drop(&mut self) {
        if !std::thread::panicking() {
            let _ = std::fs::remove_dir_all(&self.0);
        }
    }
}
fn root() -> Root {
    let stamp = std::time::SystemTime::now()
        .duration_since(std::time::UNIX_EPOCH)
        .unwrap()
        .as_nanos();
    let path = std::env::temp_dir().join(format!(
        "ryupy-opaque-str-{}-{stamp}-{}",
        std::process::id(),
        NEXT.fetch_add(1, Ordering::Relaxed)
    ));
    std::fs::create_dir(&path).unwrap();
    Root(path)
}
fn save(root: &Root, name: &str, output: &Output) {
    std::fs::write(root.0.join(format!("{name}.stdout")), &output.stdout).unwrap();
    std::fs::write(root.0.join(format!("{name}.stderr")), &output.stderr).unwrap();
    std::fs::write(
        root.0.join(format!("{name}.exit")),
        format!("{:?}", output.status.code()),
    )
    .unwrap();
}
fn compare(source: &str, expected_text: &str) {
    let root = root();
    let python = std::env::var_os("PYTHON").unwrap_or_else(|| "python3".into());
    let version = Command::new(&python).args(["-c", "import sys; assert sys.implementation.name == 'cpython'; print('.'.join(map(str, sys.version_info[:3])))"]).output().unwrap();
    assert!(version.status.success());
    let pinned = std::fs::read_to_string(
        ryupy_driver::runtime::workspace_root().join("tests/pinned-python.txt"),
    )
    .unwrap();
    assert_eq!(
        String::from_utf8_lossy(&version.stdout).trim(),
        pinned.trim()
    );
    let input = root.0.join("main.py");
    std::fs::write(&input, source).unwrap();
    std::fs::write(
        root.0.join("helpers.py"),
        include_str!("../../../tests/fixtures/opaque_str/helpers.py"),
    )
    .unwrap();
    let expected = Command::new(&python)
        .arg("-B")
        .arg(&input)
        .current_dir(&root.0)
        .output()
        .unwrap();
    save(&root, "cp", &expected);
    let artifact = ryupy_driver::build(
        &mut Session::default(),
        &BuildOptions {
            input,
            out_dir: root.0.join("out"),
            aot: false,
            search_paths: vec![],
        },
    )
    .unwrap();
    let actual = ryupy_driver::dotnet::run_dll(&artifact.dll).unwrap();
    save(&root, "ryu", &actual);
    assert_eq!(expected.status.code(), Some(0));
    let fixed = if cfg!(windows) {
        expected_text.replace('\n', "\r\n")
    } else {
        expected_text.to_owned()
    };
    assert_eq!(expected.stdout, fixed.as_bytes());
    assert!(expected.stderr.is_empty());
    assert_eq!(actual.status.code(), expected.status.code());
    assert_eq!(actual.stdout, expected.stdout);
    assert_eq!(actual.stderr, expected.stderr);
}
#[test]
fn opaque_object_identity_survives_alias_module_and_field_boundaries() {
    compare(
        include_str!("../../../tests/fixtures/opaque_str/core.py"),
        "opaque-identity-fields-loop\n",
    );
}
#[test]
fn opaque_consumers_preserve_dynamic_protocol_order() {
    compare(
        include_str!("../../../tests/fixtures/opaque_str/fallback.py"),
        "opaque-fallback-protocols\n",
    );
}
#[test]
fn opaque_mir_keeps_object_signature_exact_concat_and_numeric_loop() {
    let root = root();
    let input = root.0.join("shape.py");
    std::fs::write(
        &input,
        include_str!("../../../tests/fixtures/opaque_str/shapes.py"),
    )
    .unwrap();
    let mut session = Session::default();
    let mir = ryupy_driver::dump(&mut session, &input, Stage::Mir).unwrap();
    assert!(session.diagnostics.is_empty(), "{:?}", session.diagnostics);
    std::fs::write(root.0.join("shape.mir"), &mir).unwrap();
    let function = |name: &str| -> &str {
        let start = mir.find(&format!("fn {name}(")).unwrap();
        let tail = &mir[start..];
        &tail[..tail.find("\n}\n").unwrap() + 3]
    };
    let identity = function("identity");
    assert!(
        identity.starts_with("fn identity(_0: dyn) -> dyn"),
        "{identity}"
    );
    assert!(identity.contains(": str; // exact"), "{identity}");
    assert!(identity.contains(": i64; // count"), "{identity}");
    assert!(identity.contains("RuntimeFn::StrConcat"), "{identity}");
    assert!(!identity.contains("CastToStr"), "{identity}");
    let numeric = function("numeric_loop");
    assert!(
        numeric.starts_with("fn numeric_loop(_0: dyn, _1: i64) -> i64"),
        "{numeric}"
    );
    assert!(numeric.contains(": i64; // total"), "{numeric}");
    assert!(numeric.contains("branch "), "{numeric}");
    assert!(!numeric.contains("dyn.Add"), "{numeric}");
    assert!(!numeric.contains("DynIter"), "{numeric}");
    assert!(function("read").starts_with("fn read() -> dyn"));
    // OpaqueStr 映射为 dyn 对象槽，但这些函数仍是 Static，不应增加 owner。
    for name in ["identity", "read", "branch", "numeric_loop"] {
        let body = function(name);
        assert!(!body.contains("__function_owner"), "{body}");
        assert!(!body.contains("RuntimeFn::TracePush("), "{body}");
        let prefix =
            format!("RuntimeFn::TracePushWithOrigin(const \"shape.py\", const \"{name}\", global@");
        let origins: Vec<_> = body
            .lines()
            .filter_map(|line| line.split_once(&prefix))
            .collect();
        assert_eq!(origins.len(), 1, "{body}");
        let global = origins[0]
            .1
            .strip_suffix(");")
            .expect("Static来源必须是直接global载体");
        assert!(global.parse::<u32>().is_ok(), "{body}");
    }
    // 返回注解不匹配使 declared_int 整函数降为 Dynamic，owner 是独立尾参。
    let declared = function("declared_int");
    assert!(
        declared.starts_with("fn declared_int(_0: dyn, _1: dyn) -> dyn"),
        "{declared}"
    );
    assert!(
        declared.contains("let _1: dyn; // __function_owner"),
        "{declared}"
    );
    assert!(
        declared.contains(
            "RuntimeFn::TracePushWithOrigin(const \"shape.py\", const \"declared_int\", _1)"
        ),
        "{declared}"
    );
    assert!(!declared.contains("RuntimeFn::TracePush("), "{declared}");
}
