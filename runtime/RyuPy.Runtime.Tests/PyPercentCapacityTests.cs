// .NET最终字符串容量的显式宿主偏差；用纯计长与预分配拒绝测试，不制造巨量字符串。
using RyuPy.Runtime;
using Xunit;

namespace RyuPy.Runtime.Tests;

public class PyPercentCapacityTests
{
    [Fact]
    public void CapacityUsesDifferenceWithoutOverflowOrLargeAllocation()
    {
        int limit = PyPercentFormat.MaxLength;
        Assert.Equal(0x3FFFFFDF, limit);
        PyPercentFormat.CheckCapacity(0, limit - 1L);
        PyPercentFormat.CheckCapacity(0, limit);
        PyPercentFormat.CheckCapacity(limit, 0);
        Assert.Throws<PyNotImplementedError>(() => PyPercentFormat.CheckCapacity(0, limit + 1L));
        Assert.Throws<PyNotImplementedError>(() => PyPercentFormat.CheckCapacity(1, long.MaxValue));
    }

    [Fact]
    public void AllAppendKindsShareCumulativeUtf16Budget()
    {
        int limit = PyPercentFormat.MaxLength;
        // emoji literal占两个UTF16单位；随后字段、literal、%%、旧转换使用相同累计守卫。
        int prefix = "😀".Length;
        PyPercentFormat.CheckCapacity(prefix, limit - prefix);
        foreach (int appendedLength in new[] { 1, "%".Length, "text".Length, "-12".Length, "😀".Length })
        {
            PyPercentFormat.CheckCapacity(limit - appendedLength, appendedLength);
            Assert.Throws<PyNotImplementedError>(() => PyPercentFormat.CheckCapacity(limit - appendedLength + 1, appendedLength));
        }
    }

    [Fact]
    public void ImpossibleWidthRejectsAfterOneConversionBeforePadding()
    {
        int calls = 0;
        var cls = new PyClass("Number");
        cls.SetAttr("__int__", PyNativeFunctions.Create("__int__", ["self"], [null], 1,
            _ => { calls++; return 12L; }));
        var value = new PyInstance(cls);
        Assert.Contains("managed string capacity", Assert.Throws<PyNotImplementedError>(() =>
            PyPercentFormat.Format("x%9223372036854775807d", value)).Message);
        Assert.Equal(1, calls);
        var original = new PyValueError("conversion first");
        cls.SetAttr("__int__", PyNativeFunctions.Create("__int__", ["self"], [null], 1, _ => throw original));
        Assert.Same(original, Assert.Throws<PyValueError>(() =>
            PyPercentFormat.Format("x%9223372036854775807d", value)));
        Assert.Throws<PyTypeError>(() => PyPercentFormat.Format("%9223372036854775807d", new PyTuple([])));
    }
}
