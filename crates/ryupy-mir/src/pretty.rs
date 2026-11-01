//! MIR 文本转储，供 `ryupy dump --emit mir` 与 insta 快照使用。
//!
//! 采用类似 rustc MIR 的紧凑易读格式：
//! ```text
//! fn add(_0: i64, _1: i64) -> i64 {
//!     let _0: i64; // a
//!     let _1: i64; // b
//!     let _2: i64;
//!
//!     bb0: {
//!         _2 = Add(_0, _1);
//!         return _2;
//!     }
//! }
//! ```

use std::fmt::Write;

use crate::mir::{
    BasicBlock, Callee, Constant, Function, MirType, Operand, Program, Rvalue, Statement,
    Terminator,
};

pub fn dump(program: &Program) -> String {
    let mut out = String::new();
    for (fn_id, func) in program.functions.iter() {
        if fn_id != program.functions.iter().next().unwrap().0 {
            out.push('\n');
        }
        dump_function(&mut out, func, program);
    }
    out
}

fn dump_function(out: &mut String, func: &Function, program: &Program) {
    // 1. 函数签名
    let params_str = func
        .params
        .iter()
        .map(|&p| {
            let ty_str = format_type(&func.body.locals[p].ty);
            format!("_{}: {}", p.into_raw(), ty_str)
        })
        .collect::<Vec<_>>()
        .join(", ");
    let ret_str = format_type(&func.ret);
    writeln!(out, "fn {}({}) -> {} {{", func.name, params_str, ret_str).unwrap();

    // 2. 局部变量声明
    for (loc_id, loc) in func.body.locals.iter() {
        let ty_str = format_type(&loc.ty);
        if let Some(name) = &loc.name {
            writeln!(
                out,
                "    let _{}: {}; // {}",
                loc_id.into_raw(),
                ty_str,
                name
            )
            .unwrap();
        } else {
            writeln!(out, "    let _{}: {};", loc_id.into_raw(), ty_str).unwrap();
        }
    }
    if !func.body.locals.is_empty() {
        writeln!(out).unwrap();
    }

    // 3. 异常区域
    for (i, region) in func.body.regions.iter().enumerate() {
        let kind = match &region.kind {
            crate::mir::RegionKind::Catch { type_names } => {
                format!("catch {}", type_names.join(" | "))
            }
            crate::mir::RegionKind::CatchAll => "catch-all".to_string(),
            crate::mir::RegionKind::FinallyGuard => "finally-guard".to_string(),
        };
        writeln!(
            out,
            "    region r{i}: [{}, +{}) {kind} handler->bb{}",
            region.try_start.into_raw(),
            region.try_len,
            region.handler.into_raw()
        )
        .unwrap();
    }
    if !func.body.regions.is_empty() {
        writeln!(out).unwrap();
    }

    // 4. 基本块
    for (bb_id, bb) in func.body.blocks.iter() {
        dump_basic_block(out, bb_id, bb, func, program);
    }

    writeln!(out, "}}").unwrap();
}

fn dump_basic_block(
    out: &mut String,
    bb_id: crate::mir::BlockId,
    bb: &BasicBlock,
    _func: &Function,
    program: &Program,
) {
    writeln!(out, "    bb{}: {{", bb_id.into_raw()).unwrap();
    for stmt in &bb.stmts {
        write!(out, "        ").unwrap();
        match stmt {
            Statement::Assign { dest, value } => {
                writeln!(
                    out,
                    "_{} = {};",
                    dest.into_raw(),
                    format_rvalue(value, program)
                )
                .unwrap();
            }
            Statement::Eval(rval) => {
                writeln!(out, "{};", format_rvalue(rval, program)).unwrap();
            }
            Statement::Line(line) => {
                writeln!(out, "// line {line}").unwrap();
            }
            Statement::SetField { obj, field, value } => {
                writeln!(
                    out,
                    "{}.{} = {};",
                    format_operand(obj),
                    field,
                    format_operand(value)
                )
                .unwrap();
            }
            Statement::SetGlobal { dest, value } => {
                writeln!(out, "g{} = {};", dest.into_raw(), format_operand(value)).unwrap();
            }
        }
    }

    write!(out, "        ").unwrap();
    match &bb.terminator {
        Terminator::Suspend { value, resume } => {
            writeln!(
                out,
                "yield {} -> bb{};",
                format_operand(value),
                resume.into_raw()
            )
            .unwrap();
        }
        Terminator::Goto(target) => {
            writeln!(out, "goto -> bb{};", target.into_raw()).unwrap();
        }
        Terminator::Branch { cond, then_, else_ } => {
            writeln!(
                out,
                "branch {} -> [then: bb{}, else: bb{}];",
                format_operand(cond),
                then_.into_raw(),
                else_.into_raw()
            )
            .unwrap();
        }
        Terminator::Return(None) => {
            writeln!(out, "return;").unwrap();
        }
        Terminator::Return(Some(op)) => {
            writeln!(out, "return {};", format_operand(op)).unwrap();
        }
        Terminator::Raise { value, cause } => {
            let value_str = value.as_ref().map(format_operand).unwrap_or_default();
            let cause_str = cause
                .as_ref()
                .map(|c| format!(" from {}", format_operand(c)))
                .unwrap_or_default();
            writeln!(out, "raise {value_str}{cause_str};").unwrap();
        }
        Terminator::Unreachable => {
            writeln!(out, "unreachable;").unwrap();
        }
    }
    writeln!(out, "    }}").unwrap();
}

fn format_type(ty: &MirType) -> String {
    match ty {
        MirType::I64 => "i64".to_string(),
        MirType::F64 => "f64".to_string(),
        MirType::Bool => "bool".to_string(),
        MirType::Str => "str".to_string(),
        MirType::Unit => "()".to_string(),
        MirType::Class(id) => format!("Class({})", id.into_raw()),
        MirType::Dynamic => "dyn".to_string(),
    }
}

fn format_operand(op: &Operand) -> String {
    match op {
        Operand::Local(loc) => format!("_{}", loc.into_raw()),
        Operand::Global(global) => format!("global@{}", global.into_raw()),
        Operand::Array(items) => {
            let inner: Vec<String> = items.iter().map(format_operand).collect();
            format!("[{}]", inner.join(", "))
        }
        Operand::Const(c) => match c {
            Constant::I64(v) => format!("const {v}_i64"),
            Constant::F64(v) => format!("const {v}_f64"),
            Constant::Bool(b) => format!("const {b}"),
            Constant::Str(s) => format!("const \"{s}\""),
            Constant::BigInt(s) => format!("const {s}_bigint"),
            Constant::None => "None".to_string(),
            Constant::Null => "null".to_string(),
        },
    }
}

fn format_rvalue(rval: &Rvalue, program: &Program) -> String {
    match rval {
        Rvalue::Use(op) => format_operand(op),
        Rvalue::MakeGenerator { resume, frame } => format!(
            "MakeGenerator({}, {})",
            program.functions[*resume].name,
            frame
                .iter()
                .map(format_operand)
                .collect::<Vec<_>>()
                .join(", ")
        ),
        Rvalue::MakeFunction {
            func,
            env,
            defaults,
        } => {
            let name = program
                .functions
                .iter()
                .find(|(i, _)| *i == *func)
                .map(|(_, f)| f.name.clone())
                .unwrap_or_else(|| format!("fn_{}", func.into_raw()));
            let env_str = env
                .iter()
                .map(format_operand)
                .collect::<Vec<_>>()
                .join(", ");
            let defaults_str = defaults
                .iter()
                .map(format_operand)
                .collect::<Vec<_>>()
                .join(", ");
            format!("MakeFunction({name}; env=[{env_str}]; defaults=[{defaults_str}])")
        }
        Rvalue::Binary { op, lhs, rhs } => {
            format!("{:?}({}, {})", op, format_operand(lhs), format_operand(rhs))
        }
        Rvalue::Unary { op, operand } => {
            format!("{:?}({})", op, format_operand(operand))
        }
        Rvalue::Cast { kind, operand } => {
            format!("Cast({:?}, {})", kind, format_operand(operand))
        }
        Rvalue::Call { callee, args } => {
            let callee_str = match callee {
                Callee::Fn(id) => program
                    .functions
                    .iter()
                    .find(|(i, _)| *i == *id)
                    .map(|(_, f)| f.name.clone())
                    .unwrap_or_else(|| format!("fn_{}", id.into_raw())),
                Callee::Runtime(rt) => format!("RuntimeFn::{rt:?}"),
            };
            let args_str = args
                .iter()
                .map(format_operand)
                .collect::<Vec<_>>()
                .join(", ");
            format!("{callee_str}({args_str})")
        }
        Rvalue::NewObject { class, args } => {
            let args_str = args
                .iter()
                .map(format_operand)
                .collect::<Vec<_>>()
                .join(", ");
            format!("New(Class({}), [{}])", class.into_raw(), args_str)
        }
        Rvalue::GetField { obj, field } => {
            format!("{}.{}", format_operand(obj), field)
        }
        Rvalue::Box { value, from } => {
            format!("box<{:?}>({})", from, format_operand(value))
        }
        Rvalue::Unbox { value, to } => {
            format!("unbox<{:?}>({})", to, format_operand(value))
        }
        Rvalue::DynBinary {
            op,
            lhs,
            rhs,
            inplace,
        } => format!(
            "dyn.{:?}{}({}, {})",
            op,
            if *inplace { ".inplace" } else { "" },
            format_operand(lhs),
            format_operand(rhs)
        ),
        Rvalue::DynCompare { op, lhs, rhs } => format!(
            "dyn.cmp.{:?}({}, {})",
            op,
            format_operand(lhs),
            format_operand(rhs)
        ),
        Rvalue::DynUnary { op, operand } => {
            format!("dyn.{:?}({})", op, format_operand(operand))
        }
        Rvalue::DynTruthy { value } => format!("truthy({})", format_operand(value)),
        Rvalue::CallWithArgs {
            callee,
            prefix,
            args,
        } => {
            let prefix_str = prefix
                .iter()
                .map(format_operand)
                .collect::<Vec<_>>()
                .join(", ");
            let args_str = args
                .iter()
                .map(format_operand)
                .collect::<Vec<_>>()
                .join(", ");
            let sep = if prefix_str.is_empty() { "" } else { ", " };
            format!("{callee:?}({prefix_str}{sep}[{args_str}])")
        }
        Rvalue::DynCallValue { callee, args } => {
            let args_str = args
                .iter()
                .map(format_operand)
                .collect::<Vec<_>>()
                .join(", ");
            format!("dyn.call({}, [{args_str}])", format_operand(callee))
        }
        Rvalue::DynCallMethod { obj, name, args } => {
            let args_str = args
                .iter()
                .map(format_operand)
                .collect::<Vec<_>>()
                .join(", ");
            format!(
                "dyn.method({}, \"{name}\", [{args_str}])",
                format_operand(obj)
            )
        }
        Rvalue::CatchException => "caught_exception".to_string(),
    }
}
