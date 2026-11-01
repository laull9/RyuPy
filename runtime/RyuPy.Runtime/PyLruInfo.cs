// CacheInfo 保存一次快照，属性与序列协议共用同一个不可变四元组。
namespace RyuPy.Runtime;

internal sealed class PyLruInfo : PyNativeObject
{
    private static readonly PyNativeType NativeType = new("CacheInfo", "functools", null, value => value is PyLruInfo, "tuple");
    private readonly PyTuple _tuple;
    internal PyLruInfo(long hits, long misses, object? maximum, long size)
    { _ = NativeType; _tuple = new([hits, misses, maximum, size]); }
    public override string TypeName => "CacheInfo";
    public override object? GetAttr(string name) => name switch
    {
        "hits" => _tuple.Items[0], "misses" => _tuple.Items[1], "maxsize" => _tuple.Items[2], "currsize" => _tuple.Items[3],
        "__iter__" => PyNativeFunctions.Create(name, [], [], 0, _ => PyIter.Create(_tuple)),
        "__len__" => PyNativeFunctions.Create(name, [], [], 0, _ => 4L),
        "__getitem__" => PyNativeFunctions.Create(name, ["index"], [null], 1, a => _tuple.GetItem(a[0]!)),
        "__eq__" => PyNativeFunctions.Create(name, ["other"], [null], 1, a => PyHash.Equal(_tuple, a[0] is PyLruInfo info ? info._tuple : a[0])),
        "__hash__" => PyNativeFunctions.Create(name, [], [], 0, _ => PyHash.Of(_tuple)),
        _ => throw Missing(name),
    };
    public override string PythonRepr() => $"CacheInfo(hits={_tuple.Items[0]}, misses={_tuple.Items[1]}, maxsize={PyRepr.Repr(_tuple.Items[2])}, currsize={_tuple.Items[3]})";
}
