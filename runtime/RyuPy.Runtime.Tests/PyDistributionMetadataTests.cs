// 单个有序runtime域测试；无reset，不把不同环境塞进同一全局状态。独立未配置/晚配见host fixtures。
using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using RyuPy.Runtime;
using Xunit;
namespace RyuPy.Runtime.Tests;
[Collection("生成器帧栈")]
public class PyDistributionMetadataTests
{
    private static object? Call(object? function, params object?[] args) => PyOps.CallValue(function, args, [], []);
    private static object? Method(object? target, string name, params object?[] args) => Call(PyObject.GetAttr(target, name), args);
    private static string Payload()
    {
        byte[] text = Encoding.UTF8.GetBytes("[demo]\nfirst = metadata_probe:Box.value\nsecond = metadata_probe:Box\n[mystery]\nunused = never_registered:Target\n");
        return JsonSerializer.Serialize(new { schema_version = 1, mode = "closed_snapshot", roots = new[] { "fixture" },
            distributions = new[] { new { root = "fixture", path = "Demo.dist-info", kind = "dist-info", metadata_name = "Demo", metadata_version = "1", discovery_key = "demo", shadowed = false,
                directories = Array.Empty<string>(), files = new[] { new { path = "entry_points.txt", size = text.Length,
                    sha256 = Convert.ToHexString(SHA256.HashData(text)).ToLowerInvariant(), hex = Convert.ToHexString(text).ToLowerInvariant() } } } } });
    }
    [Fact]
    public void OwnedSnapshotLazyImportAndRebindingUseOneRealDomain()
    {
        string payload = Payload();
        Assert.Throws<PyValueError>(() => PyDistributionSnapshot.Configure(null!, payload));
        Assert.Throws<PyValueError>(() => PyDistributionSnapshot.Configure(typeof(PyDistributionMetadataTests), null!));
        PyDistributionSnapshot.Configure(typeof(PyDistributionMetadataTests), payload);
        PyDistributionSnapshot.Configure(typeof(PyDistributionMetadataTests), payload);
        Assert.Throws<PyValueError>(() => PyDistributionSnapshot.Configure(null!, payload));
        Assert.Throws<PyValueError>(() => PyDistributionSnapshot.Configure(typeof(PyDistributionMetadataTests), null!));
        PyDistributionSnapshot.Configure(typeof(PyDistributionMetadataTests), payload);
        Assert.Throws<PyNotImplementedError>(() => PyDistributionSnapshot.Configure(typeof(string), payload));
        Assert.Throws<PyNotImplementedError>(() => PyDistributionSnapshot.Configure(typeof(PyDistributionMetadataTests),
            "{\"schema_version\":1,\"mode\":\"closed_snapshot\",\"roots\":[],\"distributions\":[]}"));
        PySystem.Initialize("metadata-tests.py", []);
        var parent = PyModules.Import("importlib");
        object? original = parent.GetAttr("import_module");
        int calls = 0, initialized = 0;
        var attrs = new Dictionary<string, object?> { ["value"] = "initial" };
        PyModules.Register("metadata_box", "", "", () => { },
            name => attrs.TryGetValue(name, out var value) ? value : throw new PyAttributeError(name),
            (_, name, value) => attrs[name] = value);
        PyModule box = PyModules.Import("metadata_box");
        PyModules.Register("metadata_probe", "", "", () => { initialized++; },
            name => name == "Box" ? box : throw new PyAttributeError(name),
            (module, name, value) => PyModules.SetDynamic(module, name, value));
        var wrapper = PyNativeFunctions.Create("wrapped", ["name", "package"], [null, PyNone.Instance], 1,
            values => { calls++; return PyImportlib.ImportModule(values[0], values[1]); });
        parent.SetAttr("import_module", wrapper);
        try
        {
            var metadata = PyModules.Import("importlib.metadata");
            // 未实现API不发布占位：保hasattr与from-import的原缺属性回退时机。
            foreach (string missing in new[] { "Distribution", "version", "distribution", "distributions", "files", "requires", "packages_distributions", "metadata" })
            {
                Assert.Throws<PyAttributeError>(() => metadata.GetAttr(missing));
                Assert.Equal(false, PyOps.CallBuiltin("hasattr", [metadata, missing], [], []));
                Assert.Throws<PyImportError>(() => PyModules.ImportAttr("importlib.metadata", missing));
            }
            Assert.Same(wrapper, metadata.GetAttr("import_module"));
            parent.SetAttr("import_module", original);
            Assert.Same(wrapper, metadata.GetAttr("import_module"));
            foreach (string typeName in new[] { "EntryPoint", "EntryPoints" })
            {
                object? type = metadata.GetAttr(typeName);
                metadata.SetAttr(typeName, type);
                Assert.Same(type, metadata.GetAttr(typeName));
                Assert.Throws<PyNotImplementedError>(() => metadata.SetAttr(typeName, PyNone.Instance));
                Assert.Same(type, metadata.GetAttr(typeName));
                Assert.Throws<PyNotImplementedError>(() => metadata.SetAttr(typeName, wrapper));
                Assert.Same(type, metadata.GetAttr(typeName));
                Assert.Throws<PyNotImplementedError>(() => PyObject.DelAttr(metadata, typeName));
                Assert.Same(type, metadata.GetAttr(typeName));
            }
            // 只有两类型受限，entry_points仍是普通可重绑的模块属性。
            object? discover = metadata.GetAttr("entry_points");
            var replacement = PyNativeFunctions.Create("replacement", [], [], 0, _ => "rebound discovery");
            try
            {
                metadata.SetAttr("entry_points", replacement);
                Assert.Same(replacement, metadata.GetAttr("entry_points"));
                Assert.Equal("rebound discovery", Call(metadata.GetAttr("entry_points")));
            }
            finally { metadata.SetAttr("entry_points", discover); }
            object? points = PyOps.CallValue(metadata.GetAttr("entry_points"), [], ["group"], ["demo"]);
            Assert.Equal(2L, PyOps.Len(points)); Assert.Equal(0, calls); Assert.Equal(0, initialized);
            var iterator = PyIter.Create(points); Assert.True(iterator.MoveNext());
            Assert.Equal("first", PyObject.GetAttr(iterator.Current(), "name"));
            object? point = PyOps.GetItem(points, "first");
            Assert.Same(metadata.GetAttr("EntryPoints"), PyObject.TypeOf(points));
            Assert.Same(metadata.GetAttr("EntryPoint"), PyObject.TypeOf(point));
            Assert.Equal("initial", Method(point, "load"));
            attrs["value"] = "changed";
            Assert.Equal("changed", Method(point, "load")); Assert.Equal(2, calls); Assert.Equal(1, initialized);
            metadata.SetAttr("import_module", original);
            Assert.Equal("changed", Method(point, "load")); Assert.Equal(2, calls);
            Assert.Throws<PyKeyError>(() => PyOps.GetItem(points, "missing"));
            Assert.Throws<PyNotImplementedError>(() => PyHash.Equal(point, point));
            Assert.Throws<PyNotImplementedError>(() => PyHash.Of(point));
            object? empty = PyOps.CallValue(PyObject.GetAttr(points, "select"), [], ["name"], ["absent"]);
            Assert.Equal(0L, PyOps.Len(empty)); Assert.False(PyOps.Truthy(empty));
            Assert.Same(metadata.GetAttr("EntryPoints"), PyObject.TypeOf(empty));
            PyDistributionSnapshot.Configure(typeof(PyDistributionMetadataTests), payload);
        }
        finally { parent.SetAttr("import_module", original); }
    }
    [Fact]
    public void ImportModuleUsesNormalBinderAndDoesNotReadAbsolutePackage()
    {
        PySystem.Initialize("metadata-import-tests.py", []);
        var function = PyModules.Import("importlib").GetAttr("import_module");
        Assert.Same(PyModules.Import("sys"), Call(function, "sys", new object()));
        Assert.Throws<PyValueError>(() => Call(function, ""));
        Assert.Throws<PyTypeError>(() => Call(function, ".x"));
        Assert.Throws<PyImportError>(() => Call(function, "..x", "one"));
        Assert.Throws<PyAttributeError>(() => Call(function, 4L));
        Assert.Throws<PyTypeError>(() => Call(function));
        Assert.Same(PyModules.Import("importlib"), Call(function, ".", "importlib"));
    }
}
