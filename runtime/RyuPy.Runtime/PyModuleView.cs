using System.Linq;

namespace RyuPy.Runtime;

/// <summary>sys.modules的受控实时视图；可查询实际已导入对象，拒绝伪造/删除模块缓存。</summary>
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
            _ => throw new PyNotImplementedError("sys.modules mutation is not supported")),
        "__delitem__" => PyNativeFunctions.Create(name, ["key"], [null], 1,
            _ => throw new PyNotImplementedError("sys.modules mutation is not supported")),
        _ => throw Missing(name),
    };

    private static PyList Keys() => new(PyModules.CacheSnapshot().Keys);
}
