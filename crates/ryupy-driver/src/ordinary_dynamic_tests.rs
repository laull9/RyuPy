//! 普通动态模块的计划/旧ABI/真实发射联合合同；不以显式实例成功冒充自动迁移。
use super::*;
use ryupy_mir::{GlobalStorage, MirType, ModuleExecutionKind, source_provenance};
use std::collections::HashSet;
struct Tree(crate::test_temp::Root);
impl Tree {
    fn new() -> Self {
        Self(crate::test_temp::Root::new("ordinary-dynamic"))
    }
    fn write(&self, name: &str, source: &str) {
        let path = self.0.join(name);
        std::fs::create_dir_all(path.parent().unwrap()).unwrap();
        std::fs::write(path, source).unwrap();
    }
    fn compile(&self, policy: ModuleEnvironmentPolicy) -> ryupy_mir::LinkedProgram {
        let mut session = Session {
            module_environment_policy: policy,
            ..Default::default()
        };
        compile_linked(&mut session, &self.0.join("entry.py"), &[], false, None)
            .unwrap_or_else(|error| panic!("{error}: {}", session.render_diagnostics(false)))
            .0
    }
}

// 以真实模块入口、函数与导出表交叉核对来源，不能让待验所属表自证完整。
fn assert_linked_source_authority(linked: &ryupy_mir::LinkedProgram) {
    source_provenance::require_linked(linked).unwrap();
    let sources: Vec<_> = linked
        .globals
        .iter()
        .filter(|(_, global)| matches!(global.storage, GlobalStorage::CodeSource { .. }))
        .collect();
    assert_eq!(sources.len(), linked.modules.len());
    for (id, module) in linked.modules.iter() {
        let prefix = format!("m{}_", id.into_raw().into_u32());
        let owned: Vec<_> = sources
            .iter()
            .copied()
            .filter(|(_, global)| {
                matches!(&global.storage, GlobalStorage::CodeSource { entry, .. } if *entry == module.init)
            })
            .collect();
        assert_eq!(owned.len(), 1, "{}", module.name);
        let (source_id, global) = owned[0];
        let GlobalStorage::CodeSource {
            entry,
            functions,
            legacy_entry,
        } = &global.storage
        else {
            unreachable!()
        };
        assert_eq!(*entry, module.init);
        assert_eq!(global.ty, MirType::Dynamic);
        assert!(global.name.starts_with(&prefix));
        // link为所有真实函数加模块前缀；无signature的生成器resume不属于成员表。
        let expected: HashSet<_> = linked
            .functions
            .iter()
            .filter_map(|(id, function)| {
                (function.codegen_name.starts_with(&prefix) && function.signature_global.is_some())
                    .then_some(id)
            })
            .collect();
        assert_eq!(functions.len(), expected.len());
        assert_eq!(functions.iter().copied().collect::<HashSet<_>>(), expected);
        for function in std::iter::once(entry).chain(functions) {
            assert_eq!(
                source_provenance::for_function(&linked.globals, *function),
                Some(source_id)
            );
        }
        match module.execution {
            ModuleExecutionKind::InstanceGlobals => assert!(legacy_entry.is_none()),
            ModuleExecutionKind::LegacySingleton => {
                let initial = legacy_entry.as_ref().expect("Legacy必须保留真实入口name");
                assert_eq!(initial.initial_name, module.name);
                let names: Vec<_> = module
                    .exports
                    .iter()
                    .filter(|export| export.name == "__name__")
                    .collect();
                assert_eq!(names.len(), 1);
                assert_eq!(initial.name_global, names[0].value);
                let name = &linked.globals[names[0].value];
                assert_eq!(name.storage, GlobalStorage::LegacyStatic);
                assert!(name.name.starts_with(&prefix));
            }
        }
    }
}
#[test]
fn ordinary_dynamic_build_identity_matches_feature() {
    assert_eq!(
        Session::default().module_environment_policy
            == ModuleEnvironmentPolicy::OrdinaryDynamicPilot,
        cfg!(feature = "ordinary-dynamic-module-pilot")
    );
}
#[test]
fn ordinary_dynamic_plan_preserves_boundaries_without_new_errors() {
    let tree = Tree::new();
    tree.write("entry.py", "import subject\n");
    for source in [
        "x = 1\n",
        "def f(x: int) -> int:\n    return x\n",
        "def f():\n    return 1\nclass C:\n    pass\n",
        "def f():\n    try:\n        return 1\n    except ValueError:\n        return 2\n",
        "x: int = 1\ndef f():\n    return x\n",
        "x = [i for i in [1]]\ndef f():\n    return x\n",
    ] {
        tree.write("subject.py", source);
        for policy in [
            ModuleEnvironmentPolicy::ExplicitOnly,
            ModuleEnvironmentPolicy::OrdinaryDynamicPilot,
        ] {
            let linked = tree.compile(policy);
            assert!(
                linked
                    .modules
                    .iter()
                    .all(|(_, module)| module.execution == ModuleExecutionKind::LegacySingleton),
                "{source}"
            );
        }
    }
    tree.write("subject.py", "def f():\n    return 1\n");
    let mut session = Session {
        module_environment_policy: ModuleEnvironmentPolicy::OrdinaryDynamicPilot,
        ..Default::default()
    };
    let (sources, _) = modules::load_program(&mut session, &tree.0.join("entry.py"), &[]).unwrap();
    let subject = sources.iter().find(|m| m.name == "subject").unwrap();
    assert_eq!(
        module_environments::plan(&session, subject, true).reason,
        "aot-not-migrated"
    );
    session.fixed_binding_build = true;
    assert_eq!(
        module_environments::plan(&session, subject, false).reason,
        "fixed-binding-build"
    );
    session.instance_modules.push("subject".into());
    assert_eq!(
        module_environments::plan(&session, subject, false).reason,
        "explicit-instance"
    );
}
#[test]
fn ordinary_dynamic_package_and_dump_entry_keep_their_roles() {
    let tree = Tree::new();
    tree.write("entry.py", "import pkg\nfrom pkg import subject\n");
    tree.write("pkg/__init__.py", "def f():\n    return 1\n");
    tree.write(
        "pkg/subject.py",
        "from . import f\ndef g():\n    return f()\n",
    );
    let linked = tree.compile(ModuleEnvironmentPolicy::OrdinaryDynamicPilot);
    assert!(
        linked
            .modules
            .iter()
            .all(|(_, m)| m.execution == ModuleExecutionKind::LegacySingleton)
    );
    tree.write("pkg/subject.py", "def g():\n    return 3\n");
    tree.write("entry.py", "import pkg.subject as alias\nfrom pkg import subject\nassert subject is alias\nassert alias.g() == 3\n");
    let linked = tree.compile(ModuleEnvironmentPolicy::OrdinaryDynamicPilot);
    for (_, module) in linked.modules.iter() {
        assert_eq!(
            module.execution == ModuleExecutionKind::InstanceGlobals,
            module.name == "pkg.subject"
        );
    }
    let mut session = Session {
        module_environment_policy: ModuleEnvironmentPolicy::OrdinaryDynamicPilot,
        ..Default::default()
    };
    // dump单文件永远是入口角色，不假装它就是build图中的普通依赖模块。
    let text = dump(&mut session, &tree.0.join("pkg/__init__.py"), Stage::Cil).unwrap();
    assert!(text.contains("__mod_get_"));
}
#[test]
fn ordinary_dynamic_emitted_authority_and_mixed_consumers() {
    let tree = Tree::new();
    tree.write(
        "subject.py",
        "value = 10\ndef read(x=1):\n    return value + x\ndef stream():\n    yield value\n",
    );
    tree.write("entry.py", "import subject\nfrom subject import read as saved\ndef typed(x: int) -> int:\n    return x + 1\ndef consumer(x: int) -> int:\n    return subject.read(x)\nassert consumer(2) == 12\nassert saved(typed(1)) == 12\nassert saved.__globals__ is subject.__dict__\nsubject.value = None\nassert subject.stream().__next__() is None\nsubject.value = 'a'\nassert saved('b') == 'ab'\nprint('mixed owner ok')\n");
    let baseline = tree.compile(ModuleEnvironmentPolicy::ExplicitOnly);
    let linked = tree.compile(ModuleEnvironmentPolicy::OrdinaryDynamicPilot);
    assert_linked_source_authority(&baseline);
    assert_linked_source_authority(&linked);
    let (id, subject) = linked
        .modules
        .iter()
        .find(|(_, m)| m.name == "subject")
        .unwrap();
    assert_eq!(subject.execution, ModuleExecutionKind::InstanceGlobals);
    assert!(
        subject
            .environment_reason
            .starts_with("ordinary-dynamic-pilot:")
    );
    assert!(subject.exports.is_empty());
    let prefix = format!("m{}_", id.into_raw().into_u32());
    let selected_globals: Vec<_> = linked
        .globals
        .iter()
        .filter(|(_, global)| global.name.starts_with(&prefix))
        .collect();
    assert!(
        selected_globals
            .iter()
            .any(|(_, global)| matches!(global.storage, GlobalStorage::InstancePublic { .. }))
    );
    let selected_functions: Vec<_> = linked
        .functions
        .iter()
        .filter(|(_, f)| f.codegen_name.starts_with(&prefix))
        .collect();
    let signatures: HashSet<_> = selected_functions
        .iter()
        .filter_map(|(_, function)| function.signature_global)
        .collect();
    let actual_static: HashSet<_> = selected_globals
        .iter()
        .filter_map(|(id, global)| (global.storage == GlobalStorage::LegacyStatic).then_some(*id))
        .collect();
    assert_eq!(actual_static, signatures);
    let source = source_provenance::for_function(&linked.globals, subject.init).unwrap();
    for (global_id, global) in &selected_globals {
        match &global.storage {
            GlobalStorage::InstancePublic { .. } => {}
            GlobalStorage::LegacyStatic => assert!(signatures.contains(global_id)),
            GlobalStorage::CodeSource { entry, .. } => {
                assert_eq!(*global_id, source);
                assert_eq!(*entry, subject.init);
            }
        }
    }
    for (_, function) in selected_functions {
        assert!(
            function.function_global.is_none()
                && function.defaults_global.is_none()
                && function.specialization_identity_global.is_none()
        );
    }
    assert!(
        baseline
            .modules
            .iter()
            .all(|(_, m)| m.execution == ModuleExecutionKind::LegacySingleton)
    );
    let options = ryupy_codegen_cil::EmitOptions {
        assembly_name: "pilot".into(),
        entry_source_path: None,
    };
    let emitted = ryupy_codegen_cil::emit_assembly_with_report(&linked, &options).unwrap();
    let report = emitted
        .report
        .modules
        .iter()
        .find(|m| m.module_name == "subject")
        .unwrap();
    assert!(report.getter.is_none() && report.setter.is_none() && report.exports.is_empty());
    assert_eq!(report.environment_reason, subject.environment_reason);
    let cil = ryupy_codegen_cil::dump_cil(&linked, &options).unwrap();
    assert!(!cil.contains(&format!("__mod_get_{}", id.into_raw().into_u32())));
    if let Ok(path) = std::env::var("RYUPY_P1_STRUCTURE_OUT") {
        let root = PathBuf::from(path);
        std::fs::create_dir_all(&root).unwrap();
        std::fs::write(root.join("pilot.dll"), &emitted.bytes).unwrap();
        std::fs::write(root.join("pilot.cil"), &cil).unwrap();
        std::fs::write(
            root.join("emission-report.txt"),
            format!("{:#?}", emitted.report),
        )
        .unwrap();
        std::fs::write(root.join("linked-mir.txt"), format!("{linked:#?}")).unwrap();
        std::fs::write(root.join("legacy-linked-mir.txt"), format!("{baseline:#?}")).unwrap();
    }
    let dll = tree.0.join("pilot.dll");
    std::fs::write(&dll, emitted.bytes).unwrap();
    std::fs::write(
        tree.0.join("pilot.runtimeconfig.json"),
        ryupy_codegen_cil::runtimeconfig::render(),
    )
    .unwrap();
    runtime::copy_runtime_to(&tree.0).unwrap();
    let actual = dotnet::run_dll(&dll).unwrap();
    assert!(
        actual.status.success(),
        "{}",
        String::from_utf8_lossy(&actual.stderr)
    );
    assert_eq!(
        String::from_utf8_lossy(&actual.stdout).trim(),
        "mixed owner ok"
    );
    assert!(actual.stderr.is_empty());
}

#[test]
fn relative_instance_capability_keeps_deployment_roles() {
    let tree = Tree::new();
    tree.write("entry.py", "import pkg.subject\n");
    tree.write("pkg/__init__.py", "value = 1\n");
    tree.write(
        "pkg/subject.py",
        "from . import value\ndef read():\n    return value\n",
    );
    let mut session = Session {
        instance_modules: vec!["__main__".into(), "pkg".into(), "pkg.subject".into()],
        ..Default::default()
    };
    let (sources, _) = modules::load_program(&mut session, &tree.0.join("entry.py"), &[]).unwrap();
    for source in &sources {
        let explicit = module_environments::plan(&session, source, false);
        assert_eq!(
            explicit.capabilities.relative_from_import,
            source.name == "pkg.subject"
        );
        assert_eq!(
            explicit.capabilities.ordinary_name_handlers,
            source.name == "pkg.subject"
        );
        assert!(
            !module_environments::plan(&session, source, true)
                .capabilities
                .ordinary_name_handlers
        );
        assert!(
            !module_environments::plan(&session, source, true)
                .capabilities
                .relative_from_import
        );
    }
    session.fixed_binding_build = true;
    for source in &sources {
        assert!(
            !module_environments::plan(&session, source, false)
                .capabilities
                .ordinary_name_handlers
        );
        assert!(
            !module_environments::plan(&session, source, false)
                .capabilities
                .relative_from_import
        );
    }
}
