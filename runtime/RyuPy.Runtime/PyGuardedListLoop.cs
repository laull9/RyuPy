// 编译器已证明无观察点的布尔写入循环；失败守卫无写入，成功仅物化最终整数。
using System.Numerics;

namespace RyuPy.Runtime;

public static partial class PyOps
{
    /// <summary>精确整数及列表守卫；null要求编译器执行完整原循环。</summary>
    public static object? GuardedListSetFalseLoop(object? list, object? start, object? bound, object? step)
    {
        if (list is not PyList target || start is not long j || bound is not long end
            || step is not long stride || j < 0 || j > end || stride <= 0 || end >= target.Count)
            return null;
        return target.GuardedSetFalseLoop(j, end, stride);
    }
}

public sealed partial class PyList
{
    // 数值背板可能在写bool时物化，保留原路径的分配与异常位置。
    internal object? GuardedSetFalseLoop(long j, long end, long stride)
    {
        if (_numeric is not null) return null;
        object falseValue = PyOps.BoxBool(false);
        while (j <= end)
        {
            if (_booleans is { } booleans) booleans[(int)j] = false;
            else _items![(int)j] = falseValue;
            // 正步溢出必超过long bound，Python终值须保持任意精度。
            if (j > long.MaxValue - stride) return new BigInteger(j) + stride;
            j += stride;
        }
        return PyOps.BoxInt(j);
    }
}
