// 原生协程 ABI：区分 async 工厂发布与 await 的类型级协议查找，拒绝解释执行回落。
namespace RyuPy.Runtime;

public static partial class PyOps
{
    /// <summary>将已编译的显式状态机发布为 Python 原生协程。</summary>
    public static object CoroutineCreate(object generator) => generator is PyGenerator state
        ? new PyCoroutine(state) : throw new PyRuntimeError("invalid coroutine state machine");

    /// <summary>取得一次 await 迭代器；不再次调用 __iter__，也不将普通 iterable 当 awaitable。</summary>
    public static object CoroutineAwaitStart(object? source) => AwaitStart(source, null);
    /// <summary>异步enter只对缺失awaitable槽使用专用错误，用户协议异常原样传播。</summary>
    public static object CoroutineEnterAwaitStart(object? source) => AwaitStart(source, "__aenter__");
    /// <summary>异步exit与普通await共享委托，但保留上下文协议诊断。</summary>
    public static object CoroutineExitAwaitStart(object? source) => AwaitStart(source, "__aexit__");
    private static object AwaitStart(object? source, string? context, bool asyncNext = false)
    {
        if (source is PyCoroutine coroutine)
        {
            var wrapper = coroutine.AwaitIterator(true, asyncNext);
            return new PyGeneratorDelegation(wrapper, wrapper);
        }
        object? iterator;
        bool found;
        if (source is IPyForeignAwaitable foreign)
        {
            var awaiter = foreign.GetAwaitIterator();
            if (awaiter is not null) return new PyGeneratorDelegation(awaiter, awaiter.Target);
            found = false;
            iterator = null;
        }
        else if (asyncNext && AsyncClass(source) is not null)
        {
            found = TryAsyncMethod(source, "__await__", out object? method);
            iterator = found ? CallValue(method, [], [], []) : null;
        }
        else found = PyObject.TryMethod(source, "__await__", [], out iterator);
        if (!found)
            throw new PyTypeError(context is null
                ? $"object {PyRepr.TypeName(source)} can't be used in 'await' expression"
                : $"'async with' received an object from {context} that does not implement __await__: {PyRepr.TypeName(source)}");
        if (iterator is PyCoroutine)
            throw new PyTypeError("__await__() returned a coroutine");
        if (iterator is PyInstance instance && instance.Class.TryLookup("__next__", out _, out _))
            return new PyGeneratorDelegation(new PyInstanceIterator(instance), instance);
        if (iterator is IPyIterator ready)
            return new PyGeneratorDelegation(ready, iterator);
        throw new PyTypeError($"__await__() returned non-iterator of type '{PyRepr.TypeName(iterator)}'");
    }
}
