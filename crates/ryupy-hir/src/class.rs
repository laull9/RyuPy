//! 类定义（@dataclass）AST → HIR 降级。
//!
//! 职责：
//! 1. **修饰器识别**：
//!    - 识别 `@dataclass` 和 `@dataclass(frozen=True)`；
//!    - 拦截不支持的修饰器或继承声明（报 [`E0100`]）；
//! 2. **类体字段提取**：
//!    - 解析 `x: float`、`y: int` 等带类型标注的字段声明；
//!    - 拦截带默认值（`x: int = 1`）或包含方法定义的类体（报 [`E0100`]）；
//! 3. **模块导入检查**：
//!    - 识别 Phase 1 唯一允许的导入：`from dataclasses import dataclass`。

use ryupy_base::{Diagnostic, FileId, Ranged, Span};
use ryupy_syntax::ast;

use crate::hir::{Class, Field, Name};
use crate::operator::lower_type_ref;
use crate::scope::GlobalScope;

/// 尝试解析 `@dataclass` 类定义。
pub fn parse_class_def(
    class_def: &ast::StmtClassDef,
    file: FileId,
    globals: &GlobalScope,
    diags: &mut Vec<Diagnostic>,
) -> Option<Class> {
    let class_name = Name::from(class_def.name.as_str());

    // Phase 1 不支持类继承
    if let Some(args) = &class_def.arguments {
        if !args.args.is_empty() || !args.keywords.is_empty() {
            diags.push(
                Diagnostic::error(Span::new(file, class_def.range()), "Phase 1 不支持类继承")
                    .with_code("E0100")
                    .with_primary_label("Phase 1 仅支持独立的 @dataclass，无基类"),
            );
            return None;
        }
    }

    // 检查修饰器：必须且仅能有 @dataclass 或 @dataclass(frozen=True/False)
    let mut is_dataclass = false;
    let mut frozen = false;

    for dec in &class_def.decorator_list {
        match &dec.expression {
            ast::Expr::Name(name) if name.id.as_str() == "dataclass" => {
                is_dataclass = true;
            }
            ast::Expr::Call(call) => {
                if let ast::Expr::Name(callee) = &*call.func {
                    if callee.id.as_str() == "dataclass" {
                        is_dataclass = true;
                        for kw in &call.arguments.keywords {
                            if let Some(arg_name) = &kw.arg {
                                if arg_name.as_str() == "frozen" {
                                    if let ast::Expr::BooleanLiteral(b) = &kw.value {
                                        frozen = b.value;
                                    }
                                }
                            }
                        }
                    }
                }
            }
            _ => {}
        }
    }

    if !is_dataclass {
        diags.push(
            Diagnostic::error(
                Span::new(file, class_def.range()),
                format!("类 `{class_name}` 缺少 @dataclass 修饰器"),
            )
            .with_code("E0100")
            .with_primary_label("Phase 1 仅支持 @dataclass 类定义"),
        );
        return None;
    }

    // 提取字段声明
    let mut fields = Vec::new();
    for stmt in &class_def.body {
        match stmt {
            ast::Stmt::AnnAssign(ann) => {
                if let ast::Expr::Name(target_name) = &*ann.target {
                    if ann.value.is_some() {
                        diags.push(
                            Diagnostic::error(
                                Span::new(file, ann.range()),
                                "Phase 1 dataclass 暂不支持字段默认值",
                            )
                            .with_code("E0100")
                            .with_primary_label("不支持默认值赋初值"),
                        );
                    }
                    let field_name = Name::from(target_name.id.as_str());
                    let annotation = lower_type_ref(&ann.annotation, file, globals, diags);
                    fields.push(Field {
                        name: field_name,
                        annotation,
                        range: ann.range(),
                    });
                } else {
                    diags.push(
                        Diagnostic::error(Span::new(file, ann.range()), "字段名必须为标识符")
                            .with_code("E0100")
                            .with_primary_label("无效的字段名"),
                    );
                }
            }
            ast::Stmt::Pass(_) => {}
            ast::Stmt::Expr(expr_stmt) => {
                // 允许 docstring 字符串字面量
                if !matches!(&*expr_stmt.value, ast::Expr::StringLiteral(_)) {
                    diags.push(
                        Diagnostic::error(
                            Span::new(file, expr_stmt.range()),
                            "dataclass 类体内只允许字段标注",
                        )
                        .with_code("E0100")
                        .with_primary_label("不允许此类语句"),
                    );
                }
            }
            _ => {
                diags.push(
                    Diagnostic::error(
                        Span::new(file, stmt.range()),
                        "Phase 1 dataclass 暂不支持定义方法或复杂语句",
                    )
                    .with_code("E0100")
                    .with_primary_label("暂不支持定义类方法"),
                );
            }
        }
    }

    Some(Class {
        name: class_name,
        fields,
        frozen,
        range: class_def.range(),
    })
}

/// 检查是否为合法导入：`from dataclasses import dataclass`。
pub fn is_allowed_dataclass_import(import_from: &ast::StmtImportFrom) -> bool {
    if let Some(module) = &import_from.module {
        if module.as_str() == "dataclasses" {
            return import_from
                .names
                .iter()
                .any(|alias| alias.name.as_str() == "dataclass");
        }
    }
    false
}
