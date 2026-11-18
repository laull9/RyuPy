//! 手动源码图审计工具：请求与结果均在仓外，直接使用冻结provider版本、真实resolver和现HIR/AOT守卫。
use super::*;
use serde::Deserialize;

#[derive(Deserialize)]
struct Request {
    entry: PathBuf,
    search_paths: Vec<PathBuf>,
    provider_report: PathBuf,
    output: PathBuf,
}

#[test]
#[ignore = "手动原业务AST审计：需仓外RYUPY_INSTANCE_AUDIT_REQUEST，普通make check无外部业务依赖"]
fn audit_external_instance_graph() {
    use crate::cpython_provider::{ContainerTransfer, Plan, Source, version::PythonVersion};
    let path = std::env::var_os("RYUPY_INSTANCE_AUDIT_REQUEST").expect("仓外审计请求");
    let request: Request = serde_json::from_slice(&std::fs::read(path).unwrap()).unwrap();
    let report: serde_json::Value =
        serde_json::from_slice(&std::fs::read(&request.provider_report).unwrap()).unwrap();
    let version = report["python_version"]
        .as_str()
        .unwrap()
        .split('.')
        .map(|v| v.parse::<u32>().unwrap())
        .collect::<Vec<_>>();
    let mut sess = Session {
        cpython_provider: Some(request.provider_report.clone()),
        ..Default::default()
    };
    sess.cpython_provider_state.plan = Some(Plan {
        version: PythonVersion::new(version[0], version[1], version[2]), root: request.entry.parent().unwrap().into(),
        source: serde_json::from_value::<Source>(serde_json::json!({"project":"frozen-audit", "build_id":"source-graph", "source_url":"frozen-provider-report", "licenses":[]})).unwrap(),
        modules: report["modules"].as_object().unwrap().keys().cloned().collect(), stdlib: Default::default(), declared: Default::default(), packages: Default::default(),
        container_transfer: ContainerTransfer::Proxy, payloads: vec![], delegation: Default::default(), bridge: PathBuf::new(), shim: PathBuf::new(), _shim_build: None, dependency_report: serde_json::Value::Null,
    });
    let (sources, _) =
        modules::load_build_graph(&mut sess, &request.entry, &request.search_paths, true).unwrap();
    stop_if_errors(&sess).unwrap();
    let capabilities = ryupy_base::InstanceCapabilities {
        provider_namespace: false,
        entry_namespace: true,
        aot_class_guards: true,
        direct_globals: true,
        ordinary_name_handlers: true,
        relative_from_import: true,
    };
    let mut rows = Vec::new();
    for source in sources {
        let info = ryupy_hir::ModuleInfo {
            instance_globals: true,
            foreign_class_construction: true,
            instance_capabilities: capabilities,
            name: source.name.clone(),
            package: source.package,
            file: source.file,
            ..Default::default()
        };
        let (_, hir) = ryupy_hir::lower_module(&source.parsed, &info, &source.text);
        let aot = ryupy_hir::validate_instance_aot_inspect_profile(&source.parsed, true);
        let details = |diagnostics: Vec<ryupy_base::Diagnostic>| {
            diagnostics.into_iter().map(|d| {
            let offset = usize::from(d.primary.span.range.start());
            serde_json::json!({"message": d.message, "line": source.text[..offset].bytes().filter(|b| *b == b'\n').count() + 1,
                "offset": offset, "snippet": source.text[offset..].lines().next().unwrap_or("")})
        }).collect::<Vec<_>>()
        };
        rows.push(serde_json::json!({"module": source.name, "source": source.resolved_source_path,
            "kind": format!("{:?}",source.kind), "instance_guards": details(hir), "aot_guards": details(aot)}));
    }
    std::fs::write(request.output, serde_json::to_vec_pretty(&rows).unwrap()).unwrap();
    println!("审计{}个真实源码模块", rows.len());
}
