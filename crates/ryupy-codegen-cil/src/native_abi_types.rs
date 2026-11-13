//! ABI 到 CLR 的唯一机械映射；缓冲区展开为 nint/nuint，不请求默认 Marshal。
use dotnetdll::prelude::*;
use ryupy_mir::native_abi::{AbiSignature, AbiType};

pub(crate) fn scalar(ty: AbiType) -> MethodType {
    match ty {
        AbiType::I8 => BaseType::Int8,
        AbiType::U8 | AbiType::Bool8 => BaseType::UInt8,
        AbiType::I16 => BaseType::Int16,
        AbiType::U16 => BaseType::UInt16,
        AbiType::I32 => BaseType::Int32,
        AbiType::U32 => BaseType::UInt32,
        AbiType::I64 => BaseType::Int64,
        AbiType::U64 => BaseType::UInt64,
        AbiType::Isize => BaseType::IntPtr,
        AbiType::Usize => BaseType::UIntPtr,
        AbiType::F32 => BaseType::Float32,
        AbiType::F64 => BaseType::Float64,
        _ => unreachable!("validated ABI scalar"),
    }
    .into()
}

pub(crate) fn signature(abi: &AbiSignature) -> ManagedMethod<MethodType> {
    let mut parameters = Vec::new();
    for parameter in &abi.parameters {
        if parameter.ty.is_buffer() {
            parameters.extend([
                Parameter::value(BaseType::IntPtr.into()),
                Parameter::value(BaseType::UIntPtr.into()),
            ]);
        } else {
            parameters.push(Parameter::value(scalar(parameter.ty)));
        }
    }
    MethodSignature::static_member(
        if abi.returns == AbiType::Void {
            ReturnType::VOID
        } else {
            ReturnType::value(scalar(abi.returns))
        },
        parameters,
    )
}

/// 供 driver 将同次实际 PE 报告和声明合同逐项比较；无需再加载或发射程序集。
pub fn signature_display(abi: &AbiSignature) -> String {
    signature(abi).show(&Resolution::new(Module::new("abi")))
}

pub(crate) fn conversion(ty: AbiType) -> Instruction {
    Instruction::Convert(match ty {
        AbiType::I8 => ConversionType::Int8,
        AbiType::U8 => ConversionType::UInt8,
        AbiType::I16 => ConversionType::Int16,
        AbiType::U16 => ConversionType::UInt16,
        AbiType::I32 => ConversionType::Int32,
        AbiType::U32 => ConversionType::UInt32,
        AbiType::I64 => ConversionType::Int64,
        AbiType::U64 => ConversionType::UInt64,
        AbiType::Isize => ConversionType::IntPtr,
        AbiType::Usize => ConversionType::UIntPtr,
        AbiType::F32 => return Instruction::ConvertFloat32,
        AbiType::F64 => return Instruction::ConvertFloat64,
        AbiType::Bool8 => ConversionType::UInt8,
        _ => unreachable!("validated ABI scalar"),
    })
}
