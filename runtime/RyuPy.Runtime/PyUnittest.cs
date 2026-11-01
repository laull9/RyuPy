using System;
using System.Collections.Generic;

namespace RyuPy.Runtime;

/// <summary>unittest 的有限原生模块；TestCase 保持真实 PyClass，用户方法按普通 MRO 绑定。</summary>
public static class PyUnittest
{
    private static readonly Lazy<Dictionary<string, object?>> Attributes = new(Build);
    /// <summary>稳定模块字典；TestCase 的身份供编译器原生基类入口验证。</summary>
    public static Dictionary<string, object?> CreateAttributes() => Attributes.Value;
    /// <summary>显式可继承的原生测试基类。</summary>
    public static PyClass TestCase => (PyClass)Attributes.Value["TestCase"]!;

    private static Dictionary<string, object?> Build()
    {
        var cls = new PyClass("TestCase") { IsNativeRuntimeClass = true };
        cls.SetAttr("failureException", PyType.Get("AssertionError"));
        cls.SetAttr("__init__", Method("__init__", ["self", "methodName"], [null, "runTest"], 1, a =>
        {
            if (a[1] is not string method) throw new PyTypeError("methodName must be a string");
            PyObject.SetAttr(a[0], "_testMethodName", method);
            PyObject.SetAttr(a[0], "_cleanups", new PyList([]));
            if (method != "runTest")
            {
                try { _ = PyObject.GetAttr(a[0], method); }
                catch (PyAttributeError) { throw new PyValueError($"no such test method: {method}"); }
            }
            return PyNone.Instance;
        }));
        foreach (string name in new[] { "setUp", "tearDown" })
            cls.SetAttr(name, Method(name, ["self"], [null], 1, _ => PyNone.Instance));
        cls.SetAttr("defaultTestResult", Method("defaultTestResult", ["self"], [null], 1, _ => new PyUnittestResult()));
        cls.SetAttr("run", Method("run", ["self", "result"], [null, PyNone.Instance], 1,
            a => PyUnittestExecution.Run(a[0]!, a[1])));
        cls.SetAttr("__call__", Method("__call__", ["self", "result"], [null, PyNone.Instance], 1,
            a => Call(a[0], "run", a[1])));
        cls.SetAttr("countTestCases", Method("countTestCases", ["self"], [null], 1, _ => 1L));
        cls.SetAttr("id", Method("id", ["self"], [null], 1,
            a => ((PyInstance)a[0]!).Class.Name + "." + PyObject.GetAttr(a[0], "_testMethodName")));
        cls.SetAttr("fail", Method("fail", ["self", "msg"], [null, PyNone.Instance], 1,
            a => throw Failure(a[0]!, a[1] is PyNone ? "None" : PyRepr.Str(a[1]))));
        PyUnittestAssertions.Install(cls);
        PyUnittestExecution.Install(cls);
        var result = new PyNativeType("TestResult", "unittest", (a, n, v) =>
        {
            if (a.Length != 0 || n.Length != 0) throw new PyTypeError("TestResult() supports no arguments");
            return new PyUnittestResult();
        }, value => value is PyUnittestResult);
        var loader = new PyNativeType("TestLoader", "unittest", (a, n, v) =>
        {
            if (a.Length != 0 || n.Length != 0) throw new PyTypeError("TestLoader() takes no arguments");
            return new PyUnittestLoader();
        }, value => value is PyUnittestLoader);
        var suiteFactory = PyNativeFunctions.Create("TestSuite", ["tests"], [new PyTuple([])], 0,
            a => new PyUnittestSuite(a[0]));
        var suite = new PyNativeType("TestSuite", "unittest", (a, n, v) => PyOps.CallValue(suiteFactory, a, n, v),
            value => value is PyUnittestSuite);
        return new(StringComparer.Ordinal)
        {
            ["TestCase"] = cls, ["TestResult"] = result, ["TestLoader"] = loader,
            ["TestSuite"] = suite, ["defaultTestLoader"] = new PyUnittestLoader(),
        };
    }

    internal static PyFunction Method(string name, string[] names, object?[] defaults, int required,
        Func<object?[], object?> body) => PyNativeFunctions.Create(name, names, defaults, required, body);
    internal static object? Call(object? target, string name, params object?[] args) =>
        PyOps.CallValue(PyObject.GetAttr(target, name), args, [], []);
    internal static PyBaseException Failure(object self, string message)
    {
        object? kind = PyObject.GetAttr(self, "failureException");
        if (kind is PyType builtin) return PyExceptionTypes.Create(builtin.Name, message);
        object? value = PyOps.CallValue(kind, [message], [], []);
        return value as PyBaseException ?? throw new PyTypeError("failureException must be an exception class");
    }
    internal static bool Matches(object? error, object? expected) =>
        PyOps.Truthy(PyOps.CallBuiltin("isinstance", [error, expected], [], []));
    internal static bool Callable(object? value) => PyOps.Truthy(PyOps.CallBuiltin("callable", [value], [], []));
}
