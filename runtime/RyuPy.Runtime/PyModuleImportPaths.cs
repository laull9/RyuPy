// 普通JIT source-bound首次导入只接受已证父目录；不执行finder、不由公开元数据猜来源。
using System;
using System.IO;

namespace RyuPy.Runtime;

public static partial class PyModules
{
    private static void RequireSourceImportPath(string name, PyModule? parent,
        string? childName, Registration registration)
    {
        if (!IsSourceImportPair(parent, registration)) return;
        object? path;
        try { path = parent!.GetAttr("__path__"); }
        catch (PyAttributeError missing)
        {
            // 与普通import的非包相位一致；保原getter及外层except的异常身份/链。
            PyErrors.Prepare(missing);
            string parentName = name[..name.LastIndexOf('.')];
            var error = ImportPathMissing(name,
                $"No module named {PyRepr.Repr(name)}; {PyRepr.Repr(parentName)} is not a package");
            error.Context = missing;
            throw ImportPathError(error.WithCause(null));
        }

        // 即使path为空，自定义meta finder仍可能找到目标或抛错；不能先谎报不存在。
        RejectImportFinderHooks();
        string directory = RequireSourceImportLocation(parent, childName, registration)!;
        int count = path switch
        {
            PyList list => list.Count,
            PyTuple tuple when tuple.GetType() == typeof(PyTuple) => tuple.Length,
            _ => throw ImportPathUnsupported("custom or unsupported parent search paths"),
        };
        if (count == 0) throw ImportPathError(ImportPathMissing(name, $"No module named {PyRepr.Repr(name)}"));
        for (int i = 0; i < count; i++)
        {
            object? item = path is PyList list ? list.GetItem(i) : ((PyTuple)path!).GetItem(i);
            // 精确str按值比较，接受新容器及重复同目录项，不执行用户eq/iter/fspath。
            if (item is not string value || !string.Equals(value, directory, StringComparison.Ordinal))
                throw ImportPathUnsupported("modified or unsupported parent search paths");
        }
    }

    private static bool IsSourceImportPair(PyModule? parent, Registration registration) =>
        DefaultSourcePair(parent, registration) || parent is { Metadata: { HasSourceLocation: true } }
        && ResourceLocations.TryGetValue(parent, out ResourceLocation? location) && location.Kind == 1
        && registration.MetadataEnabled && registration.ResourceLocation is { Kind: 1 };

    // 纯来源检查也供find_spec导入中间父使用；那里已读path/检查hooks，不能再次触发getter。
    // 位置模式不足以证明父子归属：resolver可能从不同搜索根独立解析同名前缀。
    private static string? RequireSourceImportLocation(PyModule? parent, string? childName,
        Registration registration)
    {
        if (!IsSourceImportPair(parent, registration)) return null;
        if (!ResourceLocations.TryGetValue(parent!, out ResourceLocation? parentSource))
            throw new PyRuntimeError("source import parent lost its location");
        ResourceLocation childSource = registration.ResourceLocation!;
        string? directory = Path.GetDirectoryName(parentSource.Anchor);
        if (!parentSource.IsPackage || childName is null || directory is null
            || !string.Equals(Path.GetFileName(parentSource.Anchor), "__init__.py", StringComparison.Ordinal))
            throw ImportPathUnsupported("nonordinary source parents");
        // host登记名不一定来自Python解析器；不能让rooted/多分量子名逃出已证父目录。
        if (childName.Length == 0 || Path.IsPathRooted(childName)
            || childName.IndexOf(Path.DirectorySeparatorChar) >= 0
            || childName.IndexOf(Path.AltDirectorySeparatorChar) >= 0)
            throw ImportPathUnsupported("nonordinary source child names");
        string expected = childSource.IsPackage
            ? Path.Combine(directory, childName, "__init__.py") : Path.Combine(directory, childName + ".py");
        // 不消去..或规范化symlink别名，不从当前同名父Registry/可改__file__推实址。
        if (!string.Equals(childSource.Anchor, expected, StringComparison.Ordinal))
            throw ImportPathUnsupported("incoherent parent and child source locations");
        return directory;
    }

    private static void RejectImportFinderHooks()
    {
        // 不为检查而创建sys；其原生默认态没有这些配置，显式赋值才进入未知finder域。
        if (!Cache.TryGetValue("sys", out PyModule? system)) return;
        foreach (string name in new[] { "meta_path", "path_hooks", "path_importer_cache" })
        {
            try { _ = system.GetAttr(name); }
            catch (PyAttributeError) { continue; }
            throw ImportPathUnsupported("explicit sys." + name + " finder configuration");
        }
    }

    // 普通、已完成初始化的JIT源父诊断采用实际来源；不为错误格式新增公开元数据getter。
    // 可写name/spec/file与循环导入的完整诊断协议仍是旧边界，kind0/native不冒领源路径。
    private static PyBaseException ImportAttributeMissing(PyModule module, string moduleName, string name)
    {
        string message = $"cannot import name '{name}' from '{moduleName}'";
        if (module.State == 2 && module.Metadata is { HasSourceLocation: true }
            && ResourceLocations.TryGetValue(module, out ResourceLocation? source) && source.Kind == 1)
        {
            var error = new PyImportError(message + " (" + source.Anchor + ")");
            error.UserAttributes["name"] = moduleName;
            error.UserAttributes["path"] = source.Anchor;
            return ImportPathError(error);
        }
        return new PyImportError(message);
    }

    private static PyModuleNotFoundError ImportPathMissing(string name, string message)
    {
        var error = new PyModuleNotFoundError(message);
        error.UserAttributes["name"] = name;
        return error;
    }
    private static PyBaseException ImportPathUnsupported(string detail) =>
        ImportPathError(new PyNotImplementedError("import: " + detail + " are not implemented"));
    private static PyBaseException ImportPathError(PyBaseException error) =>
        (PyBaseException)PyErrors.PrepareRaise(error);
}
