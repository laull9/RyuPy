using System;

namespace RyuPy.Runtime;

/// <summary>TestCase 基础断言及 assertRaises 双形态，不在匹配失败时吞掉原异常。</summary>
internal static class PyUnittestAssertions
{
    internal static void Install(PyClass cls)
    {
        Add(cls, "assertEqual", (a, b) => PyHash.Equal(a, b), "!=");
        Add(cls, "assertNotEqual", (a, b) => !PyHash.Equal(a, b), "==");
        Add(cls, "assertIs", ReferenceEquals, "is not");
        Add(cls, "assertIsNot", (a, b) => !ReferenceEquals(a, b), "is");
        Unary(cls, "assertTrue", PyOps.Truthy, "is not true");
        Unary(cls, "assertFalse", a => !PyOps.Truthy(a), "is not false");
        Unary(cls, "assertIsNone", a => a is null or PyNone, "is not None");
        Unary(cls, "assertIsNotNone", a => a is not null and not PyNone, "is None");
        var signature = PySignature.Create("assertRaises", ["self", "expected_exception", "args", "kwargs"],
            ["pos", "pos", "varpos", "varkw"], ["0", "0", "0", "0"]);
        cls.SetAttr("assertRaises", new PyFunction("assertRaises", signature, (self, args, names, values) =>
        {
            object?[] bound = PyBinder.Bind(signature, args, names, values, self.Defaults);
            object? expected = bound[1]; Validate(expected);
            object?[] positional = ((PyTuple)bound[2]!).Items;
            var keywords = (PyDict)bound[3]!;
            object? message = PyNone.Instance;
            if (positional.Length == 0)
            {
                foreach (var (key, value) in keywords.Entries())
                {
                    if (key is not "msg") throw new PyTypeError("invalid keyword argument for assertRaises()");
                    message = value;
                }
                return new PyUnittestRaises(bound[0]!, expected, message);
            }
            var context = new PyUnittestRaises(bound[0]!, expected, message);
            var callArgs = new object?[positional.Length - 1]; Array.Copy(positional, 1, callArgs, 0, callArgs.Length);
            var callNames = new string[keywords.Count]; var callValues = new object?[keywords.Count];
            int index = 0;
            foreach (var (key, value) in keywords.Entries()) { callNames[index] = (string)key!; callValues[index++] = value; }
            long depth = PyTraceback.CaptureDepth();
            try { PyOps.CallValue(positional[0], callArgs, callNames, callValues); }
            catch (PyBaseException error)
            {
                PyErrors.PrepareRaise(error); PyTraceback.PopTo(depth + 1);
                object? previous = PyErrors.EnterHandler(error);
                try { if (!context.Exit(error)) throw; }
                finally { PyErrors.LeaveHandler(previous); }
                return PyNone.Instance;
            }
            context.Exit(null);
            return PyNone.Instance;
        }, [], false));
    }
    private static void Validate(object? expected)
    {
        if (expected is PyTuple tuple) { foreach (object? item in tuple.Items) Validate(item); return; }
        if (expected is not (PyType or PyClass)
            || !PyOps.Truthy(PyOps.CallBuiltin("issubclass", [expected, PyType.Get("BaseException")], [], [])))
            throw new PyTypeError("assertRaises() arg 1 must be an exception type or tuple of exception types");
    }
    private static void Add(PyClass cls, string name, Func<object?, object?, bool> compare, string relation) =>
        cls.SetAttr(name, PyUnittest.Method(name, ["self", "first", "second", "msg"], [null, null, null, PyNone.Instance], 3, a =>
        {
            if (!compare(a[1], a[2])) throw PyUnittest.Failure(a[0]!, Message(a[3], $"{PyRepr.Repr(a[1])} {relation} {PyRepr.Repr(a[2])}"));
            return PyNone.Instance;
        }));
    private static void Unary(PyClass cls, string name, Func<object?, bool> predicate, string text) =>
        cls.SetAttr(name, PyUnittest.Method(name, ["self", "expr", "msg"], [null, null, PyNone.Instance], 2, a =>
        {
            if (!predicate(a[1])) throw PyUnittest.Failure(a[0]!, Message(a[2], $"{PyRepr.Repr(a[1])} {text}"));
            return PyNone.Instance;
        }));
    internal static string Message(object? message, string standard) => message is null or PyNone ? standard : standard + " : " + PyRepr.Str(message);
}

/// <summary>assertRaises 上下文仅保存匹配异常；with 的异常状态由既有语言清理协议负责。</summary>
internal sealed class PyUnittestRaises(object test, object? expected, object? message) : PyNativeObject
{
    private PyBaseException? _exception;
    public override string TypeName => "_AssertRaisesContext";
    public override object? GetAttr(string name) => name switch
    {
        "__enter__" => PyNativeFunctions.Create(name, [], [], 0, _ => this),
        "__exit__" => PyNativeFunctions.Create(name, ["type", "value", "traceback"], [null, null, null], 3,
            a => Exit(a[1] as PyBaseException)),
        "exception" when _exception is not null => _exception,
        _ => throw Missing(name),
    };
    internal bool Exit(PyBaseException? error)
    {
        if (error is null) throw PyUnittest.Failure(test,
            PyUnittestAssertions.Message(message, "expected exception not raised"));
        if (!PyUnittest.Matches(error, expected)) return false;
        _exception = error;
        return true;
    }
}
