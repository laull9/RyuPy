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
        res.push_method_reference(method_ref! { static double #pymath_type::AbsF64(double) });

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
    let traceback_ref =
        res.push_type_reference(type_ref! { RyuPy.Runtime.PyTraceback in #ryupy_runtime });
    let traceback_type: MethodType = BaseType::class(traceback_ref).into();
    let report_unhandled = res
        .push_method_reference(method_ref! { static void @traceback_type::Report(@py_exc_type) });

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
        method_body.header.maximum_stack_size = 64;
        res[method_idx].body = Some(method_body);
    }

    // 包装器最后生成：需要 `__main` 的方法下标
    if let Some(wrapper) = entry_wrapper {
        emit_entry_wrapper(
            &mut res,
            wrapper,
            fn_indices[&program.entry],
            report_unhandled,
            py_exc_type,
        );
    }

    (res, module_class)
}

/// 入口包装器：`try { __main(); return 0; } catch (PyBaseException e) { Report(e); return 1; }`。
///
/// 这是 M2.2 的第一块：**未捕获异常必须按 Python 的方式退出**（stderr 上 traceback、
/// 最后一行 `Type: message`、退出码 1），而不是让 CLR 打印 "Unhandled exception" 后
/// 以 134 退出。抛出的异常对象由 catch 子句压栈，直接作为 `Report` 的实参。
fn emit_entry_wrapper(
    res: &mut Resolution<'static>,
    wrapper: MethodIndex,
    body_method: MethodIndex,
    report_ref: MethodRefIndex,
    exception_type: MethodType,
) {
    // 指令布局（分支目标是**指令下标**）：
    //   0: call __main
    //   1: leave 5
    //   2: (handler) call Report   ← 异常对象已在栈上
    //   3: ldc.i4.1
    //   4: ret
    //   5: ldc.i4.0
    //   6: ret
    // 指令布局（分支目标是**指令下标**）：
    //   0: call __main          ┐ try
    //   1: leave 4              ┘
    //   2: call Report          ┐ handler（异常对象由 catch 子句压栈）
    //   3: leave 6              ┘
    //   4: ldc.i4.0
    //   5: ret
    //   6: ldc.i4.1
    //   7: ret
    //
    // 处理区必须以 `leave` 离开、不能直接 `ret`——否则 CLR 判为非法方法体
    // （InvalidProgramException，且堆栈里看不出是哪一条指令）。
    let instructions = vec![
        Instruction::call(body_method),
        Instruction::Leave(4),
        Instruction::call(report_ref),
        Instruction::Leave(6),
        Instruction::LoadConstantInt32(0),
        Instruction::Return,
        Instruction::LoadConstantInt32(1),
        Instruction::Return,
    ];
    let mut method_body = body::Method::new(instructions);
    method_body.header.maximum_stack_size = 8;
    method_body.data_sections = vec![body::DataSection::ExceptionHandlers(vec![
        body::Exception {
            kind: body::ExceptionKind::TypedException(exception_type),
            try_offset: 0,
            try_length: 2,
            handler_offset: 2,
            handler_length: 2,
        },
    ])];
    res[wrapper].body = Some(method_body);
}

pub fn emit(program: &Program, opts: &EmitOptions) -> Result<Vec<u8>, CodegenError> {
    let (res, _) = build_resolution(program, opts);
    res.write(WriteOptions {
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
