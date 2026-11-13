// 固定CPython3.12.13词法参考；extended保留身份，不剥前缀，不推广到其他设备namespace。
using RyuPy.Runtime;
using Xunit;
namespace RyuPy.Runtime.Tests;
public class PyWindowsExtendedPathTests
{
    [InlineData("\\\\?\\C:\\", "\\\\?\\C:\\", "\\\\?\\C:\\", "", "\\\\?\\C:\\", true, "\\\\?\\C:\\", "", "\\\\?\\C:\\")]
    [InlineData("\\\\?\\C:\\a\\file.", "\\\\?\\C:\\a\\file.", "\\\\?\\C:\\", "file.", "\\\\?\\C:\\a", true, "\\\\?\\C:\\|a|file.", "file.", "\\\\?\\C:\\a")]
    [InlineData("\\\\?\\C:\\a \\b ", "\\\\?\\C:\\a \\b ", "\\\\?\\C:\\", "b ", "\\\\?\\C:\\a ", true, "\\\\?\\C:\\|a |b ", "b ", "\\\\?\\C:\\a ")]
    [InlineData("\\\\?\\C:\\a\\.\\..\\b", "\\\\?\\C:\\a\\..\\b", "\\\\?\\C:\\", "b", "\\\\?\\C:\\a\\..", true, "\\\\?\\C:\\|a|..|b", "b", "\\\\?\\C:\\a\\.\\..")]
    [InlineData("\\\\?\\C:\\中文 &\\file.txt", "\\\\?\\C:\\中文 &\\file.txt", "\\\\?\\C:\\", "file.txt", "\\\\?\\C:\\中文 &", true, "\\\\?\\C:\\|中文 &|file.txt", "file.txt", "\\\\?\\C:\\中文 &")]
    [InlineData("\\\\?\\UNC\\server\\share", "\\\\?\\UNC\\server\\share\\", "\\\\?\\UNC\\server\\share\\", "", "\\\\?\\UNC\\server\\share\\", true, "\\\\?\\UNC\\server\\share\\", "", "\\\\?\\UNC\\server\\share")]
    [InlineData("\\\\?\\UNC\\server\\share\\dir\\..", "\\\\?\\UNC\\server\\share\\dir\\..", "\\\\?\\UNC\\server\\share\\", "..", "\\\\?\\UNC\\server\\share\\dir", true, "\\\\?\\UNC\\server\\share\\|dir|..", "..", "\\\\?\\UNC\\server\\share\\dir")]
    [InlineData("\\\\?\\UNC\\server\\share\\tail. ", "\\\\?\\UNC\\server\\share\\tail. ", "\\\\?\\UNC\\server\\share\\", "tail. ", "\\\\?\\UNC\\server\\share\\", true, "\\\\?\\UNC\\server\\share\\|tail. ", "tail. ", "\\\\?\\UNC\\server\\share\\")]
    [InlineData("\\\\?\\unc\\server\\share\\file", "\\\\?\\unc\\server\\share\\file", "\\\\?\\unc\\server\\share\\", "file", "\\\\?\\unc\\server\\share\\", true, "\\\\?\\unc\\server\\share\\|file", "file", "\\\\?\\unc\\server\\share\\")]
    [InlineData("//?/C:/a", "\\\\?\\C:\\a", "\\\\?\\C:\\", "a", "\\\\?\\C:\\", true, "\\\\?\\C:\\|a", "a", "//?/C:/")]
    [InlineData("\\\\?\\C:/a", "\\\\?\\C:\\a", "\\\\?\\C:\\", "a", "\\\\?\\C:\\", true, "\\\\?\\C:\\|a", "a", "\\\\?\\C:/")]
    [InlineData("//?/UNC/server/share", "\\\\?\\UNC\\server\\share\\", "\\\\?\\UNC\\server\\share\\", "", "\\\\?\\UNC\\server\\share\\", true, "\\\\?\\UNC\\server\\share\\", "", "//?/UNC/server/share")]
    [InlineData("//?/unc/server/share/dir", "\\\\?\\unc\\server\\share\\dir", "\\\\?\\unc\\server\\share\\", "dir", "\\\\?\\unc\\server\\share\\", true, "\\\\?\\unc\\server\\share\\|dir", "dir", "//?/unc/server/share/")]
    [InlineData("\\\\?\\UNC/server/share/a", "\\\\?\\UNC\\server\\share\\a", "\\\\?\\UNC\\server\\share\\", "a", "\\\\?\\UNC\\server\\share\\", true, "\\\\?\\UNC\\server\\share\\|a", "a", "\\\\?\\UNC/server/share/")]
    [Theory]
    public void ExtendedMatrix(string input, string clean, string anchor, string name,
        string parent, bool absolute, string parts, string basename, string dirname)
    {
        Assert.Equal(clean, PyWindowsPath.Clean(input));
        Assert.Equal(anchor, PyWindowsPath.Anchor(input));
        Assert.Equal(name, PyWindowsPath.Name(input));
        Assert.Equal(parent, PyWindowsPath.Parent(input));
        Assert.Equal(absolute, PyWindowsPath.IsAbsolute(input));
        Assert.Equal(parts, string.Join("|", PyWindowsPath.Parts(input)));
        Assert.Equal(basename, PyWindowsPath.BaseName(input));
        Assert.Equal(dirname, PyWindowsPath.DirName(input));
    }
    [InlineData("\\\\?\\C:\\a", "\\b", "\\\\?\\C:\\b")]
    [InlineData("\\\\?\\C:\\a", "C:b", "C:b")]
    [InlineData("C:\\a", "\\\\?\\C:\\b", "\\\\?\\C:\\b")]
    [InlineData("\\\\?\\C:\\a", "b", "\\\\?\\C:\\a\\b")]
    [InlineData("\\\\?\\UNC\\server\\share\\a", "\\b", "\\\\?\\UNC\\server\\share\\b")]
    [InlineData("\\\\?\\UNC\\server\\share", "b", "\\\\?\\UNC\\server\\share\\b")]
    [InlineData("\\\\?\\C:\\a", "", "\\\\?\\C:\\a\\")]
    [InlineData("//?/C:/a", "/b", "//?/C:/b")]
    [InlineData("//?/UNC/server/share", "b", "//?/UNC/server/share\\b")]
    [InlineData("\\\\?\\C:/a", "b", "\\\\?\\C:/a\\b")]
    [InlineData("//?/unc/server/share/a", "/b", "//?/unc/server/share/b")]
    [Theory]
    public void ExtendedJoin(string first, string second, string expected) =>
        Assert.Equal(expected, PyWindowsPath.Join([first, second]));
    [Fact]
    public void PrefixAndTrailingCharactersRemainDistinct()
    {
        Assert.NotEqual(PyWindowsPath.ComparisonKey(@"C:\a"), PyWindowsPath.ComparisonKey(@"\\?\C:\a"));
        Assert.NotEqual(PyWindowsPath.ComparisonKey(@"\\?\C:\a."), PyWindowsPath.ComparisonKey(@"\\?\C:\a"));
        Assert.Equal(PyWindowsPath.ComparisonKey(@"\\?\C:\A"), PyWindowsPath.ComparisonKey(@"\\?\c:\a"));
    }
    [Theory]
    [InlineData(@"\\.\pipe\name")]
    [InlineData(@"\\?\GLOBALROOT\Device\HarddiskVolume1")]
    [InlineData(@"\\?\Volume{1234}\file")]
    [InlineData(@"\\?\C:relative")]
    [InlineData(@"\\?\UNC\server")]
    [InlineData(@"\\?\UNC\server\")]
    [InlineData(@"\\?\UNC\\share")]
    public void UnsupportedDeviceFormsStayRejected(string input)
    {
        Assert.Throws<PyNotImplementedError>(() => PyWindowsPath.Clean(input));
        Assert.Throws<PyNotImplementedError>(() => PyWindowsPath.Join([input, "x"]));
    }
}
