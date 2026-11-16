// inspect 的惰性原生入口；普通程序不分配签名对象，不修改函数调用布局或ABI。
using System;
using System.Collections.Generic;

namespace RyuPy.Runtime;

/// <summary>原生inspect入口和冷方法绑定工具。</summary>
public static class PyInspectModule
{
    private static readonly Lazy<Dictionary<string, object?>> Attributes = new(Create);
    /// <summary>empty是唯一类身份，与None和参数未提供哨兵分离。</summary>
    public static PyNativeType Empty => EmptyHolder.Value;
    private static class EmptyHolder
    {
        internal static readonly PyNativeType Value = new("_empty", "inspect", null, _ => false);
        static EmptyHolder() { RegisterIdentity(Value, 1); }
    }
    /// <summary>登记延迟初始化的原生模块。</summary>
    public static void Register() => PyModules.RegisterNativeLazy("inspect", "", "", () => { },
        key => Attributes.Value.TryGetValue(key, out object? value) ? value
            : throw new PyAttributeError($"module 'inspect' has no attribute '{key}'"),
        (_, key, value) => Attributes.Value[key] = value);

    /// <summary>受信跨域profile按确切函数身份读取原生API，不改写CPython模块。</summary>
    public static object Export(string name) => Attributes.Value.TryGetValue(name, out object? value) && value is not null
        ? value : throw new PyAttributeError("unsupported inspect export");
    private static System.Collections.Concurrent.ConcurrentDictionary<object, int>? identities;
    internal static void RegisterIdentity(object value, int tag)
    {
        if (identities is null) System.Threading.Interlocked.CompareExchange(ref identities,
            new(System.Collections.Generic.ReferenceEqualityComparer.Instance), null);
        identities[value] = tag;
    }
    /// <summary>仅已初始化的原生inspect对象有身份登记；普通值不触发类型初始化。</summary>
    public static int BridgeIdentity(object value) => identities is not null && identities.TryGetValue(value, out int tag) ? tag : 0;

    private static Dictionary<string, object?> Create()
    {
        var result = new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["Parameter"] = PyInspectParameter.Type, ["Signature"] = PyInspectSignature.Type,
            ["BoundArguments"] = PyInspectBoundArguments.Type, ["_empty"] = Empty,
            ["_ParameterKind"] = PyInspectKind.Type,
            ["signature"] = Method("signature", (a, n, v) => PyInspectCallable.SignatureCall(a, n, v)),
            ["unwrap"] = Method("unwrap", (a, n, v) => PyInspectCallable.UnwrapCall(a, n, v)),
        };
        RegisterIdentity(result["signature"]!, 15); RegisterIdentity(result["unwrap"]!, 16);
        return result;
    }
    internal static PyInspectMethod Method(string name, Func<object?[], string[], object?[], object?> body) => new(name, body);
    // 构造器和replace按静态schema绑定；原生inspect.bind不经过普通函数默认值填充器。
    internal static object?[] Arguments(string name, string[] parameters, string[] kinds,
        object?[] defaults, int required, object?[] args, string[] names, object?[] values)
    {
        string[] flags = new string[parameters.Length];
        for (int i = 0; i < flags.Length; i++) flags[i] = i < required ? "0" : "1";
        return PyBinder.Bind(PySignature.Create(name, parameters, kinds, flags), args, names, values, defaults);
    }
    internal static bool IsNone(object? value) => value is null or PyNone;
    internal static bool OptionalAttr(object? target, string name, out object? value)
    {
        try { value = PyObject.GetAttr(target, name); return true; }
        catch (PyAttributeError) { value = null; return false; }
    }
    internal static object?[] Sequence(object? value)
    {
        var result = new List<object?>(); var iterator = PyIter.Create(value);
        while (iterator.MoveNext()) result.Add(iterator.Current());
        return result.ToArray();
    }
}

// 方法不是待反射Python函数；只提供显式调用，不创造伪造代码/闭包元数据。
internal sealed class PyInspectMethod(string name, Func<object?[], string[], object?[], object?> body)
    : PyNativeObject, IPyNativeCallable
{
    public override string TypeName => "builtin_function_or_method";
    public object? Invoke(object?[] args, string[] names, object?[] values) => body(args, names, values) ?? PyNone.Instance;
    public override object? GetAttr(string key) => key switch
    {
        "__name__" => name, "__module__" => "inspect", "__call__" => this,
        _ => throw Missing(key),
    };
    public override string PythonRepr() => $"<built-in function {name}>";
}
