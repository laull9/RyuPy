// Scanner普通类与构造顺序：只经公开入口，验证绑定、流式副作用、失败reinit与子类字段。
using System;
using System.Collections.Generic;
using System.Linq;
using RyuPy.Runtime;
using Xunit;

namespace RyuPy.Runtime.Tests;

[Collection("生成器帧栈")]
public sealed partial class PyRegexScannerTests
{
    private static Dictionary<string, object?> Module() => PyRegex.CreateAttributes();
    private static PyClass Root(Dictionary<string, object?> module) => Assert.IsType<PyClass>(module["Scanner"]);
    private static object? Call(object? value, params object?[] args) => PyOps.CallValue(value, args, [], []);
    private static object? Method(object? self, string name, params object?[] args) => Call(PyObject.GetAttr(self, name), args);
    private static PyFunction Fn(string name, string[] names, Func<object?[], object?> body) =>
        PyNativeFunctions.Create(name, names, new object?[names.Length], names.Length, body);
    private static PyTuple Pair(object? phrase, object? action) => new([phrase, action]);
    private static PyList Lex(params PyTuple[] entries) => new(entries.Cast<object?>().ToList());
    private static PyInstance New(PyClass root, object? lexicon, object? flags = null) =>
        Assert.IsType<PyInstance>(root.Invoke([lexicon, flags ?? 0L], [], []));
    private static PyTuple Scan(object? scanner, object? source) => Assert.IsType<PyTuple>(Method(scanner, "scan", source));
    private static PyList Tokens(PyTuple result) => Assert.IsType<PyList>(result.Items[0]);
    private static PyClass Class(string name, PyClass? parent = null)
    {
        var cls = (PyClass)PyOps.MakeClass(name, parent is null ? [] : [PyRuntimeBases.Validate(parent)], null);
        PyOps.FinalizeOrdinaryClass(cls);
        return cls;
    }
    private static PyFunction Echo() => Fn("echo", ["scanner", "token"], args => args[1]);
    private static PyGenerator Generator(params object?[] entries) => new(frameObject =>
    {
        var frame = (object?[])frameObject;
        int index = (int)(long)frame[0]!;
        if (index == entries.Length) { frame[0] = -1L; return PyNone.Instance; }
        frame[0] = (long)index + 1;
        return entries[index];
    }, [0L, null, null, null], "scanner_test.py", "lexicon");

    [Fact]
    public void OrdinaryClassKeywordsAndInitialFieldsAreReal()
    {
        var module = Module(); PyClass root = Root(module);
        PyList lexicon = Lex(Pair("a", "A"));
        var scanner = Assert.IsType<PyInstance>(root.Invoke([], ["lexicon", "flags"], [lexicon, 0L]));
        Assert.Same(root, PyObject.TypeOf(scanner));
        Assert.Equal("re", PyObject.GetAttr(root, "__module__"));
        Assert.Same(lexicon, PyObject.GetAttr(scanner, "lexicon"));
        Assert.Throws<PyAttributeError>(() => PyObject.GetAttr(scanner, "match"));
        Assert.Equal("(['A'], '!')", PyRepr.Repr(PyOps.CallValue(PyObject.GetAttr(scanner, "scan"), [], ["string"], ["a!"])));
        Assert.Throws<PyAttributeError>(() => PyObject.GetAttr(scanner, "match"));
    }

    [Fact]
    public void BinderRejectsBeforeBodyAndKeepsKeywordPriority()
    {
        PyClass root = Root(Module()); var scanner = New(root, Lex(Pair("a", "A")));
        object? original = PyObject.GetAttr(scanner, "lexicon");
        Assert.Equal("Scanner.__init__() got multiple values for argument 'lexicon'",
            Assert.Throws<PyTypeError>(() => PyOps.CallValue(PyObject.GetAttr(root, "__init__"),
                [scanner, Lex(), 0L, "extra"], ["lexicon"], [Lex()])).PyMessage);
        Assert.Same(original, PyObject.GetAttr(scanner, "lexicon"));
        Assert.Equal("Scanner.__init__() takes from 2 to 3 positional arguments but 4 were given",
            Assert.Throws<PyTypeError>(() => root.Invoke([Lex(), 0L, 0L], [], [])).PyMessage);
        Assert.Equal("Scanner.scan() got multiple values for argument 'string'",
            Assert.Throws<PyTypeError>(() => PyOps.CallValue(PyObject.GetAttr(scanner, "scan"),
                ["a", "extra"], ["string"], ["b"])).PyMessage);
        Assert.Equal("Scanner.scan() got an unexpected keyword argument 'other'",
            Assert.Throws<PyTypeError>(() => PyOps.CallValue(PyObject.GetAttr(scanner, "scan"),
                ["a", "extra"], ["other"], ["b"])).PyMessage);
        Assert.Equal("Scanner.scan() missing 1 required positional argument: 'string'",
            Assert.Throws<PyTypeError>(() => Method(scanner, "scan")).PyMessage);
        Assert.Equal("Scanner.scan() missing 2 required positional arguments: 'self' and 'string'",
            Assert.Throws<PyTypeError>(() => Call(PyObject.GetAttr(root, "scan"))).PyMessage);
        Assert.Equal("Scanner.__init__() missing 1 required positional argument: 'lexicon'",
            Assert.Throws<PyTypeError>(() => root.Invoke([], [], [])).PyMessage);
    }

    [Fact]
    public void IteratorIsAcquiredOnceAndInvalidPhraseStopsBeforeNext()
    {
        PyClass root = Root(Module()), sequence = Class("Sequence"); var events = new List<string>();
        int next = 0;
        sequence.SetAttr("__iter__", Fn("iter", ["self"], a => { events.Add("iter"); return a[0]; }));
        sequence.SetAttr("__next__", Fn("next", ["self"], _ =>
        {
            events.Add("next:" + next);
            return next++ switch { 0 => Pair("a", "A"), 1 => Pair("[", "bad"), _ => throw new Exception("must not request later item") };
        }));
        sequence.SetAttr("__len__", Fn("len", ["self"], _ => throw new Exception("no len")));
        var lexicon = new PyInstance(sequence); var scanner = new PyInstance(root);
        var error = Assert.Throws<PyRegexError>(() => Call(PyObject.GetAttr(root, "__init__"), scanner, lexicon));
        Assert.Equal("[", error.Pattern); Assert.Equal(0L, error.Position);
        Assert.Equal(new[] { "iter", "next:0", "next:1" }, events);
        Assert.Same(lexicon, PyObject.GetAttr(scanner, "lexicon"));
        Assert.Throws<PyAttributeError>(() => PyObject.GetAttr(scanner, "scanner"));
    }

    [Fact]
    public void UnpackingChecksThirdItemBeforeParsingFirst()
    {
        PyClass root = Root(Module()); var consumed = new List<string>();
        PyClass row = Class("Row"); int index = 0;
        row.SetAttr("__iter__", Fn("iter", ["self"], a => a[0]));
        row.SetAttr("__next__", Fn("next", ["self"], _ =>
        {
            string value = new[] { "[", "action", "extra" }[index++]; consumed.Add(value); return value;
        }));
        var error = Assert.Throws<PyValueError>(() => New(root, new PyList([new PyInstance(row)])));
        Assert.Equal("too many values to unpack (expected 2)", error.PyMessage);
        Assert.Equal(new[] { "[", "action", "extra" }, consumed);
    }

    [Fact]
    public void FailedReinitKeepsNewLexiconOldPlanAndOldMatch()
    {
        PyClass root = Root(Module()); var scanner = New(root, Lex(Pair("a", Echo())));
        Scan(scanner, "a"); object? plan = PyObject.GetAttr(scanner, "scanner"), match = PyObject.GetAttr(scanner, "match");
        PyList replacement = Lex(Pair("[", "changed"));
        Assert.Throws<PyRegexError>(() => Method(scanner, "__init__", replacement));
        Assert.Same(replacement, PyObject.GetAttr(scanner, "lexicon"));
        Assert.Same(plan, PyObject.GetAttr(scanner, "scanner"));
        Assert.Same(match, PyObject.GetAttr(scanner, "match"));
        Assert.Equal("(['changed'], '')", PyRepr.Repr(Scan(scanner, "a")));
        Assert.Same(match, PyObject.GetAttr(scanner, "match"));
    }

    [Fact]
    public void EmptyReinitFailsAfterIterationAndPreservesOldPlanAndMatch()
    {
        PyClass root = Root(Module()), sequence = Class("Empty"); var events = new List<string>();
        var scanner = New(root, Lex(Pair("a", Echo())));
        Scan(scanner, "a"); object? plan = PyObject.GetAttr(scanner, "scanner"), match = PyObject.GetAttr(scanner, "match");
        int publishes = 0;
        root.SetAttr("scanner", new PyProperty(Fn("get", ["self"], _ => plan),
            Fn("set", ["self", "value"], _ => { publishes++; throw new Exception("empty reinit must not publish"); })));
        sequence.SetAttr("__iter__", Fn("iter", ["self"], a => { events.Add("iter"); return a[0]; }));
        sequence.SetAttr("__next__", Fn("next", ["self"], _ => { events.Add("next"); throw new PyStopIteration(); }));
        var empty = new PyInstance(sequence);
        var error = Assert.Throws<PyRuntimeError>(() => Method(scanner, "__init__", empty));
        Assert.Equal("invalid SRE code", error.PyMessage);
        Assert.Equal(new[] { "iter", "next" }, events);
        Assert.Same(empty, PyObject.GetAttr(scanner, "lexicon"));
        Assert.Same(plan, PyObject.GetAttr(scanner, "scanner"));
        Assert.Same(match, PyObject.GetAttr(scanner, "match"));
        Assert.Equal(0, publishes);
        Assert.Equal("([], '!')", PyRepr.Repr(Scan(scanner, "!")));
    }

    [Fact]
    public void GeneratorLexiconConstructsButOnlyMatchingScanNeedsIndexing()
    {
        PyClass root = Root(Module()); var generator = Generator(Pair("a", "A"));
        var scanner = New(root, generator);
        Assert.Same(generator, PyObject.GetAttr(scanner, "lexicon"));
        Assert.Equal("([], '!')", PyRepr.Repr(Scan(scanner, "!")));
        Assert.Equal("'generator' object is not subscriptable", Assert.Throws<PyTypeError>(() => Scan(scanner, "a")).PyMessage);
    }

    [Fact]
    public void SequenceFallbackIsLocalAndExistingIteratorErrorsSurvive()
    {
        PyClass root = Root(Module()), sequence = Class("Indexed"); int reads = 0;
        sequence.SetAttr("__getitem__", Fn("getitem", ["self", "index"], _ => { reads++; return Pair("a", "A"); }));
        Assert.Throws<PyNotImplementedError>(() => New(root, new PyInstance(sequence))); Assert.Equal(0, reads);
        var error = new PyValueError("iter error");
        sequence.SetAttr("__iter__", Fn("iter", ["self"], _ => throw error));
        Assert.Same(error, Assert.Throws<PyValueError>(() => New(root, new PyInstance(sequence))));
        Assert.Equal(0, reads);
    }

    [Fact]
    public void FlagsAreConsumedAfterAssignmentIterationUnpackAndSource()
    {
        PyClass root = Root(Module()), flagsClass = Class("Flags"); int arithmetic = 0;
        flagsClass.SetAttr("__index__", Fn("index", ["self"], _ => { arithmetic++; return 0L; }));
        var flags = new PyInstance(flagsClass); var scanner = new PyInstance(root);
        var badPair = new PyList([new PyTuple(["a", "A", "extra"])]);
        Assert.Throws<PyValueError>(() => Call(PyObject.GetAttr(root, "__init__"), scanner, badPair, flags));
        Assert.Same(badPair, PyObject.GetAttr(scanner, "lexicon"));
        Assert.Throws<PyTypeError>(() => New(root, Lex(Pair(42L, "A")), flags));
        Assert.Throws<PyNotImplementedError>(() => New(root, Lex(Pair("a", "A")), flags));
        Assert.Throws<PyNotImplementedError>(() => New(root, Lex(), flags));
        Assert.Equal(0, arithmetic);
    }

    [Fact]
    public void SubclassPropertyAndFinalSetterKeepOrdinaryObjectOrder()
    {
        PyClass root = Root(Module()), child = Class("Child", root); var writes = new List<string>();
        child.SetAttr("match", new PyProperty(Fn("get", ["self"], a => PyObject.GetAttr(a[0], "saved")),
            Fn("set", ["self", "value"], a => { writes.Add((string)Method(a[1], "group")!); PyObject.SetAttr(a[0], "saved", a[1]); return null; })));
        var scanner = New(child, Lex(Pair("[ab]", Echo())));
        Assert.True(PyObject.IsInstance(scanner, root));
        Assert.Equal("(['a', 'b'], '!')", PyRepr.Repr(Scan(scanner, "ab!")));
        Assert.Equal(new[] { "a", "b" }, writes);
        object? previous = PyObject.GetAttr(scanner, "scanner"); var error = new PyValueError("publish");
        child.SetAttr("scanner", new PyProperty(Fn("get", ["self"], _ => previous),
            Fn("set", ["self", "value"], _ => throw error)));
        PyList replacement = Lex(Pair("b", "B"));
        Assert.Same(error, Assert.Throws<PyValueError>(() => Method(scanner, "__init__", replacement)));
        Assert.Same(replacement, PyObject.GetAttr(scanner, "lexicon"));
        Assert.Same(previous, PyObject.GetAttr(scanner, "scanner"));
        Assert.Equal("b", Method(PyObject.GetAttr(scanner, "match"), "group"));
    }

    [Fact]
    public void MethodsAndOrdinaryFieldsRemainWritableAndDeletable()
    {
        PyClass root = Root(Module()), child = Class("Child", root);
        var scanner = New(child, Lex(Pair("a", "A")));
        child.SetAttr("scan", Fn("override", ["self", "string"], _ => "class"));
        Assert.Equal("class", Method(scanner, "scan", "a"));
        PyObject.SetAttr(scanner, "scan", Fn("instance", ["string"], _ => "instance"));
        Assert.Equal("instance", Method(scanner, "scan", "a"));
        PyObject.DelAttr(scanner, "scan"); Assert.Equal("class", Method(scanner, "scan", "a"));
        Assert.Equal("(['A'], '')", PyRepr.Repr(Call(PyObject.GetAttr(root, "scan"), scanner, "a")));
        PyObject.SetAttr(scanner, "scanner", PyNone.Instance);
        Assert.Equal("'NoneType' object has no attribute 'scanner'",
            Assert.Throws<PyAttributeError>(() => Call(PyObject.GetAttr(root, "scan"), scanner, "a")).PyMessage);
        PyObject.DelAttr(scanner, "scanner");
        Assert.Throws<PyAttributeError>(() => Call(PyObject.GetAttr(root, "scan"), scanner, "a"));
    }
}
