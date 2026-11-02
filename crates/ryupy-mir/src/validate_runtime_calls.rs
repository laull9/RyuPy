//! RuntimeFn的显式CLR签名验证；操作码是内部编译期常量，不是Python值转换。
use crate::mir::*;
use crate::validate::operand_type;

pub(super) fn validate_call(
    program: &Program,
    func: &Function,
    callee: &Callee,
    args: &[Operand],
    errors: &mut Vec<String>,
) {
    // 输出与累加器专用ABI校验，防止参数错配生成栈不平衡的CIL。
    if let Callee::Runtime(runtime) = callee {
        if *runtime == RuntimeFn::DynBinary && !valid_binary_opcode(args.first()) {
            errors.push(format!(
                "函数 `{}` 的通用二元操作码必须为合法编译期i64常量",
                func.name
            ));
        }
        let expected: Option<&[MirType]> = match runtime {
            RuntimeFn::ModuleFile => Some(&[MirType::Str]),
            RuntimeFn::CaptureModuleMethod => Some(&[MirType::Dynamic, MirType::Str]),
            RuntimeFn::NumericListCreate => Some(&[]),
            RuntimeFn::NumericListAppendF64 => Some(&[MirType::Dynamic, MirType::F64]),
            RuntimeFn::NumericListPeekList
            | RuntimeFn::NumericListCanReadF64
            | RuntimeFn::NumericListReadF64
            | RuntimeFn::NumericListReadCheckedF64 => Some(&[MirType::Dynamic, MirType::Dynamic]),
            RuntimeFn::DynBinary => Some(&[MirType::I64, MirType::Dynamic, MirType::Dynamic]),
            RuntimeFn::PrintTarget | RuntimeFn::TraceCheckDepth => Some(&[]),
            RuntimeFn::CheckGlobalBound => Some(&[MirType::Bool, MirType::Str]),
            RuntimeFn::ClassNamespaceContains => Some(&[MirType::Dynamic, MirType::Str]),
            RuntimeFn::NumericAccumulatorCreate
            | RuntimeFn::NumericAccumulatorValue
            | RuntimeFn::TypedDictFinish
            | RuntimeFn::ValidateOrdinaryBase => Some(&[MirType::Dynamic]),
            RuntimeFn::TypedDictBegin => {
                Some(&[MirType::Str, MirType::Dynamic, MirType::Bool, MirType::Str])
            }
            RuntimeFn::NumericAccumulatorAdd => Some(&[MirType::Dynamic, MirType::Dynamic]),
            RuntimeFn::WriteI64 => Some(&[MirType::Dynamic, MirType::I64]),
            RuntimeFn::WriteF64 => Some(&[MirType::Dynamic, MirType::F64]),
            RuntimeFn::WriteBool => Some(&[MirType::Dynamic, MirType::Bool]),
            RuntimeFn::WriteStr => Some(&[MirType::Dynamic, MirType::Str]),
            RuntimeFn::WriteObject => Some(&[MirType::Dynamic, MirType::Dynamic]),
            RuntimeFn::WriteSep | RuntimeFn::WriteNewline => Some(&[MirType::Dynamic]),
            _ => None,
        };
        if let Some(expected) = expected
            && (args.len() != expected.len()
                || args.iter().zip(expected).any(|(arg, ty)| {
                    operand_type(program, func, arg).is_some_and(|actual| &actual != ty)
                }))
        {
            errors.push(format!(
                "函数 {} 的 {:?} 参数不符合{}",
                func.name,
                runtime,
                if matches!(
                    runtime,
                    RuntimeFn::NumericAccumulatorCreate
                        | RuntimeFn::NumericAccumulatorAdd
                        | RuntimeFn::NumericAccumulatorValue
                ) {
                    "累加器ABI"
                } else if matches!(
                    runtime,
                    RuntimeFn::TypedDictBegin | RuntimeFn::TypedDictFinish
                ) {
                    "TypedDict构建ABI"
                } else if matches!(
                    runtime,
                    RuntimeFn::NumericListCreate
                        | RuntimeFn::NumericListAppendF64
                        | RuntimeFn::NumericListPeekList
                        | RuntimeFn::NumericListCanReadF64
                        | RuntimeFn::NumericListReadF64
                        | RuntimeFn::NumericListReadCheckedF64
                ) {
                    "数值列表ABI"
                } else if matches!(runtime, RuntimeFn::TraceCheckDepth) {
                    "递归深度检查ABI"
                } else if matches!(runtime, RuntimeFn::DynBinary) {
                    "通用二元运算ABI"
                } else if matches!(runtime, RuntimeFn::CheckGlobalBound) {
                    "模块绑定位ABI"
                } else if matches!(runtime, RuntimeFn::ClassNamespaceContains) {
                    "类命名空间存在性ABI"
                } else if matches!(runtime, RuntimeFn::ValidateOrdinaryBase) {
                    "普通基类守卫ABI"
                } else if matches!(runtime, RuntimeFn::ModuleFile) {
                    "模块位置ABI"
                } else {
                    "输出目标ABI"
                }
            ));
        }
    }
}

/// 只接受十二个既有二元操作及可选inplace位；不是任意截断后的i32。
fn valid_binary_opcode(value: Option<&Operand>) -> bool {
    let Some(Operand::Const(Constant::I64(code))) = value else {
        return false;
    };
    i32::try_from(*code).is_ok_and(|code| (1..=12).contains(&(code & !DynOp::INPLACE_FLAG)))
}

pub(super) fn validate_destination(
    func: &Function,
    dest: LocalId,
    value: &Rvalue,
    errors: &mut Vec<String>,
) {
    if let Rvalue::Call {
        callee: Callee::Runtime(runtime),
        ..
    } = value
    {
        let expected = match runtime {
            RuntimeFn::ModuleFile => Some(MirType::Str),
            RuntimeFn::CaptureModuleMethod
            | RuntimeFn::NumericListCreate
            | RuntimeFn::NumericListAppendF64
            | RuntimeFn::NumericListPeekList => Some(MirType::Dynamic),
            RuntimeFn::NumericListCanReadF64 => Some(MirType::Bool),
            RuntimeFn::NumericListReadF64 | RuntimeFn::NumericListReadCheckedF64 => {
                Some(MirType::F64)
            }
            _ => None,
        };
        if let Some(expected) = expected
            && func
                .body
                .locals
                .iter()
                .find(|(id, _)| *id == dest)
                .is_some_and(|(_, local)| local.ty != expected)
        {
            errors.push(format!("函数 `{}` 的运行时ABI返回槽类型错误", func.name));
        }
    }
    if matches!(
        value,
        Rvalue::Call {
            callee: Callee::Runtime(RuntimeFn::DynBinary),
            ..
        }
    ) && func
        .body
        .locals
        .iter()
        .find(|(id, _)| *id == dest)
        .is_some_and(|(_, local)| local.ty != MirType::Dynamic)
    {
        errors.push(format!(
            "函数 `{}` 的通用二元运算必须保存动态结果",
            func.name
        ));
    }
}
