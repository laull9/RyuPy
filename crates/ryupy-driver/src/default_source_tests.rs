//! 默认来源的生产编排合同；这里只编译/发射真实MIR，不把helper通过当产品JIT验收。
use super::*;
struct Tree(PathBuf);
impl Tree {
    fn new() -> Self {
        let path = crate::test_temp::directory("ryupy-default-source");
        Self(path)
    }
    fn write(&self, name: &str, text: &str) {
        let path = self.0.join(name);
        std::fs::create_dir_all(path.parent().unwrap()).unwrap();
        std::fs::write(path, text).unwrap();
    }
    fn session(&self) -> Session {
        Session {
            module_environment_policy: ModuleEnvironmentPolicy::OrdinaryDynamicPilot,
            default_source_state: DefaultSourceState {
                output: Some(self.0.join("out")),
                ..Default::default()
            },
            ..Default::default()
        }
    }
    fn compile(&self, session: &mut Session) -> Result<ryupy_mir::LinkedProgram> {
        compile_linked(session, &self.0.join("entry.py"), &[], false, None).map(|value| value.0)
    }
}
impl Drop for Tree {
    fn drop(&mut self) {
        let _ = std::fs::remove_dir_all(&self.0);
    }
}

#[test]
fn default_source_policy_explicit_off_aot_fixed_and_token() {
    let tree = Tree::new();
    let (path, kind, token) = source_catalog::resolve(&tree.0.join("out"), "html.parser")
        .unwrap()
        .unwrap();
    let mut session = tree.session();
    let mut source = modules::load_single(&mut session, &path).unwrap();
    source.catalog = Some(token);
    source.kind = kind;
    source.name = "html.parser".into();
    source.package = "html".into();
    let plan = module_environments::plan(&session, &source, false);
    assert!(plan.instance_globals());
    assert!(plan.automatic);
    assert!(!module_environments::plan(&session, &source, true).instance_globals());
    assert!(!optional_sources::enabled(&session, true));
    session.fixed_binding_build = true;
    assert!(!optional_sources::enabled(&session, false));
    assert!(!module_environments::plan(&session, &source, false).instance_globals());
    session.fixed_binding_build = false;
    session.module_environment_policy = ModuleEnvironmentPolicy::ExplicitOnly;
    assert!(!optional_sources::enabled(&session, false));
    assert!(!module_environments::plan(&session, &source, false).instance_globals());
    session.module_environment_policy = ModuleEnvironmentPolicy::OrdinaryDynamicPilot;
    source.catalog = None;
    assert!(!module_environments::plan(&session, &source, false).instance_globals());
}
#[test]
fn default_source_soft_parse_rejection_retains_caller_and_remaps() {
    let tree = Tree::new();
    tree.write(
        "entry.py",
        "import importlib.util as u\nif False:\n    u.find_spec('pkg.bad')\nprint('caller')\n",
    );
    tree.write("pkg/__init__.py", "print('parent')\n");
    tree.write("pkg/bad.py", "def broken(:\n    pass\n");
    let mut session = tree.session();
    let linked = tree.compile(&mut session).unwrap();
    assert!(!session.diagnostics.has_errors());
    assert_eq!(linked.modules[linked.entry].name, "__main__");
    assert!(
        linked
            .modules
            .iter()
            .any(|(_, module)| module.name == "pkg")
    );
    assert!(
        !linked
            .modules
            .iter()
            .any(|(_, module)| module.name == "pkg.bad")
    );
    assert!(
        session
            .default_source_state
            .plan
            .deferred
            .contains_key("pkg.bad")
    );
    assert!(session.default_source_state.diagnostics["pkg.bad"][0].contains("E0001"));
    ryupy_mir::source_provenance::require_linked(&linked).unwrap();
    ryupy_codegen_cil::emit_source_assembly(
        &linked,
        &ryupy_codegen_cil::EmitOptions {
            assembly_name: "soft_bad".into(),
            entry_source_path: None,
        },
    )
    .unwrap();
    tree.write("entry.py", "import pkg.bad\n");
    assert!(tree.compile(&mut tree.session()).is_err());
}
#[test]
fn default_source_soft_to_hard_catalog_dependency_fixed_point() {
    let tree = Tree::new();
    tree.write(
        "entry.py",
        "import importlib.util as u\nu.find_spec('_markupbase')\nimport helper\n",
    );
    tree.write("helper.py", "import html.parser\n");
    tree.write("_markupbase.py", "def broken(:\n    pass\n");
    let mut session = tree.session();
    assert!(tree.compile(&mut session).is_err());
    assert!(session.diagnostics.has_errors());
    assert!(
        !session
            .default_source_state
            .plan
            .deferred
            .contains_key("_markupbase")
    );
    assert!(
        session
            .module_environment_report
            .iter()
            .any(|module| module.module == "_markupbase" && module.hard)
    );
}
#[test]
fn default_source_hint_cycle_and_native_parent_are_inactive() {
    let tree = Tree::new();
    tree.write("entry.py", "import importlib.util as u\nif False:\n    import html.parser\n    u.find_spec('a')\nimport html\n");
    tree.write("a.py", "import b\n");
    tree.write("b.py", "import a\n");
    let mut session = tree.session();
    let linked = tree.compile(&mut session).unwrap_or_else(|error| {
        panic!(
            "{error:#}\n{}\nmodule plan: {}\noptional plan: {:?}",
            session.render_diagnostics(false),
            String::from_utf8_lossy(&session.module_environment_report_json().unwrap()),
            session.default_source_state.plan.runtime_payload()
        );
    });
    for name in ["a", "b", "html", "html.parser", "_markupbase"] {
        assert!(
            linked
                .candidate_modules
                .iter()
                .any(|candidate| candidate == name),
            "{name}"
        );
    }
    assert!(!session.default_source_state.plan.roots.contains("html"));
    assert!(
        session
            .default_source_state
            .plan
            .roots
            .contains("html.parser")
    );
    assert_eq!(
        linked
            .modules
            .iter()
            .filter(|(_, module)| module.name == "a")
            .count(),
        1
    );
    let mut closed = tree.session();
    closed.module_environment_policy = ModuleEnvironmentPolicy::ExplicitOnly;
    assert!(
        tree.compile(&mut closed)
            .unwrap()
            .candidate_modules
            .is_empty()
    );
}
#[test]
fn default_source_only_tagged_failure_is_deferred() {
    let tree = Tree::new();
    tree.write("entry.py", "print('caller')\n");
    let mut session = tree.session();
    let mut source = modules::load_single(&mut session, &tree.0.join("entry.py")).unwrap();
    source.candidate = true;
    source.hard = false;
    let span = ryupy_base::Span::new(source.parsed.file, ryupy_base::TextRange::default());
    let result = optional_sources::compile::<()>(&mut session, &source, |session| {
        session
            .diagnostics
            .push(ryupy_base::Diagnostic::error(span, "未知E0100不能吞").with_code("E0100"));
        Err(HasErrors.into())
    });
    assert!(result.is_err());
    assert!(session.diagnostics.has_errors());
    session.diagnostics = Default::default();
    let result = optional_sources::compile::<()>(&mut session, &source, |session| {
        session.diagnostics.push(
            ryupy_base::Diagnostic::error(span, "明确支持域")
                .with_code("E0100")
                .with_deferred_failure(ryupy_base::diagnostic::DeferredFailure::SupportBoundary),
        );
        Err(HasErrors.into())
    });
    assert!(result.unwrap().is_none());
    assert!(!session.diagnostics.has_errors());
    assert!(
        optional_sources::compile::<()>(&mut session, &source, |_| anyhow::bail!("内部MIR失败"))
            .is_err()
    );
}
#[test]
fn default_source_metadata_full_inventory_auto_failure_explicit_override() {
    let tree = Tree::new();
    tree.write("Demo.dist-info/METADATA", "Name: Demo\nVersion: 1\n");
    tree.write(
        "Demo.dist-info/entry_points.txt",
        "[selected]\nfirst = pkg.first:Thing\n[other]\nlast = cli:main\n",
    );
    let payload = distribution_metadata::automatic(std::slice::from_ref(&tree.0))
        .unwrap()
        .unwrap();
    let (selected, outside) =
        distribution_metadata::targets(&payload, &["selected".into()]).unwrap();
    assert_eq!(selected, ["pkg.first"]);
    assert_eq!(outside, ["cli"]);
    let snapshot: serde_json::Value = serde_json::from_str(&payload).unwrap();
    assert_eq!(
        snapshot["distributions"][0]["files"]
            .as_array()
            .unwrap()
            .len(),
        2
    );
    assert!(
        distribution_metadata::automatic(&[tree.0.join("absent")])
            .unwrap()
            .is_err()
    );
    let mut session = tree.session();
    session.distribution_metadata = Some(tree.0.join("absent.json"));
    assert!(distribution_metadata::prepare(&mut session).is_err());
}
#[test]
fn default_source_final_deployment_rejects_overwrite() {
    let tree = Tree::new();
    let (path, _, token) = source_catalog::resolve(&tree.0, "html.entities")
        .unwrap()
        .unwrap();
    let original = std::fs::read(&path).unwrap();
    assert!(token.authenticates(&path, &original));
    assert!(
        path.parent()
            .unwrap()
            .parent()
            .unwrap()
            .join("manifest.json")
            .is_file()
    );
    std::fs::write(&path, "user content").unwrap();
    assert!(source_catalog::resolve(&tree.0, "html.entities").is_err());
    assert_eq!(std::fs::read_to_string(&path).unwrap(), "user content");
}

#[test]
fn default_source_actual_support_boundary_not_unknown_hir() {
    let tree = Tree::new();
    tree.write(
        "entry.py",
        "import importlib.util as u\nif False:\n    u.find_spec('optional')\nprint('caller')\n",
    );
    tree.write(
        "optional.py",
        "async def run():\n    async with manager:\n        pass\n",
    );
    let mut session = tree.session();
    tree.compile(&mut session).unwrap();
    assert!(
        session
            .default_source_state
            .plan
            .deferred
            .contains_key("optional")
    );
    // 普通多继承已属于Legacy可编译域；未绑定基类在真正激活时处理。
    tree.write("optional.py", "class C(A, B):\n    pass\n");
    tree.compile(&mut tree.session()).unwrap();
    // 保留实际未支持的类关键字边界，不把新的合法MI当成旧拒绝证据。
    tree.write("optional.py", "class C(metaclass=type):\n    pass\n");
    assert!(tree.compile(&mut tree.session()).is_err());
}

#[test]
fn default_source_nested_hints_metadata_and_alias_sets_reach_fixed_point() {
    let tree = Tree::new();
    tree.write("entry.py", "import importlib.util as util\ndef unused():\n    import math as util\nif False:\n    util.find_spec(name='plugin_a')\n");
    tree.write("plugin_a.py", "import importlib.util as u\nimport importlib.metadata as md\nu.find_spec('plugin_b')\nmd.entry_points(group='plugins')\n");
    tree.write("plugin_b.py", "import importlib.util as u\nimport importlib.metadata as md\nu.find_spec('plugin_a')\nmd.entry_points(group='more')\n");
    tree.write("plugin_d.py", "value = 3\n");
    tree.write("plugin_c.py", "value = 2\n");
    tree.write("Demo.dist-info/METADATA", "Name: Demo\nVersion: 1\n");
    tree.write(
        "Demo.dist-info/entry_points.txt",
        "[plugins]\nthird = plugin_c:Thing\n[more]\nfourth = plugin_d:Thing\n[other]\nlast = outside:Thing\n",
    );
    let mut session = tree.session();
    let linked = tree.compile(&mut session).unwrap();
    for name in ["plugin_a", "plugin_b", "plugin_c", "plugin_d"] {
        assert!(
            linked
                .candidate_modules
                .iter()
                .any(|candidate| candidate == name),
            "{name}"
        );
        assert!(session.default_source_state.plan.roots.contains(name));
        assert_eq!(
            linked
                .modules
                .iter()
                .filter(|(_, module)| module.name == name)
                .count(),
            1
        );
    }
    assert!(linked.distribution_metadata.is_some());
    assert!(
        session
            .default_source_state
            .plan
            .outside
            .contains("outside")
    );
}

#[path = "catalog_environment_tests.rs"]
mod catalog_environment_tests;
