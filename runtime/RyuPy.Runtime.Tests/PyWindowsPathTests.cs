// 固定 CPython 3.12.13 ntpath 词法矩阵；纯 Windows 分支跨平台执行，公开入口在真实 Windows 上再核。
using System;
using System.Collections.Generic;
using RyuPy.Runtime;
using Xunit;

namespace RyuPy.Runtime.Tests;

[Collection("生成器帧栈")]
public class PyWindowsPathTests
{
    [InlineData("", "")]
    [InlineData("foo", "")]
    [InlineData("foo/", "foo")]
    [InlineData("foo\\", "foo")]
    [InlineData("/", "/")]
    [InlineData("//", "//")]
    [InlineData("\\", "\\")]
    [InlineData("\\\\", "\\\\")]
    [InlineData("C:", "C:")]
    [InlineData("C:foo", "C:")]
    [InlineData("C:/", "C:/")]
    [InlineData("C:\\", "C:\\")]
    [InlineData("C:/foo", "C:/")]
    [InlineData("C:/foo//", "C:/foo")]
    [InlineData("C:\\a/b\\c.txt", "C:\\a/b")]
    [InlineData("//server/share", "//server/share")]
    [InlineData("//server/share/", "//server/share/")]
    [InlineData("//server/share/foo", "//server/share/")]
    [InlineData("\\\\server\\share\\dir\\file", "\\\\server\\share\\dir")]
    [InlineData("//server", "//server")]
    [InlineData("///a", "///a")]
    [InlineData("1:foo", "1:")]
    [InlineData(":foo", "")]
    [Theory]
    public void DirNamePreservesDriveRootAndOriginalSeparators(string path, string expected) =>
        Assert.Equal(expected, PyWindowsPath.DirName(path));

    public static IEnumerable<object[]> JoinCases()
    {
        yield return new object[] { new object?[] { "C:\\a", "b" }, "C:\\a\\b" };
        yield return new object[] { new object?[] { "C:\\a", "\\b" }, "C:\\b" };
        yield return new object[] { new object?[] { "C:\\a", "/b" }, "C:/b" };
        yield return new object[] { new object?[] { "C:\\a", "D:b" }, "D:b" };
        yield return new object[] { new object?[] { "C:\\a", "c:b" }, "c:\\a\\b" };
        yield return new object[] { new object?[] { "C:a", "" }, "C:a\\" };
        yield return new object[] { new object?[] { "C:", "" }, "C:" };
        yield return new object[] { new object?[] { "C:", "a" }, "C:a" };
        yield return new object[] { new object?[] { "", "" }, "" };
        yield return new object[] { new object?[] { "a/", "" }, "a/" };
        yield return new object[] { new object?[] { "//server/share", "a" }, "//server/share\\a" };
        yield return new object[] { new object?[] { "//server/share", "/a" }, "//server/share/a" };
        yield return new object[] { new object?[] { "//server/share/a", "C:b" }, "C:b" };
        yield return new object[] { new object?[] { "a", "C:b" }, "C:b" };
        yield return new object[] { new object?[] { "a", "\\b", "" }, "\\b\\" };
        yield return new object[] { new object?[] { "C:/a", "b", "" }, "C:/a\\b\\" };
        yield return new object[] { new object?[] { "C:/a", "c:b" }, "c:/a\\b" };
        yield return new object[] { new object?[] { "//server/share", "" }, "//server/share" };
        yield return new object[] { new object?[] { "//server", "a" }, "//server\\a" };
    }
    [Theory]
    [MemberData(nameof(JoinCases))]
    public void JoinKeepsDriveRelativeRootResetAndTailSpelling(object?[] parts, string expected) =>
        Assert.Equal(expected, PyWindowsPath.Join(parts));

    private static object PathLike(Func<object?> action)
    {
        var cls = new PyClass("LexicalPathLike");
        cls.SetAttr("__fspath__", PyNativeFunctions.Create("__fspath__", ["self"], [null], 1, _ => action()));
        return new PyInstance(cls);
    }

    [Fact]
    public void PathLikeConversionIsSequentialAndStopsAtFirstFailure()
    {
        var calls = new List<string>();
        object first = PathLike(() => { calls.Add("first"); return "C:/a"; });
        object bad = PathLike(() => { calls.Add("bad"); return 42L; });
        object last = PathLike(() => { calls.Add("last"); return "tail"; });
        Assert.Throws<PyTypeError>(() => PyWindowsPath.Join([first, bad, last]));
        Assert.Equal(new[] { "first", "bad" }, calls);
        calls.Clear();
        Assert.Equal("C:/a\\tail", PyWindowsPath.Join([first, last]));
        Assert.Equal(new[] { "first", "last" }, calls);
        Assert.Throws<PyTypeError>(() => PyWindowsPath.Join([]));
        Assert.Throws<PyNotImplementedError>(() => PyWindowsPath.Join([new PyBytes([65])]));
    }

    [Fact]
    public void UnicodeOrdinaryTailIsKeptButUnimplementedDriveCaseMappingIsNotGuessed()
    {
        Assert.Equal("C:/中文 &目录", PyWindowsPath.DirName("C:/中文 &目录/文件.py"));
        Assert.Equal("C:/中文 &目录\\数据.txt", PyWindowsPath.Join(["C:/中文 &目录", "数据.txt"]));
        Assert.Equal("//服务/共享/new", PyWindowsPath.Join(["//服务/共享/old", "/new"]));
        Assert.Equal("Ä:a\\b", PyWindowsPath.Join(["Ä:a", "Ä:b"]));
        Assert.Throws<PyNotImplementedError>(() => PyWindowsPath.Join(["Ä:a", "ä:b"]));
    }

    [Theory]
    [InlineData("\\\\?\\C:\\a")]
    [InlineData("//?/C:/a")]
    [InlineData("/\\?\\C:/a")]
    [InlineData("\\/./C:/a")]
    [InlineData("\\\\.\\pipe\\name")]
    public void DeviceNamespacesRemainExplicitlyUnsupported(string path)
    {
        Assert.Throws<PyNotImplementedError>(() => PyWindowsPath.DirName(path));
        Assert.Throws<PyNotImplementedError>(() => PyWindowsPath.Join([path, "tail"]));
    }

    [Fact]
    public void PublicPlatformDispatchPreservesExplicitNormalizationBoundary()
    {
        var attributes = PyOsPath.CreateAttributes();
        if (OperatingSystem.IsWindows())
        {
            Assert.Equal("C:/中文 &目录", PyOps.CallValue(attributes["dirname"], ["C:/中文 &目录/file.py"], [], []));
            Assert.Equal("C:/中文 &目录\\data.txt", PyOps.CallValue(attributes["join"], ["C:/中文 &目录", "data.txt"], [], []));
            Assert.Throws<PyTypeError>(() => PyOps.CallValue(attributes["join"], ["a"], ["bad"], ["b"]));
            Assert.Equal("a", PyOsPath.BaseName("C:/a"));
            Assert.Throws<PyNotImplementedError>(() => PyOsPath.Normalize("C:/a"));
            Assert.Throws<PyNotImplementedError>(() => PyOsPath.Absolute("C:/a"));
            Assert.Equal(new object?[] { "C:/a", ".txt" }, PyOsPath.SplitExtension("C:/a.txt").Items);
        }
        else
        {
            Assert.Equal("C:", PyOsPath.DirName("C:/file.py"));
            Assert.Equal("C:/a/b", PyOsPath.Join(["C:/a", "b"]));
        }
    }
}
