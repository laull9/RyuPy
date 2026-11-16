//! async-for的窄耗尽区域与默认关闭合同；结构断言保护目标和正文不会落入合成try。
use crate::*;
fn lower(source: &str) -> (Module, Vec<ryupy_base::Diagnostic>) {
    let mut db = ryupy_base::SourceDb::default();
    let file = db.add("async_for.py", source);
    let (parsed, errors) = ryupy_syntax::parse(file, source);
    assert!(errors.is_empty(), "{errors:?}");
    lower_module(&parsed, &ModuleInfo::default(), source)
}

#[test]
fn async_for_is_explicitly_feature_gated() {
    let (_, errors) = lower("async def f(source):\n    async for value in source:\n        pass\n");
    if cfg!(feature = "native-coroutines") {
        assert!(errors.is_empty(), "{errors:?}");
    } else {
        assert!(
            errors
                .iter()
                .any(|e| e.message == "async for 尚未接入原生协程状态机")
        );
        assert!(errors.iter().all(|e| e.code == Some("E0100")
            && e.deferred_failure
                == Some(ryupy_base::diagnostic::DeferredFailure::SupportBoundary)));
    }
}

#[cfg(feature = "native-coroutines")]
#[test]
fn only_next_and_await_are_inside_the_synthetic_exhaustion_region() {
    let (module, errors) = lower(
        "async def f(source, target):\n    async for target.value in source:\n        raise ValueError('body')\n    else:\n        raise TypeError('else')\n",
    );
    assert!(errors.is_empty(), "{errors:?}");
    let function = module
        .functions
        .iter()
        .find(|(_, f)| f.name.as_str() == "f")
        .unwrap()
        .1;
    assert!(function.is_coroutine && function.is_generator);
    let body = &function.body;
    let while_body = body
        .stmts
        .iter()
        .find_map(|(_, s)| match &s.kind {
            StmtKind::While { body, .. } => Some(body),
            _ => None,
        })
        .unwrap();
    let StmtKind::Try {
        body: protected,
        handlers,
        finalbody,
        ..
    } = &body.stmts[while_body[0]].kind
    else {
        panic!("需要真实窄try");
    };
    assert_eq!(protected.len(), 1);
    assert!(body.stmts[protected[0]].line > 0);
    assert!(finalbody.is_empty());
    let StmtKind::Assign { value, .. } = body.stmts[protected[0]].kind else {
        panic!("需要下一项赋值");
    };
    let ExprKind::Await {
        value,
        context: AwaitContext::AsyncNext,
    } = body.exprs[value].kind
    else {
        panic!("需要专用await");
    };
    assert!(matches!(
        body.exprs[value].kind,
        ExprKind::AsyncIteration {
            op: AsyncIterationOp::Next,
            ..
        }
    ));
    assert_eq!(
        body.handlers[handlers[0]].semantics,
        HandlerSemantics::SynthesizedAsyncFor
    );
    assert!(body.handlers[handlers[0]].types.is_none());
    assert!(body.stmts[while_body[1]].line > 0);
    let StmtKind::If { then_, .. } = &body.stmts[body.root[0]].kind else {
        panic!("需要合成外壳");
    };
    assert_eq!(body.stmts[*then_.last().unwrap()].line, 0);
    assert!(matches!(
        body.stmts[while_body[1]].kind,
        StmtKind::Assign {
            target: AssignTarget::Attribute { .. },
            ..
        }
    ));
    assert!(matches!(
        body.stmts[while_body[2]].kind,
        StmtKind::Raise { .. }
    ));
    assert_eq!(
        body.exprs
            .iter()
            .filter(|(_, e)| matches!(
                e.kind,
                ExprKind::AsyncIteration {
                    op: AsyncIterationOp::Start,
                    ..
                }
            ))
            .count(),
        1
    );
}

#[cfg(feature = "native-coroutines")]
#[test]
fn async_iteration_does_not_open_plain_functions_or_async_generators() {
    let (_, errors) = lower("def f(source):\n    async for value in source:\n        pass\n");
    assert!(
        errors
            .iter()
            .any(|e| e.message == "await 只能出现在 async 函数体内")
    );
    let (_, errors) = lower("async def f():\n    yield 1\n");
    assert!(!errors.is_empty());
    assert!(
        errors.iter().all(|e| e.deferred_failure
            == Some(ryupy_base::diagnostic::DeferredFailure::SupportBoundary))
    );
}

#[cfg(feature = "native-coroutines")]
#[test]
fn coroutine_delete_scope_does_not_leak_to_nested_generators_or_classes() {
    let (_, errors) = lower(
        "async def outer():\n    def inner():\n        value = 1\n        del value\n    return inner\n",
    );
    assert!(errors.is_empty(), "{errors:?}");
    let (_, errors) = lower(
        "async def outer():\n    def inner():\n        value = 1\n        yield value\n        del value\n    return inner\n",
    );
    assert!(errors.iter().any(|e| e.message == "暂不支持该 del 目标"));
    let (_, errors) = lower(
        "async def outer():\n    class Inner:\n        value = 1\n        del value\n    return Inner\n",
    );
    assert!(!errors.is_empty(), "协程中的类体仍在原有拒绝域");
    // 允许删除不发布普通函数反射局部表；合法None与删除哨兵仍由不同真实值表示。
    let (module, errors) = lower("async def outer():\n    value = None\n    del value\n");
    assert!(errors.is_empty(), "{errors:?}");
    let function = module
        .functions
        .iter()
        .find(|(_, f)| f.is_coroutine)
        .unwrap()
        .1;
    assert!(function.body.function_locals.is_none());
}
