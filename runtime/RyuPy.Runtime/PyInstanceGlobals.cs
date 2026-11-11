// 直接globals冷路仅消费真实调用者context；普通callable自身不捕获环境。
using System;
namespace RyuPy.Runtime;

internal static class PyInstanceGlobals
{
    // 与可变默认字典独立：重绑/删除不能改变规范函数身份。
    internal static readonly PyFunction Function = Create();

    private static PyFunction Create()
    {
        var function = PyNativeFunctions.Create("globals", [], [], 0,
            _ => throw new PyNotImplementedError("globals requires a direct instance call context"));
        function.CaptureModule("builtins");
        return function;
    }
}

public static partial class PyModuleInstances
{
    /// <summary>已经按LEGB读取实际callee；不是名字intrinsic，也不读取ambient状态。</summary>
    public static object? CallDirectGlobals(object state, object? callable)
    {
        var context = Context(state);
        return ReferenceEquals(callable, PyInstanceGlobals.Function)
            ? context.State.Globals
            : PyOps.CallValue(callable, [], [], []);
    }
}
