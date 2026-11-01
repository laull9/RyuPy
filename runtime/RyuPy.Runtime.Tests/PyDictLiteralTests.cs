using System.Numerics;
using RyuPy.Runtime;
using Xunit;

namespace RyuPy.Runtime.Tests;

/// <summary>动态大字典仅在实际标量键下开放；拒绝检查不得执行 Python hash/eq。</summary>
public class PyDictLiteralTests
{
    [Fact]
    public void GuardNeverInvokesUserHashOrEquality()
    {
        int calls = 0;
        var cls = new PyClass("UnsafeKey");
        cls.SetAttr("__hash__", PyNativeFunctions.Create("__hash__", ["self"], [null], 1, _ => { calls++; return 1L; }));
        cls.SetAttr("__eq__", PyNativeFunctions.Create("__eq__", ["self", "other"], [null, null], 2, _ => { calls++; return true; }));
        var target = new PyDict();
        Assert.Throws<PyNotImplementedError>(() => PyOps.DictSetScalarKey(target, new PyInstance(cls), 3L));
        Assert.Equal(0, calls);
        Assert.Equal(0, target.Count);
        PyOps.DictSetScalarKey(target, 1L, "first");
        PyOps.DictSetScalarKey(target, true, "second");
        Assert.Equal(1, target.Count);
        Assert.Equal("second", target.Get(1L));
    }

    [Fact]
    public void LargeIntegralFloatsShareIntegerHashesAndDictionarySlots()
    {
        foreach (BigInteger number in new[] { (BigInteger.One << 63) - 1024, BigInteger.One << 63, -(BigInteger.One << 64), BigInteger.One << 100 })
        {
            object integer = number >= long.MinValue && number <= long.MaxValue ? (object)(long)number : number;
            object floating = (double)number;
            Assert.True(PyHash.Equal(integer, floating));
            Assert.Equal(PyHash.Of(integer), PyHash.Of(floating));
            var dictionary = new PyDict();
            PyOps.DictSetScalarKey(dictionary, integer, "integer");
            PyOps.DictSetScalarKey(dictionary, floating, "float");
            Assert.Equal(1, dictionary.Count);
            Assert.Equal("float", dictionary.Get(integer));
        }
    }
}
