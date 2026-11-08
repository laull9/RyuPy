//! `RuntimeRefs`：生成代码引用的运行时方法表，与 `RyuPy.Runtime` 的 API 一一对应。
//!
//! `lower_function` 与 `emit_thunk` 通过这张表取方法引用；新增运行时入口时
//! 在这里加字段、在 `refs.rs` 里建引用、在 `lower_rvalue` 的分派表里接线。
//! 数值字面量池的键与初始化也在这里（`assembly` 的池初始化使用）。

use dotnetdll::prelude::*;
use rustc_hash::FxHashMap;
use ryupy_mir::{Constant as MirConstant, GlobalId, RuntimeFn};

pub struct RuntimeRefs {
    pub(crate) deletions: Option<crate::module_deletion_refs::DeletionRefs>,
    pub(crate) owned_functions: Option<crate::owned_function_refs::OwnedFunctionRefs>,
    pub(crate) class_cell_guard: Option<MethodRefIndex>,
    pub(crate) zero_call_frame: Option<MethodRefIndex>,
    pub(crate) instances: Option<crate::instance_refs::InstanceRefs>,
    pub(crate) vectors: Option<crate::vector_refs::VectorRefs>,
    /// 普通Enum类体完成转换；Python规则由运行时处理。
    pub enum_finalize: MethodRefIndex,
    pub enum_create: MethodRefIndex,
    pub(crate) classes: crate::class_refs::ClassRefs,
    pub(crate) numeric_list: crate::numeric_list_refs::NumericListRefs,
    pub(crate) typed_dict: crate::typed_dict_refs::TypedDictRefs,
    pub dict_set_scalar_key: MethodRefIndex,
    pub(crate) accumulator: crate::accumulator_refs::AccumulatorRefs,
    pub(crate) system: crate::system_refs::SystemRefs,
    pub(crate) function_objects: crate::function_refs::FunctionRefs,
    pub(crate) delegation: crate::generator_refs::DelegateRefs,
    pub(crate) generators: crate::generator_refs::GeneratorRefs,
    pub(crate) origins: crate::origin_refs::OriginRefs,
    pub prepare_method: MethodRefIndex,
    pub str_concat: MethodRefIndex,
    pub floor_div_i64: MethodRefIndex,
    pub mod_i64: MethodRefIndex,
    pub pow_i64: MethodRefIndex,
    pub true_div_f64: MethodRefIndex,
    pub floor_div_f64: MethodRefIndex,
    pub mod_f64: MethodRefIndex,
    pub pow_f64: MethodRefIndex,
    pub f64_to_i64: MethodRefIndex,
    pub abs_i64: MethodRefIndex,
    pub abs_f64: MethodRefIndex,
    pub shl_i64: MethodRefIndex,
    pub shr_i64: MethodRefIndex,
    // ── 动态值 ABI（ADR-0010）──
    pub dyn_binary: MethodRefIndex,
    pub dyn_compare: MethodRefIndex,
    pub dyn_unary: MethodRefIndex,
    pub pic_binary: MethodRefIndex,
    pub call_value: MethodRefIndex,
    pub pic_call_value: [MethodRefIndex; 4],
    pub pic_call_method: [MethodRefIndex; 4],
    pub pic_get_attr: MethodRefIndex,
    pub pic_set_attr: MethodRefIndex,
    pub dyn_truthy: MethodRefIndex,
    pub dyn_none: MethodRefIndex,
    /// `PyOps.BoxBool(bool)`：复用 True / False 单例。
    pub box_bool: MethodRefIndex,
    /// `PyOps.BoxInt(long)`：小整数走缓存。
    pub box_i64: MethodRefIndex,
    /// `PyOps.EmptyArray()`：共享空 `object[]`，避免每次无关键字调用都 `newarr`。
    pub empty_array: MethodRefIndex,
    /// 动态数值字面量池：`Rvalue::Box` 直接复用静态字段里的装箱对象。
    pub literal_fields: FxHashMap<String, FieldIndex>,
    pub dyn_from_decimal: MethodRefIndex,
    pub dyn_to_int: MethodRefIndex,
    pub dyn_to_float: MethodRefIndex,
    pub dyn_abs: MethodRefIndex,
    pub cast_to_i64: MethodRefIndex,
    pub cast_to_f64: MethodRefIndex,
    pub cast_to_bool: MethodRefIndex,
    pub cast_to_str: MethodRefIndex,
    pub make_exception: MethodRefIndex,
    pub with_cause: MethodRefIndex,
    pub range_step: MethodRefIndex,
    // ── 异常区域与栈帧（M2.2）──
    pub handler_enter: MethodRefIndex,
    pub handler_leave: MethodRefIndex,
    pub exception_match: MethodRefIndex,
    pub reraise: MethodRefIndex,
    pub raise_prepare: MethodRefIndex,
    #[cfg(feature = "trace-adapter-test")]
    pub trace_adapter_explicit: MethodRefIndex,
    #[cfg(feature = "trace-adapter-test")]
    pub trace_adapter_handle: MethodRefIndex,
    #[cfg(feature = "trace-adapter-test")]
    pub trace_adapter_forward: MethodRefIndex,
    pub make_unbound: MethodRefIndex,
    pub load_checked: MethodRefIndex,
    /// 模块名字与属性采用 MIR 显式声明的绑定检查，保留有类型槽。
    pub(crate) bindings: crate::module_binding_refs::BindingRefs,
    pub dyn_str: MethodRefIndex,
    pub dyn_type: MethodRefIndex,
    pub trace_push: MethodRefIndex,
    /// 只检查递归限额，不增减 traceback 帧。
    pub trace_check_depth: MethodRefIndex,
    pub trace_line: MethodRefIndex,
    pub trace_pop_to: MethodRefIndex,
    /// Python 异常类型名 → CLR 类型（CIL 类型化 catch 用）。
    pub exception_types: FxHashMap<String, MethodType>,
    // ── 容器协议（M2.3）──
    pub make_list: MethodRefIndex,
    pub make_tuple: MethodRefIndex,
    pub make_set: MethodRefIndex,
    pub make_dict: MethodRefIndex,
    pub make_slice: MethodRefIndex,
    pub make_bytes: MethodRefIndex,
    pub get_item: MethodRefIndex,
    pub set_item: MethodRefIndex,
    pub del_item: MethodRefIndex,
    pub contains: MethodRefIndex,
    pub len: MethodRefIndex,
    pub is_same: MethodRefIndex,
    pub repr_value: MethodRefIndex,
    pub call_builtin: MethodRefIndex,
    pub function_locals: [MethodRefIndex; 3],
    pub call_method: MethodRefIndex,
    pub array_get: MethodRefIndex,
    pub array_set: MethodRefIndex,
    pub type_object: MethodRefIndex,
    pub arg_error: MethodRefIndex,
    pub star_arg: MethodRefIndex,
    pub format_value: MethodRefIndex,
    pub list_append: MethodRefIndex,
    pub set_add: MethodRefIndex,
    pub set_extend: MethodRefIndex,
    pub list_extend: MethodRefIndex,
    pub unpack_check: MethodRefIndex,
    pub unpack_sequence: MethodRefIndex,
    pub unpack_ex: MethodRefIndex,
    pub iter_start: MethodRefIndex,
    pub iter_next_ok: MethodRefIndex,
    pub iter_value: MethodRefIndex,
    // ── 类与属性协议（M2.5 / M2.6）──
    pub make_class: MethodRefIndex,
    pub set_attr: MethodRefIndex,
    pub del_attr: MethodRefIndex,
    pub decorate: MethodRefIndex,
    /// `RyuPy.Runtime.PyClass` 的类型（异常类构造函数参数）。
    pub py_class_type: MethodType,
    /// `PyExceptionFactory` 委托的构造函数。
    pub exception_factory_ctor: MethodRefIndex,
    /// `PyBaseException.BindPythonClass(PyClass)`。
    pub bind_python_class: MethodRefIndex,
    /// `PyErrors.UserExceptionMessageForBase(string, object[]) -> string`。
    pub user_exception_message_for_base: MethodRefIndex,
    /// `System.Object` 的 `MethodType`：`object[]` 的元素类型。
    pub object_type: MethodType,
    // ── 一等函数与参数绑定（M2.1）──
    /// 全局槽位 → 模块静态字段。
    pub global_fields: FxHashMap<GlobalId, FieldIndex>,
    pub signature_create: MethodRefIndex,
    pub bind_args: MethodRefIndex,
    pub py_call_ctor: MethodRefIndex,
    /// `RyuPy.Runtime.PyFunction` 的类型（thunk 的第一个参数）。
    pub py_function_type: MethodType,
    /// `PyFunction.get_Defaults()`。
    pub py_function_get_defaults: MethodRefIndex,
    /// `PyFunction.get_Env()`。
    pub py_function_get_env: MethodRefIndex,
    // ── 模块系统（M2.7）──
    pub import_module: MethodRefIndex,
    pub import_attr: MethodRefIndex,
    pub module_register: MethodRefIndex,
    pub module_metadata: MethodRefIndex,
    pub module_register_source_located: MethodRefIndex,
    pub module_file: MethodRefIndex,
    pub module_run_main: MethodRefIndex,
    pub module_same_name: MethodRefIndex,
    pub module_missing_attr: MethodRefIndex,
    pub module_set_dynamic: MethodRefIndex,
    pub module_init_ctor: MethodRefIndex,
    pub module_get_ctor: MethodRefIndex,
    pub module_set_ctor: MethodRefIndex,
    /// `RyuPy.Runtime.PyModule` 的类型（setter 的第一个参数）。
    pub py_module_type: MethodType,
    // ── 闭包 cell 与函数对象创建（M3.0）──
    /// `PyOps.CellNew() -> object`。
    pub cell_new: MethodRefIndex,
    /// `PyOps.CellGet(object, string) -> object`。
    pub cell_get: MethodRefIndex,
    /// `PyOps.CellSet(object, object)`。
    pub cell_set: MethodRefIndex,
    /// `PyOps.CellGetFree(object[], long, string) -> object`。
    pub cell_get_free: MethodRefIndex,
    /// `PyFunction` 构造函数（含 env 参数）。
    pub py_function_ctor_env: MethodRefIndex,
}

/// `RuntimeFn` → 方法引用。容器相关的调用与普通调用共用这张表。
pub(crate) fn runtime_method(callee: RuntimeFn, refs: &RuntimeRefs) -> MethodRefIndex {
    match callee {
        RuntimeFn::PrepareMethod => refs.prepare_method,
        RuntimeFn::CaptureModuleTarget => refs.bindings.capture_module_target,
        RuntimeFn::IsModule => refs.bindings.is_module,
        RuntimeFn::CallCapturedMethod => refs.bindings.call_captured_method,
        RuntimeFn::MakeList => refs.make_list,
        RuntimeFn::MakeTuple => refs.make_tuple,
        RuntimeFn::MakeSet => refs.make_set,
        RuntimeFn::MakeDict => refs.make_dict,
        RuntimeFn::MakeSlice => refs.make_slice,
        RuntimeFn::MakeBytes => refs.make_bytes,
        RuntimeFn::GetItem => refs.get_item,
        RuntimeFn::SetItem => refs.set_item,
        RuntimeFn::DelItem => refs.del_item,
        RuntimeFn::Contains => refs.contains,
        RuntimeFn::Len => refs.len,
        RuntimeFn::IsSame => refs.is_same,
        RuntimeFn::ReprValue => refs.repr_value,
        RuntimeFn::DynBuiltin => refs.call_builtin,
        RuntimeFn::FunctionLocalsValidate => refs.function_locals[0],
        RuntimeFn::FunctionLocalsEnsure => refs.function_locals[1],
        RuntimeFn::FunctionLocalsSync => refs.function_locals[2],
        RuntimeFn::CallMethod => refs.call_method,
        RuntimeFn::ListAppend => refs.list_append,
        RuntimeFn::SetAdd => refs.set_add,
        RuntimeFn::SetExtend => refs.set_extend,
        RuntimeFn::ListExtend => refs.list_extend,
        RuntimeFn::UnpackCheck => refs.unpack_check,
        RuntimeFn::UnpackSequence => refs.unpack_sequence,
        RuntimeFn::UnpackEx => refs.unpack_ex,
        RuntimeFn::IterStart => refs.iter_start,
        RuntimeFn::IterNextOk => refs.iter_next_ok,
        RuntimeFn::IterValue => refs.iter_value,
        RuntimeFn::MakeClass => refs.make_class,
        RuntimeFn::SetAttr => refs.set_attr,
        RuntimeFn::DelAttr => refs.del_attr,
        RuntimeFn::Decorate => refs.decorate,
        other => unreachable!("{other:?} 不是容器调用"),
    }
}

/// 可池化的动态数值字面量。
#[derive(Clone, Debug)]
pub enum LiteralInit {
    I64(i64),
    F64(f64),
    Bool(bool),
    BigInt(String),
}

/// 为 `Box` 的常量生成稳定的池键；不是数值字面量时返回 `None`。
/// 栈顶的静态值装箱成 `object`。
///
/// `bool` 复用运行时单例、`int` 走小整数缓存（都不分配），`float` 直接 `box`；
/// 引用类型（str / class / 已是 object）不需要装箱。所有静态 → 动态的装箱都应经过这里，
/// 保证 `True` 的身份在各模块之间一致。
pub fn emit_box(
    ins: &mut Vec<Instruction>,
    from: &ryupy_mir::MirType,
    refs: &RuntimeRefs,
    class_types: &crate::types::ClassTypeMap,
) {
    match from {
        ryupy_mir::MirType::Bool => ins.push(Instruction::call(refs.box_bool)),
        ryupy_mir::MirType::I64 => ins.push(Instruction::call(refs.box_i64)),
        ryupy_mir::MirType::Str | ryupy_mir::MirType::Dynamic | ryupy_mir::MirType::Class(_) => {}
        _ => {
            if let Some(mt) = crate::types::mir_type_to_method_type(from, class_types) {
                ins.push(Instruction::BoxValue(mt));
            }
        }
    }
}

pub fn boxed_literal_key(constant: &MirConstant, from: &ryupy_mir::MirType) -> Option<String> {
    use ryupy_mir::MirType;
    match (from, constant) {
        (MirType::I64, MirConstant::I64(v)) => Some(format!("i:{v}")),
        (MirType::F64, MirConstant::F64(v)) => Some(format!("f:{:016x}", v.to_bits())),
        (MirType::Bool, MirConstant::Bool(v)) => Some(format!("b:{v}")),
        (MirType::Dynamic, MirConstant::BigInt(text)) => Some(format!("n:{text}")),
        _ => None,
    }
}

/// 生成池条目的初始化值；与 [`boxed_literal_key`] 一一对应。
pub fn boxed_literal_init(
    constant: &MirConstant,
    from: &ryupy_mir::MirType,
) -> Option<LiteralInit> {
    use ryupy_mir::MirType;
    match (from, constant) {
        (MirType::I64, MirConstant::I64(v)) => Some(LiteralInit::I64(*v)),
        (MirType::F64, MirConstant::F64(v)) => Some(LiteralInit::F64(*v)),
        (MirType::Bool, MirConstant::Bool(v)) => Some(LiteralInit::Bool(*v)),
        (MirType::Dynamic, MirConstant::BigInt(text)) => Some(LiteralInit::BigInt(text.clone())),
        _ => None,
    }
}
