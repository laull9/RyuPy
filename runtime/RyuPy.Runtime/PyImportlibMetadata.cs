// metadata在普通首次导入时初始化；模块自身字典拥有绑定，重试不覆盖逃逸旧模块。
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
namespace RyuPy.Runtime;
/// <summary>封闭分发环境的有限metadata入口。</summary>
public static class PyImportlibMetadata
{
    /// <summary>只登记初始化委托，首次实际import才绑定父函数。</summary>
    public static void Register() => PyModules.RegisterNativeLazy("importlib.metadata", "importlib", "", Initialize,
        name => throw new PyAttributeError($"module 'importlib.metadata' has no attribute '{name}'"),
        SetAttribute);
    // Discover/Select尚不支持替换类型工厂；在写入前按身份拒绝，不能默默绕过重绑。
    // 仅此native模块setter受限，普通函数重绑和源码metadata自己的setter不受影响。
    private static void SetAttribute(PyModule module, string name, object? value)
    {
        if ((name == "EntryPoint" && !ReferenceEquals(value, PyEntryPoint.Type))
            || (name == "EntryPoints" && !ReferenceEquals(value, PyEntryPoints.Type)))
            throw PyDistributionSnapshot.Unsupported("replacement of native metadata type " + name);
        module.DynamicAttributes[name] = value;
    }
    private static void Initialize()
    {
        PyDistributionSnapshot.Observe();
        PyModule module = PyModules.Import("importlib.metadata");
        PyModule parent = PyModules.Import("importlib");
        if (!parent.IsNativeRegistration)
            throw PyDistributionSnapshot.Unsupported("native metadata under a source importlib package");
        module.DynamicAttributes["import_module"] = parent.GetAttr("import_module");
        module.DynamicAttributes["EntryPoint"] = PyEntryPoint.Type;
        module.DynamicAttributes["EntryPoints"] = PyEntryPoints.Type;
        module.DynamicAttributes["entry_points"] = Keywords("entry_points", (names, values) =>
            Discover(module).Select(names, values));
    }
    internal static PyFunction Keywords(string name, Func<string[], object?[], object?> call) =>
        new(name, PySignature.Create(name, [], [], []), (_, args, names, values) =>
        {
            if (args.Length != 0) throw new PyTypeError(name + "() accepts keyword arguments only");
            return call(names, values);
        }, [], false);
    private static PyEntryPoints Discover(PyModule owner)
    {
        PyModule system = PyModules.Import("sys");
        foreach (string hook in new[] { "meta_path", "path_hooks", "path_importer_cache" })
        {
            try { _ = system.GetAttr(hook); }
            catch (PyAttributeError) { continue; }
            throw PyDistributionSnapshot.Unsupported("custom sys." + hook + " discovery");
        }
        using var document = JsonDocument.Parse(PyDistributionSnapshot.Require());
        var output = new List<PyEntryPoint>();
        foreach (var dist in document.RootElement.GetProperty("distributions").EnumerateArray())
        {
            if (dist.GetProperty("shadowed").GetBoolean()) continue;
            foreach (var file in dist.GetProperty("files").EnumerateArray())
            {
                if (file.GetProperty("path").GetString() != "entry_points.txt") continue;
                string text;
                try { text = new UTF8Encoding(false, true).GetString(Convert.FromHexString(file.GetProperty("hex").GetString()!)); }
                catch (DecoderFallbackException) { throw new PyValueError("entry_points.txt is not UTF-8"); }
                Parse(text, owner, output);
            }
        }
        return new PyEntryPoints(output.ToArray());
    }
    // 保留组/条目顺序及未知组；未加载目标的value不在发现阶段验证。
    private static void Parse(string text, PyModule owner, List<PyEntryPoint> output)
    {
        string? group = null;
        foreach (string raw in text.Split('\n'))
        {
            string line = raw.Trim();
            if (line.Length == 0 || line.StartsWith('#')) continue;
            if (line.StartsWith('[') && line.EndsWith(']')) { group = line[1..^1]; continue; }
            int equals = line.IndexOf('=');
            if (group is null || equals < 0) throw new PyValueError("malformed entry_points.txt");
            output.Add(new PyEntryPoint(owner, line[..equals].Trim(), line[(equals + 1)..].Trim(), group));
        }
    }
}
