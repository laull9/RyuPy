//! 权威provider模块的IMPORT_NAME/IMPORT_FROM降级；实际hook一次求值，逐项绑定保留前缀副作用。
use crate::{build::FunctionBuilder, *};

impl FunctionBuilder<'_> {
    fn provider_import_name(&mut self, name: &str, level: u32, fromlist: Operand) -> Operand {
        let state = self.module_state.expect("validated provider namespace");
        let module =
            self.hir.functions[self.current_hir_fn].kind == ryupy_hir::FunctionKind::ModuleMain;
        let imported = self.alloc_temp(MirType::Dynamic);
        self.push_stmt(Statement::Assign {
            dest: imported,
            value: Rvalue::Call {
                callee: Callee::Runtime(RuntimeFn::InstanceImportName),
                args: vec![
                    Operand::Local(state),
                    Operand::Const(Constant::Str(name.into())),
                    Operand::Const(Constant::I64(level.into())),
                    fromlist,
                    Operand::Const(Constant::Bool(module)),
                ],
            },
        });
        Operand::Local(imported)
    }
    fn provider_import_attribute(&mut self, imported: Operand, name: &str) -> Operand {
        let value = self.alloc_temp(MirType::Dynamic);
        self.push_stmt(Statement::Assign {
            dest: value,
            value: Rvalue::Call {
                callee: Callee::Runtime(RuntimeFn::InstanceImportAttribute),
                args: vec![
                    Operand::Local(self.module_state.expect("validated provider namespace")),
                    imported,
                    Operand::Const(Constant::Str(name.into())),
                ],
            },
        });
        Operand::Local(value)
    }
    pub(crate) fn build_provider_import(
        &mut self,
        name: &str,
        binding: ryupy_hir::Binding,
        root: bool,
    ) {
        let mut value = self.provider_import_name(name, 0, Operand::Const(Constant::None));
        // __import__对普通import返回根值；dotted-as按IMPORT_FROM逐层遍历返回的任意对象。
        if !root {
            for member in name.split('.').skip(1) {
                value = self.provider_import_attribute(value, member);
            }
        }
        self.store_import_binding(binding, value);
    }
    pub(crate) fn build_provider_import_from(
        &mut self,
        name: &str,
        level: u32,
        names: &[(String, ryupy_hir::Binding)],
    ) {
        let fromlist = Operand::Array(
            names
                .iter()
                .map(|(name, _)| Operand::Const(Constant::Str(name.clone())))
                .collect(),
        );
        let imported = self.provider_import_name(name, level, fromlist);
        for (name, binding) in names {
            let value = self.provider_import_attribute(imported.clone(), name);
            self.store_import_binding(*binding, value);
        }
    }
}

pub(crate) fn signature(runtime: RuntimeFn) -> Option<(&'static [MirType], MirType)> {
    match runtime {
        RuntimeFn::InstanceImportName => Some((
            &[
                MirType::Dynamic,
                MirType::Str,
                MirType::I64,
                MirType::Dynamic,
                MirType::Bool,
            ],
            MirType::Dynamic,
        )),
        RuntimeFn::InstanceImportAttribute => Some((
            &[MirType::Dynamic, MirType::Dynamic, MirType::Str],
            MirType::Dynamic,
        )),
        _ => None,
    }
}

/// 名称/层级/fromlist及模块locals角色均由语法冻结，禁止任意MIR输入冒充真实导入现场。
pub(crate) fn validate(function: &Function, value: &Rvalue, errors: &mut Vec<String>) {
    let Rvalue::Call {
        callee: Callee::Runtime(runtime),
        args,
    } = value
    else {
        return;
    };
    if !matches!(
        runtime,
        RuntimeFn::InstanceImportName | RuntimeFn::InstanceImportAttribute
    ) {
        return;
    }
    let valid = function.instance_capabilities.provider_namespace
        && matches!(args.first(), Some(Operand::Local(state)) if Some(*state) == function.module_state)
        && match runtime {
            RuntimeFn::InstanceImportName => {
                matches!(args.as_slice(), [_, Operand::Const(Constant::Str(_)),
                Operand::Const(Constant::I64(0..)), fromlist, Operand::Const(Constant::Bool(module))]
                if *module == (function.module_state_source == Some(InstanceStateSource::ModuleEntryParameter))
                && match fromlist { Operand::Const(Constant::None) => true,
                    Operand::Array(names) => names.iter().all(|name| matches!(name, Operand::Const(Constant::Str(_)))),
                    _ => false })
            }
            RuntimeFn::InstanceImportAttribute => {
                matches!(args.as_slice(), [_, _, Operand::Const(Constant::Str(_))])
            }
            _ => false,
        };
    if !valid {
        errors.push(format!(
            "函数 `{}` 的provider导入现场或能力非法",
            function.name
        ));
    }
}

#[cfg(test)]
mod tests {
    use super::*;
    use crate::provider_namespace_tests::compile;
    #[test]
    fn name_calls_are_once_per_syntax_and_each_attribute_is_followed_by_a_binding() {
        let program = compile("import alpha.beta as value\nfrom alpha import first, second\ndef read():\n    from alpha import third\n    return third\n", true).unwrap();
        let mut imports = vec![];
        let mut attributes = 0;
        for (_, function) in program.functions.iter() {
            for stmt in function
                .body
                .blocks
                .iter()
                .flat_map(|(_, block)| &block.stmts)
            {
                if let Statement::Assign {
                    value:
                        Rvalue::Call {
                            callee: Callee::Runtime(runtime),
                            args,
                        },
                    ..
                } = stmt
                {
                    if *runtime == RuntimeFn::InstanceImportName {
                        imports.push(args[4].clone());
                    }
                    if *runtime == RuntimeFn::InstanceImportAttribute {
                        attributes += 1;
                    }
                }
            }
        }
        assert_eq!(
            imports
                .iter()
                .filter(|value| **value == Operand::Const(Constant::Bool(true)))
                .count(),
            2
        );
        assert_eq!(
            imports
                .iter()
                .filter(|value| **value == Operand::Const(Constant::Bool(false)))
                .count(),
            1
        );
        assert_eq!(attributes, 4);
    }
    #[test]
    fn importer_requires_the_frozen_capability_context_role_and_fromlist() {
        for mutation in 0..5 {
            let mut program = compile("from alpha import first\n", true).unwrap();
            let main = &mut program.functions[program.entry];
            for stmt in main
                .body
                .blocks
                .iter_mut()
                .flat_map(|(_, block)| &mut block.stmts)
            {
                if let Statement::Assign {
                    value:
                        Rvalue::Call {
                            callee: Callee::Runtime(RuntimeFn::InstanceImportName),
                            args,
                        },
                    ..
                } = stmt
                {
                    match mutation {
                        0 => main.instance_capabilities.provider_namespace = false,
                        1 => args[0] = Operand::Const(Constant::None),
                        2 => args[2] = Operand::Const(Constant::I64(-1)),
                        3 => args[3] = Operand::Array(vec![Operand::Const(Constant::I64(9))]),
                        4 => args[4] = Operand::Const(Constant::Bool(false)),
                        _ => unreachable!(),
                    }
                }
            }
            assert!(
                crate::validate::validate(&program).is_err(),
                "mutation {mutation}"
            );
        }
    }
    #[test]
    fn class_imports_have_an_explicit_prepared_namespace_boundary() {
        let errors = compile("class Box:\n    import alpha\n", true).unwrap_err();
        assert!(
            errors
                .iter()
                .any(|error| error.message.contains("prepared namespace"))
        );
    }
}
