// 有限不可变入口对象；使用既有native对象/迭代/索引桥，不借tuple整数下标快路。
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
namespace RyuPy.Runtime;
/// <summary>单个惰性入口描述符。</summary>
public sealed class PyEntryPoint : PyNativeObject
{
    /// <summary>实际公开类型，构造暂未开放。</summary>
    public static readonly PyNativeType Type = new("EntryPoint", "importlib.metadata", null, value => value is PyEntryPoint);
    private readonly PyModule owner;
    /// <summary>入口名称。</summary>
    public string Name { get; }
    /// <summary>原始加载目标文本。</summary>
    public string Value { get; }
    /// <summary>原始分组名称。</summary>
    public string Group { get; }
    internal PyEntryPoint(PyModule owner, string name, string value, string group)
    { this.owner = owner; Name = name; Value = value; Group = group; }
    /// <summary>Python类型名。</summary>
    public override string TypeName => "EntryPoint";
    /// <summary>已支持的属性与真实绑定方法。</summary>
    public override object? GetAttr(string name) => name switch
    {
        "__class__" => Type,
        "name" => Name, "value" => Value, "group" => Group,
        "load" => PyNativeFunctions.Create("load", [], [], 0, _ => Load()),
        "__hash__" or "__eq__" or "__ne__" or "module" or "attr" or "extras" or "matches" or "dist"
            => throw PyDistributionSnapshot.Unsupported("EntryPoint." + name),
        _ => throw Missing(name),
    };
    /// <summary>读取所属metadata模块的实时import_module并逐层访问属性。</summary>
    public object? Load()
    {
        if (Value.Contains('[')) throw PyDistributionSnapshot.Unsupported("entry point extras loading");
        var match = Regex.Match(Value, @"^(?<module>[\w.]+)\s*(:\s*(?<attr>[\w.]+)\s*)?$", RegexOptions.CultureInvariant);
        if (!match.Success) throw new PyAttributeError("'NoneType' object has no attribute 'group'");
        object? target = PyOps.CallValue(owner.GetAttr("import_module"), [match.Groups["module"].Value], [], []);
        if (match.Groups["attr"].Success)
            foreach (string attribute in match.Groups["attr"].Value.Split('.'))
                if (attribute.Length != 0) target = PyObject.GetAttr(target, attribute);
        return target;
    }
}
/// <summary>保序不可变入口选择结果。</summary>
public sealed class PyEntryPoints : PyNativeObject
{
    /// <summary>实际公开类型，构造暂未开放。</summary>
    public static readonly PyNativeType Type = new("EntryPoints", "importlib.metadata", null, value => value is PyEntryPoints);
    private readonly PyEntryPoint[] entries;
    internal PyEntryPoints(PyEntryPoint[] entries) => this.entries = entries;
    /// <summary>Python类型名。</summary>
    public override string TypeName => "EntryPoints";
    /// <summary>已支持的属性与真实绑定方法。</summary>
    public override object? GetAttr(string name) => name switch
    {
        "__class__" => Type,
        "__len__" => PyNativeFunctions.Create("__len__", [], [], 0, _ => (long)entries.Length),
        "__iter__" => PyNativeFunctions.Create("__iter__", [], [], 0, _ => PyIter.Create(new PyTuple(entries))),
        "__getitem__" => PyNativeFunctions.Create("__getitem__", ["name"], [null], 1, values => Item(values[0])),
        "select" => PyImportlibMetadata.Keywords("select", Select),
        "__hash__" or "__eq__" or "__ne__" or "names" or "groups"
            => throw PyDistributionSnapshot.Unsupported("EntryPoints." + name),
        _ => throw Missing(name),
    };
    private object Item(object? key)
    {
        if (key is not string name) throw PyDistributionSnapshot.Unsupported("EntryPoints non-string index");
        foreach (var entry in entries) if (entry.Name == name) return entry;
        throw PyKeyError.ForKey(name);
    }
    internal PyEntryPoints Select(string[] names, object?[] values)
    {
        var seen = new HashSet<string>(StringComparer.Ordinal);
        for (int index = 0; index < names.Length; index++)
        {
            if (!seen.Add(names[index])) throw new PyTypeError("select() got duplicate keyword '" + names[index] + "'");
            if (names[index] is not ("name" or "group") || values[index] is not string)
                throw PyDistributionSnapshot.Unsupported("select supports exact-string name/group only");
        }
        var result = new List<PyEntryPoint>();
        foreach (var entry in entries)
        {
            bool matches = true;
            for (int index = 0; index < names.Length; index++)
                matches &= (names[index] == "name" ? entry.Name : entry.Group) == (string)values[index]!;
            if (matches) result.Add(entry);
        }
        return new PyEntryPoints(result.ToArray());
    }
}
