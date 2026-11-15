// host层探针筛选：模拟源码每轮新tuple的get/整数累加/set；不是编译器或真实records性能验收。
// 仅在无locals/身份观察、无用户回调的显式实验合同下使用探针，外部失效回原tuple路径。
using System;
using System.Diagnostics;
using System.Linq;
using Xunit;
using Xunit.Abstractions;

namespace RyuPy.Runtime.Tests;

[Collection("生成器帧栈")]
public sealed class PyDictStringPairProbeDiagnosticTests(ITestOutputHelper output)
{
    private readonly record struct Input(string First, string Second);
    private sealed record Sample(double Milliseconds, long Bytes, int Fallbacks, string Result);

    [Theory]
    [InlineData(32, 0, false, false)]
    [InlineData(32, 1, false, false)]
    [InlineData(32, 10, false, false)]
    [InlineData(4096, 0, false, false)]
    [InlineData(32, 0, true, false)]
    [InlineData(32, 0, false, true)]
    public void PairedGetIncrementSetHostDiagnostic(int cardinality, int missPercent, bool collision, bool invalidate)
    {
        const int count = 60000, rounds = 6;
        // 字符串与操作序列在两臂外预生成；两臂都以相同引用读取，不将输入构造计入热区。
        Input[] initial = Enumerable.Range(0, cardinality)
            .Select(i => new Input(collision ? new string('\0', i + 1) : "user" + i, "buy")).ToArray();
        if (collision)
            foreach (Input key in initial)
                Assert.Equal(PyHash.TwoStringTupleHash(initial[0].First, "buy"), PyHash.TwoStringTupleHash(key.First, "buy"));
        Input[] inputs = Enumerable.Range(0, count)
            .Select(i => i % 100 < missPercent ? new Input("new" + i, "view") : initial[i % cardinality]).ToArray();

        // 两臂均做完整预热；每个样本重建相同字典，避免之前的miss变成后续hit。
        Sample warmA = Run(initial, inputs, false, invalidate), warmB = Run(initial, inputs, true, invalidate);
        Assert.Equal(warmA.Result, warmB.Result);
        var elapsedA = new double[rounds]; var elapsedB = new double[rounds];
        for (int round = 0; round < rounds; round++)
        {
            Sample a, b;
            if (round % 2 == 0) { a = Run(initial, inputs, false, invalidate); b = Run(initial, inputs, true, invalidate); }
            else { b = Run(initial, inputs, true, invalidate); a = Run(initial, inputs, false, invalidate); }
            Assert.Equal(a.Result, b.Result);
            Assert.Equal(invalidate ? count - count / 2 : 0, b.Fallbacks);
            elapsedA[round] = a.Milliseconds; elapsedB[round] = b.Milliseconds;
            output.WriteLine($"host-probe keys={cardinality} missPercent={missPercent} collision={collision} invalidate={invalidate} round={round} order={(round % 2 == 0 ? "AB" : "BA")} A_ms={a.Milliseconds:F3} B_ms={b.Milliseconds:F3} A_bytes={a.Bytes} B_bytes={b.Bytes} fallback={b.Fallbacks}");
        }
        Array.Sort(elapsedA); Array.Sort(elapsedB);
        output.WriteLine($"host-probe median A_ms={(elapsedA[2] + elapsedA[3]) / 2:F3} B_ms={(elapsedB[2] + elapsedB[3]) / 2:F3}; 仅host筛选，非NativeAOT产品结论");
    }

    private static Sample Run(Input[] initial, Input[] inputs, bool probe, bool invalidate)
    {
        var dict = new PyDict(); var access = new PyDict.StringPairAccess(dict); object zero = 0L;
        foreach (Input key in initial)
        {
            // 大表从空域经自身安全插入建立证明；不绕过256槽外部变更预算。
            if (probe) Assert.True(access.TrySet(key.First, key.Second, zero));
            else dict.Set(new PyTuple([key.First, key.Second]), zero);
        }
        int fallbacks = 0;
        long before = GC.GetAllocatedBytesForCurrentThread(), started = Stopwatch.GetTimestamp();
        for (int i = 0; i < inputs.Length; i++)
        {
            // 模拟区外写入：同一位置、同一非tuple键。其后必须持续走原路径。
            if (invalidate && i == inputs.Length / 2) dict.Set("outside", zero);
            Input input = inputs[i];
            if (probe && access.TryGet(input.First, input.Second, out bool found, out object? value))
            {
                object updated = (found ? (long)value! : 0L) + 1L;
                if (!access.TrySet(input.First, input.Second, updated))
                    throw new InvalidOperationException("无回调标量区中域证明异常失效");
            }
            else
            {
                if (probe) fallbacks++;
                // 对应Python源码key=(user,action)：每轮一次物化，get和set复用同一实体。
                var key = new PyTuple([input.First, input.Second]);
                object updated = (long)dict.GetOrDefault(key, zero)! + 1L;
                dict.Set(key, updated);
            }
        }
        double elapsed = Stopwatch.GetElapsedTime(started).TotalMilliseconds;
        long allocated = GC.GetAllocatedBytesForCurrentThread() - before;
        // 完整键/值/顺序在计时外校验；不只用checksum隐藏缺键或排列错误。
        return new Sample(elapsed, allocated, fallbacks, PyRepr.Repr(dict));
    }
}
