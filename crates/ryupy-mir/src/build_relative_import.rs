//! 相对from-import先完成fromlist，再逐项取值写绑定；后端只见显式原语。
use crate::{build::FunctionBuilder, *};
impl FunctionBuilder<'_> {
    pub(crate) fn build_relative_import(
        &mut self,
        level: u32,
        name: &str,
        names: &[(String, ryupy_hir::Binding)],
    ) {
        let Some(state) = self.module_state else {
            return;
        };
        let imported = self.alloc_temp(MirType::Dynamic);
        self.push_stmt(Statement::Assign {
            dest: imported,
            value: Rvalue::Call {
                callee: Callee::Runtime(RuntimeFn::InstanceRelativeImport),
                args: vec![
                    Operand::Local(state),
                    Operand::Const(Constant::Str(name.to_owned())),
                    Operand::Const(Constant::I64(level.into())),
                    Operand::Array(
                        names
                            .iter()
                            .map(|(name, _)| Operand::Const(Constant::Str(name.clone())))
                            .collect(),
                    ),
                ],
            },
        });
        for (name, binding) in names {
            let value = self.alloc_temp(MirType::Dynamic);
            self.push_stmt(Statement::Assign {
                dest: value,
                value: Rvalue::Call {
                    callee: Callee::Runtime(RuntimeFn::InstanceRelativeImportAttr),
                    args: vec![
                        Operand::Local(state),
                        Operand::Local(imported),
                        Operand::Const(Constant::Str(name.clone())),
                    ],
                },
            });
            self.store_import_binding(*binding, Operand::Local(value));
        }
    }
}
