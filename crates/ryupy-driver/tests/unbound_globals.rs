//! 未绑定检查不应让所有静态全局与引用它们的函数退化成 Dynamic。
use ryupy_driver::{Session, Stage};
mod support;
use support::dump;

#[test]
fn static_global_load_checks_bound_and_freezes_typed_value() {
    let root = std::path::Path::new(env!("CARGO_MANIFEST_DIR"));
    let path = root.join("../../tests/e2e/phase3/m3_7_static_global_bound.py");
    for (stage, name) in [
        (Stage::Types, "static_global_bound_types"),
        (Stage::Mir, "static_global_bound_mir"),
    ] {
        let mut session = Session::default();
        let output = dump(&mut session, &path, stage).expect("静态全局绑定检查应可降级");
        match stage {
            Stage::Types => {
                assert!(
                    output.contains("module var Idx::<ModuleVar>(3): Int"),
                    "{name}"
                );
                assert!(output.contains("func Idx::<Function>(0) [Static]: [] -> Int"));
            }
            Stage::Mir => {
                let read = output
                    .split("fn read_number(")
                    .nth(1)
                    .unwrap()
                    .split("fn main(")
                    .next()
                    .unwrap();
                assert!(read.contains(") -> i64"), "{name}");
                assert!(
                    read.find("CheckGlobalBound(global@4, const \"number\")")
                        .unwrap()
                        < read.find(" = global@3;").unwrap()
                );
                let main = output.split("fn main(").nth(1).unwrap();
                assert!(
                    main.find("g4 = const false").unwrap()
                        < main.find("g3 = const 42_i64").unwrap()
                );
                assert!(
                    main.find("g3 = const 42_i64").unwrap() < main.find("g4 = const true").unwrap()
                );
            }
            _ => unreachable!(),
        }
    }
}

use ryupy_mir::*;
#[test]
fn bound_flags_reset_before_body_and_follow_value_store() {
    let mut database = ryupy_base::SourceDb::default();
    let source = "def read() -> int:\n    return value\nvalue = 42\n";
    let file = database.add("bound.py", source);
    let (parsed, diagnostics) = ryupy_syntax::parse(file, source);
    assert!(diagnostics.is_empty());
    let (hir, diagnostics) = ryupy_hir::lower_module(&parsed, &Default::default(), source);
    assert!(diagnostics.is_empty());
    let (types, diagnostics) = ryupy_ty::check_module(&hir);
    assert!(diagnostics.is_empty());
    let (program, exports, diagnostics) = ryupy_mir::build_program(&hir, &types);
    assert!(diagnostics.is_empty());
    let export = exports
        .iter()
        .find(|export| export.name == "value")
        .unwrap();
    assert_eq!(program.globals[export.value].ty, MirType::I64);
    let ExportGuard::BoundFlag(flag) = export.guard else {
        panic!("静态值必须独立绑定位")
    };
    let body = &program.functions[program.entry].body;
    let statements = &body.blocks[body.entry].stmts;
    assert!(statements.iter().any(|statement| matches!(statement,
            Statement::SetGlobal { dest, value: Operand::Const(Constant::Bool(false)) } if *dest == flag)));
    let store = statements
        .iter()
        .position(|statement| {
            matches!(statement,
            Statement::SetGlobal { dest, .. } if *dest == export.value)
        })
        .unwrap();
    assert!(matches!(statements[store + 1], Statement::SetGlobal {
            dest, value: Operand::Const(Constant::Bool(true)) } if dest == flag));
    let read = program
        .functions
        .iter()
        .find(|(_, function)| function.name == "read")
        .unwrap()
        .1;
    assert_eq!(read.ret, MirType::I64);
    assert!(
        read.body
            .blocks
            .iter()
            .any(|(_, block)| block.stmts.iter().any(|statement| matches!(
                statement,
                Statement::Eval(Rvalue::Call {
                    callee: Callee::Runtime(RuntimeFn::CheckGlobalBound),
                    ..
                })
            )))
    );
    assert!(
        read.body
            .blocks
            .iter()
            .all(|(_, block)| block.stmts.iter().all(|statement| !matches!(
                statement,
                Statement::Assign {
                    value: Rvalue::Box { .. },
                    ..
                }
            )))
    );
}

#[test]
fn unbound_declared_unsupported_builtin_is_not_a_fake_unknown_name() {
    let root = ryupy_driver::runtime::workspace_root().join("target/test_unbound_builtin_guard");
    std::fs::create_dir_all(&root).unwrap();
    let input = root.join("main.py");
    std::fs::write(&input, "def probe():\n    global locals\n    return locals()\ntry:\n    probe()\nexcept NotImplementedError:\n    print('global 明确拒绝')\nif False:\n    eval = None\ntry:\n    eval\nexcept NotImplementedError:\n    print('死分支明确拒绝')\nlocals = '用户值'\nprint(locals)\n").unwrap();
    let mut session = Session::default();
    let artifact = ryupy_driver::build(
        &mut session,
        &ryupy_driver::BuildOptions {
            input,
            out_dir: root.join("out"),
            aot: false,
            search_paths: vec![],
        },
    )
    .expect("已声明槽允许编译，缺值时才明确拒绝未实现内建");
    let result = ryupy_driver::dotnet::run_dll(&artifact.dll).unwrap();
    assert!(
        result.status.success(),
        "{}",
        String::from_utf8_lossy(&result.stderr)
    );
    assert!(result.stderr.is_empty());
    assert_eq!(
        String::from_utf8_lossy(&result.stdout),
        "global 明确拒绝\n死分支明确拒绝\n用户值\n"
    );
}
