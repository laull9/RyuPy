// 模块缓存查询保留原实时读取/列表快照；唯一写入口仅允许新增真实模块别名。
using System.Linq;

namespace RyuPy.Runtime;

/// <summary>sys.modules的受控视图；查询真实缓存，仅允许受限新增模块别名，不伪装可写dict。</summary>
public sealed class PyModuleView : PyNativeObject
{
    /// <inheritdoc />
    public override string TypeName => "mappingproxy";

    private object Get(object? key)
    {
        if (key is not string name || !PyModules.TryCached(name, out PyModule? module))
            throw new PyKeyError(PyRepr.Repr(key));
        return module!;
    }

    /// <inheritdoc />
    public override object? GetAttr(string name) => name switch
    {
        "__getitem__" => PyNativeFunctions.Create(name, ["key"], [null], 1, args => Get(args[0])),
        "__contains__" => PyNativeFunctions.Create(name, ["key"], [null], 1,
            args => args[0] is string key && PyModules.TryCached(key, out _)),
        "__len__" => PyNativeFunctions.Create(name, [], [], 0, _ => (long)PyModules.CacheSnapshot().Count),
        "__iter__" => PyNativeFunctions.Create(name, [], [], 0, _ => PyIter.Create(Keys())),
        "keys" => PyNativeFunctions.Create(name, [], [], 0, _ => Keys()),
        "values" => PyNativeFunctions.Create(name, [], [], 0, _ => new PyList(PyModules.CacheSnapshot().Values)),
        "items" => PyNativeFunctions.Create(name, [], [], 0, _ => new PyList(PyModules.CacheSnapshot()
            .Select(pair => (object?)new PyTuple([pair.Key, pair.Value])))),
        "get" => PyNativeFunctions.Create(name, ["key", "default"], [null, PyNone.Instance], 1,
            args => args[0] is string key && PyModules.TryCached(key, out PyModule? module) ? module : args[1]),
        "__setitem__" => PyNativeFunctions.Create(name, ["key", "value"], [null, null], 2,
            args => PyModules.AddCacheAlias(args[0], args[1]), ["posonly", "posonly"]),
        "__delitem__" => PyNativeFunctions.Create(name, ["key"], [null], 1,
            _ => throw PyModules.CacheAliasUnsupported("deletion is not implemented")),
        _ => throw Missing(name),
    };

    private static PyList Keys() => new(PyModules.CacheSnapshot().Keys);
}
