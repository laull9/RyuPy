//! 入口包装器、序列化与 CIL 转储。
//!
//! `Main` 包装器把未捕获异常渲染成 Python traceback 后按规则退出；
//! `emit` / `dump` 是对外产物出口。

use dotnetdll::prelude::*;
use ryupy_mir::LinkedProgram;

use crate::assembly::build_resolution;
use crate::{CodegenError, EmitOptions};

/// 入口包装器：`try { __main(); return 0; } catch (PyBaseException e) { Report(e); return 1; }`。
///
/// 这是 M2.2 的第一块：**未捕获异常必须按 Python 的方式退出**（stderr 上 traceback、
/// 最后一行 `Type: message`、退出码 1），而不是让 CLR 打印 "Unhandled exception" 后
/// 以 134 退出。抛出的异常对象由 catch 子句压栈，直接作为 `Report` 的实参。
#[allow(clippy::too_many_arguments)]
pub(crate) fn emit_entry_wrapper(
    res: &mut Resolution<'static>,
    wrapper: MethodIndex,
    body_method: MethodIndex,
    report_ref: MethodRefIndex,
    report_clr_ref: MethodRefIndex,
    exception_type: MethodType,
    clr_exception_type: MethodType,
    system_exit_type: MethodType,
    set_file: MethodRefIndex,
    initialize_system: MethodRefIndex,
    system_exit_code: MethodRefIndex,
    exit_flush: MethodRefIndex,
    init_functions: MethodIndex,
    init_class_factories: MethodIndex,
    source_name: &str,
    source_path: &str,
) {
    // 指令布局（分支目标是**指令下标**）：
    //   0: ldstr source_name
    //   1: call SetFile
    //   2: ldstr source_path           ┐ try
    //   3: ldarg.0                    │ 真实 Main(string[] args)，不解析宿主命令行
    //   4: call PySystem.Initialize    │
    //   5: call __init_functions       │ 构造函数对象与默认值数组
    //   6: call __init_class_factories │ 用户异常类的 CLR 工厂委托
    //   7: call __main                │
    //   8: leave 20                   ┘
    //   9: call SystemExitCode         ┐ catch PySystemExit
    //  10: stloc.0                     │
    //  11: leave 20                    ┘
    //  12: call Report                 ┐ catch PyBaseException
    //  13: ldc.i4.1                    │
    //  14: stloc.0                     │
    //  15: leave 20                    ┘
    //  16: call ReportClr              ┐ catch System.Exception（兜底）
    //  17: ldc.i4.1                    │
    //  18: stloc.0                     │
    //  19: leave 20                    ┘
    //  20: ldloc.0                     ← 所有路径汇合于此，原退出码作为实参
    //  21: call FlushForExit           ← 刷新失败由运行时报告并改为120
    //  22: ret
    //
    // 系统初始化也位于保护区中，初始化失败与普通Python异常共享退出规则。
    // 处理区必须以 leave 离开、不能直接 ret，否则 CLR 判为非法方法体
    // （InvalidProgramException，且堆栈里看不出是哪一条指令）。
    let instructions = vec![
        Instruction::load_string(source_name),
        Instruction::call(set_file),
        Instruction::load_string(source_path),
        Instruction::LoadArgument(0),
        Instruction::call(initialize_system),
        Instruction::call(init_functions),
        Instruction::call(init_class_factories),
        Instruction::call(body_method),
        Instruction::Leave(20),
        // SystemExit：算出退出码而不是打印 traceback
        Instruction::call(system_exit_code),
        Instruction::StoreLocal(0),
        Instruction::Leave(20),
        // 其余未捕获 Python 异常：打印 traceback 并退出 1
        Instruction::call(report_ref),
        Instruction::LoadConstantInt32(1),
        Instruction::StoreLocal(0),
        Instruction::Leave(20),
        // 兜底：CLR 异常（静态 i64 溢出等）映射为 Python 错误后报告
        Instruction::call(report_clr_ref),
        Instruction::LoadConstantInt32(1),
        Instruction::StoreLocal(0),
        Instruction::Leave(20),
        // 汇合点：运行时刷新当前stdout，失败时按Python规则返回120，EH索引不变。
        Instruction::LoadLocal(0),
        Instruction::call(exit_flush),
        Instruction::Return,
    ];
    // 成功路径直接返回槽 0：with_locals 在真实 PE 方法头启用清零，
    // 不能仅依赖转储文本的 .locals init 或某个平台偶然为零的栈内容。
    let mut method_body = body::Method::with_locals(
        vec![LocalVariable::new(MethodType::from(BaseType::Int32))],
        instructions,
    );
    method_body.header.maximum_stack_size = 8;
    method_body.data_sections = vec![body::DataSection::ExceptionHandlers(vec![
        body::Exception {
            kind: body::ExceptionKind::TypedException(system_exit_type),
            try_offset: 2,
            try_length: 7,
            handler_offset: 9,
            handler_length: 3,
        },
        body::Exception {
            kind: body::ExceptionKind::TypedException(exception_type),
            try_offset: 2,
            try_length: 7,
            handler_offset: 12,
            handler_length: 4,
        },
        body::Exception {
            kind: body::ExceptionKind::TypedException(clr_exception_type),
            try_offset: 2,
            try_length: 7,
            handler_offset: 16,
            handler_length: 4,
        },
    ])];
    res[wrapper].body = Some(method_body);
}

pub fn emit(program: &LinkedProgram, opts: &EmitOptions) -> Result<Vec<u8>, CodegenError> {
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

pub fn dump(program: &LinkedProgram, opts: &EmitOptions) -> Result<String, CodegenError> {
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
                let init = if body.header.initialize_locals {
                    " init"
                } else {
                    ""
                };
                out.push_str(&format!("        .locals{init} (\n"));
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
        // 普通 class / 用户异常类由运行时对象承载，转储只展示 dataclass 的 CLR 布局。
        if !cls.is_dataclass {
            continue;
        }
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
