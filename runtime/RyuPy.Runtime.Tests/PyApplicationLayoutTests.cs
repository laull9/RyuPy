// 显式应用布局的两候选路径、旧产品兼容及严格marker验证；产物只写自己的仓外临时目录。
using System.Text.Json;
using RyuPy.Runtime;

namespace RyuPy.Runtime.Tests;

public sealed class PyApplicationLayoutTests : IDisposable
{
    private readonly string root = Path.Combine(Path.GetTempPath(), "ryupy-layout-中文 space-" + Guid.NewGuid());
    public PyApplicationLayoutTests() => Directory.CreateDirectory(root);
    public void Dispose() => Directory.Delete(root, true);
    private string WriteMarker(string entryKind = "native_aot", Action<Dictionary<string, object>>? alter = null)
    {
        string dependencies = Path.Combine(root, "internal"); Directory.CreateDirectory(dependencies);
        File.WriteAllText(Path.Combine(root, "entry"), "test entry");
        var value = new Dictionary<string, object> { ["schema_version"] = 1, ["kind"] = "ryupy-application-layout",
            ["payload_mode"] = "canonical-v1", ["entry"] = "entry", ["entry_kind"] = entryKind };
        alter?.Invoke(value);
        File.WriteAllText(Path.Combine(dependencies, PyApplicationLayout.Marker), JsonSerializer.Serialize(value));
        return dependencies;
    }
    [Fact]
    public void NoMarkerRetainsExactLegacyRootAndLogicalPrefix()
    {
        var layout = PyApplicationLayout.ResolveForDirectory(root);
        Assert.False(layout.Canonical); Assert.Equal(root, layout.DependencyRoot);
        const string relative = "_internal/native/v1/probe.so";
        Assert.Same(relative, PyApplicationLayout.MapLegacyRelative(relative, layout));
    }
    [Theory]
    [InlineData("native_aot")]
    [InlineData("apphost")]
    public void BothActualBaseShapesResolveSameExactInternalAndOldLogicalPaths(string kind)
    {
        string dependencies = WriteMarker(kind);
        foreach (string baseDirectory in new[] { root, dependencies })
        {
            var layout = PyApplicationLayout.ResolveForDirectory(baseDirectory);
            Assert.True(layout.Canonical); Assert.Equal(dependencies, layout.DependencyRoot);
            Assert.Equal("resources/v1/probe.txt", PyApplicationLayout.MapLegacyRelative("_internal/resources/v1/probe.txt", layout));
            Assert.Equal("python/ryupy_compiled/module.py", PyApplicationLayout.MapLegacyRelative("python/ryupy_compiled/module.py", layout));
        }
        Assert.Equal(Path.Combine(dependencies, "native", "v1", "demo", "probe.so"),
            PyNativeBindingLibrary.ResolveForBaseDirectory(root, "_internal/native/v1/demo/probe.so"));
        Assert.Equal(Path.Combine(dependencies, "resources", "v1", "module.py"),
            PyModuleLocations.ResolveForPlatform(2, "_internal/resources/v1/module.py", false, Path.Combine(root, "entry")));
        Assert.Equal(Path.Combine(dependencies, "python", "ryupy_compiled", "module.py"),
            PyModuleLocations.ResolveBundle("python/ryupy_compiled/module.py", root));
    }
    [Fact]
    public void MarkerOutsideTwoCandidatesDoesNotGrantLayout()
    {
        WriteMarker(); string nested = Path.Combine(root, "nested"); Directory.CreateDirectory(nested);
        var layout = PyApplicationLayout.ResolveForDirectory(nested);
        Assert.False(layout.Canonical); Assert.Equal(nested, layout.DependencyRoot);
    }
    [Theory]
    [InlineData("schema_version", 2)]
    [InlineData("kind", "other")]
    [InlineData("payload_mode", "guess")]
    [InlineData("entry_kind", "other")]
    [InlineData("entry", "../entry")]
    [InlineData("entry", "other/entry")]
    [InlineData("unknown", "extra")]
    public void InvalidMarkerCannotSilentlyFallBackToLegacy(string key, object value)
    {
        WriteMarker(alter: metadata => metadata[key] = value);
        Assert.Throws<PyRuntimeError>(() => PyApplicationLayout.ResolveForDirectory(root));
    }
    [Fact]
    public void MissingEntryMalformedJsonWrongMarkerPlacementAndDuplicatesFailExplicitly()
    {
        string dependencies = WriteMarker(); File.Delete(Path.Combine(root, "entry"));
        Assert.Throws<PyRuntimeError>(() => PyApplicationLayout.ResolveForDirectory(root));
        File.WriteAllText(Path.Combine(root, "entry"), "test entry");
        string marker = Path.Combine(dependencies, PyApplicationLayout.Marker);
        File.WriteAllText(marker, "{"); Assert.Throws<PyRuntimeError>(() => PyApplicationLayout.ResolveForDirectory(root));
        WriteMarker(); File.Copy(marker, Path.Combine(root, PyApplicationLayout.Marker));
        Assert.Throws<PyRuntimeError>(() => PyApplicationLayout.ResolveForDirectory(root));
        File.Delete(marker);
        Assert.Throws<PyRuntimeError>(() => PyApplicationLayout.ResolveForDirectory(root));
    }
    [Fact]
    public void MarkerBudgetIsExactly4096AndDuplicateFieldsFail()
    {
        string dependencies = WriteMarker(); string marker = Path.Combine(dependencies, PyApplicationLayout.Marker);
        string original = File.ReadAllText(marker);
        File.WriteAllText(marker, original.PadRight(4096));
        Assert.True(PyApplicationLayout.ResolveForDirectory(root).Canonical);
        File.WriteAllText(marker, original.PadRight(4097));
        Assert.Throws<PyRuntimeError>(() => PyApplicationLayout.ResolveForDirectory(root));
        File.WriteAllText(marker, original.Insert(1, "\"schema_version\":1,"));
        Assert.Throws<PyRuntimeError>(() => PyApplicationLayout.ResolveForDirectory(root));
    }

}
