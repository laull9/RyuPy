//! 模块函数公开绑定的真实源码结构门禁：默认流水线、单次callee冻结和未碰撞热路径。
//! 不手造MIR、不做性能测量；失败保留源码/MIR/CIL，便于父统一编译运行后审证据。
use ryupy_mir::{
    Callee, Constant, FnId, Function, GlobalId, MirType, ModuleExport, Operand, Program, RuntimeFn,
    Rvalue, Statement,
};
use std::path::PathBuf;
use std::sync::atomic::{AtomicU64, Ordering};

static NEXT_ROOT: AtomicU64 = AtomicU64::new(0);
struct Root(PathBuf);
impl Drop for Root {
    fn drop(&mut self) {
        if !std::thread::panicking() {
            let _ = std::fs::remove_dir_all(&self.0);
        }
    }
}
fn root() -> Root {
    let stamp = std::time::SystemTime::now()
        .duration_since(std::time::UNIX_EPOCH)
        .unwrap()
        .as_nanos();
    let sequence = NEXT_ROOT.fetch_add(1, Ordering::Relaxed);
    let path = std::env::temp_dir().join(format!(
        "ryupy-module-binding-structure-{}-{stamp}-{sequence}",
        std::process::id()
    ));
    std::fs::create_dir(&path).unwrap();
    eprintln!("module binding structure evidence: {}", path.display());
    Root(path)
}

const NUMERIC: &str = "
def add(left: int, right: int) -> int:
    return left + right
def numeric_loop(count: int) -> int:
    total = 0
    for index in range(count):
        total = add(total, index)
    return total
def recursive(value: int) -> int:
    if value <= 1:
        return 1
    return value * recursive(value - 1)
def swap(left, right):
    left, right = right, left
    return left + right
def leaf(value):
    return value + value
alias = leaf
";
const COLLISION: &str = "
f = 0
def f(value: int) -> int:
    return value
def replacement(value):
    return value
def callback():
    global f
    f = replacement
    return 3
def capture():
    return f(callback())
recursive_slot = 0
def recursive_slot(value):
    if value == 0:
        return 0
    return recursive_slot(value - 1)
";

fn compile(root: &Root, name: &str, source: &str) -> (Program, Vec<ModuleExport>) {
    let filename = format!("{name}.py");
    std::fs::write(root.0.join(&filename), source).unwrap();
    let mut db = ryupy_base::SourceDb::default();
    let file = db.add(filename.clone(), source);
    let (parsed, diagnostics) = ryupy_syntax::parse(file, source);
    assert!(diagnostics.is_empty(), "{diagnostics:?}");
    let info = ryupy_hir::ModuleInfo {
        instance_globals: false,
        file: filename,
        ..Default::default()
    };
    let (hir, diagnostics) = ryupy_hir::lower_module(&parsed, &info, source);
    assert!(diagnostics.is_empty(), "{diagnostics:?}");
    let (types, diagnostics) = ryupy_ty::check_module(&hir);
    assert!(diagnostics.is_empty(), "{diagnostics:?}");
    let (mut program, exports, diagnostics) = ryupy_mir::build_program(&hir, &types);
    assert!(diagnostics.is_empty(), "{diagnostics:?}");
    // 与普通driver默认调用同一入口；不关闭优化来掩盖提前绑定或冻结失效。
    ryupy_mir::passes::run_pipeline(&mut program, &ryupy_mir::passes::default_pipeline()).unwrap();
    std::fs::write(
        root.0.join(format!("{name}.mir")),
        ryupy_mir::pretty::dump(&program),
    )
    .unwrap();
    (program, exports)
}
fn function<'a>(program: &'a Program, name: &str) -> (FnId, &'a Function) {
    let matches: Vec<_> = program
        .functions
        .iter()
        .filter(|(_, func)| func.name == name)
        .collect();
    assert_eq!(matches.len(), 1, "{name}");
    matches[0]
}
fn rvalues(function: &Function) -> impl Iterator<Item = &Rvalue> {
    function
        .body
        .blocks
        .iter()
        .flat_map(|(_, block)| block.stmts.iter())
        .filter_map(|stmt| match stmt {
            Statement::Assign { value, .. } | Statement::Eval(value) => Some(value),
            _ => None,
        })
}
fn export_slot(program: &Program, exports: &[ModuleExport], name: &str) -> GlobalId {
    let matches: Vec<_> = exports
        .iter()
        .filter(|export| export.name == name)
        .collect();
    assert_eq!(matches.len(), 1, "公开名不能同时导出私有原函数");
    let slot = matches[0].value;
    assert_eq!(program.globals[slot].ty, MirType::Dynamic);
    assert!(program.globals[slot].name.starts_with("__mv"));
    assert!(matches!(
        matches[0].guard,
        ryupy_mir::ExportGuard::ValueSentinel
    ));
    slot
}
fn assert_numeric(program: &Program) {
    let (add, _) = function(program, "add");
    for name in ["add", "numeric_loop", "recursive"] {
        let (_, func) = function(program, name);
        assert_eq!(func.ret, MirType::I64, "{name}");
        assert!(
            func.dynamic_origin.is_none(),
            "未碰撞Static不追加owner: {name}"
        );
        assert_eq!(func.params.len(), if name == "add" { 2 } else { 1 });
        assert!(
            func.params
                .iter()
                .all(|param| func.body.locals[*param].ty == MirType::I64)
        );
        for value in rvalues(func) {
            assert!(
                !matches!(
                    value,
                    Rvalue::DynCallValue { .. }
                        | Rvalue::DynCallMethod { .. }
                        | Rvalue::CallWithArgs { .. }
                        | Rvalue::DynBinary { .. }
                        | Rvalue::Box { .. }
                        | Rvalue::Unbox { .. }
                        | Rvalue::Call {
                            callee: Callee::Runtime(RuntimeFn::LoadName | RuntimeFn::CallValue),
                            ..
                        }
                ),
                "未碰撞数值函数不新增公开读取/动态调用/装箱: {name}: {value:?}"
            );
        }
    }
    let (_, numeric_loop) = function(program, "numeric_loop");
    assert!(
        rvalues(numeric_loop).any(
            |value| matches!(value, Rvalue::Call { callee: Callee::Fn(id), .. } if *id == add)
        )
    );
    assert!(rvalues(numeric_loop).any(|value| matches!(value, Rvalue::Binary { .. })));
    let (recursive, body) = function(program, "recursive");
    assert!(rvalues(body).any(
        |value| matches!(value, Rvalue::Call { callee: Callee::Fn(id), .. } if *id == recursive)
    ));
    let (_, swap) = function(program, "swap");
    for value in rvalues(swap) {
        assert!(
            !matches!(
                value,
                Rvalue::CallWithArgs {
                    callee: RuntimeFn::MakeTuple,
                    ..
                } | Rvalue::Call {
                    callee: Callee::Runtime(
                        RuntimeFn::GetItem
                            | RuntimeFn::UnpackCheck
                            | RuntimeFn::UnpackSequence
                            | RuntimeFn::UnpackEx
                    ),
                    ..
                }
            ),
            "tuple_scalar直接交换仍应消容器: {value:?}"
        );
    }
    let (_, leaf) = function(program, "leaf");
    assert!(leaf.function_global.is_some());
}
fn assert_capture(program: &Program, slot: GlobalId) {
    let (callback, callback_body) = function(program, "callback");
    let callback_source = callback_body.dynamic_origin.unwrap().source_global.unwrap();
    let (_, capture) = function(program, "capture");
    let mut found = false;
    let mut loads = 0;
    for (_, block) in capture.body.blocks.iter() {
        let mut load = None;
        let mut argument = None;
        let mut callback_owner = None;
        for (position, stmt) in block.stmts.iter().enumerate() {
            if let Statement::Assign { dest, value } = stmt {
                match value {
                    Rvalue::Call {
                        callee: Callee::Runtime(RuntimeFn::LoadName),
                        args,
                    } if matches!(args.first(), Some(Operand::Global(id)) if *id == slot) => {
                        loads += 1;
                        load = Some((position, *dest));
                    }
                    Rvalue::Call {
                        callee: Callee::Runtime(RuntimeFn::LoadName),
                        args,
                    } if matches!(args.first(), Some(Operand::Global(id)) if *id == callback_source) =>
                    {
                        assert!(callback_owner.is_none(), "回调实例只能捕获一次");
                        callback_owner = Some((position, *dest));
                    }
                    Rvalue::Call {
                        callee: Callee::Fn(id),
                        args,
                    } if *id == callback => {
                        let (owner_position, owner) = callback_owner.expect("回调需真实私有实例");
                        assert!(load.unwrap().0 < owner_position && owner_position < position);
                        assert!(matches!(args.as_slice(), [Operand::Local(id)] if *id == owner));
                        argument = Some((position, *dest));
                    }
                    Rvalue::DynCallValue {
                        callee: Operand::Local(target),
                        args,
                    } => {
                        let (load_position, saved) = load.expect("必须先读取公开callee");
                        let (argument_position, result) = argument.expect("必须真实执行实参回调");
                        assert!(load_position < argument_position && argument_position < position);
                        assert_eq!(*target, saved, "实参写槽后仍调用先前冻结对象");
                        assert!(matches!(args.as_slice(), [Operand::Local(id)] if *id == result));
                        found = true;
                    }
                    _ => {}
                }
            }
        }
    }
    assert_eq!(loads, 1, "单次调用不能重复读公开槽");
    assert!(found);
}
/// 碰撞函数只能用实际thunk self，不能借普通def的最新私有来源槽。
fn assert_collision_owner(function: &Function) {
    let origin = function
        .dynamic_origin
        .expect("碰撞函数应携真实Dynamic owner");
    assert!(origin.source_global.is_none());
    assert!(function.env.is_none());
    assert_eq!(function.param_meta.len(), 1);
    assert_eq!(function.params.len(), 2);
    assert_eq!(function.params.last(), Some(&origin.owner));
    assert_eq!(function.body.locals[origin.owner].ty, MirType::Dynamic);
    let pushes: Vec<_> = rvalues(function)
        .filter_map(|value| match value {
            Rvalue::Call {
                callee: Callee::Runtime(RuntimeFn::TracePushWithOrigin),
                args,
            } => Some(args),
            _ => None,
        })
        .collect();
    assert_eq!(pushes.len(), 1);
    assert!(
        matches!(pushes[0].as_slice(), [_, _, Operand::Local(owner)] if *owner == origin.owner)
    );
    assert!(!rvalues(function).any(|value| matches!(
        value,
        Rvalue::Call {
            callee: Callee::Runtime(RuntimeFn::TracePush),
            ..
        }
    )));
}

fn assert_publication(program: &Program, slot: GlobalId, selected: FnId) {
    let main = &program.functions[program.entry];
    let mut found = false;
    for (_, block) in main.body.blocks.iter() {
        let mut created = None;
        let mut metadata = None;
        for (position, stmt) in block.stmts.iter().enumerate() {
            match stmt {
                Statement::Assign {
                    dest,
                    value: Rvalue::MakeFunction { func, .. },
                } if *func == selected => created = Some((position, *dest)),
                Statement::Eval(Rvalue::Call {
                    callee: Callee::Runtime(RuntimeFn::FunctionMetadata),
                    args,
                }) if created.is_some_and(
                    |(_, object)| matches!(args.first(), Some(Operand::Local(id)) if *id == object),
                ) =>
                {
                    metadata = Some(position)
                }
                Statement::SetGlobal {
                    dest,
                    value: Operand::Local(value),
                } if *dest == slot && created.is_some_and(|(_, object)| *value == object) => {
                    assert!(created.unwrap().0 < metadata.expect("参数注解元数据必须先成功"));
                    assert!(metadata.unwrap() < position);
                    found = true;
                }
                _ => {}
            }
        }
    }
    assert!(found, "定义创建/元数据/公开必须在同一成功路径上依次发生");
}
fn cil_method<'a>(cil: &'a str, name: &str) -> &'a str {
    cil.split("    .method ")
        .skip(1)
        .find(|method| {
            method
                .lines()
                .next()
                .unwrap()
                .contains(&format!(" {name}("))
        })
        .expect(name)
}

fn instructions(method: &str) -> Vec<&str> {
    method
        .lines()
        .filter_map(|line| {
            line.trim()
                .strip_prefix("IL_")
                .and_then(|line| line.split_once(": ").map(|(_, instruction)| instruction))
        })
        .collect()
}

#[test]
fn module_function_bindings_freeze_public_calls_and_preserve_unselected_static_paths() {
    let root = root();
    let (clean, _) = compile(&root, "clean", NUMERIC);
    assert_numeric(&clean);
    assert!(
        !clean
            .globals
            .iter()
            .any(|(_, global)| global.name.ends_with("_add"))
    );
    let source = format!("{COLLISION}{NUMERIC}");
    let (mixed, exports) = compile(&root, "mixed", &source);
    assert_numeric(&mixed);
    let slot = export_slot(&mixed, &exports, "f");
    let recursion_slot = export_slot(&mixed, &exports, "recursive_slot");
    let (selected, selected_fn) = function(&mixed, "f");
    assert_eq!(selected_fn.ret, MirType::Dynamic);
    assert_collision_owner(selected_fn);
    assert!(
        selected_fn
            .params
            .iter()
            .all(|id| selected_fn.body.locals[*id].ty == MirType::Dynamic)
    );
    assert_capture(&mixed, slot);
    assert_publication(&mixed, slot, selected);
    let (recursive, recursive_fn) = function(&mixed, "recursive_slot");
    assert_collision_owner(recursive_fn);
    assert!(rvalues(recursive_fn).any(|value| matches!(value,
        Rvalue::Call { callee: Callee::Runtime(RuntimeFn::LoadName), args }
        if matches!(args.first(), Some(Operand::Global(id)) if *id == recursion_slot))));
    assert!(rvalues(recursive_fn).any(|value| matches!(value, Rvalue::DynCallValue { .. })));
    let mir = ryupy_mir::pretty::dump(&mixed);
    for id in [selected, recursive] {
        let private = mixed.functions[id].function_global.unwrap();
        let token = format!("global@{}", private.into_raw());
        assert!(
            !mir.split(|c: char| !(c.is_ascii_alphanumeric() || c == '@' || c == '_'))
                .any(|part| part == token),
            "任何源码表达式不能读取选中名私有提前函数对象"
        );
        assert!(mixed.functions.iter().all(|(_, func)| {
            !rvalues(func).any(|value|
            matches!(value, Rvalue::Call { callee: Callee::Fn(callee), .. } if *callee == id))
        }));
    }
    let thunk = mixed
        .functions
        .iter()
        .filter(|(_, func)| func.signature_global.is_some())
        .position(|(id, _)| id == selected)
        .unwrap();
    let linked = ryupy_mir::link(
        vec![(
            mixed,
            ryupy_mir::UnitMeta {
                instance_capabilities: Default::default(),
                execution: ryupy_mir::ModuleExecutionKind::LegacySingleton,
                environment_reason: "existing-contract".into(),
                location: ryupy_mir::ModuleLocation::LegacyDiagnostic,
                file_backed_by_global: false,
                is_package: false,
                protected_exports: vec![],
                name: "__main__".into(),
                package: String::new(),
                file: "mixed.py".into(),
                exports,
            },
        )],
        0,
    );
    let cil = ryupy_codegen_cil::dump_cil(
        &linked,
        &ryupy_codegen_cil::EmitOptions {
            assembly_name: "module_binding_structure".into(),
            entry_source_path: None,
        },
    )
    .unwrap();
    std::fs::write(root.0.join("mixed.cil"), &cil).unwrap();
    let function = cil_method(&cil, "m0_f");
    assert!(
        function
            .lines()
            .next()
            .unwrap()
            .contains("m0_f([System.Runtime]System.Object, [System.Runtime]System.Object)")
    );
    let core_code = instructions(function);
    let pushes: Vec<_> = core_code
        .iter()
        .enumerate()
        .filter(|(_, instruction)| instruction.contains("PyTraceback.PushFrameWithOrigin("))
        .collect();
    assert_eq!(pushes.len(), 1);
    assert_eq!(core_code[pushes[0].0 - 1], "LoadArgument(1)");
    let thunk = cil_method(&cil, &format!("__thunk_{thunk}"));
    let thunk_code = instructions(thunk);
    let calls: Vec<_> = thunk_code
        .iter()
        .enumerate()
        .filter(|(_, instruction)| {
            instruction.starts_with("Call(") && instruction.contains("RyuPy.Generated.Module.m0_f(")
        })
        .collect();
    assert_eq!(calls.len(), 2, "保留fast/slow两条真实core调用");
    for (at, _) in calls {
        assert_eq!(
            thunk_code[at - 1],
            "LoadArgument(0)",
            "尾owner必须是实际self"
        );
    }
    let defaults: Vec<_> = thunk_code
        .iter()
        .enumerate()
        .filter(|(_, instruction)| instruction.contains("PyFunction.get_Defaults()"))
        .collect();
    assert_eq!(defaults.len(), 1);
    assert_eq!(thunk_code[defaults[0].0 - 1], "LoadArgument(0)");
    assert!(
        !thunk.contains("CastToI64") && !thunk.contains("Unbox"),
        "{thunk}"
    );
    assert!(
        thunk.contains(".get_Defaults()"),
        "旧alias默认值必须来自实际函数实例"
    );
    assert!(
        cil_method(&cil, "m0_add")
            .lines()
            .next()
            .unwrap()
            .contains("long m0_add(long, long)")
    );
    let export = linked.modules[linked.entry]
        .exports
        .iter()
        .find(|export| export.name == "f")
        .unwrap();
    let field = &linked.globals[export.value].name;
    assert!(
        !thunk.contains(&format!("LoadStaticField(RyuPy.Generated.Module.{field})")),
        "alias thunk不能重读已被实参副作用更换的公开f"
    );
    assert!(
        cil_method(&cil, "__mod_get_0")
            .contains(&format!("LoadStaticField(RyuPy.Generated.Module.{field})"))
    );
    assert!(
        cil_method(&cil, "__mod_set_0")
            .contains(&format!("StoreStaticField(RyuPy.Generated.Module.{field})"))
    );
    // None哨兵不可代替null未绑定哨兵，初始化必须由MIR真实清槽。
    assert!(linked.functions[linked.modules[linked.entry].init].body.blocks.iter().any(|(_, block)|
        block.stmts.iter().any(|stmt| matches!(stmt, Statement::SetGlobal { dest, value: Operand::Const(Constant::Null) } if *dest == export.value))));
}
