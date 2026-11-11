//! 认证catalog仍须过完整实例ABI准入；真实四源、混合ABI与普通用户边界分开验证。
use super::*;
use ryupy_mir::ModuleExecutionKind;

fn compile_report(tree: &Tree, session: &mut Session) -> ryupy_mir::LinkedProgram {
    tree.compile(session).unwrap_or_else(|error| {
        panic!(
            "{error:#}\n{}\nplan: {}",
            session.render_diagnostics(false),
            String::from_utf8_lossy(&session.module_environment_report_json().unwrap())
        );
    })
}

#[test]
fn default_source_catalog_mixed_abi_preserves_hard_and_soft() {
    for hard in [true, false] {
        let tree = Tree::new();
        tree.write(
            "entry.py",
            if hard {
                "import html.parser\n"
            } else {
                "import importlib.util as util\nutil.find_spec('html.parser')\n"
            },
        );
        let mut session = tree.session();
        let linked = compile_report(&tree, &mut session);
        for (name, execution, reason) in [
            (
                "html.parser",
                ModuleExecutionKind::InstanceGlobals,
                "compiler-catalog-authenticated-source",
            ),
            (
                "_markupbase",
                ModuleExecutionKind::LegacySingleton,
                "hir-boundary:InstanceSyntaxBoundary",
            ),
            (
                "html",
                ModuleExecutionKind::LegacySingleton,
                "entry-or-package",
            ),
            (
                "html.entities",
                ModuleExecutionKind::LegacySingleton,
                "hir-boundary:NoFunction",
            ),
        ] {
            let module = linked
                .modules
                .iter()
                .find(|(_, module)| module.name == name)
                .unwrap()
                .1;
            assert_eq!(module.execution, execution, "{name}");
            assert_eq!(module.environment_reason, reason, "{name}");
            assert!(matches!(
                module.location,
                ryupy_mir::ModuleLocation::SourceAbsolute(_)
            ));
            assert!(
                linked
                    .candidate_modules
                    .iter()
                    .any(|candidate| candidate == name)
            );
            let row = session
                .module_environment_report
                .iter()
                .find(|row| row.module == name)
                .unwrap();
            assert!(row.catalog_authenticated && row.candidate && row.compiled);
            assert_eq!(row.hard, hard, "{name}");
            let instance = execution == ModuleExecutionKind::InstanceGlobals;
            assert_eq!(row.ordinary_name_handlers, instance, "{name}");
            assert_eq!(row.relative_from_import, instance, "{name}");
        }
        assert!(!session.diagnostics.has_errors());
        assert!(session.default_source_state.plan.deferred.is_empty());
        ryupy_codegen_cil::emit_source_assembly(
            &linked,
            &ryupy_codegen_cil::EmitOptions {
                assembly_name: "catalog_mixed".into(),
                entry_source_path: None,
            },
        )
        .unwrap();
    }
}

#[test]
fn default_source_catalog_explicit_markupbase_keeps_original_rejection() {
    for hard in [true, false] {
        let tree = Tree::new();
        tree.write(
            "entry.py",
            if hard {
                "import _markupbase\n"
            } else {
                "import importlib.util as util\nutil.find_spec('_markupbase')\n"
            },
        );
        let mut session = tree.session();
        session.instance_modules.push("_markupbase".into());
        assert!(tree.compile(&mut session).is_err());
        assert!(
            session
                .diagnostics
                .iter()
                .any(|diagnostic| diagnostic.code == Some("E0100")
                    && diagnostic.message.contains("__class__")
                    && diagnostic.deferred_failure.is_none())
        );
        assert!(
            !session
                .default_source_state
                .plan
                .deferred
                .contains_key("_markupbase")
        );
        let row = session
            .module_environment_report
            .iter()
            .find(|row| row.module == "_markupbase")
            .unwrap();
        assert_eq!(row.reason, "explicit-instance");
        assert_eq!(row.hard, hard);
        assert!(!row.compiled);
    }
}

#[test]
fn default_source_catalog_identity_does_not_grant_user_class_abi() {
    let tree = Tree::new();
    let mut session = tree.session();
    let (path, _, _) = source_catalog::resolve(&tree.0.join("catalog"), "html.parser")
        .unwrap()
        .unwrap();
    let bytes = std::fs::read_to_string(path).unwrap();
    tree.write("html/__init__.py", "");
    tree.write("html/parser.py", &bytes);
    let (mut sources, _) =
        modules::load_program(&mut session, &tree.0.join("html/parser.py"), &[]).unwrap();
    let source = &mut sources[0];
    // 与catalog相同模块名/文件字节，但没有来源令牌，真实普通文件角色也不授class能力。
    source.name = "html.parser".into();
    source.kind = modules::SourceKind::ModuleFile;
    assert_eq!(source.text, bytes);
    assert!(source.catalog.is_none());
    let plan = module_environments::plan(&session, source, false);
    assert!(!plan.instance_globals());
    assert_eq!(plan.capabilities, Default::default());
    tree.write(
        "ordinary.py",
        "class Ordinary:\n    def method(self):\n        return 1\n",
    );
    let mut source = modules::load_single(&mut session, &tree.0.join("ordinary.py")).unwrap();
    source.kind = modules::SourceKind::ModuleFile;
    let plan = module_environments::plan(&session, &source, false);
    assert!(!plan.instance_globals());
    assert_eq!(plan.reason, "hir-boundary:ClassDefinition");
}

#[test]
fn default_source_shared_instance_validator_keeps_attribute_and_environment_boundaries() {
    let mut sources = ryupy_base::SourceDb::default();
    let capabilities = ryupy_base::InstanceCapabilities {
        direct_globals: false,
        ordinary_name_handlers: true,
        relative_from_import: true,
    };
    let control =
        "from . import helper\ndef work():\n    try:\n        pass\n    except:\n        pass\n";
    let file = sources.add("control.py", control);
    let (parsed, diagnostics) = ryupy_syntax::parse(file, control);
    assert!(diagnostics.is_empty());
    assert!(ryupy_hir::has_defined_functions(&parsed));
    assert!(!ryupy_hir::instance_ast_supported(
        &parsed,
        Default::default()
    ));
    assert!(ryupy_hir::instance_ast_supported(&parsed, capabilities));
    for source in [
        "def work(obj):\n    if False:\n        return obj.__class__\n",
        "def work():\n    return globals()\n",
        "def work():\n    try:\n        yield 1\n    except:\n        pass\n",
        "class Blocked:\n    try:\n        pass\n    except:\n        pass\n",
    ] {
        let file = sources.add("blocked.py", source);
        let (parsed, diagnostics) = ryupy_syntax::parse(file, source);
        assert!(diagnostics.is_empty());
        assert!(!ryupy_hir::instance_ast_supported(&parsed, capabilities));
        assert!(!ryupy_hir::instance_ast_supported(
            &parsed,
            Default::default()
        ));
    }
}
