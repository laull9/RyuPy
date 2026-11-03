// int 构造器的真实绑定、文本语法和整数协议；只经公开 CallBuiltin/ToIntValue，不依赖私有解析状态。
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using RyuPy.Runtime;
using Xunit;

namespace RyuPy.Runtime.Tests;

[Collection("生成器帧栈")]
public class PyIntConstructorTests
{
    private static object? Int(object?[] args, string[]? names = null, object?[]? values = null) =>
        PyOps.CallBuiltin("int", args, names ?? [], values ?? []);
    private static PyInstance Protocol(string name, Func<object?> action)
    {
        var cls = new PyClass("IntegerProtocol");
        cls.SetAttr(name, PyNativeFunctions.Create(name, ["self"], [null], 1, _ => action()));
        return new PyInstance(cls);
    }

    [Fact]
    public void ZeroAndExistingNumericSingleArgumentRemainNumeric()
    {
        Assert.Equal(0L, Int([])); Assert.Equal(1L, Int([true])); Assert.Equal(-3L, Int([-3.9]));
        Assert.Equal(17L, PyOps.ToIntValue(17L));
        BigInteger big = BigInteger.One << 100;
        Assert.Equal(big, Int([big]));
    }

    [Fact]
    public void PositionalAndKeywordBaseUseTheSameConstructor()
    {
        Assert.Equal(16L, Int(["10", 16L]));
        Assert.Equal(16L, Int(["10"], ["base"], [16L]));
        Assert.Equal(10L, Int(["10"]));
    }

    [Fact]
    public void BindingErrorsNeverEvaluateBaseProtocol()
    {
        int calls = 0; var radix = Protocol("__index__", () => { calls++; return 16L; });
        Assert.Throws<PyTypeError>(() => Int([], ["base"], [radix]));
        Assert.Throws<PyTypeError>(() => Int([], ["x"], ["10"]));
        Assert.Throws<PyTypeError>(() => Int(["10", radix], ["base"], [8L]));
        Assert.Throws<PyTypeError>(() => Int(["10", radix, 2L]));
        Assert.Throws<PyTypeError>(() => Int(["10"], ["other"], [radix]));
        Assert.Equal(0, calls);
    }

    [Fact]
    public void BaseIndexRunsOnceBeforeTextValidationAndItsErrorIsPreserved()
    {
        int calls = 0; var radix = Protocol("__index__", () => { calls++; return 16L; });
        Assert.Throws<PyTypeError>(() => Int([42L, radix])); Assert.Equal(1, calls);
        Assert.Throws<PyValueError>(() => Int(["bad!", radix])); Assert.Equal(2, calls);
        var failure = new PyLookupError("base failed");
        var failing = Protocol("__index__", () => throw failure);
        Assert.Same(failure, Assert.Throws<PyLookupError>(() => Int([42L, failing])));
    }

    [Fact]
    public void BaseRangeUsesFullIntegerAndDistinguishesOmittedFromNone()
    {
        foreach (object radix in new object[] { -1L, 1L, 37L, BigInteger.One << 100, true })
            Assert.Throws<PyValueError>(() => Int(["10", radix]));
        Assert.Equal(16L, Int(["0x10", false]));
        Assert.Throws<PyTypeError>(() => Int(["10", PyNone.Instance]));
        Assert.Throws<PyTypeError>(() => Int(["10", 10.0]));
    }

    [Fact]
    public void MutableTextIsReadOnlyAfterBaseCallback()
    {
        var bytes = new PyByteArray([49, 48]); int calls = 0;
        var radix = Protocol("__index__", () => { calls++; bytes.Bytes[0] = 102; bytes.Bytes[1] = 102; return 16L; });
        Assert.Equal(255L, Int([bytes, radix])); Assert.Equal(1, calls);
    }

    [Fact]
    public void ActualStringBytesAndByteArrayShareAsciiGrammar()
    {
        foreach (object value in new object[] { " -f_f ", new PyBytes(Encoding.ASCII.GetBytes(" -f_f ")), new PyByteArray(Encoding.ASCII.GetBytes(" -f_f ")) })
            Assert.Equal(-255L, Int([value, 16L]));
        Assert.Equal(123L, PyOps.ToIntValue(new PyBytes([49, 50, 51])));
        Assert.Equal(123L, PyOps.ToIntValue(new PyByteArray([49, 50, 51])));
    }

    [Fact]
    public void PrefixesAndSingleUnderscoresAreAcceptedAtExactLegalPositions()
    {
        Assert.Equal(2L, Int(["0b_10", 0L])); Assert.Equal(8L, Int(["0o_10", 0L]));
        Assert.Equal(255L, Int(["+0x_fF", 16L])); Assert.Equal(10L, Int(["  +1_0\t", 10L]));
        Assert.Equal(16L, Int(["０x10", 0L]));
    }

    [Fact]
    public void BadGrammarIsRejectedBySingleAndExplicitDecimalPaths()
    {
        foreach (string text in new[] { "", "_1", "1_", "1__2", "+_1", "- 1", "1 2", "0x10", "12\0", "\ud800", "\u001c12" })
        {
            Assert.Throws<PyValueError>(() => PyOps.ToIntValue(text));
            Assert.Throws<PyValueError>(() => Int([text, 10L]));
        }
    }

    [Fact]
    public void AutomaticBaseRejectsNonzeroLeadingZeroButKeepsZeroForms()
    {
        foreach (string text in new[] { "010", "0_1", "+00_1" }) Assert.Throws<PyValueError>(() => Int([text, 0L]));
        foreach (string text in new[] { "00", "0_0", "-０_０" }) Assert.Equal(0L, Int([text, 0L]));
        Assert.Equal(10L, Int(["010", 10L]));
    }

    [Fact]
    public void UnicodeDecimalDigitsAndWhitespaceDoNotWidenByteGrammar()
    {
        Assert.Equal(12L, Int(["\u0085１２\u2007"])); Assert.Equal(12L, Int(["١٢", 10L]));
        Assert.Equal(12L, Int(["𝟙𝟚", 10L]));
        Assert.Throws<PyValueError>(() => Int(["Ａ", 36L]));
        Assert.Throws<PyValueError>(() => Int([new PyBytes([0x85, 49, 50])]));
        Assert.Throws<PyValueError>(() => Int([new PyBytes([0xd9, 0xa1])]));
    }

    [Fact]
    public void LargeIntegerParsingKeepsExactValueAndSmallResultsNormalize()
    {
        Assert.Equal(BigInteger.One << 100, Int(["1" + new string('0', 100), 2L]));
        Assert.Equal(BigInteger.One << 100, Int(["1" + new string('0', 25), 16L]));
        Assert.Equal(BigInteger.One << 100, Int(["1" + new string('0', 50), 4L]));
        Assert.Equal(BigInteger.One << 100, Int(["2" + new string('0', 33), 8L]));
        Assert.Equal(BigInteger.One << 100, Int(["1" + new string('0', 20), 32L]));
        Assert.Equal(1295L, Int(["zz", 36L])); Assert.Equal(-128L, Int(["-10000000", 2L]));
    }

    [Fact]
    public void IntSlotTakesPriorityAndIndexIsTheFallback()
    {
        var calls = new List<string>();
        var both = Protocol("__int__", () => { calls.Add("int"); return 7L; });
        both.Class.SetAttr("__index__", PyNativeFunctions.Create("__index__", ["self"], [null], 1, _ => { calls.Add("index"); return 8L; }));
        Assert.Equal(7L, Int([both])); Assert.Equal(new[] { "int" }, calls);
        Assert.Equal(9L, Int([Protocol("__index__", () => 9L)]));
        Assert.Throws<PyTypeError>(() => Int([both, 10L])); Assert.Single(calls);
    }

    [Fact]
    public void ProtocolResultTypesAndRaisedExceptionIdentityAreChecked()
    {
        Assert.Throws<PyTypeError>(() => Int([Protocol("__int__", () => "7")]));
        Assert.Throws<PyTypeError>(() => Int([Protocol("__index__", () => 7.0)]));
        Assert.Throws<PyTypeError>(() => Int(["10", Protocol("__index__", () => "16")]));
        var error = new PyRuntimeError("conversion");
        Assert.Same(error, Assert.Throws<PyRuntimeError>(() => Int([Protocol("__int__", () => throw error)])));
    }

    [Fact]
    public void DeprecatedProtocolPathsAreExplicitUntilWarningsAreImplemented()
    {
        Assert.Throws<PyNotImplementedError>(() => Int([Protocol("__int__", () => true)]));
        Assert.Throws<PyNotImplementedError>(() => Int(["10", Protocol("__index__", () => false)]));
        int calls = 0;
        Assert.Throws<PyNotImplementedError>(() => Int([Protocol("__trunc__", () => { calls++; return 4L; })]));
        Assert.Equal(0, calls);
    }

    [Fact]
    public void DefaultDigitLimitDoesNotApplyAnArtificialPowerOfTwoBitCap()
    {
        Assert.IsType<BigInteger>(Int([new string('1', 4300), 10L]));
        Assert.Throws<PyValueError>(() => Int([new string('1', 4301), 10L]));
        Assert.IsType<BigInteger>(Int([new string('1', 4301), 2L]));
        Assert.Equal(1_000_001L, Assert.IsType<BigInteger>(Int(["1" + new string('0', 1_000_000), 2L])).GetBitLength());
        Assert.Equal(0L, Int([new string('0', 1_000_001), 2L]));
    }
}
