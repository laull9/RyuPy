// 标量键域的bool/string碰撞回归：相同hash不是相等，精确dict和真实派生保持两个键。
using System.Numerics;
using RyuPy.Runtime;

namespace RyuPy.Runtime.Tests;

[Collection("生成器帧栈")]
public sealed class PyDictScalarCollisionTests
{
    [Theory]
    [InlineData(false, "")]
    [InlineData(true, "\u0001")]
    public void BoolAndStringComparisonDoesNotAttemptNumericConversion(bool flag, string text)
    {
        Assert.Equal(PyHash.Of(flag), PyHash.Of(text));
        Assert.False(PyHash.Equal(flag, text)); Assert.False(PyHash.Equal(text, flag));
        Assert.False(PyOps.Compare(PyOps.Eq, flag, text)); Assert.False(PyOps.Compare(PyOps.Eq, text, flag));
        Assert.True(PyOps.Compare(PyOps.Ne, flag, text)); Assert.True(PyOps.Compare(PyOps.Ne, text, flag));
        Assert.True(PyHash.Equal(flag, flag ? 1L : 0L));
        Assert.True(PyHash.Equal(flag ? 1.0 : 0.0, flag));
        Assert.True(PyHash.Equal(flag, new BigInteger(flag ? 1 : 0)));
    }

    [Theory]
    [InlineData(false, "", false)]
    [InlineData(false, "", true)]
    [InlineData(true, "\u0001", false)]
    [InlineData(true, "\u0001", true)]
    public void ExactAndDerivedDictionariesKeepCollidingKeysInEitherOrder(bool flag, string text, bool textFirst)
    {
        object first = textFirst ? text : flag, second = textFirst ? flag : text;
        var cls = (PyClass)PyOps.MakeClass("CollidingDict", [PyType.Get("dict")], null);
        PyOps.FinalizeOrdinaryClass(cls);
        foreach (object mapping in new object[] { new PyDict(), cls.Invoke([], [], [])! })
        {
            PyOps.SetItem(mapping, first, "first"); PyOps.SetItem(mapping, second, "second");
            Assert.Equal(2L, PyOps.Len(mapping));
            Assert.Equal("first", PyOps.GetItem(mapping, first));
            Assert.Equal("second", PyOps.GetItem(mapping, second));
            Assert.Equal(new object?[] { first, second }, PyOps.ToSequence(mapping));
            PyOps.DelItem(mapping, first); Assert.Equal("second", PyOps.GetItem(mapping, second));
        }
        var pairs = new PyList([new PyTuple([first, "first"]), new PyTuple([second, "second"])]);
        object constructed = cls.Invoke([pairs], [], [])!;
        Assert.Equal(2L, PyOps.Len(constructed));
        Assert.Equal("first", PyOps.GetItem(constructed, first));
        Assert.Equal("second", PyOps.GetItem(constructed, second));
    }

    [Fact]
    public void ForeignComparisonStillRunsBeforeScalarRejection()
    {
        int count = 0; var cls = new PyClass("ForeignBoolEqual");
        cls.SetAttr("__eq__", PyNativeFunctions.CreateVariadic("eq", _ => { count++; return true; }));
        var foreign = new PyInstance(cls);
        Assert.True(PyHash.Equal(false, foreign)); Assert.True(PyHash.Equal(foreign, true));
        Assert.Equal(2, count);
    }
}
