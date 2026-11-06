// 每次导入尝试的初始元数据：先创建、后发布缓存，再由生成前言复制到真实全局槽。
using System;
using System.Collections.Generic;
using System.IO;

namespace RyuPy.Runtime;

internal sealed class PyModuleMetadata
{
    private readonly Dictionary<string, object?> values = new(StringComparer.Ordinal);
    internal readonly PyInstance? Spec;
    private readonly object? loader;
    private readonly string name;
    private readonly string origin;
    private readonly object? cached;
    private readonly PyList? locations;
    private readonly string? directory;

    internal PyModuleMetadata(string name, string origin, bool isPackage, PyCompiledModuleCode? code = null)
    {
        this.name = name; this.origin = origin;
        if (name == "__main__")
        {
            values["__spec__"] = PyNone.Instance; values["__loader__"] = PyNone.Instance;
            values["__cached__"] = PyNone.Instance; values["__path__"] = PyUnbound.Instance;
            return;
        }
        Spec = CreateSpec(name, origin, isPackage, code);
        loader = PyModuleSpec.Get(Spec, "loader");
        cached = PyModuleSpec.Get(Spec, "cached");
        values["__spec__"] = Spec; values["__loader__"] = loader; values["__cached__"] = cached;
        if (isPackage)
        {
            locations = (PyList)PyModuleSpec.Get(Spec, "submodule_search_locations")!;
            directory = (string)locations.GetItem(0L)!;
        }
        values["__path__"] = locations ?? (object)PyUnbound.Instance;
    }

    // 查询只新建真实规格，不建立模块/缓存，也不执行目标或改变父包属性。
    internal static PyInstance CreateSpec(string name, string origin, bool isPackage, PyCompiledModuleCode? code = null)
    {
        PyInstance spec = PyModuleSpec.New(name, PyCompiledModuleLoader.Create(name, origin, code), origin, isPackage);
        PyModuleSpec.Put(spec, "has_location", true);
        if (isPackage)
            ((PyList)PyModuleSpec.Get(spec, "submodule_search_locations")!).Append(Path.GetDirectoryName(origin) ?? "");
        return spec;
    }

    // 已编译查找域不重解释被替换/扩展的路径；缓存命中无需进入本守卫。
    internal bool SearchPathUnchanged(object? path) => ReferenceEquals(path, locations)
        && locations is { Count: 1 } && locations.GetItem(0L) is string value
        && string.Equals(value, directory, StringComparison.Ordinal);

    internal static bool IsName(string name) => name is "__spec__" or "__loader__" or "__cached__" or "__path__";
    internal object? Initial(string name) => values.TryGetValue(name, out object? value) ? value
        : throw new PyRuntimeError("unknown module metadata initialization field");

    internal object? Attribute(string name)
    {
        object? value = Initial(name);
        if (value is PyUnbound) throw new PyAttributeError($"module '{this.name}' has no attribute '{name}'");
        return value;
    }

    // 资源入口不重解释用户改过的spec/loader；每次尝试只接受自己的真实来源身份和原属性。
    internal bool ResourceUnchanged(PyModule module)
    {
        if (Spec is null || !ReferenceEquals(module.GetAttr("__spec__"), Spec)
            || !ReferenceEquals(module.GetAttr("__loader__"), loader)) return false;
        return ReferenceEquals(PyModuleSpec.Get(Spec, "name"), name)
            && ReferenceEquals(PyModuleSpec.Get(Spec, "loader"), loader)
            && ReferenceEquals(PyModuleSpec.Get(Spec, "origin"), origin)
            && ReferenceEquals(PyModuleSpec.Get(Spec, "cached"), cached)
            && PyModuleSpec.Get(Spec, "has_location") is true
            && ReferenceEquals(PyModuleSpec.Get(Spec, "submodule_search_locations"), locations)
            && locations is { Count: 1 } && ReferenceEquals(locations.GetItem(0L), directory);
    }
}

public static partial class PyModules
{
    /// <summary>仅模块入口前言调用，返回本次缓存对象初始元数据；普通读取仍是直接全局槽。</summary>
    public static object? ModuleMetadata(string name, string attribute) =>
        Cache.TryGetValue(name, out PyModule? module) && module.Metadata is { } metadata
            ? metadata.Initial(attribute)
            : throw new PyNotImplementedError("module metadata requires an explicitly registered source module");
}
