// 模块对象与普通导入机制；受限缓存别名的校验和来源记录隔离在 CacheAliases 冷路。
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
/// legacy模块变量通过编译期getter/setter访问静态槽，未声明属性保留动态字典。
/// 显式实例模块复用委托接口，全部公开属性只进入自身唯一PyDict；不复制静态槽。
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
    private readonly bool fileBackedByGlobal;
    private readonly bool legacyResourceMetadata;
    // 真正字典存储独立于来源元数据；公开新模块不伪造Metadata来启用槽语义。
    private readonly bool instanceGlobals;

    internal PyModuleMetadata? Metadata { get; set; }

    /// <summary>未声明属性的动态字典（子模块、运行时挂载的属性）。</summary>
    internal Dictionary<string, object?> DynamicAttributes { get; } = new(StringComparer.Ordinal);

    internal PyModule(
        string name,
        string package,
        string file,
        PyModuleInit init,
        PyModuleGet get,
        PyModuleSet set,
        bool fileBackedByGlobal = false,
        bool legacyResourceMetadata = false,
        bool instanceGlobals = false
    )
    {
        this.instanceGlobals = instanceGlobals;
        this.fileBackedByGlobal = fileBackedByGlobal;
        this.legacyResourceMetadata = legacyResourceMetadata;
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
        if (instanceGlobals) return get(name);
        if (DynamicAttributes.TryGetValue(name, out var dynamicValue))
        {
            return dynamicValue;
        }
        if (PyModuleMetadata.IsName(name))
        {
            // 旧十参资源ABI没有元数据槽；保持缺属性和getter副作用，不冒领新能力。
            if (legacyResourceMetadata) return get(name);
            // 真实编译槽即使未绑定也不回退初始快照，外部读写与模块内部保持同一状态。
            if (fileBackedByGlobal && Metadata is not null) return get(name);
            try { return get(name); }
            catch (PyAttributeError) when (Metadata is not null) { return Metadata.Attribute(name); }
            catch (PyAttributeError) when (name is "__spec__" or "__loader__")
            { throw new PyNotImplementedError("module spec/loader metadata requires an explicitly registered source module"); }
        }
        switch (name)
        {
            case "__name__":
                return fileBackedByGlobal && Metadata is not null ? get(name) : Name;
            case "__package__":
                return fileBackedByGlobal && Metadata is not null ? get(name) : Package;
            case "__file__":
                // 编译器明确声明的槽是可写 Python 值；未绑定错误不能被初始路径吞掉。
                return fileBackedByGlobal ? get(name) : File;
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
public static partial class PyModules
{
    private sealed class Registration(
        string package,
        string file,
        PyModuleInit init,
        PyModuleGet get,
        PyModuleSet set,
        string? resolvedFile = null,
        bool fileBackedByGlobal = false,
        ResourceLocation? resourceLocation = null,
        bool metadataEnabled = false,
        PyCompiledModuleCode? instanceCode = null
    )
    {
        public ResourceLocation? ResourceLocation { get; } = resourceLocation;
        public bool MetadataEnabled { get; } = metadataEnabled;
        public PyCompiledModuleCode? InstanceCode { get; } = instanceCode;
        public string Package { get; } = package;
        // File 仅用于原诊断来源映射，不改 warnings 的既有 basename 合同。
        public string File { get; } = file;
        public string ResolvedFile { get; } = resolvedFile ?? file;
        public bool FileBackedByGlobal { get; } = fileBackedByGlobal;
        public PyModuleInit Init { get; } = init;
        public PyModuleGet Get { get; } = get;
        public PyModuleSet Set { get; } = set;
    }

    private static readonly Dictionary<string, Registration> Registry = new(StringComparer.Ordinal);
    private static readonly Dictionary<string, PyModule> Cache = new(StringComparer.Ordinal);

    /// <summary>类定义冷路读取已注册模块的真实全局槽，不导入、不借缓存模块的固定 Name。</summary>
    internal static bool TryGetClassDefinitionModule(string key, out object? value)
    {
        value = null;
        if (!Registry.TryGetValue(key, out var registration)) return false;
        // 任意值均保原引用；真实 getter 错误不能被当作未知来源吞掉。
        value = registration.Get("__name__");
        return true;
    }

    /// <summary>从帧文件标识查唯一模块；当前编译器可能只存 basename，歧义不能猜测。</summary>
    public static string? ModuleForFile(string file)
    {
        string? found = null;
        foreach (var pair in Registry)
        {
            if (pair.Value.File.Length == 0 || !string.Equals(pair.Value.File, file, StringComparison.Ordinal)) continue;
            if (found is not null)
                throw new PyNotImplementedError("warning source file maps to multiple modules");
            found = pair.Key;
        }
        return found;
    }

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
        EnsureRegistrationAvoidsCacheAliases(name);
        Registry[name] = new Registration(package, file, init, get, set);
    }

    /// <summary>登记原生模块属性；对象缓存与普通编译模块共享，属性写入保持实时可见。</summary>
    public static void RegisterNative(string name, Dictionary<string, object?> attributes, Action<string, object?>? beforeSet = null)
    {
        Register(name, name.Contains('.') ? name[..name.LastIndexOf('.')] : "", "", () => { },
            key => attributes.TryGetValue(key, out object? value) ? value
                : throw new PyAttributeError($"module '{name}' has no attribute '{key}'"),
            (_, key, value) => { beforeSet?.Invoke(key, value); attributes[key] = value; });
    }

    internal static Dictionary<string, PyModule> CacheSnapshot() => new(Cache, StringComparer.Ordinal);
    internal static bool TryCached(string name, out PyModule? module) => Cache.TryGetValue(name, out module);

    /// <summary>导入模块；已缓存或正在初始化时直接返回现有对象。</summary>
    /// <remarks>
    /// 与 CPython 一致，点分模块先初始化父包（例如 <c>a.b.c</c> 依次初始化 <c>a</c>、
    /// <c>a.b</c>）。父包初始化期间可能已经反向导入了本模块，因此父包返回后要
    /// **重新检查缓存**，避免重复执行模块代码。
    /// </remarks>
    public static PyModule Import(string name)
    {
        if (Cache.TryGetValue(name, out var cached))
        {
            return cached;
        }

        PyModule? parent = null;
        string? childName = null;
        int dot = name.LastIndexOf('.');
        if (dot > 0)
        {
            parent = Import(name[..dot]);
            childName = name[(dot + 1)..];
            // 父包初始化时可能已经通过 `from . import child` 导入了本模块。
            if (Cache.TryGetValue(name, out var early))
            {
                return early;
            }
        }

        if (!Registry.TryGetValue(name, out var registration))
        {
            RejectCacheAliasDescendant(name);
            throw new PyModuleNotFoundError($"No module named '{name}'");
        }

        var module = registration.InstanceCode is { } code ? PyModuleInstances.Create(code) : new PyModule(
            name,
            registration.Package,
            registration.ResolvedFile,
            registration.Init,
            registration.Get,
            registration.Set,
            registration.FileBackedByGlobal,
            registration.ResourceLocation is not null && !registration.MetadataEnabled
        );
        if (registration.InstanceCode is null && registration.MetadataEnabled && registration.ResourceLocation is { Kind: 1 or 2 } source)
            module.Metadata = new PyModuleMetadata(name, registration.ResolvedFile, source.IsPackage);
        // 必须先登记再执行：循环导入的另一端需要看到「初始化中」的模块对象。
        if (registration.ResourceLocation is { } resourceLocation)
            ResourceLocations.Add(module, resourceLocation);
        Cache[name] = module;
        module.State = 1;
        try
        {
            module.RunInit();
            module.State = 2;
        }
        catch
        {
            // 失败模块不能留在缓存里；已经成功导入的依赖保留。
            if (Cache.TryGetValue(name, out var current) && ReferenceEquals(current, module)) Cache.Remove(name);
            throw;
        }

        // 成功导入后挂到父包上，`pkg.sub` 才能像 CPython 一样直接取到子模块。
        if (parent is not null && childName is not null)
        {
            parent.SetAttr(childName, module);
        }
        return module;
    }

    /// <summary><c>from m import name</c>：先取属性，缺属性时尝试导入子模块。</summary>
    public static object? ImportAttr(string moduleName, string name)
    {
        var module = Import(moduleName);
        try
        {
            return module.GetAttr(name);
        }
        catch (PyAttributeError missing)
        {
            if (CacheAliasNames?.Contains(moduleName) == true)
            {
                // 已有属性仍走原对象；未知子模块域先拒绝，保留 getter 缺属性与外层异常链。
                PyErrors.Prepare(missing);
                throw CacheAliasUnsupported("missing attributes cannot fall back to alias submodules", missing);
            }
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

    /// <summary>执行入口模块（<c>__main__</c>）；开启 <c>RYUPY_STATS</c> 时从这里开始计执行耗时。</summary>
    public static void RunMain()
    {
        PyStats.MarkMainStart();
        Import("__main__");
    }

    /// <summary>编译期字符串比较助手（生成的模块 getter / setter 用）。</summary>
    public static bool SameName(string left, string right) =>
        string.Equals(left, right, StringComparison.Ordinal);

    /// <summary>模块没有该属性时抛 <c>AttributeError</c>。</summary>
    public static void MissingAttr(string module, string name) =>
        throw new PyAttributeError($"module '{module}' has no attribute '{name}'");

    /// <summary>legacy未声明属性写动态字典；实例模块仍写唯一globals。</summary>
    public static void SetDynamic(PyModule module, string name, object? value)
    {
        if (PyModuleInstances.IsInstance(module)) module.SetAttr(name, value);
        else module.DynamicAttributes[name] = value;
    }
}
