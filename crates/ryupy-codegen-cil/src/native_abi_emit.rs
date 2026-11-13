//! v3 wrapper：受信帧转换参数，静态 PInvoke；最外层 finally 覆盖装载与返回物化。
use crate::{fixed_binding_refs::BindingRefs, fixed_bindings::FixedI32Binding, native_abi_types};
use dotnetdll::prelude::*;
use ryupy_mir::native_abi::{AbiSignature, AbiType};

struct Refs {
    frame: MethodType,
    prepare: MethodRefIndex,
    signed: MethodRefIndex,
    unsigned: MethodRefIndex,
    floating: MethodRefIndex,
    boolean: MethodRefIndex,
    pointer: MethodRefIndex,
    length: MethodRefIndex,
    dispose: MethodRefIndex,
    create: MethodRefIndex,
    box_signed: MethodRefIndex,
    box_unsigned: MethodRefIndex,
    box_floating: MethodRefIndex,
    box_boolean: MethodRefIndex,
}

impl Refs {
    fn build(res: &mut Resolution<'static>, runtime: AssemblyRefIndex) -> Self {
        let frame = res.push_type_reference(type_ref! { RyuPy.Runtime.PyNativeCall in #runtime });
        let frame: MethodType = BaseType::class(frame).into();
        let owner =
            res.push_type_reference(type_ref! { RyuPy.Runtime.PyNativeBindings in #runtime });
        let owner: MethodType = BaseType::class(owner).into();
        let function = res.push_type_reference(type_ref! { RyuPy.Runtime.PyFunction in #runtime });
        let function: MethodType = BaseType::class(function).into();
        let system = res.push_assembly_reference(crate::assembly_refs::system_runtime());
        let func = res.push_type_reference(type_ref! { System.Func<2> in #system });
        let func: MethodType = BaseType::class(TypeSource::generic(
            func,
            vec![ctype! { object[] }, ctype! { object }],
        ))
        .into();
        Self {
            prepare: res.push_method_reference(
                method_ref! { static @frame @frame::Prepare(object[], string, string, string) },
            ),
            signed: res.push_method_reference(method_ref! { long @frame::Signed(int) }),
            unsigned: res.push_method_reference(method_ref! { ulong @frame::Unsigned(int) }),
            floating: res.push_method_reference(method_ref! { double @frame::Floating(int) }),
            boolean: res.push_method_reference(method_ref! { byte @frame::Boolean(int) }),
            pointer: res.push_method_reference(method_ref! { nint @frame::Pointer(int) }),
            length: res.push_method_reference(method_ref! { nuint @frame::Length(int) }),
            dispose: res.push_method_reference(method_ref! { void @frame::Dispose() }),
            create: res.push_method_reference(
                method_ref! { static @function @owner::CreateAbi(string, string, @func) },
            ),
            box_signed: res
                .push_method_reference(method_ref! { static object @owner::BoxSigned(long) }),
            box_unsigned: res
                .push_method_reference(method_ref! { static object @owner::BoxUnsigned(ulong) }),
            box_floating: res
                .push_method_reference(method_ref! { static object @owner::BoxFloating(double) }),
            box_boolean: res
                .push_method_reference(method_ref! { static object @owner::BoxBoolean(byte) }),
            frame,
        }
    }
}

pub(crate) fn function(
    res: &mut Resolution<'static>,
    class: TypeIndex,
    binding: &FixedI32Binding,
    abi: &AbiSignature,
    name: &str,
    refs: &BindingRefs,
) -> (Vec<Instruction>, MethodIndex, MethodIndex) {
    let api = Refs::build(res, refs.runtime);
    let suffix = binding.export.into_raw().into_u32();
    let scope = res.push_module_reference(ExternalModuleReference::new(binding.module_ref.clone()));
    let mut import = PInvoke::new(scope, binding.entry_point.clone());
    import.no_mangle = true;
    import.calling_convention = UnmanagedCallingConvention::Cdecl;
    import.character_set = CharacterSet::NotSpecified;
    let mut method = Method::new(
        Accessibility::Private,
        native_abi_types::signature(abi),
        format!("__abi_native_{suffix}"),
        None,
    );
    method.pinvoke = Some(import);
    method.preserve_sig = true;
    let native = res.push_method(class, method);
    let names = abi
        .parameters
        .iter()
        .map(|parameter| parameter.name.as_str())
        .collect::<Vec<_>>()
        .join(",");
    let types = abi
        .parameters
        .iter()
        .map(|parameter| parameter.type_name.as_str())
        .collect::<Vec<_>>()
        .join(",");
    // Prepare 在保护区外完成全部检查与快照；其构造器自行回滚部分固定失败。
    // 成功返回帧后立即进入最外层 try，后续任何托管异常都经过 finally。
    let mut ins = vec![
        Instruction::LoadArgument(0),
        Instruction::load_string(types),
        Instruction::load_string(names.clone()),
        Instruction::load_string(name),
        Instruction::call(api.prepare),
        Instruction::StoreLocal(0),
    ];
    let outer_start = ins.len();
    // 逻辑缓冲参数逐个展开为相邻的地址和字节长度，其他参数保持声明次序。
    for (index, parameter) in abi.parameters.iter().enumerate() {
        let ty = parameter.ty;
        let readers: Vec<_> = if ty.is_buffer() {
            vec![api.pointer, api.length]
        } else {
            vec![match ty {
                AbiType::F32 | AbiType::F64 => api.floating,
                AbiType::Bool8 => api.boolean,
                AbiType::U8 | AbiType::U16 | AbiType::U32 | AbiType::U64 | AbiType::Usize => {
                    api.unsigned
                }
                _ => api.signed,
            }]
        };
        for reader in readers {
            ins.extend([
                Instruction::LoadLocal(0),
                Instruction::LoadConstantInt32(index as i32),
                Instruction::call(reader),
            ]);
            if !ty.is_buffer() {
                ins.push(native_abi_types::conversion(ty));
            }
        }
    }
    ins.push(Instruction::call(native));
    if abi.returns != AbiType::Void {
        ins.push(Instruction::StoreLocal(1));
    }
    let inner_leave = ins.len();
    ins.push(Instruction::Leave(0));
    let inner_len = ins.len() - outer_start;
    let mut clauses = Vec::new();
    // 仅装载相关 CLR 异常翻译为 ImportError；返回值物化位于这些 catch 之外，
    // 因而非规范 bool8 等返回协议错误保留自己的 Python 异常类型。
    for error in &refs.loader_errors {
        let start = ins.len();
        ins.extend([
            Instruction::load_string(binding.module_ref.clone()),
            Instruction::load_string(binding.entry_point.clone()),
            Instruction::call(refs.failure),
            Instruction::Throw,
        ]);
        clauses.push(body::Exception {
            kind: body::ExceptionKind::TypedException(error.clone()),
            try_offset: outer_start,
            try_length: inner_len,
            handler_offset: start,
            handler_length: ins.len() - start,
        });
    }
    ins[inner_leave] = Instruction::Leave(ins.len());
    if abi.returns == AbiType::Void {
        ins.push(Instruction::LoadNull);
    } else {
        ins.push(Instruction::LoadLocal(1));
        let (conversion, box_result) = match abi.returns {
            AbiType::F32 | AbiType::F64 => (Instruction::ConvertFloat64, api.box_floating),
            AbiType::Bool8 => (Instruction::Convert(ConversionType::UInt8), api.box_boolean),
            AbiType::U8 | AbiType::U16 | AbiType::U32 | AbiType::U64 | AbiType::Usize => (
                Instruction::Convert(ConversionType::UInt64),
                api.box_unsigned,
            ),
            _ => (Instruction::Convert(ConversionType::Int64), api.box_signed),
        };
        ins.extend([conversion, Instruction::call(box_result)]);
    }
    ins.push(Instruction::StoreLocal(2));
    let outer_leave = ins.len();
    ins.push(Instruction::Leave(0));
    // 外层范围同时包含 PInvoke、窄 catch 与返回物化；leave 保证成功也释放。
    let finally = ins.len();
    ins.extend([
        Instruction::LoadLocal(0),
        Instruction::call(api.dispose),
        Instruction::EndFinally,
    ]);
    clauses.push(body::Exception {
        kind: body::ExceptionKind::Finally,
        try_offset: outer_start,
        try_length: finally - outer_start,
        handler_offset: finally,
        handler_length: 3,
    });
    ins[outer_leave] = Instruction::Leave(ins.len());
    ins.extend([Instruction::LoadLocal(2), Instruction::Return]);
    let result_type = if abi.returns == AbiType::Void {
        ctype! { object }
    } else {
        native_abi_types::scalar(abi.returns)
    };
    let mut body = body::Method::with_locals(
        vec![
            LocalVariable::new(api.frame),
            LocalVariable::new(result_type),
            LocalVariable::new(ctype! { object }),
        ],
        ins,
    );
    // 最多 16 个缓冲展开为 32 个机器参数，读取下一参数需额外帧和索引两个槽。
    body.header.maximum_stack_size = 34;
    body.data_sections = vec![body::DataSection::ExceptionHandlers(clauses)];
    let wrapper = res.push_method(
        class,
        Method::new(
            Accessibility::Private,
            msig! { static object (object[]) },
            format!("__abi_body_{suffix}"),
            Some(body),
        ),
    );
    (
        vec![
            Instruction::load_string(name),
            Instruction::load_string(names),
            Instruction::LoadNull,
            Instruction::LoadMethodPointer(MethodSource::User(UserMethod::Definition(wrapper))),
            Instruction::NewObject(UserMethod::Reference(refs.delegate_ctor)),
            Instruction::call(api.create),
        ],
        wrapper,
        native,
    )
}
