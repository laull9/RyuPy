//! 裸return必须产生Python None，不泄漏CLR null；真实CP/JIT与类型化MIR双门。
use ryupy_driver::{BuildOptions, Session};
use ryupy_mir::{Callee, Constant, MirType, Operand, RuntimeFn, Rvalue, Statement};
use std::{fs, path::PathBuf, process::Command};

const SOURCE: &str = r#"def bare(x):
    if x:
        return
    return 'kept'
def explicit(x):
    if x:
        return None
    return 'kept'
def implicit(x):
    if not x:
        return 'kept'
def annotated(x: bool) -> int:
    if x:
        return
    return 7
def unit() -> None:
    return
def scalar(x: int) -> int:
    return x + 1
def cleanup(x):
    try:
        if x:
            return
        return 'kept'
    finally:
        print('cleanup')
def override():
    try:
        return 'discard'
    finally:
        return
def run():
    for fn in [bare, explicit, implicit, annotated, cleanup]:
        v = fn(True)
        print(v is None, v is not None, str(v), repr(v), type(v).__name__)
        print(fn(False))
    print(unit() is None, scalar(3), override() is None)
run()
"#;

struct Root(PathBuf);
impl Root {
    fn new() -> Self {
        let stamp = std::time::SystemTime::now()
            .duration_since(std::time::UNIX_EPOCH)
            .unwrap()
            .as_nanos();
        let path =
            std::env::temp_dir().join(format!("ryupy-bare-return-{}-{stamp}", std::process::id()));
        fs::create_dir(&path).unwrap();
        eprintln!("bare return evidence: {}", path.display());
        Self(path)
    }
}
impl Drop for Root {
    fn drop(&mut self) {
        if !std::thread::panicking() {
            let _ = fs::remove_dir_all(&self.0);
        }
    }
}

#[test]
fn bare_return_matches_none_across_finally_and_static_abi() {
    let root = Root::new();
    let input = root.0.join("returns.py");
    fs::write(&input, SOURCE).unwrap();
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
    let expected = Command::new(python).arg("-B").arg(&input).output().unwrap();
    fs::write(root.0.join("cp.stdout"), &expected.stdout).unwrap();
    fs::write(root.0.join("cp.stderr"), &expected.stderr).unwrap();
    assert!(expected.status.success());
    let mut session = Session::default();
    let artifact = ryupy_driver::build(
        &mut session,
        &BuildOptions {
            input,
            out_dir: root.0.join("out"),
            aot: false,
            search_paths: vec![],
        },
    )
    .unwrap();
    let actual = ryupy_driver::dotnet::run_dll(&artifact.dll).unwrap();
    fs::write(root.0.join("jit.stdout"), &actual.stdout).unwrap();
    fs::write(root.0.join("jit.stderr"), &actual.stderr).unwrap();
    assert!(
        actual.status.success(),
        "{}",
        String::from_utf8_lossy(&actual.stderr)
    );
    assert_eq!(actual.stderr, expected.stderr);
    assert_eq!(actual.stdout, expected.stdout);
}

#[test]
fn bare_return_mir_keeps_python_none_and_static_calling_conventions() {
    let mut db = ryupy_base::SourceDb::default();
    let file = db.add("returns.py", SOURCE);
    let (parsed, errors) = ryupy_syntax::parse(file, SOURCE);
    assert!(errors.is_empty(), "{errors:?}");
    let (hir, errors) = ryupy_hir::lower_module(
        &parsed,
        &ryupy_hir::ModuleInfo {
            name: "__main__".into(),
            file: "returns.py".into(),
            ..Default::default()
        },
        SOURCE,
    );
    assert!(errors.is_empty(), "{errors:?}");
    let (types, errors) = ryupy_ty::check_module(&hir);
    assert!(errors.is_empty(), "{errors:?}");
    let (mut program, _, errors) = ryupy_mir::build_program(&hir, &types);
    assert!(errors.is_empty(), "{errors:?}");
    ryupy_mir::passes::run_pipeline(&mut program, &ryupy_mir::passes::default_pipeline()).unwrap();
    for (name, ret, static_abi) in [
        ("bare", MirType::Dynamic, false),
        ("explicit", MirType::Dynamic, false),
        ("implicit", MirType::Dynamic, false),
        ("annotated", MirType::Dynamic, false),
        ("cleanup", MirType::Dynamic, false),
        ("override", MirType::Dynamic, false),
        ("unit", MirType::Unit, true),
        ("scalar", MirType::I64, true),
    ] {
        let function = program
            .functions
            .iter()
            .find(|(_, f)| f.name == name)
            .unwrap()
            .1;
        assert_eq!(function.ret, ret, "{name}");
        assert_eq!(function.dynamic_origin.is_none(), static_abi, "{name}");
        let mut none_calls = 0;
        for (_, block) in function.body.blocks.iter() {
            for statement in &block.stmts {
                if let Statement::Assign { dest, value } = statement {
                    if matches!(
                        value,
                        Rvalue::Call {
                            callee: Callee::Runtime(RuntimeFn::DynNone),
                            ..
                        }
                    ) {
                        none_calls += 1;
                    }
                    assert!(
                        !matches!(value, Rvalue::Use(Operand::Const(Constant::I64(_))))
                            || function.body.locals[*dest].ty != MirType::Dynamic,
                        "{name}: 动态槽不能直接接受未装箱i64"
                    );
                }
            }
        }
        if !static_abi {
            assert!(none_calls > 0, "{name}: 缺少None单例");
        }
        if name == "scalar" {
            assert_eq!(function.params.len(), 1);
            assert_eq!(function.body.locals[function.params[0]].ty, MirType::I64);
        }
    }
}
