// 生成程序集初始化期的声明模块注册；预检也保护已有缓存别名，普通旧注册覆盖规则不变。
using System;

namespace RyuPy.Runtime;

public static partial class PyModules
{
    /// <summary>只读检查，不创建占位；必须在生成程序集任何公开槽写入前调用。</summary>
    public static void EnsureDeclaredBindingAvailable(string name)
    {
        EnsureRegistrationAvoidsCacheAliases(name);
        foreach (string existing in Registry.Keys)
            if (ConflictsWithDeclaredBinding(existing, name)) throw DeclaredBindingConflict(name);
        foreach (string existing in Cache.Keys)
            if (ConflictsWithDeclaredBinding(existing, name)) throw DeclaredBindingConflict(name);
    }

    /// <summary>重复检查后一次Add；无伪源码文件，__file__由实际getter/后续动态赋值负责。</summary>
    public static void RegisterDeclaredBinding(string name, PyModuleInit init, PyModuleGet get, PyModuleSet set)
    {
        EnsureDeclaredBindingAvailable(name);
        object? Read(string key)
        {
            try { return get(key); }
            // 声明生成的synthetic模块没有源码metadata；真实importlib稍后填本次spec/loader。
            catch (PyAttributeError) when (key is "__spec__" or "__loader__") { return PyNone.Instance; }
        }
        Registry.Add(name, new Registration("", "", init, Read, set, fileBackedByGlobal: true));
        PyProvider.Current?.Registered(name);
    }

    private static bool ConflictsWithDeclaredBinding(string existing, string name) =>
        string.Equals(existing, name, StringComparison.Ordinal)
        || existing.StartsWith(name + ".", StringComparison.Ordinal);

    private static PyBaseException DeclaredBindingConflict(string name) =>
        (PyBaseException)PyErrors.PrepareRaise(new PyImportError($"declared native module '{name}' conflicts with an existing module"));
}
