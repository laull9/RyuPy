//! 重建后的会话边界与真实模块编译测试；本轮结果独立于丢失的旧提交。
use super::*;
use ryupy_mir::{Callee, ModuleExecutionKind, RuntimeFn, Rvalue, Statement};

pub(super) struct Tree(pub crate::test_temp::Root);
impl Tree {
    pub(super) fn new() -> Self {
        Self(crate::test_temp::Root::new("foreach-reconstruction"))
    }
    pub(super) fn write(&self, name: &str, text: &str) {
        let path = self.0.join(name);
        std::fs::create_dir_all(path.parent().unwrap()).unwrap();
        std::fs::write(path, text).unwrap();
    }
}
pub(super) fn session(enabled: bool) -> Session {
    Session {
        module_environment_policy: ModuleEnvironmentPolicy::OrdinaryDynamicPilot,
        experimental_foreach_accumulator: enabled,
        ..Default::default()
    }
}
pub(super) fn holders(linked: &ryupy_mir::LinkedProgram) -> usize {
    linked
        .functions
        .iter()
        .map(|(_, f)| {
            f.body
                .blocks
                .iter()
                .flat_map(|(_, b)| &b.stmts)
                .filter(|s| {
                    matches!(
                        s,
                        Statement::Assign {
                            value: Rvalue::Call {
                                callee: Callee::Runtime(RuntimeFn::NumericAccumulatorCreate),
                                ..
                            },
                            ..
                        }
                    )
                })
                .count()
        })
        .sum()
}
pub(super) const FOLD: &str = "def fold(seed, values):\n    total = seed\n    for value in values:\n        total += value\n    return total\n";

#[test]
fn foreach_selection_uses_real_plan_not_reason_or_policy_name() {
    assert!(!Session::default().experimental_foreach_accumulator);
    for selected in [false, true] {
        for automatic in [false, true] {
            for execution in [
                ModuleExecutionKind::LegacySingleton,
                ModuleExecutionKind::InstanceGlobals,
            ] {
                for reason in [
                    "explicit-instance",
                    "ordinary-dynamic-pilot:1-functions",
                    "arbitrary",
                ] {
                    let plan = module_environments::ModuleEnvironmentPlan {
                        capabilities: Default::default(),
                        execution,
                        automatic,
                        reason: reason.into(),
                    };
                    let s = session(selected);
                    let expected = cfg!(feature = "ordinary-dynamic-module-pilot")
                        && selected
                        && automatic
                        && execution == ModuleExecutionKind::InstanceGlobals;
                    assert_eq!(accumulator_options::enabled(&s, &plan), expected);
                    let pipeline = accumulator_options::pipeline(&s, &plan).unwrap();
                    assert_eq!(
                        pipeline
                            .iter()
                            .any(|p| p.name() == "foreach_numeric_accumulator_experimental"),
                        expected
                    );
                }
            }
        }
    }
}

#[test]
fn foreach_conflict_is_rejected_before_input_and_output() {
    let tree = Tree::new();
    let opts = BuildOptions {
        input: tree.0.join("missing.py"),
        out_dir: tree.0.join("out"),
        aot: false,
        search_paths: vec![],
    };
    let mut s = session(true);
    s.experimental_direct_accumulator = true;
    for aot in [false, true] {
        let opts = BuildOptions {
            aot,
            ..opts.clone()
        };
        for resources in [None, Some(tree.0.join("missing-resource.json"))] {
            let error = build_with_resources(&mut s, &opts, resources.as_deref()).unwrap_err();
            assert!(error.to_string().contains("不能同时启用"), "{error}");
        }
        let error = build_configured(
            &mut s,
            &opts,
            None,
            Some(&tree.0.join("missing-binding.json")),
        )
        .unwrap_err();
        assert!(error.to_string().contains("不能同时启用"), "{error}");
    }
    for stage in [Stage::Ast, Stage::Hir, Stage::Types, Stage::Mir, Stage::Cil] {
        let error = dump(&mut s, &opts.input, stage).unwrap_err();
        assert!(error.to_string().contains("不能同时启用"), "{error}");
    }
    assert!(!opts.out_dir.exists());
    assert_eq!(std::fs::read_dir(&tree.0).unwrap().count(), 0);
}

#[test]
fn foreach_real_build_hits_only_automatic_dependency() {
    let tree = Tree::new();
    tree.write(
        "entry.py",
        "import subject\nprint(subject.fold(0, [1, 2]))\n",
    );
    tree.write("subject.py", FOLD);
    for enabled in [false, true] {
        for explicit in [false, true] {
            let mut s = session(enabled);
            if explicit {
                s.instance_modules.push("subject".into());
            }
            let (linked, _, _) = compile_linked(&mut s, &tree.0.join("entry.py"), &[], false, None)
                .unwrap_or_else(|e| panic!("{e}: {}", s.render_diagnostics(false)));
            assert_eq!(
                holders(&linked),
                usize::from(
                    cfg!(feature = "ordinary-dynamic-module-pilot") && enabled && !explicit
                )
            );
            assert_eq!(
                s.module_environment_report
                    .iter()
                    .filter(|r| r.compiled)
                    .count(),
                2
            );
            // dump仍是入口角色，不能拿同文件名猜成依赖模块。
            let mir = dump(&mut s, &tree.0.join("subject.py"), Stage::Mir);
            if explicit {
                assert!(mir.is_err());
            } else {
                assert!(!mir.unwrap().contains("NumericAccumulatorCreate"));
            }
        }
    }
}

#[test]
fn foreach_plan_boundaries_do_not_migrate_other_families() {
    let tree = Tree::new();
    tree.write("entry.py", "import subject\n");
    for source in [
        "def fold(seed: int, n: int) -> int:\n    total = seed\n    for value in range(n):\n        total += value\n    return total\n",
        "class C:\n    pass\ndef fold(seed, values):\n    total = seed\n    for value in values:\n        total += value\n    return total\n",
    ] {
        tree.write("subject.py", source);
        let mut s = session(true);
        let (linked, _, _) =
            compile_linked(&mut s, &tree.0.join("entry.py"), &[], false, None).unwrap();
        assert!(
            linked
                .modules
                .iter()
                .all(|(_, m)| m.execution == ModuleExecutionKind::LegacySingleton)
        );
        assert_eq!(holders(&linked), 0);
    }
    tree.write("subject.py", FOLD);
    let mut s = session(true);
    let (sources, _) = modules::load_program(&mut s, &tree.0.join("entry.py"), &[]).unwrap();
    let subject = sources.iter().find(|m| m.name == "subject").unwrap();
    assert!(!accumulator_options::enabled(
        &s,
        &module_environments::plan(&s, subject, true)
    ));
    s.fixed_binding_build = true;
    assert!(!accumulator_options::enabled(
        &s,
        &module_environments::plan(&s, subject, false)
    ));
    tree.write("pkg/__init__.py", FOLD);
    tree.write("entry.py", "import pkg\n");
    let (linked, _, _) = compile_linked(
        &mut session(true),
        &tree.0.join("entry.py"),
        &[],
        false,
        None,
    )
    .unwrap();
    assert_eq!(holders(&linked), 0);
}
