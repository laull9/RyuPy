// str前后缀的窄绑定与码点窗口；保存实参引用防重入，候选按序短路，不复制文本或tuple。
using System;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace RyuPy.Runtime;

internal static class PyTextAffix
{
    // 直接/根调用与保存的绑定方法只有keyword错误标签不同，正文和错误优先级共用。
    internal static bool Match(string text, string method, object?[] args, string[] names, bool qualified)
    {
        if (names.Length != 0)
            throw Error(new PyTypeError($"{(qualified ? "str." : "")}{method}() takes no keyword arguments"));
        if (args.Length < 1)
            throw Error(new PyTypeError($"{method}() takes at least 1 argument (0 given)"));
        if (args.Length > 3)
            throw Error(new PyTypeError($"{method}() takes at most 3 arguments ({args.Length} given)"));

        // PIC按站点复用args；start回调可能重入同站点，因此任何回调前冻结三个原对象。
        // 只冻结end对象，不提前查询它的__index__，start修改其类槽仍应可见。
        object? prefix = args[0];
        object? startObject = args.Length > 1 ? args[1] : null;
        object? endObject = args.Length > 2 ? args[2] : null;
        int start = 0, end = text.Length;
        if (args.Length > 1)
        {
            long logicalStart = Index(startObject, 0);
            long logicalEnd = Index(endObject, long.MaxValue);
            start = Offset(text, logicalStart, clipEnd: false);
            end = Offset(text, logicalEnd, clipEnd: true);
        }
        bool valid = start >= 0 && start <= end;
        bool suffix = method == "endswith";
        if (PyStringLayout.TryGet(prefix, out string single))
            return MatchesWindow(text, single, start, end, valid, suffix);
        if (prefix is PyTuple tuple)
        {
            // 不调用tuple派生类的迭代/下标，也不预先检查尚未访问的后续候选。
            foreach (object? candidate in tuple.Items)
            {
                if (!PyStringLayout.TryGet(candidate, out string part))
                    throw Error(new PyTypeError($"tuple for {method} must only contain str, not {PyRepr.TypeName(candidate)}"));
                if (MatchesWindow(text, part, start, end, valid, suffix)) return true;
            }
            return false;
        }
        throw Error(new PyTypeError($"{method} first arg must be str or a tuple of str, not {PyRepr.TypeName(prefix)}"));
    }

    // 单参数直接使用整串UTF16窗口；不扫描码点长度，不制造Substring或候选数组。
    // 窗口端点已按码点定位，ordinal成功后只需检查另一个匹配端点没有切开代理对。
    private static bool MatchesWindow(string text, string part, int start, int end, bool valid, bool suffix)
    {
        if (!valid || part.Length > end - start) return false;
        if (part.Length == 0) return true;
        ReadOnlySpan<char> window = text.AsSpan(start, end - start);
        bool matches = suffix ? window.EndsWith(part.AsSpan(), StringComparison.Ordinal)
            : window.StartsWith(part.AsSpan(), StringComparison.Ordinal);
        if (!matches) return false;
        int boundary = suffix ? end - part.Length : start + part.Length;
        return boundary == 0 || boundary == text.Length
            || !char.IsHighSurrogate(text[boundary - 1]) || !char.IsLowSurrogate(text[boundary]);
    }

    // 不先计算整串码点长度：正值从首定位，负值从尾定位。正start越界保-1而非夹尾，
    // 因为空前缀在start==len时成立、start>len时不成立。每个tuple仅算一次窗口。
    private static int Offset(string text, long logical, bool clipEnd) =>
        PyTextWindow.Offset(text, logical, clipEnd);

    private static long Index(object? value, long fallback) => value switch
    {
        null or PyNone => fallback,
        long integer => integer,
        int integer => integer,
        bool boolean => boolean ? 1L : 0L,
        _ => IndexSlow(value),
    };

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static long IndexSlow(object? value)
    {
        object? integer = value;
        if (integer is not BigInteger)
        {
            // 类型级协议仅一次；正文异常原样传播，不catch或重写用户现场。
            if (!PyObject.TryMethod(value, "__index__", [], out integer))
                throw Error(new PyTypeError("slice indices must be integers or None or have an __index__ method"));
            if (integer is bool)
                throw Error(new PyNotImplementedError("str affix bounds: __index__ returning bool requires DeprecationWarning support"));
            if (!PyInt.IsInt(integer))
                throw Error(new PyTypeError($"__index__ returned non-int (type {PyRepr.TypeName(integer)})"));
        }
        if (PyInt.TryI64(integer!, out long small)) return small;
        var wide = (BigInteger)integer!;
        return wide < long.MinValue ? long.MinValue : wide > long.MaxValue ? long.MaxValue : (long)wide;
    }

    // 仅新造的本层错误准备caller/context；成功路和用户协议正文不做帧快照。
    private static T Error<T>(T error) where T : PyBaseException => (T)PyErrors.PrepareRaise(error);
}
