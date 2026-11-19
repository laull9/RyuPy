// getopt 模块及解析的定向测试：身份、完整扫描、码点和长参数回调；不重复异常类型套件。
using System;
using System.Collections.Generic;
using RyuPy.Runtime;
using Xunit;
namespace RyuPy.Runtime.Tests;

[Collection("生成器帧栈")]
public class PyGetoptTests
{
    public PyGetoptTests() { PySystem.Initialize("getopt-tests.py", []); PyNativeModules.Register(); }
    private static readonly Dictionary<string, object?> Module = PyGetopt.CreateAttributes();
    private static PyTuple Call(object?[] args, string[]? names = null, object?[]? values = null) =>
        Assert.IsType<PyTuple>(PyOps.CallValue(Module["getopt"], args, names ?? [], values ?? []));
    private static PyTuple Parse(object args, object? shortopts = null, object? longopts = null) =>
        Call([args, shortopts ?? "", longopts ?? new PyList()]);
    private static PyList Options(PyTuple result) => Assert.IsType<PyList>(result.Items[0]);
    private static PyTuple Pair(PyTuple result, int index = 0) => Assert.IsType<PyTuple>(Options(result).Items[index]);
    private static object Truth(Func<object?> body)
    {
        var cls = (PyClass)PyOps.MakeClass("ValueTruth", [], null);
        cls.SetAttr("__bool__", new PyFunction("__bool__", PySignature.Create("__bool__", ["self"], ["pos"], ["0"]),
            (_, _, _, _) => body(), []));
        return Assert.IsType<PyInstance>(cls.Invoke([], [], []));
    }

    [Fact]
    public void ModuleHasRealFunctionAndSingleExceptionAlias()
    {
        var module = PyModules.Import("getopt");
        Assert.Same(Module["getopt"], module.GetAttr("getopt"));
        Assert.Same(PyType.Get("GetoptError"), module.GetAttr("GetoptError"));
        Assert.Same(module.GetAttr("GetoptError"), module.GetAttr("error"));
        Assert.Throws<PyAttributeError>(() => module.GetAttr("gnu_getopt"));
        Assert.Throws<PyNotImplementedError>(() => module.SetAttr("GetoptError", null));
        Assert.Throws<PyNotImplementedError>(() => module.SetAttr("error", null));
    }

    [Fact]
    public void BinderHasPythonKeywordsAndExactReferenceErrors()
    {
        Assert.Equal("('-a', '')", PyRepr.Repr(Pair(Call([], ["args", "shortopts"], [new PyList(["-a"]), "a"]))));
        var cases = new (object?[] Args, string[] Names, object?[] Values, string Message)[]
        {
            ([], [], [], "getopt() missing 2 required positional arguments: 'args' and 'shortopts'"),
            ([new PyList()], [], [], "getopt() missing 1 required positional argument: 'shortopts'"),
            ([new PyList(), "", new PyList(), 1L], [], [], "getopt() takes from 2 to 3 positional arguments but 4 were given"),
            ([new PyList(), ""], ["unknown"], [1L], "getopt() got an unexpected keyword argument 'unknown'"),
            ([new PyList(), ""], ["args"], [new PyList()], "getopt() got multiple values for argument 'args'"),
        };
        foreach (var item in cases)
            Assert.Equal(item.Message, Assert.Throws<PyTypeError>(() => Call(item.Args, item.Names, item.Values)).PyMessage);
    }

    [Fact]
    public void NoConsumptionPreservesIdentityAndDoesNotReadUnusedMetadata()
    {
        object[] sources = [new PyList(), new PyTuple([]), new PyList(["word", 7L]), new PyTuple(["-", 7L])];
        foreach (object source in sources)
        {
            PyTuple result = Call([source, PyNone.Instance, new PyList([7L])]);
            Assert.Empty(Options(result).Items); Assert.Same(source, result.Items[1]);
        }
    }

    [Fact]
    public void ConsumptionUsesRealTailSlicesAndPreservesTupleAndValueIdentity()
    {
        object marker = new();
        var list = new PyList(["--", marker]); var tuple = new PyTuple(["-a", "rest", marker]);
        PyTuple first = Parse(list); PyTuple second = Parse(tuple, "a");
        Assert.NotSame(list, first.Items[1]); Assert.Same(marker, Assert.IsType<PyList>(first.Items[1]).Items[0]);
        Assert.NotSame(tuple, second.Items[1]); Assert.Same(marker, Assert.IsType<PyTuple>(second.Items[1]).Items[1]);
        Assert.Equal(2, list.Count); Assert.Equal(3, tuple.Length);
        Assert.Equal("('-a', '')", PyRepr.Repr(Pair(second)));
        Assert.Equal("(['-a'],)", PyRepr.Repr(new PyTuple([Parse(new PyList(["--", "-a"])).Items[1]])));
    }

    [Fact]
    public void ShortClustersKeepFirstDeclarationCodepointsAndArguments()
    {
        Assert.Equal("[('-a', ''), ('-a', ''), ('-b', 'value')]", PyRepr.Repr(Options(Parse(new PyList(["-aabvalue"]), "ab:"))));
        Assert.Equal("[('-😀', ''), ('-é', ''), ('-x', '')]", PyRepr.Repr(Options(Parse(new PyList(["-😀éx"]), "😀éx"))));
        Assert.Equal("[('-😀', 'payload')]", PyRepr.Repr(Options(Parse(new PyList(["-😀payload"]), "😀:"))));
        Assert.Equal("[('-e', ''), ('-́', '')]", PyRepr.Repr(Options(Parse(new PyList(["-é"]), "é"))));
        Assert.Equal("[('-a', '')]", PyRepr.Repr(Options(Parse(new PyList(["-a", "tail"]), "aa:"))));
        Assert.Equal("--", Pair(Parse(new PyList(["-b", "--"]), "b::")).Items[1]);
        Assert.Equal("-\ud800", Pair(Parse(new PyList(["-\ud800"]), "\ud800")).Items[0]);
        Assert.Equal("-+", Pair(Parse(new PyList(["-+"]), "+")).Items[0]);
    }

    [Theory]
    [InlineData("-z", "a", "", "option -z not recognized", "z")]
    [InlineData("-:", ":", "", "option -: not recognized", ":")]
    [InlineData("-b", "b:", "", "option -b requires argument", "b")]
    [InlineData("--z", "", "alpha", "option --z not recognized", "z")]
    [InlineData("--al", "", "alpha=", "option --alpha requires argument", "alpha")]
    [InlineData("--al=", "", "alpha", "option --alpha must not have an argument", "alpha")]
    public void RealGetoptErrorsUseCanonicalOption(string token, string shorts, string longs, string message, string option)
    {
        var error = Assert.Throws<PyGetoptError>(() => Parse(new PyList([token]), shorts, longs));
        Assert.Equal(message, PyRepr.Str(error)); Assert.Equal(message, error.Args[0]); Assert.Equal(option, error.Args[1]);
        Assert.Equal(option, PyObject.GetAttr(error, "opt"));
    }

    [Fact]
    public void LongMatchingDoesNotDeduplicateAndExactNoArgumentWins()
    {
        Assert.Equal("('--alpha', '')", PyRepr.Repr(Pair(Parse(new PyList(["--alpha"]), "", new PyTuple(["alpha", "alpha"])))));
        Assert.Equal("('--alpha', 'x=y')", PyRepr.Repr(Pair(Parse(new PyList(["--al=x=y"]), "", "alpha="))));
        Assert.Equal("('--alpha', '')", PyRepr.Repr(Pair(Parse(new PyList(["--alpha="]), "", "alpha="))));
        Assert.Equal("option --al not a unique prefix", Assert.Throws<PyGetoptError>(() => Parse(new PyList(["--al"]), "", new PyList(["alpha", "alpha"]))).PyMessage);
        Assert.Equal("option --al not a unique prefix", Assert.Throws<PyGetoptError>(() => Parse(new PyList(["--al"]), "", new PyList(["alpha", "alpine"]))).PyMessage);
        foreach (object declarations in new object[] { new PyList(["alpha=", "alpha"]), new PyList(["alpha", "alpha="]) })
            Assert.Equal("option --alpha must not have an argument", Assert.Throws<PyGetoptError>(() => Parse(new PyList(["--alpha=x"]), "", declarations)).PyMessage);
    }

    [Fact]
    public void BadOrdinaryValuesFailOnlyWhenUsedAndLongScanningCompletes()
    {
        foreach (object source in new object[] { new PyList(), new PyList([1L]), new PyList(["-z"]) })
            Assert.Equal("'NoneType' object is not iterable", Assert.Throws<PyTypeError>(() => Call([source, "", PyNone.Instance])).PyMessage);
        Assert.Equal("'int' object is not iterable", Assert.Throws<PyTypeError>(() => Call([new PyList(), "", 1L])).PyMessage);
        Assert.Equal("'int' object has no attribute 'startswith'", Assert.Throws<PyAttributeError>(() => Parse(new PyList([1L]))).PyMessage);
        Assert.Equal("'NoneType' object has no attribute 'startswith'", Assert.Throws<PyAttributeError>(() => Parse(new PyList([PyNone.Instance]))).PyMessage);
        Assert.Equal("object of type 'NoneType' has no len()", Assert.Throws<PyTypeError>(() => Call([new PyList(["-a"]), PyNone.Instance])).PyMessage);
        Assert.Equal("'int' object has no attribute 'startswith'", Assert.Throws<PyAttributeError>(() => Parse(new PyList(["--help"]), "", new PyList(["help", 1L]))).PyMessage);
        Assert.Equal("[('-a', '')]", PyRepr.Repr(Options(Parse(new PyList(["-a"]), "a", new PyList([1L])))));
    }

    [Fact]
    public void ShortValuesNeverCallTruthAndLongValuesPreserveTruthyIdentity()
    {
        int calls = 0; object value = Truth(() => { calls++; return true; });
        Assert.Same(value, Pair(Parse(new PyList(["-b", value]), "b:")).Items[1]); Assert.Equal(0, calls);
        Assert.Same(value, Pair(Parse(new PyList(["--beta", value]), "", "beta=")).Items[1]); Assert.Equal(1, calls);
        Assert.Same(PyNone.Instance, Pair(Parse(new PyList(["-b", PyNone.Instance]), "b:")).Items[1]);
        foreach (object? item in new object?[] { PyNone.Instance, false, 0L, "" })
            Assert.Equal("", Pair(Parse(new PyList(["--beta", item]), "", "beta=")).Items[1]);
    }

    [Fact]
    public void LongValueTruthSeesIsolatedTailAndOriginalExceptionContext()
    {
        var source = new PyList(); int calls = 0;
        object value = Truth(() => { calls++; source.Items.Clear(); return true; });
        source.Items.AddRange(["--beta", value, "rest"]);
        PyTuple result = Parse(source, "", "beta=");
        Assert.Equal(1, calls); Assert.Empty(source.Items); Assert.Equal("['rest']", PyRepr.Repr(result.Items[1]));
        Assert.Same(value, Pair(result).Items[1]);

        var outer = new PyValueError("outer"); var original = new PyValueError("value-bool");
        object failing = Truth(() => throw (PyBaseException)PyErrors.PrepareRaise(original));
        object? previous = PyErrors.EnterHandler(outer);
        try
        {
            var caught = Assert.Throws<PyValueError>(() => Parse(new PyList(["--beta", failing]), "", "beta="));
            Assert.Same(original, caught); Assert.Same(outer, caught.Context);
        }
        finally { PyErrors.LeaveHandler(previous); }
    }

    [Fact]
    public void OwnedErrorsAttachContextAndCustomProtocolIsExplicitlyUnsupported()
    {
        var outer = new PyValueError("outer"); object? previous = PyErrors.EnterHandler(outer);
        try
        {
            Assert.Same(outer, Assert.Throws<PyGetoptError>(() => Parse(new PyList(["-z"]))).Context);
            Assert.Same(outer, Assert.Throws<PyTypeError>(() => Call([])).Context);
            Assert.Same(outer, Assert.Throws<PyAttributeError>(() => Parse(new PyList([1L]))).Context);
        }
        finally { PyErrors.LeaveHandler(previous); }
        int calls = 0; object custom = Truth(() => { calls++; return true; });
        Assert.Throws<PyNotImplementedError>(() => Call([new PyList(), "", custom]));
        Assert.Throws<PyNotImplementedError>(() => Call([custom, ""]));
        Assert.Throws<PyNotImplementedError>(() => Parse(new PyList([custom])));
        Assert.Throws<PyNotImplementedError>(() => Parse(new PyList(["-a"]), custom));
        Assert.Equal(0, calls);
        Assert.Same(custom, Assert.IsType<PyList>(Parse(new PyList(["operand", custom])).Items[1]).Items[1]);
    }
}
