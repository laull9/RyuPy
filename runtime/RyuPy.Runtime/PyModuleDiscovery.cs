// 已编译模块finder只消费明确登记的源码/普通包描述；查询不复用普通Import目标执行路径。
using System;

namespace RyuPy.Runtime;

public static partial class PyModules
{
    internal static object DiscoverSpec(string fullname)
    {
        // 别名后代未纳入闭世界 finder；在父 __path__ getter 或其它用户协议之前明确拒绝。
        RejectCacheAliasDescendant(fullname);
        PyModule? parent = null;
        object? parentPath = null;
        int dot = fullname.LastIndexOf('.');
        if (dot > 0)
        {
            string parentName = fullname[..dot];
            parent = ImportDiscoveryParent(parentName);
            parentPath = DiscoveryParentPath(parent, parentName, fullname, importing: false);
        }
        // CP _find_spec在此时决定is_reload；父正文若已导入目标，仍返回finder的新规格。
        // 不在父导入之后重新读取目标缓存，避免错误返回父正文改写的__spec__。
        RejectFinderHooks();
        RequireOriginalSearchPath(parent, parentPath);
        Registration registration = SourceDiscoveryRegistration(fullname);
        return PyModuleMetadata.CreateSpec(fullname, registration.ResolvedFile, registration.ResourceLocation!.IsPackage, registration.InstanceCode);
    }

    // 仅查询所需的父导入链加守卫，不改变普通Import的ABI或行为。每个未缓存层都是真正
    // 的一次查找，必须在执行该层正文前检查hooks和实际父路径；缓存层不重查其祖先。
    private static PyModule ImportDiscoveryParent(string name)
    {
        if (Cache.TryGetValue(name, out PyModule? cached)) return cached;
        PyModule? parent = null;
        object? parentPath = null;
        int dot = name.LastIndexOf('.');
        if (dot > 0)
        {
            string parentName = name[..dot];
            parent = ImportDiscoveryParent(parentName);
            // 与普通import一样，父初始化可以主动导入当前层；此时无需再次finder查找。
            if (Cache.TryGetValue(name, out cached)) return cached;
            parentPath = DiscoveryParentPath(parent, parentName, name, importing: true);
        }
        RejectFinderHooks();
        RequireOriginalSearchPath(parent, parentPath);
        _ = SourceDiscoveryRegistration(name);
        // 父正文异常原身份传播；不能转换为查无目标、猜补异常name或清理成功的祖先。
        return Import(name);
    }

    private static object? DiscoveryParentPath(PyModule parent, string parentName, string fullname, bool importing)
    {
        // native/旧ABI/无位置初值未承诺来源能力；缺path不能据此谎报真实非包。
        if (parent.Metadata is not { HasSourceLocation: true })
            throw DiscoveryUnsupported("native or legacy parents without source metadata");
        try { return parent.GetAttr("__path__"); }
        catch (PyAttributeError missing)
        {
            // 原getter可直接抛异常；先补它当时的外层except，再保留这层原身份。
            PyErrors.Prepare(missing);
            var error = new PyModuleNotFoundError(importing
                ? $"No module named {PyRepr.Repr(fullname)}; {PyRepr.Repr(parentName)} is not a package"
                : $"__path__ attribute not found on {PyRepr.Repr(parentName)} while trying to find {PyRepr.Repr(fullname)}")
                { Context = missing };
            error.UserAttributes["name"] = fullname;
            // 普通父导入链采用_bootstrap的fromNone；util最终父查询则显式from原AttributeError。
            throw DiscoveryError(error.WithCause(importing ? null : missing));
        }
    }

    private static void RequireOriginalSearchPath(PyModule? parent, object? parentPath)
    {
        if (parent is not null && (parent.Metadata is null || !parent.Metadata.SearchPathUnchanged(parentPath)))
            throw DiscoveryUnsupported("modified or unsupported parent search paths");
    }

    private static Registration SourceDiscoveryRegistration(string name)
    {
        // 闭世界没有负存在性证明；未登记名称明确NI，不把旧Import错误当作文件不存在证明。
        if (!Registry.TryGetValue(name, out Registration? registration))
            throw DiscoveryUnsupported("names outside the compiled module domain");
        if (!registration.MetadataEnabled || registration.ResourceLocation is not { Kind: 1 or 2 })
            throw DiscoveryUnsupported("legacy or native module registrations without source metadata");
        return registration;
    }

    private static void RejectFinderHooks()
    {
        // 系统初始化前的C#嵌入仍可使用内置编译finder；这里不为检查而创建sys。
        if (!Cache.TryGetValue("sys", out PyModule? system)) return;
        foreach (string name in new[] { "meta_path", "path_hooks", "path_importer_cache" })
        {
            try { _ = system.GetAttr(name); }
            catch (PyAttributeError) { continue; }
            throw DiscoveryUnsupported("explicit sys." + name + " finder configuration");
        }
    }

    private static PyBaseException DiscoveryUnsupported(string detail) =>
        DiscoveryError(new PyNotImplementedError("find_spec(): " + detail + " are not implemented"));

    private static PyBaseException DiscoveryError(PyBaseException error) => (PyBaseException)PyErrors.PrepareRaise(error);
}
