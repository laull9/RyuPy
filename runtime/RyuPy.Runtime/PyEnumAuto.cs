// 真实enum.auto原生标记：值由Enum赋值期协议生成并缓存，不把调用语法折叠成整数。
namespace RyuPy.Runtime;

internal sealed class PyEnumAuto : PyNativeObject
{
    private sealed class AutoNull : PyNativeObject
    {
        internal static readonly AutoNull Instance = new();
        internal static readonly PyNativeType Type = new("_auto_null", "enum", null, value => value is AutoNull);
        public override string TypeName => "_auto_null";
        public override object? GetAttr(string name) => name == "__class__" ? Type : throw Missing(name);
        public override string PythonRepr() => "_auto_null";
    }
    internal static readonly PyNativeType Type = new("auto", "enum", Invoke, value => value is PyEnumAuto);
    private static readonly PySignature Signature = PySignature.Create("auto", ["value"], ["pos"], ["1"]);
    private object? value;
    private PyEnumAuto(object? value) => this.value = value;
    internal bool Unresolved => ReferenceEquals(value, AutoNull.Instance);
    internal object? Value => value;
    internal void Resolve(object? resolved) => value = resolved;
    private static object Invoke(object?[] args, string[] names, object?[] values)
    {
        object? actual = PyBinder.Bind(Signature, args, names, values, [AutoNull.Instance])[0];
        ValidateValue(actual);
        return new PyEnumAuto(actual);
    }
    public override string TypeName => "auto";
    public override object? GetAttr(string name) => name switch
    {
        "value" => value,
        "__class__" => Type,
        _ => throw Missing(name),
    };
    public override void SetAttr(string name, object? item)
    {
        if (name != "value") throw Missing(name);
        ValidateValue(item); value = item;
    }
    // 删除value会改变标记识别协议；本竖切不伪装成仍有未赋值哨兵。
    public override void DelAttr(string name)
    {
        if (name == "value") throw new PyNotImplementedError("deleting enum.auto.value is not supported");
        throw Missing(name);
    }
    private static void ValidateValue(object? item)
    {
        if (item is not (string or bool or long or int or System.Numerics.BigInteger or PyNone or AutoNull) && item is not null)
            throw new PyNotImplementedError("enum.auto explicit values support scalar values only");
    }
    public override string PythonRepr() => $"auto({PyRepr.Repr(value)})";
}
