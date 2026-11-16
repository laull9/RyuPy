// typed定义的通用伴生入口只在缺省补齐/通用体传递时使用；实际函数弱键保持唯一Python身份。
using System;
using System.Runtime.CompilerServices;

namespace RyuPy.Runtime;

public static partial class PyOps
{
    private sealed class GenericFunctionCore(PyCall body)
    {
        internal readonly PyCall Body = body;
    }
    // 独立冷类型延迟初始化；没有伴生定义的产品不为此表分配常量成本。
    private static class GenericFunctionRegistry
    {
        internal static readonly ConditionalWeakTable<PyFunction, GenericFunctionCore> Cores = new();
    }

    /// <summary>真实def创建后登记静态生成的通用委托；不复制函数、签名或默认值。</summary>
    public static void RegisterGenericFunction(object? value, PyCall body)
    {
        if (value is not PyFunction function) throw new PyTypeError("generic core requires function");
        GenericFunctionRegistry.Cores.Add(function, new GenericFunctionCore(body));
    }

    /// <summary>通用体的显式调用域；先按原Python规则展开实参，再匹配实际函数身份。</summary>
    public static object? CallValueGeneric(object? callable, object?[] rawArgs, object?[] rawNames, object?[] rawValues)
    {
        var (args, names, values) = FlattenArgs(rawArgs, rawNames, rawValues);
        return InvokeGenericFunction(callable, args, names, values);
    }

    internal static object? InvokeGenericFunction(object? callable, object?[] args, string[] names, object?[] values)
    {
        if (callable is PyFunction function && GenericFunctionRegistry.Cores.TryGetValue(function, out var core))
            return core.Body(function, args, names, values);
        // 显式包装透传同一冷域；无TLS/环境变量，不改变普通Invoke热路。
        if (callable is PyPartial partial) return partial.InvokeGeneric(args, names, values);
        if (callable is PyStaticMethod sm) return InvokeGenericFunction(sm.Value, args, names, values);
        if (callable is PyClassMethod cm) return InvokeGenericFunction(cm.Value, args, names, values);
        if (callable is PyBoundMethod method)
        {
            var bound = new object?[args.Length + 1];
            bound[0] = method.Self;
            Array.Copy(args, 0, bound, 1, args.Length);
            return InvokeGenericFunction(method.Target, bound, names, values);
        }
        return CallValue(callable, args, names, values);
    }
}
