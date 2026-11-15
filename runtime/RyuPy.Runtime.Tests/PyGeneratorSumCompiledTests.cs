// 编译ABI的手写恢复代理测试；真实MIR/CIL发射、Python差分及性能另行验收。
using System;
using System.Numerics;
using RyuPy.Runtime;

namespace RyuPy.Runtime.Tests;

[Collection("生成器帧栈")]
public class PyGeneratorSumCompiledTests
{
    private sealed class SpyIterator : IPyIterator
    {
        internal int Calls;
        public bool MoveNext() { Calls++; return true; }
        public object Current() { Calls++; return 1L; }
    }

    [Fact]
    public void RangeGuardUsesExactSealedTypeWithoutConsuming()
    {
        var spy = new SpyIterator();
        Assert.False(PyOps.GeneratorSumRangeGuard(spy));
        Assert.False(PyOps.GeneratorSumRangeGuard(new object()));
        Assert.False(PyOps.GeneratorSumRangeGuard(null));
        Assert.Equal(0, spy.Calls);
        var iterator = new PyRangeIter(new PyRange(0, 3, 1));
        Assert.True(PyOps.GeneratorSumRangeGuard(iterator));
        Assert.True(iterator.MoveNext());
        Assert.Equal(0L, iterator.Current());
    }

    [Fact]
    public void OpaqueStateRejectsForgedObjectsAndLeavesRejectedItemUnconsumed()
    {
        Assert.False(PyOps.GeneratorSumTryConsume(new object(), 1L));
        var state = new PyGeneratorSumState(long.MaxValue);
        Assert.False(PyOps.GeneratorSumTryConsume(state, 1L));
        Assert.False(PyOps.GeneratorSumTryConsume(state, 1.0));
        Assert.Equal(long.MaxValue, state.Total);
        Assert.False(state.ConsumedAny);
        Assert.True(PyOps.GeneratorSumTryConsume(state, -1L));
        Assert.Equal(long.MaxValue - 1, state.Total);
        Assert.True(state.ConsumedAny);
    }

    private static PyGenerator Create(object?[] items, Action batchCalled, Action ordinaryCalled)
    {
        int index = 0;
        object? Step(object frame)
        {
            if (PyOps.GeneratorStateGetPC(frame) != 0) PyOps.GeneratorInput(frame);
            if (index == items.Length)
            {
                PyOps.GeneratorStateSetPC(frame, -1);
                return 999L;
            }
            object? item = items[index++];
            PyOps.GeneratorStateSetPC(frame, index);
            return item;
        }
        PyResumeSumCompiled compiled = (frame, sum) => {
            batchCalled();
            while (true)
            {
                object? item = Step(frame);
                if (PyOps.GeneratorStateGetPC(frame) == -1 || !PyOps.GeneratorSumTryConsume(sum, item))
                    return item;
            }
        };
        return new PyGenerator(frame => { ordinaryCalled(); return Step(frame); }, new object(),
            0, "compiled-sum.py", "g", null, compiled);
    }

    [Fact]
    public void CompiledBridgeBatchesAndPreservesNormalNextBeforeSum()
    {
        int batches = 0, ordinary = 0;
        var g = Create([1L, 2L, 3L], () => batches++, () => ordinary++);
        Assert.Equal(1L, g.Next(null, false));
        Assert.Equal(5L, PyOps.CallBuiltin("sum", [g], [], []));
        Assert.Equal(1, batches);
        Assert.Equal(1, ordinary);
        Assert.False(g.TrySumBatch(0, out _));
    }

    [Fact]
    public void CompiledBridgeOverflowFallsBackOnceAndRetainsEmptyStartIdentity()
    {
        object start = long.MaxValue;
        var g = Create([1L, 7L], () => { }, () => { });
        Assert.Equal(new BigInteger(long.MaxValue) + 8,
            Assert.IsType<BigInteger>(PyOps.CallBuiltin("sum", [g, start], [], [])));
        Assert.Same(start, PyOps.CallBuiltin("sum", [Create([], () => { }, () => { }), start], [], []));
    }

#if !TRACE_ADAPTER_TEST
    [Fact]
    public void CompiledBridgeCannotEnterBatchInTraceDomain()
    {
        int batches = 0, ordinary = 0;
        var g = Create([1L], () => batches++, () => ordinary++);
        PyTraceback.Reset();
        PyTraceProduct.Enter(1);
        try
        {
            Assert.False(g.TrySumBatch(0, out _));
            Assert.Equal(0, batches);
            Assert.Equal(0, ordinary);
        }
        finally { PyTraceProduct.Exit(); }
    }
#endif

    [Fact]
    public void PublicCompiledAbiDoesNotExposePrivateAccumulatorOrRemoveOldConstructor()
    {
        Assert.True(typeof(PyResumeSumCompiled).IsPublic);
        Assert.False(typeof(PyGeneratorSumState).IsPublic);
        Assert.False(typeof(PyResumeSum).IsPublic);
        Assert.NotNull(typeof(PyGenerator).GetConstructor([
            typeof(PyResume), typeof(object), typeof(long), typeof(string), typeof(string), typeof(object)]));
        Assert.NotNull(typeof(PyGenerator).GetConstructor([
            typeof(PyResume), typeof(object), typeof(long), typeof(string), typeof(string), typeof(object),
            typeof(PyResumeSumCompiled)]));
        Assert.Null(typeof(PyGenerator).GetConstructor([
            typeof(PyResume), typeof(object), typeof(long), typeof(string), typeof(string), typeof(object),
            typeof(PyResumeSum)]));
    }
}
