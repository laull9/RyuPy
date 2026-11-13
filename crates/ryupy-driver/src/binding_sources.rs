//! 真实绑定构建的来源事务：仅授权认证 catalog，退出时还原全部会话来源状态与固定绑定标志。
use crate::{BuildOptions, DefaultSourceState, Session, manifest, resources};
use anyhow::Result;
use std::path::Path;

type Compiled = (
    ryupy_mir::LinkedProgram,
    Vec<manifest::SourceSummary>,
    Option<resources::ResourcePlan>,
    Option<serde_json::Value>,
);

/// 调用方已成功解析 BindingPlan；普通调用方设置 fixed 标志不能取得此私有授权。
/// 计划留在返回的 MIR/资源中，下一次 build/probe 不会继承本次权限或候选来源。
pub(crate) fn compile(
    sess: &mut Session,
    opts: &BuildOptions,
    resource_request: Option<&Path>,
    binding_module: &str,
) -> Result<Compiled> {
    compile_context(
        sess,
        opts,
        resource_request.map(|path| resources::ResourceRequestContext {
            path,
            output_roots: &[],
        }),
        binding_module,
    )
}

/// 保留布局拥有者传入的输出边界，不能因进入绑定catalog事务而丢失资源预检。
pub(crate) fn compile_context(
    sess: &mut Session,
    opts: &BuildOptions,
    resource_request: Option<resources::ResourceRequestContext<'_>>,
    binding_module: &str,
) -> Result<Compiled> {
    let previous_fixed = std::mem::replace(&mut sess.fixed_binding_build, true);
    let previous_state = std::mem::replace(
        &mut sess.default_source_state,
        DefaultSourceState {
            binding_catalog: true,
            binding_module: Some(binding_module.into()),
            report_output: Some(opts.out_dir.clone()),
            ..Default::default()
        },
    );
    sess.default_source_state.aot_request =
        crate::default_aot::eligible(sess, opts.aot, resource_request.is_some());
    if crate::optional_sources::enabled(sess, opts.aot) {
        sess.default_source_state.output = Some(opts.out_dir.clone());
    }
    let result = crate::compile_linked_locations_context(
        sess,
        &opts.input,
        &opts.search_paths,
        true,
        resource_request,
        opts.aot,
    );
    let report = sess.default_source_state.pending_report.take();
    sess.default_source_state = previous_state;
    sess.fixed_binding_build = previous_fixed;
    result.map(|(program, sources, resources)| (program, sources, resources, report))
}

#[cfg(test)]
mod tests {
    use super::*;
    #[test]
    fn failed_binding_compile_restores_prior_session_permissions() {
        let mut session = Session::default();
        session.default_source_state.output = Some("prior-output".into());
        session.default_source_state.report_output = Some("prior-report".into());
        session
            .default_source_state
            .plan
            .roots
            .insert("prior-root".into());
        let result = compile(
            &mut session,
            &BuildOptions {
                input: "absent-binding-source-for-state-test.py".into(),
                out_dir: "unused-binding-source-output".into(),
                aot: false,
                search_paths: vec![],
            },
            None,
            "fixture_native",
        );
        assert!(result.is_err());
        assert!(!session.fixed_binding_build);
        assert!(!session.default_source_state.binding_catalog);
        assert!(!session.default_source_state.aot_request);
        assert_eq!(
            session.default_source_state.output.as_deref(),
            Some(Path::new("prior-output"))
        );
        assert_eq!(
            session.default_source_state.report_output.as_deref(),
            Some(Path::new("prior-report"))
        );
        assert!(
            session
                .default_source_state
                .plan
                .roots
                .contains("prior-root")
        );
    }
    #[test]
    fn catalog_resource_and_declared_native_share_one_graph() {
        let root = std::env::temp_dir().join(format!(
            "ryupy-binding-catalog-{}-{}",
            std::process::id(),
            std::time::SystemTime::now()
                .duration_since(std::time::UNIX_EPOCH)
                .unwrap()
                .as_nanos()
        ));
        std::fs::create_dir_all(root.join("pkgdata")).unwrap();
        std::fs::write(root.join("pkgdata/__init__.py"), "value = 1\n").unwrap();
        std::fs::write(root.join("pkgdata/data.txt"), "payload").unwrap();
        std::fs::write(root.join("entry.py"),
            "import colorsys\nimport pkgdata\nimport fixture_native\nprint(colorsys.rgb_to_hsv(1.0, 0.0, 0.0))\n").unwrap();
        let request = root.join("resources.json");
        std::fs::write(
            &request,
            r#"{"schema_version":1,"modules":[{"module":"pkgdata","files":["data.txt"]}]}"#,
        )
        .unwrap();
        let opts = BuildOptions {
            input: root.join("entry.py"),
            out_dir: root.join("out"),
            aot: true,
            search_paths: vec![],
        };
        let mut session = Session {
            module_environment_policy: crate::ModuleEnvironmentPolicy::OrdinaryDynamicPilot,
            ..Default::default()
        };
        let compiled = compile(&mut session, &opts, Some(&request), "fixture_native");
        if let Err(error) = &compiled {
            panic!("{error:?}\n{}", session.render_diagnostics(false));
        }
        let (program, sources, resources, report) = compiled.unwrap();
        assert_eq!(program.modules.len(), sources.len());
        assert!(
            sources
                .iter()
                .any(|source| source.module_name == "colorsys")
        );
        let plan = resources.unwrap();
        assert!(plan.automatic.is_some());
        assert_eq!(plan.claims.len(), 1);
        let report = report.unwrap();
        assert_eq!(
            report["modules"]["fixture_native"]["provider"],
            "c_abi_binding"
        );
        assert_eq!(report["static_closure_complete"], true);
        assert!(!session.default_source_state.binding_catalog);
        assert!(!session.fixed_binding_build);
        std::fs::remove_dir_all(root).unwrap();
    }
}
