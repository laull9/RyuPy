// inspect桥的受信函数种类只登记在canonical PyFunction弱键上；不扩普通函数字段/调用热路。
using System;
using System.Runtime.CompilerServices;

namespace RyuPy.Runtime;

/// <summary>代码生成器在真实def创建后冷发布的种类，不接受可写名称/注解/签名作为证明。</summary>
public static class PyInspectFunctionKind
{
    private sealed record Info(long Kind);
    private static readonly ConditionalWeakTable<PyFunction, Info> Kinds = new();
    /// <summary>只有模块级普通函数可按CP原生callable协议隐藏不存在的CodeType。</summary>
    public static bool IsOrdinaryModule(PyFunction function) => Kinds.TryGetValue(function, out Info? info) && info.Kind == 0;
    internal static void Register(object? function, long kind)
    {
        if (function is not PyFunction actual) throw new PyTypeError("function introspection kind requires a canonical function");
        if (kind < 0 || kind > 4) throw new PyValueError("invalid trusted function introspection kind");
        try { Kinds.Add(actual, new(kind)); }
        catch (ArgumentException) { throw new PyRuntimeError("canonical function introspection kind is already registered"); }
    }
}

public static partial class PyOps
{
    /// <summary>0普通模块函数、1生成器、2协程、3异步生成器、4非模块普通函数；旧宿主未登记保持unknown。</summary>
    public static void FunctionIntrospectionKind(object function, long kind) => PyInspectFunctionKind.Register(function, kind);
}
