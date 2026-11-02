// 原生属性表的每次导入尝试：只在成功时公布整张表，失败旧对象与后续重试严格隔离。
using System;
using System.Collections.Generic;

namespace RyuPy.Runtime;

internal sealed class PyNativeModuleAttempt
{
    private readonly string _name;
    private readonly Func<Dictionary<string, object?>> _factory;
    private readonly Action<string, object?>? _beforeSet;
    private Dictionary<string, object?>? _attributes;

    private PyNativeModuleAttempt(string name, Func<Dictionary<string, object?>> factory,
        Action<string, object?>? beforeSet)
    { _name = name; _factory = factory; _beforeSet = beforeSet; }

    internal static PyModule Create(string name, string package,
        Func<Dictionary<string, object?>> factory, Action<string, object?>? beforeSet)
    {
        var attempt = new PyNativeModuleAttempt(name, factory, beforeSet);
        return new PyModule(name, package, "", attempt.Initialize, attempt.Get, attempt.Set);
    }

    private void Initialize()
    {
        // Import 已先登记模块并设 State=1。这里不用 Lazy，不缓存异常，也不复制成功字典。
        Dictionary<string, object?> attributes = _factory()
            ?? throw new PyRuntimeError("native module factory returned no attribute table");
        _attributes = attributes;
    }

    private object? Get(string name) => _attributes is { } attributes && attributes.TryGetValue(name, out object? value)
        ? value : throw new PyAttributeError($"module '{_name}' has no attribute '{name}'");

    private void Set(PyModule module, string name, object? value)
    {
        // 新 API 不支持未完成表的发布/合并；普通 RegisterNative 的写入行为不受影响。
        if (_attributes is not { } attributes)
            throw new PyNotImplementedError("writing an initializing native module is not supported");
        _beforeSet?.Invoke(name, value);
        attributes[name] = value;
    }
}

public static partial class PyModules
{
    /// <summary>登记延迟原生表；仅在首次导入尝试时创建，失败允许重新调用工厂。</summary>
    /// <remarks>沿既有单线程导入模型；不增加导入锁，未完成尝试的属性写入明确拒绝。</remarks>
    public static void RegisterNativeFactory(string name, Func<Dictionary<string, object?>> factory,
        Action<string, object?>? beforeSet = null)
    {
        ArgumentNullException.ThrowIfNull(factory);
        // 此分支由 Import 的 NativeFactory 分派，不使用编译模块的 init/get/set 委托。
        Registry[name] = new Registration(name.Contains('.') ? name[..name.LastIndexOf('.')] : "", "",
            null, null, null, factory, beforeSet);
    }
}
