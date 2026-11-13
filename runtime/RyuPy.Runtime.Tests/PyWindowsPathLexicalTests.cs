// 固定CPython 3.12.13 PureWindowsPath/ntpath矩阵；参考脚本位于tests/validation/windows_path。
using System;
using RyuPy.Runtime;
using Xunit;

namespace RyuPy.Runtime.Tests;

public class PyWindowsPathLexicalTests
{
    [InlineData("", ".", "", "", ".", false, "", "")]
    [InlineData(".", ".", "", "", ".", false, "", ".")]
    [InlineData("a//./b/../.hidden.txt/", "a\\b\\..\\.hidden.txt", "", ".hidden.txt", "a\\b\\..", false, "a|b|..|.hidden.txt", "")]
    [InlineData("file.", "file.", "", "file.", ".", false, "file.", "file.")]
    [InlineData(".hidden", ".hidden", "", ".hidden", ".", false, ".hidden", ".hidden")]
    [InlineData("..", "..", "", "..", ".", false, "..", "..")]
    [InlineData("C:", "C:", "C:", "", "C:", false, "C:", "")]
    [InlineData("C:.", "C:", "C:", "", "C:", false, "C:", ".")]
    [InlineData("C:foo", "C:foo", "C:", "foo", "C:", false, "C:|foo", "foo")]
    [InlineData("C:/", "C:\\", "C:\\", "", "C:\\", true, "C:\\", "")]
    [InlineData("C:/a//./b/../.hidden.txt/", "C:\\a\\b\\..\\.hidden.txt", "C:\\", ".hidden.txt", "C:\\a\\b\\..", true, "C:\\|a|b|..|.hidden.txt", "")]
    [InlineData("\\foo", "\\foo", "\\", "foo", "\\", false, "\\|foo", "foo")]
    [InlineData("\\", "\\", "\\", "", "\\", false, "\\", "")]
    [InlineData("//server/share", "\\\\server\\share\\", "\\\\server\\share\\", "", "\\\\server\\share\\", true, "\\\\server\\share\\", "")]
    [InlineData("//server/share/a/..", "\\\\server\\share\\a\\..", "\\\\server\\share\\", "..", "\\\\server\\share\\a", true, "\\\\server\\share\\|a|..", "..")]
    [InlineData("//server/share/dir/file.txt", "\\\\server\\share\\dir\\file.txt", "\\\\server\\share\\", "file.txt", "\\\\server\\share\\dir", true, "\\\\server\\share\\|dir|file.txt", "file.txt")]
    [InlineData("中文/文件.txt", "中文\\文件.txt", "", "文件.txt", "中文", false, "中文|文件.txt", "文件.txt")]
    [InlineData("./C:foo", ".\\C:foo", "", "C:foo", ".", false, "C:foo", "C:foo")]
    [InlineData("/:a", "\\:a", "\\", ":a", "\\", false, "\\|:a", ":a")]
    [InlineData("/:.", "\\:.", "\\", ":.", "\\", false, "\\|:.", ":.")]
    [InlineData("/:/a", "\\:\\a", "\\", "a", "\\:", false, "\\|:|a", "a")]
    [InlineData("\\:a", "\\:a", "\\", ":a", "\\", false, "\\|:a", ":a")]
    [Theory]
    public void PurePathMatrix(string input, string clean, string anchor, string name,
        string parent, bool absolute, string parts, string basename)
    {
        Assert.Equal(clean, PyWindowsPath.Clean(input));
        Assert.Equal(anchor, PyWindowsPath.Anchor(input));
        Assert.Equal(name, PyWindowsPath.Name(input));
        Assert.Equal(parent, PyWindowsPath.Parent(input));
        Assert.Equal(absolute, PyWindowsPath.IsAbsolute(input));
        Assert.Equal(parts, string.Join("|", PyWindowsPath.Parts(input)));
        Assert.Equal(basename, PyWindowsPath.BaseName(input));
    }

    [Theory]
    [InlineData(@"\\?\C:\a")]
    [InlineData(@"\\.\pipe\name")]
    [InlineData("//server")]
    [InlineData("///a")]
    public void UnsupportedPathFormsFailBeforeFilesystem(string input)
    {
        Assert.Throws<PyNotImplementedError>(() => PyWindowsPath.Clean(input));
        Assert.Throws<PyNotImplementedError>(() => PyWindowsPath.Parts(input));
        Assert.Throws<PyNotImplementedError>(() => PyWindowsPath.IsAbsolute(input));
    }

    [Theory]
    [InlineData("C:foo", false, false)]
    [InlineData(@"\foo", false, true)]
    [InlineData(@"C:\foo", true, true)]
    [InlineData(@"\\server\share", true, true)]
    public void Os312AndPathlibAbsoluteRulesDiffer(string path, bool pure, bool os)
    {
        Assert.Equal(pure, PyWindowsPath.IsAbsolute(path));
        Assert.Equal(os, PyWindowsPath.OsIsAbsolute(path));
    }

    [Theory]
    [InlineData("/:a", "/", ":a", "C:/:a")]
    [InlineData("/:/a", "/:", "a", "C:/:/a")]
    public void RootRelativeColonIsNotDrive(string input, string parent, string name, string joined)
    {
        Assert.Equal(parent, PyWindowsPath.DirName(input));
        Assert.Equal(name, PyWindowsPath.BaseName(input));
        Assert.Equal(joined, PyWindowsPath.Join(["C:/base", input]));
    }

    [Fact]
    public void ComparisonIsAsciiOnlyAndDoesNotGuessUnicodeCaseMapping()
    {
        Assert.Equal(PyWindowsPath.ComparisonKey("C:/Foo"), PyWindowsPath.ComparisonKey(@"c:\foo"));
        Assert.Throws<PyNotImplementedError>(() => PyWindowsPath.ComparisonKey("中文"));
    }
}
