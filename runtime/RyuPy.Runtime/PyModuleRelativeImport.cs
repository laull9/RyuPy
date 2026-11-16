// 实例相对from-import的两阶段协议；只消费真实globals与已登记来源，不调用未知finder。
using System;
namespace RyuPy.Runtime;

public static partial class PyModuleInstances
{
    /// <summary>先处理完整fromlist，尚不写调用者的任何别名。</summary>
    public static object RelativeImport(object context, string name, long level, object?[] fromlist)
    {
        ImportGuard(context);
        string package = RelativePackage(Context(context).State.Globals);
        if (package.Length == 0) throw new PyImportError("attempted relative import with no known parent package");
        if (level < 1) throw new PyRuntimeError("invalid compiled relative import level");
        for (long index = 1; index < level; index++)
        {
            int dot = package.LastIndexOf('.');
            if (dot < 0) throw new PyImportError("attempted relative import beyond top-level package");
            package = package[..dot];
        }
        string absolute = name.Length == 0 ? package : package + "." + name;
        return PyModules.RelativeFromList(absolute, fromlist);
    }

    // 即使package是合法字符串，CP也先消费spec.parent以检查不一致；非法package则先失败。
    private static string RelativePackage(PyDict globals)
    {
        globals.TryGet("__package__", out object? package);
        globals.TryGet("__spec__", out object? spec);
        if (package is not (null or PyNone or string)) throw new PyTypeError("package must be a string");
        string? parent = null;
        if (spec is not (null or PyNone))
        {
            if (spec is not PyInstance instance || !PyModuleSpec.IsOriginal(instance))
                throw RelativeUnsupported("custom or modified __spec__");
            // 原parent属性会读取这两个字段；先拒绝可能调用用户rpartition的对象。
            object? locations = PyModuleSpec.Get(instance, "submodule_search_locations");
            object? specName = PyModuleSpec.Get(instance, "name");
            if (specName is not string || locations is not (null or PyNone or PyList))
                throw RelativeUnsupported("custom __spec__ parent fields");
            parent = (string)PyModuleSpec.Get(instance, "parent")!;
        }
        if (package is string text)
        {
            if (parent is not null && text != parent)
                throw RelativeUnsupported("__package__ mismatch warning protocol");
            return text;
        }
        if (parent is not null) return parent;
        // CP在读取name/path之前先发ImportWarning；首切不静默略过此相位。
        throw RelativeUnsupported("missing package/spec fallback warning protocol");
    }

    /// <summary>第二阶段每个名字读取后立即绑定；不重新解析包、不再次检查hook。</summary>
    public static object? RelativeImportAttr(object context, object imported, string name)
    {
        _ = Context(context);
        if (imported is not PyModule module) throw new PyRuntimeError("invalid relative import module");
        return PyModules.RelativeAttribute(module, name);
    }
    private static PyNotImplementedError RelativeUnsupported(string detail) =>
        new("relative import: " + detail + " is not implemented");
}

public static partial class PyModules
{
    internal static PyModule RelativeFromList(string name, object?[] fromlist)
    {
        // 无负存在性证明的外部来源在父初始化前拒绝；不冒称ModuleNotFoundError。
        _ = SourceDiscoveryRegistration(name);
        PyModule module = ImportDiscoveryParent(name);
        bool package;
        try { _ = module.GetAttr("__path__"); package = true; }
        catch (PyAttributeError) { package = false; }
        if (!package) return module;
        foreach (object? item in fromlist)
        {
            if (item is not string member) throw new PyRuntimeError("invalid compiled fromlist");
            try { _ = module.GetAttr(member); continue; }
            catch (PyAttributeError) { }
            // 只有当前缺名才进入发现；不能预检后项而跳过前项应有的初始化。
            object? publicName = module.GetAttr("__name__");
            if (publicName is not string currentName)
                throw new PyNotImplementedError("relative import: non-string package __name__ is not implemented");
            string child = currentName + "." + member;
            // 缓存命中不消费finder/path；未命中才由真实目标的父链检查实际路径。
            if (!Cache.ContainsKey(child)) _ = SourceDiscoveryRegistration(child);
            try { _ = ImportDiscoveryParent(child); }
            catch (PyModuleNotFoundError missing)
            {
                // CP在处理missing期间读取真实name；getter失败不能伪装成默认None。
                object? previous = PyErrors.EnterHandler(missing);
                try
                {
                    object? failedName;
                    try { failedName = PyObject.ReadImportErrorName(missing, requireComparableName: true); }
                    catch (PyBaseException lookup) { PyErrors.PrepareRaise(lookup); throw; }
                    if (failedName is null or PyNone) throw;
                    if (failedName is not string failed)
                        throw new PyNotImplementedError("relative import: custom ModuleNotFoundError.name comparison is not implemented") { Context = missing };
                    if (failed != child) throw;
                    // 同名缺失进入第二阶段；现缓存不开放sys.modules[name]=None协议。
                }
                finally { PyErrors.LeaveHandler(previous); }
            }
        }
        return module;
    }

    internal static object? RelativeAttribute(PyModule module, string name)
    {
        try { return module.GetAttr(name); }
        catch (PyAttributeError)
        {
            // 循环导入时子模块仍在初始化，父属性尚未发布；CP IMPORT_FROM可取缓存中的同一对象。
            object? publicName = module.GetAttr("__name__");
            if (publicName is not string moduleName)
                throw new PyNotImplementedError("relative import: non-string module __name__ is not implemented");
            if (Cache.TryGetValue(moduleName + "." + name, out PyModule? partial)) return partial;
            // 不重启子模块发现；第一阶段已经完成。普通非包缺export仍是ImportError。
            throw new PyImportError($"cannot import name '{name}' from '{moduleName}'");
        }
    }
}
