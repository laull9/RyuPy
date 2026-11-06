// bytes正则C公开闭合专项：跨域缓存、真实消费者、组号冷协议及re.error原对象/两次边界转换。
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using Xunit;

namespace RyuPy.Runtime.Tests;

[Collection("生成器帧栈")]
public sealed class PyRegexBytesPublicTests
{
    private static PyBytes B(string text) => new(Encoding.Latin1.GetBytes(text));
    private static object? Call(PyNativeObject owner, string name, params object?[] args) => PyOps.CallValue(owner.GetAttr(name), args, [], []);
    private static object? Module(string name, params object?[] args) => PyOps.CallValue(PyRegex.CreateAttributes()[name], args, [], []);
    private static PyRegexMatch Match(PyRegexPattern pattern, PyBytes source) => Assert.IsType<PyRegexMatch>(Call(pattern, "search", source));
    private static PyInstance Obj(string name, params (string Name, Func<object?[], object?> Body)[] methods)
    {
        var cls = new PyClass(name);
        foreach (var method in methods) cls.SetAttr(method.Name, PyNativeFunctions.CreateVariadic(method.Name, method.Body));
        return new PyInstance(cls);
    }
    private static PyRegexError Error(object? message, object? pattern, object? position) => PyRegex.ConstructError([message, pattern, position], [], []);

    [Fact]
    public void CacheSeparatesDomainsPreservesFirstPatternAndPurgeDoesNotInvalidateSavedObjects()
    {
        Module("purge"); PyBytes source = B("cache-a+"); var pattern = PyRegex.Compile(source);
        Assert.Same(pattern, PyRegex.Compile(B("cache-a+"))); Assert.Same(source, pattern.GetAttr("pattern"));
        Assert.Same(pattern, PyRegex.Compile(pattern)); Assert.NotSame(pattern, PyRegex.Compile("cache-a+"));
        Assert.Equal(0L, pattern.GetAttr("flags")); Assert.Equal(32L, PyRegex.Compile("cache-a+").GetAttr("flags"));
        Assert.Equal("cannot process flags argument with a compiled pattern",
            Assert.Throws<PyValueError>(() => PyRegex.Compile(pattern, 2L)).PyMessage);
        Module("purge"); Assert.NotSame(pattern, PyRegex.Compile(source));
        Assert.Equal("b'cache-aaa'", PyRepr.Repr(Call(Match(pattern, B("cache-aaa")), "group")));
        // 本仓64项FIFO仍统一约束两个域；不宣称CP的512/256缓存容量。
        var first = PyRegex.Compile(B("fifo-first"));
        for (int index = 0; index < 64; index++) PyRegex.Compile(index % 2 == 0 ? B("fifo" + index) : "fifo" + index);
        Assert.NotSame(first, PyRegex.Compile(B("fifo-first")));
    }

    [Fact]
    public void AllPublicMatchingConsumersUseTheActualBytesDomain()
    {
        PyBytes expression = B("(?P<x>a+)"); PyBytes source = B("aa"); var pattern = PyRegex.Compile(expression);
        foreach (string method in new[] { "match", "search", "fullmatch" })
        {
            var direct = Assert.IsType<PyRegexMatch>(Call(pattern, method, source));
            var module = Assert.IsType<PyRegexMatch>(Module(method, expression, source));
            Assert.Same(source, Call(direct, "group")); Assert.Same(source, module.GetAttr("string"));
            Assert.Same(pattern, module.GetAttr("re")); Assert.Equal("(0, 2)", PyRepr.Repr(Call(module, "span")));
        }
        Assert.Equal("[b'aa']", PyRepr.Repr(Module("findall", expression, source)));
        Assert.Equal("[b'aa']", PyRepr.Repr(Call(pattern, "findall", source)));
        foreach (object? result in new[] { Module("finditer", expression, source), Call(pattern, "finditer", source) })
        {
            var iterator = Assert.IsAssignableFrom<IPyIterator>(result); Assert.True(iterator.MoveNext());
            Assert.Same(source, Call(Assert.IsType<PyRegexMatch>(iterator.Current()), "group")); Assert.False(iterator.MoveNext());
        }
        Assert.Equal("(b'aa', b'aa')", PyRepr.Repr(Call(Match(pattern, source), "group", 0L, "x")));
    }

    [Fact]
    public void PublicTemplatesEscapeAndTextControlsStayConnected()
    {
        var pattern = PyRegex.Compile(B("(a)")); PyBytes source = B("a a");
        Assert.Equal("b'<a> <a>'", PyRepr.Repr(Module("sub", B("(a)"), B(@"<\1>"), source)));
        Assert.Equal("(b'X X', 2)", PyRepr.Repr(Module("subn", B("a"), B("X"), source)));
        Assert.Equal("(b'X a', 1)", PyRepr.Repr(Call(pattern, "subn", B("X"), source, 1L)));
        Assert.Equal("b'<a>'", PyRepr.Repr(Call(Match(pattern, source), "expand", B(@"<\1>"))));
        Assert.Equal("X", Call(Match(pattern, source), "expand", "X"));
        Assert.Equal(B(@"\[\]\#\ ").Bytes, Assert.IsType<PyBytes>(Module("escape", B("[]# "))).Bytes);
        Assert.Equal(@"\[\]\#\ ", Module("escape", "[]# "));
        Assert.Equal("<猫>", Module("sub", "(猫)", @"<\1>", "猫"));
        Assert.IsType<PyNotImplementedError>(Assert.Throws<PyNotImplementedError>(() => PyRegex.Compile(B("(?P<x>a)")).GetAttr("groupindex")));
    }

    [Fact]
    public void MutableSubjectsOpenMatchingConsumersAndKeepOtherDomainsExplicit()
    {
        var mutable = new PyByteArray([97]); var pattern = PyRegex.Compile(B("a"));
        Assert.Equal("unhashable type: 'bytearray'", Assert.Throws<PyTypeError>(() => PyRegex.Compile(mutable)).PyMessage);
        Assert.IsType<PyRegexMatch>(Call(pattern, "search", mutable));
        var iterator = Assert.IsAssignableFrom<IPyIterator>(Call(pattern, "finditer", mutable));
        Assert.True(iterator.MoveNext()); Assert.Equal("b'a'", PyRepr.Repr(Call(Assert.IsType<PyRegexMatch>(iterator.Current()), "group")));
        Assert.False(iterator.MoveNext());
        Assert.Equal("b'X'", PyRepr.Repr(Call(pattern, "sub", B("X"), mutable)));
        Assert.Equal("(b'X', 1)", PyRepr.Repr(Call(pattern, "subn", B("X"), mutable)));
        Assert.Equal("b'X'", PyRepr.Repr(Module("sub", B("a"), B("X"), mutable)));
        Assert.Equal("(b'X', 1)", PyRepr.Repr(Module("subn", B("a"), B("X"), mutable)));
        Assert.Equal("b'a'", PyRepr.Repr(Call(pattern, "sub", mutable, B("a"))));
        Assert.Equal("b'a'", PyRepr.Repr(Module("escape", mutable)));
        Assert.Equal("cannot use a bytes pattern on a string-like object", Assert.Throws<PyTypeError>(() => Call(pattern, "search", "a")).PyMessage);
        Assert.Equal("cannot use a string pattern on a bytes-like object", Assert.Throws<PyTypeError>(() => Call(PyRegex.Compile("a"), "search", B("a"))).PyMessage);
        Assert.Throws<PyNotImplementedError>(() => Call(Match(pattern, B("a")), "expand", @"\g<é>"));
    }

    [Fact]
    public void GroupNumbersUseRangeErrorsRatherThanPosCountOverflow()
    {
        var match = Match(PyRegex.Compile(B("(a)")), B("a"));
        foreach (string method in new[] { "group", "start", "end", "span" })
        foreach (object number in new object[] { 1L << 40, BigInteger.One << 100, -(BigInteger.One << 100), -1L })
            Assert.Equal("no such group", Assert.Throws<PyIndexError>(() => Call(match, method, number)).PyMessage);
        int calls = 0; var index = Obj("Index", ("__index__", _ => { calls++; return 1L; }));
        foreach (string method in new[] { "group", "start", "end", "span" }) Call(match, method, index);
        Assert.Equal(4, calls);
        Assert.Equal("b'a'", PyRepr.Repr(Call(match, "group", true)));
        Assert.Equal("__index__ returned non-int (type str)", Assert.Throws<PyTypeError>(() => Call(match, "group", Obj("Bad", ("__index__", _ => "bad")))).PyMessage);
        var marker = new PyRuntimeError("group-index-marker");
        Assert.Same(marker, Assert.Throws<PyRuntimeError>(() => Call(match, "group", Obj("Raised", ("__index__", _ => throw marker)))));
    }

    [Fact]
    public void NameLookupUsesPythonHashAndEqualityOnlyWhenThePatternHasNames()
    {
        var named = Match(PyRegex.Compile(B("(?P<x>a)")), B("a")); var plain = Match(PyRegex.Compile(B("(a)")), B("a"));
        var events = new List<string>();
        var key = Obj("Name", ("__hash__", _ => { events.Add("hash"); return PyHash.Of("x"); }),
            ("__eq__", args => { events.Add("eq"); return args[1] is string text && text == "x"; }));
        Assert.Equal("b'a'", PyRepr.Repr(Call(named, "group", key))); Assert.Equal<string>(["hash", "eq"], events);
        events.Clear(); Assert.Throws<PyIndexError>(() => Call(plain, "group", key)); Assert.Empty(events);
        Assert.Throws<PyIndexError>(() => Call(plain, "group", new PyList([])));
        Assert.Equal("unhashable type: 'list'", Assert.Throws<PyTypeError>(() => Call(named, "group", new PyList([]))).PyMessage);
        var marker = new PyRuntimeError("hash-marker");
        Assert.Same(marker, Assert.Throws<PyRuntimeError>(() => Call(named, "group", Obj("BadHash", ("__hash__", _ => throw marker)))));
        var eqMarker = new PyRuntimeError("eq-marker");
        Assert.Same(eqMarker, Assert.Throws<PyRuntimeError>(() => Call(named, "group", Obj("BadEq",
            ("__hash__", _ => PyHash.Of("x")), ("__eq__", _ => throw eqMarker)))));
    }

    [Fact]
    public void GroupIndexConsumesTypeSlotsAndNeverFallsBackAfterAnIndexError()
    {
        var match = Match(PyRegex.Compile(B("(?P<x>a)")), B("a")); int calls = 0;
        var value = Obj("Plain"); PyObject.SetAttr(value, "__index__", PyNativeFunctions.CreateVariadic("ignored", _ => { calls++; return 0L; }));
        Assert.Throws<PyIndexError>(() => Call(match, "group", value)); Assert.Equal(0, calls);
        var descriptor = Obj("Descriptor", ("__get__", _ => { calls++; return 0L; }), ("__call__", _ => { calls++; return 0L; }));
        var cls = new PyClass("IndexDescriptor"); cls.SetAttr("__index__", descriptor);
        Assert.Throws<PyNotImplementedError>(() => Call(match, "group", new PyInstance(cls))); Assert.Equal(0, calls);
        var marker = new PyRuntimeError("index-not-name");
        var failing = Obj("Both", ("__index__", _ => throw marker), ("__hash__", _ => { calls++; return PyHash.Of("x"); }));
        Assert.Same(marker, Assert.Throws<PyRuntimeError>(() => Call(match, "group", failing))); Assert.Equal(0, calls);
    }

    [Fact]
    public void PublicErrorPreservesNegativeAndArbitrarilyWidePositionMetadata()
    {
        PyBytes pattern = B("a\nb\nc"); var negative = Error("bad", pattern, -1L);
        Assert.Same(pattern, negative.Pattern); Assert.Equal(-1L, negative.Position);
        Assert.Equal(3L, negative.LineNumber); Assert.Equal(-4L, negative.ColumnNumber);
        Assert.Equal("bad at position -1 (line 3, column -4)", negative.PyMessage);
        BigInteger large = BigInteger.One << 100; var wide = Error("wide", pattern, large);
        Assert.Equal(large, wide.Position); Assert.Equal(3L, wide.LineNumber); Assert.Equal(large - 3, wide.ColumnNumber);
        Assert.Equal("wide at position " + large + " (line 3, column " + (large - 3) + ")", wide.PyMessage);
        var veryNegative = Error("low", pattern, -large);
        Assert.Equal(1L, veryNegative.LineNumber); Assert.Equal(-large + 1, veryNegative.ColumnNumber);
    }

    [Fact]
    public void PublicErrorFormatsBeforeTwoIndependentIndexCallsAndOriginalSubtraction()
    {
        var events = new List<string>(); int indexes = 0;
        var message = Obj("Message", ("__str__", _ => { events.Add("str"); return "msg"; }));
        var position = Obj("Position", ("__int__", _ => { events.Add("int"); return 5L; }),
            ("__index__", _ => { events.Add("index"); return ++indexes == 1 ? 2L : 4L; }),
            ("__sub__", args => { events.Add("sub:" + PyRepr.Str(args[1])); return 7L; }));
        var error = Error(message, B("a\nb\nc"), position);
        Assert.Same(message, error.MessageText); Assert.Same(position, error.Position);
        Assert.Equal<string>(["str", "int", "index", "index", "sub:3"], events);
        Assert.Equal(2L, error.LineNumber); Assert.Equal(7L, error.ColumnNumber);
        Assert.Equal("msg at position 5 (line 2, column 7)", error.PyMessage);
    }

    [Fact]
    public void PublicErrorKeepsUserFailuresAndNoPositionKeepsMessageLazy()
    {
        int calls = 0; var marker = new PyRuntimeError("second-index");
        var position = Obj("Position", ("__int__", _ => 1L), ("__index__", _ => { if (++calls == 2) throw marker; return 1L; }));
        Assert.Same(marker, Assert.Throws<PyRuntimeError>(() => Error("bad", B("a\nb"), position))); Assert.Equal(2, calls);
        int strings = 0; var message = Obj("Message", ("__str__", _ => { strings++; return "later"; }));
        PyBytes pattern = B("raw"); var lazy = Error(message, pattern, PyNone.Instance);
        Assert.Equal(0, strings); Assert.Same(pattern, lazy.Pattern); Assert.Same(message, lazy.MessageText);
        Assert.IsType<PyNone>(lazy.LineNumber); Assert.Equal("later", lazy.PyMessage); Assert.Equal(1, strings);
        Assert.Equal("slice indices must be integers or None or have an __index__ method",
            Assert.Throws<PyTypeError>(() => Error("bad", B("a"), 1.2)).PyMessage);
    }

    [Fact]
    public void LocalBindingAndOwnErrorsCaptureCallerWhileUserErrorsStayOriginal()
    {
        PyTraceback.Reset(); var ambient = new PyValueError("ambient"); object? token = PyErrors.EnterHandler(ambient);
        try
        {
            PyTraceback.PushFrame("bytes_regex_c.py", "caller"); PyTraceback.SetLine(29);
            var pattern = PyRegex.Compile(B("(a)")); var match = Match(pattern, B("a"));
            PyBaseException[] errors = [Assert.Throws<PyTypeError>(() => Module("search")),
                Assert.Throws<PyTypeError>(() => Call(pattern, "sub", B("x"))),
                Assert.Throws<PyTypeError>(() => PyOps.CallValue(match.GetAttr("group"), [], ["bad"], [1L])),
                Assert.Throws<PyIndexError>(() => Call(match, "group", BigInteger.One << 100)),
                Assert.Throws<PyTypeError>(() => Error("bad", B("a"), 1.2))];
            foreach (PyBaseException error in errors)
            {
                Assert.Same(ambient, error.Context); Assert.Equal("caller", Assert.Single(error.Frames).Function); Assert.Equal(29, error.Frames[0].Line);
            }
            var marker = new PyTypeError("user-type-marker");
            var callback = PyNativeFunctions.CreateVariadic("callback", _ => throw marker);
            Assert.Same(marker, Assert.Throws<PyTypeError>(() => Call(pattern, "sub", callback, B("a"))));
            Assert.Same(ambient, PyErrors.Current); Assert.Equal(1L, PyTraceback.CaptureDepth());
        }
        finally { PyErrors.LeaveHandler(token); PyTraceback.Reset(); }
    }

    [Fact]
    public void ModuleCompileErrorsPrecedeCountConversionAndSubjectConsumption()
    {
        int calls = 0; var count = Obj("Count", ("__index__", _ => { calls++; return 0L; }));
        Assert.Throws<PyRegexError>(() => Module("sub", B("["), B("x"), 3L, count)); Assert.Equal(0, calls);
        Assert.Throws<PyValueError>(() => Module("sub", B("a"), B("x"), 3L, count, 32L)); Assert.Equal(0, calls);
        Assert.Throws<PyRegexError>(() => Module("sub", B("a"), B(@"\9"), 3L, count)); Assert.Equal(1, calls);
    }
}
