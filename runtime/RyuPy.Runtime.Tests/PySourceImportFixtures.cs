// 普通source首次Import测试的真实槽/来源夹具；不创建磁盘文件，不把虚拟位置当文件发现证明。
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using RyuPy.Runtime;

namespace RyuPy.Runtime.Tests;

internal sealed class SourceImportFixture
{
    internal readonly Dictionary<string, object?> Fields = new(StringComparer.Ordinal);
    internal readonly List<string> Reads = new();
    internal Func<string, object?>? Getter;
    internal readonly string Name;
    internal readonly string File;

    internal SourceImportFixture(string name, bool package, Action? body = null,
        string? file = null, long kind = 1)
    {
        Name = name; File = file ?? Location(name, package);
        string owner = package ? name : name.Contains('.') ? name[..name.LastIndexOf('.')] : "";
        PyModules.RegisterSourceLocated(name, owner, Path.GetFileName(File), kind, File, true, package,
            () => {
                foreach (string key in new[] { "__spec__", "__loader__", "__cached__", "__path__" })
                    Fields[key] = PyModules.ModuleMetadata(name, key);
                Fields["__name__"] = name; Fields["__file__"] = PyModules.ModuleFile(name);
                body?.Invoke();
            }, key => { Reads.Add(key); return Getter is null ? Read(key) : Getter(key); },
            (_, key, value) => Fields[key] = value);
    }

    internal object? Read(string key) => Fields.TryGetValue(key, out var value) && value is not PyUnbound
        ? value : throw new PyAttributeError($"module '{Name}' has no attribute '{key}'");
    internal string Directory => Path.GetDirectoryName(File)!;
    internal static string Unique() => "source_import_" + Guid.NewGuid().ToString("N");
    internal static string Location(string name, bool package) => Path.GetFullPath(
        Path.Combine("source-import-fixtures", name.Replace('.', Path.DirectorySeparatorChar))
        + (package ? Path.DirectorySeparatorChar + "__init__.py" : ".py"));
    internal static bool Cached(string name) => PyOps.Contains(new PyModuleView(), name);
    internal static object? Find(string name) => PyOps.CallValue(
        PyImportlibUtil.CreateAttributes()["find_spec"], [name], [], []);

    // sys公开删除尚未开放；反射仅恢复测试临时动态覆盖，不给生产新增配置/缓存操作接口。
    // 全部调用者在同一串行collection，finally恢复原值，不能污染其它模块测试。
    internal sealed class FinderConfiguration : IDisposable
    {
        private readonly Dictionary<string, object?> dynamic;
        private readonly string name;
        private readonly bool existed;
        private readonly object? previous;
        internal FinderConfiguration(string name)
        {
            PySystem.Initialize("source-import-tests.py", []);
            PyModule system = PyModules.Import("sys"); this.name = name;
            dynamic = (Dictionary<string, object?>)typeof(PyModule).GetProperty("DynamicAttributes",
                BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(system)!;
            existed = dynamic.TryGetValue(name, out previous);
            PyModules.SetDynamic(system, name, new PyList([]));
        }
        public void Dispose()
        {
            if (existed) dynamic[name] = previous;
            else dynamic.Remove(name);
        }
    }
}
