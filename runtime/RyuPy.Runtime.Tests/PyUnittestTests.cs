using RyuPy.Runtime;
using Xunit;

namespace RyuPy.Runtime.Tests;

/// <summary>原生 TestCase 实际绑定、断言上下文与结构化结果的基本契约。</summary>
public class PyUnittestTests
{
    private static object? Call(object? obj, string name, params object?[] args) => PyOps.CallValue(PyObject.GetAttr(obj, name), args, [], []);
    private static object Case() => PyUnittest.TestCase.Invoke([], [], [])!;

    [Fact]
    public void AssertionsBindSelfAndRaisePythonFailure()
    {
        object test = Case();
        Call(test, "assertEqual", 1L, 1L);
        Assert.Throws<PyAssertionError>(() => Call(test, "assertEqual", 1L, 2L));
        Call(test, "assertTrue", new PyList([1L]));
        Assert.Throws<PyAssertionError>(() => Call(test, "assertIsNone", 1L));
    }

    [Fact]
    public void RaisesContextRetainsMatchedExceptionAndDoesNotSwallowWrongType()
    {
        object test = Case();
        object context = Call(test, "assertRaises", PyType.Get("ValueError"))!;
        Assert.Same(context, Call(context, "__enter__"));
        var error = new PyValueError("caught");
        Assert.Equal(true, Call(context, "__exit__", PyType.Get("ValueError"), error, PyNone.Instance));
        Assert.Same(error, PyObject.GetAttr(context, "exception"));
        Assert.Equal(false, Call(context, "__exit__", PyType.Get("TypeError"), new PyTypeError("wrong"), PyNone.Instance));
        Assert.Throws<PyAssertionError>(() => Call(context, "__exit__", PyNone.Instance, PyNone.Instance, PyNone.Instance));
    }

    [Fact]
    public void CaseRunCallsInstanceOverrideAndTracksErrors()
    {
        object test = Case();
        PyObject.SetAttr(test, "runTest", PyNativeFunctions.Create("runTest", [], [], 0, _ => throw new PyValueError("oops")));
        var result = new PyUnittestResult();
        Assert.Same(result, Call(test, "run", result));
        Assert.Equal(1L, PyObject.GetAttr(result, "testsRun"));
        Assert.Single(((PyList)PyObject.GetAttr(result, "errors")!).Items);
        Assert.Equal(false, Call(result, "wasSuccessful"));
    }
    [Fact]
    public void HandBuiltSuiteRejectsUnsupportedClassFixtures()
    {
        var cls = (PyClass)PyOps.MakeClass("FixtureCase", [PyUnittest.TestCase], PyNone.Instance);
        cls.SetAttr("setUpClass", PyNativeFunctions.Create("setUpClass", [], [], 0, _ => PyNone.Instance));
        object test = cls.Invoke([], [], [])!;
        var suite = new PyUnittestSuite(new PyTuple([test]));
        Assert.Throws<PyNotImplementedError>(() => Call(suite, "run", new PyUnittestResult()));
    }

    [Fact]
    public void RunDoesNotConvertSystemExitIntoOrdinaryError()
    {
        object test = Case();
        PyObject.SetAttr(test, "runTest", PyNativeFunctions.Create("runTest", [], [], 0, _ => throw new PySystemExit(7L)));
        var result = new PyUnittestResult();
        Assert.Throws<PySystemExit>(() => Call(test, "run", result));
        Assert.Empty(((PyList)PyObject.GetAttr(result, "errors")!).Items);
    }
}
