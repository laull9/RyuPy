// 字节域内部匹配分支：Latin1仅用于宿主执行，公开捕获始终切原PyBytes，偏移直接按byte计算。
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace RyuPy.Runtime;

internal static partial class PyRegexBytes
{
    internal static readonly PyBytes Empty = new([]);
    internal static PyBytes Slice(PyBytes source, int start, int length) =>
        start == 0 && length == source.Count ? source : length == 0 ? Empty
            : new PyBytes(source.Span.Slice(start, length).ToArray());

    internal static void CheckFlags(int flags)
    {
        if ((flags & 32) != 0) throw new PyValueError("cannot use UNICODE flag with a bytes pattern");
        if ((flags & (256 | 4)) == (256 | 4)) throw new PyValueError("ASCII and LOCALE flags are incompatible");
        if ((flags & 4) != 0) throw new PyNotImplementedError("re: LOCALE bytes patterns are not supported");
        if ((flags & ~(2 | 8 | 16 | 64 | 256)) != 0) throw new PyNotImplementedError("re: unsupported flags");
    }
}

public partial class PyRegexPattern
{
    // 公共Compile的exact bytes构造入口；只在本地解析/构造失败时准备原异常。
    internal static PyRegexPattern CreateBytes(PyBytes pattern, int flags)
    {
        try { return new PyRegexPattern(Encoding.Latin1.GetString(pattern.Bytes), flags, pattern); }
        catch (PyBaseException error)
        {
            // 此段只有本地解析/构造，不执行用户协议，准备原错误而不改身份或消息。
            PyErrors.PrepareRaise(error); throw;
        }
    }

    internal object ApplyBytes(string method, PyBytes value, long start, long end)
    {
        int first = (int)Math.Clamp(start, 0L, value.Count), last = (int)Math.Clamp(end, 0L, value.Count);
        // 每次操作只编码一次；没有码点offsets数组，也不把内部文本公开为subject。
        string subject = Encoding.Latin1.GetString(value.Bytes, 0, last);
        if (method == "finditer") return new PyRegexIterator(EnumerateBytes(value, subject, first, last).GetEnumerator());
        if (method == "findall")
        {
            var values = new List<object?>();
            foreach (var match in EnumerateBytes(value, subject, first, last))
                values.Add(Groups == 0 ? match.Group(0) : Groups == 1 ? match.GroupOrEmpty(1)
                    : new PyTuple(Enumerable.Range(1, Groups).Select(match.GroupOrEmpty).ToArray()));
            return new PyList(values);
        }
        if (first > last) return PyNone.Instance;
        Match result = RunBytes(method == "fullmatch" ? _full.Value : _search, subject, first);
        if (!result.Success || method == "match" && result.Index != first) return PyNone.Instance;
        return new PyRegexMatch(this, value, result, first, last);
    }

    private IEnumerable<PyRegexMatch> EnumerateBytes(PyBytes value, string subject, int start, int end)
    {
        if (start > end) yield break;
        int position = start; bool retry = false;
        while (position <= end)
        {
            Match result = RunBytes(retry ? _nonempty.Value : _search, subject, position);
            if (!result.Success)
            {
                if (!retry || position == end) yield break;
                position++; retry = false; continue;
            }
            yield return new PyRegexMatch(this, value, result, start, end);
            position = result.Index + result.Length; retry = result.Length == 0;
        }
    }

    private static Match RunBytes(Regex regex, string subject, int position)
    {
        try { return Run(regex, subject, position); }
        catch (PyBaseException error) { PyErrors.PrepareRaise(error); throw; }
    }

    private string BytesRepr()
    {
        var names = new List<string>();
        foreach (var (flag, name) in new[] { (2, "IGNORECASE"), (8, "MULTILINE"), (16, "DOTALL"), (64, "VERBOSE"), (256, "ASCII") })
            if ((Flags & flag) != 0) names.Add("re." + name);
        return "re.compile(" + PyRepr.Repr(BytePattern) + (names.Count == 0 ? "" : ", " + string.Join('|', names)) + ")";
    }
}
