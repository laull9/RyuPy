// 整数富比较入口的边界与单例结果；混合数值、NaN和非法操作码保留既有语义。
using System.Numerics;
using RyuPy.Runtime;
namespace RyuPy.Runtime.Tests;

public class PyIntegerRichCompareTests
{
    [Fact]
    public void AllIntegerRelationsMatchBooleanAbiAtBoundaries()
    {
        long[] values = [long.MinValue, -1, 0, 1, long.MaxValue];
        int[] ops = [PyOps.Eq, PyOps.Ne, PyOps.Lt, PyOps.Le, PyOps.Gt, PyOps.Ge];
        foreach (long a in values)
            foreach (long b in values)
                foreach (int op in ops)
                    Assert.Same(PyOps.BoxBool(PyOps.Compare(op, a, b)), PyOps.CompareValue(op, a, b));
    }
    [Fact]
    public void MixedTypesAndNaNStillFollowOriginalComparison()
    {
        Assert.Same(PyOps.TrueObject, PyOps.CompareValue(PyOps.Eq, 1L, true));
        Assert.Same(PyOps.TrueObject, PyOps.CompareValue(PyOps.Lt, long.MaxValue, (BigInteger)long.MaxValue + 1));
        Assert.Same(PyOps.FalseObject, PyOps.CompareValue(PyOps.Le, 0L, double.NaN));
        Assert.Same(PyOps.TrueObject, PyOps.CompareValue(PyOps.Lt, 1L, 1.5));
        Assert.Throws<PyRuntimeError>(() => PyOps.CompareValue(9999, 0L, 1L));
    }
}
