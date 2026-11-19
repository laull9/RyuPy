// partition/rpartition公开入口专项；CP参考先冻结，覆盖身份、原文本、代理边界与绑定错误现场。
using System;
using System.Collections.Generic;
using Xunit;

namespace RyuPy.Runtime.Tests;

[Collection("生成器帧栈")]
public sealed class PyTextPartitionTests
{
    private static readonly string[] Routes = ["direct", "saved", "prepared", "root"];
    private static readonly string[] Methods = ["partition", "rpartition"];
    private static PyFunction Function(Func<object?[], object?> body) => PyNativeFunctions.CreateVariadic("partition_test", body);
    private static PyClass TextClass()
    {
        var cls = (PyClass)PyOps.MakeClass("PartitionText", [PyType.Get("str")], null);
        PyOps.FinalizeOrdinaryClass(cls);
        return cls;
    }
    private static object? Route(string route, object text, string name, object?[] args, string[]? keys = null)
    {
        string[] names = keys ?? [];
        object?[] values = Array.ConvertAll(names, _ => (object?)1L);
        if (route == "direct") return PyOps.CallMethod(text, name, args, names, values);
        if (route == "root") return PyOps.CallValue(PyObject.GetAttr(PyType.Get("str"), name), [text, .. args], names, values);
        object? target = route == "saved" ? PyObject.GetAttr(text, name) : PyOps.PrepareMethod(text, name);
        return PyOps.CallValue(target, args, names, values);
    }
    private static PyTuple Split(object text, string name, object separator) =>
        Assert.IsType<PyTuple>(Route("direct", text, name, [separator]));
    private static void Parts(PyTuple result, string left, object separator, string right)
    {
        Assert.Equal(3, result.Length);
        Assert.Equal(left, Assert.IsType<string>(result.Items[0]));
        Assert.Same(separator, result.Items[1]);
        Assert.Equal(right, Assert.IsType<string>(result.Items[2]));
    }

    [Fact]
    public void EveryRouteKeepsFirstAndLastMultipleCharacterMatchesDistinct()
    {
        string separator = new string([':', ':']);
        foreach (string route in Routes)
        {
            Parts(Assert.IsType<PyTuple>(Route(route, "aa::bb::cc", "partition", [separator])), "aa", separator, "bb::cc");
            Parts(Assert.IsType<PyTuple>(Route(route, "aa::bb::cc", "rpartition", [separator])), "aa::bb", separator, "cc");
        }
        Parts(Split("ababa", "partition", "aba"), "", "aba", "ba");
        Parts(Split("ababa", "rpartition", "aba"), "ab", "aba", "");
        Parts(Split("::::", "partition", separator), "", separator, "::");
        Parts(Split("::::", "rpartition", separator), "::", separator, "");
    }

    [Fact]
    public void ExactAndSubtypeMissesKeepTheSourceObjectIncludingEmptySource()
    {
        PyClass cls = TextClass();
        foreach (string raw in new[] { new string("dynamic untouched payload".ToCharArray()), "" })
            foreach (object source in new[] { (object)raw, cls.Invoke([raw], [], [])! })
                foreach (string route in Routes)
                    foreach (string method in Methods)
                    {
                        var result = Assert.IsType<PyTuple>(Route(route, source, method, ["not present separator"]));
                        int retained = method == "partition" ? 0 : 2;
                        Assert.Same(source, result.Items[retained]);
                        Assert.Same(PyObject.TypeOf(source), PyObject.TypeOf(result.Items[retained]));
                        Assert.Equal("", Assert.IsType<string>(result.Items[1]));
                        Assert.Equal("", Assert.IsType<string>(result.Items[2 - retained]));
                    }
    }

    [Fact]
    public void HitsKeepTheOriginalSeparatorIncludingSubtypesAndReturnExactOuterStrings()
    {
        PyClass cls = TextClass(); int callbacks = 0;
        cls.SetAttr("__str__", Function(_ => { callbacks++; throw new PyValueError("must not convert"); }));
        cls.SetAttr("__repr__", Function(_ => { callbacks++; throw new PyValueError("must not represent"); }));
        string rawSeparator = new string("separator_dynamic_42".ToCharArray());
        foreach (object separator in new[] { (object)rawSeparator, cls.Invoke([rawSeparator], [], [])! })
            foreach (string rawSource in new[] { "left" + rawSeparator + "right", rawSeparator })
                foreach (object source in new[] { (object)rawSource, cls.Invoke([rawSource], [], [])! })
                    foreach (string route in Routes)
                        foreach (string method in Methods)
                        {
                            bool whole = rawSource == rawSeparator;
                            Parts(Assert.IsType<PyTuple>(Route(route, source, method, [separator])),
                                whole ? "" : "left", separator, whole ? "" : "right");
                        }
        Assert.Equal(0, callbacks);
    }

    [Fact]
    public void OrdinalMatchingKeepsNulCombiningMarksAndAstralScalars()
    {
        Parts(Split("a\0b\0c", "partition", "\0"), "a", "\0", "b\0c");
        Parts(Split("a\0b\0c", "rpartition", "\0"), "a\0b", "\0", "c");
        Parts(Split("a😀b😀c", "partition", "😀"), "a", "😀", "b😀c");
        Parts(Split("a😀b😀c", "rpartition", "😀"), "a😀b", "😀", "c");
        Parts(Split("e\u0301é", "partition", "é"), "e\u0301", "é", "");
        string decomposed = new string(['e', '\u0301']);
        Assert.Same(decomposed, Split(decomposed, "partition", "é").Items[0]);
        Parts(Split("x\U0010FFFFy", "rpartition", "\U0010FFFF"), "x", "\U0010FFFF", "y");
    }

    [Fact]
    public void MatchingNeverCutsANormalSurrogatePairAtEitherEnd()
    {
        foreach (string route in Routes)
            foreach (string method in Methods)
                foreach (string separator in new[] { "\uD83D", "\uDE00", "a\uD83D", "\uDE00b" })
                {
                    string source = "a😀b";
                    var result = Assert.IsType<PyTuple>(Route(route, source, method, [separator]));
                    Assert.Same(source, result.Items[method == "partition" ? 0 : 2]);
                    Assert.Equal("", result.Items[1]);
                }
    }

    [Fact]
    public void InvalidCandidatesDoNotHideLaterValidIsolatedSurrogates()
    {
        foreach (string method in Methods)
        {
            Parts(Split("😀x\uD83Dz", method, "\uD83D"), "😀x", "\uD83D", "z");
            Parts(Split("\uDE00x😀", method, "\uDE00"), "", "\uDE00", "x😀");
            Parts(Split("a\uD83Db", method, "\uD83D"), "a", "\uD83D", "b");
            Parts(Split("a\uDE00b", method, "\uDE00"), "a", "\uDE00", "b");
        }
        // 非D009反例：正常scalar旁边只有同向孤代理，合法命中与首个非法命中重叠。
        Parts(Split("😀\uDE00\uDE00", "partition", "\uDE00\uDE00"), "😀", "\uDE00\uDE00", "");
        Parts(Split("\uD83D\uD83D😀", "rpartition", "\uD83D\uD83D"), "", "\uD83D\uD83D", "😀");
    }

    [Fact]
    public void EveryRouteChecksKeywordsBeforeArityAndSeparatorContent()
    {
        PyClass cls = TextClass();
        foreach (object source in new[] { (object)"abc", cls.Invoke(["abc"], [], [])! })
            foreach (string route in Routes)
                foreach (string method in Methods)
                {
                    foreach (object?[] args in new object?[][] { [], [""], [42L], ["", 42L] })
                        Assert.Equal($"str.{method}() takes no keyword arguments",
                            Assert.Throws<PyTypeError>(() => Route(route, source, method, args, ["sep"])).PyMessage);
                    foreach (object?[] args in new object?[][] { [], ["", 42L] })
                        Assert.Equal($"str.{method}() takes exactly one argument ({args.Length} given)",
                            Assert.Throws<PyTypeError>(() => Route(route, source, method, args)).PyMessage);
                    Assert.Equal("empty separator", Assert.Throws<PyValueError>(() => Route(route, source, method, [""])).PyMessage);
                    Assert.Equal("empty separator", Assert.Throws<PyValueError>(() => Route(route, source, method, [cls.Invoke([""], [], [])])).PyMessage);
                }
    }

    [Fact]
    public void InvalidSeparatorsNeverInvokeStringConversion()
    {
        var cls = new PyClass("Convert"); int calls = 0;
        cls.SetAttr("__str__", Function(_ => { calls++; return ":"; }));
        object[] bad = [PyNone.Instance, 1L, true, new PyBytes([58]), new PyByteArray([58]), new PyList([":"]), new PyTuple([":"]), new PyInstance(cls)];
        foreach (string route in Routes)
            foreach (string method in Methods)
                foreach (object separator in bad)
                    Assert.Equal($"must be str, not {PyRepr.TypeName(separator)}",
                        Assert.Throws<PyTypeError>(() => Route(route, "a:b", method, [separator])).PyMessage);
        Assert.Equal(0, calls);
    }

    [Fact]
    public void RootReceiverErrorsPrecedeAllOtherBindingChecks()
    {
        foreach (string method in Methods)
        {
            object? root = PyObject.GetAttr(PyType.Get("str"), method);
            Assert.Equal($"unbound method str.{method}() needs an argument",
                Assert.Throws<PyTypeError>(() => PyOps.CallValue(root, [], ["self", "sep"], ["a", ":"])).PyMessage);
            Assert.Equal($"descriptor '{method}' for 'str' objects doesn't apply to a 'int' object",
                Assert.Throws<PyTypeError>(() => PyOps.CallValue(root, [42L, "", 1L], ["sep"], [":"])).PyMessage);
        }
    }

    [Fact]
    public void SavedAndCapturedMethodsKeepTheirTargetWhileRootBypassesOverrides()
    {
        foreach (string method in Methods)
        {
            PyClass cls = TextClass(); object source = cls.Invoke(["a:b"], [], [])!;
            object? saved = PyObject.GetAttr(source, method);
            object? captured = PyOps.CaptureModuleTarget(source, method);
            cls.SetAttr(method, Function(_ => "override"));
            Parts(Assert.IsType<PyTuple>(PyOps.CallValue(saved, [":"], [], [])), "a", ":", "b");
            Parts(Assert.IsType<PyTuple>(PyOps.CallCapturedMethod(true, captured, method, [":"], [], [])), "a", ":", "b");
            Assert.Equal("override", Route("direct", source, method, [":"]));
            Parts(Assert.IsType<PyTuple>(Route("root", source, method, [":"])), "a", ":", "b");
            PyObject.SetAttr(source, method, Function(_ => "instance"));
            Assert.Equal("instance", Route("direct", source, method, [":"]));
        }
    }

    [Fact]
    public void PropertyCaptureStillPrecedesArgumentsAndPreservesUserExceptions()
    {
        foreach (string method in Methods)
        {
            PyClass cls = TextClass(); object source = cls.Invoke(["a:b"], [], [])!;
            var events = new List<string>();
            object? target = Function(_ => { events.Add("old-call"); return "old"; });
            cls.SetAttr(method, new PyProperty(Function(_ => { events.Add("get"); return target; })));
            object? captured = PyOps.CaptureModuleTarget(source, method);
            events.Add("argument"); target = Function(_ => "new");
            Assert.Equal("old", PyOps.CallCapturedMethod(true, captured, method, [":"], [], []));
            Assert.Equal(new[] { "get", "argument", "old-call" }, events);
            var error = new PyValueError("getter-body");
            cls.SetAttr(method, new PyProperty(Function(_ => throw error)));
            Assert.Same(error, Assert.Throws<PyValueError>(() => PyOps.CaptureModuleTarget(source, method)));
        }
    }

    [Fact]
    public void OwnedFailuresKeepTheCallerFrameAndActiveContext()
    {
        long depth = PyTraceback.PushFrame("partition_test.py", "caller");
        var outer = new PyRuntimeError("outer"); object? previous = PyErrors.EnterHandler(outer);
        try
        {
            PyTraceback.SetLine(57);
            foreach (string route in Routes)
                foreach (string method in Methods)
                {
                    PyBaseException[] errors =
                    [
                        Assert.Throws<PyTypeError>(() => Route(route, "a:b", method, [], ["sep"])),
                        Assert.Throws<PyTypeError>(() => Route(route, "a:b", method, [])),
                        Assert.Throws<PyTypeError>(() => Route(route, "a:b", method, [42L])),
                        Assert.Throws<PyValueError>(() => Route(route, "a:b", method, [""])),
                    ];
                    foreach (PyBaseException error in errors)
                    {
                        Assert.Same(outer, error.Context);
                        Assert.Equal(new PyStackFrame("partition_test.py", "caller", 57), error.Frames[0]);
                    }
                }
        }
        finally { PyErrors.LeaveHandler(previous); PyTraceback.PopTo(depth); }
    }
}
