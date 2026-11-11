//! 将词法类最终值一次发布，并记录实际MIR目标，供cell验证器检查local/free/global/namespace。
use crate::build::FunctionBuilder;
use crate::*;
use ryupy_hir::{Binding, LexicalBinding};
impl FunctionBuilder<'_> {
    pub(crate) fn publish_lexical_class(
        &mut self,
        target: LexicalBinding,
        value: Operand,
        namespace: LocalId,
        cell: Option<LocalId>,
    ) {
        let publication = match &target {
            LexicalBinding::Outer(Binding::Local(local)) => {
                let local = self.local_map[local];
                Some(if self.locals[local].is_cell {
                    LexicalClassPublication::Cell(local)
                } else {
                    LexicalClassPublication::Local(local)
                })
            }
            LexicalBinding::Outer(Binding::ModuleVar(var)) => Some(match self.module_state {
                Some(context) => LexicalClassPublication::InstanceName {
                    context,
                    name: self.hir.vars[*var].name.to_string(),
                },
                None => LexicalClassPublication::Global(self.module_vars[var]),
            }),
            LexicalBinding::Outer(Binding::Free(_)) => None,
            LexicalBinding::Namespace(name) => match self.class_namespace {
                Some(Operand::Local(parent)) => Some(LexicalClassPublication::Namespace {
                    namespace: parent,
                    name: name.to_string(),
                    lexical: self.lexical_namespace,
                }),
                _ => None,
            },
        };
        let free = match target {
            LexicalBinding::Outer(Binding::Free(index)) => Some(index),
            _ => None,
        };
        match target {
            LexicalBinding::Outer(binding) => self.store_binding(binding, value, &MirType::Dynamic),
            LexicalBinding::Namespace(name) => {
                if let Some(parent) = self.class_namespace.clone() {
                    self.emit_set_attr(parent, name.as_str(), value);
                }
            }
        }
        let publication = publication.or_else(|| {
            free.and_then(|index| match self.blocks[self.current_bb].stmts.last() {
                Some(Statement::Eval(Rvalue::Call {
                    callee: Callee::Runtime(RuntimeFn::CellSet),
                    args,
                })) => match args.first() {
                    Some(Operand::Local(cell)) => {
                        Some(LexicalClassPublication::FreeCell { cell: *cell, index })
                    }
                    _ => None,
                },
                _ => None,
            })
        });
        if let Some(cell) = cell
            && let Some((_, plan)) = self
                .class_cells
                .iter_mut()
                .find(|(_, plan)| plan.cell == cell)
        {
            if let Some(publication) = publication {
                plan.lexical = Some(LexicalClassCell {
                    namespace,
                    publication,
                });
            } else {
                self.diagnostics.push(
                    ryupy_base::Diagnostic::error(
                        ryupy_base::Span::new(
                            self.hir.file,
                            self.hir.functions[self.current_hir_fn].range,
                        ),
                        "词法类发布目标缺少真实MIR来源",
                    )
                    .with_code("E0300"),
                );
            }
        }
    }
}
