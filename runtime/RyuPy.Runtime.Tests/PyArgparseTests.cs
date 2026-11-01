// 解析器测试用显式 argv，帮助输出返回字符串，不依赖测试宿主命令行。
using Xunit;
namespace RyuPy.Runtime.Tests;
public class PyArgparseTests
{
    private static object Parser() => PyOps.CallValue(PyArgparse.CreateAttributes()["ArgumentParser"], [], ["prog"], ["fixture"])!;
    private static object? Call(object target, string name, object?[] args, string[]? keys = null, object?[]? values = null) =>
        PyOps.CallValue(PyObject.GetAttr(target, name), args, keys ?? [], values ?? []);
    [Fact]
    public void PositionalsOptionsConversionAliasesAndDefaultsAreParsed()
    {
        object parser = Parser(); Call(parser, "add_argument", ["name"]);
        Call(parser, "add_argument", ["-n", "--count"], ["type", "default"], [PyType.Get("int"), 1L]);
        Call(parser, "add_argument", ["-v", "--verbose"], ["action"], ["store_true"]);
        object result = Call(parser, "parse_args", [new PyList(["cat", "-vn3"])])!;
        Assert.Equal("cat", PyObject.GetAttr(result, "name")); Assert.Equal(3L, PyObject.GetAttr(result, "count"));
        Assert.Equal(true, PyObject.GetAttr(result, "verbose"));
        result = Call(parser, "parse_args", [new PyList(["--cou=-2", "cat"])])!;
        Assert.Equal(-2L, PyObject.GetAttr(result, "count")); Assert.Equal(false, PyObject.GetAttr(result, "verbose"));
        result = Call(parser, "parse_args", [new PyList(["--", "-cat"])])!;
        Assert.Equal("-cat", PyObject.GetAttr(result, "name")); Assert.Equal(1L, PyObject.GetAttr(result, "count"));
    }
    [Fact]
    public void HelpUsesMetadataRatherThanHardcodedArgumentNames()
    {
        object parser = Parser(); Call(parser, "add_argument", ["name"], ["help"], ["name to print"]);
        Assert.Same(PyNone.Instance, PyObject.GetAttr(parser, "description"));
        string help = (string)Call(parser, "format_help", [])!;
        Assert.Contains("usage: fixture [-h] name\n", help);
        Assert.Contains("positional arguments:\n  name        name to print\n", help);
        Assert.Contains("-h, --help  show this help message and exit", help);
        Assert.Throws<PyNotImplementedError>(() => Call(parser, "add_argument", ["--many"], ["nargs"], ["+"]));
    }
    [Fact]
    public void NamespacePreservesExistingValuesAndStringDefaultsConvertOnlyWhenMissing()
    {
        object parser = Parser(); Call(parser, "add_argument", ["--count"], ["type", "default"], [PyType.Get("int"), "3"]);
        object result = Call(parser, "parse_args", [new PyList()])!;
        Assert.Equal(3L, PyObject.GetAttr(result, "count"));
        var supplied = new PyArgparseNamespace(); supplied.SetAttr("count", 9L);
        Assert.Same(supplied, Call(parser, "parse_args", [new PyList(), supplied]));
        Assert.Equal(9L, supplied.GetAttr("count"));
    }
}
