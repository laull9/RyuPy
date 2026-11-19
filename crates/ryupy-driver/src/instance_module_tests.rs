//! 能力选择与实际模块图严格分离；只编译，不执行模块正文。
use super::*;
struct Tree(crate::test_temp::Root);
impl Tree {
    fn new() -> Self {
        Self(crate::test_temp::Root::new("instance"))
    }
    fn write(&self, name: &str, source: &str) {
        std::fs::write(self.0.join(name), source).unwrap();
    }
}

#[test]
fn instance_selection_does_not_include_unknown_module() {
    let tree = Tree::new();
    tree.write("main.py", "x = 1\n");
    tree.write("other.py", "y = 2\n");
    let mut session = Session {
        instance_modules: vec!["other".into()],
        ..Default::default()
    };
    assert!(compile_linked(&mut session, &tree.0.join("main.py"), &[], false, None).is_err());
    assert!(
        session
            .render_diagnostics(false)
            .contains("不在实际源码依赖图")
    );
}
#[test]
fn instance_selection_preserves_unselected_static_abi() {
    let tree = Tree::new();
    tree.write("main.py", "import other\nx = 1\n");
    tree.write("other.py", "x = injected\nx += 1\nprint(x)\n");
    let mut session = Session {
        instance_modules: vec!["other".into()],
        ..Default::default()
    };
    let (program, _, _) =
        compile_linked(&mut session, &tree.0.join("main.py"), &[], false, None).unwrap();
    assert!(session.diagnostics.is_empty());
    for (_, module) in program.modules.iter() {
        let function = &program.functions[module.init];
        if module.name == "other" {
            assert_eq!(
                module.execution,
                ryupy_mir::ModuleExecutionKind::InstanceGlobals
            );
            assert!(function.module_state.is_some());
            assert!(module.exports.is_empty());
        } else {
            assert_eq!(
                module.execution,
                ryupy_mir::ModuleExecutionKind::LegacySingleton
            );
            assert!(function.module_state.is_none());
            assert!(!module.exports.is_empty());
        }
    }
}

#[test]
fn instance_selection_rejects_aot_without_resource_before_codegen() {
    let tree = Tree::new();
    tree.write("main.py", "import other\n");
    tree.write("other.py", "x = 1\n");
    let mut session = Session {
        instance_modules: vec!["other".into()],
        ..Default::default()
    };
    assert!(
        compile_linked_locations(&mut session, &tree.0.join("main.py"), &[], true, None, true)
            .is_err()
    );
    assert!(session.render_diagnostics(false).contains("需要资源清单"));
}
#[test]
fn instance_dump_rejects_missing_single_module_target() {
    let tree = Tree::new();
    tree.write("main.py", "x = 1\n");
    let mut session = Session {
        instance_modules: vec!["other".into()],
        ..Default::default()
    };
    assert!(dump(&mut session, &tree.0.join("main.py"), Stage::Mir).is_err());
    assert!(session.render_diagnostics(false).contains("单模块图"));
}

#[test]
fn instance_selection_preserves_preexisting_parse_diagnostics() {
    let tree = Tree::new();
    tree.write("main.py", "x =\n");
    for selected in [false, true] {
        let mut session = Session::default();
        let module = modules::load_single(&mut session, &tree.0.join("main.py")).unwrap();
        assert!(session.diagnostics.has_errors());
        if selected {
            session.instance_modules.push(module.name.clone());
        }
        let before = session.render_diagnostics(false);
        assert!(
            instance_modules::validate_selection(&mut session, &[module], 0, false, false, &[])
                .is_ok()
        );
        assert_eq!(session.render_diagnostics(false), before);
    }
}

#[test]
fn instance_dump_ast_preserves_parse_error_stage_boundary() {
    let tree = Tree::new();
    tree.write("main.py", "x =\n");
    for selection in [vec![], vec!["__main__".into()]] {
        let mut session = Session {
            instance_modules: selection,
            ..Default::default()
        };
        assert!(dump(&mut session, &tree.0.join("main.py"), Stage::Ast).is_ok());
        assert!(session.diagnostics.has_errors());
    }
    let mut session = Session {
        instance_modules: vec!["missing".into()],
        ..Default::default()
    };
    assert!(dump(&mut session, &tree.0.join("main.py"), Stage::Ast).is_err());
    assert!(session.render_diagnostics(false).contains("单模块图"));
}

#[test]
fn direct_globals_original_thirteen_fixtures_reject_in_real_session() {
    let root = PathBuf::from(env!("CARGO_MANIFEST_DIR"))
        .join("../../tests/validation/instance_globals/negative");
    let cases: serde_json::Value =
        serde_json::from_slice(&std::fs::read(root.join("CASES.json")).unwrap()).unwrap();
    let cases = cases["cases"].as_array().unwrap();
    assert_eq!(cases.len(), 13);
    for case in cases {
        let id = case["id"].as_str().unwrap();
        let selected: Vec<String> = case["selected"]
            .as_array()
            .unwrap()
            .iter()
            .map(|v| v.as_str().unwrap().to_owned())
            .collect();
        assert_eq!(case["policy"], "OrdinaryDynamicPilot");
        assert_eq!(case["aot"], false);
        let input = root.join(case["input"].as_str().unwrap());
        let source = match id {
            "entry" => input.clone(),
            "package" => input.parent().unwrap().join("pkg/__init__.py"),
            _ => input.parent().unwrap().join("subject.py"),
        };
        let text = std::fs::read_to_string(&source).unwrap();
        let syntax = match id {
            "bare" => "return globals\n",
            "positional" | "parameter_positional" => "return globals(1)",
            "star" => "globals(*[])",
            "keyword_star" => "globals(**{})",
            "list_comp" => "[x for x in globals()]",
            "set_comp" => "{x for x in globals()}",
            "dict_comp" => "{x: x for x in globals()}",
            "generator_expr" => "(x for x in globals())",
            "generator_function" => "yield globals()",
            "default_off" => "return globals()",
            "entry" | "package" => "top = globals()",
            _ => panic!("未知手写输入ID：{id}"),
        };
        assert!(text.contains(syntax), "{id}: {text}");
        if id == "parameter_positional" {
            assert!(text.contains("def f(globals)"));
        }
        let mut session = Session {
            module_environment_policy: ModuleEnvironmentPolicy::OrdinaryDynamicPilot,
            instance_modules: selected,
            instance_direct_globals: case["instance_direct_globals"].as_bool().unwrap(),
            ..Default::default()
        };
        let error = match compile_linked(&mut session, &input, &[], false, None) {
            Ok(_) => panic!("{id}: unexpectedly compiled"),
            Err(error) => error,
        };
        assert!(error.is::<HasErrors>(), "{id}: {error:#}");
        assert!(session.render_diagnostics(false).contains("E0100"), "{id}");
        println!("A1_REJECT {id}: HasErrors/E0100");
    }
}
