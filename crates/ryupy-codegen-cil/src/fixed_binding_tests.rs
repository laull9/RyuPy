//! 固定绑定A的真实PE与拒绝路径：不以源码命名猜import，也不把PE检查冒作native执行。
use dotnetdll::prelude::*;
use la_arena::Arena;
use ryupy_mir::{ExportGuard, GlobalDecl, LinkedProgram, MirType, ModuleDef, ModuleExport};

use crate::fixed_bindings::{FixedI32Binding, emit, validate};

fn options() -> crate::EmitOptions {
    crate::EmitOptions {
        assembly_name: "fixed_binding".into(),
        entry_source_path: None,
    }
}

pub(crate) fn fixture() -> (LinkedProgram, FixedI32Binding) {
    let mut program = crate::exact_i64_tests::program();
    let initializer = program.functions.alloc(crate::exact_i64_tests::function(
        "__declared_init",
        crate::exact_i64_tests::single_block(Arena::default(), vec![], None),
        vec![],
        MirType::Unit,
    ));
    let export = program.globals.alloc(GlobalDecl {
        storage: ryupy_mir::GlobalStorage::LegacyStatic,
        name: "declared_add".into(),
        ty: MirType::Dynamic,
    });
    let module = program.modules.alloc(ModuleDef {
        instance_capabilities: Default::default(),
        execution: ryupy_mir::ModuleExecutionKind::LegacySingleton,
        environment_reason: "existing-contract".into(),
        location: Default::default(),
        file_backed_by_global: true,
        is_package: false,
        protected_exports: vec![],
        name: "native_math".into(),
        package: String::new(),
        file: String::new(),
        init: initializer,
        exports: vec![ModuleExport {
            name: "add".into(),
            value: export,
            guard: ExportGuard::ValueSentinel,
        }],
    });
    (
        program,
        FixedI32Binding {
            module,
            initializer,
            export,
            module_ref: "ryupy_cabi_probe".into(),
            entry_point: "ryupy_demo_add_i32".into(),
        },
    )
}

#[test]
fn empty_records_preserve_ordinary_pe_bytes_except_timestamp() {
    let program = crate::exact_i64_tests::program();
    // vendor写入当前COFF时间戳；仅清这个非语义字段，所有元数据/IL字节仍精确比较。
    let normalize = |mut bytes: Vec<u8>| {
        let pe = u32::from_le_bytes(bytes[0x3c..0x40].try_into().unwrap()) as usize;
        assert_eq!(&bytes[pe..pe + 4], b"PE\0\0");
        bytes[pe + 8..pe + 12].fill(0);
        bytes
    };
    assert_eq!(
        normalize(crate::emit_assembly(&program, &options()).unwrap()),
        normalize(emit(&program, &options(), &[]).unwrap())
    );
}

#[test]
fn invalid_sidecars_fail_before_emission() {
    for case in 0..12 {
        let (mut program, mut binding) = fixture();
        match case {
            0 => binding.initializer = ryupy_mir::FnId::from_raw(la_arena::RawIdx::from(u32::MAX)),
            1 => program.globals[binding.export].ty = MirType::I64,
            2 => program.modules[binding.module].exports[0].guard = ExportGuard::None,
            3 => program.modules[binding.module].file = "invented.py".into(),
            4 => binding.module_ref = "../unsafe".into(),
            5 => program.modules[binding.module].name = "__main__".into(),
            6 => program.functions[binding.initializer].function_global = Some(binding.export),
            7 => program.modules[program.entry].name = "native_math.child".into(),
            8 => {
                let function = &mut program.functions[binding.initializer];
                function.body.blocks[function.body.entry].terminator =
                    ryupy_mir::Terminator::Unreachable;
            }
            9 => program.modules[binding.module].exports[0].name = "__name__".into(),
            10 => program.modules[binding.module].exports[0].name = "__package__".into(),
            11 => program.modules[binding.module].exports[0].name = "__file__".into(),
            _ => unreachable!(),
        }
        assert!(
            emit(&program, &options(), &[binding]).is_err(),
            "case {case}"
        );
    }
    let (program, binding) = fixture();
    assert!(validate(&program, &[binding.clone(), binding]).is_err());
}

#[test]
fn real_pe_has_exact_native_signature_and_three_loader_clauses() {
    let (program, binding) = fixture();
    let bytes = emit(&program, &options(), &[binding]).unwrap();
    let res = Resolution::parse(&bytes, ReadOptions::default()).unwrap();
    let methods: Vec<_> = res
        .type_definitions
        .iter()
        .flat_map(|ty| &ty.methods)
        .collect();
    let native = methods
        .iter()
        .find(|method| method.name == "__fixed_i32_native")
        .unwrap();
    assert_eq!(native.signature, msig! { static long (int, int) });
    assert!(native.body.is_none() && native.preserve_sig);
    let pinvoke = native.pinvoke.as_ref().unwrap();
    assert!(pinvoke.no_mangle && !pinvoke.supports_last_error);
    assert!(matches!(
        pinvoke.calling_convention,
        UnmanagedCallingConvention::Cdecl
    ));
    let body = methods
        .iter()
        .find(|method| method.name == "__fixed_i32_body")
        .unwrap()
        .body
        .as_ref()
        .unwrap();
    let checks: Vec<_> = body
        .instructions
        .iter()
        .enumerate()
        .filter(|(_, ins)| ins.show(&res).contains("CheckedI32"))
        .map(|(i, _)| i)
        .collect();
    assert_eq!(checks.len(), 2);
    let clauses = match &body.data_sections[0] {
        body::DataSection::ExceptionHandlers(clauses) => clauses,
        _ => panic!("missing EH"),
    };
    assert_eq!(clauses.len(), 3);
    for (clause, expected) in clauses.iter().zip([
        "DllNotFoundException",
        "EntryPointNotFoundException",
        "BadImageFormatException",
    ]) {
        assert!(checks[1] < clause.try_offset);
        match &clause.kind {
            body::ExceptionKind::TypedException(ty) => assert!(ty.show(&res).contains(expected)),
            _ => panic!("not typed catch"),
        }
        let handler = &body.instructions
            [clause.handler_offset..clause.handler_offset + clause.handler_length];
        assert!(handler[0].show(&res).contains("ryupy_cabi_probe"));
        assert!(handler[1].show(&res).contains("ryupy_demo_add_i32"));
        assert!(handler[2].show(&res).contains("LoadFailure"));
        assert!(matches!(handler[3], Instruction::Throw));
    }
    assert_eq!(
        body.instructions
            .iter()
            .filter(|ins| ins.show(&res).contains("__fixed_i32_native("))
            .count(),
        1
    );
    assert_eq!(
        body.instructions
            .iter()
            .filter(|ins| ins.show(&res).contains("PyOps.BoxInt(long)"))
            .count(),
        1
    );
    assert!(
        !body
            .instructions
            .iter()
            .any(|ins| matches!(ins, Instruction::BoxValue(_)))
    );
}

#[test]
fn registration_is_cold_and_publication_follows_factory_success() {
    let (program, binding) = fixture();
    let bytes = emit(&program, &options(), &[binding]).unwrap();
    let res = Resolution::parse(&bytes, ReadOptions::default()).unwrap();
    let methods: Vec<_> = res
        .type_definitions
        .iter()
        .flat_map(|ty| &ty.methods)
        .collect();
    let startup = methods
        .iter()
        .find(|method| method.name == "__init_functions")
        .unwrap()
        .body
        .as_ref()
        .unwrap();
    assert!(matches!(
        startup.instructions[0],
        Instruction::LoadString(_)
    ));
    assert!(
        startup.instructions[1]
            .show(&res)
            .contains("EnsureDeclaredBindingAvailable")
    );
    assert!(
        startup.instructions[2]
            .show(&res)
            .contains("__init_literals")
    );
    assert_eq!(
        startup
            .instructions
            .iter()
            .filter(|ins| ins.show(&res).contains("RegisterDeclaredBinding"))
            .count(),
        1
    );
    let init = methods
        .iter()
        .find(|method| method.name == "__declared_init")
        .unwrap()
        .body
        .as_ref()
        .unwrap();
    assert_eq!(init.instructions.len(), 7);
    assert!(matches!(
        init.instructions[2],
        Instruction::LoadMethodPointer(_)
    ));
    assert!(init.instructions[4].show(&res).contains("CreateI32Binary"));
    assert!(matches!(
        init.instructions[5],
        Instruction::StoreStaticField { .. }
    ));
    assert!(matches!(init.instructions[6], Instruction::Return));
    let setter = methods
        .iter()
        .find(|method| method.name == "__mod_set_1")
        .unwrap()
        .body
        .as_ref()
        .unwrap();
    assert!(
        !setter
            .instructions
            .iter()
            .any(|ins| ins.show(&res).contains("PyCast"))
    );
    let getter = methods
        .iter()
        .find(|method| method.name == "__mod_get_1")
        .unwrap()
        .body
        .as_ref()
        .unwrap();
    assert!(
        !getter
            .instructions
            .iter()
            .any(|ins| ins.show(&res).contains("__file__"))
    );
}

/// 加载异常的显示名不能证明CLR可加载；逐个读回TypeRef所属正式facade与强身份。
#[test]
fn loader_exception_scopes_match_actual_net10_reference_assemblies() {
    let (program, binding) = fixture();
    let bytes = emit(&program, &options(), &[binding]).unwrap();
    let res = Resolution::parse(&bytes, ReadOptions::default()).unwrap();
    for (name, assembly) in [
        ("DllNotFoundException", "System.Runtime.InteropServices"),
        ("EntryPointNotFoundException", "System.Runtime"),
        ("BadImageFormatException", "System.Runtime"),
    ] {
        let types: Vec<_> = res
            .type_references
            .iter()
            .filter(|ty| ty.name == name && ty.namespace.as_deref() == Some("System"))
            .collect();
        assert_eq!(types.len(), 1, "{name}");
        let ResolutionScope::Assembly(scope) = types[0].scope else {
            panic!("non-assembly scope for {name}")
        };
        let reference = &res[scope];
        assert_eq!(reference.name, assembly);
        assert_eq!(
            (
                reference.version.major,
                reference.version.minor,
                reference.version.build,
                reference.version.revision
            ),
            (crate::runtimeconfig::TARGET_FRAMEWORK_MAJOR as u16, 0, 0, 0)
        );
        assert_eq!(
            reference.public_key_or_token.as_deref(),
            Some(&[0xb0, 0x3f, 0x5f, 0x7f, 0x11, 0xd5, 0x0a, 0x3a][..])
        );
        assert!(!reference.has_full_public_key);
        assert!(
            reference
                .culture
                .as_ref()
                .is_none_or(|value| value.is_empty())
        );
    }
}
