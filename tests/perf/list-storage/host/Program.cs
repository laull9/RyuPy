// 存储原型筛选器：逐阶段测引用存储，不冒充原始 list_ops 或 Python/AOT 验收。
using System.Diagnostics;
using System.Text.Json;
using RyuPy.Runtime;

int rounds = args.Length == 0 ? 10 : int.Parse(args[0]);
if (rounds < 1) throw new ArgumentOutOfRangeException(nameof(rounds));
object?[] boxes = Enumerable.Range(0, 300_000).Select(i => (object?)i).ToArray();
var samples = new List<object>();
void Measure(int round, string mode, string stage, Action action, Action verify)
{
    // 源构造、委托分配、回收及结果校验全部在计时外；RSS不能作为独立臂峰值。
    GC.Collect(); GC.WaitForPendingFinalizers(); GC.Collect();
    long allocated = GC.GetAllocatedBytesForCurrentThread();
    long begin = Stopwatch.GetTimestamp();
    action();
    long end = Stopwatch.GetTimestamp();
    long bytes = GC.GetAllocatedBytesForCurrentThread() - allocated;
    verify();
    if (round >= 0) samples.Add(new { round, mode, stage, ticks = end - begin, allocated_bytes = bytes });
}
void Verify(IReadOnlyList<object?> result, int count, int offset, int period)
{
    if (result.Count != count) throw new InvalidOperationException("存储长度不一致");
    for (int i = 0; i < count; i++)
        if (!ReferenceEquals(result[i], boxes[offset + i % period]))
            throw new InvalidOperationException("存储顺序或身份不一致");
}
for (int round = -2; round < rounds; round++)
{
    foreach (string mode in round % 2 == 0 ? new[] { "contiguous", "segmented" } : new[] { "segmented", "contiguous" })
    {
        // 两臂使用同一切片规格，协议阶段双方均执行Indices；另列预归一化存储复制。
        var slices = Enumerable.Range(0, 200).Select(i => new PySlice((long)i, (long)i + 500, null)).ToArray();
        if (mode == "contiguous")
        {
            var items = new List<object?>();
            Measure(round, mode, "append", () => { foreach (object? box in boxes) items.Add(box); },
                () => Verify(items, boxes.Length, 0, boxes.Length));
            List<object?>? slice = null;
            Measure(round, mode, "slice", () => {
                foreach (PySlice spec in slices)
                {
                    var (start, _, _, count) = spec.Indices(items.Count);
                    slice = items.GetRange((int)start, (int)count);
                }
            }, () => Verify(slice!, 500, 199, 500));
            Measure(round, mode, "slice_normalized", () => {
                for (int i = 0; i < 200; i++) slice = items.GetRange(i, 500);
            }, () => Verify(slice!, 500, 199, 500));
            var chunk = items.GetRange(1000, 1000);
            var more = new List<object?>();
            Measure(round, mode, "extend", () => {
                for (int i = 0; i < 2000; i++) more.AddRange(chunk);
            }, () => Verify(more, 2_000_000, 1000, 1000));
        }
        else
        {
            var items = new PySegmentedListStorage();
            Measure(round, mode, "append", () => { foreach (object? box in boxes) items.Append(box); },
                () => Verify(items, boxes.Length, 0, boxes.Length));
            PySegmentedListStorage? slice = null;
            Measure(round, mode, "slice", () => {
                foreach (PySlice spec in slices) slice = items.Slice(spec);
            }, () => Verify(slice!, 500, 199, 500));
            Measure(round, mode, "slice_normalized", () => {
                for (int i = 0; i < 200; i++) slice = items.SliceRange(i, 500);
            }, () => Verify(slice!, 500, 199, 500));
            var chunk = items.SliceRange(1000, 1000);
            var more = new PySegmentedListStorage();
            Measure(round, mode, "extend", () => {
                for (int i = 0; i < 2000; i++) more.ExtendExact(chunk);
            }, () => Verify(more, 2_000_000, 1000, 1000));
        }
    }
}
Console.WriteLine(JsonSerializer.Serialize(new { schema = "list-storage-prototype-v3", frequency = Stopwatch.Frequency,
    framework = System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription,
    scope = "warm-process per-stage reference storage; source boxing excluded; not list_ops; not product AOT", samples }));
