//! 组装并序列化程序集。
//!
//! 生成的程序集结构：
//!
//! ```text
//! .assembly <name>
//! .assembly extern System.Runtime
//! .assembly extern RyuPy.Runtime
//! .class public abstract sealed RyuPy.Generated.Module      // 所有顶层函数都是它的静态方法
//!     .method public static void Main(string[] args) { .entrypoint ... }
//!     .method public static int64 fib(int64 n) { ... }
//! .class public RyuPy.Generated.<DataclassName>               // 每个 @dataclass 一个
//! ```

use dotnetdll::prelude::*;
use ryupy_mir::Program;

use crate::stackify::{CompiledBody, RuntimeRefs, lower_function};
use crate::{CodegenError, EmitOptions};

pub fn build_resolution(
    program: &Program,
    opts: &EmitOptions,
) -> (Resolution<'static>, CompiledBody) {
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
    let write_newline =
        res.push_method_reference(method_ref! { static void @builtins_type::WriteNewline() });
    let write_sep =
        res.push_method_reference(method_ref! { static void #builtins_type::WriteSep() });

    let runtime_refs = RuntimeRefs {
        write_i64,
        write_newline,
        write_sep,
    };

    let module_class = res.push_type_definition(TypeDefinition::new(
        Some("RyuPy.Generated".into()),
        "Module",
    ));
    res[module_class].set_extends(object_ref);
    res[module_class].flags.abstract_type = true;
    res[module_class].flags.sealed = true;
    res[module_class].flags.accessibility = TypeAccessibility::Public;

    let entry_func = &program.functions[program.entry];
    let compiled_body = lower_function(entry_func, &runtime_refs);

    let main_method = res.push_method(
        module_class,
        Method::new(
            Accessibility::Public,
            msig! { static void (string[]) },
            "Main",
            Some(body::Method {
                data_sections: Vec::new(),
                ..body::Method::with_locals(
                    compiled_body.locals.clone(),
                    compiled_body.instructions.clone(),
                )
            }),
        ),
    );
    res.set_entry_point(main_method);

    (res, compiled_body)
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
    let (res, compiled) = build_resolution(program, opts);
    let mut out = String::new();
    out.push_str(&format!(".assembly {}\n{{\n}}\n", opts.assembly_name));
    out.push_str(".assembly extern System.Runtime\n{\n}\n");
    out.push_str(".assembly extern RyuPy.Runtime\n{\n}\n\n");
    out.push_str(
        ".class public abstract sealed auto ansi beforefieldinit RyuPy.Generated.Module\n",
    );
    out.push_str("    extends [System.Runtime]System.Object\n{\n");
    out.push_str("    .method public static void Main(string[] args) cil managed\n    {\n");
    out.push_str("        .entrypoint\n");
    if !compiled.locals.is_empty() {
        out.push_str("        .locals init (\n");
        for (i, loc) in compiled.locals.iter().enumerate() {
            let comma = if i + 1 < compiled.locals.len() {
                ","
            } else {
                ""
            };
            out.push_str(&format!("            [{i}] {}{comma}\n", loc.show(&res)));
        }
        out.push_str("        )\n");
    }
    for (i, ins) in compiled.instructions.iter().enumerate() {
        out.push_str(&format!("        IL_{i:04x}: {}\n", ins.show(&res)));
    }
    out.push_str("    }\n");
    out.push_str("}\n");
    Ok(out)
}
