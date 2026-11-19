// 正常 CPython tp_call 持有原位置元组；只给已生成读点的形参借用元组槽，不提前持有元素。
using RyuPy.Runtime;

namespace RyuPy.CPython;

public sealed partial class CPythonHost
{
    private object? InvokeCallback(object target, nint tuple, nint keywords)
    {
        PyFunction? function = target switch
        {
            PyFunction plain => plain,
            ForeignMethodTarget method => method.Function,
            _ => null,
        };
        if (function is not null && PyDeferredCallbackArgument.Accepts(function)
            && Native.rp_exact_tuple(tuple) != 0)
        {
            int length = checked((int)Native.rp_length(tuple)); ThrowIfError();
            int eager = target is ForeignMethodTarget ? 1 : 0;
            if (length > eager && length == function.Signature.Names.Length
                && (keywords == 0 || Native.rp_container_kind(keywords) == 2 && Native.rp_length(keywords) == 0))
            {
                using var scope = new CallbackArguments(this, tuple);
                var arguments = new object?[length];
                // 构造、方法上下文和接收者守卫可以在进入 CIL 之前观察首参。
                if (eager != 0) arguments[0] = scope.Read(0);
                for (int i = eager; i < length; i++) arguments[i] = new DeferredArgument(scope, i);
                return PyOps.CallValue(target, arguments, [], []);
            }
            ThrowIfError();
        }
        object?[] positional = ReadTuple(tuple);
        (string[] names, object?[] values) = ReadKeywords(keywords);
        return PyOps.CallValue(target, positional, names, values);
    }

    private sealed class CallbackArguments(CPythonHost host, nint borrowedTuple) : IDisposable
    {
        private readonly int thread = Environment.CurrentManagedThreadId;
        private nint tuple = borrowedTuple;

        internal object? Read(int index)
        {
            if (Environment.CurrentManagedThreadId != thread || tuple == 0)
                throw new PyRuntimeError("CPython callback argument borrow has ended");
            return host.FromOwned(Native.rp_op(18, tuple, n: index));
        }

        // 即使 binder/body 抛错也立即失效，不触碰仍由原生调用持有的元组引用。
        public void Dispose() => tuple = 0;
    }

    private sealed class DeferredArgument(CallbackArguments source, int index) : PyDeferredCallbackArgument
    {
        protected override object? Materialize() => source.Read(index);
    }
}
