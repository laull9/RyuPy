// 普通动态import_module只解析精确字符串名；复用注册导入生命周期，不实现磁盘finder。
using System;
using System.Collections.Generic;
using System.Numerics;
namespace RyuPy.Runtime;
/// <summary>普通精确字符串动态导入入口。</summary>
public static class PyImportlib
{
    private static readonly PyFunction ImportFunction = PyNativeFunctions.Create("import_module",
        ["name", "package"], [null, PyNone.Instance], 1, values => ImportModule(values[0], values[1]));
    /// <summary>父importlib公开属性。</summary>
    public static Dictionary<string, object?> CreateAttributes() => new() { ["import_module"] = ImportFunction };
    /// <summary>解析绝对或相对名，复用已登记模块生命周期。</summary>
    public static PyModule ImportModule(object? name, object? package)
    {
        if (name is not string text)
        {
            if (name is null or PyNone or long or int or double or bool or BigInteger)
                throw new PyAttributeError($"'{PyRepr.TypeName(name)}' object has no attribute 'startswith'");
            throw Unsupported("custom or subclass module names");
        }
        if (text.StartsWith('.'))
        {
            // 绝对名完全不碰package；相对名只接受无需用户协议的有限值。
            if (package is PyInstance or PyNativeObject or PyStringInstance)
                throw Unsupported("custom or subclass relative packages");
            if (package is not (null or PyNone or string or long or int or double or bool or BigInteger or PyList or PyTuple or PyDict or PySet or PyFrozenSet or PyBytes or PyByteArray))
                throw Unsupported("custom relative packages");
            if (!PyOps.Truthy(package))
                throw new PyTypeError($"the 'package' argument is required to perform a relative import for {PyRepr.Repr(text)}");
            if (package is not string parent) throw new PyTypeError("__package__ not set to a string");
            int level = 0;
            while (level < text.Length && text[level] == '.') level++;
            for (int index = 1; index < level; index++)
            {
                int dot = parent.LastIndexOf('.');
                if (dot < 0) throw new PyImportError("attempted relative import beyond top-level package");
                parent = parent[..dot];
            }
            text = level == text.Length ? parent : parent + "." + text[level..];
        }
        if (text.Length == 0) throw new PyValueError("Empty module name");
        return PyModules.Import(text);
    }
    internal static PyNotImplementedError Unsupported(string detail) => new("importlib: " + detail + " are not implemented");
}
