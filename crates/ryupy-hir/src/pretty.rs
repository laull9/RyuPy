//! HIR 文本转储，供 `ryupy dump --emit hir` 与 insta 快照使用。

use std::fmt::Write;

use crate::hir::{
    AssignTarget, BinOp, Body, Builtin, CallArg, ExprId, ExprKind, Function, FunctionKind, Literal,
    Module, ParamKind, StmtKind, TypeRef,
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
        FunctionKind::Nested => "def [nested]",
        FunctionKind::ModuleMain => "def [main]",
    };
    write!(out, "{kind_str} {}(", func.name).unwrap();
    for (i, p) in func.params.iter().enumerate() {
        if i > 0 {
            out.push_str(", ");
        }
        match p.kind {
            ParamKind::VarPositional => out.push('*'),
            ParamKind::VarKeyword => out.push_str("**"),
            ParamKind::PositionalOnly => {}
            ParamKind::PositionalOrKeyword | ParamKind::KeywordOnly => {}
        }
        let loc = &func.body.locals[p.local];
        write!(out, "{}", loc.name).unwrap();
        if let Some(ann) = &p.annotation {
            write!(out, ": {}", dump_type_ref(ann)).unwrap();
        }
        if p.default.is_some() {
            out.push_str("=...");
        }
    }
    out.push(')');
    if let Some(ret) = &func.ret {
        write!(out, " -> {}", dump_type_ref(ret)).unwrap();
    }
    out.push_str(":\n");
    if !func.closure.cells.is_empty() || !func.closure.free.is_empty() {
        writeln!(
            out,
            "    # cells={:?}, free={:?}",
            func.closure.cells, func.closure.free
        )
        .unwrap();
    }

    if func.body.root.is_empty() {
        out.push_str("    pass\n");
    } else {
        for &stmt_id in &func.body.root {
            dump_stmt(out, 1, module, &func.body, stmt_id);
        }
    }
}

/// 循环的 `else` 子句；为空时不输出。
fn dump_loop_else(
    out: &mut String,
    indent: usize,
    module: &Module,
    body: &Body,
    orelse: &[crate::hir::StmtId],
) {
    if orelse.is_empty() {
        return;
    }
    let pad = "    ".repeat(indent);
    writeln!(out, "{pad}else:").unwrap();
    for &s in orelse {
        dump_stmt(out, indent + 1, module, body, s);
    }
}

/// 绑定目标的显示名：局部变量用局部名，模块变量加 `global.` 前缀以便区分。
fn binding_name(module: &Module, body: &Body, binding: crate::hir::Binding) -> String {
    match binding {
        crate::hir::Binding::Local(local) => body.locals[local].name.as_str().to_string(),
        crate::hir::Binding::ModuleVar(var) => format!("global.{}", module.vars[var].name),
        crate::hir::Binding::Free(idx) => format!("cell[{idx}]"),
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
        StmtKind::Import {
            module: imported,
            bind,
            bind_root,
        } => {
            if *bind_root {
                writeln!(out, "{pad}import {imported}").unwrap();
            } else {
                writeln!(
                    out,
                    "{pad}import {imported} as {}",
                    binding_name(module, body, crate::hir::Binding::ModuleVar(*bind))
                )
                .unwrap();
            }
        }
        StmtKind::ImportFrom {
            module: imported,
            names,
        } => {
            let items = names
                .iter()
                .map(|(name, bind)| {
                    let bind_name =
                        binding_name(module, body, crate::hir::Binding::ModuleVar(*bind));
                    if *name == bind_name {
                        name.clone()
                    } else {
                        format!("{name} as {bind_name}")
                    }
                })
                .collect::<Vec<_>>()
                .join(", ");
            writeln!(out, "{pad}from {imported} import {items}").unwrap();
        }
        StmtKind::Assign { target, value } => {
            let target_str = match target {
                AssignTarget::Local(loc_id) => body.locals[*loc_id].name.as_str().to_string(),
                AssignTarget::ModuleVar(var) => format!("global.{}", module.vars[*var].name),
                AssignTarget::Free(idx) => format!("cell[{idx}]"),
                AssignTarget::Attribute { base, name } => {
                    format!("{}.{}", dump_expr(module, body, *base), name)
                }
                AssignTarget::Subscript { base, index } => {
                    format!(
                        "{}[{}]",
                        dump_expr(module, body, *base),
                        dump_expr(module, body, *index)
                    )
                }
                AssignTarget::Sequence(items) => items
                    .iter()
                    .map(|item| match item {
                        crate::hir::SequenceItem::One(binding) => {
                            binding_name(module, body, *binding)
                        }
                        crate::hir::SequenceItem::Rest(binding) => {
                            format!("*{}", binding_name(module, body, *binding))
                        }
                    })
                    .collect::<Vec<_>>()
                    .join(", "),
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
        StmtKind::Raise { value, cause } => {
            let _ = (value, cause);
            writeln!(out, "raise ...;").unwrap();
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
        StmtKind::While {
            cond,
            body: stmts,
            orelse,
        } => {
            writeln!(out, "{pad}while {}:", dump_expr(module, body, *cond)).unwrap();
            for &s in stmts {
                dump_stmt(out, indent + 1, module, body, s);
            }
            dump_loop_else(out, indent, module, body, orelse);
        }
        StmtKind::ForEach {
            var,
            iter,
            body: stmts,
            orelse,
        } => {
            writeln!(
                out,
                "{pad}for {} in {}:",
                binding_name(module, body, *var),
                dump_expr(module, body, *iter)
            )
            .unwrap();
            for &s in stmts {
                dump_stmt(out, indent + 1, module, body, s);
            }
            dump_loop_else(out, indent, module, body, orelse);
        }
        StmtKind::ForRange {
            var,
            start,
            stop,
            step,
            body: stmts,
            orelse,
        } => {
            let var_name = binding_name(module, body, *var);
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
            dump_loop_else(out, indent, module, body, orelse);
        }
        StmtKind::Delete(targets) => {
            let names: Vec<String> = targets
                .iter()
                .map(|e| dump_expr(module, body, *e))
                .collect();
            writeln!(out, "{pad}del {}", names.join(", ")).unwrap();
        }
        StmtKind::FunctionDefaults(id) => {
            writeln!(out, "{pad}defaults of {}", module.functions[*id].name).unwrap()
        }
        StmtKind::NestedDef { func, target } => {
            let target_str = match target {
                AssignTarget::Local(loc_id) => body.locals[*loc_id].name.as_str().to_string(),
                AssignTarget::ModuleVar(var) => format!("global.{}", module.vars[*var].name),
                AssignTarget::Free(idx) => format!("cell[{idx}]"),
                _ => "?".to_string(),
            };
            writeln!(
                out,
                "{pad}{} = def {} (nested)",
                target_str, module.functions[*func].name
            )
            .unwrap()
        }
        StmtKind::ClassDef(id) => {
            let class = &module.classes[*id];
            let base = match class.base {
                None => String::new(),
                Some(crate::hir::ClassBase::User(base_id)) => {
                    format!("({})", module.classes[base_id].name)
                }
                Some(crate::hir::ClassBase::Builtin(name)) => format!("({name})"),
            };
            writeln!(out, "{pad}class {}{base}:", class.name).unwrap();
            for item in &class.body {
                match &item.kind {
                    crate::hir::ClassItemKind::Attr { name, value } => {
                        writeln!(
                            out,
                            "{pad}    {} = {}",
                            name,
                            dump_expr(module, body, *value)
                        )
                        .unwrap();
                    }
                    crate::hir::ClassItemKind::Method { func, decorators } => {
                        let decorators: Vec<&str> = decorators
                            .iter()
                            .map(|d| match d {
                                crate::hir::Decorator::Property => "@property",
                                crate::hir::Decorator::StaticMethod => "@staticmethod",
                                crate::hir::Decorator::ClassMethod => "@classmethod",
                            })
                            .collect();
                        for decorator in decorators {
                            writeln!(out, "{pad}    {decorator}").unwrap();
                        }
                        writeln!(
                            out,
                            "{pad}    def {}(self, ...)",
                            module.functions[*func].name
                        )
                        .unwrap();
                    }
                    crate::hir::ClassItemKind::Expr(value) => {
                        writeln!(out, "{pad}    {}", dump_expr(module, body, *value)).unwrap();
                    }
                }
            }
        }
        StmtKind::Break => writeln!(out, "{pad}break").unwrap(),
        StmtKind::Continue => writeln!(out, "{pad}continue").unwrap(),
        StmtKind::Try {
            body: try_body,
            handlers,
            orelse,
            finalbody,
        } => {
            writeln!(out, "{pad}try:").unwrap();
            for &s in try_body {
                dump_stmt(out, indent + 1, module, body, s);
            }
            for &handler_id in handlers {
                let handler = &body.handlers[handler_id];
                let header = match &handler.types {
                    None => "except:".to_string(),
                    Some(types) => format!("except {}:", types.join(" | ")),
                };
                match handler.name {
                    Some(binding) => writeln!(
                        out,
                        "{pad}{} as {}:",
                        header.trim_end_matches(':'),
                        binding_name(module, body, binding)
                    )
                    .unwrap(),
                    None => writeln!(out, "{pad}{header}").unwrap(),
                }
                for &s in &handler.body {
                    dump_stmt(out, indent + 1, module, body, s);
                }
            }
            if !orelse.is_empty() {
                writeln!(out, "{pad}else:").unwrap();
                for &s in orelse {
                    dump_stmt(out, indent + 1, module, body, s);
                }
            }
            if !finalbody.is_empty() {
                writeln!(out, "{pad}finally:").unwrap();
                for &s in finalbody {
                    dump_stmt(out, indent + 1, module, body, s);
                }
            }
        }
    }
}

fn dump_list(module: &Module, body: &Body, items: &[ExprId]) -> String {
    items
        .iter()
        .map(|e| dump_expr(module, body, *e))
        .collect::<Vec<_>>()
        .join(", ")
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
        ExprKind::ModuleVar(var) => format!("global.{}", module.vars[*var].name),
        ExprKind::Free(idx) => format!("cell[{idx}]"),
        ExprKind::Lambda(func_id) => {
            format!("<lambda {}>", module.functions[*func_id].name)
        }
        ExprKind::Func(fn_id) => module.functions[*fn_id].name.as_str().to_string(),
        ExprKind::Class(cls_id) => module.classes[*cls_id].name.as_str().to_string(),
        ExprKind::Builtin(b) => match b {
            Builtin::Exception(name) => name.to_string(),
            Builtin::Print => "print".to_string(),
            Builtin::Int => "int".to_string(),
            Builtin::Float => "float".to_string(),
            Builtin::Abs => "abs".to_string(),
            Builtin::Str => "str".to_string(),
            Builtin::Type => "type".to_string(),
            Builtin::Dynamic(name) => name.to_string(),
        },
        ExprKind::List(items) => format!("[{}]", dump_list(module, body, items)),
        ExprKind::Tuple(items) => format!("({})", dump_list(module, body, items)),
        ExprKind::Set(items) => format!("{{{}}}", dump_list(module, body, items)),
        ExprKind::Dict(pairs) => {
            let parts: Vec<String> = pairs
                .iter()
                .map(|(k, v)| {
                    format!(
                        "{}: {}",
                        dump_expr(module, body, *k),
                        dump_expr(module, body, *v)
                    )
                })
                .collect();
            format!("{{{}}}", parts.join(", "))
        }
        ExprKind::Bytes(bytes) => format!("b{:?}", bytes),
        ExprKind::Subscript { base, index } => {
            format!(
                "{}[{}]",
                dump_expr(module, body, *base),
                dump_expr(module, body, *index)
            )
        }
        ExprKind::Slice { start, stop, step } => {
            let part =
                |e: &Option<ExprId>| e.map(|id| dump_expr(module, body, id)).unwrap_or_default();
            format!("{}:{}:{}", part(start), part(stop), part(step))
        }
        ExprKind::Starred(inner) => format!("*{}", dump_expr(module, body, *inner)),
        ExprKind::Unary { op, operand } => format!("{op:?}({})", dump_expr(module, body, *operand)),
        ExprKind::Binary {
            op,
            lhs,
            rhs,
            inplace,
        } => {
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
            // 增量赋值显示为 `op=`，与普通二元运算区分
            let suffix = if *inplace { "=" } else { "" };
            format!(
                "({} {op_str}{suffix} {})",
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
            let args_str: Vec<String> = args
                .iter()
                .map(|arg| match arg {
                    CallArg::Positional(id) => dump_expr(module, body, *id),
                    CallArg::Starred(id) => format!("*{}", dump_expr(module, body, *id)),
                    CallArg::Keyword(name, id) => {
                        format!("{name}={}", dump_expr(module, body, *id))
                    }
                    CallArg::DoubleStarred(id) => format!("**{}", dump_expr(module, body, *id)),
                })
                .collect();
            format!("{callee_str}({})", args_str.join(", "))
        }
        ExprKind::Attribute { base, name } => format!("{}.{name}", dump_expr(module, body, *base)),
        ExprKind::FString(parts) => {
            let inner: Vec<String> = parts
                .iter()
                .map(|part| match part {
                    crate::hir::FStringPart::Literal(text) => text.to_string(),
                    crate::hir::FStringPart::Value { value, .. } => {
                        format!("{{{}}}", dump_expr(module, body, *value))
                    }
                })
                .collect();
            format!("f\"{}\"", inner.join(""))
        }
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
