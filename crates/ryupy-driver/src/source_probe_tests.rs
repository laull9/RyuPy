//! 静态报告合同回归：来源隔离、搜索优先级、缺失固定点和动态语法候选。
use super::*;
use crate::ModuleEnvironmentPolicy;

struct Tree(PathBuf);
impl Tree {
    fn new() -> Self {
        static NEXT: std::sync::atomic::AtomicUsize = std::sync::atomic::AtomicUsize::new(0);
        let stamp = std::time::SystemTime::now()
            .duration_since(std::time::UNIX_EPOCH)
            .unwrap()
            .as_nanos();
        let serial = NEXT.fetch_add(1, std::sync::atomic::Ordering::Relaxed);
        let root = std::env::temp_dir().join(format!(
            "ryupy-source-probe-{}-{stamp}-{serial}",
            std::process::id()
        ));
        std::fs::create_dir(&root).unwrap();
        Self(root.canonicalize().unwrap())
    }
    fn write(&self, name: &str, text: &str) {
        let path = self.0.join(name);
        std::fs::create_dir_all(path.parent().unwrap()).unwrap();
        std::fs::write(path, text).unwrap();
    }
    fn report(&self, policy: ModuleEnvironmentPolicy, paths: Vec<PathBuf>) -> Value {
        let mut session = Session {
            module_environment_policy: policy,
            ..Default::default()
        };
        let text = probe_sources(
            &mut session,
            &BuildOptions {
                input: self.0.join("main.py"),
                out_dir: self.0.join("out"),
                aot: false,
                search_paths: paths,
            },
        )
        .unwrap();
        serde_json::from_str(&text).unwrap()
    }
}
impl Drop for Tree {
    fn drop(&mut self) {
        let _ = std::fs::remove_dir_all(&self.0);
    }
}

#[test]
fn source_probe_catalog_versions_closed_colorsys_and_runtime() {
    let tree = Tree::new();
    tree.write(
        "main.py",
        "import colorsys\nimport math\nimport textwrap\nraise RuntimeError('不得执行')\n",
    );
    let report = tree.report(ModuleEnvironmentPolicy::OrdinaryDynamicPilot, vec![]);
    assert_eq!(report["catalog_version"], source_catalog::VERSION);
    assert_eq!(report["modules"]["colorsys"]["provider"], "cpython_source");
    assert_eq!(report["modules"]["colorsys"]["dependencies"], json!([]));
    assert_eq!(report["modules"]["colorsys"]["version"], "CPython v3.12.13");
    assert_eq!(report["modules"]["math"]["provider"], "csharp_runtime");
    assert_eq!(report["modules"]["textwrap"]["provider"], "csharp_runtime");
    assert!(
        report["modules"]["textwrap"]["source"][0]
            .as_str()
            .unwrap()
            .contains("PyTextWrap.cs:")
    );
    assert_eq!(report["static_closure_complete"], true);
    assert_eq!(report["compiler_verified"], false);
    assert_eq!(report["complete_stdlib"], false);
    assert!(tree.0.join("out/main.source-plan.json").is_file());
}

#[test]
fn source_probe_user_bytes_never_inherit_catalog_provenance() {
    let tree = Tree::new();
    tree.write("main.py", "import colorsys\n");
    tree.write(
        "colorsys.py",
        std::str::from_utf8(source_catalog::source("colorsys").unwrap().unwrap()).unwrap(),
    );
    tree.write(
        "extra/colorsys.py",
        "raise RuntimeError('后续根不得执行')\n",
    );
    let report = tree.report(
        ModuleEnvironmentPolicy::OrdinaryDynamicPilot,
        vec![tree.0.join("extra")],
    );
    let module = &report["modules"]["colorsys"];
    assert_eq!(module["provider"], "user_source");
    assert_eq!(
        module["source"],
        tree.0.join("colorsys.py").to_str().unwrap()
    );
    assert_eq!(module["provenance_authorized"], false);
    assert!(module["catalog"].is_null());
    assert!(module["version"].is_null());
}

#[test]
fn source_probe_missing_dependency_propagates_through_cycles_and_nested_imports() {
    let tree = Tree::new();
    tree.write("main.py", "import first\nimport queue\n");
    tree.write("first.py", "import second\n");
    tree.write(
        "second.py",
        "import first\ndef unused():\n    from absent import *\n",
    );
    let report = tree.report(ModuleEnvironmentPolicy::OrdinaryDynamicPilot, vec![]);
    assert_eq!(
        report["modules"]["first"]["dependency_closure"],
        "unavailable"
    );
    assert_eq!(report["modules"]["absent"]["status"], "dependency_missing");
    assert_eq!(report["modules"]["queue"]["status"], "catalog_unavailable");
    assert_eq!(
        report["modules"]["threading"]["status"],
        "dependency_missing"
    );
    assert_eq!(report["modules"]["heapq"]["status"], "dependency_missing");
    assert_eq!(report["static_closure_complete"], false);
}

#[test]
fn source_probe_unselected_user_dependencies_are_found_without_authority() {
    let tree = Tree::new();
    tree.write("main.py", "import queue\nfrom helper import *\n");
    tree.write(
        "threading.py",
        "import later\nraise RuntimeError('禁止执行')\n",
    );
    tree.write("helper.py", "import math\nfrom nested import child\n");
    tree.write("nested/__init__.py", "\n");
    tree.write("nested/child.py", "import later\n");
    let report = tree.report(ModuleEnvironmentPolicy::OrdinaryDynamicPilot, vec![]);
    for name in ["threading", "helper", "nested", "nested.child"] {
        let module = &report["modules"][name];
        assert_eq!(module["status"], "source_detected_unselected", "{name}");
        assert!(module["source"].as_str().unwrap().ends_with(".py"));
        assert!(module["provider"].is_null());
        assert_eq!(module["provenance_authorized"], false);
    }
    assert_eq!(report["modules"]["later"]["status"], "dependency_missing");
    assert_eq!(report["modules"]["math"]["provider"], "csharp_runtime");
    assert_eq!(report["static_closure_complete"], false);
}

#[test]
fn source_probe_from_catalog_child_requires_provider_without_shadowing_user_attributes() {
    let tree = Tree::new();
    tree.write("main.py", "from html import parser\n");
    let report = tree.report(ModuleEnvironmentPolicy::ExplicitOnly, vec![]);
    assert_eq!(
        report["modules"]["html.parser"]["status"],
        "catalog_unavailable"
    );
    tree.write("html/__init__.py", "parser = 1\n");
    let shadowed = tree.report(ModuleEnvironmentPolicy::ExplicitOnly, vec![]);
    assert_eq!(shadowed["modules"]["html"]["provider"], "user_source");
    assert!(shadowed["modules"].get("html.parser").is_none());
}

#[test]
fn source_probe_parent_pinning_and_from_attributes_do_not_fabricate_modules() {
    let tree = Tree::new();
    tree.write(
        "main.py",
        "import html.parser\nfrom math import sin\nfrom pkg import child\n",
    );
    tree.write("html/__init__.py", "value = 1\n");
    tree.write("other/html/__init__.py", "\n");
    tree.write(
        "other/html/parser.py",
        "raise RuntimeError('禁止跨根拼接')\n",
    );
    tree.write("pkg/__init__.py", "\n");
    tree.write("pkg/child.py", "from . import sibling\n");
    tree.write("pkg/sibling.py", "value = 42\n");
    let report = tree.report(
        ModuleEnvironmentPolicy::OrdinaryDynamicPilot,
        vec![tree.0.join("other")],
    );
    assert_eq!(
        report["modules"]["html.parser"]["status"],
        "dependency_missing"
    );
    assert!(report["modules"].get("math.sin").is_none());
    assert_eq!(report["modules"]["pkg.child"]["provider"], "user_source");
    assert_eq!(report["modules"]["pkg.sibling"]["provider"], "user_source");
}

#[test]
fn source_probe_dynamic_import_aliases_are_uncertain_without_execution() {
    let tree = Tree::new();
    tree.write("main.py", "import importlib as il\nfrom builtins import __import__ as loader\nname = 'colorsys'\nil.import_module(name)\nloader('queue')\nexec('import colorsys')\n");
    let report = tree.report(ModuleEnvironmentPolicy::OrdinaryDynamicPilot, vec![]);
    assert_eq!(report["dynamic_status"], "uncertain");
    assert_eq!(report["uncertainties"].as_array().unwrap().len(), 3);
    assert!(report["modules"].get("colorsys").is_none());
    assert!(report["modules"].get("queue").is_none());
    assert_eq!(report["dynamic_analysis_complete"], false);
}

#[test]
fn source_probe_explicit_only_keeps_catalog_disabled() {
    let tree = Tree::new();
    tree.write("main.py", "import colorsys\n");
    let report = tree.report(ModuleEnvironmentPolicy::ExplicitOnly, vec![]);
    assert_eq!(
        report["modules"]["colorsys"]["status"],
        "catalog_unavailable"
    );
    assert!(report["modules"]["colorsys"]["provider"].is_null());
    assert!(!tree.0.join("out/compiler-sources").exists());
}

#[test]
fn source_probe_search_root_order_and_session_state_are_preserved() {
    let tree = Tree::new();
    tree.write("main.py", "import colorsys\n");
    tree.write("first/colorsys.py", "value = 1\n");
    tree.write("second/colorsys.py", "import absent\n");
    let mut session = Session {
        module_environment_policy: ModuleEnvironmentPolicy::OrdinaryDynamicPilot,
        ..Default::default()
    };
    session
        .default_source_state
        .plan
        .roots
        .insert("previous".into());
    session.default_source_state.output = Some(tree.0.join("original"));
    let text = probe_sources(
        &mut session,
        &BuildOptions {
            input: tree.0.join("main.py"),
            out_dir: tree.0.join("out"),
            aot: true,
            search_paths: vec![tree.0.join("first"), tree.0.join("second")],
        },
    )
    .unwrap();
    let report: Value = serde_json::from_str(&text).unwrap();
    assert_eq!(
        report["modules"]["colorsys"]["source"],
        tree.0.join("first/colorsys.py").to_str().unwrap()
    );
    assert!(report["modules"].get("absent").is_none());
    assert!(session.default_source_state.plan.roots.contains("previous"));
    assert_eq!(
        session.default_source_state.output,
        Some(tree.0.join("original"))
    );
}

#[test]
fn source_probe_branch_aliases_preserve_dynamic_candidates() {
    let tree = Tree::new();
    tree.write(
        "main.py",
        "import importlib as dispatcher\nif False:\n    import math as dispatcher\ndispatcher.import_module('colorsys')\nloader = dispatcher.import_module\nloader('queue')\n",
    );
    let report = tree.report(ModuleEnvironmentPolicy::OrdinaryDynamicPilot, vec![]);
    let candidates = report["uncertainties"].as_array().unwrap();
    assert_eq!(report["dynamic_status"], "uncertain");
    assert_eq!(candidates.len(), 2);
    assert!(
        candidates
            .iter()
            .all(|candidate| { candidate["evidence"]["operation"] == "importlib.import_module" })
    );
    assert_eq!(candidates[0]["evidence"]["literal_target"], "colorsys");
    assert_eq!(candidates[1]["evidence"]["literal_target"], "queue");
    assert!(report["modules"].get("colorsys").is_none());
    assert!(report["modules"].get("queue").is_none());
}
