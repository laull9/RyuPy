//! 生成器类型化合同：状态保存/恢复、None返回与迭代过滤，不锁局部编号。
use ryupy_mir::{
    Callee, Constant, MirType, Operand, Program, RuntimeFn, Rvalue, Statement, Terminator,
};
use std::collections::BTreeSet;

pub const SOURCE: &str = "values = (x * 2 for x in [1, 2] if x > 1)\nprint(list(values))\n";
pub const BASIC: &str = "def values(limit):\n    for value in range(limit):\n        yield value\n    return\n\nprint(list(values(2)))\n";
fn compile(source: &str) -> Program {
    let mut db = ryupy_base::SourceDb::default();
    let file = db.add("generator.py", source);
    let (parsed, errors) = ryupy_syntax::parse(file, source);
    assert!(errors.is_empty());
    let (hir, errors) = ryupy_hir::lower_module(&parsed, &ryupy_hir::ModuleInfo::default(), source);
    assert!(errors.is_empty());
    let (types, errors) = ryupy_ty::check_module(&hir);
    assert!(errors.is_empty());
    let (mut program, _, errors) = ryupy_mir::build_program(&hir, &types);
    assert!(errors.is_empty());
    ryupy_mir::passes::run_pipeline(&mut program, &ryupy_mir::passes::default_pipeline()).unwrap();
    program
}
fn values(program: &Program) -> impl Iterator<Item = &Rvalue> {
    program.functions.iter().flat_map(|(_, f)| {
        f.body.blocks.iter().flat_map(|(_, b)| {
            b.stmts.iter().filter_map(|s| match s {
                Statement::Assign { value, .. } | Statement::Eval(value) => Some(value),
                _ => None,
            })
        })
    })
}
fn check(program: &Program, basic: bool) {
    let generators: Vec<_> = values(program)
        .filter_map(|value| match value {
            Rvalue::MakeGenerator {
                resume,
                frame,
                origin,
            } => Some((*resume, frame, origin)),
            _ => None,
        })
        .collect();
    assert_eq!(generators.len(), 1);
    let (resume, frame, origin) = generators[0];
    assert!(origin.is_some());
    let function = &program.functions[resume];
    assert_eq!(function.params.len(), 1);
    assert_eq!(function.ret, MirType::Dynamic);
    let frame_arg = Operand::Local(function.params[0]);
    let mut reads = BTreeSet::new();
    let mut writes = BTreeSet::new();
    let mut state_writes = 0;
    let mut returns = 0;
    let mut calls = Vec::new();
    let mut mul = 0;
    let mut filter = 0;
    let mut numeric_add = 0;
    let mut numeric_lt = 0;
    for (_, block) in function.body.blocks.iter() {
        assert!(!matches!(block.terminator, Terminator::Suspend { .. }));
        if matches!(block.terminator, Terminator::Return(Some(_))) {
            returns += 1;
        }
        for statement in &block.stmts {
            let value = match statement {
                Statement::Assign { dest, value } => {
                    assert!(
                        !matches!(value, Rvalue::Use(Operand::Const(Constant::I64(_))))
                            || function.body.locals[*dest].ty != MirType::Dynamic
                    );
                    value
                }
                Statement::Eval(value) => value,
                _ => continue,
            };
            if matches!(
                value,
                Rvalue::Binary {
                    op: ryupy_mir::BinOp::Add,
                    ..
                }
            ) {
                numeric_add += 1;
            }
            if matches!(
                value,
                Rvalue::Binary {
                    op: ryupy_mir::BinOp::Lt,
                    ..
                }
            ) {
                numeric_lt += 1;
            }
            if let Rvalue::DynBinary {
                op: ryupy_mir::DynOp::Mul,
                ..
            } = value
            {
                mul += 1;
            }
            if let Rvalue::DynCompare {
                op: ryupy_mir::DynOp::Gt,
                ..
            } = value
            {
                filter += 1;
            }
            if let Rvalue::Call {
                callee: Callee::Runtime(runtime),
                args,
            } = value
            {
                calls.push(*runtime);
                if matches!(runtime, RuntimeFn::ArrayGet | RuntimeFn::ArraySet) {
                    assert_eq!(args[0], frame_arg);
                    let Operand::Const(Constant::I64(slot)) = args[1] else {
                        panic!("状态槽必须显式")
                    };
                    assert!(slot >= 0 && (slot as usize) < frame.len());
                    if *runtime == RuntimeFn::ArrayGet {
                        assert!(reads.insert(slot));
                    } else if slot == 0 {
                        state_writes += 1;
                        writes.insert(slot);
                    } else {
                        assert!(writes.insert(slot));
                    }
                }
            }
        }
    }
    assert_eq!(returns, 2);
    assert_eq!(state_writes, 2); // 产出暂停与终止分别写状态。
    assert_eq!(reads, writes);
    assert_eq!(
        reads,
        (0..frame.len() as i64)
            .filter(|i| *i == 0 || *i >= 4)
            .collect()
    );
    let expected_calls: &[RuntimeFn] = if basic {
        &[RuntimeFn::DynNone, RuntimeFn::GeneratorInput]
    } else {
        &[
            RuntimeFn::DynNone,
            RuntimeFn::GeneratorInput,
            RuntimeFn::IterStart,
            RuntimeFn::IterNextOk,
            RuntimeFn::IterValue,
        ]
    };
    for runtime in expected_calls {
        assert_eq!(
            calls.iter().filter(|r| *r == runtime).count(),
            1,
            "{runtime:?}"
        );
    }
    if basic {
        assert_eq!((numeric_add, numeric_lt), (1, 1));
    } else {
        assert_eq!((mul, filter), (1, 1));
    }
}

pub fn check_with_negative_controls(basic: bool) {
    let source = if basic { BASIC } else { SOURCE };
    let program = compile(source);
    check(&program, basic);
    for removed in [
        RuntimeFn::DynNone,
        RuntimeFn::ArrayGet,
        RuntimeFn::GeneratorInput,
    ] {
        let mut broken = compile(source);
        for (_, f) in broken.functions.iter_mut() {
            for (_, block) in f.body.blocks.iter_mut() {
                for statement in &mut block.stmts {
                    if let Statement::Assign { value, .. } = statement {
                        if matches!(value, Rvalue::Call { callee: Callee::Runtime(runtime), .. } if *runtime == removed)
                        {
                            *value =
                                Rvalue::Use(Operand::Const(if removed == RuntimeFn::DynNone {
                                    Constant::I64(0)
                                } else {
                                    Constant::None
                                }));
                        }
                    }
                }
            }
        }
        assert!(
            std::panic::catch_unwind(|| check(&broken, basic)).is_err(),
            "负控必须拒绝{removed:?}"
        );
    }
}

#[test]
fn generator_resume_and_bare_return_match_python() {
    use std::{fs, process::Command};
    // StopIteration.value必须是Python None；不能只比较repr（CLR null也可能打印None）。
    let source = r#"def finish():
    yield 4
    return

def inspect(g):
    print(next(g))
    try:
        next(g)
    except StopIteration as error:
        print(error.value is None, error.value is not None)
    print(list(g))

def run():
    events = []
    def marked_values():
        events.append('start')
        yield 1
        events.append('resume')
        yield 2
        return
    g = (x * 2 for x in marked_values() if x > 1)
    print(events)
    inspect(g)
    print(events)
    inspect(finish())
    ranged = values(2)
    print(next(ranged), next(ranged))
    try:
        next(ranged)
    except StopIteration as error:
        print(error.value is None, list(ranged))
run()
"#;
    let stamp = std::time::SystemTime::now()
        .duration_since(std::time::UNIX_EPOCH)
        .unwrap()
        .as_nanos();
    let root =
        std::env::temp_dir().join(format!("ryupy-genexpr-none-{}-{stamp}", std::process::id()));
    fs::create_dir(&root).unwrap();
    let input = root.join("probe.py");
    fs::write(&input, format!("{BASIC}{source}")).unwrap();
    let python = std::env::var_os("PYTHON").unwrap_or_else(|| "python3".into());
    let version = Command::new(&python).args(["-c", "import sys; assert sys.implementation.name == 'cpython'; print('.'.join(map(str, sys.version_info[:3])))"]).output().unwrap();
    let pinned =
        fs::read_to_string(ryupy_driver::runtime::workspace_root().join("tests/pinned-python.txt"))
            .unwrap();
    assert!(version.status.success());
    assert_eq!(
        String::from_utf8_lossy(&version.stdout).trim(),
        pinned.trim()
    );
    let expected = Command::new(&python)
        .arg("-B")
        .arg(&input)
        .output()
        .unwrap();
    assert!(
        expected.status.success(),
        "{}",
        String::from_utf8_lossy(&expected.stderr)
    );
    let artifact = ryupy_driver::build(
        &mut ryupy_driver::Session::default(),
        &ryupy_driver::BuildOptions {
            input,
            out_dir: root.join("out"),
            aot: false,
            search_paths: vec![],
        },
    )
    .unwrap();
    let actual = ryupy_driver::dotnet::run_dll(&artifact.dll).unwrap();
    fs::write(root.join("cp.stdout"), &expected.stdout).unwrap();
    fs::write(root.join("jit.stdout"), &actual.stdout).unwrap();
    fs::write(root.join("jit.stderr"), &actual.stderr).unwrap();
    assert!(
        actual.status.success(),
        "{} ({})",
        String::from_utf8_lossy(&actual.stderr),
        root.display()
    );
    assert_eq!(actual.stderr, expected.stderr);
    assert_eq!(actual.stdout, expected.stdout, "{}", root.display());
    fs::remove_dir_all(root).unwrap();
}
