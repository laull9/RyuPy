//! 普通类与内建异常基类解析；特殊原生构建协议由独立入口验证。
use crate::hir::{Class, ClassBase, Name};
use crate::scope::{GlobalScope, Resolution};
use ryupy_base::{Diagnostic, FileId, Ranged, Span};
use ryupy_syntax::ast;

/// 解析 `class X(Base):` 的基类。
pub(crate) fn parse_base(
    class_def: &ast::StmtClassDef,
    file: FileId,
    globals: &GlobalScope,
    classes: &la_arena::Arena<Class>,
    diags: &mut Vec<Diagnostic>,
) -> Option<ClassBase> {
    let Some(args) = &class_def.arguments else {
        return None;
    };
    if !args.keywords.is_empty() {
        diags.push(
            Diagnostic::error(
                Span::new(file, class_def.range()),
                "不支持类关键字参数（元类等属于暂缓能力）",
            )
            .with_code("E0100")
            .with_primary_label("不支持 metaclass / 其它类关键字"),
        );
        return None;
    }
    if args.args.len() > 1 {
        diags.push(
            Diagnostic::error(Span::new(file, class_def.range()), "暂不支持多重继承")
                .with_code("E0100")
                .with_primary_label("只支持一个基类"),
        );
        return None;
    }
    let base_expr = args.args.first()?;
    let ast::Expr::Name(base_name) = base_expr else {
        diags.push(
            Diagnostic::error(
                Span::new(file, base_expr.range()),
                "基类必须是已定义的类名或内建异常类型",
            )
            .with_code("E0100")
            .with_primary_label("不支持任意基类表达式"),
        );
        return None;
    };
    let name = Name::from(base_name.id.as_str());
    if let Some(resolution) = globals.names.get(&name) {
        return match resolution {
            Resolution::Class(id) => {
                // 基类必须在源码里已经定义：类体在 class 语句执行时求值，
                // 前向引用在 CPython 里同样会 NameError。
                let _ = &classes[*id];
                Some(ClassBase::User(*id))
            }
            _ => {
                diags.push(
                    Diagnostic::error(
                        Span::new(file, base_name.range),
                        format!("`{name}` 不是可继承的类"),
                    )
                    .with_code("E0100")
                    .with_primary_label("基类必须是类"),
                );
                None
            }
        };
    }
    // `class X(object)` 与不写基类等价；显式写出来是常见写法，不应被当成未知基类。
    if base_name.id.as_str() == "object" {
        return None;
    }
    match crate::scope::resolve_builtin(base_name.id.as_str()) {
        Some(crate::hir::Builtin::Exception(type_name)) => Some(ClassBase::Builtin(type_name)),
        _ => {
            diags.push(
                Diagnostic::error(
                    Span::new(file, base_name.range),
                    format!("未知基类 `{name}`"),
                )
                .with_code("E0100")
                .with_primary_label("基类必须是用户类或内建异常类"),
            );
            None
        }
    }
}
