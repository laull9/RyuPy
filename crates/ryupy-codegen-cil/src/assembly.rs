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

use crate::stackify::{RuntimeRefs, lower_function};
use crate::{CodegenError, EmitOptions};

pub fn build_resolution(program: &Program, opts: &EmitOptions) -> (Resolution<'static>, TypeIndex) {
    let dll_name = format!("{}.dll", opts.assembly_name);
    let mut res = Resolution::new(Module::new(dll_name));
    res.assembly = Some(Assembly::new(opts.assembly_name.clone()));

    let sys_runtime = res.push_assembly_reference(ExternalAssemblyReference::new("System.Runtime"));
    let object_ref = res.push_type_reference(type_ref! { System.Object in #sys_runtime });

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
        method_ref! { static string #builtins_type::StrConcat(string, string) },
    );

    let pymath_ref = res.push_type_reference(type_ref! { RyuPy.Runtime.PyMath in #ryupy_runtime });
    let pymath_type: MethodType = BaseType::class(pymath_ref).into();

    let floor_div_i64 = res
        .push_method_reference(method_ref! { static long @pymath_type::FloorDivI64(long, long) });
    let mod_i64 =
        res.push_method_reference(method_ref! { static long @pymath_type::ModI64(long, long) });
    let pow_i64 =
        res.push_method_reference(method_ref! { static long @pymath_type::PowI64(long, long) });

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
        floor_div_f64,
        mod_f64,
        pow_f64,
        f64_to_i64,
        abs_i64,
        abs_f64,
    };

    let module_class = res.push_type_definition(TypeDefinition::new(
        Some("RyuPy.Generated".into()),
        "Module",
    ));
    res[module_class].set_extends(object_ref);
    res[module_class].flags.abstract_type = true;
    res[module_class].flags.sealed = true;
    res[module_class].flags.accessibility = TypeAccessibility::Public;

    // 第一遍：为所有函数分配 MethodIndex
    let mut fn_indices: FxHashMap<FnId, MethodIndex> = FxHashMap::default();
    for (fn_id, func) in program.functions.iter() {
        let method_idx = if fn_id == program.entry {
            let main_method = res.push_method(
                module_class,
                Method::new(
                    Accessibility::Public,
                    msig! { static void (string[]) },
                    "Main",
                    None,
                ),
            );
            res.set_entry_point(main_method);
            main_method
        } else {
            let params: Vec<_> = func
                .params
                .iter()
                .map(|&p| crate::types::mir_type_to_parameter(&func.body.locals[p].ty).unwrap())
                .collect();
            let ret = crate::types::mir_type_to_return_type(&func.ret);
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
        let compiled = lower_function(func, &runtime_refs, &fn_indices);
        let mut method_body = body::Method::with_locals(compiled.locals, compiled.instructions);
        method_body.header.maximum_stack_size = 64;
        res[method_idx].body = Some(method_body);
    }

    (res, module_class)
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
    Ok(out)
}
