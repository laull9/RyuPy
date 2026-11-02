//! 运行时方法引用表（`RuntimeRefs`）的构造。
//!
//! 生成代码里出现的每一个 `RyuPy.Runtime` 方法 / 类型都在这里建立引用；
//! `build_resolution` 的其余步骤（类、函数、模块初始化）通过 `RuntimeRefs`
//! 与 [`RefExtras`] 取用。

use dotnetdll::prelude::*;
use rustc_hash::FxHashMap;

use crate::stackify::RuntimeRefs;

/// build_resolution 后续步骤需要的引用（不在 `RuntimeRefs` 里的散项）。
pub(crate) struct RefExtras {
    pub object_ref: TypeRefIndex,
    pub object_ctor_ref: MethodRefIndex,
    pub py_signature_ref: TypeRefIndex,
    pub py_exc_ref: TypeRefIndex,
    pub py_exc_type: MethodType,
    pub exception_type_refs: FxHashMap<String, TypeRefIndex>,
    #[allow(dead_code)]
    pub sys_runtime: AssemblyRefIndex,
    pub ryupy_runtime: AssemblyRefIndex,
    pub set_file: MethodRefIndex,
    pub traceback_type: MethodType,
    pub report_unhandled: MethodRefIndex,
    pub report_clr: MethodRefIndex,
    pub clr_exc_type: MethodType,
}

/// 建立程序集引用与全部运行时方法引用，返回 `RuntimeRefs` 与零散需要的引用。
pub(crate) fn build_runtime_refs(res: &mut Resolution<'static>) -> (RuntimeRefs, RefExtras) {
    let sys_runtime = res.push_assembly_reference(crate::assembly_refs::system_runtime());
    let object_ref = res.push_type_reference(type_ref! { System.Object in #sys_runtime });
    let object_ctor_ref = res.push_method_reference(method_ref! { void object::.ctor() });

    let ryupy_runtime = res.push_assembly_reference(crate::assembly_refs::ryupy_runtime());
    let builtins_ref =
        res.push_type_reference(type_ref! { RyuPy.Runtime.Builtins in #ryupy_runtime });
    let builtins_type: MethodType = BaseType::class(builtins_ref).into();

    let str_concat = res.push_method_reference(
        method_ref! { static string @builtins_type::StrConcat(string, string) },
    );
    // 保留旧Flush ABI引用；入口改用FinishExit，运行时报告等内部路径仍可直接刷新。
    let _flush_stdout =
        res.push_method_reference(method_ref! { static void @builtins_type::Flush() });

    let crate::math_refs::MathRefs {
        floor_div_i64,
        mod_i64,
        pow_i64,
        true_div_f64,
        floor_div_f64,
        mod_f64,
        pow_f64,
        f64_to_i64,
        abs_i64,
        abs_f64,
        shl_i64,
        shr_i64,
    } = crate::math_refs::MathRefs::build(res, ryupy_runtime);

    // ── 动态值 ABI（ADR-0010）──
    let pyops_ref = res.push_type_reference(type_ref! { RyuPy.Runtime.PyOps in #ryupy_runtime });
    let pyops_type: MethodType = BaseType::class(pyops_ref).into();
    let prepare_method = res.push_method_reference(
        method_ref! { static object @pyops_type::PrepareMethod(object, string) },
    );
    let dyn_binary = res.push_method_reference(
        method_ref! { static object @pyops_type::Binary(int, object, object) },
    );
    let dyn_compare = res.push_method_reference(
        method_ref! { static bool @pyops_type::Compare(int, object, object) },
    );
    let dyn_unary =
        res.push_method_reference(method_ref! { static object @pyops_type::Unary(int, object) });
    // PIC：动态运算与方法调用的有界内联缓存（M2.9）。
    let pypic_ref = res.push_type_reference(type_ref! { RyuPy.Runtime.PyPic in #ryupy_runtime });
    let pypic_type: MethodType = BaseType::class(pypic_ref).into();
    let pic_binary = res.push_method_reference(method_ref! {
        static object @pypic_type::Binary(int, int, object, object)
    });
    let pic_call_value = [
        res.push_method_reference(method_ref! {
            static object @pypic_type::CallValue0(int, object)
        }),
        res.push_method_reference(method_ref! {
            static object @pypic_type::CallValue1(int, object, object)
        }),
        res.push_method_reference(method_ref! {
            static object @pypic_type::CallValue2(int, object, object, object)
        }),
        res.push_method_reference(method_ref! {
            static object @pypic_type::CallValue3(int, object, object, object, object)
        }),
    ];
    let pic_call_method = [
        res.push_method_reference(method_ref! {
            static object @pypic_type::CallMethod0(int, object, string)
        }),
        res.push_method_reference(method_ref! {
            static object @pypic_type::CallMethod1(int, object, string, object)
        }),
        res.push_method_reference(method_ref! {
            static object @pypic_type::CallMethod2(int, object, string, object, object)
        }),
        res.push_method_reference(method_ref! {
            static object @pypic_type::CallMethod3(int, object, string, object, object, object)
        }),
    ];
    let dyn_truthy =
        res.push_method_reference(method_ref! { static bool @pyops_type::Truthy(object) });
    let dyn_none = res.push_method_reference(method_ref! { static object @pyops_type::None() });
    let box_bool =
        res.push_method_reference(method_ref! { static object @pyops_type::BoxBool(bool) });
    let box_i64 =
        res.push_method_reference(method_ref! { static object @pyops_type::BoxInt(long) });
    let empty_array = res.push_method_reference(method_ref! {
        static object[] @pyops_type::EmptyArray()
    });
    let dyn_from_decimal = res.push_method_reference(
        method_ref! { static object @pyops_type::FromDecimalString(string) },
    );
    let dyn_to_int =
        res.push_method_reference(method_ref! { static object @pyops_type::ToIntValue(object) });
    let dyn_to_float =
        res.push_method_reference(method_ref! { static object @pyops_type::ToFloatValue(object) });
    let dyn_abs =
        res.push_method_reference(method_ref! { static object @pyops_type::AbsValue(object) });

    // 未捕获异常的顶层报告（M2.2）
    let py_exc_ref =
        res.push_type_reference(type_ref! { RyuPy.Runtime.PyBaseException in #ryupy_runtime });
    let py_exc_type: MethodType = BaseType::class(py_exc_ref).into();
    let clr_exc_ref = res.push_type_reference(type_ref! { System.Exception in #sys_runtime });
    let clr_exc_type: MethodType = BaseType::class(clr_exc_ref).into();
    let traceback_ref =
        res.push_type_reference(type_ref! { RyuPy.Runtime.PyTraceback in #ryupy_runtime });
    let traceback_type: MethodType = BaseType::class(traceback_ref).into();
    let report_unhandled = res
        .push_method_reference(method_ref! { static void @traceback_type::Report(@py_exc_type) });
    // 兜底：非 Python 异常（静态 i64 溢出等）也按 Python 格式退出，不泄漏 .NET 调用栈
    let report_clr = res.push_method_reference(
        method_ref! { static void @traceback_type::ReportClr(@clr_exc_type) },
    );

    let pycast_ref = res.push_type_reference(type_ref! { RyuPy.Runtime.PyCast in #ryupy_runtime });
    let pycast_type: MethodType = BaseType::class(pycast_ref).into();
    let cast_to_i64 =
        res.push_method_reference(method_ref! { static long @pycast_type::ToInt64(object) });
    let cast_to_f64 =
        res.push_method_reference(method_ref! { static double @pycast_type::ToFloat64(object) });
    let cast_to_bool =
        res.push_method_reference(method_ref! { static bool @pycast_type::ToBool(object) });
    let cast_to_str =
        res.push_method_reference(method_ref! { static string @pycast_type::ToStr(object) });
    let range_step =
        res.push_method_reference(method_ref! { static long @pycast_type::RangeStep(object) });
    let make_exception = res.push_method_reference(
        method_ref! { static object @pyops_type::MakeException(string, object) },
    );
    let with_cause = res.push_method_reference(
        method_ref! { static object @pyops_type::WithCause(object, object) },
    );

    // ── 异常区域与栈帧（M2.2）──
    let pyerrors_ref =
        res.push_type_reference(type_ref! { RyuPy.Runtime.PyErrors in #ryupy_runtime });
    let pyerrors_type: MethodType = BaseType::class(pyerrors_ref).into();
    let handler_enter = res
        .push_method_reference(method_ref! { static object @pyerrors_type::EnterHandler(object) });
    let handler_leave =
        res.push_method_reference(method_ref! { static void @pyerrors_type::LeaveHandler(object) });
    let reraise =
        res.push_method_reference(method_ref! { static object @pyerrors_type::Reraise() });
    let raise_prepare = res
        .push_method_reference(method_ref! { static object @pyerrors_type::PrepareRaise(object) });
    let make_unbound =
        res.push_method_reference(method_ref! { static object @pyerrors_type::Unbound() });
    let load_checked = res
        .push_method_reference(method_ref! { static object @pyerrors_type::Load(object, string) });

    // ── 容器协议（M2.3）──
    let make_list =
        res.push_method_reference(method_ref! { static object @pyops_type::MakeList(object[]) });
    let make_tuple =
        res.push_method_reference(method_ref! { static object @pyops_type::MakeTuple(object[]) });
    let make_set =
        res.push_method_reference(method_ref! { static object @pyops_type::MakeSet(object[]) });
    let make_dict =
        res.push_method_reference(method_ref! { static object @pyops_type::MakeDict() });
    let make_slice = res.push_method_reference(
        method_ref! { static object @pyops_type::MakeSlice(object, object, object) },
    );
    let make_bytes =
        res.push_method_reference(method_ref! { static object @pyops_type::MakeBytes(string) });
    let get_item = res
        .push_method_reference(method_ref! { static object @pyops_type::GetItem(object, object) });
    let set_item = res.push_method_reference(
        method_ref! { static void @pyops_type::SetItem(object, object, object) },
    );
    let del_item =
        res.push_method_reference(method_ref! { static void @pyops_type::DelItem(object, object) });
    let contains = res
        .push_method_reference(method_ref! { static bool @pyops_type::Contains(object, object) });
    let len = res.push_method_reference(method_ref! { static long @pyops_type::Len(object) });
    let is_same =
        res.push_method_reference(method_ref! { static bool @pyops_type::Is(object, object) });
    let repr_value =
        res.push_method_reference(method_ref! { static string @pyops_type::ReprValue(object) });
    let call_builtin = res.push_method_reference(method_ref! {
        static object @pyops_type::CallBuiltin(string, object[], object[], object[])
    });
    let call_value = res.push_method_reference(method_ref! {
        static object @pypic_type::CallValue(int, object, object[], object[], object[])
    });
    let call_method = res.push_method_reference(method_ref! {
        static object @pypic_type::CallMethod(int, object, string, object[], object[], object[])
    });
    let array_get = res
        .push_method_reference(method_ref! { static object @pyops_type::ArrayGet(object, long) });
    let array_set = res.push_method_reference(
        method_ref! { static void @pyops_type::ArraySet(object, long, object) },
    );
    let type_object =
        res.push_method_reference(method_ref! { static object @pyops_type::TypeObject(string) });
    let arg_error =
        res.push_method_reference(method_ref! { static object @pyops_type::ArgError(int, string) });
    let star_arg =
        res.push_method_reference(method_ref! { static object @pyops_type::StarArg(object) });
    let format_value = res.push_method_reference(
        method_ref! { static string @pyops_type::FormatValue(object, string, int) },
    );
    let set_add =
        res.push_method_reference(method_ref! { static void @pyops_type::SetAdd(object, object) });
    let set_extend = res
        .push_method_reference(method_ref! { static void @pyops_type::SetExtend(object, object) });
    let list_append = res
        .push_method_reference(method_ref! { static void @pyops_type::ListAppend(object, object) });
    let list_extend = res
        .push_method_reference(method_ref! { static void @pyops_type::ListExtend(object, object) });
    let unpack_check = res.push_method_reference(
        method_ref! { static void @pyops_type::UnpackCheck(long, long, bool) },
    );
    let iter_start =
        res.push_method_reference(method_ref! { static object @pyops_type::IterStart(object) });
    let iter_next_ok =
        res.push_method_reference(method_ref! { static bool @pyops_type::IterNextOk(object) });
    let iter_value =
        res.push_method_reference(method_ref! { static object @pyops_type::IterValue(object) });
    // ── 对象协议（M2.2 最小集）──
    let pyrepr_ref = res.push_type_reference(type_ref! { RyuPy.Runtime.PyRepr in #ryupy_runtime });
    let pyrepr_type: MethodType = BaseType::class(pyrepr_ref).into();
    let pyobject_ref =
        res.push_type_reference(type_ref! { RyuPy.Runtime.PyObject in #ryupy_runtime });
    let pyobject_type: MethodType = BaseType::class(pyobject_ref).into();
    let dyn_str =
        res.push_method_reference(method_ref! { static string @pyrepr_type::StrValue(object) });
    let dyn_type =
        res.push_method_reference(method_ref! { static object @pyobject_type::TypeOf(object) });
    let pic_get_attr = res.push_method_reference(method_ref! {
        static object @pypic_type::GetAttr(int, object, string)
    });
    let pic_set_attr = res.push_method_reference(method_ref! {
        static void @pypic_type::SetAttr(int, object, string, object)
    });
    // ── 普通类与属性协议（M2.5 / M2.6）──
    let pyclass_ref =
        res.push_type_reference(type_ref! { RyuPy.Runtime.PyClass in #ryupy_runtime });
    let pyclass_type: MethodType = BaseType::class(pyclass_ref).into();
    let make_class = res.push_method_reference(method_ref! {
        static object @pyops_type::MakeClass(string, object[], object)
    });
    let set_attr = res.push_method_reference(method_ref! {
        static void @pyops_type::SetAttr(object, string, object)
    });
    let del_attr = res.push_method_reference(method_ref! {
        static void @pyops_type::DelAttr(object, string)
    });
    let decorate = res.push_method_reference(method_ref! {
        static object @pyops_type::Decorate(int, object)
    });
    let exception_factory_ref =
        res.push_type_reference(type_ref! { RyuPy.Runtime.PyExceptionFactory in #ryupy_runtime });
    let exception_factory_type: MethodType = BaseType::class(exception_factory_ref).into();
    let exception_factory_ctor = res.push_method_reference(method_ref! {
        void @exception_factory_type::.ctor(object, nint)
    });
    let bind_python_class = res.push_method_reference(method_ref! {
        void @py_exc_type::BindPythonClass(@pyclass_type)
    });
    let user_exception_message_for_base = res.push_method_reference(method_ref! {
        static string @pyerrors_type::UserExceptionMessageForBase(string, object[])
    });
    let trace_push = res.push_method_reference(
        method_ref! { static long @traceback_type::PushFrame(string, string) },
    );
    let trace_check_depth =
        res.push_method_reference(method_ref! { static void @traceback_type::CheckDepth() });
    let trace_line =
        res.push_method_reference(method_ref! { static void @traceback_type::SetLine(int) });
    let trace_pop_to =
        res.push_method_reference(method_ref! { static void @traceback_type::PopTo(long) });
    let set_file =
        res.push_method_reference(method_ref! { static void @traceback_type::SetFile(string) });

    // ── 一等函数与参数绑定（M2.1）──
    let py_signature_ref =
        res.push_type_reference(type_ref! { RyuPy.Runtime.PySignature in #ryupy_runtime });
    let py_signature_type: MethodType = BaseType::class(py_signature_ref).into();
    let py_call_ref = res.push_type_reference(type_ref! { RyuPy.Runtime.PyCall in #ryupy_runtime });
    let py_call_type: MethodType = BaseType::class(py_call_ref).into();
    let py_function_ref =
        res.push_type_reference(type_ref! { RyuPy.Runtime.PyFunction in #ryupy_runtime });
    let py_function_type: MethodType = BaseType::class(py_function_ref).into();
    let py_binder_ref =
        res.push_type_reference(type_ref! { RyuPy.Runtime.PyBinder in #ryupy_runtime });
    let py_binder_type: MethodType = BaseType::class(py_binder_ref).into();
    let signature_create = res.push_method_reference(method_ref! {
        static @py_signature_type @py_signature_type::Create(string, string[], string[], string[])
    });
    let bind_args = res.push_method_reference(method_ref! {
        static object[] @py_binder_type::Bind(
            @py_signature_type, object[], string[], object[], object[]
        )
    });
    let py_function_ctor = res.push_method_reference(method_ref! {
        void @py_function_type::.ctor(string, @py_signature_type, @py_call_type, object[], bool, object[])
    });
    let py_call_ctor =
        res.push_method_reference(method_ref! { void @py_call_type::.ctor(object, nint) });
    let py_function_get_defaults = res.push_method_reference(method_ref! {
        object[] @py_function_type::get_Defaults()
    });
    let py_function_get_env = res.push_method_reference(method_ref! {
        object[] @py_function_type::get_Env()
    });

    let module_init_ref =
        res.push_type_reference(type_ref! { RyuPy.Runtime.PyModuleInit in #ryupy_runtime });
    let module_get_ref =
        res.push_type_reference(type_ref! { RyuPy.Runtime.PyModuleGet in #ryupy_runtime });
    let module_set_ref =
        res.push_type_reference(type_ref! { RyuPy.Runtime.PyModuleSet in #ryupy_runtime });
    let module_init_type: MethodType = BaseType::class(module_init_ref).into();
    let module_get_type: MethodType = BaseType::class(module_get_ref).into();
    let module_set_type: MethodType = BaseType::class(module_set_ref).into();

    // ── 模块系统（M2.7）──
    let py_modules_ref =
        res.push_type_reference(type_ref! { RyuPy.Runtime.PyModules in #ryupy_runtime });
    let py_modules_type: MethodType = BaseType::class(py_modules_ref).into();
    let py_module_ref =
        res.push_type_reference(type_ref! { RyuPy.Runtime.PyModule in #ryupy_runtime });
    let py_module_type: MethodType = BaseType::class(py_module_ref).into();
    let import_module = res.push_method_reference(
        method_ref! { static @py_module_type @py_modules_type::Import(string) },
    );
    let import_attr = res.push_method_reference(
        method_ref! { static object @py_modules_type::ImportAttr(string, string) },
    );
    let module_run_main =
        res.push_method_reference(method_ref! { static void @py_modules_type::RunMain() });
    let module_same_name = res.push_method_reference(
        method_ref! { static bool @py_modules_type::SameName(string, string) },
    );
    let module_missing_attr = res.push_method_reference(
        method_ref! { static void @py_modules_type::MissingAttr(string, string) },
    );
    let module_set_dynamic = res.push_method_reference(method_ref! {
        static void @py_modules_type::SetDynamic(@py_module_type, string, object)
    });
    let module_register = res.push_method_reference(method_ref! {
        static void @py_modules_type::Register(string, string, string, @module_init_type, @module_get_type, @module_set_type)
    });
    let module_register_located = res.push_method_reference(method_ref! {
        static void @py_modules_type::RegisterLocated(string, string, string, long, string, bool, @module_init_type, @module_get_type, @module_set_type)
    });
    let module_file = res.push_method_reference(method_ref! {
        static string @py_modules_type::ModuleFile(string)
    });
    let module_init_ctor =
        res.push_method_reference(method_ref! { void @module_init_type::.ctor(object, nint) });
    let module_get_ctor =
        res.push_method_reference(method_ref! { void @module_get_type::.ctor(object, nint) });
    let module_set_ctor =
        res.push_method_reference(method_ref! { void @module_set_type::.ctor(object, nint) });

    // ── 闭包 cell（M3.0）──
    let cell_new = res.push_method_reference(method_ref! {
        static object @pyops_type::CellNew()
    });
    let cell_get = res.push_method_reference(method_ref! {
        static object @pyops_type::CellGet(object, string)
    });
    let cell_set = res.push_method_reference(method_ref! {
        static void @pyops_type::CellSet(object, object)
    });
    let cell_get_free = res.push_method_reference(method_ref! {
        static object @pyops_type::CellGetFree(object, long, string)
    });

    // Python 异常类型名 → CLR 类型：CIL 的类型化 catch 按这些类型匹配。
    // 通常为 `Py<PythonName>`；re.error 显式对应 PyRegexError，不能构造不存在的 Pyerror。
    let mut exception_types: FxHashMap<String, MethodType> = FxHashMap::default();
    let mut exception_type_refs: FxHashMap<String, TypeRefIndex> = FxHashMap::default();
    for type_name in ryupy_mir::EXCEPTION_TYPE_NAMES {
        let type_ref = if *type_name == "BaseException" {
            py_exc_ref
        } else {
            res.push_type_reference(ExternalTypeReference::new(
                Some("RyuPy.Runtime".into()),
                match *type_name {
                    "re.error" => "PyRegexError".to_owned(),
                    "io.UnsupportedOperation" => "PyUnsupportedOperation".to_owned(),
                    "json.JSONDecodeError" => "PyJSONDecodeError".to_owned(),
                    "dataclasses.FrozenInstanceError" => "PyFrozenInstanceError".to_owned(),
                    name => format!("Py{name}"),
                },
                ResolutionScope::Assembly(ryupy_runtime),
            ))
        };
        exception_types.insert((*type_name).to_string(), BaseType::class(type_ref).into());
        exception_type_refs.insert((*type_name).to_string(), type_ref);
    }
    let (generator_resume_ctor, generator_ctor, generator_input, generator_retry) =
        crate::generator_refs::build(res, ryupy_runtime);
    let enum_type = res.push_type_reference(type_ref! { RyuPy.Runtime.PyEnum in #ryupy_runtime });
    let enum_type: MethodType = BaseType::class(enum_type).into();
    let enum_finalize = res.push_method_reference(
        method_ref! { static object @enum_type::Finalize(object, object[]) },
    );
    let enum_create = res.push_method_reference(
        method_ref! { static object @enum_type::Create(string, object[], object) },
    );
    let dict_set_scalar_key = res.push_method_reference(
        method_ref! { static void @pyops_type::DictSetScalarKey(object, object, object) },
    );
    let runtime_refs = RuntimeRefs {
        dict_set_scalar_key,
        enum_create,
        classes: crate::class_refs::ClassRefs::build(res, ryupy_runtime),
        typed_dict: crate::typed_dict_refs::TypedDictRefs::build(res, ryupy_runtime),
        vectors: None,
        numeric_list: crate::numeric_list_refs::NumericListRefs::build(res, ryupy_runtime),
        enum_finalize,
        accumulator: crate::accumulator_refs::AccumulatorRefs::build(res, ryupy_runtime),
        system: crate::system_refs::SystemRefs::build(res, ryupy_runtime),
        function_objects: crate::function_refs::FunctionRefs::build(res, ryupy_runtime),
        delegation: crate::generator_refs::DelegateRefs::build(res, ryupy_runtime),
        prepare_method,
        generator_resume_ctor,
        generator_ctor,
        generator_input,
        generator_retry,
        str_concat,
        floor_div_i64,
        mod_i64,
        pow_i64,
        true_div_f64,
        floor_div_f64,
        mod_f64,
        pow_f64,
        f64_to_i64,
        abs_i64,
        abs_f64,
        shl_i64,
        shr_i64,
        dyn_binary,
        dyn_compare,
        dyn_unary,
        pic_binary,
        call_value,
        pic_call_value,
        pic_call_method,
        pic_get_attr,
        pic_set_attr,
        dyn_truthy,
        dyn_none,
        box_bool,
        box_i64,
        empty_array,
        literal_fields: FxHashMap::default(),
        dyn_from_decimal,
        dyn_to_int,
        dyn_to_float,
        dyn_abs,
        cast_to_i64,
        cast_to_f64,
        cast_to_bool,
        cast_to_str,
        range_step,
        make_exception,
        with_cause,
        handler_enter,
        handler_leave,
        reraise,
        raise_prepare,
        make_unbound,
        load_checked,
        bindings: crate::module_binding_refs::BindingRefs::build(res, ryupy_runtime),
        dyn_str,
        dyn_type,
        trace_push,
        trace_check_depth,
        trace_line,
        trace_pop_to,
        exception_types,
        make_list,
        make_tuple,
        make_set,
        make_dict,
        make_slice,
        make_bytes,
        get_item,
        set_item,
        del_item,
        contains,
        len,
        is_same,
        repr_value,
        call_builtin,
        call_method,
        array_get,
        array_set,
        type_object,
        arg_error,
        star_arg,
        format_value,
        list_append,
        set_add,
        set_extend,
        list_extend,
        unpack_check,
        iter_start,
        iter_next_ok,
        iter_value,
        make_class,
        set_attr,
        del_attr,
        decorate,
        py_class_type: pyclass_type,
        exception_factory_ctor,
        bind_python_class,
        user_exception_message_for_base,
        object_type: MethodType::from(BaseType::class(object_ref)),
        global_fields: FxHashMap::default(),
        signature_create,
        bind_args,
        py_function_ctor,
        py_call_ctor,
        py_function_type: py_function_type.clone(),
        py_function_get_defaults,
        py_function_get_env,
        import_module,
        import_attr,
        module_register,
        module_register_located,
        module_file,
        module_run_main,
        module_same_name,
        module_missing_attr,
        module_set_dynamic,
        module_init_ctor,
        module_get_ctor,
        module_set_ctor,
        py_module_type,
        cell_new,
        cell_get,
        cell_set,
        cell_get_free,
        py_function_ctor_env: py_function_ctor,
    };

    (
        runtime_refs,
        RefExtras {
            object_ref,
            object_ctor_ref,
            py_signature_ref,
            py_exc_ref,
            py_exc_type,
            exception_type_refs,
            sys_runtime,
            ryupy_runtime,
            set_file,
            traceback_type,
            report_unhandled,
            report_clr,
            clr_exc_type,
        },
    )
}
