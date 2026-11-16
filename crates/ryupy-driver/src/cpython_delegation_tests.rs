//! 路线 A 本地整模块与入口 CPython 委派计划、冲突校验、源码部署及模块图测试。
use super::*;
use crate::cpython_provider::{ContainerTransfer, Plan, Source, native};
use std::collections::BTreeSet;

fn base_session(root: &Path, delegated: Vec<String>, entry: bool) -> Session {
    let mut sess = Session {
        cpython_provider: Some(root.join("provider.json")),
        cpython_modules: delegated.clone(),
        cpython_entry: entry,
        ..Session::default()
    };
    sess.cpython_provider_state.plan = Some(Plan {
        version: native::version::PythonVersion::new(3, 12, 13),
        root: root.to_path_buf(),
        modules: delegated.clone(),
        stdlib: ["sys", "json", "asyncio", "inspect"]
            .into_iter()
            .map(str::to_owned)
            .collect(),
        declared: Default::default(),
        packages: Default::default(),
        container_transfer: ContainerTransfer::Synchronized,
        payloads: vec![],
        delegation: DelegationPlan {
            local_modules: delegated,
            dynamic_modules: vec![],
            local_payloads: vec![],
            entry_script: None,
            compiled_sources: Vec::new(),
        },
        bridge: root.to_path_buf(),
        shim: root.join("shim"),
        _shim_build: None,
        source: Source {
            project: "python-build-standalone".into(),
            build_id: "20260623".into(),
            source_url:
                "https://github.com/astral-sh/python-build-standalone/releases/tag/20260623".into(),
            licenses: vec![],
        },
        dependency_report: serde_json::Value::Null,
    });
    sess
}

#[test]
fn delegation_rejects_invalid_and_conflicting_requests() {
    let root = crate::test_temp::Root::new("delegation-validate");
    for bad in ["__main__", "pkg.sub", "1bad", ""] {
        let sess = Session {
            cpython_modules: vec![bad.into()],
            ..Session::default()
        };
        assert!(validate_requests(&sess, &[], false).is_err(), "{bad}");
    }
    let conflict_include = Session {
        cpython_modules: vec!["routes".into()],
        include_modules: vec!["routes.user".into()],
        ..Session::default()
    };
    assert!(validate_requests(&conflict_include, &[], false).is_err());

    let conflict_instance = Session {
        cpython_modules: vec!["routes".into()],
        instance_modules: vec!["routes".into()],
        ..Session::default()
    };
    assert!(validate_requests(&conflict_instance, &[], false).is_err());

    let conflict_dynamic = Session {
        cpython_modules: vec!["routes".into()],
        cpython_dynamic_modules: vec!["routes".into()],
        ..Session::default()
    };
    assert!(validate_requests(&conflict_dynamic, &[], false).is_err());

    let conflict_entry_instance = Session {
        cpython_entry: true,
        instance_modules: vec!["__main__".into()],
        ..Session::default()
    };
    assert!(validate_requests(&conflict_entry_instance, &[], false).is_err());

    let stdlib: BTreeSet<String> = ["json"].into_iter().map(str::to_owned).collect();
    let third_party: BTreeSet<String> = ["fastapi"].into_iter().map(str::to_owned).collect();
    let declared = BTreeSet::new();
    let entry = root.join("app.py");
    std::fs::write(&entry, "print(1)\n").unwrap();
    assert!(
        resolve_plan(
            &entry,
            &[],
            DelegationRequest {
                local_modules: vec!["missing_mod".into()],
                dynamic_modules: vec![],
                delegate_entry: false,
            },
            &stdlib,
            &third_party,
            &declared,
        )
        .unwrap_err()
        .to_string()
        .contains("未在入口目录或搜索路径中找到")
    );
    assert!(
        resolve_plan(
            &entry,
            &[],
            DelegationRequest {
                local_modules: vec!["json".into()],
                dynamic_modules: vec![],
                delegate_entry: false,
            },
            &stdlib,
            &third_party,
            &declared,
        )
        .unwrap_err()
        .to_string()
        .contains("标准库同名")
    );
    assert!(
        resolve_plan(
            &entry,
            &[],
            DelegationRequest {
                local_modules: vec!["fastapi".into()],
                dynamic_modules: vec![],
                delegate_entry: false,
            },
            &stdlib,
            &third_party,
            &declared,
        )
        .unwrap_err()
        .to_string()
        .contains("第三方包冲突")
    );
}

#[test]
fn delegation_stages_local_package_and_entry_safely() {
    let root = crate::test_temp::Root::new("delegation-stage");
    let src = root.join("src");
    std::fs::create_dir_all(src.join("routes/__pycache__")).unwrap();
    std::fs::write(src.join("routes/__init__.py"), "from . import api\n").unwrap();
    std::fs::write(
        src.join("routes/api.py"),
        "async def ping():\n    return {'ok': True}\n",
    )
    .unwrap();
    std::fs::write(src.join("routes/__pycache__/api.cpython-312.pyc"), b"pyc").unwrap();
    std::fs::write(src.join("routes/.DS_Store"), b"mac").unwrap();
    std::fs::write(root.join("worker.py"), "async def work():\n    return 42\n").unwrap();
    let entry = root.join("probe.py");
    std::fs::write(
        &entry,
        "import asyncio\nfrom routes.api import ping\nasync def run():\n    return await ping()\n",
    )
    .unwrap();

    let plan = resolve_plan(
        &entry,
        &[src],
        DelegationRequest {
            local_modules: vec!["routes".into(), "worker".into()],
            dynamic_modules: vec!["aiosqlite".into()],
            delegate_entry: true,
        },
        &BTreeSet::new(),
        &BTreeSet::new(),
        &BTreeSet::new(),
    )
    .unwrap();

    let python_dir = root.join("dist/python");
    std::fs::create_dir_all(&python_dir).unwrap();
    let (search_path, entry_rel) = stage(&plan, &python_dir).unwrap();
    assert_eq!(search_path.as_deref(), Some("python/ryupy_delegated"));
    assert_eq!(entry_rel.as_deref(), Some("python/ryupy_entry/probe.py"));
    assert!(
        python_dir
            .join("ryupy_delegated/routes/__init__.py")
            .is_file()
    );
    assert!(python_dir.join("ryupy_delegated/routes/api.py").is_file());
    assert!(python_dir.join("ryupy_delegated/worker.py").is_file());
    assert!(python_dir.join("ryupy_entry/probe.py").is_file());
    assert!(
        !python_dir
            .join("ryupy_delegated/routes/__pycache__")
            .exists()
    );
    assert!(!python_dir.join("ryupy_delegated/routes/.DS_Store").exists());
}

#[cfg(unix)]
#[test]
fn delegation_rejects_symlink_escape() {
    use std::os::unix::fs::symlink;
    let root = crate::test_temp::Root::new("delegation-symlink");
    std::fs::create_dir_all(root.join("pkg")).unwrap();
    std::fs::write(root.join("pkg/__init__.py"), "").unwrap();
    std::fs::write(root.join("secret.py"), "x = 1\n").unwrap();
    symlink(root.join("secret.py"), root.join("pkg/leak.py")).unwrap();
    let entry = root.join("app.py");
    std::fs::write(&entry, "import pkg\n").unwrap();

    let plan = resolve_plan(
        &entry,
        &[],
        DelegationRequest {
            local_modules: vec!["pkg".into()],
            dynamic_modules: vec![],
            delegate_entry: false,
        },
        &BTreeSet::new(),
        &BTreeSet::new(),
        &BTreeSet::new(),
    )
    .unwrap();
    let python_dir = root.join("out/python");
    std::fs::create_dir_all(&python_dir).unwrap();
    assert!(
        stage(&plan, &python_dir)
            .unwrap_err()
            .to_string()
            .contains("越过授权源根")
    );
}

#[test]
fn delegated_local_module_enqueues_non_delegated_local_dependencies_without_silent_fallback() {
    let root = crate::test_temp::Root::new("delegation-graph");
    std::fs::create_dir_all(root.join("routes")).unwrap();
    std::fs::write(root.join("routes/__init__.py"), "").unwrap();
    std::fs::write(
        root.join("routes/api.py"),
        "import sync_helper\nimport async_unmarked\nasync def handler():\n    return sync_helper.val\n",
    )
    .unwrap();
    std::fs::write(root.join("sync_helper.py"), "val = 7\n").unwrap();
    std::fs::write(
        root.join("async_unmarked.py"),
        "async def bad():\n    return 1\n",
    )
    .unwrap();
    let entry = root.join("entry.py");
    std::fs::write(
        &entry,
        "import routes.api\nasync def main():\n    return 1\n",
    )
    .unwrap();

    // 显式委派入口与 routes，但 async_unmarked 未委派：图加载必须继续发现 async_unmarked，由编译器报 E0100。
    let mut sess = base_session(&root, vec!["routes".into()], true);
    let plan = sess.cpython_provider_state.plan.as_mut().unwrap();
    plan.delegation.entry_script = Some(DelegatedEntry {
        source: entry.clone(),
        destination: "ryupy_entry/entry.py".into(),
    });
    let (sources, entry_idx) =
        crate::modules::load_build_graph(&mut sess, &entry, &[], false).unwrap();
    assert_eq!(entry_idx, 0);
    let names: BTreeSet<_> = sources.iter().map(|s| s.name.as_str()).collect();
    assert!(names.contains("__main__"));
    assert!(names.contains("sync_helper"));
    assert!(names.contains("async_unmarked"));
    assert!(!names.contains("routes") && !names.contains("routes.api"));
}

#[test]
fn delegated_entry_compiles_stub_and_emits_valid_assembly_and_probe() {
    let root = crate::test_temp::Root::new("delegation-compile");
    std::fs::create_dir_all(root.join("routes")).unwrap();
    std::fs::write(root.join("routes/__init__.py"), "").unwrap();
    std::fs::write(
        root.join("routes/api.py"),
        "async def ping():\n    return 'ok'\n",
    )
    .unwrap();
    std::fs::write(
        root.join("sync_helper.py"),
        "def double(n):\n    return n * 2\n",
    )
    .unwrap();
    let entry = root.join("app.py");
    std::fs::write(
        &entry,
        "import asyncio\nimport routes.api\nimport sync_helper\nasync def run():\n    return await routes.api.ping()\nprint(sync_helper.double(21))\n",
    )
    .unwrap();

    let mut sess = base_session(&root, vec!["routes".into()], true);
    let plan = sess.cpython_provider_state.plan.as_mut().unwrap();
    plan.delegation.entry_script = Some(DelegatedEntry {
        source: entry.clone(),
        destination: "ryupy_entry/app.py".into(),
    });

    let (linked, summaries, _) = crate::compile_linked(&mut sess, &entry, &[], true, None).unwrap();
    assert_eq!(
        linked.external_provider.as_deref(),
        Some(crate::cpython_provider::CONFIG)
    );
    assert_eq!(linked.modules[linked.entry].name, "__main__");
    assert_eq!(
        linked.modules[linked.entry].environment_reason,
        "explicit-cpython-entry"
    );
    assert!(linked.modules.iter().any(|(_, m)| m.name == "sync_helper"));
    assert!(!linked.modules.iter().any(|(_, m)| m.name == "routes"));
    assert!(!sess.module_environment_report[0].compiled);
    assert_eq!(
        sess.module_environment_report[0].execution,
        "CPythonDelegatedEntry"
    );

    let emitted = ryupy_codegen_cil::emit_source_assembly_with_report(
        &linked,
        &ryupy_codegen_cil::EmitOptions {
            assembly_name: "app".into(),
            entry_source_path: Some(entry.to_string_lossy().into_owned()),
        },
    )
    .unwrap();
    let context = crate::manifest::ManifestContext::new(
        emitted.report,
        summaries,
        linked.entry.into_raw().into_u32(),
        &emitted.bytes,
        &sess,
    )
    .unwrap();
    assert_eq!(context.sources[0].module_name, "__main__");

    // 静态探测也应把 __main__ 与 routes 标为 cpython，sync_helper 标为 user_source。
    let (sources, _) = crate::modules::load_build_graph(&mut sess, &entry, &[], false).unwrap();
    let probe =
        crate::source_probe::analyze(&sess, &sources, std::slice::from_ref(&*root)).unwrap();
    assert_eq!(probe["modules"]["__main__"]["provider"], "cpython");
    assert_eq!(
        probe["modules"]["__main__"]["reason"],
        "explicit_cpython_entry"
    );
    assert_eq!(probe["modules"]["routes.api"]["provider"], "cpython");
    assert_eq!(
        probe["modules"]["routes.api"]["reason"],
        "explicit_local_delegation"
    );
    assert_eq!(probe["modules"]["sync_helper"]["provider"], "user_source");
}
