//! HIR 文本转储，供 `ryupy dump --emit hir` 与 insta 快照使用。

use std::fmt::Write;

use crate::hir::{
    AssignTarget, BinOp, Body, Builtin, ExprId, ExprKind, Function, FunctionKind, Literal, Module,
    StmtKind, TypeRef,
};

pub fn dump(module: &Module) -> String {
    let mut out = String::new();
    for (_fn_id, func) in module.functions.iter() {
        dump_func(&mut out, module, func);
        out.push('\n');
    }
    out
}

fn dump_func(out: &mut String, module: &Module, func: &Function) {
    let kind_str = match func.kind {
        FunctionKind::User => "def",
        FunctionKind::ModuleMain => "def [main]",
    };
    write!(out, "{kind_str} {}(", func.name).unwrap();
    for (i, p) in func.params.iter().enumerate() {
        if i > 0 {
            out.push_str(", ");
        }
        let loc = &func.body.locals[p.local];
        write!(out, "{}", loc.name).unwrap();
        if let Some(ann) = &p.annotation {
            write!(out, ": {}", dump_type_ref(ann)).unwrap();
        }
    }
    out.push(')');
    if let Some(ret) = &func.ret {
        write!(out, " -> {}", dump_type_ref(ret)).unwrap();
    }
    out.push_str(":\n");

    if func.body.root.is_empty() {
        out.push_str("    pass\n");
    } else {
        for &stmt_id in &func.body.root {
            dump_stmt(out, 1, module, &func.body, stmt_id);
        }
    }
}

fn dump_stmt(
    out: &mut String,
    indent: usize,
    module: &Module,
    body: &Body,
    stmt_id: crate::hir::StmtId,
) {
    let pad = "    ".repeat(indent);
    let stmt = &body.stmts[stmt_id];
    match &stmt.kind {
        StmtKind::Expr(e) => {
            writeln!(out, "{pad}{}", dump_expr(module, body, *e)).unwrap();
        }
        StmtKind::Assign { target, value } => {
            let target_str = match target {
                AssignTarget::Local(loc_id) => body.locals[*loc_id].name.as_str().to_string(),
                AssignTarget::Attribute { base, name } => {
                    format!("{}.{}", dump_expr(module, body, *base), name)
                }
            };
            writeln!(
                out,
                "{pad}{target_str} = {}",
                dump_expr(module, body, *value)
            )
            .unwrap();
        }
        StmtKind::Return(opt_e) => {
            if let Some(e) = opt_e {
                writeln!(out, "{pad}return {}", dump_expr(module, body, *e)).unwrap();
            } else {
                writeln!(out, "{pad}return").unwrap();
            }
        }
        StmtKind::Pass => {
            writeln!(out, "{pad}pass").unwrap();
        }
        StmtKind::If { cond, then_, else_ } => {
            writeln!(out, "{pad}if {}:", dump_expr(module, body, *cond)).unwrap();
            for &s in then_ {
                dump_stmt(out, indent + 1, module, body, s);
            }
            if !else_.is_empty() {
                writeln!(out, "{pad}else:").unwrap();
                for &s in else_ {
                    dump_stmt(out, indent + 1, module, body, s);
                }
            }
        }
        StmtKind::While { cond, body: stmts } => {
            writeln!(out, "{pad}while {}:", dump_expr(module, body, *cond)).unwrap();
            for &s in stmts {
                dump_stmt(out, indent + 1, module, body, s);
            }
        }
        StmtKind::ForRange {
            var,
            start,
            stop,
            step,
            body: stmts,
        } => {
            let var_name = &body.locals[*var].name;
            let step_str = step.map_or(String::new(), |s| {
                format!(", {}", dump_expr(module, body, s))
            });
            writeln!(
                out,
                "{pad}for {var_name} in range({}, {}{step_str}):",
                dump_expr(module, body, *start),
                dump_expr(module, body, *stop)
            )
            .unwrap();
            for &s in stmts {
                dump_stmt(out, indent + 1, module, body, s);
            }
        }
        StmtKind::Break => writeln!(out, "{pad}break").unwrap(),
        StmtKind::Continue => writeln!(out, "{pad}continue").unwrap(),
    }
}

fn dump_expr(module: &Module, body: &Body, expr_id: ExprId) -> String {
    let expr = &body.exprs[expr_id];
    match &expr.kind {
        ExprKind::Missing => "<missing>".to_string(),
        ExprKind::Literal(lit) => match lit {
            Literal::Int(v) => v.to_string(),
            Literal::Float(v) => format!("{v:?}"),
            Literal::Bool(b) => (if *b { "True" } else { "False" }).to_string(),
            Literal::Str(s) => format!("{s:?}"),
            Literal::None => "None".to_string(),
        },
        ExprKind::Local(loc_id) => body.locals[*loc_id].name.as_str().to_string(),
        ExprKind::Func(fn_id) => module.functions[*fn_id].name.as_str().to_string(),
        ExprKind::Class(cls_id) => module.classes[*cls_id].name.as_str().to_string(),
        ExprKind::Builtin(b) => match b {
            Builtin::Print => "print".to_string(),
            Builtin::Int => "int".to_string(),
            Builtin::Float => "float".to_string(),
            Builtin::Abs => "abs".to_string(),
        },
        ExprKind::Unary { op, operand } => format!("{op:?}({})", dump_expr(module, body, *operand)),
        ExprKind::Binary { op, lhs, rhs } => {
            let op_str = match op {
                BinOp::Add => "+",
                BinOp::Sub => "-",
                BinOp::Mul => "*",
                BinOp::TrueDiv => "/",
                BinOp::FloorDiv => "//",
                BinOp::Mod => "%",
                BinOp::Pow => "**",
                BinOp::BitAnd => "&",
                BinOp::BitOr => "|",
                BinOp::BitXor => "^",
                BinOp::LShift => "<<",
                BinOp::RShift => ">>",
            };
            format!(
                "({} {op_str} {})",
                dump_expr(module, body, *lhs),
                dump_expr(module, body, *rhs)
            )
        }
        ExprKind::Compare { op, lhs, rhs } => {
            format!(
                "({} {op:?} {})",
                dump_expr(module, body, *lhs),
                dump_expr(module, body, *rhs)
            )
        }
        ExprKind::BoolOp { op, lhs, rhs } => {
            format!(
                "({} {op:?} {})",
                dump_expr(module, body, *lhs),
                dump_expr(module, body, *rhs)
            )
        }
        ExprKind::IfExp { cond, then_, else_ } => format!(
            "({} if {} else {})",
            dump_expr(module, body, *then_),
            dump_expr(module, body, *cond),
            dump_expr(module, body, *else_)
        ),
        ExprKind::Call { callee, args } => {
            let callee_str = dump_expr(module, body, *callee);
            let args_str: Vec<String> = args.iter().map(|&a| dump_expr(module, body, a)).collect();
            format!("{callee_str}({})", args_str.join(", "))
        }
        ExprKind::Attribute { base, name } => format!("{}.{name}", dump_expr(module, body, *base)),
    }
}

fn dump_type_ref(ty: &TypeRef) -> &'static str {
    match ty {
        TypeRef::Int => "int",
        TypeRef::Float => "float",
        TypeRef::Bool => "bool",
        TypeRef::Str => "str",
        TypeRef::None => "None",
        TypeRef::Class(_) => "class",
        TypeRef::Error => "<error>",
    }
}
