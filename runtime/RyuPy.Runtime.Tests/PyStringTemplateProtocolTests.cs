// 模板回调顺序、真实异常与对象身份；所有测试共用生成器帧栈隔离集合，不直接测试token替代行为。
using System;
using System.Collections.Generic;
using RyuPy.Runtime;
using Xunit;

namespace RyuPy.Runtime.Tests;

public sealed partial class PyStringTemplateTests
{
    private static PyInstance Node(List<string> events)
    {
        var cls = new PyClass("Node"); var node = new PyInstance(cls);
        cls.SetAttr("a", new PyProperty(Fn("a", _ => { events.Add("property:a"); return node; })));
        cls.SetAttr("__getattr__", Fn("__getattr__", args => { events.Add("attr:" + args[1]); return node; }));
        cls.SetAttr("__getitem__", Fn("__getitem__", args =>
        {
            events.Add((args[1] is string ? "str:" : "int:") + args[1]); return node;
        }));
        cls.SetAttr("__format__", Fn("__format__", args => { events.Add("format:" + args[1]); return "v"; }));
        return node;
    }

    [Fact]
    public void AttributeItemNavigationIsOrderedAndIncremental()
    {
        var events = new List<string>(); var node = Node(events);
        Assert.Equal("v", Render("{0.a[x].b[2]}", node));
        Assert.Equal(new[] { "property:a", "str:x", "attr:b", "int:2", "format:" }, events);
    }

    [Fact]
    public void ItemKeysKeepPunctuationQuotesWhitespaceAndBraces()
    {
        foreach (string key in new[] { "-1", "+1", "'x'", "x:y", " ", "a.b", "!", ":", "{", "}", "[" })
        {
            var events = new List<string>();
            Assert.Equal("v", Render("{0[" + key + "]}", Node(events)));
            Assert.Equal(new[] { "str:" + key, "format:" }, events);
        }
    }

    [Fact]
    public void ItemNdKeysUseIntegersAndCheckNumericOverflowBeforeSuffix()
    {
        foreach (string key in new[] { "0", "00", "٠", "𝟎" })
        {
            var events = new List<string>(); Assert.Equal("v", Render("{0[" + key + "]}", Node(events)));
            Assert.Equal(new[] { "int:0", "format:" }, events);
        }
        var untouched = new List<string>();
        Error("{0[9223372036854775808x]}", "Too many decimal digits in format string", Node(untouched));
        Assert.Empty(untouched);
    }

    [Fact]
    public void InvalidPathSuffixFailsOnlyAfterPriorPropertyAndItemCalls()
    {
        var events = new List<string>(); var node = Node(events);
        Error("{0.a[x]tail}", "Only '.' or '[' may follow ']' in format field specifier", node);
        Assert.Equal(new[] { "property:a", "str:x" }, events); events.Clear();
        Error("{0.a.}", "Empty attribute in format string", node);
        Assert.Equal(new[] { "property:a" }, events); events.Clear();
        Error("{0[]}", "Empty attribute in format string", node); Assert.Empty(events);
    }

    [Fact]
    public void UnknownConversionFollowsLookupButMalformedConversionDoesNot()
    {
        var events = new List<string>(); var node = Node(events);
        Error("{0.a!q}", "Unknown conversion specifier q", node);
        Assert.Equal(new[] { "property:a" }, events); events.Clear();
        Error("{0.a!ss}", "expected ':' after conversion specifier", node); Assert.Empty(events);
    }

    [Fact]
    public void ConversionRunsBeforeNestedSpecificationAndPreventsOuterValueSlot()
    {
        var events = new List<string>();
        var outer = Value(_ => throw new Exception("converted value used original format slot"));
        outer.Class.SetAttr("__str__", Fn("__str__", _ => { events.Add("str"); return "x"; }));
        var inner = Value(_ => { events.Add("inner"); return ">3"; });
        Assert.Equal("  x", Render("{0!s:{1}}", outer, inner));
        Assert.Equal(new[] { "str", "inner" }, events);
    }

    [Fact]
    public void AsciiConversionEscapesOnlyNonAsciiCodePointsAndCallsReprOnce()
    {
        int calls = 0; var value = Value(_ => throw new Exception("value format after conversion"));
        value.Class.SetAttr("__repr__", Fn("__repr__", _ => { calls++; return "é😀\ud800"; }));
        Assert.Equal("\\xe9\\U0001f600\\ud800", Render("{0!a}", value));
        value.Class.SetAttr("__repr__", Fn("__repr__", _ => { calls++; return "A\n\x01"; }));
        Assert.Equal("A\n\x01", Render("{0!a}", value)); Assert.Equal(2, calls);
    }

    [Fact]
    public void ConversionReturningOrdinaryStringSubclassUsesInheritedValueFormat()
    {
        object returned = Text("value"); var value = Value(_ => returned);
        value.Class.SetAttr("__str__", Fn("__str__", _ => returned));
        value.Class.SetAttr("__repr__", Fn("__repr__", _ => returned));
        foreach (string conversion in new[] { "s", "r", "a" })
        {
            object actual = Render("{0!" + conversion + "}", value);
            Assert.Equal("value", Assert.IsType<string>(actual)); Assert.NotSame(returned, actual);
        }
    }

    [Fact]
    public void NestedSpecificationStripsSubclassWithoutCallingItsStrOverride()
    {
        // nested Substring精确化来自固定CP源码；公开builtin的原spec身份合同不改变。
        object returned = Text("width");
        ((PyInstance)returned).Class.SetAttr("__str__", Fn("__str__", _ => throw new Exception("nested str")));
        var inner = Value(_ => returned);
        var outer = Value(args => { Assert.Equal("width", Assert.IsType<string>(args[1])); return "ok"; });
        Assert.Equal("ok", Render("{0:{1}}", outer, inner));
    }

    [Fact]
    public void NestedCallbackCanReplaceOuterClassSlotBeforeItIsResolved()
    {
        var events = new List<string>(); var outer = Value(_ => throw new Exception("stale slot"));
        var inner = Value(_ =>
        {
            events.Add("inner");
            outer.Class.SetAttr("__format__", Fn("__format__", args => { events.Add("new:" + args[1]); return "changed"; }));
            return "width";
        });
        Assert.Equal("changed", Render("{0:{1}}", outer, inner));
        Assert.Equal(new[] { "inner", "new:width" }, events);
    }

    [Fact]
    public void ExistingStaticClassPropertyCallableSlotsAndInstanceShadowRemainCorrect()
    {
        var cls = new PyClass("Slots"); var value = new PyInstance(cls);
        PyObject.SetAttr(value, "__format__", Fn("__format__", _ => throw new Exception("instance slot")));
        cls.SetAttr("__format__", new PyStaticMethod(Fn("__format__", args => { Assert.Single(args); return "static"; })));
        Assert.Equal("static", Render("{0:spec}", value));
        cls.SetAttr("__format__", new PyClassMethod(Fn("__format__", args => { Assert.Same(cls, args[0]); return "class"; })));
        Assert.Equal("class", Render("{0:spec}", value));
        var callableClass = new PyClass("Callable");
        callableClass.SetAttr("__call__", Fn("__call__", args => { Assert.Equal("spec", args[1]); return "callable"; }));
        var callable = new PyInstance(callableClass);
        cls.SetAttr("__format__", new PyProperty(Fn("getter", _ => callable)));
        Assert.Equal("callable", Render("{0:spec}", value));
        cls.SetAttr("__format__", callable); Assert.Equal("callable", Render("{0:spec}", value));
    }

    [Fact]
    public void ReentrantCallDoesNotConsumeOuterAutomaticNumbering()
    {
        int calls = 0; var value = Value(_ => { calls++; return Render("{} {}", "a", "b"); });
        Assert.Equal("a b outer-second", Render("{} {}", value, "outer-second")); Assert.Equal(1, calls);
    }

    [Fact]
    public void UserFailuresPreserveOriginalObjectAndStopAtEveryCallbackBoundary()
    {
        var events = new List<string>(); var sentinel = new PyRuntimeError("sentinel");
        var cls = new PyClass("Fails"); var bad = new PyInstance(cls);
        foreach (string method in new[] { "__getattr__", "__getitem__", "__str__", "__repr__", "__format__" })
        {
            string captured = method;
            cls.SetAttr(method, Fn(method, _ => { events.Add(captured); throw sentinel; }));
        }
        var good = Value(_ => { events.Add("good"); return "F<>"; });
        var cases = new (string Template, object?[] Args, string[] Events)[]
        {
            ("{0.a:{1}}", [bad, good], ["__getattr__"]),
            ("{0[x]:{1}}", [bad, good], ["__getitem__"]),
            ("{0!s:{1}}", [bad, good], ["__str__"]),
            ("{0!r:{1}}", [bad, good], ["__repr__"]),
            ("{0:{1}}", [good, bad], ["__format__"]),
            ("{0:{1}}", [bad, good], ["good", "__format__"]),
        };
        foreach (var item in cases)
        {
            events.Clear(); Assert.Same(sentinel, Assert.Throws<PyRuntimeError>(() => Render(item.Template, item.Args)));
            Assert.Equal(item.Events, events);
        }
    }

    [Fact]
    public void InvalidFormatAndConversionReturnsDoNotFallbackOrRepeat()
    {
        int calls = 0; var value = Value(_ => { calls++; return 7L; });
        Assert.Equal("__format__ must return a str, not int", Assert.Throws<PyTypeError>(() => Render("{0}", value)).Message);
        value.Class.SetAttr("__str__", Fn("__str__", _ => { calls++; return 7L; }));
        value.Class.SetAttr("__repr__", Fn("__repr__", _ => { calls++; return 7L; }));
        Assert.Equal("__str__ returned non-string (type int)", Assert.Throws<PyTypeError>(() => Render("{0!s}", value)).Message);
        foreach (string conversion in new[] { "r", "a" })
            Assert.Equal("__repr__ returned non-string (type int)", Assert.Throws<PyTypeError>(() => Render("{0!" + conversion + "}", value)).Message);
        Assert.Equal(4, calls);
    }

    [Fact]
    public void UnicodeLiteralFillAndLoneSurrogateStayIntact()
    {
        Assert.Equal("😀 🦊🦊🦊é \ud800", Render("😀 {0:🦊>4} \ud800", "é"));
    }

    [Fact]
    public void OwnedParserNumberingRootAndDepthErrorsCaptureCallerAndAmbient()
    {
        var ambient = new PyValueError("ambient"); object? token = PyErrors.EnterHandler(ambient);
        long depth = PyTraceback.PushFrame("template_use.py", "caller");
        try
        {
            PyTraceback.SetLine(12);
            Action[] actions = [() => Render("{"), () => Render("{} {0}", "x"),
                () => Render("{1}"), () => Render("{missing}"), () => Render("{0!q}", "x"),
                () => Render("{0:{1:{2}}}", "x", "y", "z")];
            foreach (Action action in actions)
            {
                var error = Assert.IsAssignableFrom<PyBaseException>(Record.Exception(action));
                Assert.Same(ambient, error.Context); Assert.Null(error.Cause);
                Assert.Equal(new PyStackFrame("template_use.py", "caller", 12), error.Frames[0]);
                Assert.Same(ambient, PyErrors.Current);
            }
        }
        finally { PyTraceback.PopTo(depth); PyErrors.LeaveHandler(token); }
    }

    [Fact]
    public void UserPreparedExceptionKeepsCauseContextAndAllFramesUnchanged()
    {
        var ambient = new PyValueError("ambient"); var sentinel = new PyRuntimeError("sentinel");
        var cause = new PyValueError("explicit cause"); PyErrors.WithCause(sentinel, cause);
        object? token = PyErrors.EnterHandler(ambient); long depth = PyTraceback.PushFrame("template_use.py", "caller");
        try
        {
            PyTraceback.SetLine(10);
            var value = Value(_ =>
            {
                long callback = PyTraceback.PushFrame("template_use.py", "__format__");
                try { PyTraceback.SetLine(20); throw (PyRuntimeError)PyErrors.PrepareRaise(sentinel); }
                finally { PyTraceback.PopTo(callback); }
            });
            Assert.Same(sentinel, Assert.Throws<PyRuntimeError>(() => Render("{0}", value)));
            Assert.Same(ambient, sentinel.Context); Assert.Same(cause, sentinel.Cause);
            Assert.True(sentinel.SuppressContextFlag);
            Assert.Equal(2, sentinel.Frames.Length);
            Assert.Equal(new PyStackFrame("template_use.py", "caller", 10), sentinel.Frames[1]);
            Assert.Equal(new PyStackFrame("template_use.py", "__format__", 20), sentinel.Frames[0]);
            Assert.Same(ambient, PyErrors.Current);
        }
        finally { PyTraceback.PopTo(depth); PyErrors.LeaveHandler(token); }
    }

    [Fact]
    public void RepeatedFieldsReadMutatedStateInsteadOfCachingResolvedValues()
    {
        var cls = new PyClass("Record"); var root = new PyInstance(cls); int reads = 0;
        cls.SetAttr("a", new PyProperty(Fn("a", _ => { reads++; return reads == 1 ? "first" : "second"; })));
        Assert.Equal("first second", Render("{0.a} {0.a}", root)); Assert.Equal(2, reads);
    }
}
