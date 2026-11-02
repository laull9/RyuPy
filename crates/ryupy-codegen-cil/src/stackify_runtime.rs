//! RuntimeFn到CLR方法调用的集中映射；实参和PIC编号已由rvalue发射器先压栈。
//! 这里只选择ABI，不解释Python容器、枚举或异常协议。
use crate::stackify::LowerCtx;
use crate::stackify_refs::runtime_method;
use dotnetdll::prelude::*;
use ryupy_mir::RuntimeFn;

pub(crate) fn emit_call(rt_fn: &RuntimeFn, ins: &mut Vec<Instruction>, ctx: &LowerCtx<'_>) {
    let system = &ctx.runtime_refs.system;
    match rt_fn {
        RuntimeFn::PrintTarget => ins.push(Instruction::call(ctx.runtime_refs.system.print_target)),
        RuntimeFn::WriteI64 => ins.push(Instruction::call(system.write_i64)),
        RuntimeFn::WriteF64 => ins.push(Instruction::call(system.write_f64)),
        RuntimeFn::WriteBool => ins.push(Instruction::call(system.write_bool)),
        RuntimeFn::WriteStr => ins.push(Instruction::call(system.write_str)),
        RuntimeFn::WriteNewline => ins.push(Instruction::call(system.write_newline)),
        RuntimeFn::WriteSep => ins.push(Instruction::call(system.write_sep)),
        RuntimeFn::StrConcat => ins.push(Instruction::call(ctx.runtime_refs.str_concat)),
        RuntimeFn::FloorDivI64 => ins.push(Instruction::call(ctx.runtime_refs.floor_div_i64)),
        RuntimeFn::ModI64 => ins.push(Instruction::call(ctx.runtime_refs.mod_i64)),
        RuntimeFn::PowI64 => ins.push(Instruction::call(ctx.runtime_refs.pow_i64)),
        RuntimeFn::TrueDivF64 => ins.push(Instruction::call(ctx.runtime_refs.true_div_f64)),
        RuntimeFn::FloorDivF64 => ins.push(Instruction::call(ctx.runtime_refs.floor_div_f64)),
        RuntimeFn::ModF64 => ins.push(Instruction::call(ctx.runtime_refs.mod_f64)),
        RuntimeFn::PowF64 => ins.push(Instruction::call(ctx.runtime_refs.pow_f64)),
        RuntimeFn::F64ToI64 => ins.push(Instruction::call(ctx.runtime_refs.f64_to_i64)),
        RuntimeFn::AbsI64 => ins.push(Instruction::call(ctx.runtime_refs.abs_i64)),
        RuntimeFn::AbsF64 => ins.push(Instruction::call(ctx.runtime_refs.abs_f64)),
        RuntimeFn::ShlI64 => ins.push(Instruction::call(ctx.runtime_refs.shl_i64)),
        RuntimeFn::ShrI64 => ins.push(Instruction::call(ctx.runtime_refs.shr_i64)),
        RuntimeFn::GeneratorDelegateStart => {
            ins.push(Instruction::call(ctx.runtime_refs.delegation.start))
        }
        RuntimeFn::GeneratorDelegateStep => {
            ins.push(Instruction::call(ctx.runtime_refs.delegation.step))
        }
        RuntimeFn::GeneratorDelegateCurrent => {
            ins.push(Instruction::call(ctx.runtime_refs.delegation.current))
        }
        RuntimeFn::GeneratorDelegateResult => {
            ins.push(Instruction::call(ctx.runtime_refs.delegation.result))
        }
        RuntimeFn::GeneratorInput => ins.push(Instruction::call(ctx.runtime_refs.generator_input)),
        RuntimeFn::GeneratorRetry => ins.push(Instruction::call(ctx.runtime_refs.generator_retry)),
        RuntimeFn::ArrayGet => ins.push(Instruction::call(ctx.runtime_refs.array_get)),
        RuntimeFn::ArraySet => ins.push(Instruction::call(ctx.runtime_refs.array_set)),
        RuntimeFn::NumericListCreate => {
            ins.push(Instruction::call(ctx.runtime_refs.numeric_list.create))
        }
        RuntimeFn::NumericListAppendF64 => {
            ins.push(Instruction::call(ctx.runtime_refs.numeric_list.append_f64))
        }
        RuntimeFn::NumericListPeekList => {
            ins.push(Instruction::call(ctx.runtime_refs.numeric_list.peek_list))
        }
        RuntimeFn::NumericListCanReadF64 => ins.push(Instruction::call(
            ctx.runtime_refs.numeric_list.can_read_f64,
        )),
        RuntimeFn::NumericListReadCheckedF64 => ins.push(Instruction::call(
            ctx.runtime_refs.numeric_list.read_checked_f64,
        )),
        RuntimeFn::NumericListReadF64 => {
            ins.push(Instruction::call(ctx.runtime_refs.numeric_list.read_f64))
        }
        RuntimeFn::NumericAccumulatorCreate => {
            ins.push(Instruction::call(ctx.runtime_refs.accumulator.create))
        }
        RuntimeFn::NumericAccumulatorAdd => {
            ins.push(Instruction::call(ctx.runtime_refs.accumulator.add))
        }
        RuntimeFn::NumericAccumulatorValue => {
            ins.push(Instruction::call(ctx.runtime_refs.accumulator.value))
        }
        RuntimeFn::CellNew => ins.push(Instruction::call(ctx.runtime_refs.cell_new)),
        RuntimeFn::CellGet => ins.push(Instruction::call(ctx.runtime_refs.cell_get)),
        RuntimeFn::CellSet => ins.push(Instruction::call(ctx.runtime_refs.cell_set)),
        RuntimeFn::CellGetFree => ins.push(Instruction::call(ctx.runtime_refs.cell_get_free)),
        RuntimeFn::CallValue => ins.push(Instruction::call(ctx.runtime_refs.call_value)),
        RuntimeFn::BuiltinFunction => {
            ins.push(Instruction::call(ctx.runtime_refs.function_objects.builtin))
        }
        RuntimeFn::ClassNamespaceContains => ins.push(Instruction::call(
            ctx.runtime_refs.classes.namespace_contains,
        )),
        RuntimeFn::ClassNamespaceGet => ins.push(Instruction::call(
            ctx.runtime_refs.function_objects.class_namespace_get,
        )),
        RuntimeFn::DictSetScalarKey => {
            ins.push(Instruction::call(ctx.runtime_refs.dict_set_scalar_key))
        }
        RuntimeFn::ValidateOrdinaryBase => {
            ins.push(Instruction::call(ctx.runtime_refs.classes.validate))
        }
        RuntimeFn::TypedDictBegin => ins.push(Instruction::call(ctx.runtime_refs.typed_dict.begin)),
        RuntimeFn::TypedDictFinish => {
            ins.push(Instruction::call(ctx.runtime_refs.typed_dict.finish))
        }
        RuntimeFn::EnumCreate => ins.push(Instruction::call(ctx.runtime_refs.enum_create)),
        RuntimeFn::EnumFinalize => ins.push(Instruction::call(ctx.runtime_refs.enum_finalize)),
        RuntimeFn::DeclareDataclass => ins.push(Instruction::call(
            ctx.runtime_refs.function_objects.declare_dataclass,
        )),
        RuntimeFn::ApplyDataclass => ins.push(Instruction::call(
            ctx.runtime_refs.function_objects.apply_dataclass,
        )),
        RuntimeFn::FunctionMetadata => ins.push(Instruction::call(
            ctx.runtime_refs.function_objects.metadata,
        )),
        RuntimeFn::TypeObject => ins.push(Instruction::call(ctx.runtime_refs.type_object)),
        RuntimeFn::ArgError => ins.push(Instruction::call(ctx.runtime_refs.arg_error)),
        RuntimeFn::StarArg => ins.push(Instruction::call(ctx.runtime_refs.star_arg)),
        RuntimeFn::FormatValue => ins.push(Instruction::call(ctx.runtime_refs.format_value)),
        RuntimeFn::WriteObject => ins.push(Instruction::call(system.write_object)),
        RuntimeFn::DynBinary => ins.push(Instruction::call(ctx.runtime_refs.dyn_binary)),
        RuntimeFn::DynCompare => ins.push(Instruction::call(ctx.runtime_refs.dyn_compare)),
        RuntimeFn::DynUnary => ins.push(Instruction::call(ctx.runtime_refs.dyn_unary)),
        RuntimeFn::DynTruthy => ins.push(Instruction::call(ctx.runtime_refs.dyn_truthy)),
        RuntimeFn::DynNone => ins.push(Instruction::call(ctx.runtime_refs.dyn_none)),
        RuntimeFn::DynFromDecimal => ins.push(Instruction::call(ctx.runtime_refs.dyn_from_decimal)),
        RuntimeFn::DynToInt => ins.push(Instruction::call(ctx.runtime_refs.dyn_to_int)),
        RuntimeFn::DynToFloat => ins.push(Instruction::call(ctx.runtime_refs.dyn_to_float)),
        RuntimeFn::DynAbs => ins.push(Instruction::call(ctx.runtime_refs.dyn_abs)),
        RuntimeFn::MakeException => ins.push(Instruction::call(ctx.runtime_refs.make_exception)),
        RuntimeFn::WithCause => ins.push(Instruction::call(ctx.runtime_refs.with_cause)),
        RuntimeFn::RangeStep => ins.push(Instruction::call(ctx.runtime_refs.range_step)),
        RuntimeFn::HandlerEnter => ins.push(Instruction::call(ctx.runtime_refs.handler_enter)),
        RuntimeFn::HandlerLeave => ins.push(Instruction::call(ctx.runtime_refs.handler_leave)),
        RuntimeFn::Reraise => ins.push(Instruction::call(ctx.runtime_refs.reraise)),
        RuntimeFn::RaisePrepare => ins.push(Instruction::call(ctx.runtime_refs.raise_prepare)),
        RuntimeFn::MakeUnbound => ins.push(Instruction::call(ctx.runtime_refs.make_unbound)),
        RuntimeFn::TraceCheckDepth => {
            ins.push(Instruction::call(ctx.runtime_refs.trace_check_depth))
        }
        RuntimeFn::TracePush => ins.push(Instruction::call(ctx.runtime_refs.trace_push)),
        RuntimeFn::TraceLine => ins.push(Instruction::call(ctx.runtime_refs.trace_line)),
        RuntimeFn::TracePopTo => ins.push(Instruction::call(ctx.runtime_refs.trace_pop_to)),
        RuntimeFn::DynStr => ins.push(Instruction::call(ctx.runtime_refs.dyn_str)),
        RuntimeFn::DynType => ins.push(Instruction::call(ctx.runtime_refs.dyn_type)),
        RuntimeFn::DynGetAttr => ins.push(Instruction::call(ctx.runtime_refs.pic_get_attr)),
        RuntimeFn::SetAttr => ins.push(Instruction::call(ctx.runtime_refs.pic_set_attr)),
        RuntimeFn::LoadChecked => ins.push(Instruction::call(ctx.runtime_refs.load_checked)),
        RuntimeFn::LoadName => ins.push(Instruction::call(ctx.runtime_refs.bindings.load_name)),
        RuntimeFn::CheckGlobalBound => {
            ins.push(Instruction::call(ctx.runtime_refs.bindings.check_name))
        }
        RuntimeFn::ModuleFile => ins.push(Instruction::call(ctx.runtime_refs.module_file)),
        RuntimeFn::ImportModule => ins.push(Instruction::call(ctx.runtime_refs.import_module)),
        RuntimeFn::ImportAttr => ins.push(Instruction::call(ctx.runtime_refs.import_attr)),
        RuntimeFn::CallMethod => ins.push(Instruction::call(ctx.runtime_refs.call_method)),
        // 容器相关的调用与 CallWithArgs 走同一张表
        RuntimeFn::MakeList
        | RuntimeFn::MakeTuple
        | RuntimeFn::MakeSet
        | RuntimeFn::MakeDict
        | RuntimeFn::MakeSlice
        | RuntimeFn::MakeBytes
        | RuntimeFn::GetItem
        | RuntimeFn::SetItem
        | RuntimeFn::DelItem
        | RuntimeFn::Contains
        | RuntimeFn::Len
        | RuntimeFn::IsSame
        | RuntimeFn::ReprValue
        | RuntimeFn::DynBuiltin
        | RuntimeFn::CaptureModuleTarget
        | RuntimeFn::IsModule
        | RuntimeFn::CallCapturedMethod
        | RuntimeFn::PrepareMethod
        | RuntimeFn::SetAdd
        | RuntimeFn::SetExtend
        | RuntimeFn::ListAppend
        | RuntimeFn::ListExtend
        | RuntimeFn::UnpackCheck
        | RuntimeFn::IterStart
        | RuntimeFn::IterNextOk
        | RuntimeFn::IterValue
        | RuntimeFn::MakeClass
        | RuntimeFn::DelAttr
        | RuntimeFn::Decorate => {
            ins.push(Instruction::call(runtime_method(*rt_fn, ctx.runtime_refs)))
        }
        RuntimeFn::TraceAttach => {
            // M2.2 用 PrepareRaise 合并了「挂栈帧 + 挂上下文」，这里不再单独发射
            ins.push(Instruction::NoOperation);
        }
        RuntimeFn::CastToI64 => ins.push(Instruction::call(ctx.runtime_refs.cast_to_i64)),
        RuntimeFn::CastToF64 => ins.push(Instruction::call(ctx.runtime_refs.cast_to_f64)),
        RuntimeFn::CastToBool => ins.push(Instruction::call(ctx.runtime_refs.cast_to_bool)),
        RuntimeFn::CastToStr => ins.push(Instruction::call(ctx.runtime_refs.cast_to_str)),
    }
}
