// mutable/view正则阶段A：仅内部eager匹配；一次扫描快照与公开Match活读分开，未开放iterator/sub。
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace RyuPy.Runtime;

public sealed partial class PyRegexPattern
{
    internal object ApplyMutableBytes(string method, object source, long start, long end)
    {
        if (method is not ("match" or "search" or "fullmatch" or "findall"))
            throw PyRegexBytes.Unsupported("mutable or view regex subject");
        using PyRegexBufferLease lease = PyRegexBufferSubject.Acquire(source);
        int first = (int)Math.Clamp(start, 0L, lease.Length), last = (int)Math.Clamp(end, 0L, lease.Length);
        string input = lease.EncodeLatin1(last);
        if (method == "findall") return FindAllMutableBytes(lease, input, first, last);
        if (first > last) return PyNone.Instance;
        Match result = RunBytes(method == "fullmatch" ? _full.Value : _search, input, first);
        if (!result.Success || method == "match" && result.Index != first) return PyNone.Instance;
        return new PyRegexMatch(this, new PyRegexMutableCaptures(this, source, result), first, last);
    }

    private object FindAllMutableBytes(PyRegexBufferLease lease, string input, int start, int end)
    {
        var values = new List<object?>(); int position = start; bool retry = false;
        // eager扫描无Python回调，保一次全窗口编码；只复制输出range，不制造逃逸Match或额外group租约。
        while (position <= end)
        {
            Match result = RunBytes(retry ? _nonempty.Value : _search, input, position);
            if (!result.Success)
            {
                if (!retry || position == end) break;
                position++; retry = false; continue;
            }
            object Group(int number)
            {
                Group group = number == 0 ? result.Groups[0] : result.Groups["ryu" + number];
                return group.Success ? lease.CopyRange(group.Index, group.Length) : PyRegexBytes.Empty;
            }
            values.Add(Groups == 0 ? Group(0) : Groups == 1 ? Group(1)
                : new PyTuple(Enumerable.Range(1, Groups).Select(Group).ToArray()));
            position = result.Index + result.Length; retry = result.Length == 0;
        }
        return new PyList(values);
    }
}

internal sealed class PyRegexMutableCaptures
{
    internal object Source { get; }
    internal readonly record struct Capture(bool Success, int Start, int End);
    private readonly Capture[] _captures;
    internal PyRegexMutableCaptures(PyRegexPattern pattern, object source, Match match)
    {
        Source = source; _captures = new Capture[pattern.Groups + 1];
        for (int number = 0; number < _captures.Length; number++)
        {
            Group group = number == 0 ? match.Groups[0] : match.Groups["ryu" + number];
            _captures[number] = new(group.Success, group.Index, group.Index + group.Length);
        }
    }
    internal Capture Get(int number) => _captures[number];
    internal object Group(int number)
    {
        Capture group = _captures[number];
        return group.Success ? PyRegexBufferSubject.Capture(Source, group.Start, group.End) : PyNone.Instance;
    }
}
