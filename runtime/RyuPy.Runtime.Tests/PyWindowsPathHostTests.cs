// 真Windows宿主验收：明确跳过非Windows，CI必须核三项实际执行且零跳过。
using System;
using System.Collections.Generic;
using System.IO;
using RyuPy.Runtime;
using Xunit;

namespace RyuPy.Runtime.Tests;

public sealed class WindowsPathFactAttribute : FactAttribute
{
    public WindowsPathFactAttribute()
    {
        if (!OperatingSystem.IsWindows()) Skip = "需要真实Windows宿主";
    }
}

[Collection("生成器帧栈")]
public class PyWindowsPathHostTests
{
    [WindowsPathFact]
    public void LexicalObjectsAndContextBoundaries()
    {
        var path = new PyPath("C:/a//./b/../.hidden.txt/");
        Assert.Equal(@"C:\a\b\..\.hidden.txt", path.Value);
        Assert.Equal(".hidden.txt", path.GetAttr("name"));
        Assert.Equal(".txt", path.GetAttr("suffix"));
        Assert.Equal(".hidden", path.GetAttr("stem"));
        Assert.Equal(@"C:\a\b\..", Assert.IsType<PyPath>(path.GetAttr("parent")).Value);
        Assert.Equal(new object?[] { @"C:\", "a", "b", "..", ".hidden.txt" }, Assert.IsType<PyTuple>(path.GetAttr("parts")).Items);
        object? Call(PyPath p, string name, params object?[] args) => PyOps.CallValue(p.GetAttr(name), args, [], []);
        Assert.False(Assert.IsType<bool>(Call(new PyPath(@"\a"), "is_absolute")));
        Assert.False(Assert.IsType<bool>(Call(new PyPath("C:a"), "is_absolute")));
        Assert.True(Assert.IsType<bool>(Call(path, "is_absolute")));
        Assert.Equal(path.Value, Assert.IsType<PyPath>(Call(path, "absolute")).Value);
        Assert.Throws<PyNotImplementedError>(() => Call(new PyPath("a"), "absolute"));
        Assert.Throws<PyNotImplementedError>(() => Call(new PyPath("C:a"), "absolute"));
        Assert.Throws<PyNotImplementedError>(() => Call(new PyPath(@"\a"), "absolute"));
        Assert.True(PyHash.Equal(new PyPath("C:/Foo"), new PyPath(@"c:\foo")));
        Assert.Equal(Call(new PyPath("C:/Foo"), "__hash__"), Call(new PyPath(@"c:\foo"), "__hash__"));
        Assert.Throws<PyNotImplementedError>(() => Call(new PyPath("中文"), "__hash__"));
        Assert.Equal(@"C:\next", Assert.IsType<PyPath>(Call(path, "joinpath", @"\next")).Value);
    }

    [WindowsPathFact]
    public void FactoriesHaveHostIdentity()
    {
        var attrs = PyPaths.CreateAttributes();
        var generic = Assert.IsType<PyNativeType>(attrs["Path"]);
        var windows = Assert.IsType<PyNativeType>(attrs["WindowsPath"]);
        var posix = Assert.IsType<PyNativeType>(attrs["PosixPath"]);
        var path = Assert.IsType<PyPath>(generic.Invoke(["a", "b"], [], []));
        Assert.Same(windows, PyObject.TypeOf(path));
        Assert.True(windows.Accepts(path));
        Assert.True(generic.Accepts(path));
        Assert.False(posix.Accepts(path));
        Assert.True(windows.IsSubclassOf(generic));
        Assert.Equal(path.Value, Assert.IsType<PyPath>(windows.Invoke(["a", "b"], [], [])).Value);
        Assert.Throws<PyNotImplementedError>(() => posix.Invoke([], [], []));
        Assert.Equal(new PyPath(Directory.GetCurrentDirectory()).Value,
            Assert.IsType<PyPath>(PyOps.CallValue(windows.GetAttr("cwd"), [], [], [])).Value);
    }

    [WindowsPathFact]
    public void RegisteredResourceUsesRealIsolatedFiles()
    {
        string folder = Path.Combine(Path.GetTempPath(), "ryupy-资源-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);
        try
        {
            string name = "windows_resource_" + Guid.NewGuid().ToString("N");
            string init = Path.Combine(folder, "__init__.py");
            File.WriteAllText(init, "");
            File.WriteAllBytes(Path.Combine(folder, "字体.txt"), [65, 10, 66]);
            var attrs = new Dictionary<string, object?>();
            PyModules.RegisterResourceLocated(name, name, "__init__.py", 1, init, false, true,
                () => { }, key => attrs.TryGetValue(key, out var value) ? value : throw new PyAttributeError(key),
                (_, key, value) => attrs[key] = value);
            var root = Assert.IsType<PyPath>(PyImportlibResources.Files([name], [], []));
            object? Call(object value, string method, params object?[] args) => PyOps.CallMethod(value, method, args, [], []);
            Assert.Equal(new PyPath(folder).Value, root.Value);
            Assert.True(Assert.IsType<bool>(Call(root, "is_dir")));
            var file = Assert.IsType<PyPath>(Call(root, "joinpath", "字体.txt"));
            Assert.True(Assert.IsType<bool>(Call(file, "exists")));
            Assert.True(Assert.IsType<bool>(Call(file, "is_file")));
            Assert.Equal(new byte[] { 65, 10, 66 }, Assert.IsType<PyBytes>(Call(file, "read_bytes")).Bytes);
            object stream = Call(file, "open", "rb")!;
            try { Assert.Equal(new byte[] { 65, 10, 66 }, Assert.IsType<PyBytes>(Call(stream, "read")).Bytes); }
            finally { Call(stream, "close"); }
            var missing = Assert.IsType<PyPath>(Call(root, "joinpath", "missing.txt"));
            Assert.False(Assert.IsType<bool>(Call(missing, "exists")));
            Assert.Throws<PyFileNotFoundError>(() => Call(missing, "open", "rb"));
        }
        finally { Directory.Delete(folder, true); }
    }
}
