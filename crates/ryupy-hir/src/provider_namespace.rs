//! provider真实模块字典的显式语义合同；共享既有Instance ABI与Dynamic推导，旧实例/AOT首切守卫不变。
use ryupy_base::{Diagnostic, InstanceCapabilities};
use ryupy_syntax::ParsedModule;

pub fn provider_namespace_capabilities() -> InstanceCapabilities {
    InstanceCapabilities {
        provider_namespace: true,
        entry_namespace: true,
        aot_class_guards: true,
        direct_globals: true,
        ordinary_name_handlers: true,
        relative_from_import: true,
    }
}

/// 全图来源由driver证明；这里保留所有未扩围的语法边界，后续HIR/MIR仍逐模块验证完整实现。
/// 无def/Entry/Package均有同一实例执行ABI，不沿用旧首切lambda/typed/NoFunction的资格理由。
pub fn validate_provider_namespace_aot(parsed: &ParsedModule) -> Vec<Diagnostic> {
    // 同步生成器沿既有GeneratorFrame保存/恢复context；异步与生成器handler仍由下层精确守卫。
    crate::instance_globals::validate(
        &parsed.module.body,
        parsed.file,
        provider_namespace_capabilities(),
    )
}

#[cfg(test)]
mod tests {
    fn lower(source: &str, provider: bool) -> (crate::Module, Vec<ryupy_base::Diagnostic>) {
        let mut db = ryupy_base::SourceDb::default();
        let file = db.add("provider_namespace.py", source);
        let (parsed, errors) = ryupy_syntax::parse(file, source);
        assert!(errors.is_empty(), "{errors:?}");
        let mut capabilities = super::provider_namespace_capabilities();
        capabilities.provider_namespace = provider;
        crate::lower_module(
            &parsed,
            &crate::ModuleInfo {
                instance_globals: true,
                instance_capabilities: capabilities,
                ..Default::default()
            },
            source,
        )
    }
    #[test]
    fn complex_handlers_keep_real_target_expressions_only_in_new_profile() {
        let source = "def route(target):\n    try:\n        raise ValueError('original')\n    except (LookupError, target.problem) as caught:\n        return caught\n    except target.fallback:\n        return None\n";
        let (module, errors) = lower(source, true);
        assert!(errors.is_empty(), "{errors:?}");
        let function = module
            .functions
            .iter()
            .find(|(_, f)| f.name.as_str() == "route")
            .unwrap()
            .1;
        assert_eq!(function.body.handlers.len(), 2);
        for (_, handler) in function.body.handlers.iter() {
            assert_eq!(
                handler.semantics,
                crate::HandlerSemantics::InstanceNameDynamic
            );
            assert!(handler.type_expr.is_some() && handler.types.is_none());
        }
        assert!(
            lower(source, false)
                .1
                .iter()
                .any(|d| d.message.contains("非Name/bare"))
        );
    }
    #[test]
    fn actual_unimplemented_handler_and_generator_domains_remain_diagnostic() {
        for (source, reason) in [
            (
                "try:\n    pass\nexcept target():\n    pass\n",
                "非Name/bare",
            ),
            (
                "def source():\n    try:\n        yield 1\n    except ValueError:\n        yield 2\n",
                "生成器或类体",
            ),
            ("async def source():\n    yield 1\n", "异步函数"),
        ] {
            let (_, errors) = lower(source, true);
            assert!(
                errors.iter().any(|d| d.message.contains(reason)),
                "{errors:?}"
            );
        }
    }
    #[test]
    fn synchronous_generator_context_is_admitted_only_in_the_provider_profile() {
        let source = "value = 3\ndef source():\n    yield globals()\n    yield from (value + item for item in [1, 2])\n";
        let (module, errors) = lower(source, true);
        assert!(errors.is_empty(), "{errors:?}");
        assert!(module.functions.iter().any(|(_, function)| function.is_generator));
        assert!(lower(source, false).1.iter().any(|d| d.message.contains("globals") || d.message.contains("生成器表达式")));
    }
    #[test]
    fn authoritative_dictionary_attribute_and_item_deletion_use_existing_targets() {
        let source = "import state\ndel state.value\ndel state.__dict__['other']\n";
        assert!(lower(source, true).1.is_empty());
        assert!(
            lower(source, false)
                .1
                .iter()
                .any(|d| d.message.contains("del目标"))
        );
    }
}
