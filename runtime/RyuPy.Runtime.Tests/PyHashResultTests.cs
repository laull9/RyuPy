// 用户 hash 返回归约及其必要整数值一致性闭包；期望取自唯一 CPython 3.12.13 参考原流。
// 普通/通用入口同测，真实 int 子类与空表有效 hash 调用次数仍不在本切。
using System;
using System.Linq;
using System.Numerics;
using Xunit;

namespace RyuPy.Runtime.Tests;

[Collection("生成器帧栈")]
public sealed class PyHashResultTests
{
    private const long P = (1L << 61) - 1;
    private static readonly BigInteger Wide = BigInteger.One << 100;
    private static PyFunction Fn(string name, string[] parameters, Func<object?[], object?> body) =>
        new(name, PySignature.Create(name, parameters, parameters.Select(_ => "pos").ToArray(),
            parameters.Select(_ => "0").ToArray()), (_, args, _, _) => body(args), []);
    private static PyInstance Hasher(bool ordinary, Func<object?> body, object? equal = null)
    {
        var cls = ordinary ? (PyClass)PyOps.MakeClass("HashResult", [], null) : new PyClass("ManualHashResult");
        cls.SetAttr("__hash__", Fn("__hash__", ["self"], _ => body()));
        if (equal is not null)
            cls.SetAttr("__eq__", Fn("__eq__", ["self", "other"], args => PyHash.Equal(equal, args[1])));
        if (ordinary) PyOps.FinalizeOrdinaryClass(cls);
        return new PyInstance(cls);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void UserIntegerResultsKeepI64ValuesAndReduceOnlyOverflow(bool ordinary)
    {
        (object Value, long Hash)[] cases = [
            (false, 0), (true, 1), (-1, -2), (-1L, -2), (new BigInteger(-1), -2),
            (0L, 0), (17L, 17), (P - 1, P - 1), (P, P), (P + 1, P + 1),
            (-P + 1, -P + 1), (-P, -P), (-P - 1, -P - 1),
            (long.MinValue, long.MinValue), (long.MinValue + 1, long.MinValue + 1),
            (long.MaxValue - 1, long.MaxValue - 1), (long.MaxValue, long.MaxValue),
            (new BigInteger(P), P), (new BigInteger(long.MaxValue), long.MaxValue),
            (new BigInteger(long.MinValue), long.MinValue),
            (new BigInteger(long.MinValue) - 1, -5), (new BigInteger(long.MaxValue) + 1, 4),
            (Wide, 549755813888), (-Wide, -549755813888),
            (new BigInteger(P) * 8 + 1, 1), (-(new BigInteger(P) * 8 + 1), -2),
            (new BigInteger(P) * 8, 0), (-new BigInteger(P) * 8, 0)];
        foreach (var item in cases)
        {
            object returned = item.Value; int calls = 0;
            var key = Hasher(ordinary, () => { calls++; return returned; });
            PyObject.SetAttr(key, "kept", returned);
            Assert.Equal(item.Hash, PyHash.Of(key));
            Assert.Equal(1, calls); Assert.Same(item.Value, returned);
            Assert.Same(returned, PyObject.GetAttr(key, "kept"));
        }
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void InvalidResultsDoNotInvokeProtocolsAndPrepareOnlyTheirOwnError(bool ordinary)
    {
        int protocolCalls = 0;
        var cls = (PyClass)PyOps.MakeClass("IntegerPretender", [], null);
        foreach (string name in new[] { "__index__", "__int__", "__float__", "__hash__", "__repr__" })
            cls.SetAttr(name, Fn(name, ["self"], _ => { protocolCalls++; return 1L; }));
        PyOps.FinalizeOrdinaryClass(cls);
        object?[] invalid = [null, PyNone.Instance, 1.0, 1.5, "1", new PyInstance(cls), new object(), new PyList()];
        foreach (object? returned in invalid)
        {
            int calls = 0; var key = Hasher(ordinary, () => { calls++; return returned; });
            WithCaller(outer =>
            {
                var error = Assert.Throws<PyTypeError>(() => PyHash.Of(key));
                Assert.Equal("__hash__ method should return an integer", error.Message);
                Assert.Same(outer, error.Context); Assert.Single(error.Frames);
                Assert.Equal(new PyStackFrame("hash_result.py", "caller", 23), error.Frames[0]);
            });
            Assert.Equal(1, calls); Assert.Equal(0, protocolCalls);
        }
    }

    private static void WithCaller(Action<PyValueError> body)
    {
        long depth = PyTraceback.PushFrame("hash_result.py", "caller");
        var outer = new PyValueError("outer"); object? previous = PyErrors.EnterHandler(outer);
        try { PyTraceback.SetLine(23); body(outer); }
        finally { PyErrors.LeaveHandler(previous); PyTraceback.PopTo(depth); }
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void UserExceptionIdentityContextAndFramesAreNotWrapped(bool ordinary)
    {
        var failure = new PyValueError("user failure"); WithCaller(_ => PyErrors.PrepareRaise(failure));
        var frames = failure.Frames; var snapshot = frames.ToArray(); var context = failure.Context; int calls = 0;
        var key = Hasher(ordinary, () => { calls++; throw failure; });
        Assert.Same(failure, Assert.Throws<PyValueError>(() => PyHash.Of(key)));
        Assert.Same(frames, failure.Frames); Assert.Equal(snapshot, failure.Frames);
        Assert.Same(context, failure.Context); Assert.Equal(1, calls);
    }

    [Fact]
    public void IntegerValuesHaveFixedOracleAcrossLongAndBigIntegerRepresentations()
    {
        (long Value, long Hash)[] cases = [(-1, -2), (0, 0), (1, 1), (P - 1, P - 1),
            (P, 0), (P + 1, 1), (-P + 1, -P + 1), (-P, 0), (-P - 1, -2),
            (long.MinValue, -4), (long.MinValue + 1, -3), (long.MaxValue - 1, 2), (long.MaxValue, 3)];
        foreach (var item in cases)
        {
            Assert.Equal(item.Hash, PyHash.Of(item.Value));
            Assert.Equal(item.Hash, PyHash.Of(new BigInteger(item.Value)));
            if (item.Value >= int.MinValue && item.Value <= int.MaxValue)
                Assert.Equal(item.Hash, PyHash.Of((int)item.Value));
        }
        Assert.Equal(-5L, PyHash.Of(new BigInteger(long.MinValue) - 1));
        Assert.Equal(4L, PyHash.Of(new BigInteger(long.MaxValue) + 1));
        Assert.Equal(549755813888L, PyHash.Of(Wide)); Assert.Equal(-549755813888L, PyHash.Of(-Wide));
        Assert.Equal(-2L, PyHash.Of(-(new BigInteger(P) * 8 + 1)));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void EqualKeysFindEachOtherAcrossRepresentationsInBothDirections(bool ordinary)
    {
        (object[] Values, long Hash)[] cases = [
            ([true, 1, 1L, BigInteger.One, 1.0], 1),
            ([-1, -1L, new BigInteger(-1), -1.0], -2),
            ([P, new BigInteger(P)], 0), ([-P, new BigInteger(-P)], 0),
            ([long.MaxValue, new BigInteger(long.MaxValue)], 3),
            ([long.MinValue, new BigInteger(long.MinValue), -9223372036854775808.0], -4),
            ([1L << 61, new BigInteger(1L << 61), 2305843009213693952.0], 1),
            ([-(1L << 61), new BigInteger(-(1L << 61)), -2305843009213693952.0], -2),
            ([new BigInteger(long.MaxValue) + 1, 9223372036854775808.0], 4),
            ([Wide, (double)Wide], 549755813888), ([-Wide, -(double)Wide], -549755813888)];
        foreach (var item in cases)
        {
            var key = Hasher(ordinary, () => item.Hash, item.Values[0]);
            object[] values = [.. item.Values, key];
            foreach (object left in values)
            foreach (object right in values)
            {
                Assert.True(PyHash.Equal(left, right));
                // 直接比较64位值，避免CLR comparer折叠的偶发碰撞掩盖错误。
                Assert.Equal(item.Hash, PyHash.Of(left)); Assert.Equal(item.Hash, PyHash.Of(right));
                var dict = new PyDict(); dict.Set(left, "kept");
                Assert.Equal("kept", dict.Get(right)); dict.Set(right, "updated");
                Assert.Equal(1, dict.Count); Assert.Equal("updated", dict.Get(left));
                var set = new PySet([left]); Assert.True(set.Contains(right));
                set.Add(right); Assert.Equal(1, set.Count);
            }
        }
        Assert.False(PyHash.Equal(long.MaxValue, (double)long.MaxValue));
        Assert.False(PyHash.Equal(P, (double)P));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void InvalidHashDoesNotInsertOrDeleteAndCallsOnlyOnce(bool ordinary)
    {
        int calls = 0; var key = Hasher(ordinary, () => { calls++; return 1.5; });
        var dict = new PyDict(); dict.Set("kept", 7L); long version = dict.Version;
        var set = new PySet(["kept"]);
        Action[] operations = [() => dict.Set(key, 9L), () => dict.Get(key), () => dict.Delete(key),
            () => set.Add(key), () => set.Contains(key), () => set.Remove(key)];
        foreach (var operation in operations)
        {
            calls = 0; Assert.Throws<PyTypeError>(operation); Assert.Equal(1, calls);
            Assert.Equal(1, dict.Count); Assert.Equal(version, dict.Version); Assert.Equal(7L, dict.Get("kept"));
            Assert.Equal(1, set.Count); Assert.True(set.Contains("kept"));
        }
    }
}
