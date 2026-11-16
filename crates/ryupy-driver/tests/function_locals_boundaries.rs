//! locals已登记支持边界，不计入CPython一致性用例；运行时必须明确NI而非借用调用者缓存。
//! 独立保存源码、构建诊断和JIT原始流，正例仅用于锁定真实用户遮蔽。
#[path = "../src/test_temp.rs"]
mod test_temp;
use ryupy_mir::{Callee, Constant, Operand, RuntimeFn, Rvalue, Statement};

const BOUNDARIES: &str = r#"
try:
    locals()
except NotImplementedError:
    print('boundary.module')
try:
    class Example:
        value = locals()
except NotImplementedError:
    print('boundary.class')
saved = locals
def invoke_alias():
    mapping = locals()
    mapping['sentinel'] = 7
    try:
        saved()
    except NotImplementedError:
        print('boundary.alias', mapping['sentinel'])
invoke_alias()
def generator():
    locals()
    yield 1
try:
    next(generator())
except NotImplementedError:
    print('boundary.generator')
def free_outer():
    value = 3
    def inner():
        print('boundary.free-value', value)
        return locals()
    return inner
try:
    free_outer()()
except NotImplementedError:
    print('boundary.free')
def cell_outer():
    value = 3
    def inner():
        return value
    return locals()
try:
    cell_outer()
except NotImplementedError:
    print('boundary.cell')
try:
    [locals() for item in [1]]
except NotImplementedError:
    print('boundary.comprehension')
def containing_comp():
    values = [item for item in [1]]
    return locals()
try:
    containing_comp()
except NotImplementedError:
    print('boundary.containing-comp')
"#;

// 模块赋值同名locals影响整个模块解析，须与真实内建边界分开编译。
const SHADOWS: &str = r#"
def by_param(locals):
    return locals(2)
def by_local():
    locals = lambda value: value + 3
    return locals(4)
print('shadow.param', by_param(lambda value: value + 1))
print('shadow.local', by_local())
locals = lambda value: value + 5
print('shadow.module', locals(6))
"#;

fn run(label: &str, source: &str, expected: &str) {
    let root = test_temp::Root::new(&format!("function-locals-boundaries-{label}"));
    eprintln!("locals边界证据: {}", root.display());
    let input = root.join("boundary.py");
    std::fs::write(&input, source).unwrap();
    let mut session = ryupy_driver::Session::default();
    let result = ryupy_driver::build(
        &mut session,
        &ryupy_driver::BuildOptions {
            input,
            out_dir: root.join("out"),
            aot: false,
            search_paths: vec![],
        },
    );
    std::fs::write(
        root.join("build.diagnostics"),
        format!("{:?}\n", session.diagnostics),
    )
    .unwrap();
    let artifact = result.unwrap();
    let result = ryupy_driver::dotnet::run_dll_with(
        &artifact.dll,
        &ryupy_driver::dotnet::RunOptions {
            cwd: Some(root.clone()),
            ..Default::default()
        },
    )
    .unwrap();
    std::fs::write(root.join("jit.stdout"), &result.stdout).unwrap();
    std::fs::write(root.join("jit.stderr"), &result.stderr).unwrap();
    std::fs::write(
        root.join("jit.exit"),
        format!("{:?}\n", result.status.code()),
    )
    .unwrap();
    assert_eq!(
        result.status.code(),
        Some(0),
        "{}",
        String::from_utf8_lossy(&result.stderr)
    );
    assert_eq!(result.stdout, expected.as_bytes());
    assert!(result.stderr.is_empty());
}

#[test]
fn unsupported_scopes_raise_explicitly_without_borrowing_caller_cache() {
    run(
        "unsupported",
        BOUNDARIES,
        "boundary.module\nboundary.class\nboundary.alias 7\nboundary.generator\nboundary.free-value 3\nboundary.free\nboundary.cell\nboundary.comprehension\nboundary.containing-comp\n",
    );
}

#[test]
fn user_module_local_and_parameter_shadowing_remains_callable() {
    run(
        "shadowing",
        SHADOWS,
        "shadow.param 3\nshadow.local 7\nshadow.module 11\n",
    );
}

#[test]
fn source_local_metadata_includes_later_load_but_excludes_with_and_try_temporaries() {
    let source = "def probe(manager):\n    declared: int\n    early = 1\n    with manager as handle:\n        try:\n            later = declared\n        except Exception as caught:\n            other = 2\n    return locals()\n";
    let mut db = ryupy_base::SourceDb::default();
    let file = db.add("metadata.py", source);
    let (parsed, diagnostics) = ryupy_syntax::parse(file, source);
    assert!(diagnostics.is_empty(), "{diagnostics:?}");
    let (hir, diagnostics) = ryupy_hir::lower_module(&parsed, &Default::default(), source);
    assert!(diagnostics.is_empty(), "{diagnostics:?}");
    let (types, diagnostics) = ryupy_ty::check_module(&hir);
    assert!(diagnostics.is_empty(), "{diagnostics:?}");
    let (program, _, diagnostics) = ryupy_mir::build_program(&hir, &types);
    assert!(diagnostics.is_empty(), "{diagnostics:?}");
    let f = program
        .functions
        .iter()
        .find(|(_, f)| f.name == "probe")
        .unwrap()
        .1;
    let names: Vec<_> = f
        .body
        .blocks
        .iter()
        .flat_map(|(_, b)| &b.stmts)
        .filter_map(|stmt| match stmt {
            Statement::Eval(Rvalue::Call {
                callee: Callee::Runtime(RuntimeFn::FunctionLocalsSync),
                args,
            }) => {
                assert!(matches!(args[2], Operand::Local(_)), "必须读取原始槽");
                match &args[1] {
                    Operand::Const(Constant::Str(name)) => Some(name.as_str()),
                    _ => panic!("同步名称必须为编译常量"),
                }
            }
            _ => None,
        })
        .collect();
    assert_eq!(
        names,
        [
            "manager", "early", "handle", "declared", "later", "caught", "other"
        ]
    );
}

#[test]
fn unsupported_frames_and_saved_alias_have_no_locals_cache_calls() {
    let mut db = ryupy_base::SourceDb::default();
    let file = db.add("boundaries.py", BOUNDARIES);
    let (parsed, diagnostics) = ryupy_syntax::parse(file, BOUNDARIES);
    assert!(diagnostics.is_empty(), "{diagnostics:?}");
    let (hir, diagnostics) = ryupy_hir::lower_module(&parsed, &Default::default(), BOUNDARIES);
    assert!(diagnostics.is_empty(), "{diagnostics:?}");
    let (types, diagnostics) = ryupy_ty::check_module(&hir);
    assert!(diagnostics.is_empty(), "{diagnostics:?}");
    let (program, _, diagnostics) = ryupy_mir::build_program(&hir, &types);
    assert!(diagnostics.is_empty(), "{diagnostics:?}");
    for (_, function) in program.functions.iter() {
        let mut ensures = 0;
        for (_, block) in function.body.blocks.iter() {
            for statement in &block.stmts {
                if matches!(
                    statement,
                    Statement::Assign {
                        value: Rvalue::Call {
                            callee: Callee::Runtime(RuntimeFn::FunctionLocalsEnsure),
                            ..
                        },
                        ..
                    }
                ) {
                    ensures += 1;
                }
            }
        }
        // invoke_alias唯一的真正直接调用可建缓存；saved()不得添加第二次Ensure。
        assert_eq!(
            ensures,
            usize::from(function.name == "invoke_alias"),
            "{}",
            function.name
        );
    }
}

#[test]
fn unsupported_expansion_interleavings_are_diagnostics_not_approximate_calls() {
    for (call, rejected) in [
        ("locals(1, *items)", true),
        ("locals(*items, 1)", true),
        ("locals(*items, *items)", true),
        ("locals(**mapping, key=1)", true),
        ("locals(**mapping, **mapping)", true),
        ("locals()", false),
        ("locals(1, 2)", false),
        ("locals(*items)", false),
        ("locals(key=1, **mapping)", false),
        ("locals(*items, key=1, **mapping)", false),
    ] {
        let source = format!("def f(items, mapping):\n    return {call}\n");
        let mut db = ryupy_base::SourceDb::default();
        let file = db.add("expansion.py", &source);
        let (parsed, diagnostics) = ryupy_syntax::parse(file, &source);
        assert!(diagnostics.is_empty(), "{call}: {diagnostics:?}");
        let (hir, diagnostics) = ryupy_hir::lower_module(&parsed, &Default::default(), &source);
        assert!(diagnostics.is_empty(), "{call}: {diagnostics:?}");
        let (types, diagnostics) = ryupy_ty::check_module(&hir);
        assert!(diagnostics.is_empty(), "{call}: {diagnostics:?}");
        let (program, _, diagnostics) = ryupy_mir::build_program(&hir, &types);
        if !rejected {
            assert!(diagnostics.is_empty(), "{call}: {diagnostics:?}");
            continue;
        }
        assert!(
            diagnostics.iter().any(|d| d.code == Some("E0305")),
            "{call}: {diagnostics:?}"
        );
        let f = program
            .functions
            .iter()
            .find(|(_, f)| f.name == "f")
            .unwrap()
            .1;
        let dump = format!("{:?}", f.body);
        assert!(
            !dump.contains("FunctionLocals"),
            "拒绝形状不得执行局部同步: {call}"
        );
        assert!(
            !dump.contains("DynBuiltin"),
            "拒绝形状不得回落无帧builtin: {call}"
        );
    }
}

#[test]
fn dynamic_local_delete_preserves_unbound_checks_cleanup_rebinding_and_partial_effects() {
    let source = r#"
def probe(parameter):
    mapping = locals()
    del parameter
    print('delete.parameter', 'parameter' in locals())
    parameter = 8
    print('delete.rebind', locals()['parameter'])
    try:
        del missing
    except UnboundLocalError:
        print('delete.unbound')
    first = 1
    try:
        del first, missing
    except UnboundLocalError:
        print('delete.partial', 'first' in locals(), 'missing' in locals())
    try:
        print(first)
    except UnboundLocalError:
        print('delete.read')
    first = 9
    print('delete.reassigned', locals()['first'])
probe(3)
"#;
    run(
        "delete",
        source,
        "delete.parameter False\ndelete.rebind 8\ndelete.unbound\ndelete.partial False False\ndelete.read\ndelete.reassigned 9\n",
    );
}

#[test]
fn module_and_global_deletion_validate_through_hir_types_and_mir() {
    for source in [
        "value = 1\ndel value\n",
        "def f():\n    global value\n    del value\n",
    ] {
        let mut db = ryupy_base::SourceDb::default();
        let file = db.add("module-delete.py", source);
        let (parsed, diagnostics) = ryupy_syntax::parse(file, source);
        assert!(diagnostics.is_empty(), "{source}: {diagnostics:?}");
        let (hir, diagnostics) = ryupy_hir::lower_module(&parsed, &Default::default(), source);
        assert!(diagnostics.is_empty(), "{source}: {diagnostics:?}");
        assert!(
            hir.vars
                .iter()
                .any(|(_, var)| var.name.as_str() == "value" && var.may_delete)
        );
        let (types, diagnostics) = ryupy_ty::check_module(&hir);
        assert!(diagnostics.is_empty(), "{source}: {diagnostics:?}");
        let (mut program, _, diagnostics) = ryupy_mir::build_program(&hir, &types);
        assert!(diagnostics.is_empty(), "{source}: {diagnostics:?}");
        // 保留原先被拒绝的两份源码，必须实际生成删除检查，不能仅静默接受。
        assert_eq!(
            program
                .functions
                .iter()
                .flat_map(|(_, function)| function.body.blocks.iter())
                .flat_map(|(_, block)| &block.stmts)
                .filter(|statement| matches!(
                    statement,
                    Statement::Eval(Rvalue::Call {
                        callee: Callee::Runtime(
                            RuntimeFn::CheckModuleDelete | RuntimeFn::CheckModuleDeleteValue
                        ),
                        ..
                    })
                ))
                .count(),
            1,
            "{source}"
        );
        let result = ryupy_mir::validate::validate(&program);
        assert!(result.is_ok(), "{source}: {result:?}");
        let result =
            ryupy_mir::passes::run_pipeline(&mut program, &ryupy_mir::passes::default_pipeline());
        assert!(result.is_ok(), "{source}: {result:?}");
    }
}

#[test]
fn deletion_retains_explicit_scope_and_static_storage_boundaries() {
    for source in [
        "def f():\n    value = 1\n    def inner():\n        return value\n    del value\n",
        "def f(value: int) -> int:\n    del value\n    return 1\n",
        "def f():\n    x = 1\n    y = 2\n    del (x, y)\n",
    ] {
        let mut db = ryupy_base::SourceDb::default();
        let file = db.add("delete-boundary.py", source);
        let (parsed, diagnostics) = ryupy_syntax::parse(file, source);
        assert!(diagnostics.is_empty(), "{diagnostics:?}");
        let (hir, diagnostics) = ryupy_hir::lower_module(&parsed, &Default::default(), source);
        if diagnostics.iter().any(|d| d.code == Some("E0100")) {
            continue;
        }
        assert!(diagnostics.is_empty(), "{source}: {diagnostics:?}");
        let (types, diagnostics) = ryupy_ty::check_module(&hir);
        assert!(diagnostics.is_empty(), "{source}: {diagnostics:?}");
        let (_, _, diagnostics) = ryupy_mir::build_program(&hir, &types);
        assert!(
            diagnostics.iter().any(|d| d.code == Some("E0305")),
            "{source}: {diagnostics:?}"
        );
    }
}
