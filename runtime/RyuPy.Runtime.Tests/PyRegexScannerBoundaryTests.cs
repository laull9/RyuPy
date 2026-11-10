// Scanner边界单独记账：入口binder/Compile优先于NI，资源门不预读后续项，不把CP正例改为非法。
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Reflection;
using System.Text.RegularExpressions;
using RyuPy.Runtime;
using Xunit;

namespace RyuPy.Runtime.Tests;

public sealed partial class PyRegexScannerTests
{
    [Fact]
    public void CompoundCompileIdentityAndPurgeKeepExistingPlanAlive()
    {
        var module = Module(); var scanner = New(Root(module), Lex(Pair("a", Echo())));
        object? plan = PyObject.GetAttr(scanner, "scanner");
        Assert.Same(plan, PyRegex.Compile(plan)); Assert.Same(plan, Call(module["compile"], plan));
        Assert.Throws<PyValueError>(() => PyRegex.Compile(plan, 2L));
        Call(module["purge"]);
        Assert.Same(plan, PyRegex.Compile(plan));
        Assert.Equal("(['a', 'a'], '!')", PyRepr.Repr(Scan(scanner, "aa!")));
    }

    [Fact]
    public void ModuleConsumersRejectAfterCompileBeforeInputCountOrTemplate()
    {
        var module = Module(); var scanner = New(Root(module), Lex(Pair("a", Echo())));
        object? plan = PyObject.GetAttr(scanner, "scanner"); int indexes = 0;
        PyClass indexClass = Class("Index");
        indexClass.SetAttr("__index__", Fn("index", ["self"], _ => { indexes++; return 0L; }));
        var index = new PyInstance(indexClass);
        foreach (string name in new[] { "match", "search", "fullmatch", "findall", "finditer" })
        {
            Assert.Throws<PyTypeError>(() => Call(module[name], plan));
            Assert.Throws<PyNotImplementedError>(() => Call(module[name], plan, 42L));
            Assert.Throws<PyValueError>(() => Call(module[name], plan, 42L, 2L));
            Assert.Throws<PyNotImplementedError>(() => Call(module[name], plan, Bytes("a"), index));
        }
        Assert.Equal(5, indexes); indexes = 0;
        foreach (string name in new[] { "sub", "subn" })
        {
            Assert.Throws<PyTypeError>(() => Call(module[name], plan));
            Assert.Throws<PyNotImplementedError>(() => Call(module[name], plan, @"\bad", 42L, index));
            Assert.Throws<PyValueError>(() => Call(module[name], plan, @"\bad", 42L, index, 2L));
        }
        Assert.Throws<PyNotImplementedError>(() => Call(module["split"], plan, 42L, index));
        Assert.Throws<PyValueError>(() => Call(module["split"], plan, 42L, index, 2L));
        Assert.Equal(0, indexes);
    }

    [Fact]
    public void SavedPatternMethodsBindBeforeRejectingWithoutConsumingArguments()
    {
        var scanner = New(Root(Module()), Lex(Pair("a", Echo()))); object? plan = PyObject.GetAttr(scanner, "scanner");
        foreach (string name in new[] { "match", "search", "fullmatch", "findall", "finditer", "scanner", "split" })
        {
            object? method = PyObject.GetAttr(plan, name);
            Assert.Throws<PyTypeError>(() => Call(method));
            Assert.Throws<PyTypeError>(() => PyOps.CallValue(method, ["a"], ["string"], ["b"]));
            Assert.Throws<PyNotImplementedError>(() => Call(method, PyNone.Instance));
        }
        foreach (string name in new[] { "sub", "subn" })
        {
            object? method = PyObject.GetAttr(plan, name);
            Assert.Throws<PyTypeError>(() => Call(method, "x"));
            Assert.Throws<PyNotImplementedError>(() => Call(method, @"\bad", PyNone.Instance, "invalid count"));
        }
        Assert.Throws<PyNotImplementedError>(() => PyObject.GetAttr(plan, "groupindex"));
        Assert.Throws<PyNotImplementedError>(() => PyRepr.Repr(plan));
        Assert.Throws<PyNotImplementedError>(() => PyRepr.Str(plan));
        Assert.Throws<PyAttributeError>(() => PyObject.GetAttr(plan, "new_future_method"));
    }

    [Fact]
    public void ScannerMatchExpandBindsThenRejectsInBothInputDomains()
    {
        PyClass root = Root(Module()); var scanner = New(root, Lex(Pair("a", Echo())));
        foreach (object source in new object[] { "a", Bytes("a") })
        {
            Scan(scanner, source); object? match = PyObject.GetAttr(scanner, "match");
            object? expand = PyObject.GetAttr(match, "expand");
            Assert.Throws<PyTypeError>(() => Call(expand));
            Assert.Throws<PyNotImplementedError>(() => Call(expand, 42L));
            Assert.Throws<PyNotImplementedError>(() => Call(expand, @"\bad"));
            Assert.Throws<PyNotImplementedError>(() => Call(expand, Bytes(@"\bad")));
        }
    }

    [Fact]
    public void ReplacementBackendGetterIsReadButMethodIsNotCalled()
    {
        PyClass root = Root(Module()), backendClass = Class("Backend"); int reads = 0, calls = 0;
        var scanner = New(root, Lex(Pair("a", Echo())));
        backendClass.SetAttr("scanner", new PyProperty(Fn("get", ["self"], _ =>
        { reads++; return Fn("method", ["source"], _ => { calls++; return PyNone.Instance; }); })));
        PyObject.SetAttr(scanner, "scanner", new PyInstance(backendClass));
        Assert.Throws<PyNotImplementedError>(() => Scan(scanner, 42L)); Assert.Equal(1, reads); Assert.Equal(0, calls);
        var error = new PyValueError("getter");
        backendClass.SetAttr("scanner", new PyProperty(Fn("get", ["self"], _ => throw error)));
        Assert.Same(error, Assert.Throws<PyValueError>(() => Scan(scanner, "a")));
        PyObject.SetAttr(scanner, "scanner", PyRegex.Compile("a"));
        Assert.Throws<PyNotImplementedError>(() => Scan(scanner, "a"));
    }

    [Fact]
    public void UnknownActionDescriptorRejectsAfterWritingMatchWithoutCallingGetter()
    {
        PyClass root = Root(Module()), actionClass = Class("Action"); int getters = 0;
        actionClass.SetAttr("__call__", new PyProperty(Fn("get", ["self"], _ => { getters++; return Echo(); })));
        var action = new PyInstance(actionClass);
        foreach (object wrapped in new object[] { action, new PyStaticMethod(action), new PyStaticMethod(new PyStaticMethod(action)) })
        {
            var scanner = New(root, Lex(Pair("a", wrapped)));
            Assert.Throws<PyNotImplementedError>(() => Scan(scanner, "a")); Assert.Equal(0, getters);
            Assert.Equal("a", Method(PyObject.GetAttr(scanner, "match"), "group"));
        }
        int calls = 0;
        var rawClassMethod = new PyClassMethod(Fn("must-not-call", ["scanner", "token"], _ => { calls++; return PyNone.Instance; }));
        foreach (object wrapped in new object[] { new PyStaticMethod(rawClassMethod), new PyStaticMethod(new PyStaticMethod(rawClassMethod)) })
        {
            var invalid = New(root, Lex(Pair("a", wrapped)));
            Assert.Equal("'classmethod' object is not callable", Assert.Throws<PyTypeError>(() => Scan(invalid, "a")).PyMessage);
            Assert.Equal("a", Method(PyObject.GetAttr(invalid, "match"), "group")); Assert.Equal(0, calls);
        }
    }

    [Fact]
    public void PatternAndSubjectDomainsRejectOnlyAtConsumption()
    {
        PyClass root = Root(Module());
        foreach (object? source in new object?[] { PyNone.Instance, 42L, 1.5, Lex(), new PyDict(), PyRegex.Compile("a") })
            Assert.IsType<PyTypeError>(Assert.Throws<PyTypeError>(() => New(root, Lex(Pair(source, "A")))));
        Assert.Throws<PyNotImplementedError>(() => New(root, Lex(Pair(new PyByteArray([97]), "A"))));
        var unknown = new PyInstance(Class("UnknownBuffer"));
        Assert.Throws<PyNotImplementedError>(() => New(root, Lex(Pair(unknown, "A"))));
        var scanner = New(root, Lex(Pair("a", Echo())));
        Assert.Throws<PyNotImplementedError>(() => Scan(scanner, unknown));
        PyObject.SetAttr(scanner, "lexicon", unknown); // 赋值本身不消费或限制值域。
        Assert.Equal("([], '!')", PyRepr.Repr(Scan(scanner, "!")));
        Assert.Throws<PyTypeError>(() => Scan(scanner, "a"));
    }

    [Fact]
    public void InlineFlagSyntaxErrorsStayAheadOfLaterSyntaxAndExternalDomainChecks()
    {
        PyClass root = Root(Module());
        var locale = Assert.Throws<PyRegexError>(() => New(root, Lex(Pair("(?L)[", "A"))));
        Assert.Equal("bad inline flags: cannot use 'L' flag with a str pattern", locale.MessageText);
        Assert.Equal(3L, locale.Position);
        var incompatible = Assert.Throws<PyRegexError>(() => New(root, Lex(Pair("(?au)[", "A"))));
        Assert.Equal("bad inline flags: flags 'a', 'u' and 'L' are incompatible", incompatible.MessageText);
        Assert.Equal(4L, incompatible.Position);
        Assert.Throws<PyRegexError>(() => New(root, Lex(Pair("[", "A")), 4L));
        Assert.Equal("cannot use LOCALE flag with a str pattern",
            Assert.Throws<PyValueError>(() => New(root, Lex(Pair("a", "A")), 4L)).PyMessage);
        Assert.Throws<PyRegexError>(() => New(root, Lex(Pair("[", "A")), 288L));
        Assert.Equal("ASCII and UNICODE flags are incompatible",
            Assert.Throws<PyValueError>(() => New(root, Lex(Pair("a", "A")), 288L)).PyMessage);
        Assert.Throws<PyRegexError>(() => New(root, Lex(Pair(Bytes("["), "A")), 32L));
        Assert.Equal("cannot use UNICODE flag with a bytes pattern",
            Assert.Throws<PyValueError>(() => New(root, Lex(Pair(Bytes("a"), "A")), 32L)).PyMessage);
        Assert.Equal("ASCII and LOCALE flags are incompatible",
            Assert.Throws<PyValueError>(() => New(root, Lex(Pair(Bytes("a"), "A")), 260L)).PyMessage);
        Assert.Throws<PyNotImplementedError>(() => New(root, Lex(Pair(Bytes("a"), "A")), 4L));
    }

    [Fact]
    public void UnsupportedSyntaxIsLocalAndDoesNotDisableOrdinaryRegex()
    {
        PyClass root = Root(Module());
        foreach (string pattern in new[] { "(a)", "(?P<x>a)", @"(a)\1", "(a)(?(1)b|c)", "(?<=a)b", "(?<!a)b", "(?i:a)", "(?>a)", "a++" })
            Assert.Throws<PyNotImplementedError>(() => New(root, Lex(Pair(pattern, "A"))));
        Assert.Throws<PyRegexError>(() => New(root, Lex(Pair("(a)[", "A"))));
        Assert.Throws<PyNotImplementedError>(() => New(root, Lex(Pair("(?<=a)[", "A"))));
        Assert.IsType<PyRegexMatch>(Method(PyRegex.Compile(@"(a)\1"), "match", "aa"));
        Assert.IsType<PyRegexMatch>(Method(PyRegex.Compile("(a)(?(1)b|c)"), "match", "ab"));
    }

    [Fact]
    public void BasicFlagsRejectUnsupportedValuesWithoutCallingIndex()
    {
        PyClass root = Root(Module());
        foreach (object flags in new object[] { -1L, 1L, 128L, 512L, new BigInteger(int.MaxValue) + 1, "0" })
            Assert.Throws<PyNotImplementedError>(() => New(root, Lex(), flags));
        foreach (long flags in new[] { 0L, 2L, 8L, 16L, 32L, 64L, 256L, 2L | 8 | 16 | 32 | 64 })
            Assert.Equal("([], '!')", PyRepr.Repr(Scan(New(root, Lex(Pair("a", "A")), flags), "!")));
        Assert.Equal("invalid SRE code", Assert.Throws<PyRuntimeError>(() => New(root, Lex(), 0L)).PyMessage);
        Assert.Equal("invalid SRE code", Assert.Throws<PyRuntimeError>(() => New(root, Lex(), false)).PyMessage);
        foreach (long flags in new[] { 2L, 8L, 16L, 32L, 64L, 256L })
            Assert.Throws<PyNotImplementedError>(() => New(root, Lex(), flags));
        Assert.Throws<PyNotImplementedError>(() => New(root, Lex(), 288L));
        Assert.Throws<PyNotImplementedError>(() => New(root, Lex(), 4L));
        Assert.Equal("(['A'], '')", PyRepr.Repr(Scan(New(root, Lex(Pair("a", "A")), false), "a")));
    }

    [Fact]
    public void PatternLengthAndNestingResourcesArePreparedLocally()
    {
        PyClass root = Root(Module());
        Assert.Contains("pattern length", Assert.Throws<PyNotImplementedError>(() => New(root, Lex(Pair(new string('a', 16385), "A")))).PyMessage);
        Assert.Contains("pattern length", Assert.Throws<PyNotImplementedError>(() => New(root, Lex(Pair(new PyBytes(new byte[16385]), "A")))).PyMessage);
        string verbose = "(?x)" + new string(' ', 8190);
        Assert.Contains("pattern length", Assert.Throws<PyNotImplementedError>(() => New(root, Lex(Pair(verbose, "A"), Pair(verbose, "B")))).PyMessage);
        string deep = string.Concat(Enumerable.Repeat("(?:", 257)) + "a" + new string(')', 257);
        long depth = PyTraceback.PushFrame("scanner_resource.py", "caller");
        try
        {
            PyTraceback.SetLine(23);
            var error = Assert.Throws<PyRecursionError>(() => New(root, Lex(Pair(deep, "A"))));
            Assert.Equal(23, error.Frames[0].Line);
        }
        finally { PyTraceback.PopTo(depth); }
    }

    [Fact]
    public void ItemLimitRunsAfterCurrentUnpackButNeverRequestsFollowingItem()
    {
        PyClass root = Root(Module()), sequence = Class("Many"); int count = 0;
        sequence.SetAttr("__iter__", Fn("iter", ["self"], a => a[0]));
        sequence.SetAttr("__next__", Fn("next", ["self"], _ =>
        {
            count++; if (count > 1025) throw new Exception("must not request next item");
            return Pair(count == 1025 ? "[" : "", "A");
        }));
        Assert.Contains("item resource", Assert.Throws<PyNotImplementedError>(() => New(root, new PyInstance(sequence))).PyMessage);
        Assert.Equal(1025, count); count = 0;
        sequence.SetAttr("__next__", Fn("next", ["self"], _ => ++count == 1025 ? new PyTuple(["[", "A", "extra"]) : Pair("", "A")));
        Assert.Throws<PyValueError>(() => New(root, new PyInstance(sequence))); Assert.Equal(1025, count);
    }

    [Fact]
    public void AggregateTranslatedLimitRejectsBeforeAccumulatingOrReadingNextPhrase()
    {
        PyClass root = Root(Module()), sequence = Class("UnicodeWords"); int count = 0;
        sequence.SetAttr("__iter__", Fn("iter", ["self"], a => a[0]));
        sequence.SetAttr("__next__", Fn("next", ["self"], _ =>
        {
            count++;
            if (count == 1024) throw new Exception("aggregate translated cap must precede item/source caps");
            return Pair(@"\w", "word");
        }));
        var scanner = new PyInstance(root);
        var error = Assert.Throws<PyNotImplementedError>(() => Call(PyObject.GetAttr(root, "__init__"), scanner, new PyInstance(sequence), 32L));
        Assert.Contains("translated pattern size", error.PyMessage); Assert.InRange(count, 2, 1023);
        Assert.Throws<PyAttributeError>(() => PyObject.GetAttr(scanner, "scanner"));
        // 每个\w的既有parser仍有单phrase临时输出；总门限制保留fragments，并非峰值仅200000。
    }

    [Fact]
    public void OrdinaryPatternAndMatchFieldSetsAndNonvirtualLoopsStayBounded()
    {
        const BindingFlags declared = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;
        Assert.Equal(8, typeof(PyRegexPattern).GetFields(declared).Length);
        Assert.Equal(7, typeof(PyRegexMatch).GetFields(declared).Length);
        foreach (string name in new[] { "Apply", "Enumerate", "Substitute", "ApplyBytes", "EnumerateBytes", "ApplyBytesInput", "SubstituteBytesInput", "SplitInput" })
            Assert.False(typeof(PyRegexPattern).GetMethod(name, declared)!.IsVirtual);
        // 恢复旧公开PE旗标：sealed类型不等于给override额外写入Final方法旗标。
        Assert.True(typeof(PyRegexPattern).IsSealed);
        Assert.Equal(1048833, (int)typeof(PyRegexPattern).Attributes);
        MethodInfo typeName = typeof(PyRegexPattern).GetProperty("TypeName")!.GetMethod!;
        Assert.False(typeName.IsFinal);
        Assert.Equal(2246, (int)typeName.Attributes);
        FieldInfo search = typeof(PyRegexPattern).GetField("_search", declared)!;
        Assert.Equal(typeof(Regex), search.FieldType);
        var scanner = New(Root(Module()), Lex(Pair("a", Echo())));
        var owner = Assert.IsType<PyRegexPattern>(PyObject.GetAttr(scanner, "scanner"));
        Assert.Same(owner, PyRegex.Compile(owner));
        var executor = Assert.IsAssignableFrom<Regex>(search.GetValue(owner));
        Assert.NotEqual(typeof(Regex), executor.GetType());
        Assert.Equal(typeof(Regex), executor.GetType().BaseType);
        Assert.True(executor.GetType().IsSealed);
        Assert.False(executor.GetType().IsVisible);
        Assert.Empty(executor.GetType().GetFields(declared));
        Assert.Equal(RegexOptions.CultureInvariant, executor.Options);
        Assert.Equal(TimeSpan.FromSeconds(2), executor.MatchTimeout);
        Assert.Equal("(['a', 'a'], '!')", PyRepr.Repr(Scan(scanner, "aa!")));
        var match = Assert.IsType<PyRegexMatch>(PyObject.GetAttr(scanner, "match"));
        Assert.Same(owner, PyObject.GetAttr(match, "re"));
        Assert.Throws<PyAttributeError>(() => PyObject.GetAttr(owner, "ScannerPlan"));
        Assert.Throws<PyAttributeError>(() => PyObject.GetAttr(owner, "_search"));
        // 字段/虚槽断言不是对象实测布局、JIT去虚化或吞吐回归结论。
    }
}
