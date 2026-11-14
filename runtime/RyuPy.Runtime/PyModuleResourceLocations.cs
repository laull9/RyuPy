// 文件包资源能力随注册及每个导入尝试绑定；不读取公开 __file__、不猜包名、不重查替代注册。
using System;
using System.IO;
using System.Runtime.CompilerServices;

namespace RyuPy.Runtime;

public static partial class PyModules
{
    private sealed record ResourceLocation(bool IsPackage, long Kind, string Anchor);
    private static readonly ConditionalWeakTable<PyModule, ResourceLocation> ResourceLocations = new();

    /// <summary>单次原子登记真实 resolver 包能力；旧九参入口不自动获得资源资格。</summary>
    public static void RegisterResourceLocated(string name, string package, string diagnosticFile, long locationKind,
        string locationValue, bool fileBackedByGlobal, bool isPackage, PyModuleInit init, PyModuleGet get, PyModuleSet set)
        => RegisterResourceSource(name, package, diagnosticFile, locationKind, locationValue,
            fileBackedByGlobal, isPackage, init, get, set, false);

    /// <summary>新编译器显式声明元数据前言能力；不改变旧十参入口的缺属性合同。</summary>
    public static void RegisterSourceLocated(string name, string package, string diagnosticFile, long locationKind,
        string locationValue, bool fileBackedByGlobal, bool isPackage, PyModuleInit init, PyModuleGet get, PyModuleSet set)
        => RegisterResourceSource(name, package, diagnosticFile, locationKind, locationValue,
            fileBackedByGlobal, isPackage, init, get, set, true);

    private static void RegisterResourceSource(string name, string package, string diagnosticFile, long locationKind,
        string locationValue, bool fileBackedByGlobal, bool isPackage, PyModuleInit init, PyModuleGet get, PyModuleSet set,
        bool metadataEnabled)
    {
        EnsureRegistrationAvoidsCacheAliases(name);
        string resolved = PyModuleLocations.Resolve(locationKind, locationKind == 0 ? diagnosticFile : locationValue);
        Registry[name] = new Registration(package, diagnosticFile, init, get, set, resolved, fileBackedByGlobal,
            new ResourceLocation(isPackage, locationKind, resolved), metadataEnabled);
        PyProvider.Current?.Registered(name);
    }

    internal static PyPath ResourceFiles(PyModule module)
    {
        if (!ResourceLocations.TryGetValue(module, out ResourceLocation? location)
            || !location.IsPackage || location.Kind is not (1 or 2))
            throw ResourceUnsupported("files(): anchor is not a registered file-backed resource package");
        // 只使用本次未替换的源规格；用户覆盖/修改仍明确拒绝，不默默绕过自定义loader。
        if (module.Metadata is { } metadata)
        {
            if (!metadata.ResourceUnchanged(module))
                throw ResourceUnsupported("files(): explicit module spec/loader overrides are not implemented");
        }
        else
        {
            RejectResourceMetadata(module, "__spec__");
            RejectResourceMetadata(module, "__loader__");
        }
        string? parent = Path.GetDirectoryName(location.Anchor);
        if (parent is null || !Path.IsPathFullyQualified(parent))
            throw ResourceUnsupported("files(): resource package has no absolute directory");
        return new PyPath(parent);
    }

    private static void RejectResourceMetadata(PyModule module, string name)
    {
        try { _ = module.GetAttr(name); }
        catch (PyAttributeError) { return; }
        throw ResourceUnsupported("files(): explicit module spec/loader overrides are not implemented");
    }

    private static PyBaseException ResourceUnsupported(string message) =>
        (PyBaseException)PyErrors.PrepareRaise(new PyNotImplementedError(message));
}
