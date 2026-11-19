//! 显式AOT普通类测试桥：真实模块init的MakeFunction给FnId归属，同次发射报告给PE定位。
//! 不扩CLI/Runtime ABI，不从诊断文件名、符号后缀或CIL Call倒推源码身份。
use super::*;

#[derive(serde::Deserialize)]
#[serde(deny_unknown_fields)]
struct Request {
    name: String,
    entry: PathBuf,
    search_paths: Vec<PathBuf>,
    selected: Vec<String>,
    include_modules: Vec<String>,
    resources: PathBuf,
    distribution_metadata: Option<PathBuf>,
}

fn methods(
    linked: &ryupy_mir::LinkedProgram,
    selected: &[String],
    sources: &[manifest::SourceSummary],
) -> serde_json::Value {
    use ryupy_mir::{InstanceStateSource, Rvalue, Statement};
    let mut rows = Vec::new();
    let mut seen = std::collections::BTreeSet::new();
    for (module_id, module) in linked.modules.iter() {
        if !selected.contains(&module.name) {
            continue;
        }
        let summaries: Vec<_> = sources
            .iter()
            .filter(|s| s.module_name == module.name)
            .collect();
        assert_eq!(summaries.len(), 1);
        let source = summaries[0];
        assert_eq!(source.module_id, module_id.into_raw().into_u32());
        for (_, block) in linked.functions[module.init].body.blocks.iter() {
            for statement in &block.stmts {
                let Statement::Assign {
                    value:
                        Rvalue::MakeFunction {
                            func,
                            instance_context: Some(_),
                            ..
                        },
                    ..
                } = statement
                else {
                    continue;
                };
                let function = &linked.functions[*func];
                assert_eq!(
                    function.module_state_source,
                    Some(InstanceStateSource::FunctionParameter)
                );
                let id = func.into_raw().into_u32();
                assert!(seen.insert(id), "同一FnId不能有重复创建/模块归属");
                let definition = function.definition.as_ref().expect("真实方法定义来源");
                rows.push(serde_json::json!({"function_id": id, "module": module.name,
                    "module_id": source.module_id, "source_sha256": source.compiled_text_sha256,
                    "logical_source": source.logical_source, "lexical_qualname": definition.lexical_qualname,
                    "firstlineno": definition.firstlineno, "name": function.name}));
            }
        }
    }
    let owned: std::collections::BTreeSet<_> = linked
        .functions
        .iter()
        .filter(|(_, f)| f.module_state_source == Some(InstanceStateSource::FunctionParameter))
        .map(|(id, _)| id.into_raw().into_u32())
        .collect();
    assert_eq!(
        seen, owned,
        "每个owner必须在实际选中模块init有唯一MakeFunction来源"
    );
    serde_json::Value::Array(rows)
}

#[test]
#[ignore = "需要固定完整源资源及同源hosted身份，必须显式运行"]
fn instance_class_aot_real_source_and_pe_gate() {
    let request = std::env::var("RYUPY_INSTANCE_CLASS_REQUEST").expect("固定请求");
    let output = PathBuf::from(std::env::var("RYUPY_INSTANCE_CLASS_OUT").expect("证据目录"));
    let requests: Vec<Request> = serde_json::from_slice(&std::fs::read(request).unwrap()).unwrap();
    assert!(!requests.is_empty());
    std::fs::create_dir_all(&output).unwrap();
    let mut names = std::collections::BTreeSet::new();
    for item in requests {
        assert!(
            !item.name.is_empty()
                && item
                    .name
                    .bytes()
                    .all(|b| b.is_ascii_alphanumeric() || b == b'-' || b == b'_')
        );
        assert!(names.insert(item.name.clone()));
        assert!(!item.selected.is_empty());
        assert_eq!(item.name, assembly_name(&item.entry));
        let mut session = Session {
            instance_modules: item.selected,
            include_modules: item.include_modules,
            distribution_metadata: item.distribution_metadata,
            module_environment_policy: ModuleEnvironmentPolicy::ExplicitOnly,
            ..Default::default()
        };
        let compiled = compile_linked_locations(
            &mut session,
            &item.entry,
            &item.search_paths,
            true,
            Some(&item.resources),
            true,
        );
        std::fs::write(
            output.join(format!("{}-diagnostics.txt", item.name)),
            session.render_diagnostics(false),
        )
        .unwrap();
        std::fs::write(
            output.join(format!("{}-plan.json", item.name)),
            session.module_environment_report_json().unwrap(),
        )
        .unwrap();
        let (linked, sources, _) = compiled.unwrap();
        std::fs::write(
            output.join(format!("{}-linked-mir.txt", item.name)),
            format!("{linked:#?}"),
        )
        .unwrap();
        for selected in &session.instance_modules {
            assert_eq!(
                linked
                    .modules
                    .iter()
                    .filter(|(_, m)| &m.name == selected)
                    .count(),
                1
            );
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
                assert!(module.instance_capabilities.aot_class_guards);
            } else {
                assert_eq!(
                    module.execution,
                    ryupy_mir::ModuleExecutionKind::LegacySingleton
                );
                assert!(!module.instance_capabilities.aot_class_guards);
            }
        }
        let options = ryupy_codegen_cil::EmitOptions {
            assembly_name: assembly_name(&item.entry),
            entry_source_path: Some(item.entry.to_string_lossy().into_owned()),
        };
        let emitted =
            ryupy_codegen_cil::emit_source_assembly_with_report(&linked, &options).unwrap();
        // 先保存原PE；后续任何结构失败都不能丢原件。
        std::fs::write(output.join(format!("{}.dll", item.name)), &emitted.bytes).unwrap();
        std::fs::write(
            output.join(format!("{}-emission.txt", item.name)),
            format!("{:#?}", emitted.report),
        )
        .unwrap();
        let definitions = methods(&linked, &session.instance_modules, &sources);
        std::fs::write(
            output.join(format!("{}-methods.json", item.name)),
            serde_json::to_vec_pretty(&definitions).unwrap(),
        )
        .unwrap();
        if let Ok(manifest) = std::env::var("RYUPY_INSTANCE_CLASS_SOURCE_MANIFEST") {
            assert!(
                std::env::var_os("GITHUB_ACTIONS").is_none(),
                "本地身份不得覆盖hosted身份"
            );
            let bytes = std::fs::read(manifest).expect("真实冻结源清单");
            super::instance_aot_evidence_tests::save_with_source_manifest(
                &output,
                &item.name,
                &linked,
                &session.instance_modules,
                &sources,
                &emitted,
                &crate::manifest_files::hash_bytes(&bytes),
            );
        } else {
            super::instance_aot_evidence_tests::save(
                &output,
                &item.name,
                &linked,
                &session.instance_modules,
                &sources,
                &emitted,
            );
        }
        for module in &emitted.report.modules {
            if session.instance_modules.contains(&module.module_name) {
                assert!(
                    module.getter.is_none() && module.setter.is_none() && module.exports.is_empty()
                );
            }
        }
    }
}
