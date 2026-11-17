//! 接口目录红绿门：声明形状、来源分离、真实初始化符号和解析缺口必须可审计。
use super::*;
use ryupy_syntax::ast::PythonVersion;
#[test]
fn interface_stub_keeps_overloads_parameter_shapes_and_source_evidence() {
    let source = "from typing import overload as over\nclass Engine:\n @over\n def run(self, data: bytes, /, *, strict: bool = True) -> bytes: ...\n @over\n def run(self, data: str, /, *, strict: bool = True) -> str: ...\nasync def poll(*args: int, callback: object, **kwargs: str) -> None: ...\nraise RuntimeError('不得求值stub')\n";
    let report = stub::extract(source, "pkg/core.pyi", PythonVersion::PY312, true);
    assert_eq!(report["role"], "declaration_only");
    assert_eq!(report["business_body"], false);
    let declarations = report["declarations"].as_array().unwrap();
    let functions: Vec<_> = declarations
        .iter()
        .filter(|d| d["name"] == "Engine.run")
        .collect();
    assert_eq!(functions.len(), 2);
    assert_eq!(functions[0]["overload"], true);
    assert_eq!(functions[0]["line"], 4);
    assert_eq!(functions[0]["parameters"][1]["kind"], "positional_only");
    assert_eq!(functions[0]["parameters"][1]["annotation"], "bytes");
    assert_eq!(functions[0]["parameters"][2]["kind"], "keyword_only");
    assert_eq!(functions[0]["parameters"][2]["default"], "True");
    let poll = declarations.iter().find(|d| d["name"] == "poll").unwrap();
    assert_eq!(poll["async"], true);
    assert_eq!(poll["parameters"][0]["kind"], "var_positional");
    assert_eq!(poll["parameters"][2]["kind"], "var_keyword");
    assert_eq!(report["machine_abi"]["status"], "not_derived");
    assert_eq!(report["complete"], false);
}
#[test]
fn interface_stub_errors_are_reported_with_version_without_executing() {
    let text = "def f[T](value: T) -> T: ...\n";
    let supported = stub::extract(text, "core.pyi", PythonVersion::PY312, true);
    assert!(supported["diagnostics"].as_array().unwrap().is_empty());
    let earlier = stub::extract(text, "core.pyi", PythonVersion::PY311, true);
    assert!(!earlier["diagnostics"].as_array().unwrap().is_empty());
    let broken = stub::extract("def broken(:\n", "bad.pyi", PythonVersion::PY312, true);
    assert_eq!(broken["parse_status"], "partial");
    assert!(!broken["diagnostics"].as_array().unwrap().is_empty());
}
#[test]
fn interface_native_suffixes_and_macho_symbol_prefix_are_exact() {
    let suffixes = vec![
        ".cpython-312-darwin.so".into(),
        ".abi3.so".into(),
        ".so".into(),
    ];
    assert_eq!(
        native::module_name(Path::new("pkg/backend.cpython-312-darwin.so"), &suffixes).as_deref(),
        Some("pkg.backend")
    );
    assert_eq!(
        native::module_name(Path::new("pkg/.libs/libbackend.so"), &suffixes),
        None
    );
    assert_eq!(
        native::module_name(Path::new("pkg/backend.cpython-311-darwin.so"), &suffixes),
        None
    );
    assert!(native::symbol_matches(
        true,
        b"_PyInit_backend",
        b"PyInit_backend"
    ));
    assert!(!native::symbol_matches(
        false,
        b"_PyInit_backend",
        b"PyInit_backend"
    ));
    assert!(!native::symbol_matches(
        true,
        b"_PyInit_backend_extra",
        b"PyInit_backend"
    ));
}

struct Tree(std::path::PathBuf);
impl Tree {
    fn new() -> Self {
        static NEXT: std::sync::atomic::AtomicUsize = std::sync::atomic::AtomicUsize::new(0);
        let stamp = std::time::SystemTime::now()
            .duration_since(std::time::UNIX_EPOCH)
            .unwrap()
            .as_nanos();
        let root = std::env::temp_dir().join(format!(
            "ryupy-interface-{}-{stamp}-{}",
            std::process::id(),
            NEXT.fetch_add(1, std::sync::atomic::Ordering::Relaxed)
        ));
        std::fs::create_dir(&root).unwrap();
        Self(root)
    }
    fn write(&self, path: &str, text: &str) {
        let target = self.0.join(path);
        std::fs::create_dir_all(target.parent().unwrap()).unwrap();
        std::fs::write(target, text).unwrap();
    }
}
impl Drop for Tree {
    fn drop(&mut self) {
        let _ = std::fs::remove_dir_all(&self.0);
    }
}
#[test]
fn interface_catalog_uses_selected_staged_files_and_stub_never_grants_execution() {
    use crate::cpython_provider::{
        Payload, Plan,
        delegation::{DelegatedEntry, DelegationPlan},
        version::PythonVersion,
    };
    let tree = Tree::new();
    let site = crate::cpython_provider::native::site_packages_relpath_for_version(
        &PythonVersion::new(3, 12, 13),
    )
    .unwrap();
    let entries = [
        ("engine.py", "def run(x): return x\n"),
        ("engine.pyi", "def run(x: int) -> int: ...\n"),
        ("stubonly.pyi", "def absent() -> bytes: ...\n"),
        ("bad.so", "bad image"),
        ("unselected.pyi", "def bad(:\n"),
    ];
    let mut files = vec![];
    let mut payloads = vec![];
    for (name, text) in entries {
        let relative = site.join(name).to_string_lossy().into_owned();
        tree.write(&relative, text);
        files.push(json!({"path":relative}));
        if name != "unselected.pyi" {
            payloads.push(Payload {
                source: tree.0.join(&relative),
                destination: name.into(),
            });
        }
    }
    let compiled = "python/ryupy_compiled/__main__.py";
    tree.write(compiled, "raise RuntimeError('源码仅审计不得执行')\n");
    files.push(json!({"path":compiled}));
    let stdlib = "python/lib/python3.12/stdlib_unselected.py";
    tree.write(stdlib, "def malformed(:\n");
    files.push(json!({"path":stdlib}));
    let plan = Plan {
        version: PythonVersion::new(3, 12, 13),
        root: tree.0.clone(),
        source: serde_json::from_value(
            json!({"project":"fixture","build_id":"1","source_url":"local","licenses":[]}),
        )
        .unwrap(),
        modules: vec!["engine".into()],
        stdlib: Default::default(),
        declared: Default::default(),
        packages: Default::default(),
        container_transfer: Default::default(),
        payloads,
        delegation: DelegationPlan {
            compiled_sources: vec![DelegatedEntry {
                source: tree.0.join(compiled),
                destination: "ryupy_compiled/__main__.py".into(),
            }],
            ..Default::default()
        },
        bridge: tree.0.clone(),
        shim: tree.0.join("shim"),
        _shim_build: None,
        dependency_report: Value::Null,
    };
    let report = analyze(&plan, &tree.0, &files).unwrap();
    assert_eq!(report["schema"], 1);
    assert_eq!(report["files"].as_array().unwrap().len(), 5);
    assert_eq!(report["modules"]["__main__"]["execution_provider"], "cil");
    assert_eq!(
        report["modules"]["engine"]["execution_provider"],
        "cpython_source"
    );
    assert_eq!(
        report["modules"]["engine"]["declarations"]
            .as_array()
            .unwrap()
            .len(),
        1
    );
    assert!(report["modules"]["stubonly"]["execution_provider"].is_null());
    assert_eq!(report["coverage"]["stdlib"], "inventory_only");
    let bad = report["files"]
        .as_array()
        .unwrap()
        .iter()
        .find(|e| e["module"] == "bad")
        .unwrap();
    assert_eq!(bad["native"]["status"], "audit_error");
    assert!(!bad["native"]["error"].as_str().unwrap().is_empty());
}
#[cfg(any(target_os = "macos", target_os = "linux"))]
#[test]
fn interface_native_real_exports_distinguish_extension_from_same_suffix_library() {
    let tree = Tree::new();
    for (name, source, expected) in [
        ("engine", "int PyInit_engine(void) {return 1;}\n", true),
        (
            "ordinary",
            "int ordinary_add(int a,int b){return a+b;}\n",
            false,
        ),
    ] {
        tree.write(&format!("{name}.c"), source);
        let library = tree.0.join(format!("{name}.so"));
        let mut command = std::process::Command::new("cc");
        command.args(if cfg!(target_os = "macos") {
            vec!["-dynamiclib"]
        } else {
            vec!["-shared", "-fPIC"]
        });
        let output = command
            .arg(tree.0.join(format!("{name}.c")))
            .arg("-o")
            .arg(&library)
            .output()
            .unwrap();
        assert!(
            output.status.success(),
            "{}",
            String::from_utf8_lossy(&output.stderr)
        );
        let rid = crate::cpython_provider::native::PlatformKind::current()
            .unwrap()
            .rid();
        let report = native::inspect(&library, Some(name), rid);
        assert_eq!(report["status"], "audited");
        assert_eq!(report["initializer_verified"], expected);
        assert_eq!(
            report["kind"],
            if expected {
                "cpython_extension"
            } else {
                "native_dependency"
            }
        );
        assert_eq!(report["method_signatures_verified"], false);
    }
}
#[test]
fn interface_native_pe_forwarder_is_not_a_local_initializer() {
    let tree = Tree::new();
    let mut bytes = vec![0u8; 1024];
    fn u16_at(bytes: &mut [u8], offset: usize, value: u16) {
        bytes[offset..offset + 2].copy_from_slice(&value.to_le_bytes());
    }
    fn u32_at(bytes: &mut [u8], offset: usize, value: u32) {
        bytes[offset..offset + 4].copy_from_slice(&value.to_le_bytes());
    }
    bytes[..2].copy_from_slice(b"MZ");
    u32_at(&mut bytes, 60, 0x80);
    bytes[0x80..0x84].copy_from_slice(b"PE\0\0");
    u16_at(&mut bytes, 0x84, 0x8664);
    u16_at(&mut bytes, 0x86, 1);
    u16_at(&mut bytes, 0x94, 240);
    u16_at(&mut bytes, 0x96, 0x2022);
    u16_at(&mut bytes, 0x98, 0x20b);
    u32_at(&mut bytes, 0x98 + 108, 16);
    u32_at(&mut bytes, 0x98 + 112, 0x1000);
    u32_at(&mut bytes, 0x98 + 116, 0x100);
    bytes[0x188..0x18e].copy_from_slice(b".edata");
    u32_at(&mut bytes, 0x188 + 8, 0x200);
    u32_at(&mut bytes, 0x188 + 12, 0x1000);
    u32_at(&mut bytes, 0x188 + 16, 0x200);
    u32_at(&mut bytes, 0x188 + 20, 0x200);
    for (offset, value) in [
        (12, 0x1050),
        (16, 1),
        (20, 1),
        (24, 1),
        (28, 0x1060),
        (32, 0x1064),
        (36, 0x1068),
    ] {
        u32_at(&mut bytes, 0x200 + offset, value);
    }
    u32_at(&mut bytes, 0x260, 0x1080);
    u32_at(&mut bytes, 0x264, 0x10a0);
    bytes[0x250..0x25c].copy_from_slice(b"forward.dll\0");
    bytes[0x280..0x293].copy_from_slice(b"real.PyInit_engine\0");
    bytes[0x2a0..0x2ae].copy_from_slice(b"PyInit_engine\0");
    let path = tree.0.join("engine.pyd");
    std::fs::write(&path, bytes).unwrap();
    let report = native::inspect(&path, Some("engine"), "win-x64");
    assert_eq!(report["status"], "audited");
    assert_eq!(report["local_export_count"], 0);
    assert_eq!(report["initializer_verified"], false);
}
