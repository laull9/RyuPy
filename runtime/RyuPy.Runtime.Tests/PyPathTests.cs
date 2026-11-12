// 原生路径 API 的词法边界、隔离目录副作用与延迟迭代回归。
using System;
using System.IO;
using Xunit;

namespace RyuPy.Runtime.Tests;

public class PyPathTests
{
    private static object? Call(PyNativeObject target, string name, params object?[] args) =>
        PyOps.CallValue(target.GetAttr(name), args, [], []);

    [Fact]
    public void WindowsLexicalGateRejectsWithoutTouchingFilesystem()
    {
        PyOsPath.EnsureLexicalPlatform(false);
        Assert.Throws<PyNotImplementedError>(() => PyOsPath.EnsureLexicalPlatform(true));
    }

    [Theory]
    [InlineData("a//b/../c/", "a/c")]
    [InlineData("", ".")]
    [InlineData("//server//a", "//server/a")]
    [InlineData("///a/../../b", "/b")]
    [InlineData("a/../../b", "../b")]
    public void PosixNormalizationPreservesRelativeAndDoubleSlash(string input, string expected)
    {
        if (OperatingSystem.IsWindows()) return;
        Assert.Equal(expected, PyOsPath.Normalize(input));
    }

    [Fact]
    public void LexicalComponentsDoNotResolveParentDirectories()
    {
        if (OperatingSystem.IsWindows()) return;
        var path = new PyPath("a//./b/../.hidden.txt/");
        Assert.Equal("a/b/../.hidden.txt", path.Value);
        Assert.Equal(".hidden.txt", path.GetAttr("name"));
        Assert.Equal(".hidden", path.GetAttr("stem"));
        Assert.Equal(".txt", path.GetAttr("suffix"));
        Assert.Equal("a/b/..", Assert.IsType<PyPath>(path.GetAttr("parent")).Value);
        Assert.Equal("", new PyPath("/").GetAttr("name"));
        Assert.Equal("/", Assert.IsType<PyPath>(new PyPath("/").GetAttr("parent")).Value);
        Assert.Equal("", new PyPath("file.").GetAttr("suffix"));
        Assert.Equal("", new PyPath(".hidden").GetAttr("suffix"));
        Assert.Equal("//", new PyPath("//server/a").GetAttr("anchor"));
        Assert.Equal("PosixPath('a\\b')".Replace("\\", "\\\\"), new PyPath("a\\b").PythonRepr());
        Assert.Equal("a/b", PyOsPath.DirName("a/b/"));
        Assert.Equal("", PyOsPath.BaseName("a/b/"));
        Assert.Equal(".hidden", PyOsPath.SplitExtension(".hidden").Items[0]);
        Assert.Equal("", PyOsPath.SplitExtension(".hidden").Items[1]);
        Assert.True(PyHash.Equal(new PyPath("a/./b"), new PyPath("a/b")));
    }

    [Fact]
    public void FilesystemOperationsStayInsideOwnedTemporaryDirectory()
    {
        string temp = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "ryupy-path-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(temp);
        try
        {
            var directory = new PyPath(System.IO.Path.Combine(temp, "nested"));
            Call(directory, "mkdir");
            Assert.Throws<PyFileExistsError>(() => Call(directory, "mkdir"));
            var file = Assert.IsType<PyPath>(Call(directory, "joinpath", "x.txt"));
            Assert.Equal(3L, Call(file, "write_bytes", new PyBytes([1, 2, 3])));
            Assert.Equal(new byte[] { 1, 2, 3 }, Assert.IsType<PyBytes>(Call(file, "read_bytes")).Bytes);
            Assert.Throws<PyTypeError>(() => Call(file, "write_text", 12L));
            Assert.Equal(new byte[] { 1, 2, 3 }, File.ReadAllBytes(file.Value));
            Assert.True(Assert.IsType<bool>(Call(file, "exists")));
            var moved = Assert.IsType<PyPath>(Call(file, "rename", System.IO.Path.Combine(directory.Value, "y.txt")));
            Assert.False(File.Exists(file.Value));
            Assert.True(File.Exists(moved.Value));
            Call(moved, "unlink");
            Assert.Throws<PyFileNotFoundError>(() => Call(moved, "unlink"));
            Call(moved, "unlink", true);
            Call(directory, "rmdir");
        }
        finally { Directory.Delete(temp, true); }
    }

    [Fact]
    public void PathFactoriesAreNativeTypesWithConcreteIdentity()
    {
        var attributes = PyPaths.CreateAttributes();
        Assert.Same(attributes, PyPaths.CreateAttributes());
        System.Threading.Tasks.Parallel.For(0, 8, _ =>
            Assert.Same(attributes["Path"], PyPaths.CreateAttributes()["Path"]));
        var pathType = Assert.IsType<PyNativeType>(attributes["Path"]);
        var posixType = Assert.IsType<PyNativeType>(attributes[OperatingSystem.IsWindows() ? "WindowsPath" : "PosixPath"]);
        object value = pathType.Invoke(["a", "b"], [], [])!;
        Assert.True(pathType.Accepts(value));
        Assert.True(posixType.Accepts(value));
        Assert.Same(posixType, PyObject.TypeOf(value));
        Assert.True(posixType.IsSubclassOf(pathType));
        Assert.Equal("pathlib", pathType.GetAttr("__module__"));
        var cwd = Assert.IsType<PyPath>(PyOps.CallValue(pathType.GetAttr("cwd"), [], [], []));
        Assert.Equal(Directory.GetCurrentDirectory(), cwd.Value);
        var windows = Assert.IsType<PyNativeType>(attributes[OperatingSystem.IsWindows() ? "PosixPath" : "WindowsPath"]);
        Assert.Throws<PyNotImplementedError>(() => windows.Invoke([], [], []));
    }

    [Fact]
    public void PathAbsolutePreservesParentComponentsUnlikeOsAbspath()
    {
        if (OperatingSystem.IsWindows()) return;
        var relative = Assert.IsType<PyPath>(Call(new PyPath("a/../b"), "absolute"));
        Assert.Equal(Directory.GetCurrentDirectory() + "/a/../b", relative.Value);
        var absolute = Assert.IsType<PyPath>(Call(new PyPath("/a/../b"), "absolute"));
        Assert.Equal("/a/../b", absolute.Value);
        Assert.Equal("/b", PyOsPath.Absolute("/a/../b"));
    }

    [Fact]
    public void PathQueryMissingAndPermissionErrorsAreDifferent()
    {
        Assert.Null(PyPathQueries.Probe("a\0b"));
        Assert.Null(PyPathQueries.Probe("unused", _ => throw new FileNotFoundException()));
        Assert.Null(PyPathQueries.Probe("unused", _ => throw new DirectoryNotFoundException()));
        Assert.Throws<PyPermissionError>(() => PyPathQueries.Probe("unused", _ => throw new UnauthorizedAccessException()));
        Assert.Throws<PyOSError>(() => PyPathQueries.Probe("unused", _ => throw new IOException()));
    }

    [Fact]
    public void EmptyOsPathsFailWithoutModifyingWorkingDirectory()
    {
        string before = Directory.GetCurrentDirectory();
        Assert.Throws<PyFileNotFoundError>(() => PyOs.ListDirectory(""));
        Assert.Throws<PyFileNotFoundError>(() => PyOs.MakeDirectory("", 511L, false, false));
        Assert.Throws<PyFileNotFoundError>(() => PyOs.Remove("", false));
        Assert.Throws<PyFileNotFoundError>(() => PyOs.RemoveDirectory(""));
        Assert.Equal(before, Directory.GetCurrentDirectory());
    }

    [Fact]
    public void DirectoryIteratorReadsAtFirstAdvanceAndGlobUsesPythonWildcard()
    {
        string temp = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "ryupy-glob-" + Guid.NewGuid().ToString("N"));
        var root = new PyPath(temp);
        var iterator = Assert.IsAssignableFrom<IPyIterator>(Call(root, "iterdir"));
        Directory.CreateDirectory(temp);
        try
        {
            File.WriteAllText(System.IO.Path.Combine(temp, "plain"), "");
            File.WriteAllText(System.IO.Path.Combine(temp, "file.txt"), "");
            Assert.True(iterator.MoveNext());
            var glob = Assert.IsAssignableFrom<IPyIterator>(Call(root, "glob", "*.*"));
            Assert.True(glob.MoveNext());
            Assert.Equal("file.txt", Assert.IsType<PyPath>(glob.Current()).GetAttr("name"));
            Assert.False(glob.MoveNext());
            File.WriteAllText(System.IO.Path.Combine(temp, "🎉.txt"), "");
            var unicode = Assert.IsAssignableFrom<IPyIterator>(Call(root, "glob", "?.txt"));
            Assert.True(unicode.MoveNext());
            Assert.Equal("🎉.txt", Assert.IsType<PyPath>(unicode.Current()).GetAttr("name"));
            Assert.False(unicode.MoveNext());
            var unsupported = Assert.IsAssignableFrom<IPyIterator>(Call(root, "glob", "**/*.txt"));
            Assert.Throws<PyNotImplementedError>(() => unsupported.MoveNext());
        }
        finally { Directory.Delete(temp, true); }
    }
}
