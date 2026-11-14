//! 默认发行策略的来源边界：认证源码自动接入，用户源码和动态发现仍保留独立策略。
use super::*;
use ryupy_mir::ModuleExecutionKind;

#[test]
fn catalog_only_static_source_uses_authenticated_instance_environment() {
    let tree = Tree::new();
    tree.write(
        "entry.py",
        "import colorsys\nprint(colorsys.rgb_to_yiq(1.0, 0.0, 0.0))\n",
    );
    let mut session = tree.session();
    session.module_environment_policy = ModuleEnvironmentPolicy::CatalogOnly;
    let linked = tree.compile(&mut session).unwrap();
    let module = linked
        .modules
        .iter()
        .find(|(_, module)| module.name == "colorsys")
        .unwrap()
        .1;
    assert_eq!(module.execution, ModuleExecutionKind::InstanceGlobals);
    assert_eq!(
        module.environment_reason,
        "compiler-catalog-authenticated-source"
    );
    let row = session
        .module_environment_report
        .iter()
        .find(|row| row.module == "colorsys")
        .unwrap();
    assert!(row.catalog_authenticated && row.hard && row.compiled);
    assert!(default_aot::eligible(&session, true, false));
    session.module_environment_policy = ModuleEnvironmentPolicy::ExplicitOnly;
    assert!(!optional_sources::enabled(&session, false));
    assert!(!default_aot::eligible(&session, true, false));
}

#[test]
fn catalog_only_user_source_keeps_legacy_and_explicit_override() {
    let tree = Tree::new();
    tree.write("ordinary.py", "def value():\n    return 7\n");
    let mut session = tree.session();
    session.module_environment_policy = ModuleEnvironmentPolicy::CatalogOnly;
    let mut source = modules::load_single(&mut session, &tree.0.join("ordinary.py")).unwrap();
    source.name = "ordinary".into();
    source.kind = modules::SourceKind::ModuleFile;
    let plan = module_environments::plan(&session, &source, false);
    assert!(!plan.instance_globals());
    assert_eq!(plan.reason, "policy-disabled");
    session.instance_modules.push("ordinary".into());
    let explicit = module_environments::plan(&session, &source, false);
    assert!(explicit.instance_globals());
    assert!(!explicit.automatic);
}

#[test]
fn catalog_only_dynamic_hint_does_not_materialize_catalog_or_user_module() {
    let tree = Tree::new();
    tree.write("entry.py", "import importlib.util as util\nif False:\n    util.find_spec('colorsys')\n    util.find_spec('unexecuted')\nprint('entry')\n");
    tree.write("unexecuted.py", "raise RuntimeError('不得发现')\n");
    let mut session = tree.session();
    session.module_environment_policy = ModuleEnvironmentPolicy::CatalogOnly;
    let linked = tree.compile(&mut session).unwrap();
    for name in ["colorsys", "unexecuted"] {
        assert!(!linked.modules.iter().any(|(_, module)| module.name == name));
        assert!(!session.default_source_state.plan.roots.contains(name));
    }
}

#[test]
fn catalog_only_same_name_same_bytes_cannot_acquire_catalog_capabilities() {
    let tree = Tree::new();
    let (path, _, _) = source_catalog::resolve(&tree.0.join("catalog"), "colorsys")
        .unwrap()
        .unwrap();
    tree.write("colorsys.py", &std::fs::read_to_string(path).unwrap());
    tree.write("entry.py", "import colorsys\n");
    let mut session = tree.session();
    session.module_environment_policy = ModuleEnvironmentPolicy::CatalogOnly;
    let mut source = modules::load_single(&mut session, &tree.0.join("colorsys.py")).unwrap();
    source.name = "colorsys".into();
    source.kind = modules::SourceKind::ModuleFile;
    assert!(source.catalog.is_none());
    let plan = module_environments::plan(&session, &source, false);
    assert!(!plan.instance_globals());
    assert_eq!(plan.capabilities, Default::default());
    assert_eq!(plan.reason, "policy-disabled");
}
