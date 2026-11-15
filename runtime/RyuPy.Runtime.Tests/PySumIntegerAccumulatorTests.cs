// 验证内建 sum 原生整数累加器优化：
// 快路径原生 long 累加、避免中间装箱、小整数规范化、溢出与回落、严格交错与早停保证。
using System;
using System.Collections.Generic;
using System.Numerics;
using RyuPy.Runtime;
using Xunit;

namespace RyuPy.Runtime.Tests;

public class PySumIntegerAccumulatorTests
{
    private sealed class TrackingIterator : IPyIterator
    {
        private readonly List<string> _events;
        private readonly object?[] _items;
        private int _index;
        private readonly int _failAtMoveNext;
        private readonly int _failAtCurrent;

        public TrackingIterator(
            List<string> events,
            object?[] items,
            int failAtMoveNext = -1,
            int failAtCurrent = -1
        )
        {
            _events = events;
            _items = items;
            _failAtMoveNext = failAtMoveNext;
            _failAtCurrent = failAtCurrent;
        }

        public bool MoveNext()
        {
            _events.Add($"MoveNext:{_index}");
            if (_failAtMoveNext >= 0 && _index == _failAtMoveNext)
            {
                throw new PyRuntimeError($"iterator failed at MoveNext:{_index}");
            }
            if (_index >= _items.Length)
            {
                return false;
            }
            _index++;
            return true;
        }

        public object? Current()
        {
            int currentIdx = _index - 1;
            _events.Add($"Current:{currentIdx}");
            if (_failAtCurrent >= 0 && currentIdx == _failAtCurrent)
            {
                throw new PyRuntimeError($"iterator failed at Current:{currentIdx}");
            }
            return _items[currentIdx];
        }
    }

    [Fact]
    public void EmptyIterablePreservesStartIdentityAndCanonicalZero()
    {
        var customStart = new PyList([100L]);
        object? positional = PyOps.CallBuiltin("sum", [new PyList([]), customStart], [], []);
        Assert.Same(customStart, positional);

        object? keyword = PyOps.CallBuiltin("sum", [new PyList([])], ["start"], [customStart]);
        Assert.Same(customStart, keyword);

        // 验证 shared long start 在空输入时原样返回
        object sharedLong = 999999L;
        object? resShared = PyOps.CallBuiltin("sum", [new PyList([]), sharedLong], [], []);
        Assert.Same(sharedLong, resShared);

        // 默认 start 为规范小整数 0L
        object? defaultStart = PyOps.CallBuiltin("sum", [new PyList([])], [], []);
        Assert.Equal(0L, defaultStart);
        Assert.Same(PyInt.Box(0L), defaultStart);
    }

    [Fact]
    public void SmallIntCanonicalAndNegativeCancellation()
    {
        // 累加结果落在小整数区间 [-5, 256] 时必须返回小整数缓存单例
        object? sum10 = PyOps.CallBuiltin("sum", [new PyList([3L, 7L])], [], []);
        Assert.Equal(10L, sum10);
        Assert.Same(PyInt.Box(10L), sum10);

        // 负数抵消回到 0
        object? sumZero = PyOps.CallBuiltin("sum", [new PyList([1000L, -1000L])], [], []);
        Assert.Equal(0L, sumZero);
        Assert.Same(PyInt.Box(0L), sumZero);

        // 下界 -5L
        object? sumMin = PyOps.CallBuiltin("sum", [new PyList([-2L, -3L])], [], []);
        Assert.Equal(-5L, sumMin);
        Assert.Same(PyInt.Box(-5L), sumMin);

        // 上界 256L
        object? sumMax = PyOps.CallBuiltin("sum", [new PyList([250L, 6L])], [], []);
        Assert.Equal(256L, sumMax);
        Assert.Same(PyInt.Box(256L), sumMax);
    }

    [Fact]
    public void GreaterThan256IntegersAccumulateAccurately()
    {
        object? result = PyOps.CallBuiltin("sum", [new PyList([200L, 300L, 500L])], [], []);
        Assert.Equal(1000L, result);
        Assert.IsType<long>(result);
    }

    [Fact]
    public void OverflowToBigIntegerAndFallBackToLong()
    {
        // 正向溢出升级 BigInteger
        object? posOverflow = PyOps.CallBuiltin("sum", [new PyList([long.MaxValue, 1L])], [], []);
        Assert.IsType<BigInteger>(posOverflow);
        Assert.Equal((BigInteger)long.MaxValue + 1, (BigInteger)posOverflow);

        // 负向溢出升级 BigInteger
        object? negOverflow = PyOps.CallBuiltin("sum", [new PyList([long.MinValue, -1L])], [], []);
        Assert.IsType<BigInteger>(negOverflow);
        Assert.Equal((BigInteger)long.MinValue - 1, (BigInteger)negOverflow);

        // 溢出后加负数回落到 long，且能继续进入快路
        object? fallback = PyOps.CallBuiltin("sum", [new PyList([long.MaxValue, 5L, -5L, -10L])], [], []);
        Assert.IsType<long>(fallback);
        Assert.Equal(long.MaxValue - 10L, fallback);
        object? negativeFallback = PyOps.CallBuiltin("sum", [new PyList([long.MinValue, -5L, 5L, 10L])], [], []);
        Assert.Equal(long.MinValue + 10L, Assert.IsType<long>(negativeFallback));
    }

    [Fact]
    public void MixedTypesFloatAndBoolRetainProtocols()
    {
        // bool 不隐式压入快路，通过通用二元加法处理
        object? sumBool = PyOps.CallBuiltin("sum", [new PyList([10L, true, 20L])], [], []);
        Assert.Equal(31L, sumBool);

        // float 走通用二元加法
        object? sumFloat = PyOps.CallBuiltin("sum", [new PyList([10L, 2.5, 20L])], [], []);
        Assert.Equal(32.5, sumFloat);
    }

    [Fact]
    public void UserCallbacksNeverInvokeIAddAndSupportBothProtocols()
    {
        int addCalls = 0;
        int iaddCalls = 0;

        var startClass = new PyClass("CustomStartNoIAdd");
        startClass.SetAttr("__add__", PyNativeFunctions.Create("__add__", ["self", "other"], [null, null], 2, a =>
        {
            addCalls++;
            return PyOps.Binary(PyOps.Add, 50L, a[1]!);
        }));
        startClass.SetAttr("__iadd__", PyNativeFunctions.Create("__iadd__", ["self", "other"], [null, null], 2, a =>
        {
            iaddCalls++;
            return a[0];
        }));

        var startInstance = new PyInstance(startClass);
        object? res = PyOps.CallBuiltin("sum", [new PyList([5L]), startInstance], [], []);

        Assert.Equal(55L, res);
        Assert.Equal(1, addCalls);
        Assert.Equal(0, iaddCalls);
    }

    [Fact]
    public void UserCallbackReturningSharedLongPreservesIdentity()
    {
        object sharedLong = 888888L;

        var cls = new PyClass("SharedLongReturner");
        cls.SetAttr("__radd__", PyNativeFunctions.Create("__radd__", ["self", "other"], [null, null], 2, _ => sharedLong));
        var item = new PyInstance(cls);

        // 用户协议返回 sharedLong 后无新快路加法，必须原样返回该对象引用
        object? result = PyOps.CallBuiltin("sum", [new PyList([item])], [], []);
        Assert.Same(sharedLong, result);
        object? afterNativeStreak = PyOps.CallBuiltin("sum", [new PyList([10L, 20L, item])], [], []);
        Assert.Same(sharedLong, afterNativeStreak);
    }

    [Fact]
    public void LongNativeStreakPassesExactTotalToSubsequentCustomItem()
    {
        object? observedTotal = null;

        var cls = new PyClass("ObserverItem");
        cls.SetAttr("__radd__", PyNativeFunctions.Create("__radd__", ["self", "other"], [null, null], 2, a =>
        {
            observedTotal = a[1];
            return PyOps.Binary(PyOps.Add, a[1]!, 5L);
        }));
        var observer = new PyInstance(cls);

        object? res = PyOps.CallBuiltin("sum", [new PyList([100L, 200L, 300L, observer])], [], []);
        Assert.Equal(605L, res);
        Assert.NotNull(observedTotal);
        Assert.Equal(600L, observedTotal);
        Assert.IsType<long>(observedTotal);
    }

    [Fact]
    public void StrictlyInterleavedAndEarlyStopOnException()
    {
        var events = new List<string>();

        var cls = new PyClass("ThrowingAdd");
        cls.SetAttr("__radd__", PyNativeFunctions.Create("__radd__", ["self", "other"], [null, null], 2, _ =>
            throw new PyValueError("boom during add")));
        var throwingItem = new PyInstance(cls);

        var iter = new TrackingIterator(events, [10L, 20L, throwingItem, 40L]);
        var err = Assert.Throws<PyValueError>(() => PyOps.CallBuiltin("sum", [iter], [], []));
        Assert.Equal("boom during add", err.Message);

        // 验证抛出异常后第四项 (40L) 的 MoveNext 和 Current 绝不被调用
        Assert.Equal(
            ["MoveNext:0", "Current:0", "MoveNext:1", "Current:1", "MoveNext:2", "Current:2"],
            events
        );
    }

    [Fact]
    public void CurrentExceptionStopsIterationImmediately()
    {
        var events = new List<string>();
        var iter = new TrackingIterator(events, [1L, 2L, 3L], failAtCurrent: 1);

        var err = Assert.Throws<PyRuntimeError>(() => PyOps.CallBuiltin("sum", [iter], [], []));
        Assert.Equal("iterator failed at Current:1", err.Message);
        Assert.Equal(["MoveNext:0", "Current:0", "MoveNext:1", "Current:1"], events);
    }

    [Fact]
    public void AllocationUpperBoundOnLongStreak()
    {
        // 预造 10,000 个预装箱 long 元素，装在 PyList 内部
        const int count = 10000;
        var items = new List<object?>(count);
        for (int i = 0; i < count; i++)
        {
            items.Add((long)i);
        }
        var list = new PyList(items);

        // 预热 JIT
        _ = PyOps.CallBuiltin("sum", [list], [], []);

        // 实际测量连续原生 long 累加的分配开销
        long allocatedBefore = GC.GetAllocatedBytesForCurrentThread();
        object? result = PyOps.CallBuiltin("sum", [list], [], []);
        long allocatedAfter = GC.GetAllocatedBytesForCurrentThread();

        long allocated = allocatedAfter - allocatedBefore;

        // 期望值: 0 + 1 + ... + 9999 = 49995000L
        Assert.Equal(49995000L, result);

        // 若每项装箱 (24 字节)，10,000 次加法至少分配 240,000 字节。
        // 原生累加消除了每项中间装箱，整轮迭代除游标外无多余分配，分配量远低于 2048 字节。
        Assert.True(allocated < 2048, $"Allocated {allocated} bytes during sum(), expected < 2048 bytes");
    }
}
