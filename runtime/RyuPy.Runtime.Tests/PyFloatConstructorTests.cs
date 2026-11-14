// float 绑定、原协议身份与严格文本边界；帧栈测试串行，不将通用展开旧债冒充已修。
using System;
using System.Numerics;
using System.Text;
using RyuPy.Runtime;
using Xunit;

namespace RyuPy.Runtime.Tests;

[Collection("生成器帧栈")]
public class PyFloatConstructorTests
{
    private static object? Float(object?[] args, object?[]? names = null, object?[]? values = null) =>
        PyOps.CallBuiltin("float", args, names ?? [], values ?? []);
    private static PyInstance Protocol(string slot, Func<object?> action)
    {
        var cls = new PyClass("FloatProtocol");
        cls.SetAttr(slot, PyNativeFunctions.Create(slot, ["self"], [null], 1, _ => action()));
        return new PyInstance(cls);
    }
    private static void Caller(Action<PyValueError> body)
    {
        long depth = PyTraceback.PushFrame("float_test.py", "caller");
        var outer = new PyValueError("outer"); object? previous = PyErrors.EnterHandler(outer);
        try { PyTraceback.SetLine(29); body(outer); }
        finally { PyErrors.LeaveHandler(previous); PyTraceback.PopTo(depth); }
    }
    private static void Prepared(PyBaseException error, PyBaseException outer)
    {
        Assert.Same(outer, error.Context);
        Assert.Equal(new PyStackFrame("float_test.py", "caller", 29), error.Frames[0]);
    }
    private static double Number(object? input) => Assert.IsType<double>(Float([input]));

    [Fact]
    public void ZeroAndPrimitiveInputsKeepValuesAndExactDoubleIdentity()
    {
        Assert.Equal(0.0, Float([])); Assert.Equal(1.0, Number(true)); Assert.Equal(-3.0, Number(-3L));
        object original = 1.25; Assert.Same(original, Float([original])); Assert.Same(original, PyOps.ToFloatValue(original));
    }
    [Fact]
    public void TooManyArgumentsRejectBeforeAnyProtocol()
    {
        int calls = 0; var value = Protocol("__float__", () => { calls++; return 1.0; });
        Assert.Throws<PyTypeError>(() => Float([value, value])); Assert.Equal(0, calls);
    }
    [Fact]
    public void KeywordErrorTakesPriorityOverArity()
    {
        var error = Assert.Throws<PyTypeError>(() => Float([1L, 2L], ["x"], [3L]));
        Assert.Contains("takes no keyword arguments", error.Message);
        Assert.Throws<PyTypeError>(() => Float([], ["x"], [1L]));
    }
    [Fact]
    public void ExpandedCallsUseStrictBinderWithoutChangingInvalidStar()
    {
        Assert.Equal(0.0, Float([PyOps.StarArg(new PyList([]))]));
        Assert.Equal(2.5, Float([PyOps.StarArg(new PyList(["2.5"]))]));
        Assert.Throws<PyTypeError>(() => Float([PyOps.StarArg(new PyList([1L, 2L]))]));
        Assert.Throws<PyTypeError>(() => Float([PyOps.StarArg(42L)]));
    }
    [Fact]
    public void FloatSlotWinsAndIndexIsOnlyFallback()
    {
        int first = 0, second = 0; var value = Protocol("__float__", () => { first++; return 2.5; });
        value.Class.SetAttr("__index__", PyNativeFunctions.Create("__index__", ["self"], [null], 1, _ => { second++; return 4L; }));
        Assert.Equal(2.5, Number(value)); Assert.Equal(1, first); Assert.Equal(0, second);
        Assert.Equal(7.0, Number(Protocol("__index__", () => 7L)));
    }
    [Fact]
    public void DisabledOrThrowingFloatNeverFallsBackAndKeepsFailure()
    {
        int fallback = 0; var value = Protocol("__index__", () => { fallback++; return 1L; });
        value.Class.SetAttr("__float__", PyNone.Instance);
        Assert.Throws<PyTypeError>(() => Number(value)); Assert.Equal(0, fallback);
        var error = new PyLookupError("protocol failure");
        Caller(_ => Assert.Same(error, Assert.Throws<PyLookupError>(() => Number(Protocol("__float__", () => throw error)))));
        Assert.Empty(error.Frames); Assert.Null(error.Context);
    }
    [Fact]
    public void ReturnedExactFloatKeepsBoxAndRunsOnce()
    {
        object result = 8.25; int calls = 0;
        Assert.Same(result, Float([Protocol("__float__", () => { calls++; return result; })])); Assert.Equal(1, calls);
    }
    [Fact]
    public void WrongProtocolResultsAreErrorsAndDeprecatedBoolIndexIsExplicit()
    {
        foreach (object result in new object[] { true, 1L, PyNone.Instance, new PyList([]) })
            Assert.Throws<PyTypeError>(() => Number(Protocol("__float__", () => result)));
        Assert.Throws<PyTypeError>(() => Number(Protocol("__index__", () => 1.0)));
        Assert.Throws<PyNotImplementedError>(() => Number(Protocol("__index__", () => true)));
    }
    [Fact]
    public void HugeIntegerAndIndexOverflowArePreparedOnlyInFloatPath()
    {
        BigInteger huge = BigInteger.One << 20000;
        Caller(outer =>
        {
            Prepared(Assert.Throws<PyOverflowError>(() => Number(huge)), outer);
            Prepared(Assert.Throws<PyOverflowError>(() => Number(Protocol("__index__", () => huge))), outer);
        });
        Assert.Equal(9007199254740992.0, Number(new BigInteger(9007199254740993L)));
    }
    [Fact]
    public void ActualTextKindsShareAsciiGrammarAndMutableBytesAreLive()
    {
        Assert.Equal(125.0, Number("  +1_2.5e1\t"));
        Assert.Equal(125.0, Number(new PyBytes(Encoding.ASCII.GetBytes("+1_2.5e1"))));
        var bytes = new PyByteArray([49, 46, 53]); Assert.Equal(1.5, Number(bytes));
        bytes.Bytes[0] = 50; Assert.Equal(2.5, Number(bytes));
        foreach (string text in new[] { "1.", ".1", "1.e2" }) Assert.IsType<double>(Float([text]));
    }
    [Fact]
    public void UnderscoresAndOtherInvalidGrammarAreRejectedExactly()
    {
        foreach (string text in new[] { "", " ", "_1", "1_", "1__2", "1_.0", "1._0", "1e_2", "1e+_2", "1 2", "1,000", "0x1p0", "nan(x)", "1\0", "\ud800" })
            Assert.Throws<PyValueError>(() => Number(text));
        Assert.Equal(123.4, Number("1_2.3_4e+0_1"));
    }
    [Fact]
    public void UnicodeDigitsAndSpaceUseFixedRulesButBytesStayAscii()
    {
        Assert.Equal(1250.0, Number("١٢.٥e２")); Assert.Equal(12.5, Number("\u200312.5\u00a0"));
        Assert.Equal(1.5, Number("\U0001d7cf.5"));
        Assert.Throws<PyValueError>(() => Number("\u001c1"));
        Assert.Throws<PyValueError>(() => Number(new PyBytes([0xa0, 49])));
    }
    [Fact]
    public void SpecialValuesPreserveSignAndOnlyAcceptFullTokens()
    {
        Assert.Equal(double.PositiveInfinity, Number("+InFiNiTy")); Assert.Equal(double.NegativeInfinity, Number("-inf"));
        Assert.True(double.IsNaN(Number("NaN")));
        Assert.True(BitConverter.DoubleToInt64Bits(Number("-NaN")) < 0);
        Assert.True(BitConverter.DoubleToInt64Bits(Number("NaN")) >= 0);
        Assert.Equal(long.MinValue, BitConverter.DoubleToInt64Bits(Number("-0.0")));
    }
    [Fact]
    public void RoundingAndExponentBoundaryValuesMatchReferenceBits()
    {
        Assert.Equal(9007199254740992.0, Number("9007199254740993"));
        Assert.Equal(double.MaxValue, Number("1.7976931348623157e308"));
        Assert.Equal(double.PositiveInfinity, Number("1.7976931348623159e308"));
        Assert.Equal(1L, BitConverter.DoubleToInt64Bits(Number("4.9406564584124654e-324")));
        Assert.Equal(0L, BitConverter.DoubleToInt64Bits(Number("2.4703282292062327e-324")));
    }
    [Fact]
    public void LongDigitsAndExponentsHaveNoIntegerDigitLimitOrGuessedSaturation()
    {
        Assert.Equal(1.0, Number(new string('0', 5000) + "1"));
        Assert.Equal(double.PositiveInfinity, Number(new string('1', 5000)));
        string exponent = new string('9', 40);
        Assert.Equal(double.PositiveInfinity, Number("1e" + exponent));
        Assert.Equal(long.MinValue, BitConverter.DoubleToInt64Bits(Number("-1e-" + exponent)));
        Assert.Equal(0.0, Number("0e" + exponent));
        Assert.Equal(long.MinValue, BitConverter.DoubleToInt64Bits(Number("-0e" + exponent)));
        Assert.Equal(1.0, Number("0." + new string('0', 5000) + "1e5001"));
    }
    [Fact]
    public void ColdOwnErrorsPrepareCallerAndProtocolBodyFramesRemainUntouched()
    {
        Caller(outer =>
        {
            Prepared(Assert.Throws<PyTypeError>(() => Float([1L, 2L])), outer);
            Prepared(Assert.Throws<PyTypeError>(() => Number(PyNone.Instance)), outer);
            Prepared(Assert.Throws<PyValueError>(() => Number("bad")), outer);
            Prepared(Assert.Throws<PyTypeError>(() => Number(Protocol("__float__", () => 1L))), outer);
        });
        var view = PyMemoryView.Create([new PyBytes([49, 46, 53])], [], []);
        Assert.Throws<PyNotImplementedError>(() => Number(view));
        var error = new PyValueError("already prepared");
        Caller(_ => PyErrors.PrepareRaise(error)); var frames = error.Frames; var context = error.Context;
        Assert.Same(error, Assert.Throws<PyValueError>(() => Number(Protocol("__float__", () => throw error))));
        Assert.Same(frames, error.Frames); Assert.Same(context, error.Context);
    }
}
