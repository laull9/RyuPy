//! dataclass 属性的静态读写检查，未知类型回到动态路径。

use crate::check::{FunctionChecker, module_type_ref_to_ty};
use crate::ty::Ty;
use ryupy_base::{Diagnostic, Span, TextRange};
use ryupy_hir::{ExprId, Name};

impl FunctionChecker<'_> {
    pub(crate) fn check_attribute_read(
        &mut self,
        base_ty: &Ty,
        range: TextRange,
        name: &Name,
    ) -> Ty {
        match base_ty {
            Ty::Class(cls_id) => {
                let cls = &self.module.classes[*cls_id];
                if cls.decorated_binding.is_some() || cls.runtime_dataclass {
                    return Ty::Dynamic;
                }
                if let Some(field) = cls.fields.iter().find(|f| f.name == *name) {
                    module_type_ref_to_ty(self.module, &field.annotation)
                } else {
                    self.diagnostics.push(
                        Diagnostic::error(
                            Span::new(self.file, range),
                            format!("类 `{}` 没有名为 `{}` 的属性", cls.name, name),
                        )
                        .with_code("E0203")
                        .with_primary_label("未定义的属性"),
                    );
                    Ty::Error
                }
            }
            Ty::Dynamic | Ty::Error => Ty::Dynamic,
            _ => {
                self.diagnostics.push(
                    Diagnostic::error(
                        Span::new(self.file, range),
                        format!("类型 `{base_ty:?}` 不支持属性访问 `{name}`"),
                    )
                    .with_code("E0203")
                    .with_primary_label("类型不支持属性访问"),
                );
                Ty::Error
            }
        }
    }

    pub(crate) fn check_attribute_assign(
        &mut self,
        base_ty: Ty,
        base: ExprId,
        name: Name,
        val_ty: Ty,
        range: TextRange,
    ) {
        match base_ty {
            Ty::Class(cls_id) => {
                let cls = &self.module.classes[cls_id];
                if cls.decorated_binding.is_some() || cls.runtime_dataclass {
                    self.soft_demote = true;
                    return;
                }
                if cls.frozen {
                    self.diagnostics.push(
                        Diagnostic::error(
                            Span::new(self.file, self.func.body.exprs[base].range),
                            format!(
                                "不能修改被 @dataclass(frozen=True) 修饰的 `{}` 实例属性",
                                cls.name
                            ),
                        )
                        .with_code("E0203")
                        .with_primary_label("frozen dataclass 实例不可变"),
                    );
                } else if let Some(field) = cls.fields.iter().find(|f| f.name == name) {
                    let expected_ty = module_type_ref_to_ty(self.module, &field.annotation);
                    if expected_ty != Ty::Error
                        && val_ty != Ty::Error
                        && expected_ty != val_ty
                        && !(expected_ty == Ty::Float && val_ty == Ty::Int)
                    {
                        // 字段类型不符：与实参一样交给运行时
                        self.soft_demote = true;
                    }
                } else {
                    self.diagnostics.push(
                        Diagnostic::error(
                            Span::new(self.file, range),
                            format!("类 `{}` 没有名为 `{}` 的属性", cls.name, name),
                        )
                        .with_code("E0203")
                        .with_primary_label("未定义的属性"),
                    );
                }
            }
            Ty::Dynamic | Ty::Error => self.soft_demote = true,
            _ => {
                self.diagnostics.push(
                    Diagnostic::error(
                        Span::new(self.file, range),
                        format!("类型 `{base_ty:?}` 不支持属性赋值"),
                    )
                    .with_code("E0203")
                    .with_primary_label("不支持属性赋值"),
                );
            }
        }
    }
}
