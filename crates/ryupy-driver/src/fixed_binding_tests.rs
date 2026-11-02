//! 私有内存声明验证：不新增CLI/JSON，不用伪SourceModule替代真实生成模块。
use super::*;
use ryupy_codegen_cil::fixed_bindings::{self, FixedI32Binding};
use ryupy_mir::{
    BasicBlock, Body, ExportGuard, Function, GlobalDecl, MirType, ModuleDef, ModuleExport,
    Terminator,
};

struct Root(PathBuf);
impl Drop for Root {
    fn drop(&mut self) {
        if !std::thread::panicking() {
            let _ = std::fs::remove_dir_all(&self.0);
        }
    }
}
fn root() -> Root {
    static SEQUENCE: std::sync::atomic::AtomicU64 = std::sync::atomic::AtomicU64::new(0);
    let serial = SEQUENCE.fetch_add(1, std::sync::atomic::Ordering::Relaxed);
    let time = std::time::SystemTime::now()
        .duration_since(std::time::UNIX_EPOCH)
        .unwrap()
        .as_nanos();
    let path = std::env::temp_dir().join(format!(
        "ryupy-fixed-binding-{}-{time}-{serial}",
        std::process::id()
    ));
    std::fs::create_dir(&path).unwrap();
    Root(path)
}
fn declare(program: &mut ryupy_mir::LinkedProgram, name: &str) -> FixedI32Binding {
    assert!(
        !program
            .modules
            .iter()
            .any(|(_, m)| m.name == name || m.name.starts_with(&format!("{name}.")))
    );
    let export = program.globals.alloc(GlobalDecl {
        name: "declared_add".into(),
        ty: MirType::Dynamic,
    });
    // 先创建空Body再分配其真实入口；不新增arena依赖。
    let mut body = Body {
        locals: Default::default(),
        blocks: Default::default(),
        entry: program.functions[program.modules[program.entry].init]
            .body
            .entry,
        regions: vec![],
    };
    let blocks = &mut body.blocks;
    let entry = blocks.alloc(BasicBlock {
        stmts: vec![],
        terminator: Terminator::Return(None),
    });
    let initializer = program.functions.alloc(Function {
        name: name.into(),
        codegen_name: "__declared_binding_init".into(),
        source_file: String::new(),
        params: vec![],
        ret: MirType::Unit,
        body: {
            body.entry = entry;
            body
        },
        param_meta: vec![],
        function_global: None,
        specialization_identity_global: None,
        defaults_global: None,
        signature_global: None,
        env: None,
    });
    let module = program.modules.alloc(ModuleDef {
        location: Default::default(),
        file_backed_by_global: true,
        protected_exports: vec![],
        name: name.into(),
        package: String::new(),
        file: String::new(),
        init: initializer,
        exports: vec![ModuleExport {
            name: "add".into(),
            value: export,
            guard: ExportGuard::ValueSentinel,
        }],
    });
    FixedI32Binding {
        module,
        initializer,
        export,
        module_ref: "ryupy_deliberately_absent_binding_A".into(),
        entry_point: "ryupy_demo_add_i32".into(),
    }
}
fn execute(source: &str, module: &str) -> std::process::Output {
    let root = root();
    let input = root.0.join("boundary.py");
    std::fs::write(&input, source).unwrap();
    let mut session = Session::default();
    let (mut program, _, _) = compile_linked(&mut session, &input, &[], false, None).unwrap();
    let binding = declare(&mut program, module);
    let bytes = fixed_bindings::emit(
        &program,
        &ryupy_codegen_cil::EmitOptions {
            assembly_name: "FixedBoundary".into(),
            entry_source_path: Some(input.to_str().unwrap().into()),
        },
        &[binding],
    )
    .unwrap();
    let dll = root.0.join("FixedBoundary.dll");
    std::fs::write(&dll, bytes).unwrap();
    std::fs::write(
        root.0.join("FixedBoundary.runtimeconfig.json"),
        ryupy_codegen_cil::runtimeconfig::render(),
    )
    .unwrap();
    runtime::copy_runtime_to(&root.0).unwrap();
    dotnet::run_dll(&dll).unwrap()
}
fn expected(text: &str) -> Vec<u8> {
    if cfg!(windows) {
        text.replace('\n', "\r\n").into_bytes()
    } else {
        text.as_bytes().to_vec()
    }
}
#[test]
fn fixed_binding_real_module_binder_and_dynamic_identity() {
    let result = execute(include_str!("fixed_binding_tests.py"), "declared_demo");
    assert!(
        result.status.success(),
        "{}",
        String::from_utf8_lossy(&result.stderr)
    );
    assert!(result.stderr.is_empty());
    assert_eq!(
        result.stdout,
        expected(concat!(
            "file-absent\nkind\nkind\nkind\nkind\nkind\nrange\nrange\nrange\nrange\n",
            "arity0\narity1\narity3\nposonly\nevaluate-before-check\nleft-before-right\n",
            "missing-library\ncallee-frozen\nsaved-identity\nnoncallable\nbinding-boundary-ok\n"
        ))
    );
}
#[test]
fn fixed_binding_native_name_conflict_precedes_user_code() {
    let result = execute("print('FORBIDDEN BODY')\n", "math");
    assert_eq!(result.status.code(), Some(1));
    assert!(result.stdout.is_empty());
    let stderr = String::from_utf8_lossy(&result.stderr);
    assert!(
        stderr.contains(
            "ImportError: declared native module 'math' conflicts with an existing module"
        ),
        "{stderr}"
    );
    assert!(!stderr.contains("Unhandled exception"));
}
#[test]
fn fixed_binding_binder_and_conversion_errors_keep_caller_frame() {
    for (call, kind) in [
        ("declared_demo.add()", "TypeError"),
        ("declared_demo.add(True, 0)", "TypeError"),
        ("declared_demo.add(2147483648, 0)", "OverflowError"),
        ("declared_demo.add(0, 0)", "ImportError"),
    ] {
        let source = format!("import declared_demo\ndef caller():\n    {call}\ncaller()\n");
        let result = execute(&source, "declared_demo");
        assert_eq!(result.status.code(), Some(1));
        assert!(result.stdout.is_empty());
        let stderr = String::from_utf8_lossy(&result.stderr);
        assert!(stderr.contains("line 4, in <module>"), "{stderr}");
        assert!(stderr.contains("line 3, in caller"), "{stderr}");
        assert!(!stderr.contains("Unhandled exception"), "{stderr}");
        assert!(!stderr.contains("BindingBody"), "{stderr}");
        assert!(stderr.contains(&format!("\n{kind}:")), "{stderr}");
    }
}
