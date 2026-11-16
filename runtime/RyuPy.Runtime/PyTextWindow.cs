// 文本纯窗口工具：码点边界转UTF16、检查代理对、计数；没有用户协议或实参绑定。
namespace RyuPy.Runtime;

internal static class PyTextWindow
{
    // 正start越尾返回-1，end独立夹尾；负下标从尾走，不对long.MinValue取负。
    internal static int Offset(string text, long logical, bool clipEnd)
    {
        if (logical == 0) return 0;
        if (logical > 0)
        {
            if (logical > text.Length || clipEnd && logical == text.Length)
                return clipEnd ? text.Length : -1;
            int offset = PyText.OffsetOf(text, logical);
            return offset < 0 && clipEnd ? text.Length : offset;
        }
        if (logical <= -(long)text.Length) return 0;
        int tail = text.Length;
        // 保持负remaining逐次递增，不对long.MinValue取负而溢出。
        while (logical < 0 && tail > 0)
        {
            char last = text[--tail];
            if (char.IsLowSurrogate(last) && tail > 0 && char.IsHighSurrogate(text[tail - 1])) tail--;
            logical++;
        }
        return tail;
    }

    internal static bool IsBoundary(string text, int offset) => offset == 0 || offset == text.Length
        || !char.IsHighSurrogate(text[offset - 1]) || !char.IsLowSurrogate(text[offset]);

    // 调用者保证两个端点都是有效码点边界；不创建切片字符串。
    internal static long Count(string text, int start, int end)
    {
        long count = 0;
        while (start < end)
        {
            char current = text[start++];
            if (char.IsHighSurrogate(current) && start < end && char.IsLowSurrogate(text[start])) start++;
            count++;
        }
        return count;
    }
}
