// 手写专用恢复入口验证批量协议；不是编译器融合或性能验收。
using System;
using System.Numerics;
using RyuPy.Runtime;

namespace RyuPy.Runtime.Tests;

public class PyGeneratorSumTests
{
    private sealed class Payload(object?[] items)
    {
        internal object?[] Items = items;
        internal int Index;
        internal int BatchCalls;
        internal int OrdinaryCalls;
    }

    // 模拟生成器lowering：每项先发布PC，再offer；失败项不能再次从来源取出。
    private static PyGenerator Create(Payload data)
    {
        object? Step(object frame)
        {
            if (PyOps.GeneratorStateGetPC(frame) != 0) PyOps.GeneratorInput(frame);
            if (data.Index == data.Items.Length)
            {
                PyOps.GeneratorStateSetPC(frame, -1);
                return 999L; // 返回值不是yield，不应累计。
            }
            object? value = data.Items[data.Index++];
            PyOps.GeneratorStateSetPC(frame, data.Index);
            return value;
        }
        return new PyGenerator(frame => { data.OrdinaryCalls++; return Step(frame); }, data,
            0, "sum.py", "g", null, (frame, sum) => {
                data.BatchCalls++;
                while (true)
                {
                    object? current = Step(frame);
                    if (PyOps.GeneratorStateGetPC(frame) == -1 || !sum.TryConsume(current))
                        return current;
                }
            });
    }

    [Fact]
    public void LongRunUsesOneResumeAndDiscardsReturnValue()
    {
        var data = new Payload([1L, 2L, 3L]);
        var g = Create(data);
        Assert.Equal(6L, PyOps.CallBuiltin("sum", [g], [], []));
        Assert.Equal(1, data.BatchCalls);
        Assert.Equal(0, data.OrdinaryCalls);
        Assert.Equal("done", g.Next("done", true));
    }

    [Fact]
    public void EmptyPreservesStartAndZeroStillMakesResultDirty()
    {
        object start = 1000000L;
        Assert.Same(start, PyOps.CallBuiltin("sum", [Create(new Payload([])), start], [], []));
        object? result = PyOps.CallBuiltin("sum", [Create(new Payload([0L])), start], [], []);
        Assert.Equal(start, result);
        Assert.NotSame(start, result);
    }

    [Theory]
    [InlineData(long.MaxValue, 1L)]
    [InlineData(long.MinValue, -1L)]
    public void OverflowItemIsAddedExactlyOnce(long start, long item)
    {
        var data = new Payload([item, 7L]);
        Assert.Equal(new BigInteger(start) + item + 7,
            PyOps.CallBuiltin("sum", [Create(data), start], [], []));
        Assert.Equal(2, data.Index);
    }

    [Fact]
    public void ConsumerStopIterationDoesNotEnterProducerPep479OrCloseIt()
    {
        var cls = new PyClass("SumFailure");
        cls.SetAttr("__radd__", PyNativeFunctions.Create("__radd__", ["self", "other"], [null, null], 2,
            _ => throw new PyStopIteration("consumer")));
        var data = new Payload([1L, new PyInstance(cls), 8L]);
        var g = Create(data);
        Assert.Throws<PyStopIteration>(() => PyOps.CallBuiltin("sum", [g], [], []));
        Assert.Equal(2, data.Index);
        Assert.Equal(8L, g.Next(null, false));
    }

    [Fact]
    public void PausedGeneratorKeepsResumePositionAndOrdinarySend()
    {
        var data = new Payload([10L, 20L, 30L]);
        var g = Create(data);
        Assert.Equal(10L, g.Next(null, false));
        Assert.Equal(20L, g.Send(PyNone.Instance));
        Assert.Equal(30L, PyOps.CallBuiltin("sum", [g], [], []));
        Assert.Equal(2, data.OrdinaryCalls);
        Assert.Equal(1, data.BatchCalls);
    }

    [Fact]
    public void ProducerStopIterationStillUsesPep479()
    {
        var g = new PyGenerator(_ => throw new PyStopIteration("producer"), new object(),
            0, "s.py", "g", null, (_, _) => throw new PyStopIteration("producer"));
        var error = Assert.Throws<PyRuntimeError>(() => PyOps.CallBuiltin("sum", [g], [], []));
        Assert.Equal("generator raised StopIteration", error.Message);
        Assert.False(g.MoveNext());
    }
    [Fact]
    public void CallbackReturningLongKeepsItsBoxWhenNoLaterAdditionOccurs()
    {
        object returned = 1000000L;
        var cls = new PyClass("SumBox");
        cls.SetAttr("__radd__", PyNativeFunctions.Create("__radd__", ["self", "other"], [null, null], 2,
            _ => returned));
        Assert.Same(returned, PyOps.CallBuiltin("sum", [Create(new Payload([new PyInstance(cls)]))], [], []));
    }

    [Fact]
    public void ProducerReentryIsRejectedButConsumerCallbackCanResume()
    {
        PyGenerator? g = null;
        g = new PyGenerator(_ => null, new object(), 0, "s.py", "g", null, (frame, sum) => {
            Assert.Throws<PyValueError>(() => g!.Next(null, false));
            PyOps.GeneratorStateSetPC(frame, -1);
            return PyNone.Instance;
        });
        Assert.Equal(0L, PyOps.CallBuiltin("sum", [g], [], []));

        var cls = new PyClass("ResumeDuringAdd");
        cls.SetAttr("__radd__", PyNativeFunctions.Create("__radd__", ["self", "other"], [null, null], 2,
            _ => { Assert.Equal(7L, g!.Next(null, false)); return 5L; }));
        g = Create(new Payload([new PyInstance(cls), 7L]));
        Assert.Equal(5L, PyOps.CallBuiltin("sum", [g], [], []));
    }

#if !TRACE_ADAPTER_TEST
    [Fact]
    public void TraceDomainRejectsBatchWithoutConsuming()
    {
        PyTraceback.Reset();
        PyTraceProduct.Enter(1);
        try
        {
            var data = new Payload([1L]);
            Assert.False(Create(data).TrySumBatch(0L, out _));
            Assert.Equal(0, data.Index);
            Assert.Equal(0, data.BatchCalls);
        }
        finally { PyTraceProduct.Exit(); }
    }

#endif

}
