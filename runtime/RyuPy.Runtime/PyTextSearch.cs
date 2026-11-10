// 五个str搜索方法的唯一绑定、索引转换和码点窗口权威；不分配文本切片或码点数组。
using System;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace RyuPy.Runtime;

internal static class PyTextSearch
{
    internal static bool Has(string name) => name is "count" or "find" or "rfind" or "index" or "rindex";

    internal static long Invoke(string text, string name, object?[] args, string[] names)
    {
        if (names.Length != 0) throw Own(new PyTypeError($"{name}() takes no keyword arguments"));
        if (args.Length < 1) throw Own(new PyTypeError($"{name}() takes at least 1 argument (0 given)"));
        if (args.Length > 3) throw Own(new PyTypeError($"{name}() takes at most 3 arguments ({args.Length} given)"));
        // PIC同站点重入会覆盖数组。先冻结引用，但end槽要在start转换之后才查询。
        object? needleObject = args[0];
        object? startObject = args.Length > 1 ? args[1] : null;
        object? endObject = args.Length > 2 ? args[2] : null;
        long logicalStart = Index(startObject, 0);
        long logicalEnd = Index(endObject, long.MaxValue);
        if (!PyStringLayout.TryGet(needleObject, out string needle))
            throw Own(new PyTypeError($"must be str, not {PyRepr.TypeName(needleObject)}"));
        int start = logicalStart == 0 ? 0 : PyTextWindow.Offset(text, logicalStart, clipEnd: false);
        int end = logicalEnd == long.MaxValue ? text.Length : PyTextWindow.Offset(text, logicalEnd, clipEnd: true);
        bool valid = start >= 0 && start <= end;
        bool count = name == "count", reverse = name is "rfind" or "rindex";
        if (!valid) return count ? 0 : Missing(name);
        if (needle.Length == 0)
            return count ? PyTextWindow.Count(text, start, end) + 1
                : PyTextWindow.Count(text, 0, reverse ? end : start);
        if (needle.Length > end - start) return count ? 0 : Missing(name);
        if (count) return Count(text, needle, start, end);
        int found = Find(text, needle, start, end, reverse);
        // 只对最终命中做一次码点计数，避免每个候选都从头扫描。
        return found < 0 ? Missing(name) : PyTextWindow.Count(text, 0, found);
    }

    private static long Missing(string name)
    {
        if (name is "index" or "rindex") throw Own(new PyValueError("substring not found"));
        return -1;
    }

    // ordinal有界搜索只移动候选范围；必须同时检查命中的左右端点。
    private static int Find(string text, string needle, int start, int end, bool reverse)
    {
        while (end - start >= needle.Length)
        {
            ReadOnlySpan<char> window = text.AsSpan(start, end - start);
            int local = reverse ? window.LastIndexOf(needle.AsSpan(), StringComparison.Ordinal)
                : window.IndexOf(needle.AsSpan(), StringComparison.Ordinal);
            if (local < 0) return -1;
            int found = start + local;
            if (PyTextWindow.IsBoundary(text, found) && PyTextWindow.IsBoundary(text, found + needle.Length))
                return found;
            // 排除当前候选而保留所有更早/更晚候选；范围内部可以临时落在代理对中。
            if (reverse) end = found + needle.Length - 1;
            else start = found + 1;
        }
        return -1;
    }

    private static long Count(string text, string needle, int start, int end)
    {
        long count = 0;
        while (end - start >= needle.Length)
        {
            int found = Find(text, needle, start, end, reverse: false);
            if (found < 0) break;
            count++;
            start = found + needle.Length;
        }
        return count;
    }

    private static long Index(object? value, long fallback) => value switch
    {
        null or PyNone => fallback,
        int number => number,
        long number => number,
        bool flag => flag ? 1 : 0,
        BigInteger number => Saturate(number),
        _ => IndexSlow(value),
    };

    private static long Saturate(BigInteger number) => number < long.MinValue ? long.MinValue
        : number > long.MaxValue ? long.MaxValue : (long)number;

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static long IndexSlow(object? value)
    {
        if (!PyIndexSlot.TryInvoke(value, out object? result))
            throw Own(new PyTypeError("slice indices must be integers or None or have an __index__ method"));
        if (result is bool flag)
        {
            PyWarnings.IndexReturnedStrictSubclass("bool");
            return flag ? 1 : 0;
        }
        return result switch
        {
            int number => number,
            long number => number,
            BigInteger number => Saturate(number),
            _ => throw Own(new PyTypeError($"__index__ returned non-int (type {PyRepr.TypeName(result)})")),
        };
    }

    // 用户协议与warning异常不捕获；仅本API新造错误准备现场。
    private static T Own<T>(T error) where T : PyBaseException => (T)PyErrors.PrepareRaise(error);
}
