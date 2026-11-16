//! 小Instance调用者的真实发射门：MIR合同独立于PE，定位仅取同次报告的真实ID/slot。
use super::*;
use ryupy_mir::{Callee, InstanceStateSource, Operand, RuntimeFn, Rvalue, Statement};
use serde_json::json;

fn local(operand: &Operand) -> ryupy_mir::LocalId {
    match operand {
        Operand::Local(id) => *id,
        _ => panic!("合同需要独立局部"),
    }
}

// Python参数位于params前缀，隐藏state/owner随后；与既有build_params及stackify一致。
fn parameter(function: &ryupy_mir::Function, name: &str) -> ryupy_mir::LocalId {
    let positions: Vec<_> = function
        .param_meta
        .iter()
        .enumerate()
        .filter(|(_, parameter)| parameter.name == name)
        .collect();
    assert_eq!(positions.len(), 1);
    let id = function.params[positions[0].0];
    assert_eq!(function.body.locals[id].name.as_deref(), Some(name));
    id
}

fn slot(function: &ryupy_mir::Function, id: ryupy_mir::LocalId) -> usize {
    function
        .params
        .iter()
        .position(|value| *value == id)
        .unwrap()
}

fn named(args: &[Operand], index: usize, expected: &str) -> bool {
    matches!(
        args.get(index),
        Some(Operand::Const(ryupy_mir::Constant::Str(name))) if name == expected
    )
}

fn check_init(function: &ryupy_mir::Function) {
    assert_eq!(
        function.module_state_source,
        Some(InstanceStateSource::ModuleEntryParameter)
    );
    let state = function.module_state.unwrap();
    let mut canonical = None;
    let mut stored = false;
    for (_, block) in function.body.blocks.iter() {
        for statement in &block.stmts {
            match statement {
                Statement::Assign {
                    dest,
                    value:
                        Rvalue::Call {
                            callee: Callee::Runtime(RuntimeFn::InstanceLoadName),
                            args,
                        },
                } if named(args, 1, "super") => {
                    assert_eq!(local(&args[0]), state);
                    assert!(canonical.replace(*dest).is_none());
                }
                Statement::Eval(Rvalue::Call {
                    callee: Callee::Runtime(RuntimeFn::InstanceStoreName),
                    args,
                }) if named(args, 1, "alias") => {
                    assert_eq!(local(&args[0]), state);
                    assert_eq!(local(&args[2]), canonical.unwrap());
                    assert!(!stored);
                    stored = true;
                }
                _ => {}
            }
        }
    }
    assert!(stored);
}

fn check_body(function: &ryupy_mir::Function) {
    assert_eq!(
        function.module_state_source,
        Some(InstanceStateSource::FunctionParameter)
    );
    assert!(function.body.regions.is_empty());
    let values: Vec<_> = function
        .body
        .blocks
        .iter()
        .flat_map(|(_, block)| &block.stmts)
        .filter_map(|statement| match statement {
            Statement::Assign { dest, value } => Some((*dest, value)),
            _ => None,
        })
        .collect();
    let loads: Vec<_> = values
        .iter()
        .filter_map(|(dest, value)| match value {
            Rvalue::Call {
                callee: Callee::Runtime(RuntimeFn::InstanceLoadName),
                args,
            } => Some((*dest, args)),
            _ => None,
        })
        .collect();
    match function.name.as_str() {
        "canonical" | "construct" | "direct" => {
            assert_eq!(loads.len(), 1);
            assert_eq!(loads[0].1.len(), 2);
            assert_eq!(local(&loads[0].1[0]), function.module_state.unwrap());
            let name = if function.name == "direct" {
                "super"
            } else {
                "alias"
            };
            assert!(loads[0].1.iter().any(|arg| {
                matches!(arg, Operand::Const(ryupy_mir::Constant::Str(value)) if value == name)
            }));
            if function.name != "canonical" {
                let calls: Vec<_> = values
                    .iter()
                    .filter_map(|(_, value)| match value {
                        Rvalue::DynCallValue { callee, args } => Some((callee, args)),
                        _ => None,
                    })
                    .collect();
                assert_eq!(calls.len(), 1);
                assert_eq!(local(calls[0].0), loads[0].0);
                assert_eq!(calls[0].1.len(), 2);
                assert_eq!(local(&calls[0].1[0]), parameter(function, "start"));
                assert_eq!(local(&calls[0].1[1]), parameter(function, "receiver"));
            }
        }
        "invoke_read" | "invoke_callback" => {
            assert!(loads.is_empty());
            let attribute = if function.name == "invoke_callback" {
                "call"
            } else {
                "read"
            };
            let mut receiver = None;
            let mut flag = None;
            let mut target = None;
            let mut callback = None;
            let mut invoke = None;
            for (index, (dest, value)) in values.iter().enumerate() {
                match value {
                    Rvalue::Use(Operand::Local(source))
                        if *source == parameter(function, "proxy") =>
                    {
                        assert!(receiver.replace(*dest).is_none());
                    }
                    Rvalue::Call {
                        callee: Callee::Runtime(RuntimeFn::IsModule),
                        args,
                    } => {
                        assert_eq!(args.len(), 1);
                        assert_eq!(local(&args[0]), receiver.unwrap());
                        assert!(flag.replace((index, *dest)).is_none());
                    }
                    Rvalue::Call {
                        callee: Callee::Runtime(RuntimeFn::CaptureModuleTarget),
                        args,
                    } => {
                        assert_eq!(args.len(), 2);
                        assert_eq!(local(&args[0]), receiver.unwrap());
                        assert!(named(args, 1, attribute));
                        assert!(target.replace((index, *dest)).is_none());
                    }
                    Rvalue::InstanceZeroCall { callee, .. } => {
                        assert_eq!(local(callee), parameter(function, "callback"));
                        assert!(callback.replace((index, *dest)).is_none());
                    }
                    Rvalue::DynCallMethod {
                        captured: Some(captured),
                        obj,
                        args,
                        name,
                    } => {
                        assert_eq!(name, attribute);
                        assert!(invoke.replace(index).is_none());
                        assert_eq!(local(captured), flag.unwrap().1);
                        assert_eq!(local(obj), target.unwrap().1);
                        if function.name == "invoke_callback" {
                            assert_eq!(args.len(), 1);
                            assert_eq!(local(&args[0]), callback.unwrap().1);
                        } else {
                            assert!(args.is_empty());
                        }
                    }
                    _ => {}
                }
            }
            assert!(flag.unwrap().0 < target.unwrap().0);
            assert!(target.unwrap().0 < invoke.unwrap());
            if function.name == "invoke_callback" {
                assert!(target.unwrap().0 < callback.unwrap().0);
                assert!(callback.unwrap().0 < invoke.unwrap());
            } else {
                assert!(callback.is_none());
            }
        }
        _ => panic!("不在五个调用者函数合同内"),
    }
}

#[test]
#[ignore = "hosted真实小Instance调用者；必须提供新输出目录，不能零覆盖返回"]
fn explicit_super_caller_emit_gate() {
    let root = PathBuf::from(env!("CARGO_MANIFEST_DIR")).join("../..");
    let entry = root.join("tests/aot/wcwidth_ci/super_caller/entry.py");
    let output = PathBuf::from(std::env::var("RYUPY_SUPER_CALLER_OUT").expect("新证据目录"));
    std::fs::create_dir(&output).unwrap();
    let mut session = Session {
        instance_modules: vec!["caller".into()],
        module_environment_policy: ModuleEnvironmentPolicy::ExplicitOnly,
        ..Default::default()
    };
    let compiled = compile_linked_locations(&mut session, &entry, &[], true, None, false);
    std::fs::write(
        output.join("diagnostics.txt"),
        session.render_diagnostics(false),
    )
    .unwrap();
    std::fs::write(
        output.join("plan.json"),
        session.module_environment_report_json().unwrap(),
    )
    .unwrap();
    let (linked, sources, _) = compiled.unwrap();
    std::fs::write(output.join("linked-mir.txt"), format!("{linked:#?}")).unwrap();
    let modules: Vec<_> = linked
        .modules
        .iter()
        .filter(|(_, module)| module.name == "caller")
        .collect();
    assert_eq!(modules.len(), 1);
    let (module_id, module) = modules[0];
    for (_, current) in linked.modules.iter() {
        let expected = if current.name == "caller" {
            ryupy_mir::ModuleExecutionKind::InstanceGlobals
        } else {
            ryupy_mir::ModuleExecutionKind::LegacySingleton
        };
        assert_eq!(current.execution, expected);
    }
    let functions: Vec<_> = linked
        .functions
        .iter()
        .filter(|(_, function)| {
            function.source_file == module.file
                && function.module_state_source == Some(InstanceStateSource::FunctionParameter)
        })
        .collect();
    assert_eq!(functions.len(), 5);
    check_init(&linked.functions[module.init]);
    for (_, function) in &functions {
        check_body(function);
    }
    let options = ryupy_codegen_cil::EmitOptions {
        assembly_name: "super_caller".into(),
        entry_source_path: Some(entry.to_string_lossy().into_owned()),
    };
    let artifact = ryupy_codegen_cil::emit_assembly_with_report(&linked, &options).unwrap();
    std::fs::write(output.join("super_caller.dll"), &artifact.bytes).unwrap();
    std::fs::write(
        output.join("super_caller.runtimeconfig.json"),
        ryupy_codegen_cil::runtimeconfig::render(),
    )
    .unwrap();
    runtime::copy_runtime_to(&output).unwrap();
    std::fs::write(
        output.join("emission.txt"),
        format!("{:#?}", artifact.report),
    )
    .unwrap();
    super::instance_aot_evidence_tests::save(
        &output,
        "super_caller",
        &linked,
        &session.instance_modules,
        &sources,
        &artifact,
    );
    let roles: Vec<_> = functions
        .iter()
        .map(|(id, function)| {
            let parameters: std::collections::BTreeMap<_, _> = function
                .param_meta
                .iter()
                .map(|meta| {
                    (
                        meta.name.clone(),
                        slot(function, parameter(function, &meta.name)),
                    )
                })
                .collect();
            json!({"function_id": id.into_raw().into_u32(), "role": function.name,
                "module_id": module_id.into_raw().into_u32(), "source": function.source_file,
                "state_slot": slot(function, function.module_state.unwrap()),
                "parameter_slots": parameters})
        })
        .collect();
    let init = &linked.functions[module.init];
    let roles = json!({"roles": roles,
        "module_init_function_id": module.init.into_raw().into_u32(),
        "module_init_state_slot": slot(init, init.module_state.unwrap()),
        "scope": "生产driver测试入口同次linked/report发射；不是CLI产物"});
    std::fs::write(
        output.join("roles.json"),
        serde_json::to_vec_pretty(&roles).unwrap(),
    )
    .unwrap();
}
