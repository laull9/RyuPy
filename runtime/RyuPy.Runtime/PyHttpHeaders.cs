// HTTPMessage 的有限只读多值头集合：保留重复头、大小写不敏感查找和原始排列。
using System;
using System.Collections.Generic;
using System.Linq;

namespace RyuPy.Runtime;

/// <summary>响应头映射；重复 Set-Cookie 等字段保持多值。</summary>
internal sealed class PyHttpHeaders : PyNativeObject
{
    private readonly List<(string Name, string Value)> entries;
    internal PyHttpHeaders(IEnumerable<(string Name, string Value)> entries) => this.entries = entries.ToList();
    /// <inheritdoc/>
    public override string TypeName => "HTTPMessage";
    internal object? Get(string name, object? fallback) => entries.FirstOrDefault(entry => entry.Name.Equals(name, StringComparison.OrdinalIgnoreCase)).Value ?? fallback;
    internal object? Joined(string name, object? fallback)
    {
        var values = entries.Where(entry => entry.Name.Equals(name, StringComparison.OrdinalIgnoreCase)).Select(entry => entry.Value).ToArray();
        return values.Length == 0 ? fallback : string.Join(", ", values);
    }
    private PyList Items() => new(entries.Select(entry => (object?)new PyTuple([entry.Name, entry.Value])));
    /// <inheritdoc/>
    public override object? GetAttr(string name) => name switch
    {
        "get" => PyNativeFunctions.Create(name, ["name", "failobj"], [null, null], 1, args => Get(PyUrlParse.Text(args[0]), args[1])),
        "get_all" => PyNativeFunctions.Create(name, ["name", "failobj"], [null, null], 1, args =>
        {
            var values = entries.Where(entry => entry.Name.Equals(PyUrlParse.Text(args[0]), StringComparison.OrdinalIgnoreCase))
                .Select(entry => (object?)entry.Value).ToList();
            return values.Count > 0 ? new PyList(values) : args[1];
        }),
        "items" => PyNativeFunctions.Create(name, [], [], 0, _ => Items()),
        "keys" => PyNativeFunctions.Create(name, [], [], 0, _ => new PyList(entries.Select(entry => (object?)entry.Name))),
        "values" => PyNativeFunctions.Create(name, [], [], 0, _ => new PyList(entries.Select(entry => (object?)entry.Value))),
        "__len__" => PyNativeFunctions.Create(name, [], [], 0, _ => (long)entries.Count),
        "__getitem__" => PyNativeFunctions.Create(name, ["key"], [null], 1, args => Get(PyUrlParse.Text(args[0]), null)),
        "__contains__" => PyNativeFunctions.Create(name, ["key"], [null], 1, args => Get(PyUrlParse.Text(args[0]), null) is not null),
        "__iter__" => PyNativeFunctions.Create(name, [], [], 0, _ => PyIter.Create(new PyList(entries.Select(entry => (object?)entry.Name)))),
        _ => throw Missing(name),
    };
}
