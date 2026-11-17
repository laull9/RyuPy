// 外来模块的中性冷协议：保持真实字典视图及sys.modules三态，普通PyDict不承担后端分派。
namespace RyuPy.Runtime;

/// <summary>真实模块字典的活视图；实现方负责所有外部读写的身份与异常语义。</summary>
public interface IPyModuleNamespace
{
    /// <summary>Python可观察的同一真实字典对象，不得返回镜像或快照。</summary>
    object ViewObject { get; }
    /// <summary>缺键返回false；已绑定None或其它值返回true并保原对象。</summary>
    bool TryGet(string name, out object? value);
    /// <summary>写入权威字典。</summary>
    void Set(string name, object? value);
    /// <summary>只删真实键；缺键返回false，不执行同名属性回退。</summary>
    bool DeleteIfPresent(string name);
}

/// <summary>严格导入的可选能力；不改变旧IPyModuleProvider的模块返回ABI。</summary>
public interface IPyStrictImportProvider
{
    /// <summary>缺键false；None阻塞及任意替换对象均true，不能强转为模块。</summary>
    bool TryGetSysModuleEntry(string name, out object? entry);
    /// <summary>只适配所属解释器已验证的精确dict；输出ViewObject须与输入同一对象，不复制内容。</summary>
    bool TryGetNamespace(object value, out IPyModuleNamespace? result);
    /// <summary>解析精确dict或真实module的原md_dict；不查询用户__dict__属性，输出为实际dict视图。</summary>
    bool TryGetBuiltinsNamespace(object value, out IPyModuleNamespace? result);
    /// <summary>执行真实__import__协议，globals为调用者真实字典，返回值可为任意对象。</summary>
    object ImportLevel(string name, object? globals, object?[] fromlist, long level);
    /// <summary>绑定内核已创建的__main__，不分配替代模块；执行由ExecuteBound另行触发。</summary>
    PyModule BindExistingMainModule(object codeToken);
}

// 只被Instance与反射/trace冷消费者调用；没有在PyDict或普通PyFunction增加字段或查表。
// 当前不缓存foreign键：没有覆盖全部外部写入的版本租约证明时，每次名称/守卫读取都直达权威字典。
internal static class PyModuleNamespaceAccess
{
    internal static object View(object storage) => storage is IPyModuleNamespace ns ? ns.ViewObject : storage;
    internal static bool TryGet(object storage, string name, out object? value) => storage is PyDict native
        ? native.TryGet(name, out value) : ((IPyModuleNamespace)storage).TryGet(name, out value);
    internal static object RequireBuiltins(object? value)
    {
        if (value is PyDict or IPyModuleNamespace) return value;
        if (value is not null && PyProvider.Current is IPyCompiledBuiltinsProvider compiled
            && compiled.TryGetCompiledBuiltinsNamespace(value, out var canonical))
            return canonical ?? throw new PyRuntimeError("compiled builtins has no dictionary identity");
        if (value is not null && PyProvider.Current is IPyStrictImportProvider provider
            && provider.TryGetBuiltinsNamespace(value, out var ns))
        {
            // builtins源可为模块，输出必须再获精确dict证明；不能借module解析放宽普通namespace身份。
            if (ns is null || !provider.TryGetNamespace(ns.ViewObject, out var verified)
                || verified is null || !ReferenceEquals(verified.ViewObject, ns.ViewObject))
                throw new PyRuntimeError("builtins adapter has no exact dictionary identity");
            return ns;
        }
        throw new PyNotImplementedError("instance __builtins__ must be a dictionary namespace");
    }
}
