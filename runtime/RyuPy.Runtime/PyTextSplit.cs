// str.split共享绑定/转换/正文：所有调用形态保持index先于separator验证，码点边界不拆代理对。
using System;
using System.Numerics;

namespace RyuPy.Runtime;

internal static class PyTextSplit
{
    internal static PyList Invoke(object receiver, object?[] args, string[] names, object?[] values)
    {
        string text = PyStringLayout.RequireRaw(receiver);
        if (args.Length + names.Length > 2) throw Own(new PyTypeError($"split() takes at most 2 {(args.Length == 0 ? "keyword arguments" : "arguments")} ({args.Length + names.Length} given)"));
        object? separator = args.Length > 0 ? args[0] : PyNone.Instance;
        object? maximum = args.Length > 1 ? args[1] : -1L;
        bool hasSeparator = args.Length > 0, hasMaximum = args.Length > 1;
        for (int i = 0; i < names.Length; i++)
        {
            switch (names[i])
            {
                case "sep":
                    if (hasSeparator) throw Own(new PyTypeError("argument for split() given by name ('sep') and position (1)"));
                    hasSeparator = true; separator = values[i]; break;
                case "maxsplit":
                    if (hasMaximum) throw Own(new PyTypeError("argument for split() given by name ('maxsplit') and position (2)"));
                    hasMaximum = true; maximum = values[i]; break;
                default: throw Own(new PyTypeError($"'{names[i]}' is an invalid keyword argument for split()"));
            }
        }
        // 在用户index回调之前冻结引用；回调重入不得导致实参数组重读。
        long limit = Index(maximum);
        if (separator is null or PyNone) return Whitespace(text, limit);
        if (!PyStringLayout.TryGet(separator, out string part))
            throw Own(new PyTypeError($"must be str or None, not {PyRepr.TypeName(separator)}"));
        if (part.Length == 0) throw Own(new PyValueError("empty separator"));
        var result = new PyList();
        int start = 0, scan = 0;
        while (limit != 0 && scan <= text.Length - part.Length)
        {
            int relative = text.AsSpan(scan).IndexOf(part.AsSpan(), StringComparison.Ordinal);
            if (relative < 0) break;
            int found = scan + relative;
            if (!PyTextPartition.Boundary(text, found) || !PyTextPartition.Boundary(text, found + part.Length))
            { scan = found + 1; continue; }
            result.Append(text[start..found]);
            start = scan = found + part.Length;
            if (limit > 0) limit--;
        }
        result.Append(text[start..]);
        return result;
    }

    private static PyList Whitespace(string text, long limit)
    {
        var result = new PyList();
        int offset = 0;
        while (offset < text.Length)
        {
            while (offset < text.Length && PyUnicodeWhitespace.Is(text[offset])) offset++;
            if (offset == text.Length) break;
            if (limit == 0) { result.Append(text[offset..]); break; }
            int start = offset;
            while (offset < text.Length && !PyUnicodeWhitespace.Is(text[offset])) offset++;
            result.Append(text[start..offset]);
            if (limit > 0) limit--;
        }
        return result;
    }

    private static long Index(object? value)
    {
        if (value is not (int or long or bool or BigInteger))
        {
            if (!PyIndexSlot.TryInvoke(value, out object? converted))
                throw Own(new PyTypeError($"'{PyRepr.TypeName(value)}' object cannot be interpreted as an integer"));
            value = converted;
            if (value is bool) PyWarnings.IndexReturnedStrictSubclass("bool");
            if (value is not (int or long or bool or BigInteger))
                throw Own(new PyTypeError($"__index__ returned non-int (type {PyRepr.TypeName(value)})"));
        }
        return value switch
        {
            bool flag => flag ? 1 : 0,
            int number => number,
            long number => number,
            BigInteger number when number >= long.MinValue && number <= long.MaxValue => (long)number,
            _ => throw Own(new PyOverflowError("Python int too large to convert to C ssize_t")),
        };
    }
    private static T Own<T>(T error) where T : PyBaseException => (T)PyErrors.PrepareRaise(error);
}
