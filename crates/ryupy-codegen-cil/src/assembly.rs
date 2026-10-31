//! 组装并序列化程序集。
//!
//! 生成的程序集结构：
//!
//! ```text
//! .assembly <name>
//! .assembly extern System.Runtime
//! .assembly extern System.Console
//! .assembly extern RyuPy.Runtime
//! .class public abstract sealed RyuPy.Generated.Module      // 所有顶层函数都是它的静态方法
//!     .method public static void Main(string[] args) { .entrypoint ... }
//!     .method public static int64 fib(int64 n) { ... }
//! .class public RyuPy.Generated.<DataclassName>               // 每个 @dataclass 一个
//! ```

use std::borrow::Cow;

use dotnetdll::prelude::*;
use rustc_hash::FxHashMap;
use ryupy_mir::{FnId, Program};

use crate::stackify::{ClassCodegenInfo, ClassInfoMap, RuntimeRefs, lower_function};
use crate::types::ClassTypeMap;
use crate::{CodegenError, EmitOptions};

pub fn build_resolution(program: &Program, opts: &EmitOptions) -> (Resolution<'static>, TypeIndex) {
    let dll_name = format!("{}.dll", opts.assembly_name);
    let mut res = Resolution::new(Module::new(dll_name));
    res.assembly = Some(Assembly::new(opts.assembly_name.clone()));

    let sys_runtime = res.push_assembly_reference(ExternalAssemblyReference::new("System.Runtime"));
    let object_ref = res.push_type_reference(type_ref! { System.Object in #sys_runtime });
    let valuetype_ref = res.push_type_reference(type_ref! { System.ValueType in #sys_runtime });
    let object_ctor_ref = res.push_method_reference(method_ref! { void object::.ctor() });

    let ryupy_runtime =
        res.push_assembly_reference(ExternalAssemblyReference::new("RyuPy.Runtime"));
    let builtins_ref =
        res.push_type_reference(type_ref! { RyuPy.Runtime.Builtins in #ryupy_runtime });
    let builtins_type: MethodType = BaseType::class(builtins_ref).into();

    let write_i64 =
        res.push_method_reference(method_ref! { static void @builtins_type::WriteI64(long) });
    let write_f64 =
        res.push_method_reference(method_ref! { static void @builtins_type::WriteF64(double) });
    let write_bool =
        res.push_method_reference(method_ref! { static void @builtins_type::WriteBool(bool) });
    let write_str =
        res.push_method_reference(method_ref! { static void @builtins_type::WriteStr(string) });
    let write_newline =
        res.push_method_reference(method_ref! { static void @builtins_type::WriteNewline() });
    let write_sep =
        res.push_method_reference(method_ref! { static void @builtins_type::WriteSep() });
    let str_concat = res.push_method_reference(
        method_ref! { static string @builtins_type::StrConcat(string, string) },
    );
    // stdout 带缓冲：入口包装器退出前必须显式刷新
    let flush_stdout =
        res.push_method_reference(method_ref! { static void @builtins_type::Flush() });

    let pymath_ref = res.push_type_reference(type_ref! { RyuPy.Runtime.PyMath in #ryupy_runtime });
    let pymath_type: MethodType = BaseType::class(pymath_ref).into();

    let floor_div_i64 = res
        .push_method_reference(method_ref! { static long @pymath_type::FloorDivI64(long, long) });
    let mod_i64 =
        res.push_method_reference(method_ref! { static long @pymath_type::ModI64(long, long) });
    let pow_i64 =
        res.push_method_reference(method_ref! { static long @pymath_type::PowI64(long, long) });

    let true_div_f64 = res.push_method_reference(
        method_ref! { static double @pymath_type::TrueDivF64(double, double) },
    );
    let floor_div_f64 = res.push_method_reference(
        method_ref! { static double @pymath_type::FloorDivF64(double, double) },
    );
    let mod_f64 = res
        .push_method_reference(method_ref! { static double @pymath_type::ModF64(double, double) });
    let pow_f64 = res
        .push_method_reference(method_ref! { static double @pymath_type::PowF64(double, double) });
    let f64_to_i64 =
        res.push_method_reference(method_ref! { static long @pymath_type::F64ToI64(double) });
    let abs_i64 = res.push_method_reference(method_ref! { static long @pymath_type::AbsI64(long) });
    let abs_f64 =
        res.push_method_reference(method_ref! { static double @pymath_type::AbsF64(double) });
    let shl_i64 = res
        .push_method_reference(method_ref! { static long @pymath_type::ShiftLeftI64(long, long) });
    let shr_i64 = res
        .push_method_reference(method_ref! { static long @pymath_type::ShiftRightI64(long, long) });

    // ── 动态值 ABI（ADR-0010）──
    let write_object =
        res.push_method_reference(method_ref! { static void @builtins_type::WriteObject(object) });

    let pyops_ref = res.push_type_reference(type_ref! { RyuPy.Runtime.PyOps in #ryupy_runtime });
    let pyops_type: MethodType = BaseType::class(pyops_ref).into();
    let dyn_binary = res.push_method_reference(
        method_ref! { static object @pyops_type::Binary(int, object, object) },
    );
    let dyn_compare = res.push_method_reference(
        method_ref! { static bool @pyops_type::Compare(int, object, object) },
    );
    let dyn_unary =
        res.push_method_reference(method_ref! { static object @pyops_type::Unary(int, object) });
    let dyn_truthy =
        res.push_method_reference(method_ref! { static bool @pyops_type::Truthy(object) });
    let dyn_none = res.push_method_reference(method_ref! { static object @pyops_type::None() });
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
    let call_builtin = res.push_method_reference(
        method_ref! { static object @pyops_type::CallBuiltin(string, object[]) },
    );
    let call_method = res.push_method_reference(
        method_ref! { static object @pyops_type::CallMethod(object, string, object[]) },
    );
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
    let dyn_get_attr = res.push_method_reference(
        method_ref! { static object @pyobject_type::GetAttr(object, string) },
    );
    let trace_push =
        res.push_method_reference(method_ref! { static long @traceback_type::PushFrame(string) });
    let trace_line =
        res.push_method_reference(method_ref! { static void @traceback_type::SetLine(int) });
    let trace_pop_to =
        res.push_method_reference(method_ref! { static void @traceback_type::PopTo(long) });
    let set_file =
        res.push_method_reference(method_ref! { static void @traceback_type::SetFile(string) });

    // Python 异常类型名 → CLR 类型：CIL 的类型化 catch 按这些类型匹配。
    // 命名约定是 `Py<PythonName>`（如 `PyValueError`），与运行时 `PyExceptionTypes` 一致。
    let mut exception_types: FxHashMap<String, MethodType> = FxHashMap::default();
    for type_name in ryupy_mir::EXCEPTION_TYPE_NAMES {
        let type_ref = if *type_name == "BaseException" {
            py_exc_ref
        } else {
            res.push_type_reference(ExternalTypeReference::new(
                Some("RyuPy.Runtime".into()),
                format!("Py{type_name}"),
                ResolutionScope::Assembly(ryupy_runtime),
            ))
        };
        exception_types.insert((*type_name).to_string(), BaseType::class(type_ref).into());
    }

    let runtime_refs = RuntimeRefs {
        write_i64,
        write_f64,
        write_bool,
        write_str,
        write_newline,
        write_sep,
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
        write_object,
        dyn_binary,
        dyn_compare,
        dyn_unary,
        dyn_truthy,
        dyn_none,
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
        dyn_str,
        dyn_type,
        dyn_get_attr,
        trace_push,
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
        list_append,
        list_extend,
        unpack_check,
        iter_start,
        iter_next_ok,
        iter_value,
        object_type: MethodType::from(BaseType::class(object_ref)),
    };

    let module_class = res.push_type_definition(TypeDefinition::new(
        Some("RyuPy.Generated".into()),
        "Module",
    ));
    res[module_class].set_extends(object_ref);
    res[module_class].flags.abstract_type = true;
    res[module_class].flags.sealed = true;
    res[module_class].flags.accessibility = TypeAccessibility::Public;

    // 1. 为所有类预先分配 TypeIndex
    let mut class_types = ClassTypeMap::new(object_ref);
    let mut class_defs: Vec<(ryupy_mir::ClassDefId, TypeIndex)> = Vec::new();
    for (cls_id, cls) in program.classes.iter() {
        let type_idx = res.push_type_definition(TypeDefinition::new(
            Some("RyuPy.Generated".into()),
            cls.name.clone(),
        ));
        class_types
            .classes
            .insert(cls_id, (type_idx, cls.is_value_type));
        class_defs.push((cls_id, type_idx));
    }

    // 2. 为每个类配置 extends、flags、fields 与 .ctor
    let mut class_info: ClassInfoMap = FxHashMap::default();
    for &(cls_id, type_idx) in &class_defs {
        let cls = &program.classes[cls_id];
        if cls.is_value_type {
            res[type_idx].set_extends(valuetype_ref);
            res[type_idx].flags.layout = Layout::Sequential(None);
            res[type_idx].flags.sealed = true;
            res[type_idx].flags.accessibility = TypeAccessibility::Public;
        } else {
            res[type_idx].set_extends(object_ref);
            res[type_idx].flags.sealed = true;
            res[type_idx].flags.accessibility = TypeAccessibility::Public;
        }

        let mut field_indices = Vec::new();
        for (field_name, field_ty) in &cls.fields {
            let member_ty = crate::types::mir_type_to_member_type(field_ty, &class_types).unwrap();
            let field_idx = res.push_field(
                type_idx,
                Field::instance(Accessibility::Public, field_name.clone(), member_ty),
            );
            field_indices.push(field_idx);
        }

        // 构造函数 .ctor
        let ctor_params: Vec<Parameter<MethodType>> = cls
            .fields
            .iter()
            .map(|(_, field_ty)| {
                crate::types::mir_type_to_parameter(field_ty, &class_types).unwrap()
            })
            .collect();
        let ctor_sig = MethodSignature::instance(ReturnType::VOID, ctor_params);
        let ctor_method_idx = res.push_method(
            type_idx,
            Method::new(Accessibility::Public, ctor_sig, ".ctor", None),
        );
        res[ctor_method_idx].special_name = true;
        res[ctor_method_idx].runtime_special_name = true;

        let mut ctor_instructions = Vec::new();
        if !cls.is_value_type {
            ctor_instructions.push(Instruction::LoadArgument(0));
            ctor_instructions.push(Instruction::call(object_ctor_ref));
        }
        for (i, &f_idx) in field_indices.iter().enumerate() {
            ctor_instructions.push(Instruction::LoadArgument(0));
            ctor_instructions.push(Instruction::LoadArgument((i + 1) as u16));
            ctor_instructions.push(Instruction::StoreField {
                unaligned: None,
                volatile: false,
                param0: f_idx.into(),
            });
        }
        ctor_instructions.push(Instruction::Return);
        let mut ctor_body = body::Method::new(ctor_instructions);
        ctor_body.header.maximum_stack_size = 8;
        res[ctor_method_idx].body = Some(ctor_body);

        class_info.insert(
            cls_id,
            ClassCodegenInfo {
                ctor: ctor_method_idx,
                fields: field_indices,
            },
        );
    }

    // 第一遍：为所有函数分配 MethodIndex。
    //
    // 入口函数拆成两个方法：`Main` 是返回 int 的包装器（带 try/catch，负责把未捕获的
    // Python 异常渲染成 traceback 并返回 1），`__main` 承载真正的模块顶层代码。
    let mut fn_indices: FxHashMap<FnId, MethodIndex> = FxHashMap::default();
    let mut entry_wrapper: Option<MethodIndex> = None;
    for (fn_id, func) in program.functions.iter() {
        let method_idx = if fn_id == program.entry {
            let main_method = res.push_method(
                module_class,
                Method::new(
                    Accessibility::Public,
                    msig! { static int (string[]) },
                    "Main",
                    None,
                ),
            );
            res.set_entry_point(main_method);
            entry_wrapper = Some(main_method);
            // 顶层代码放进 __main()，保留 void 返回，避免包装器与模块返回值的耦合
            res.push_method(
                module_class,
                Method::new(
                    Accessibility::Private,
                    msig! { static void () },
                    "__main",
                    None,
                ),
            )
        } else {
            let params: Vec<_> = func
                .params
                .iter()
                .map(|&p| {
                    crate::types::mir_type_to_parameter(&func.body.locals[p].ty, &class_types)
                        .unwrap()
                })
                .collect();
            let ret = crate::types::mir_type_to_return_type(&func.ret, &class_types);
            let sig = MethodSignature::new(false, ret, params);
            res.push_method(
                module_class,
                Method::new(
                    Accessibility::Public,
                    sig,
                    Cow::Owned(func.name.clone()),
                    None,
                ),
            )
        };
        fn_indices.insert(fn_id, method_idx);
    }

    // 第二遍：逐函数编译并填充 body
    for (fn_id, func) in program.functions.iter() {
        let method_idx = fn_indices[&fn_id];
        let compiled = lower_function(func, &runtime_refs, &fn_indices, &class_types, &class_info);
        let mut method_body = body::Method::with_locals(compiled.locals, compiled.instructions);
        method_body.header.maximum_stack_size = compiled.max_stack_size;
        if !compiled.exceptions.is_empty() {
            method_body.data_sections =
                vec![body::DataSection::ExceptionHandlers(compiled.exceptions)];
        }
        res[method_idx].body = Some(method_body);
    }

    // 包装器最后生成：需要 `__main` 的方法下标
    if let Some(wrapper) = entry_wrapper {
        let py_system_exit_ref = res.push_type_reference(ExternalTypeReference::new(
            Some("RyuPy.Runtime".into()),
            "PySystemExit",
            ResolutionScope::Assembly(ryupy_runtime),
        ));
        let py_system_exit_type: MethodType = BaseType::class(py_system_exit_ref).into();
        let system_exit_code = res.push_method_reference(
            method_ref! { static int @traceback_type::SystemExitCode(@py_system_exit_type) },
        );
        emit_entry_wrapper(
            &mut res,
            wrapper,
            fn_indices[&program.entry],
            report_unhandled,
            report_clr,
            py_exc_type,
            clr_exc_type,
            py_system_exit_type,
            set_file,
            system_exit_code,
            flush_stdout,
            &program.source_name,
        );
    }

    (res, module_class)
}

/// 入口包装器：`try { __main(); return 0; } catch (PyBaseException e) { Report(e); return 1; }`。
///
/// 这是 M2.2 的第一块：**未捕获异常必须按 Python 的方式退出**（stderr 上 traceback、
/// 最后一行 `Type: message`、退出码 1），而不是让 CLR 打印 "Unhandled exception" 后
/// 以 134 退出。抛出的异常对象由 catch 子句压栈，直接作为 `Report` 的实参。
#[allow(clippy::too_many_arguments)]
fn emit_entry_wrapper(
    res: &mut Resolution<'static>,
    wrapper: MethodIndex,
    body_method: MethodIndex,
    report_ref: MethodRefIndex,
    report_clr_ref: MethodRefIndex,
    exception_type: MethodType,
    clr_exception_type: MethodType,
    system_exit_type: MethodType,
    set_file: MethodRefIndex,
    system_exit_code: MethodRefIndex,
    flush_stdout: MethodRefIndex,
    source_name: &str,
) {
    // 指令布局（分支目标是**指令下标**）：
    //   0: ldstr source_name
    //   1: call SetFile
    //   2: call __main              ┐ try
    //   3: leave 15                 ┘
    //   4: call SystemExitCode      ┐ catch PySystemExit
    //   5: stloc.0                  │
    //   6: leave 15                 ┘
    //   7: call Report              ┐ catch PyBaseException
    //   8: ldc.i4.1                 │
    //   9: stloc.0                  │
    //  10: leave 15                 ┘
    //  11: call ReportClr           ┐ catch System.Exception（兜底）
    //  12: ldc.i4.1                 │
    //  13: stloc.0                  │
    //  14: leave 15                 ┘
    //  15: call Flush               ← 所有路径汇合于此
    //  16: ldloc.0
    //  17: ret
    //
    // 处理区必须以 `leave` 离开、不能直接 `ret`——否则 CLR 判为非法方法体
    // （InvalidProgramException，且堆栈里看不出是哪一条指令）。
    let instructions = vec![
        Instruction::load_string(source_name),
        Instruction::call(set_file),
        Instruction::call(body_method),
        Instruction::Leave(15),
        // SystemExit：算出退出码而不是打印 traceback
        Instruction::call(system_exit_code),
        Instruction::StoreLocal(0),
        Instruction::Leave(15),
        // 其余未捕获 Python 异常：打印 traceback 并退出 1
        Instruction::call(report_ref),
        Instruction::LoadConstantInt32(1),
        Instruction::StoreLocal(0),
        Instruction::Leave(15),
        // 兜底：CLR 异常（静态 i64 溢出等）映射为 Python 错误后报告
        Instruction::call(report_clr_ref),
        Instruction::LoadConstantInt32(1),
        Instruction::StoreLocal(0),
        Instruction::Leave(15),
        // 汇合点：刷新 stdout 缓冲后返回退出码
        Instruction::call(flush_stdout),
        Instruction::LoadLocal(0),
        Instruction::Return,
    ];
    let mut method_body = body::Method::new(instructions);
    method_body.header.maximum_stack_size = 8;
    method_body.header.local_variables =
        vec![LocalVariable::new(MethodType::from(BaseType::Int32))];
    method_body.data_sections = vec![body::DataSection::ExceptionHandlers(vec![
        body::Exception {
            kind: body::ExceptionKind::TypedException(system_exit_type),
            try_offset: 2,
            try_length: 2,
            handler_offset: 4,
            handler_length: 3,
        },
        body::Exception {
            kind: body::ExceptionKind::TypedException(exception_type),
            try_offset: 2,
            try_length: 2,
            handler_offset: 7,
            handler_length: 4,
        },
        body::Exception {
            kind: body::ExceptionKind::TypedException(clr_exception_type),
            try_offset: 2,
            try_length: 2,
            handler_offset: 11,
            handler_length: 4,
        },
    ])];
    res[wrapper].body = Some(method_body);
}

pub fn emit(program: &Program, opts: &EmitOptions) -> Result<Vec<u8>, CodegenError> {
    let (res, _) = build_resolution(program, opts);
    res.write(WriteOptions {
        // `true` 生成 PE32 + ILONLY（AnyCPU），不是「32 位程序」：
        // .NET 编译器对 AnyCPU 程序集同样使用 I386 文件头，运行时按目标平台 JIT。
        // 改成 `false` 会写成 AMD64 专用镜像，反而不能在 arm64 / x86 上加载。
        is_32_bit: true,
        is_executable: false,
    })
    .map_err(|e| CodegenError::Write(e.to_string()))
}

pub fn dump(program: &Program, opts: &EmitOptions) -> Result<String, CodegenError> {
    let (res, module_class) = build_resolution(program, opts);
    let mut out = String::new();
    out.push_str(&format!(".assembly {}\n{{\n}}\n", opts.assembly_name));
    out.push_str(".assembly extern System.Runtime\n{\n}\n");
    out.push_str(".assembly extern RyuPy.Runtime\n{\n}\n\n");
    out.push_str(
        ".class public abstract sealed auto ansi beforefieldinit RyuPy.Generated.Module\n",
    );
    out.push_str("    extends [System.Runtime]System.Object\n{\n");

    for (idx, method) in res[module_class].methods.iter().enumerate() {
        if idx > 0 {
            out.push('\n');
        }
        let ret_str = method.signature.return_type.show(&res);
        let params_str = method
            .signature
            .parameters
            .iter()
            .map(|p| p.show(&res))
            .collect::<Vec<_>>()
            .join(", ");
        out.push_str(&format!(
            "    .method public static {ret_str} {}({params_str}) cil managed\n    {{\n",
            method.name
        ));

        if method.name == "Main" {
            out.push_str("        .entrypoint\n");
        }

        if let Some(body) = &method.body {
            if !body.header.local_variables.is_empty() {
                out.push_str("        .locals init (\n");
                for (i, loc) in body.header.local_variables.iter().enumerate() {
                    let comma = if i + 1 < body.header.local_variables.len() {
                        ","
                    } else {
                        ""
                    };
                    out.push_str(&format!("            [{i}] {}{comma}\n", loc.show(&res)));
                }
                out.push_str("        )\n");
            }
            for (i, ins) in body.instructions.iter().enumerate() {
                out.push_str(&format!("        IL_{i:04x}: {}\n", ins.show(&res)));
            }
            // 异常表：排错时没有它就等于盲调
            for section in &body.data_sections {
                if let body::DataSection::ExceptionHandlers(clauses) = section {
                    for clause in clauses {
                        let kind = match &clause.kind {
                            body::ExceptionKind::TypedException(ty) => {
                                format!("catch {}", ty.show(&res))
                            }
                            body::ExceptionKind::Filter { offset } => format!("filter {offset}"),
                            body::ExceptionKind::Finally => "finally".to_string(),
                            body::ExceptionKind::Fault => "fault".to_string(),
                        };
                        out.push_str(&format!(
                            "        // EH: try [{}..{}) {kind} handler [{}..{})\n",
                            clause.try_offset,
                            clause.try_offset + clause.try_length,
                            clause.handler_offset,
                            clause.handler_offset + clause.handler_length
                        ));
                    }
                }
            }
        }
        out.push_str("    }\n");
    }

    out.push_str("}\n");

    for (_cls_id, cls) in program.classes.iter() {
        out.push('\n');
        let extends_str = if cls.is_value_type {
            "[System.Runtime]System.ValueType"
        } else {
            "[System.Runtime]System.Object"
        };
        out.push_str(&format!(
            ".class public auto ansi sealed beforefieldinit RyuPy.Generated.{}\n    extends {}\n{{\n",
            cls.name, extends_str
        ));
        for (f_name, f_ty) in &cls.fields {
            let ty_str = match f_ty {
                ryupy_mir::MirType::I64 => "int64",
                ryupy_mir::MirType::F64 => "float64",
                ryupy_mir::MirType::Bool => "bool",
                ryupy_mir::MirType::Str => "string",
                _ => "object",
            };
            out.push_str(&format!("    .field public {ty_str} {f_name}\n"));
        }
        out.push_str("}\n");
    }

    Ok(out)
}
