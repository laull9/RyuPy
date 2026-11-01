using System;

namespace RyuPy.Runtime;

/// <summary>语言兼容目标与实际实现身份分离；版本序列不可变，不冒充 CPython 实现。</summary>
public static class PySystemVersion
{
    private static readonly Lazy<PyVersionInfo> Info = new(() =>
    {
        _ = new PyNativeType("version_info", "sys", null, value => value is PyVersionInfo, "tuple");
        return new PyVersionInfo();
    });
    /// <summary>固定项目基线对应的语言版本，不代表该版本全部能力已实现。</summary>
    public static PyVersionInfo VersionInfo => Info.Value;
    /// <summary>明确包含 RyuPy 身份；不复用 CPython 的构建字符串。</summary>
    public static string Version => "3.12.13 (RyuPy; Python compatibility target)";
    /// <summary>仅公开已确认的实现标识，不伪造 CPython cache_tag/构建参数。</summary>
    public static PyNativeObject Implementation { get; } = new ImplementationInfo();

    private sealed class ImplementationInfo : PyNativeObject
    {
        public override string TypeName => "SimpleNamespace";
        public override object? GetAttr(string name) => name == "name" ? "ryupy" : throw Missing(name);
        public override string PythonRepr() => "namespace(name='ryupy')";
    }
}

/// <summary>sys.version_info 的只读 tuple 派生协议；内部元组用于共享索引/切片/比较/哈希规则。</summary>
public sealed class PyVersionInfo : PyNativeObject
{
    private readonly PyTuple _items = new([3L, 12L, 13L, "final", 0L]);
    internal PyVersionInfo() { }
    /// <inheritdoc />
    public override string TypeName => "version_info";
    /// <inheritdoc />
    public override object? GetAttr(string name) => name switch
    {
        "major" => 3L, "minor" => 12L, "micro" => 13L, "releaselevel" => "final", "serial" => 0L,
        "n_fields" or "n_sequence_fields" => 5L, "n_unnamed_fields" => 0L,
        "__class__" => PyNativeType.LookupByName("version_info"),
        "__iter__" => Method(name, [], [], 0, _ => PyIter.Create(_items)),
        "__len__" => Method(name, [], [], 0, _ => 5L),
        "__getitem__" => Method(name, ["index"], [null], 1, a => PyOps.GetItem(_items, a[0])),
        "__contains__" => Method(name, ["value"], [null], 1, a => PyOps.Contains(_items, a[0])),
        "__hash__" => Method(name, [], [], 0, _ => PyHash.Of(_items)),
        "__eq__" => Compare(name, PyOps.Eq), "__ne__" => Compare(name, PyOps.Ne),
        "__lt__" => Compare(name, PyOps.Lt), "__le__" => Compare(name, PyOps.Le),
        "__gt__" => Compare(name, PyOps.Gt), "__ge__" => Compare(name, PyOps.Ge),
        _ => throw Missing(name),
    };
    private PyFunction Compare(string name, int op) => Method(name, ["other"], [null], 1, a =>
    {
        object? other = a[0] is PyVersionInfo info ? info._items : a[0];
        return other is PyTuple tuple ? PyOps.Compare(op, _items, tuple) : PyNotImplemented.Instance;
    });
    private static PyFunction Method(string name, string[] names, object?[] defaults, int required, Func<object?[], object?> body) =>
        PyNativeFunctions.Create(name, names, defaults, required, body, Array.ConvertAll(names, _ => "posonly"));
    /// <inheritdoc />
    public override string PythonRepr() => "sys.version_info(major=3, minor=12, micro=13, releaselevel='final', serial=0)";
}
