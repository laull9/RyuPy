//! 能力选择与实际模块图严格分离；只编译，不执行模块正文。
use super::*;
struct Tree(PathBuf);
impl Tree {
    fn new() -> Self {
        let stamp = std::time::SystemTime::now()
            .duration_since(std::time::UNIX_EPOCH)
            .unwrap()
            .as_nanos();
        let path =
            std::env::temp_dir().join(format!("ryupy-instance-{}-{stamp}", std::process::id()));
        std::fs::create_dir(&path).unwrap();
        Self(path)
    }
    fn write(&self, name: &str, source: &str) {
        std::fs::write(self.0.join(name), source).unwrap();
    }
}
impl Drop for Tree {
    fn drop(&mut self) {
        let _ = std::fs::remove_dir_all(&self.0);
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
    for resource in [None::<PathBuf>] {
        let mut session = Session {
            instance_modules: vec!["other".into()],
            ..Default::default()
        };
        assert!(
            compile_linked_locations(
                &mut session,
                &tree.0.join("main.py"),
                &[],
                true,
                resource.as_deref(),
                true
            )
            .is_err()
        );
        assert!(session.render_diagnostics(false).contains("需要资源清单"));
    }
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
            instance_modules::validate_selection(&mut session, &[module], 0, false, false).is_ok()
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
