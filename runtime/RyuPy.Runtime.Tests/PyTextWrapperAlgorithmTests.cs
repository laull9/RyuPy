// 固定 CPython 3.12.13 TextWrapper 参考：精确词块、默认算法和可观察列表/回调副作用。
using System;
using System.Collections.Generic;
using System.Linq;
using RyuPy.Runtime;
using Xunit;

namespace RyuPy.Runtime.Tests;

[Collection("生成器帧栈")]
public sealed class PyTextWrapperAlgorithmTests
{
    private static PyClass Root => Assert.IsType<PyClass>(PyTextWrap.CreateAttributes()["TextWrapper"]);
    private static PyClass Derived()
    {
        var cls = (PyClass)PyOps.MakeClass("WrapperAlgorithmChild", [Root], null);
        PyOps.FinalizeOrdinaryClass(cls);
        return cls;
    }
    private static PyInstance New(params (string Name, object? Value)[] options) =>
        Assert.IsType<PyInstance>(Derived().Invoke([], options.Select(x => x.Name).ToArray(), options.Select(x => x.Value).ToArray()));
    private static object? Method(object? self, string name, params object?[] args) =>
        PyOps.CallValue(PyObject.GetAttr(self, name), args, [], []);
    private static string[] Strings(object? value) => Assert.IsType<PyList>(value).Items.Cast<string>().ToArray();
    private static void SameLines(string[] expected, object? value) => Assert.Equal(expected, Strings(value));

    public static IEnumerable<object?[]> SplitCases()
    {
        var rows = new (string Text, string[] Hyphens, string[] Simple)[]
        {
            ("", [], []),
            ("  a b  ", ["  ", "a", " ", "b", "  "], ["  ", "a", " ", "b", "  "]),
            ("a\tb\nc\vd\fe\rf", ["a", "\t", "b", "\n", "c", "\v", "d", "\f", "e", "\r", "f"],
                ["a", "\t", "b", "\n", "c", "\v", "d", "\f", "e", "\r", "f"]),
            ("a\u00a0b c", ["a\u00a0b", " ", "c"], ["a\u00a0b", " ", "c"]),
            ("goof-ball", ["goof-", "ball"], ["goof-ball"]),
            ("ab-cd-ef", ["ab-", "cd-", "ef"], ["ab-cd-ef"]),
            ("a-b-c", ["a-b-c"], ["a-b-c"]),
            ("foo--bar", ["foo", "--", "bar"], ["foo--bar"]),
            ("a------b", ["a", "------", "b"], ["a------b"]),
            ("你好-世界 😀ab", ["你好-", "世界", " ", "😀ab"], ["你好-世界", " ", "😀ab"]),
        };
        foreach (var row in rows)
        {
            yield return [row.Text, true, row.Hyphens];
            yield return [row.Text, false, row.Simple];
            yield return [row.Text, 1L, row.Simple];
        }
    }

    [Theory]
    [MemberData(nameof(SplitCases))]
    public void SplitKeepsExactChunksAndTrueIdentity(string text, object hyphens, string[] expected)
    {
        var self = New(("break_on_hyphens", hyphens));
        object? first = Method(self, "_split", text);
        SameLines(expected, first);
        Assert.NotSame(first, Method(self, "_split", text));
    }

    public static IEnumerable<object?[]> WrapCases()
    {
        yield return ["hello world", 2L, null, null, new[] { "he", "ll", "o ", "wo", "rl", "d" }];
        yield return ["a\tb", 4L, null, null, new[] { "a", "b" }];
        yield return ["a\tb", 3L, "expand_tabs", false, new[] { "a b" }];
        yield return ["a\nb c", 3L, "replace_whitespace", false, new[] { "a\nb", "c" }];
        yield return ["a\u00a0b c", 3L, null, null, new[] { "a\u00a0b", "c" }];
        yield return ["你好世界abc", 3L, null, null, new[] { "你好世", "界ab", "c" }];
        yield return ["😀😀abcd", 3L, null, null, new[] { "😀😀a", "bcd" }];
        yield return ["goof-ball a-b-c", 5L, "break_on_hyphens", false, new[] { "goof-", "ball", "a-b-c" }];
        yield return ["abcdef", 2L, "break_long_words", false, new[] { "abcdef" }];
        yield return ["  a b  ", 3L, "drop_whitespace", false, new[] { "  a", " b", "  " }];
        yield return ["abc", 1L, "indent-pair", null, new[] { ">>a", "--b", "--c" }];
        yield return ["", 2L, null, null, Array.Empty<string>()];
    }

    [Theory]
    [MemberData(nameof(WrapCases))]
    public void DefaultInheritedWrapAndFillMatchFixedReference(string text, long width, string? name,
        object? value, string[] expected)
    {
        var self = New(("width", width));
        if (name == "indent-pair")
        {
            PyObject.SetAttr(self, "initial_indent", ">>"); PyObject.SetAttr(self, "subsequent_indent", "--");
        }
        else if (name is not null) PyObject.SetAttr(self, name, value);
        SameLines(expected, Method(self, "wrap", text));
        Assert.Equal(string.Join('\n', expected), Method(self, "fill", text));
    }

    [Fact]
    public void WrapChunksConsumesOriginalList()
    {
        var chunks = new PyList(["ab", " ", "cd", " ", "ef"]);
        SameLines(["ab", "cd", "ef"], Method(New(("width", 3L)), "_wrap_chunks", chunks));
        Assert.Empty(chunks.Items);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void LongWordUpdatesBothOriginalLists(bool breaking)
    {
        var remaining = new PyList(["abcdef"]); var current = new PyList();
        Assert.Same(PyNone.Instance, Method(New(("break_long_words", breaking)), "_handle_long_word", remaining, current, 0L, 2L));
        SameLines(breaking ? ["ab"] : ["abcdef"], current);
        SameLines(breaking ? ["cdef"] : [], remaining);
    }

    [Fact]
    public void DynamicLongWordObservesActualRemainingChunks()
    {
        PyClass cls = Derived(); var events = new List<string>();
        object? baseMethod = PyObject.GetAttr(Root, "_handle_long_word");
        cls.SetAttr("_handle_long_word", PyNativeFunctions.CreateVariadic("_handle_long_word", args =>
        {
            events.Add(PyRepr.Repr(args[1]));
            return PyOps.CallValue(baseMethod, args, [], []);
        }));
        var self = Assert.IsType<PyInstance>(cls.Invoke([], ["width"], [2L]));
        var remaining = new PyList(["abcdef"]);
        SameLines(["ab", "cd", "ef"], Method(self, "_wrap_chunks", remaining));
        Assert.Equal(new[] { "['abcdef']", "['cdef']" }, events);
        Assert.Empty(remaining.Items);
    }

    [Fact]
    public void RaisingHookKeepsOriginalExceptionAndPartialListState()
    {
        PyClass cls = Derived(); var boom = new PyValueError("hook stop"); PyList? seen = null;
        cls.SetAttr("_handle_long_word", PyNativeFunctions.CreateVariadic("_handle_long_word", args =>
        {
            seen = Assert.IsType<PyList>(args[1]);
            SameLines(["ab", " "], args[2]);
            Assert.Equal(3L, args[3]); Assert.Equal(3L, args[4]);
            seen.Items.Add("seen"); throw boom;
        }));
        var self = cls.Invoke([], ["width"], [3L]); var chunks = new PyList(["ab", " ", "cdefgh"]);
        Assert.Same(boom, Assert.Throws<PyValueError>(() => Method(self, "_wrap_chunks", chunks)));
        Assert.Same(chunks, seen); SameLines(["cdefgh", "seen"], chunks);
    }

    [Fact]
    public void UnusedOptionsDoNotFailOrChangeInputIdentity()
    {
        var token = new PyList();
        var self = New(("width", 4L), ("expand_tabs", false), ("tabsize", token), ("placeholder", token));
        SameLines(["abc"], Method(self, "wrap", "abc"));
        SameLines(["ab", "cd"], Method(self, "wrap", "ab cd"));
        PyObject.SetAttr(self, "replace_whitespace", false);
        Assert.Same(token, Method(self, "_munge_whitespace", token));
    }

    [Fact]
    public void ErrorOrderIsTextThenWidthAndWidthBeforeListMutation()
    {
        var self = New(("width", 0L));
        Assert.Equal("invalid width 0 (must be > 0)", Assert.Throws<PyValueError>(() => Method(self, "wrap", "")).PyMessage);
        Assert.Equal("'NoneType' object has no attribute 'expandtabs'", Assert.Throws<PyAttributeError>(() => Method(self, "wrap", PyNone.Instance)).PyMessage);
        PyObject.SetAttr(self, "expand_tabs", false); PyObject.SetAttr(self, "replace_whitespace", false);
        Assert.Equal("expected string or bytes-like object, got 'NoneType'", Assert.Throws<PyTypeError>(() => Method(self, "wrap", PyNone.Instance)).PyMessage);
        var chunks = new PyList(["first", "second"]);
        Assert.Throws<PyValueError>(() => Method(self, "_wrap_chunks", chunks));
        SameLines(["first", "second"], chunks);
    }

    [Fact]
    public void DeferredModesAndConsumedClassDataFailExplicitly()
    {
        Assert.Throws<PyNotImplementedError>(() => Method(New(("fix_sentence_endings", true)), "wrap", "one. Two."));
        Assert.Throws<PyNotImplementedError>(() => Method(New(("max_lines", 1L)), "wrap", "one two three"));
        Assert.Throws<PyNotImplementedError>(() => Method(New(("width", 1.5)), "wrap", "a"));
        var self = New(("break_on_hyphens", true));
        PyObject.SetAttr(self, "wordsep_re", new PyList());
        Assert.Throws<PyNotImplementedError>(() => Method(self, "_split", "abc"));
        PyObject.SetAttr(self, "break_on_hyphens", false);
        SameLines(["abc"], Method(self, "_split", "abc"));
        PyObject.SetAttr(self, "unicode_whitespace_trans", new PyList());
        Assert.Throws<PyNotImplementedError>(() => Method(self, "_munge_whitespace", "abc"));
        PyObject.SetAttr(self, "replace_whitespace", false);
        Assert.Equal("abc", Method(self, "_munge_whitespace", "abc"));
    }

    [Fact]
    public void OversizedTabsizeFallsBackBeforeExpansionEvenWithoutTabs()
    {
        var self = New(("tabsize", (long)int.MaxValue + 1));
        Assert.Throws<PyNotImplementedError>(() => Method(self, "wrap", "abc"));
        PyObject.SetAttr(self, "expand_tabs", false);
        SameLines(["abc"], Method(self, "wrap", "abc"));
    }

    [Fact]
    public void InvalidWidthFormatsASecondPropertyReadWithoutReplacingItsError()
    {
        var self = New(); int reads = 0;
        self.Class.SetAttr("width", new PyProperty(PyNativeFunctions.Create("width", ["self"], [null], 1,
            _ => ++reads == 1 ? 0L : "changed")));
        Assert.Equal("invalid width 'changed' (must be > 0)",
            Assert.Throws<PyValueError>(() => Method(self, "_wrap_chunks", new PyList())).PyMessage);
        Assert.Equal(2, reads);
        reads = 0; var boom = new PyValueError("second getter");
        self.Class.SetAttr("width", new PyProperty(PyNativeFunctions.Create("width", ["self"], [null], 1,
            _ => ++reads == 1 ? 0L : throw boom)));
        Assert.Same(boom, Assert.Throws<PyValueError>(() => Method(self, "_wrap_chunks", new PyList())));
        Assert.Equal(2, reads);
    }

    [Fact]
    public void UnrelatedDuckDoesNotInventMissingDefaultClassData()
    {
        var duck = new PyInstance(new PyClass("ChunkDuck"));
        PyObject.SetAttr(duck, "expand_tabs", false); PyObject.SetAttr(duck, "replace_whitespace", true);
        object? munge = PyObject.GetAttr(Root, "_munge_whitespace");
        Assert.Equal("'ChunkDuck' object has no attribute 'unicode_whitespace_trans'",
            Assert.Throws<PyAttributeError>(() => PyOps.CallValue(munge, [duck, "abc"], [], [])).PyMessage);
        PyObject.SetAttr(duck, "replace_whitespace", false);
        var token = new PyList(); Assert.Same(token, PyOps.CallValue(munge, [duck, token], [], []));
        PyObject.SetAttr(duck, "break_on_hyphens", true);
        object? split = PyObject.GetAttr(Root, "_split");
        Assert.Equal("'ChunkDuck' object has no attribute 'wordsep_re'",
            Assert.Throws<PyAttributeError>(() => PyOps.CallValue(split, [duck, "abc"], [], [])).PyMessage);
        PyObject.SetAttr(duck, "wordsep_re", new PyList());
        Assert.Throws<PyNotImplementedError>(() => PyOps.CallValue(split, [duck, "abc"], [], []));
    }
}
