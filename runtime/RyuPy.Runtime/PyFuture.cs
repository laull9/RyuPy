using System;
using System.Collections.Generic;

namespace RyuPy.Runtime;

/// <summary>固定 CPython 3.12.13 的 future 特性事实元数据；启用语义由 HIR 明确验证，不由运行时猜测。</summary>
public static class PyFuture
{
    private static readonly Lazy<Dictionary<string, object?>> Attributes = new(Build);
    /// <summary>特性对象保持同一模块内的稳定身份；不是运行期编译标志开关。</summary>
    public static Dictionary<string, object?> CreateAttributes() => Attributes.Value;

    private static Dictionary<string, object?> Build()
    {
        var result = new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["_Feature"] = new PyNativeType("_Feature", "__future__", null, value => value is Feature),
        };
        Add("nested_scopes", Release(2, 1, "beta", 1), Release(2, 2, "alpha", 0), 16);
        Add("generators", Release(2, 2, "alpha", 1), Release(2, 3, "final", 0), 0);
        Add("division", Release(2, 2, "alpha", 2), Release(3, 0, "alpha", 0), 131072);
        Add("absolute_import", Release(2, 5, "alpha", 1), Release(3, 0, "alpha", 0), 262144);
        Add("with_statement", Release(2, 5, "alpha", 1), Release(2, 6, "alpha", 0), 524288);
        Add("print_function", Release(2, 6, "alpha", 2), Release(3, 0, "alpha", 0), 1048576);
        Add("unicode_literals", Release(2, 6, "alpha", 2), Release(3, 0, "alpha", 0), 2097152);
        Add("barry_as_FLUFL", Release(3, 1, "alpha", 2), Release(4, 0, "alpha", 0), 4194304);
        Add("generator_stop", Release(3, 5, "beta", 1), Release(3, 7, "alpha", 0), 8388608);
        Add("annotations", Release(3, 7, "beta", 1), PyNone.Instance, 16777216);
        var names = new PyList();
        foreach (string key in result.Keys) if (key != "_Feature") names.Items.Add(key);
        result["all_feature_names"] = names;
        return result;

        void Add(string name, object optional, object mandatory, long flag) =>
            result.Add(name, new Feature(optional, mandatory, flag));
    }

    private static PyTuple Release(long major, long minor, string level, long serial) =>
        new([major, minor, 0L, level, serial]);

    /// <summary>只读特性元数据；完整 _Feature 用户构造与属性改写不在本次范围。</summary>
    private sealed class Feature(object optional, object mandatory, long flag) : PyNativeObject
    {
        public override string TypeName => "_Feature";
        public override object? GetAttr(string name) => name switch
        {
            "optional" => optional, "mandatory" => mandatory, "compiler_flag" => flag,
            "getOptionalRelease" => PyNativeFunctions.Create(name, [], [], 0, _ => optional),
            "getMandatoryRelease" => PyNativeFunctions.Create(name, [], [], 0, _ => mandatory),
            _ => throw Missing(name),
        };
        public override string PythonRepr() => $"_Feature({PyRepr.Repr(optional)}, {PyRepr.Repr(mandatory)}, {flag})";
    }
}
