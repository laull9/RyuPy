// 编译loader公开冷协议：弱能力捕获不可变代码，来源字段修改不触发动态查表或源码重编译。
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace RyuPy.Runtime;

internal static partial class PyCompiledModuleLoader
{
    private sealed record Capability(PyCompiledModuleCode? Code, string Name, string Path);
    private static readonly ConditionalWeakTable<PyInstance, Capability> Capabilities = new();
    private static Dictionary<string, object?>? OriginalAttributes;
    private static readonly PyFunction CreateModuleFunction = PyNativeFunctions.Create("create_module",
        ["self", "spec"], [null, null], 2, args => CreateModule(args[0]));
    private static readonly PyFunction ExecModuleFunction = PyNativeFunctions.Create("exec_module",
        ["self", "module"], [null, null], 2, args => ExecuteModule(args[0], args[1]));

    internal static bool AttributesUnchanged(PyClass cls, Dictionary<string, object?> original)
    {
        if (cls.BaseClass is not null || cls.Attributes.Count != original.Count) return false;
        foreach (var (key, value) in original)
            if (!cls.Attributes.TryGetValue(key, out object? current) || !ReferenceEquals(value, current)) return false;
        return true;
    }

    // module_from_spec不绕过用户替换的方法；完整动态协议下一切实现，未知回调前明确拒绝。
    internal static void RequireOriginalProtocol(PyInstance loader)
    {
        if (!ReferenceEquals(loader.Class, LoaderClass.Value)
            || !AttributesUnchanged(loader.Class, OriginalAttributes!)
            || HasOwn(loader, "create_module") || HasOwn(loader, "exec_module"))
            throw Unsupported("custom loader protocols");
        _ = Source(loader);
    }

    private static bool HasOwn(PyInstance loader, string name) =>
        loader.Class.TryGetInstanceSlot(name, out int slot) && loader.TryGetSlot(slot, out _);

    private static Capability Source(object? self)
    {
        if (self is not PyInstance loader || !Capabilities.TryGetValue(loader, out var source))
            throw Unsupported("loaders without captured source capability");
        // 保存的bound method仍指向原实现；这里只检查会改变来源读取的协议，不因另一方法替换而失效。
        foreach (string name in new[] { "__getattribute__", "__getattr__", "name", "path" })
            if (loader.Class.TryLookup(name, out _, out _)) throw Unsupported("modified loader source protocols");
        if (!StringField(loader, "name", out string? currentName) || currentName != source.Name
            || !StringField(loader, "path", out string? currentPath) || currentPath != source.Path)
            throw Unsupported("modified loader source fields");
        return source;
    }

    internal static PyCompiledModuleCode RequireInstanceCode(PyInstance loader)
    {
        return Source(loader).Code ?? throw Unsupported("static compiled source execution");
    }

    private static object CreateModule(object? self)
    {
        _ = Source(self);
        return PyNone.Instance;
    }

    private static object ExecuteModule(object? self, object? target)
    {
        // 任意对象的属性访问可能运行回调，首切只接受真实模块对象。
        if (target is not PyModule module) throw Unsupported("non-module execution targets");
        object? name;
        try { name = module.GetAttr("__name__"); }
        catch (PyAttributeError missing)
        {
            missing.UserAttributes["name"] = "__name__";
            missing.UserAttributes["obj"] = module;
            throw (PyBaseException)PyErrors.PrepareRaise(missing);
        }
        var source = Source(self);
        if (name is not (null or PyNone or string)) throw Unsupported("custom module name protocols");
        if (name is string text && text != source.Name)
        {
            var error = new PyImportError($"loader for {source.Name} cannot handle {text}");
            error.UserAttributes["name"] = text;
            throw (PyBaseException)PyErrors.PrepareRaise(error);
        }
        // None沿CP采用loader名；不改目标__name__。错误名称必须先于builtins/目标存储检查。
        if (source.Code is not { } code) throw Unsupported("static compiled source execution");
        PyModuleInstances.Execute(code, module);
        return PyNone.Instance;
    }

    private static PyNotImplementedError Unsupported(string detail) =>
        (PyNotImplementedError)PyErrors.PrepareRaise(new PyNotImplementedError("compiled loader: " + detail + " are not implemented"));
}
