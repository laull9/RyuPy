// ModuleSpec 是稳定的普通 Python 类；字段存实例槽，属性在消费时才读取并保留对象身份。
using System;
using System.Collections.Generic;

namespace RyuPy.Runtime;

/// <summary>importlib.machinery 的真实 ModuleSpec 普通类导出。</summary>
public static partial class PyModuleSpec
{
    private static readonly Lazy<PyClass> SpecClass = new(Build);
    internal static PyClass Type => SpecClass.Value;
    private static Dictionary<string, object?>? OriginalAttributes;

    // 冷公开创建入口在首次未知描述符/回调之前拒绝；比较真实属性身份允许恢复原定义。
    internal static bool IsOriginal(PyInstance spec) => ReferenceEquals(spec.Class, Type)
        && PyCompiledModuleLoader.AttributesUnchanged(Type, OriginalAttributes!);
    /// <summary>每次模块登记共享同一个已构造完成的公开类型。</summary>
    public static Dictionary<string, object?> CreateAttributes() => new() { ["ModuleSpec"] = Type };

    private static PyClass Build()
    {
        var cls = new PyClass("ModuleSpec") { NativeDefinitionModule = "_frozen_importlib" };
        cls.SetAttr("__module__", "_frozen_importlib");
        cls.SetAttr("__init__", PyNativeFunctions.Create("ModuleSpec.__init__",
            ["self", "name", "loader", "origin", "loader_state", "is_package"],
            [null, null, null, PyNone.Instance, PyNone.Instance, PyNone.Instance], 3, Initialize,
            ["pos", "pos", "pos", "kwonly", "kwonly", "kwonly"]));
        Property(cls, "parent", Parent);
        Property(cls, "has_location", s => Get(s, "_set_fileattr"),
            (s, value) => Put(s, "_set_fileattr", PyOps.Truthy(value)));
        Property(cls, "cached", Cached, (s, value) => Put(s, "_cached", value));
        AddObjectProtocol(cls);
        PyOps.FinalizeOrdinaryClass(cls);
        OriginalAttributes = new(cls.Attributes, StringComparer.Ordinal);
        return cls;
    }

    private static object? Initialize(object?[] args)
    {
        object self = args[0]!;
        Put(self, "name", args[1]); Put(self, "loader", args[2]);
        Put(self, "origin", args[3]); Put(self, "loader_state", args[4]);
        // 参数绑定已经完成；用户真值异常原样传播，不能先校验或字符串化其它字段。
        Put(self, "submodule_search_locations", PyOps.Truthy(args[5]) ? new PyList([]) : PyNone.Instance);
        Put(self, "_uninitialized_submodules", new PyList([]));
        Put(self, "_set_fileattr", false); Put(self, "_cached", PyNone.Instance);
        return PyNone.Instance;
    }

    private static void Property(PyClass cls, string name, Func<object, object?> read,
        Action<object, object?>? write = null)
    {
        var getter = PyNativeFunctions.Create(name, ["self"], [null], 1, a => read(a[0]!));
        object? setter = write is null ? null : PyNativeFunctions.Create(name, ["self", "value"],
            [null, null], 2, a => { write(a[0]!, a[1]); return PyNone.Instance; });
        cls.SetAttr(name, new PyProperty(getter, setter));
    }

    internal static object? Get(object self, string name) => PyObject.GetAttr(self, name);
    internal static void Put(object self, string name, object? value) => PyObject.SetAttr(self, name, value);

    private static object? Parent(object self)
    {
        object? locations = Get(self, "submodule_search_locations");
        object? name = Get(self, "name");
        if (locations is not (null or PyNone)) return name;
        // 使用实际属性/调用协议，任意名字在构造时合法，错误延迟到消费 rpartition。
        object? partition = PyOps.CallMethod(name, "rpartition", ["."], [], []);
        return PyOps.GetItem(partition, 0L);
    }

    private static object? Cached(object self)
    {
        // 条件、计算与返回分别触发属性协议；子类描述符可能每次返回不同值或记录副作用。
        if (Get(self, "_cached") is null or PyNone)
        {
            if (Get(self, "origin") is not (null or PyNone) && PyOps.Truthy(Get(self, "_set_fileattr")))
                Put(self, "_cached", SourceCache(Get(self, "origin")!));
        }
        return Get(self, "_cached");
    }

    // 固定 CP3.12、optimize=0 的宿主源路径合同；纯路径运算，不检查/生成任何 pyc。
    private static object SourceCache(object origin)
    {
        if (origin is not string path)
        {
            // 缺少 endswith 的对象仍在 CP 的首次消费相位报 AttributeError；合法自定义协议明确延期。
            _ = PyObject.GetAttr(origin, "endswith");
            throw new PyNotImplementedError("ModuleSpec.cached custom origin protocols are not implemented");
        }
        if (path.EndsWith(".pyc", StringComparison.Ordinal)) return origin;
        if (!path.EndsWith(".py", StringComparison.Ordinal)
            && !(OperatingSystem.IsWindows() && path.EndsWith(".pyw", StringComparison.Ordinal))) return PyNone.Instance;
        int slash = path.LastIndexOf('/');
        if (OperatingSystem.IsWindows()) slash = Math.Max(slash, path.LastIndexOf('\\'));
        string head = slash < 0 ? "" : path[..(slash + 1)];
        if (!OperatingSystem.IsWindows())
        {
            // CP 的 _path_split 去掉分隔符，_path_join 忽略空head并去掉尾分隔符。
            string parent = slash < 0 ? "" : path[..slash];
            head = parent.Length == 0 ? "" : parent.TrimEnd('/') + "/";
        }
        string tail = path[(slash + 1)..];
        int dot = tail.LastIndexOf('.');
        string stem = dot == 0 ? tail[1..] : tail[..dot];
        return head + "__pycache__" + System.IO.Path.DirectorySeparatorChar + stem + ".cpython-312.pyc";
    }

    internal static PyInstance New(string name, object loader, string origin, bool isPackage)
    {
        return (PyInstance)PyOps.CallValue(Type, [name, loader],
            ["origin", "is_package"], [origin, isPackage])!;
    }
}
