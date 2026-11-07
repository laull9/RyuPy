//! 显式 AOT 两阶段合同；测试只走编译/链接，不发布或运行 Native。
use super::*;
struct Tree(PathBuf);
impl Tree {
    fn new() -> Self {
        let stamp = std::time::SystemTime::now()
            .duration_since(std::time::UNIX_EPOCH)
            .unwrap()
            .as_nanos();
        let root =
            std::env::temp_dir().join(format!("ryupy-instance-aot-{}-{stamp}", std::process::id()));
        std::fs::create_dir(&root).unwrap();
        let tree = Self(root);
        tree.write("entry.py", "from pkg import subject\n");
        tree.write("pkg/__init__.py", "value = 1\n");
        tree.write("pkg/subject.py", "from . import helper\ndef f(x):\n    try:\n        return helper.value + x\n    except ValueError:\n        return 0\n");
        tree.write("pkg/helper.py", "value = 2\n");
        tree
    }
    fn write(&self, name: &str, source: &str) {
        let path = self.0.join(name);
        std::fs::create_dir_all(path.parent().unwrap()).unwrap();
        std::fs::write(path, source).unwrap();
    }
    fn resources(&self, names: &[&str]) -> PathBuf {
        let modules: Vec<_> = names
            .iter()
            .map(|name| serde_json::json!({"module": name, "files": []}))
            .collect();
        self.write(
            "resources.json",
            &serde_json::json!({"schema_version": 1, "modules": modules}).to_string(),
        );
        self.0.join("resources.json")
    }
    fn compile(
        &self,
        session: &mut Session,
        resource: Option<&Path>,
    ) -> Result<ryupy_mir::LinkedProgram> {
        compile_linked_locations(
            session,
            &self.0.join("entry.py"),
            &[],
            false,
            resource,
            true,
        )
        .map(|p| p.0)
    }
}
impl Drop for Tree {
    fn drop(&mut self) {
        let _ = std::fs::remove_dir_all(&self.0);
    }
}
fn session(policy: ModuleEnvironmentPolicy) -> Session {
    Session {
        instance_modules: vec!["pkg.subject".into()],
        module_environment_policy: policy,
        ..Default::default()
    }
}
#[test]
fn explicit_aot_contract_works_without_auto_policy() {
    let tree = Tree::new();
    let resources = tree.resources(&["pkg", "pkg.subject", "pkg.helper"]);
    for policy in [
        ModuleEnvironmentPolicy::ExplicitOnly,
        ModuleEnvironmentPolicy::OrdinaryDynamicPilot,
    ] {
        let mut session = session(policy);
        let linked = tree
            .compile(&mut session, Some(&resources))
            .unwrap_or_else(|e| panic!("{e}: {}", session.render_diagnostics(false)));
        for (_, module) in linked.modules.iter() {
            if module.name == "__main__" {
                continue;
            }
            assert!(matches!(
                module.location,
                ryupy_mir::ModuleLocation::ExecutableRelative(_)
            ));
            if module.name == "pkg.subject" {
                assert_eq!(
                    module.execution,
                    ryupy_mir::ModuleExecutionKind::InstanceGlobals
                );
                assert!(module.instance_capabilities.relative_from_import);
                assert!(module.instance_capabilities.ordinary_name_handlers);
                assert!(linked.functions[module.init].module_state.is_some());
            } else {
                assert_eq!(
                    module.execution,
                    ryupy_mir::ModuleExecutionKind::LegacySingleton
                );
            }
        }
    }
}
#[test]
fn explicit_aot_requires_entire_source_graph_before_compilation() {
    let tree = Tree::new();
    for names in [
        vec![],
        vec!["pkg.subject"],
        vec!["pkg", "pkg.subject"],
        vec!["pkg.helper"],
    ] {
        let resource = tree.resources(&names);
        for policy in [
            ModuleEnvironmentPolicy::ExplicitOnly,
            ModuleEnvironmentPolicy::OrdinaryDynamicPilot,
        ] {
            let mut session = session(policy);
            assert!(tree.compile(&mut session, Some(&resource)).is_err());
            assert!(session.module_environment_report.is_empty());
            assert!(session.render_diagnostics(false).contains("E0100"));
            assert!(session.render_diagnostics(false).contains("锚点"));
        }
    }
}
#[test]
fn explicit_aot_rejects_graph_and_ast_boundaries_before_compilation() {
    let tree = Tree::new();
    let resource = tree.resources(&["pkg", "pkg.subject", "pkg.helper"]);
    for selection in ["__main__", "pkg", "missing"] {
        let mut session = session(ModuleEnvironmentPolicy::ExplicitOnly);
        session.instance_modules = vec![selection.into()];
        assert!(tree.compile(&mut session, Some(&resource)).is_err());
        assert!(session.module_environment_report.is_empty());
        assert!(session.render_diagnostics(false).contains("E0100"));
    }
    // 资源与绑定守卫各用全新会话；先在未改写的合法源码上核两种策略。
    for policy in [
        ModuleEnvironmentPolicy::ExplicitOnly,
        ModuleEnvironmentPolicy::OrdinaryDynamicPilot,
    ] {
        let mut missing_resources = session(policy);
        assert!(tree.compile(&mut missing_resources, None).is_err());
        assert!(missing_resources.module_environment_report.is_empty());
        let diagnostics = missing_resources.render_diagnostics(false);
        assert!(diagnostics.contains("E0100") && diagnostics.contains("需要资源清单"));

        let mut fixed_binding = session(policy);
        fixed_binding.fixed_binding_build = true;
        assert!(tree.compile(&mut fixed_binding, Some(&resource)).is_err());
        assert!(fixed_binding.module_environment_report.is_empty());
        let diagnostics = fixed_binding.render_diagnostics(false);
        assert!(diagnostics.contains("E0100") && diagnostics.contains("不支持原生绑定"));
    }
    // 前置守卫均核完后才构造非法 AST，不把上一策略的源改写带入下一守卫。
    tree.write("pkg/subject.py", "from . import helper\ndef f():\n    def inner():\n        return 1\n    return inner\n");
    for policy in [
        ModuleEnvironmentPolicy::ExplicitOnly,
        ModuleEnvironmentPolicy::OrdinaryDynamicPilot,
    ] {
        let mut session = session(policy);
        assert!(tree.compile(&mut session, Some(&resource)).is_err());
        assert!(session.module_environment_report.is_empty());
        let diagnostics = session.render_diagnostics(false);
        assert!(diagnostics.contains("E0100") && diagnostics.contains("嵌套 def"));
    }
}

/// hosted 显式门：没有固定源码/输出路径时失败，不能把未运行当作成功。
#[test]
#[ignore = "需要固定 wcwidth 六源及独立证据目录；hosted 在 Native publish 前执行"]
fn explicit_aot_real_source_and_pe_gate() {
    let root = PathBuf::from(env!("CARGO_MANIFEST_DIR")).join("../..");
    let source = PathBuf::from(std::env::var("WCWIDTH_SOURCE").expect("固定源码根"));
    let output =
        PathBuf::from(std::env::var("RYUPY_INSTANCE_AOT_STRUCTURE_OUT").expect("证据目录"));
    std::fs::create_dir_all(&output).unwrap();
    let fixtures = root.join("tests/aot/wcwidth_ci/instance_fixtures");
    let hashes: std::collections::BTreeMap<String, String> = serde_json::from_slice(
        &std::fs::read(root.join("tests/aot/wcwidth_ci/source-hashes.json")).unwrap(),
    )
    .unwrap();
    for (name, expected) in hashes {
        let bytes = std::fs::read(source.join("wcwidth").join(name)).unwrap();
        assert_eq!(crate::manifest_files::hash_bytes(&bytes), expected);
    }
    for combined in [false, true] {
        let name = if combined { "combined" } else { "original" };
        let entry = if combined {
            fixtures.join("combined.py")
        } else {
            root.join("tests/aot/wcwidth_ci/01_original.py")
        };
        let mut session = session(ModuleEnvironmentPolicy::ExplicitOnly);
        session.instance_modules = vec!["wcwidth.wcwidth".into()];
        if combined {
            session.instance_modules.extend(
                [
                    "lambda_subject",
                    "cycle_left",
                    "cycle_right",
                    "reentry_subject",
                    "retry_subject",
                ]
                .into_iter()
                .map(str::to_owned),
            );
        }
        let resource = fixtures.join(format!("{name}-resources.json"));
        let (linked, _, _) = compile_linked_locations(
            &mut session,
            &entry,
            std::slice::from_ref(&source),
            true,
            Some(&resource),
            true,
        )
        .unwrap_or_else(|e| panic!("{e}: {}", session.render_diagnostics(false)));
        for selected in &session.instance_modules {
            assert!(
                linked
                    .modules
                    .iter()
                    .any(|(_, module)| &module.name == selected),
                "最终链接图缺少显式选择模块：{selected}"
            );
        }
        let options = ryupy_codegen_cil::EmitOptions {
            assembly_name: name.into(),
            entry_source_path: None,
        };
        let emitted = ryupy_codegen_cil::emit_assembly_with_report(&linked, &options).unwrap();
        let cil = ryupy_codegen_cil::dump_cil(&linked, &options).unwrap();
        // 报告来自本次真实 PE；每个 owner 函数都有初始化时的静态 ldftn 根。
        for (id, function) in linked.functions.iter() {
            if function.module_state_source
                == Some(ryupy_mir::InstanceStateSource::FunctionParameter)
            {
                assert!(
                    emitted.report.delegate_references.iter().any(|reference| {
                        reference.function_id == Some(id.into_raw().into_u32())
                            && reference.source.name == "__init_functions"
                            && reference.target.is_some()
                    }),
                    "缺少静态 owner thunk 根：{}",
                    function.codegen_name
                );
                let thunk_name = format!(" __thunk_{}(", id.into_raw().into_u32());
                let thunk = cil
                    .split("    .method ")
                    .find(|body| {
                        body.lines()
                            .next()
                            .is_some_and(|header| header.contains(&thunk_name))
                    })
                    .expect("实际 CIL 必须包含该 owner thunk");
                assert!(
                    thunk.lines().any(|line| line.contains("call ")
                        && line.contains(&format!("{}(", function.codegen_name))),
                    "owner thunk 必须静态调用目标 core"
                );
            }
        }
        for (_, module) in linked.modules.iter() {
            if module.name != "__main__" {
                assert!(matches!(
                    module.location,
                    ryupy_mir::ModuleLocation::ExecutableRelative(_)
                ));
            }
            if session.instance_modules.contains(&module.name) {
                assert_eq!(
                    module.execution,
                    ryupy_mir::ModuleExecutionKind::InstanceGlobals
                );
                assert!(module.instance_capabilities.relative_from_import);
                assert!(module.instance_capabilities.ordinary_name_handlers);
                let record = emitted
                    .report
                    .modules
                    .iter()
                    .find(|m| m.module_name == module.name)
                    .unwrap();
                assert!(
                    record.getter.is_none() && record.setter.is_none() && record.exports.is_empty()
                );
            } else {
                assert_eq!(
                    module.execution,
                    ryupy_mir::ModuleExecutionKind::LegacySingleton
                );
            }
        }
        let mut lambda_count = 0;
        for (_, function) in linked.functions.iter() {
            for (_, block) in function.body.blocks.iter() {
                for statement in &block.stmts {
                    if let ryupy_mir::Statement::Assign {
                        value:
                            ryupy_mir::Rvalue::MakeFunction {
                                func,
                                env,
                                defaults,
                                instance_context,
                            },
                        ..
                    } = statement
                    {
                        let target = &linked.functions[*func];
                        if target.name == "<lambda>" {
                            lambda_count += 1;
                            assert!(env.is_empty());
                            assert!(instance_context.is_some());
                            assert!(defaults.iter().all(|v| matches!(
                                v,
                                ryupy_mir::Operand::Const(ryupy_mir::Constant::Null)
                            )));
                            assert_eq!(
                                target.module_state_source,
                                Some(ryupy_mir::InstanceStateSource::FunctionParameter)
                            );
                        }
                    }
                }
            }
        }
        assert_eq!(lambda_count, if combined { 6 } else { 1 });
        std::fs::write(output.join(format!("{name}.dll")), emitted.bytes).unwrap();
        std::fs::write(output.join(format!("{name}.cil")), cil).unwrap();
        std::fs::write(
            output.join(format!("{name}-linked-mir.txt")),
            format!("{linked:#?}"),
        )
        .unwrap();
        std::fs::write(
            output.join(format!("{name}-emission.txt")),
            format!("{:#?}", emitted.report),
        )
        .unwrap();
        std::fs::write(
            output.join(format!("{name}-plan.json")),
            session.module_environment_report_json().unwrap(),
        )
        .unwrap();
    }
}

#[test]
fn explicit_aot_rejects_namespace_or_file_package_ancestors() {
    for file_parent in [false, true] {
        let tree = Tree::new();
        std::fs::remove_file(tree.0.join("pkg/__init__.py")).unwrap();
        if file_parent {
            tree.write("pkg.py", "value = 1\n");
        }
        tree.write("entry.py", "import pkg.subject\n");
        let names = if file_parent {
            vec!["pkg", "pkg.subject", "pkg.helper"]
        } else {
            vec!["pkg.subject", "pkg.helper"]
        };
        let resource = tree.resources(&names);
        let mut session = session(ModuleEnvironmentPolicy::ExplicitOnly);
        assert!(tree.compile(&mut session, Some(&resource)).is_err());
        assert!(session.module_environment_report.is_empty());
        assert!(session.render_diagnostics(false).contains("包祖先"));
    }
}

#[test]
fn explicit_aot_resource_parse_errors_are_not_masked() {
    let tree = Tree::new();
    for names in [vec!["unknown"], vec!["pkg", "pkg"]] {
        let resource = tree.resources(&names);
        let mut session = session(ModuleEnvironmentPolicy::ExplicitOnly);
        let error = tree.compile(&mut session, Some(&resource)).unwrap_err();
        assert!(error.to_string().contains("资源模块"));
        assert!(session.module_environment_report.is_empty());
    }
}

#[test]
fn explicit_aot_rejects_missing_intermediate_package() {
    let tree = Tree::new();
    tree.write("entry.py", "import pkg.middle.subject\n");
    tree.write("pkg/middle/subject.py", "def f():\n    return 1\n");
    let resource = tree.resources(&["pkg", "pkg.middle.subject"]);
    let mut session = session(ModuleEnvironmentPolicy::ExplicitOnly);
    session.instance_modules = vec!["pkg.middle.subject".into()];
    assert!(tree.compile(&mut session, Some(&resource)).is_err());
    assert!(session.module_environment_report.is_empty());
    let diagnostics = session.render_diagnostics(false);
    assert!(diagnostics.contains("pkg.middle") && diagnostics.contains("E0100"));
}
