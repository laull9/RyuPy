using System;
using System.Collections.Generic;

namespace RyuPy.Runtime;

/// <summary>模块初始化委托：执行模块顶层代码。</summary>
public delegate void PyModuleInit();

/// <summary>读取模块变量的委托；名字未声明时抛 <see cref="PyAttributeError"/>。</summary>
public delegate object? PyModuleGet(string name);

/// <summary>写入模块变量的委托；名字未声明时交给运行时动态字典。</summary>
public delegate void PyModuleSet(PyModule module, string name, object? value);

/// <summary>
/// Python 模块对象（M2.7）。
/// </summary>
/// <remarks>
/// 模块变量在生成的程序集里是**静态槽位**，读写通过编译期生成的 getter / setter 委托完成，
/// 因此模块内部代码保持静态类型，外部访问看到的仍是 Python 值。
/// 未在模块里声明的属性（例如自动挂上去的子模块）放在动态字典里。
/// </remarks>
public sealed class PyModule
{
    /// <summary>点分绝对模块名；入口模块是 <c>__main__</c>。</summary>
    public string Name { get; }

    /// <summary><c>__package__</c>。</summary>
    public string Package { get; }

    /// <summary><c>__file__</c>。</summary>
    public string File { get; }

    /// <summary>0 = 未初始化，1 = 初始化中，2 = 已完成。</summary>
    internal int State;

    private readonly PyModuleInit init;
    private readonly PyModuleGet get;
    private readonly PyModuleSet set;

    /// <summary>未声明属性的动态字典（子模块、运行时挂载的属性）。</summary>
    internal Dictionary<string, object?> DynamicAttributes { get; } = new(StringComparer.Ordinal);

    internal PyModule(
        string name,
        string package,
        string file,
        PyModuleInit init,
        PyModuleGet get,
        PyModuleSet set
    )
    {
        Name = name;
        Package = package;
        File = file;
        this.init = init;
        this.get = get;
        this.set = set;
    }

    internal void RunInit() => init();

    /// <summary>读取模块属性：动态字典 → 特殊名字 → 编译期槽位。</summary>
    public object? GetAttr(string name)
    {
        if (DynamicAttributes.TryGetValue(name, out var dynamicValue))
        {
            return dynamicValue;
        }
        switch (name)
        {
            case "__name__":
                return Name;
            case "__package__":
                return Package;
            case "__file__":
                return File;
        }
        return get(name);
    }

    /// <summary>写模块属性：编译期槽位优先，未声明的名字进动态字典。</summary>
    public void SetAttr(string name, object? value) => set(this, name, value);

    /// <inheritdoc />
    public override string ToString() => $"<module '{Name}'>";
}

/// <summary>
/// 模块注册表与导入机制（M2.7）。
/// </summary>
/// <remarks>
/// 语义要点：
/// <list type="bullet">
/// <item>导入前先登记模块对象（初始化中），循环导入能看到部分初始化的状态；</item>
/// <item>重复导入返回同一个对象，模块代码只执行一次；</item>
/// <item>初始化抛异常时移除缓存条目，之后重试会重新执行模块代码；</item>
/// <item>未注册的模块名抛 <c>ModuleNotFoundError</c>（消息与 CPython 一致）。</item>
/// </list>
/// 注册表由生成的 <c>__init_functions</c> 在入口处填充，因此只有编译时可达的模块能被导入。
/// </remarks>
public static class PyModules
{
    private sealed class Registration(
        string package,
        string file,
        PyModuleInit init,
        PyModuleGet get,
        PyModuleSet set
    )
    {
        public string Package { get; } = package;
        public string File { get; } = file;
        public PyModuleInit Init { get; } = init;
        public PyModuleGet Get { get; } = get;
        public PyModuleSet Set { get; } = set;
    }

    private static readonly Dictionary<string, Registration> Registry = new(StringComparer.Ordinal);
    private static readonly Dictionary<string, PyModule> Cache = new(StringComparer.Ordinal);

    /// <summary>登记一个已编译模块。同名重复登记以最后一次为准。</summary>
    public static void Register(
        string name,
        string package,
        string file,
        PyModuleInit init,
        PyModuleGet get,
        PyModuleSet set
    )
    {
        Registry[name] = new Registration(package, file, init, get, set);
    }

    /// <summary>导入模块；已缓存或正在初始化时直接返回现有对象。</summary>
    public static PyModule Import(string name)
    {
        if (Cache.TryGetValue(name, out var cached))
        {
            return cached;
        }
        if (!Registry.TryGetValue(name, out var registration))
        {
            throw new PyModuleNotFoundError($"No module named '{name}'");
        }

        var module = new PyModule(
            name,
            registration.Package,
            registration.File,
            registration.Init,
            registration.Get,
            registration.Set
        );
        // 必须先登记再执行：循环导入的另一端需要看到「初始化中」的模块对象。
        Cache[name] = module;
        module.State = 1;
        try
        {
            module.RunInit();
            module.State = 2;
            return module;
        }
        catch
        {
            // 失败模块不能留在缓存里；已经成功导入的依赖保留。
            Cache.Remove(name);
            throw;
        }
    }

    /// <summary><c>from m import name</c>：先取属性，缺属性时尝试导入子模块。</summary>
    public static object? ImportAttr(string moduleName, string name)
    {
        var module = Import(moduleName);
        try
        {
            return module.GetAttr(name);
        }
        catch (PyAttributeError)
        {
            var submodule = moduleName + "." + name;
            if (Registry.ContainsKey(submodule))
            {
                var imported = Import(submodule);
                // CPython 会把子模块挂到父包上，后续 pkg.sub 也能取到。
                module.SetAttr(name, imported);
                return imported;
            }
            throw new PyImportError($"cannot import name '{name}' from '{moduleName}'");
        }
    }

    /// <summary>执行入口模块（<c>__main__</c>）。</summary>
    public static void RunMain() => Import("__main__");

    /// <summary>编译期字符串比较助手（生成的模块 getter / setter 用）。</summary>
    public static bool SameName(string left, string right) =>
        string.Equals(left, right, StringComparison.Ordinal);

    /// <summary>模块没有该属性时抛 <c>AttributeError</c>。</summary>
    public static void MissingAttr(string module, string name) =>
        throw new PyAttributeError($"module '{module}' has no attribute '{name}'");

    /// <summary>把未声明的模块属性写进动态字典。</summary>
    public static void SetDynamic(PyModule module, string name, object? value) =>
        module.DynamicAttributes[name] = value;
}
