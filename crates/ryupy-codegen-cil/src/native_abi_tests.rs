//! 声明 ABI 的真实 PE 回读：机器签名、准备顺序、窄 catch 与最外层释放。
use dotnetdll::prelude::*;
use ryupy_mir::native_abi::{AbiParameter, AbiSignature, AbiType};

use crate::fixed_bindings::{emit, validate};

fn signature(kinds: &[&str], returns: AbiType) -> AbiSignature {
    AbiSignature {
        parameters: kinds
            .iter()
            .enumerate()
            .map(|(index, kind)| AbiParameter {
                name: format!("arg{index}"),
                ty: AbiType::parse(kind).unwrap(),
                type_name: (*kind).into(),
            })
            .collect(),
        returns,
    }
}

fn options() -> crate::EmitOptions {
    crate::EmitOptions {
        assembly_name: "native_abi".into(),
        entry_source_path: None,
    }
}

#[test]
fn scalar_types_roundtrip_as_exact_static_pinvoke_metadata() {
    for kind in [
        "i8", "u8", "i16", "u16", "i32", "u32", "i64", "u64", "isize", "usize", "f32", "f64",
        "bool8",
    ] {
        let (program, mut binding) = crate::fixed_binding_tests::fixture();
        let abi = signature(&[kind], AbiType::parse(kind).unwrap());
        binding.signature = Some(abi.clone());
        let bytes = emit(&program, &options(), &[binding]).unwrap();
        let resolution = Resolution::parse(&bytes, ReadOptions::default()).unwrap();
        let native = resolution
            .type_definitions
            .iter()
            .flat_map(|class| &class.methods)
            .find(|method| method.pinvoke.is_some())
            .unwrap();
        assert_eq!(
            native.signature,
            crate::native_abi_types::signature(&abi),
            "{kind}"
        );
        assert!(native.preserve_sig && native.body.is_none());
        assert!(
            native
                .parameter_metadata
                .iter()
                .flatten()
                .all(|parameter| parameter.marshal.is_none())
        );
        let import = native.pinvoke.as_ref().unwrap();
        assert!(import.no_mangle && !import.supports_last_error);
        assert!(matches!(
            import.calling_convention,
            UnmanagedCallingConvention::Cdecl
        ));
    }
}

#[test]
fn borrowed_buffers_expand_and_finally_covers_loader_and_return_materialization() {
    let (program, mut binding) = crate::fixed_binding_tests::fixture();
    binding.signature = Some(signature(&["utf8", "bytes", "u64"], AbiType::Bool8));
    let bytes = emit(&program, &options(), &[binding]).unwrap();
    let resolution = Resolution::parse(&bytes, ReadOptions::default()).unwrap();
    let methods: Vec<_> = resolution
        .type_definitions
        .iter()
        .flat_map(|class| &class.methods)
        .collect();
    let native = methods
        .iter()
        .find(|method| method.pinvoke.is_some())
        .unwrap();
    assert_eq!(
        native.signature,
        msig! { static byte (nint, nuint, nint, nuint, ulong) }
    );
    let body = methods
        .iter()
        .find(|method| method.name.starts_with("__abi_body_"))
        .unwrap()
        .body
        .as_ref()
        .unwrap();
    let position = |name: &str| {
        body.instructions
            .iter()
            .position(|instruction| instruction.show(&resolution).contains(name))
            .unwrap()
    };
    let prepare = position("Prepare");
    let materialize = position("BoxBoolean");
    let dispose = position("Dispose");
    let body::DataSection::ExceptionHandlers(clauses) = &body.data_sections[0] else {
        panic!("EH");
    };
    assert_eq!(clauses.len(), 4);
    let cleanup = clauses
        .iter()
        .find(|clause| matches!(clause.kind, body::ExceptionKind::Finally))
        .unwrap();
    assert!(prepare < cleanup.try_offset);
    assert!(materialize < cleanup.try_offset + cleanup.try_length);
    assert_eq!(dispose, cleanup.handler_offset + 1);
    for clause in &clauses[..3] {
        assert!(clause.try_offset >= cleanup.try_offset);
        assert!(clause.handler_offset + clause.handler_length <= materialize);
        assert!(
            clause.handler_offset + clause.handler_length < cleanup.try_offset + cleanup.try_length
        );
        assert!(matches!(
            clause.kind,
            body::ExceptionKind::TypedException(_)
        ));
    }
    assert!(
        body.instructions
            .iter()
            .any(|instruction| matches!(instruction, Instruction::EndFinally))
    );
}

#[test]
fn maximal_buffer_signature_is_bounded_and_void_uses_no_return_value() {
    for kinds in [vec!["bytes"; 16], vec![]] {
        let (program, mut binding) = crate::fixed_binding_tests::fixture();
        binding.signature = Some(signature(&kinds, AbiType::Void));
        let bytes = emit(&program, &options(), &[binding]).unwrap();
        let resolution = Resolution::parse(&bytes, ReadOptions::default()).unwrap();
        let native = resolution
            .type_definitions
            .iter()
            .flat_map(|class| &class.methods)
            .find(|method| method.pinvoke.is_some())
            .unwrap();
        assert_eq!(native.signature.parameters.len(), kinds.len() * 2);
        assert_eq!(native.signature.return_type, ReturnType::VOID);
    }
}

#[test]
fn invalid_internal_records_fail_before_emission() {
    for abi in [
        signature(&["void"], AbiType::I32),
        signature(&["i32"], AbiType::Bytes),
        signature(&["i32"; 17], AbiType::I64),
    ] {
        let (program, mut binding) = crate::fixed_binding_tests::fixture();
        binding.signature = Some(abi);
        assert!(emit(&program, &options(), &[binding]).is_err());
    }
    let (mut program, mut first) = crate::fixed_binding_tests::fixture();
    first.signature = Some(signature(&["i32"], AbiType::I32));
    let mut second = first.clone();
    second.export = program.globals.alloc(program.globals[first.export].clone());
    let mut export = program.modules[first.module].exports[0].clone();
    export.name = "alias".into();
    export.value = second.export;
    program.modules[first.module].exports.push(export);
    assert!(validate(&program, &[first.clone(), second.clone()]).is_ok());
    second.signature = Some(signature(&["u32"], AbiType::I32));
    assert!(validate(&program, &[first.clone(), second.clone()]).is_err());
    second.signature = None;
    assert!(validate(&program, &[first, second]).is_err());
}
