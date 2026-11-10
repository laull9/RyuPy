//! RuntimeFn的显式CLR签名验证；操作码是内部编译期常量，不是Python值转换。
use crate::mir::*;
use crate::validate::operand_type;

/// 这些运行时方法只有固定Call签名，不能通过CallWithArgs追加数组改变ABI。
pub(super) fn fixed_abi(callee: RuntimeFn) -> bool {
    if crate::trace_product_abi::signature(callee).is_some() {
        return true;
    }
    #[cfg(feature = "trace-adapter-test")]
    if matches!(
        callee,
        RuntimeFn::TraceAdapterExplicit
            | RuntimeFn::TraceAdapterHandle
            | RuntimeFn::TraceAdapterForward
    ) {
        return true;
    }
    matches!(
        callee,
        RuntimeFn::InstanceClassGuard
            | RuntimeFn::InstanceClassCellGuard
            | RuntimeFn::InstanceRelativeImport
            | RuntimeFn::InstanceRelativeImportAttr
            | RuntimeFn::InstanceImportGuard
            | RuntimeFn::InstanceLoadName
            | RuntimeFn::InstanceStoreName
            | RuntimeFn::InstanceDeleteName
            | RuntimeFn::CheckModuleDelete
            | RuntimeFn::CheckModuleDeleteValue
            | RuntimeFn::LoadDeletedModuleName
            | RuntimeFn::LoadDefaultBuiltin
            | RuntimeFn::InstanceMakeClass
            | RuntimeFn::InstanceExecutionSource
            | RuntimeFn::InstanceCaptureDefinitionContext
            | RuntimeFn::InstanceDefinitionModule
            | RuntimeFn::ModuleFile
            | RuntimeFn::ModuleMetadata
            | RuntimeFn::FunctionLocalsValidate
            | RuntimeFn::FunctionLocalsEnsure
            | RuntimeFn::FunctionLocalsSync
            | RuntimeFn::FinalizeOrdinaryClass
            | RuntimeFn::FunctionModule
            | RuntimeFn::FunctionDefaults
            | RuntimeFn::TracePushWithOrigin
            | RuntimeFn::ModuleExecutionSource
            | RuntimeFn::ModuleExecutionSourceWithCode
            | RuntimeFn::UnpackEx
    ) || crate::validate_module_pairs::fixed_abi(callee)
}

pub(super) fn validate_call(
    program: &Program,
    func: &Function,
    callee: &Callee,
    args: &[Operand],
    errors: &mut Vec<String>,
) {
    // 输出与累加器专用ABI校验，防止参数错配生成栈不平衡的CIL。
    if let Callee::Runtime(runtime) = callee {
        if let Some((expected, _)) = crate::trace_product_abi::signature(*runtime) {
            if args.len() != expected.len()
                || args
                    .iter()
                    .zip(expected)
                    .any(|(arg, ty)| operand_type(program, func, arg).as_ref() != Some(ty))
            {
                errors.push(format!(
                    "函数 `{}` 的产品 trace ABI 实参类型/数量错误",
                    func.name
                ));
            }
            return;
        }
        if *runtime == RuntimeFn::DynBinary && !valid_binary_opcode(args.first()) {
            errors.push(format!(
                "函数 `{}` 的通用二元操作码必须为合法编译期i64常量",
                func.name
            ));
        }
        let expected: Option<&[MirType]> = match runtime {
            #[cfg(feature = "trace-adapter-test")]
            RuntimeFn::TraceAdapterExplicit => Some(&[MirType::Dynamic]),
            #[cfg(feature = "trace-adapter-test")]
            RuntimeFn::TraceAdapterHandle | RuntimeFn::TraceAdapterForward => {
                Some(&[MirType::Dynamic, MirType::I64])
            }
            RuntimeFn::InstanceRelativeImport => Some(&[
                MirType::Dynamic,
                MirType::Str,
                MirType::I64,
                MirType::Dynamic,
            ]),
            RuntimeFn::InstanceRelativeImportAttr => {
                Some(&[MirType::Dynamic, MirType::Dynamic, MirType::Str])
            }
            RuntimeFn::InstanceMakeClass => {
                Some(&[MirType::Dynamic, MirType::Str, MirType::Dynamic])
            }
            RuntimeFn::InstanceClassGuard | RuntimeFn::InstanceClassCellGuard => {
                Some(&[MirType::Dynamic])
            }
            RuntimeFn::InstanceLoadName => Some(&[MirType::Dynamic, MirType::Str]),
            RuntimeFn::LoadDefaultBuiltin => Some(&[MirType::Str]),
            RuntimeFn::InstanceDeleteName
            | RuntimeFn::CheckModuleDeleteValue
            | RuntimeFn::LoadDeletedModuleName => Some(&[MirType::Dynamic, MirType::Str]),
            RuntimeFn::CheckModuleDelete => Some(&[MirType::Bool, MirType::Str]),
            RuntimeFn::InstanceStoreName => {
                Some(&[MirType::Dynamic, MirType::Str, MirType::Dynamic])
            }
            RuntimeFn::InstanceImportGuard
            | RuntimeFn::InstanceExecutionSource
            | RuntimeFn::InstanceCaptureDefinitionContext
            | RuntimeFn::InstanceDefinitionModule => Some(&[MirType::Dynamic]),
            RuntimeFn::FunctionLocalsValidate => {
                Some(&[MirType::Dynamic, MirType::Dynamic, MirType::Dynamic])
            }
            RuntimeFn::FunctionLocalsEnsure => Some(&[MirType::Dynamic]),
            RuntimeFn::FunctionLocalsSync => {
                Some(&[MirType::Dynamic, MirType::Str, MirType::Dynamic])
            }
            RuntimeFn::ExceptionMatch => Some(&[MirType::Dynamic, MirType::Dynamic]),
            RuntimeFn::UnpackEx => Some(&[MirType::Dynamic, MirType::I64, MirType::I64]),
            RuntimeFn::FinalizeOrdinaryClass => Some(&[MirType::Dynamic]),
            RuntimeFn::FunctionModule => Some(&[MirType::Dynamic, MirType::Dynamic]),
            RuntimeFn::ModuleExecutionSourceWithCode => Some(&[MirType::Dynamic, MirType::Dynamic]),
            RuntimeFn::FunctionDefaults | RuntimeFn::ModuleExecutionSource => {
                Some(&[MirType::Dynamic])
            }
            RuntimeFn::TracePushWithOrigin => Some(&[MirType::Str, MirType::Str, MirType::Dynamic]),
            RuntimeFn::ModuleFile => Some(&[MirType::Str]),
            RuntimeFn::ModuleMetadata => Some(&[MirType::Str, MirType::Str]),
            RuntimeFn::CaptureModuleTarget => Some(&[MirType::Dynamic, MirType::Str]),
            RuntimeFn::IsModule => Some(&[MirType::Dynamic]),
            RuntimeFn::CallCapturedMethod => Some(&[
                MirType::Bool,
                MirType::Dynamic,
                MirType::Str,
                MirType::Dynamic,
                MirType::Dynamic,
                MirType::Dynamic,
            ]),
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
        if *runtime == RuntimeFn::FunctionLocalsValidate
            && !matches!(
                args,
                [Operand::Array(_), Operand::Array(_), Operand::Array(_)]
            )
        {
            errors.push(format!(
                "函数 `{}` 的locals绑定ABI必须接收三个object数组",
                func.name
            ));
        }
        if *runtime == RuntimeFn::CallCapturedMethod
            && !matches!(
                args,
                [
                    _,
                    _,
                    _,
                    Operand::Array(_),
                    Operand::Array(_),
                    Operand::Array(_)
                ]
            )
        {
            errors.push(format!(
                "函数 `{}` 的捕获调用必须传三个显式参数数组",
                func.name
            ));
        }
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
                } else if matches!(
                    runtime,
                    RuntimeFn::CheckGlobalBound
                        | RuntimeFn::CheckModuleDelete
                        | RuntimeFn::CheckModuleDeleteValue
                        | RuntimeFn::LoadDeletedModuleName
                        | RuntimeFn::InstanceDeleteName
                ) {
                    "模块绑定位ABI"
                } else if matches!(runtime, RuntimeFn::ClassNamespaceContains) {
                    "类命名空间存在性ABI"
                } else if matches!(runtime, RuntimeFn::ValidateOrdinaryBase) {
                    "普通基类守卫ABI"
                } else if matches!(runtime, RuntimeFn::ModuleFile | RuntimeFn::ModuleMetadata) {
                    "模块位置ABI"
                } else if matches!(runtime, RuntimeFn::UnpackEx) {
                    "星号解包ABI"
                } else if matches!(runtime, RuntimeFn::LoadDefaultBuiltin) {
                    "默认内建读取ABI"
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
    if matches!(
        value,
        Rvalue::Call {
            callee: Callee::Runtime(
                RuntimeFn::InstanceClassGuard
                    | RuntimeFn::InstanceClassCellGuard
                    | RuntimeFn::InstanceImportGuard
                    | RuntimeFn::InstanceStoreName
                    | RuntimeFn::InstanceDeleteName
                    | RuntimeFn::CheckModuleDelete
                    | RuntimeFn::CheckModuleDeleteValue
                    | RuntimeFn::FinalizeOrdinaryClass
                    | RuntimeFn::FunctionModule
                    | RuntimeFn::FunctionLocalsValidate
                    | RuntimeFn::FunctionLocalsSync
            ),
            ..
        }
    ) {
        errors.push(format!(
            "函数 `{}` 的运行时ABI返回void，只允许Eval",
            func.name
        ));
    }
    if let Rvalue::Call {
        callee: Callee::Runtime(runtime),
        ..
    } = value
    {
        if let Some((_, expected)) = crate::trace_product_abi::signature(*runtime) {
            if expected == MirType::Unit
                || crate::validate::local_decl(&func.body.locals, dest)
                    .is_none_or(|decl| decl.ty != expected)
            {
                errors.push(format!("函数 `{}` 的产品 trace ABI 返回槽非法", func.name));
            }
            return;
        }
        #[cfg(feature = "trace-adapter-test")]
        if matches!(
            runtime,
            RuntimeFn::TraceAdapterHandle | RuntimeFn::TraceAdapterForward
        ) {
            errors.push(format!(
                "函数 `{}` 的S2a接收ABI返回void，只允许Eval",
                func.name
            ));
        }
        let expected = match runtime {
            #[cfg(feature = "trace-adapter-test")]
            RuntimeFn::TraceAdapterExplicit => Some(MirType::Dynamic),
            RuntimeFn::ModuleFile => Some(MirType::Str),
            RuntimeFn::TracePushWithOrigin => Some(MirType::I64),
            RuntimeFn::InstanceRelativeImport
            | RuntimeFn::InstanceRelativeImportAttr
            | RuntimeFn::InstanceLoadName
            | RuntimeFn::LoadDeletedModuleName
            | RuntimeFn::LoadDefaultBuiltin
            | RuntimeFn::InstanceMakeClass
            | RuntimeFn::InstanceExecutionSource
            | RuntimeFn::InstanceCaptureDefinitionContext
            | RuntimeFn::InstanceDefinitionModule
            | RuntimeFn::ModuleMetadata
            | RuntimeFn::FunctionLocalsEnsure
            | RuntimeFn::CaptureModuleTarget
            | RuntimeFn::FunctionDefaults
            | RuntimeFn::ModuleExecutionSource
            | RuntimeFn::ModuleExecutionSourceWithCode
            | RuntimeFn::UnpackEx
            | RuntimeFn::CallCapturedMethod
            | RuntimeFn::NumericListCreate
            | RuntimeFn::NumericListAppendF64
            | RuntimeFn::NumericListPeekList => Some(MirType::Dynamic),
            RuntimeFn::IsModule | RuntimeFn::NumericListCanReadF64 | RuntimeFn::ExceptionMatch => {
                Some(MirType::Bool)
            }
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

/// 运行时返回值必须消费；这是既有 Eval 规则的等价归拢，仅新增星号入口。
pub(super) fn validate_eval(func: &Function, value: &Rvalue, errors: &mut Vec<String>) {
    if let Rvalue::Call {
        callee: Callee::Runtime(runtime),
        ..
    } = value
        && let Some((_, result)) = crate::trace_product_abi::signature(*runtime)
        && result != MirType::Unit
    {
        errors.push(format!(
            "函数 `{}` 的产品 trace ABI 返回值必须消费",
            func.name
        ));
    }
    #[cfg(feature = "trace-adapter-test")]
    if matches!(
        value,
        Rvalue::Call {
            callee: Callee::Runtime(RuntimeFn::TraceAdapterExplicit),
            ..
        }
    ) {
        errors.push(format!("函数 `{}` 的S2a源返回值必须写入临时", func.name));
    }
    if matches!(
        value,
        Rvalue::Call {
            callee: Callee::Runtime(
                RuntimeFn::InstanceMakeClass
                    | RuntimeFn::InstanceLoadName
                    | RuntimeFn::LoadDeletedModuleName
                    | RuntimeFn::LoadDefaultBuiltin
                    | RuntimeFn::InstanceExecutionSource
                    | RuntimeFn::InstanceCaptureDefinitionContext
                    | RuntimeFn::InstanceDefinitionModule
                    | RuntimeFn::FunctionLocalsEnsure
                    | RuntimeFn::ModuleFile
                    | RuntimeFn::ModuleMetadata
                    | RuntimeFn::TracePushWithOrigin
                    | RuntimeFn::FunctionDefaults
                    | RuntimeFn::ModuleExecutionSource
                    | RuntimeFn::ModuleExecutionSourceWithCode
                    | RuntimeFn::CaptureModuleTarget
                    | RuntimeFn::IsModule
                    | RuntimeFn::CallCapturedMethod
                    | RuntimeFn::DynBinary
                    | RuntimeFn::ClassNamespaceContains
                    | RuntimeFn::GeneratorInput
                    | RuntimeFn::GeneratorRetry
                    | RuntimeFn::PrintTarget
                    | RuntimeFn::NumericListCreate
                    | RuntimeFn::NumericListAppendF64
                    | RuntimeFn::NumericListPeekList
                    | RuntimeFn::NumericListCanReadF64
                    | RuntimeFn::NumericListReadCheckedF64
                    | RuntimeFn::NumericListReadF64
                    | RuntimeFn::NumericAccumulatorCreate
                    | RuntimeFn::NumericAccumulatorValue
                    | RuntimeFn::ValidateOrdinaryBase
                    | RuntimeFn::TypedDictBegin
                    | RuntimeFn::TypedDictFinish
                    | RuntimeFn::UnpackEx
                    | RuntimeFn::ExceptionMatch
            ),
            ..
        }
    ) {
        errors.push(format!(
            "函数 `{}` 的运行时控制调用返回值必须写入临时，不能遗留CIL求值栈",
            func.name
        ));
    }
}
