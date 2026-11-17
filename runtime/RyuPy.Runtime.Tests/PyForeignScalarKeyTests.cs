// foreign精确标量保持原键身份、统一内部hash域、保留码点证据；mock不实现转换协议，防止隐式用户调用。
using System;
using System.Linq;
using System.Numerics;
using RyuPy.Runtime;
using Xunit;

namespace RyuPy.Runtime.Tests;

public class PyForeignScalarKeyTests
{
    private sealed class Scalar(object value, string? points = null) : IPyForeignExactScalar
    {
        internal bool Alive = true;
        internal int Copies;
        public bool TryReadExactScalar(out object? scalar, out string? unicodeCodePoints, bool materialize = true)
        {
            if (!Alive) throw new ObjectDisposedException("foreign scalar");
            if (materialize) Copies++;
            scalar = materialize ? value : null;
            unicodeCodePoints = points;
            return true;
        }
    }

    [Fact]
    public void NumericAliasesUseOneHashDomainAndPreserveFirstKey()
    {
        object huge = BigInteger.One << 20000;
        foreach (object value in new object[] { 1L, true, 1.0, -0.0, huge, PyNone.Instance })
        {
            var foreign = new Scalar(value);
            object local = value is bool ? 1L : value;
            var left = new PyDict(); left.Set(foreign, 1L); left.Set(local, 2L);
            Assert.Equal(1, left.Count); Assert.Same(foreign, left.Keys.First()); Assert.Equal(2L, left.Get(local));
            var right = new PyDict(); right.Set(local, 1L); right.Set(foreign, 2L);
            Assert.Equal(1, right.Count); Assert.Same(local, right.Keys.First()); Assert.Equal(2L, right.Get(foreign));
            Assert.Equal(1, foreign.Copies);
        }
    }

    [Fact]
    public void UnicodeHashCollisionsRetainActualCodePointEquality()
    {
        const string text = "\ud83d\ude00";
        var scalar = new Scalar(text, "\u0001\uf600");
        var pair = new Scalar(text, "\u0000\ud83d\u0000\ude00");
        var lone = new Scalar("\ud800x", "\u0000\ud800\u0000x");
        var dict = new PyDict();
        dict.Set(scalar, 1L); dict.Set(pair, 2L); dict.Set(text, 3L);
        Assert.Equal(2, dict.Count); Assert.Same(scalar, dict.Keys.First());
        Assert.Equal(3L, dict.Get(scalar)); Assert.Equal(2L, dict.Get(pair));
        var reverse = new PyDict(); reverse.Set(text, 1L); reverse.Set(scalar, 2L); reverse.Set(pair, 3L);
        Assert.Equal(2, reverse.Count); Assert.Same(text, reverse.Keys.First());
        dict.Set(lone, 4L); dict.Set("\ud800x", 5L);
        Assert.Equal(3, dict.Count); Assert.Equal(5L, dict.Get(lone));
    }

    [Fact]
    public void NaNIdentityAndCachedReaderLivenessRemainObservable()
    {
        var first = new Scalar(double.NaN); var second = new Scalar(double.NaN);
        var dict = new PyDict(); dict.Set(first, 1L); dict.Set(first, 2L); dict.Set(second, 3L);
        Assert.Equal(2, dict.Count); Assert.Equal(2L, dict.Get(first)); Assert.Equal(3L, dict.Get(second));
        first.Alive = false;
        Assert.Throws<ObjectDisposedException>(() => dict.Get(first));
    }
}
