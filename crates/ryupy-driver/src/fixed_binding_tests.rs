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
        let _ = std::fs::remove_dir_all(&self.0);
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
    Root(path.canonicalize().unwrap())
}
fn declare(program: &mut ryupy_mir::LinkedProgram, name: &str) -> FixedI32Binding {
    assert!(
        !program
            .modules
            .iter()
            .any(|(_, m)| m.name == name || m.name.starts_with(&format!("{name}.")))
    );
    let export = program.globals.alloc(GlobalDecl {
        storage: ryupy_mir::GlobalStorage::LegacyStatic,
        name: "declared_add".into(),
        ty: MirType::Dynamic,
    });
    // 先创建空Body再分配其真实入口；不新增arena依赖。
    let mut body = Body {
        direct_globals_contracts: Vec::new(),
        source_handler_contracts: Vec::new(),
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
        module_state: None,
        instance_capabilities: Default::default(),
        module_state_source: None,
        name: name.into(),
        codegen_name: "__declared_binding_init".into(),
        source_file: String::new(),
        definition: None,
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
        dynamic_origin: None,
    });
    let module = program.modules.alloc(ModuleDef {
        instance_capabilities: Default::default(),
        execution: ryupy_mir::ModuleExecutionKind::LegacySingleton,
        environment_reason: "existing-contract".into(),
        location: Default::default(),
        file_backed_by_global: true,
        is_package: false,
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
        signature: None,
        module,
        initializer,
        export,
        module_ref: "ryupy_deliberately_absent_binding_A".into(),
        entry_point: "ryupy_demo_add_i32".into(),
    }
}
fn execute(source: &str, module: &str) -> std::process::Output {
    execute_configured(source, module, None).0
}
fn execute_configured(
    source: &str,
    module: &str,
    relative_file: Option<&str>,
) -> (std::process::Output, PathBuf) {
    let root = root();
    let input = root.0.join("boundary.py");
    std::fs::write(&input, source).unwrap();
    let mut session = Session::default();
    let (mut program, _, _) = compile_linked(&mut session, &input, &[], false, None).unwrap();
    let binding = declare(&mut program, module);
    let options = ryupy_codegen_cil::EmitOptions {
        assembly_name: "FixedBoundary".into(),
        entry_source_path: Some(input.to_str().unwrap().into()),
    };
    let bytes = if let Some(relative_file) = relative_file {
        // B故意不部署库，冷Register不能提前探库而阻断正常模块导入。
        assert!(!root.0.join(relative_file).exists());
        fixed_bindings::emit_with_report(
            &program,
            &options,
            &[binding],
            &fixed_bindings::BindingDeployment {
                relative_file: relative_file.into(),
            },
        )
        .unwrap()
        .bytes
    } else {
        fixed_bindings::emit(&program, &options, &[binding]).unwrap()
    };
    let dll = root.0.join("FixedBoundary.dll");
    std::fs::write(&dll, bytes).unwrap();
    std::fs::write(
        root.0.join("FixedBoundary.runtimeconfig.json"),
        ryupy_codegen_cil::runtimeconfig::render(),
    )
    .unwrap();
    runtime::copy_runtime_to(&root.0).unwrap();
    (dotnet::run_dll(&dll).unwrap(), root.0.clone())
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

// 仅关闭真实CLR facade/resolver冷入口装载缺口，不称C调用成功或AOT发布完成。
#[test]
fn fixed_binding_b_resolver_loads_before_body_and_missing_library_keeps_caller() {
    let relative_file = if cfg!(target_os = "windows") {
        "_internal/native/v1/b0/library.dll"
    } else if cfg!(target_os = "macos") {
        "_internal/native/v1/b0/library.dylib"
    } else {
        "_internal/native/v1/b0/library.so"
    };
    let source = concat!(
        "import declared_demo\n",
        "from declared_demo import add as saved\n",
        "assert saved is declared_demo.add\n",
        "print('registered-module-identity')\n",
        "def caller():\n",
        "    saved(0, 0)\n",
        "caller()\n",
    );
    let (result, deployed_root) = execute_configured(source, "declared_demo", Some(relative_file));
    assert_eq!(result.status.code(), Some(1));
    assert_eq!(result.stdout, expected("registered-module-identity\n"));
    let stderr = String::from_utf8_lossy(&result.stderr);
    assert!(stderr.contains("line 7, in <module>"), "{stderr}");
    assert!(stderr.contains("line 6, in caller"), "{stderr}");
    assert!(stderr.contains("ImportError: cannot load declared native library 'ryupy_deliberately_absent_binding_A' symbol 'ryupy_demo_add_i32'"), "{stderr}");
    // 显式部署路径和底层文件系统原因属于加载诊断合同；保留编译调用者的两个帧。
    let library_path =
        deployed_root.join(relative_file.replace('/', std::path::MAIN_SEPARATOR_STR));
    let diagnostic = format!(
        "native binding 'ryupy_deliberately_absent_binding_A' at '{}' is unavailable: ",
        library_path.display()
    );
    let reason = stderr
        .split_once(&diagnostic)
        .unwrap_or_else(|| panic!("{stderr}"))
        .1;
    assert!(
        reason.contains(&*library_path.to_string_lossy()),
        "{stderr}"
    );
    assert!(!reason.trim().is_empty(), "{stderr}");
    for forbidden in [
        "Unhandled exception",
        "TypeLoadException",
        "Could not load type",
        "MissingMethodException",
        "BindingBody",
    ] {
        assert!(!stderr.contains(forbidden), "{stderr}");
    }
}
