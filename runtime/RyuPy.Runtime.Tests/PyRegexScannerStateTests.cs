// Scanner真实多token状态机：左优先/零长、live下标、回调与递归及可观察Match身份。
using System;
using System.Collections.Generic;
using RyuPy.Runtime;
using Xunit;

namespace RyuPy.Runtime.Tests;

public sealed partial class PyRegexScannerTests
{
    [Fact]
    public void LeftPriorityNoSkippingAndZeroLengthStopAreExact()
    {
        PyClass root = Root(Module());
        Assert.Equal("(['short'], 'b!')", PyRepr.Repr(Scan(New(root, Lex(Pair("a", "short"), Pair("ab", "long"))), "ab!")));
        Assert.Equal("([], 'b')", PyRepr.Repr(Scan(New(root, Lex(Pair("a*", Echo()), Pair("b", "B"))), "b")));
        Assert.Equal("(['a'], 'b')", PyRepr.Repr(Scan(New(root, Lex(Pair("a*", Echo()), Pair("b", "B"))), "ab")));
        Assert.Equal("([], '!a')", PyRepr.Repr(Scan(New(root, Lex(Pair("a", "A"))), "!a")));
        Assert.Equal("(['aa', 'a'], '!')", PyRepr.Repr(Scan(New(root, Lex(Pair("a{1,2}", Echo()))), "aaa!")));
        Assert.Equal("(['a', 'a'], '!')", PyRepr.Repr(Scan(New(root, Lex(Pair("a+?", Echo()))), "aa!")));
    }

    [Fact]
    public void AnchorsAndLookaheadRetainWholeInputContext()
    {
        PyClass root = Root(Module());
        var scanner = New(root, Lex(Pair(@"\Aa", "first"), Pair(@"a\b", "word-end"), Pair("b(?=!)", "peek")));
        Assert.Equal("(['first', 'word-end'], '!')", PyRepr.Repr(Scan(scanner, "aa!")));
        Assert.Equal("(['first', 'peek'], '!')", PyRepr.Repr(Scan(scanner, "ab!")));
        var boundary = New(root, Lex(Pair("a", "a"), Pair(@"\bb", "boundary")));
        Assert.Equal("(['a'], 'b!')", PyRepr.Repr(Scan(boundary, "ab!")));
        Assert.Equal("(['a', 'b'], '!')", PyRepr.Repr(Scan(New(root,
            Lex(Pair("a(?!x)", "a"), Pair("b", "b"))), "ab!")));
        Assert.Equal("(['a', '\\n', 'a'], '!')", PyRepr.Repr(Scan(New(root,
            Lex(Pair("^a", Echo()), Pair("\\n", Echo())), 8L), "a\na!")));
    }

    [Fact]
    public void ConstantActionsAndCallbackResultsNeverTestTruth()
    {
        PyClass root = Root(Module()), opaqueClass = Class("Opaque");
        opaqueClass.SetAttr("__bool__", Fn("bool", ["self"], _ => throw new Exception("no bool")));
        opaqueClass.SetAttr("__len__", Fn("len", ["self"], _ => throw new Exception("no len")));
        var opaque = new PyInstance(opaqueClass); var empty = new PyList([]);
        var rawClassMethod = new PyClassMethod(Echo());
        var scanner = New(root, Lex(Pair("a", false), Pair("b", 0L), Pair("c", ""), Pair("d", empty),
            Pair("e", opaque), Pair("f", rawClassMethod), Pair("g", PyNone.Instance)));
        PyTuple output = Scan(scanner, "abcdefg!");
        Assert.Equal(6, Tokens(output).Count);
        Assert.Equal(false, Tokens(output).Items[0]); Assert.Equal(0L, Tokens(output).Items[1]);
        Assert.Equal("", Tokens(output).Items[2]); Assert.Same(empty, Tokens(output).Items[3]);
        Assert.Same(opaque, Tokens(output).Items[4]); Assert.Same(rawClassMethod, Tokens(output).Items[5]);
        Assert.Equal("!", output.Items[1]);
        Assert.Throws<PyAttributeError>(() => PyObject.GetAttr(scanner, "match"));
        scanner = New(root, Lex(Pair("a", Fn("callback", ["scanner", "token"], _ => opaque))));
        Assert.Same(opaque, Tokens(Scan(scanner, "a")).Items[0]);
        scanner = New(root, Lex(Pair("a", new PyStaticMethod(Echo()))));
        Assert.Equal("(['a'], '!')", PyRepr.Repr(Scan(scanner, "a!")));
    }

    [Fact]
    public void MatchIsStickyAcrossConstantFailureEmptyAndZeroScans()
    {
        PyClass root = Root(Module()); var scanner = New(root, Lex(Pair("a", Echo()), Pair("b", "B")));
        Scan(scanner, "a"); object? saved = PyObject.GetAttr(scanner, "match");
        Assert.Equal("(['B'], '!')", PyRepr.Repr(Scan(scanner, "b!")));
        Assert.Equal("([], '!')", PyRepr.Repr(Scan(scanner, "!")));
        Assert.Equal("([], '')", PyRepr.Repr(Scan(scanner, "")));
        Method(scanner, "__init__", Lex(Pair("a*", Echo())));
        Assert.Equal("([], 'b')", PyRepr.Repr(Scan(scanner, "b")));
        Assert.Same(saved, PyObject.GetAttr(scanner, "match"));
    }

    [Fact]
    public void EachTokenReadsCurrentLexiconAndEntryThroughTwoSubscripts()
    {
        PyClass root = Root(Module()); PyList lexicon = Lex();
        var first = Fn("first", ["scanner", "token"], args =>
        { lexicon.Items[1] = Pair("ignored", "changed"); return args[1]; });
        lexicon.Items.Add(Pair("a", first)); lexicon.Items.Add(Pair("b", "old"));
        var scanner = New(root, lexicon);
        Assert.Equal("(['a', 'changed'], '!')", PyRepr.Repr(Scan(scanner, "ab!")));
        var replacement = Lex(Pair("z", "unused"), Pair("z", "replacement"));
        lexicon.Items[0] = Pair("a", Fn("rebind", ["scanner", "token"], args =>
        { PyObject.SetAttr(args[0], "lexicon", replacement); return args[1]; }));
        Assert.Equal("(['a', 'replacement'], '!')", PyRepr.Repr(Scan(scanner, "ab!")));

        var reads = new List<string>(); PyClass live = Class("Live"), row = Class("Row");
        var rowObject = new PyInstance(row);
        live.SetAttr("__getitem__", Fn("get", ["self", "index"], args =>
        { reads.Add("lex:" + args[1]); return rowObject; }));
        row.SetAttr("__getitem__", Fn("get", ["self", "index"], args =>
        { reads.Add("row:" + args[1]); return "live"; }));
        PyObject.SetAttr(scanner, "lexicon", new PyInstance(live));
        Assert.Equal("(['live', 'live'], '!')", PyRepr.Repr(Scan(scanner, "ab!")));
        Assert.Equal(new[] { "lex:0", "row:1", "lex:1", "row:1" }, reads);
    }

    [Fact]
    public void BackendRebindOnlyAffectsNextScanAndMatchKeepsOldPlan()
    {
        PyClass root = Root(Module()); var other = New(root, Lex(Pair("b", Echo())));
        object? replacement = PyObject.GetAttr(other, "scanner"), original = null;
        var seen = new List<object?>();
        var scanner = New(root, Lex(Pair("a", Fn("first", ["scanner", "token"], args =>
        {
            PyObject.SetAttr(args[0], "scanner", replacement);
            seen.Add(PyObject.GetAttr(PyObject.GetAttr(args[0], "match"), "re")); return args[1];
        })), Pair("b", Fn("second", ["scanner", "token"], args =>
        { seen.Add(PyObject.GetAttr(PyObject.GetAttr(args[0], "match"), "re")); return "B"; }))));
        original = PyObject.GetAttr(scanner, "scanner");
        Assert.Equal("(['a', 'B'], '!')", PyRepr.Repr(Scan(scanner, "ab!")));
        Assert.All(seen, value => Assert.Same(original, value));
        Assert.Same(replacement, PyObject.GetAttr(scanner, "scanner"));
        Assert.Equal("(['b'], '!')", PyRepr.Repr(Scan(scanner, "b!")));
    }

    [Fact]
    public void NestedScanHasIndependentCursorAndDoesNotRestorePublicMatch()
    {
        PyClass root = Root(Module()); var events = new List<string>();
        var callback = Fn("callback", ["scanner", "token"], args =>
        {
            if ((string)args[1]! == "a")
            {
                object? outer = PyObject.GetAttr(args[0], "match");
                events.Add(PyRepr.Repr(Scan(args[0], "b!")));
                Assert.NotSame(outer, PyObject.GetAttr(args[0], "match"));
            }
            return args[1];
        });
        var scanner = New(root, Lex(Pair("[ab]", callback)));
        Assert.Equal("(['a'], '!')", PyRepr.Repr(Scan(scanner, "a!")));
        Assert.Equal(new[] { "(['b'], '!')" }, events);
        Assert.Equal("b", Method(PyObject.GetAttr(scanner, "match"), "group"));
        Assert.Equal("(['a', 'b'], '!')", PyRepr.Repr(Scan(scanner, "ab!")));
    }

    [Fact]
    public void CallableInstanceUsesClassSlotAndSetterCanChangeIt()
    {
        PyClass root = Root(Module()), child = Class("Child", root), actionClass = Class("Action");
        var action = new PyInstance(actionClass);
        PyObject.SetAttr(action, "__call__", Fn("instance", ["scanner", "token"], _ => throw new Exception("instance slot ignored")));
        var scanner = New(root, Lex(Pair("a", action)));
        Assert.Same(action, Tokens(Scan(scanner, "a")).Items[0]);
        actionClass.SetAttr("__call__", Fn("old", ["self", "scanner", "token"], _ => throw new Exception("must not prebind")));
        child.SetAttr("match", new PyProperty(Fn("get", ["self"], a => PyObject.GetAttr(a[0], "saved")),
            Fn("set", ["self", "match"], a =>
            {
                PyObject.SetAttr(a[0], "saved", a[1]);
                actionClass.SetAttr("__call__", Fn("new", ["self", "scanner", "token"], b => "new:" + b[2]));
                return null;
            })));
        scanner = New(child, Lex(Pair("a", action)));
        Assert.Equal("(['new:a'], '!')", PyRepr.Repr(Scan(scanner, "a!")));
    }

    [Fact]
    public void NoneCallSlotStillWritesMatchBeforeCallError()
    {
        PyClass root = Root(Module()), actionClass = Class("Action");
        actionClass.SetAttr("__call__", PyNone.Instance);
        var scanner = New(root, Lex(Pair("a", new PyInstance(actionClass))));
        Assert.Throws<PyTypeError>(() => Scan(scanner, "a!"));
        Assert.Equal("a", Method(PyObject.GetAttr(scanner, "match"), "group"));
    }

    [Fact]
    public void CallbackAndActionLookupErrorsPreserveIdentityAndLastMatch()
    {
        PyClass root = Root(Module()); var error = new PyValueError("callback");
        var scanner = New(root, Lex(Pair("a", Fn("raise", ["scanner", "token"], _ => throw error))));
        Assert.Same(error, Assert.Throws<PyValueError>(() => Scan(scanner, "a!")));
        Assert.Equal("a", Method(PyObject.GetAttr(scanner, "match"), "group"));
        Assert.Equal("([], '!')", PyRepr.Repr(Scan(scanner, "!")));

        PyList lexicon = Lex();
        lexicon.Items.Add(Pair("a", Fn("mutate", ["scanner", "token"], a => { lexicon.Items.Clear(); return a[1]; })));
        lexicon.Items.Add(Pair("b", "B")); scanner = New(root, lexicon);
        Assert.Throws<PyIndexError>(() => Scan(scanner, "ab!"));
        Assert.Equal("(0, 1)", PyRepr.Repr(Method(PyObject.GetAttr(scanner, "match"), "span")));
    }

    [Fact]
    public void SavedCallbackExceptionKeepsCauseContextAndTraceback()
    {
        PyClass root = Root(Module()); var error = new PyValueError("saved");
        var context = new PyValueError("original");
        var later = new PyValueError("later");
        var cause = new PyValueError("explicit cause"); error.WithCause(cause);
        object? previous = PyErrors.EnterHandler(context); long depth = PyTraceback.PushFrame("saved.py", "original");
        try { PyTraceback.SetLine(7); PyErrors.PrepareRaise(error); }
        finally { PyTraceback.PopTo(depth); PyErrors.LeaveHandler(previous); }
        var frames = error.Frames;
        var scanner = New(root, Lex(Pair("a", Fn("raise", ["scanner", "token"], _ => throw error))));
        previous = PyErrors.EnterHandler(later); depth = PyTraceback.PushFrame("later.py", "caller");
        try
        {
            PyTraceback.SetLine(99);
            Assert.Same(error, Assert.Throws<PyValueError>(() => Scan(scanner, "a")));
            Assert.Same(context, error.Context); Assert.Same(frames, error.Frames); Assert.Equal(7, error.Frames[0].Line);
            Assert.Same(cause, error.Cause); Assert.True(error.SuppressContextFlag);
        }
        finally { PyTraceback.PopTo(depth); PyErrors.LeaveHandler(previous); }
    }
}
