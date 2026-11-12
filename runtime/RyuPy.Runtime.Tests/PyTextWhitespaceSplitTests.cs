// Python空白统一合同与split完整参数：不依赖toml，不按调用形态放宽结果。
using System.Numerics;
using Xunit;

namespace RyuPy.Runtime.Tests;

public class PyTextWhitespaceSplitTests
{
    private static object? Call(object receiver, string method, object?[] args, string[]? names = null, object?[]? values = null)
        => PyOps.CallMethod(receiver, method, args, names ?? [], values ?? []);
    private static object? Bound(object receiver, string method, object?[] args, string[] names, object?[] values)
        => PyOps.CallValue(PyObject.GetAttr(receiver, method), args, names, values);
    private static object? Root(string method, object?[] args, string[] names, object?[] values)
        => PyOps.CallValue(PyObject.GetAttr(PyType.Get("str"), method), args, names, values);
    private static void Parts(object? actual, params object?[] expected)
        => Assert.Equal(expected, Assert.IsType<PyList>(actual).Items);

    [Fact]
    public void AllPythonWhitespaceIsSharedWithDecimalAndDefaultStrip()
    {
        const string spaces = "\t\n\v\f\r \u001c\u001d\u001e\u001f\u0085\u00a0\u1680\u2000\u2001\u2002\u2003\u2004\u2005\u2006\u2007\u2008\u2009\u200a\u2028\u2029\u202f\u205f\u3000";
        foreach (char ch in spaces)
        {
            string space = ch.ToString();
            Assert.Equal(true, Call(space, "isspace", []));
            Assert.Equal(true, Bound(space, "isspace", [], [], []));
            Assert.Equal(true, Root("isspace", [space], [], []));
            Assert.Equal("1.20", Call(space + "1.20" + space, "strip", []));
            Assert.Equal("1.20", PyRepr.Str(PyDecimalText.Parse(space + "1.20" + space)));
            Parts(Call(space + "a" + space + "b" + space, "split", []), "a", "b");
        }
        foreach (string text in new[] { "", "x", " x ", "\u200b", "\ufeff", "\ud800" })
            Assert.Equal(false, Call(text, "isspace", []));
        Assert.Equal("\u001ca\u001c", Call("\u001ca\u001c", "strip", [" "]));
    }

    [Fact]
    public void SplitAllCallShapesBindKeywordsAndPreserveRemainder()
    {
        Parts(Call("a=b=c", "split", ["=", 1L]), "a", "b=c");
        Parts(Bound("a=b=c", "split", [], ["maxsplit", "sep"], [1L, "="]), "a", "b=c");
        Parts(Root("split", ["a=b=c"], ["sep", "maxsplit"], ["=", 1L]), "a", "b=c");
        Parts(Call("  a  b  ", "split", [PyNone.Instance, 0L]), "a  b  ");
        Parts(Call("  a  b  ", "split", [], ["maxsplit"], [1L]), "a", "b  ");
        Parts(Call("  ", "split", []));
        Parts(Call("", "split", [","]), "");
        Parts(Call("a,,", "split", [","]), "a", "", "");
        Parts(Call("😀x😀", "split", ["\ud83d"]), "😀x😀");
        Parts(Call("😀x😀", "split", ["😀"]), "", "x", "");
        Parts(Call("a\ud83db", "split", ["\ud83d"]), "a", "b");
    }

    [Fact]
    public void InvalidArgumentsAreNotIgnored()
    {
        Assert.Throws<PyValueError>(() => Call("", "split", ["", 0L]));
        Assert.Throws<PyTypeError>(() => Call("x", "split", [42L]));
        Assert.Throws<PyOverflowError>(() => Call("", "split", ["", BigInteger.One << 100]));
        Assert.Throws<PyTypeError>(() => Bound("x", "split", [","], ["sep"], [":"]));
        Assert.Throws<PyTypeError>(() => Call("x", "split", [], ["bogus"], [1L]));
        Assert.Throws<PyTypeError>(() => Call("x", "isspace", [1L]));
        Assert.Throws<PyTypeError>(() => Bound("x", "isspace", [], ["bogus"], [1L]));
        Assert.Throws<PyTypeError>(() => Root("isspace", [42L], [], []));
    }
    [Fact]
    public void IndexRunsOnceBeforeSeparatorValidationAndDoesNotRereadArguments()
    {
        int calls = 0;
        var cls = new PyClass("SplitIndex");
        object?[] args = ["=", null];
        cls.SetAttr("__index__", PyNativeFunctions.CreateVariadic("index", _ =>
        { calls++; args[0] = "wrong"; return 1L; }));
        var index = new PyInstance(cls); args[1] = index;
        Parts(Call("a=b=c", "split", args), "a", "b=c");
        Assert.Equal(1, calls);
        Assert.Throws<PyTypeError>(() => Call("", "split", [42L, index]));
        Assert.Equal(2, calls);
        Assert.Throws<PyValueError>(() => Call("", "split", ["", index]));
        Assert.Equal(3, calls);
        Assert.Throws<PyTypeError>(() => Call("", "split", [], ["bad", "maxsplit"], [0L, index]));
        Assert.Equal(3, calls);
    }

    [Fact]
    public void ExactBuiltinClassUsesRealTypeWithoutInterceptingUserDescriptors()
    {
        object?[] values = [new PyDict(), new PyList(), new PyTuple([]), new PySet(),
            "", 1L, 1, BigInteger.One, true, 1.0, PyNone.Instance, null];
        foreach (object? value in values)
            Assert.Same(PyObject.TypeOf(value), PyObject.GetAttr(value, "__class__"));
        var cls = new PyClass("Masked");
        cls.SetAttr("__class__", new PyProperty(PyNativeFunctions.CreateVariadic("getclass", _ => "masked")));
        var instance = new PyInstance(cls);
        Assert.Equal("masked", PyObject.GetAttr(instance, "__class__"));
        Assert.Same(cls, PyObject.TypeOf(instance));
    }

}
