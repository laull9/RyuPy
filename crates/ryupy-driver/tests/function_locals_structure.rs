//! 普通函数locals编译结构合同：共享槽只惰性建立，调用点同步且保用户同名调用。
#[path = "../src/test_temp.rs"]
mod test_temp;
use ryupy_mir::{Callee, Constant, Function, Operand, RuntimeFn, Rvalue, Statement};

const SOURCE: &str = "
def probe(z, a, /, b=3, *rest, key=4, **extras):
    first = locals()
    late = 8
    return locals()
def excess(callback):
    return locals(callback(), callback(), callback())
def shadowed(locals):
    return locals(3)
def local_shadow():
    locals = lambda x: x
    return locals(4)
def unrelated(value):
    return value + 1
";

fn values(function: &Function) -> Vec<&Rvalue> {
    function
        .body
        .blocks
        .iter()
        .flat_map(|(_, block)| &block.stmts)
        .filter_map(|stmt| match stmt {
            Statement::Assign { value, .. } | Statement::Eval(value) => Some(value),
            _ => None,
        })
        .collect()
}

#[test]
fn function_locals_sync_and_shadowing_structure() {
    let root = test_temp::Root::new("function-locals-structure");
    eprintln!("locals结构原证据: {}", root.display());
    std::fs::write(root.join("structure.py"), SOURCE).unwrap();
    let mut db = ryupy_base::SourceDb::default();
    let file = db.add("structure.py", SOURCE);
    let (parsed, diagnostics) = ryupy_syntax::parse(file, SOURCE);
    assert!(diagnostics.is_empty(), "{diagnostics:?}");
    let (hir, diagnostics) = ryupy_hir::lower_module(&parsed, &Default::default(), SOURCE);
    assert!(diagnostics.is_empty(), "{diagnostics:?}");
    let (types, diagnostics) = ryupy_ty::check_module(&hir);
    assert!(diagnostics.is_empty(), "{diagnostics:?}");
    let (mut program, _, diagnostics) = ryupy_mir::build_program(&hir, &types);
    assert!(diagnostics.is_empty(), "{diagnostics:?}");
    ryupy_mir::passes::run_pipeline(&mut program, &ryupy_mir::passes::default_pipeline()).unwrap();
    std::fs::write(
        root.join("structure.mir"),
        ryupy_mir::pretty::dump(&program),
    )
    .unwrap();
    let function = |name: &str| {
        program
            .functions
            .iter()
            .find(|(_, function)| function.name == name)
            .unwrap()
            .1
    };
    // 两调用点必须写回同一缓存槽；无参热路不构造绑定数组。
    let probe = function("probe");
    let ensures: Vec<_> = probe
        .body
        .blocks
        .iter()
        .flat_map(|(_, block)| &block.stmts)
        .filter_map(|stmt| match stmt {
            Statement::Assign {
                dest,
                value:
                    Rvalue::Call {
                        callee: Callee::Runtime(RuntimeFn::FunctionLocalsEnsure),
                        args,
                    },
            } => Some((*dest, args)),
            _ => None,
        })
        .collect();
    assert_eq!(ensures.len(), 2);
    assert_eq!(ensures[0].0, ensures[1].0);
    let cache = ensures[0].0;
    for (_, args) in ensures {
        assert!(matches!(args.as_slice(), [Operand::Local(slot)] if *slot == cache));
    }
    let probe_values = values(probe);
    let sync_names: Vec<_> = probe_values
        .iter()
        .filter_map(|value| match value {
            Rvalue::Call {
                callee: Callee::Runtime(RuntimeFn::FunctionLocalsSync),
                args,
            } => {
                let [
                    Operand::Local(slot),
                    Operand::Const(Constant::Str(name)),
                    Operand::Local(_),
                ] = args.as_slice()
                else {
                    panic!("同步必须直接读取源码槽: {args:?}")
                };
                assert_eq!(*slot, cache);
                Some(name.as_str())
            }
            _ => None,
        })
        .collect();
    let expected_names = ["z", "a", "b", "key", "rest", "extras", "first", "late"];
    assert_eq!(sync_names, expected_names.repeat(2));
    for value in probe_values {
        if let Rvalue::Call { callee, args } = value {
            assert!(!matches!(
                callee,
                Callee::Runtime(RuntimeFn::FunctionLocalsValidate | RuntimeFn::DynBuiltin)
            ));
            assert!(!args.iter().any(|arg| matches!(arg, Operand::Array(_))));
        }
    }
    for name in ["shadowed", "local_shadow", "unrelated"] {
        let body = values(function(name));
        assert!(
            !body.iter().any(|value| matches!(
                value,
                Rvalue::Call {
                    callee: Callee::Runtime(
                        RuntimeFn::FunctionLocalsEnsure
                            | RuntimeFn::FunctionLocalsSync
                            | RuntimeFn::FunctionLocalsValidate
                    ),
                    ..
                }
            )),
            "{name}: {body:?}"
        );
    }
    // 错误实参也必须完整求值；绑定检查不能提前跳过回调。
    let excess = values(function("excess"));
    assert_eq!(
        excess
            .iter()
            .filter(|value| matches!(value, Rvalue::DynCallValue { args, .. } if args.is_empty()))
            .count(),
        3
    );
    let validate = excess
        .iter()
        .position(|value| {
            matches!(
                value,
                Rvalue::Call {
                    callee: Callee::Runtime(RuntimeFn::FunctionLocalsValidate),
                    ..
                }
            )
        })
        .expect("错误实参仍保绑定检查");
    assert_eq!(
        excess[..validate]
            .iter()
            .filter(|value| matches!(value, Rvalue::DynCallValue { args, .. } if args.is_empty()))
            .count(),
        3
    );
    let ensure = excess
        .iter()
        .position(|value| {
            matches!(
                value,
                Rvalue::Call {
                    callee: Callee::Runtime(RuntimeFn::FunctionLocalsEnsure),
                    ..
                }
            )
        })
        .unwrap();
    assert!(validate < ensure, "非法调用不能先刷新快照");
}
