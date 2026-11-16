//! async 身份与 await 节点显式保留；域外异步语法在 HIR 给诊断。
use crate::*;

fn lower(source: &str) -> (Module, Vec<ryupy_base::Diagnostic>) {
    let mut db = ryupy_base::SourceDb::default();
    let file = db.add("coroutine.py", source);
    let (parsed, errors) = ryupy_syntax::parse(file, source);
    assert!(errors.is_empty(), "{errors:?}");
    lower_module(&parsed, &ModuleInfo::default(), source)
}

#[test]
fn coroutine_identity_is_feature_gated_and_never_a_synchronous_function() {
    let (module, errors) = lower("async def operation():\n    return 42\n");
    let function = module
        .functions
        .iter()
        .find(|(_, f)| f.name.as_str() == "operation")
        .unwrap()
        .1;
    assert!(function.is_coroutine && function.is_generator);
    assert_eq!(errors.is_empty(), cfg!(feature = "native-coroutines"));
    for error in &errors {
        assert_eq!(
            error.deferred_failure,
            Some(ryupy_base::diagnostic::DeferredFailure::SupportBoundary)
        );
    }
}

#[cfg(feature = "native-coroutines")]
#[test]
fn await_is_an_explicit_hir_node_and_async_generator_and_for_are_rejected() {
    let (module, errors) = lower("async def operation(value):\n    return await value\n");
    assert!(errors.is_empty(), "{errors:?}");
    assert!(module.functions.iter().any(|(_, function)| {
        function
            .body
            .exprs
            .iter()
            .any(|(_, expression)| matches!(expression.kind, ExprKind::Await { .. }))
    }));
    for source in [
        "async def f():\n    yield 1\n",
        "async def f():\n    async for value in []:\n        pass\n",
        "def f(value):\n    return await value\n",
        "class C:\n    async def f(self):\n        return super()\n",
    ] {
        assert!(!lower(source).1.is_empty(), "{source}");
    }
}

#[cfg(not(feature = "native-coroutines"))]
#[test]
fn disabled_nested_async_keeps_the_original_single_diagnostic() {
    let (_, errors) =
        lower("def outer():\n    async def inner():\n        return 1\n    return inner\n");
    assert_eq!(errors.len(), 1);
    assert_eq!(errors[0].message, "嵌套函数暂不支持 async");
}

#[cfg(feature = "native-coroutines")]
#[test]
fn await_call_preparation_does_not_change_yield_identity() {
    let source = "async def method(receiver, pause):\n    return receiver.method(await pause())\nasync def star(target, values, pause):\n    return target(*values, await pause())\n";
    let (module, errors) = lower(source);
    assert!(errors.is_empty(), "{errors:?}");
    for (_, function) in module.functions.iter().filter(|(_, f)| f.is_coroutine) {
        assert!(
            function
                .body
                .exprs
                .iter()
                .any(|(_, expression)| matches!(expression.kind, ExprKind::PreparedCall { .. }))
        );
    }
    let mut db = ryupy_base::SourceDb::default();
    let file = db.add("await_identity.py", source);
    let (parsed, errors) = ryupy_syntax::parse(file, source);
    assert!(errors.is_empty());
    for statement in &parsed.module.body {
        let ryupy_syntax::ast::Stmt::FunctionDef(function) = statement else {
            unreachable!()
        };
        assert!(!crate::generator_scan::contains_yield(&function.body));
    }
    let (_, errors) = lower(
        "async def f(target, pause, mapping):\n    return target(await pause(), **mapping)\n",
    );
    assert!(
        errors
            .iter()
            .any(|error| error.message == "调用双星号实参暂未支持跨暂停的映射快照")
    );
}
