// 同轮三臂尺寸诊断；只排查引用存储机制，不等于产品list_ops/AOT性能。
using System.Diagnostics;
using System.Text.Json;
using RyuPy.Runtime;

int rounds = args.Length == 0 ? 12 : int.Parse(args[0]);
if (rounds < 6 || rounds % 6 != 0) throw new ArgumentException("轮数须为6的正倍数");
object?[] boxes = Enumerable.Range(0, 8192).Select(i => (object?)i).ToArray();
var contiguous = new List<object?>(boxes);
var segmented = new PySegmentedListStorage();
segmented.Extend(boxes);
int[] sizes = [0, 1, 64, 500, 1024, 1025, 4096];
string[][] orders = [
    ["contiguous", "fixed", "compact"], ["fixed", "compact", "contiguous"],
    ["compact", "contiguous", "fixed"], ["compact", "fixed", "contiguous"],
    ["fixed", "contiguous", "compact"], ["contiguous", "compact", "fixed"]];
var samples = new List<object>();
const int repeats = 2000;
for (int round = -6; round < rounds; round++)
{
    foreach (int size in round % 2 == 0 ? sizes : sizes.Reverse())
    foreach (int start in new[] { 0, 1000 })
    {
        string[] order = orders[(round + 6) % 6];
        for (int orderIndex = 0; orderIndex < order.Length; orderIndex++)
        {
            string mode = order[orderIndex];
            Func<IReadOnlyList<object?>> operation = mode switch
            {
                "contiguous" => () => contiguous.GetRange(start, size),
                "fixed" => () => segmented.SliceRange(start, size),
                _ => () => segmented.SliceRangeCompact(start, size),
            };
            GC.Collect(); GC.WaitForPendingFinalizers(); GC.Collect();
            long bytes = GC.GetAllocatedBytesForCurrentThread();
            long begin = Stopwatch.GetTimestamp();
            IReadOnlyList<object?>? result = null;
            long observedCount = 0;
            for (int iteration = 0; iteration < repeats; iteration++)
            {
                result = operation();
                observedCount += result.Count;
            }
            long ticks = Stopwatch.GetTimestamp() - begin;
            bytes = GC.GetAllocatedBytesForCurrentThread() - bytes;
            if (observedCount != (long)repeats * size || result!.Count != size)
                throw new InvalidOperationException("长度不符");
            for (int i = 0; i < size; i++)
                if (!ReferenceEquals(result[i], boxes[start + i])) throw new InvalidOperationException("引用次序不符");
            if (round >= 0) samples.Add(new { round, mode, size, start, order_index = orderIndex,
                repeats, ticks, allocated_bytes = bytes });
        }
    }
}
Console.WriteLine(JsonSerializer.Serialize(new { schema = "list-slice-size-study-v1", frequency = Stopwatch.Frequency,
    framework = System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription,
    scope = "warm-process normalized slice; three storage candidates; not product AOT; no RSS claim", samples }));
