// 存储原型筛选器：只测引用追加/固定长度批量复制，不冒充原始 list_ops 或 Python/AOT 验收。
using System.Diagnostics;
using System.Text.Json;
using RyuPy.Runtime;

int rounds = args.Length == 0 ? 10 : int.Parse(args[0]);
if (rounds < 1) throw new ArgumentOutOfRangeException(nameof(rounds));
object?[] boxes = Enumerable.Range(0, 300_000).Select(i => (object?)i).ToArray();
var samples = new List<object>();
for (int round = -2; round < rounds; round++)
{
    foreach (string mode in round % 2 == 0 ? new[] { "contiguous", "segmented" } : new[] { "segmented", "contiguous" })
    {
        // 明确将回收置于计时外；同一进程共享源盒，RSS不用于独立臂峰值结论。
        GC.Collect(); GC.WaitForPendingFinalizers(); GC.Collect();
        long allocated = GC.GetAllocatedBytesForCurrentThread();
        long begin = Stopwatch.GetTimestamp();
        int count;
        object? sentinel;
        if (mode == "contiguous")
        {
            var items = new List<object?>();
            foreach (object? box in boxes) items.Add(box);
            var chunk = items.GetRange(1000, 1000);
            var more = new List<object?>();
            for (int i = 0; i < 2000; i++) more.AddRange(chunk);
            count = more.Count;
            sentinel = more[^1];
            GC.KeepAlive(items); GC.KeepAlive(more);
        }
        else
        {
            var items = new PySegmentedListStorage();
            foreach (object? box in boxes) items.Append(box);
            var chunk = items.Slice(new PySlice(1000L, 2000L, null));
            var more = new PySegmentedListStorage();
            for (int i = 0; i < 2000; i++) more.ExtendExact(chunk);
            count = more.Count;
            sentinel = more.GetItem(-1);
            GC.KeepAlive(items); GC.KeepAlive(more);
        }
        long end = Stopwatch.GetTimestamp();
        long bytes = GC.GetAllocatedBytesForCurrentThread() - allocated;
        if (count != 2_000_000 || !ReferenceEquals(sentinel, boxes[1999]))
            throw new InvalidOperationException("存储结果或身份不一致");
        if (round >= 0) samples.Add(new { round, mode, ticks = end - begin, allocated_bytes = bytes, count });
    }
}
Console.WriteLine(JsonSerializer.Serialize(new { schema = "list-storage-prototype-v1", frequency = Stopwatch.Frequency,
    framework = System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription,
    scope = "warm-process reference append+slice+extend; source boxing excluded; not list_ops", samples }));
