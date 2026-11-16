// 内部模板引擎的真实对象合同：178项CP参考及固定源码writer补证；不宣称公开入口已接通。
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.ExceptionServices;
using RyuPy.Runtime;
using Xunit;

namespace RyuPy.Runtime.Tests;

[Collection("生成器帧栈")]
public sealed partial class PyStringTemplateTests
{
    // 引擎尚不公开；只反射固定测试入口，不增加生产friend/public面。
    private static readonly MethodInfo Entry = typeof(PyOps).Assembly
        .GetType("RyuPy.Runtime.PyStringTemplate", throwOnError: true)!
        .GetMethod("Format", BindingFlags.Static | BindingFlags.NonPublic)!;
    private static object Format(object template, object?[] args, string[] names, object?[] values)
    {
        try { return Entry.Invoke(null, [template, args, names, values])!; }
        catch (TargetInvocationException error) when (error.InnerException is not null)
        {
            ExceptionDispatchInfo.Capture(error.InnerException).Throw();
            throw;
        }
    }
    private static object Render(object template, params object?[] args) => Format(template, args, [], []);
    private static PyFunction Fn(string name, Func<object?[], object?> body) =>
        PyNativeFunctions.CreateVariadic(name, body);
    private static PyInstance Value(Func<object?[], object?> body)
    {
        var cls = new PyClass("Value");
        cls.SetAttr("__format__", Fn("__format__", body));
        return new PyInstance(cls);
    }
    private static object Text(string text)
    {
        var cls = (PyClass)PyOps.MakeClass("S", [PyType.Get("str")], null);
        PyOps.FinalizeOrdinaryClass(cls);
        return cls.Invoke([text], [], [])!;
    }
    private static void Error(string template, string message, params object?[] args) =>
        Assert.Equal(message, Assert.Throws<PyValueError>(() => Render(template, args)).Message);

    [Fact]
    public void EmptyAndUnusedArgumentsDoNotInspectValues()
    {
        var value = Value(_ => throw new Exception("unused argument was touched"));
        Assert.Same(string.Empty, Render("", value));
        Assert.Equal("abc", Format("abc", [value], ["unused"], [value]));
    }

    [Fact]
    public void NamesAreOrdinaryKeysAndMayCoexistWithAutomaticFields()
    {
        Assert.Equal("a:b:x:o", Format("{self}:{format_string}:{}:{other}",
            ["x"], ["self", "format_string", "other"], ["a", "b", "o"]));
    }

    [Fact]
    public void EscapedBracesAndRepeatedFieldsRenderNormally()
    {
        int calls = 0; var value = Value(_ => { calls++; return "v"; });
        Assert.Equal("{v}vv{}", Render("{{{0}}}{0}{0}{{}}", value));
        Assert.Equal(3, calls);
    }

    [Fact]
    public void OuterLexicalErrorsHaveDistinctMessages()
    {
        foreach (string text in new[] { "{", "a{" }) Error(text, "Single '{' encountered in format string");
        foreach (string text in new[] { "}", "a}" }) Error(text, "Single '}' encountered in format string");
        Error("{0", "expected '}' before end of string");
        Error("{0!}", "unmatched '{' in format spec");
        Error("{0!ss}", "expected ':' after conversion specifier");
        Error("{0:{1}", "unmatched '{' in format spec");
    }

    [Fact]
    public void LaterMalformedBraceDoesNotEraseEarlierCallback()
    {
        var events = new List<string>(); var value = Value(_ => { events.Add("format"); return "v"; });
        Error("{0} {", "Single '{' encountered in format string", value);
        Error("{0} }", "Single '}' encountered in format string", value);
        Assert.Equal(new[] { "format", "format" }, events);
    }

    [Fact]
    public void NumberingSwitchFailsAfterEarlierFieldAndStateResets()
    {
        int calls = 0; var value = Value(_ => { calls++; return "v"; });
        Error("{} {0}", "cannot switch from automatic field numbering to manual field specification", value);
        Error("{0} {}", "cannot switch from manual field specification to automatic field numbering", value);
        Assert.Equal(2, calls);
        Assert.Equal("v", Render("{}", value)); Assert.Equal(3, calls);
    }

    [Fact]
    public void UnicodeNdAndLeadingZerosArePositionalIndices()
    {
        foreach (string field in new[] { "00", "٠", "０", "𝟎", "٠0", new string('0', 80) })
            Assert.Equal("yes", Render("{" + field + "}", "yes"));
    }

    [Fact]
    public void NonNdNamesAreNotTrimmedOrParsedAsExpressions()
    {
        foreach (string name in new[] { "²", "-1", "+1", " ", "x y" })
            Assert.Equal("yes", Format("{" + name + "}", [], [name], ["yes"]));
    }

    [Fact]
    public void IndexOverflowIsDistinctFromMissingPositionIncludingNumericPrefix()
    {
        Assert.Equal("Replacement index 9223372036854775807 out of range for positional args tuple",
            Assert.Throws<PyIndexError>(() => Render("{9223372036854775807}")).Message);
        // 尾字母前即时溢出来自固定CP源码；不冒称178参考实测。
        foreach (string field in new[] { "9223372036854775808", new string('9', 100), "9223372036854775808x" })
            Error("{" + field + "}", "Too many decimal digits in format string");
    }

    [Fact]
    public void MissingRootPrecedesUnknownConversion()
    {
        Assert.Throws<PyIndexError>(() => Render("{1.a!q}", "x"));
        Assert.Throws<PyKeyError>(() => Render("{missing.a!q}", "x"));
        Assert.Throws<PyAttributeError>(() => Render("{0.missing!q}", new PyInstance(new PyClass("Empty"))));
        Assert.Throws<PyKeyError>(() => Render("{0[x]!q}", new PyDict()));
    }

    [Fact]
    public void EmptyHeadConsumesAutomaticPositionForAttributeAndItem()
    {
        var value = new PyInstance(new PyClass("Record")); PyObject.SetAttr(value, "name", "v");
        var dict = new PyDict(); dict.Set("x", "item");
        Assert.Equal("v next", Render("{.name} {}", value, "next"));
        Assert.Equal("item next", Render("{[x]} {}", dict, "next"));
    }

    [Fact]
    public void NestedSpecificationSharesNumberingAndAcceptsInnerEmptySpec()
    {
        var value = Value(args => "F<" + Assert.IsType<string>(args[1]) + ">");
        Assert.Equal("F<4>", Render("{:{}}", value, "4"));
        Assert.Equal("F<4>", Render("{0:{1:}}", value, "4"));
        Error("{0:{}}", "cannot switch from manual field specification to automatic field numbering", value, "4");
        Error("{:{0}}", "cannot switch from automatic field numbering to manual field specification", value, "4");
    }

    [Fact]
    public void DepthFailureDoesNotRunDeeperFormatButMissingInnerRootWins()
    {
        int calls = 0; var value = Value(_ => { calls++; return "v"; });
        Error("{0:{1:{2}}}", "Max string recursion exceeded", value, value, value);
        Error("{0:{1:{missing}}}", "Max string recursion exceeded", value, value);
        Assert.Throws<PyKeyError>(() => Render("{0:{missing:{2}}}", value, value, value));
        Assert.Equal(0, calls);
    }

    [Fact]
    public void EscapedSpecAndGeneratedBracesAreNeverExpandedAgain()
    {
        var value = Value(args => "F<" + Assert.IsType<string>(args[1]) + ">");
        Assert.Equal("F<{}>", Render("{0:{{}}}", value));
        Assert.Equal("F<>}", Render("{0:}}}", value));
        Assert.Equal("F<{2}>", Render("{0:{1}}", value, "{2}"));
    }

    [Fact]
    public void NoFieldExactAndNonemptySubclassPreserveIdentityButEmptySubclassDoesNot()
    {
        string exact = new(new[] { 'a', 'b', 'c' }); object sub = Text("abc"); object empty = Text("");
        Assert.Same(exact, Render(exact)); Assert.Same(sub, Render(sub));
        Assert.Same(string.Empty, Render(empty)); Assert.NotSame(empty, Render(empty));
    }

    [Fact]
    public void SingleWholeResultPreservesExactAndSubclassIdentity()
    {
        object[] results = [new string(new[] { 'v', 'a', 'l' }), Text("val")];
        foreach (object result in results) Assert.Same(result, Render("{0}", Value(_ => result)));
    }

    [Fact]
    public void NonemptyLiteralOrRepeatedResultMaterializesExactText()
    {
        object result = Text("value"); var value = Value(_ => result);
        foreach (var pair in new[] { ("x{0}", "xvalue"), ("{0}x", "valuex"), ("{0}{0}", "valuevalue"), ("{{}}{0}", "{}value") })
        {
            object actual = Render(pair.Item1, value);
            Assert.Equal(pair.Item2, Assert.IsType<string>(actual)); Assert.NotSame(result, actual);
        }
    }

    [Fact]
    public void TerminalWriterQualificationDistinguishesEmptyBeforeAndAfter()
    {
        // 以下空片组合来自固定CP writer源码推导，尚非178原流中的组合。
        object result = Text("value"); var value = Value(_ => result); var empty = Value(_ => "");
        Assert.Same(result, Render("{0}{1}", empty, value));
        Assert.Equal("value", Assert.IsType<string>(Render("{0}{1}", value, empty)));
        Assert.Same(result, Render("{0}{0}{1}", empty, value));
        Assert.Equal("value", Assert.IsType<string>(Render("{0}{1}{0}", empty, value)));
    }

    [Fact]
    public void EmptySubclassResultsNeverSurviveWriterAndEmptyExactIsCanonical()
    {
        var emptySub = Value(_ => Text(""));
        Assert.Same(string.Empty, Render("{0}", emptySub));
        Assert.Same(string.Empty, Render("{0}{0}", Value(_ => "")));
    }

    [Fact]
    public void TemplatePayloadDoesNotInvokeItsStringRepresentation()
    {
        var template = Assert.IsAssignableFrom<PyInstance>(Text("x{0}"));
        template.Class.SetAttr("__str__", Fn("__str__", _ => throw new Exception("template str")));
        template.Class.SetAttr("__repr__", Fn("__repr__", _ => throw new Exception("template repr")));
        Assert.Equal("xy", Render(template, "y"));
    }

    [Fact]
    public void CapacityGuardUsesRealClrLimitWithoutAllocatingLargeText()
    {
        var check = typeof(PyOps).Assembly.GetType("RyuPy.Runtime.PyStringTemplateWriter", true)!
            .GetMethod("CheckCapacity", BindingFlags.Static | BindingFlags.NonPublic)!;
        var ambient = new PyValueError("capacity ambient"); object? token = PyErrors.EnterHandler(ambient);
        long depth = PyTraceback.PushFrame("template_capacity.py", "caller");
        try
        {
            PyTraceback.SetLine(7);
            foreach (long size in new long[] { 0, 1_000_001, 0x3FFFFFDF })
                check.Invoke(null, [size]);
            foreach (long size in new long[] { 0x3FFFFFE0, long.MaxValue })
            {
                var wrapped = Assert.Throws<TargetInvocationException>(() => check.Invoke(null, [size]));
                var error = Assert.IsType<PyNotImplementedError>(wrapped.InnerException);
                Assert.Equal("str.format: result exceeds managed string capacity", error.Message);
                Assert.Same(ambient, error.Context); Assert.Null(error.Cause);
                Assert.Equal(new PyStackFrame("template_capacity.py", "caller", 7), error.Frames[0]);
                Assert.Same(ambient, PyErrors.Current);
            }
        }
        finally { PyTraceback.PopTo(depth); PyErrors.LeaveHandler(token); }
    }

}
