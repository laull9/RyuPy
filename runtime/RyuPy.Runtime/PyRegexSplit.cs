// split的局部绑定与原始匹配扫描：捕获保None，空匹配同位重试，整片身份与buffer租约各自明确。
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace RyuPy.Runtime;

internal static class PyRegexSplit
{
    private static readonly string[] ModuleNames = ["pattern", "string", "maxsplit", "flags"];
    private static readonly PySignature ModuleSignature = PySignature.Create("split", ModuleNames,
        ["pos", "pos", "pos", "pos"], ["0", "0", "1", "1"]);
    private static readonly PySignature PatternSignature = PySignature.Create("split", ["string", "maxsplit"],
        ["pos", "pos"], ["0", "1"]);

    internal static PyFunction Module() => new("split", ModuleSignature, (_, args, names, values) =>
    {
        object?[] bound = BindModule(args, names, values);
        // 保留现有Compile的flags与模式域，不能先消费maxsplit或subject。
        return PyRegex.Compile(bound[0], bound[3]).SplitInput(bound[1], bound[2]);
    }, [null, null, 0L, 0L], false);

    internal static PyFunction Pattern(PyRegexPattern pattern) => new("split", PatternSignature, (_, args, names, values) =>
    {
        var (source, limit) = BindPattern(args, names, values);
        return pattern.SplitInput(source, limit);
    }, [null, 0L], false);

    private static PyBaseException Error(string message) => PyRegexBytes.Own(new PyTypeError(message));

    // Python函数绑定先检查关键字，再检查位置数量；仅本API适配，不改共享binder的旧语义。
    private static object?[] BindModule(object?[] args, string[] names, object?[] values)
    {
        var bound = new object?[] { null, null, 0L, 0L };
        var filled = new bool[4];
        for (int i = 0; i < Math.Min(args.Length, 4); i++) { bound[i] = args[i]; filled[i] = true; }
        for (int i = 0; i < names.Length; i++)
        {
            int slot = Array.IndexOf(ModuleNames, names[i]);
            if (slot < 0) throw Error($"split() got an unexpected keyword argument '{names[i]}'");
            if (filled[slot]) throw Error($"split() got multiple values for argument '{names[i]}'");
            bound[slot] = values[i]; filled[slot] = true;
        }
        if (args.Length > 4) throw Error($"split() takes from 2 to 4 positional arguments but {args.Length} were given");
        if (!filled[0] && !filled[1]) throw Error("split() missing 2 required positional arguments: 'pattern' and 'string'");
        if (!filled[0] || !filled[1]) throw Error($"split() missing 1 required positional argument: '{(!filled[0] ? "pattern" : "string")}'");
        return bound;
    }

    // Pattern为C风格签名：先总数，再必填、位置重复和未知关键字；不借Python函数错误文本。
    private static (object? Source, object? Limit) BindPattern(object?[] args, string[] names, object?[] values)
    {
        int count = args.Length + names.Length;
        if (count > 2) throw Error($"split() takes at most 2 arguments ({count} given)");
        int sourceSlot = Array.IndexOf(names, "string"), limitSlot = Array.IndexOf(names, "maxsplit");
        if (args.Length == 0 && sourceSlot < 0) throw Error("split() missing required argument 'string' (pos 1)");
        if (args.Length > 0 && sourceSlot >= 0) throw Error("argument for split() given by name ('string') and position (1)");
        if (args.Length > 1 && limitSlot >= 0) throw Error("argument for split() given by name ('maxsplit') and position (2)");
        foreach (string name in names)
            if (name is not ("string" or "maxsplit")) throw Error($"'{name}' is an invalid keyword argument for split()");
        return (args.Length > 0 ? args[0] : values[sourceSlot],
            args.Length > 1 ? args[1] : limitSlot >= 0 ? values[limitSlot] : 0L);
    }
}

public partial class PyRegexPattern
{
    internal PyList SplitInput(object? source, object? maxsplit)
    {
        PyRegexScannerPattern.RejectConsumer(this);
        // 两域同用ssize_t，协议只执行一次；之后才观察可被index改写/resize/release的输入。
        long limit = PyRegexBytes.Index(maxsplit);
        if (BytePattern is not null)
        {
            if (source is PyByteArray or PyMemoryView) return SplitMutableBytes(source, limit);
            PyBytes bytes = PyRegexBytes.Subject(source);
            if (limit < 0) return new PyList([bytes]);
            return SplitRaw(Encoding.Latin1.GetString(bytes.Bytes), limit, bytes, null);
        }
        if (source is string text)
            return limit < 0 ? new PyList([text]) : SplitRaw(text, limit, null, null);
        // 活性/导出检查先于交叉域错误；即使maxsplit为负，也不能绕过subject检查。
        if (source is PyByteArray or PyMemoryView)
        {
            using PyRegexBufferLease lease = PyRegexBufferSubject.Acquire(source);
            throw PyRegexBytes.Own(new PyTypeError("cannot use a string pattern on a bytes-like object"));
        }
        if (source is PyBytes) throw PyRegexBytes.Own(new PyTypeError("cannot use a string pattern on a bytes-like object"));
        if (source is PyStringInstance) throw PyRegexBytes.Unsupported("split native str subclass subject");
        throw PyRegexBytes.Own(new PyTypeError($"expected string or bytes-like object, got '{PyRepr.TypeName(source)}'"));
    }

    // executor只供局部受控异常测试；正常调用不创建delegate，也不暴露宿主执行器。
    internal PyList SplitMutableBytes(object source, long limit, Func<Regex, string, int, Match>? executor = null)
    {
        using PyRegexBufferLease lease = PyRegexBufferSubject.Acquire(source);
        if (limit < 0) return new PyList([lease.CopyRange(0, lease.Length)]);
        return SplitRaw(lease.EncodeLatin1(lease.Length), limit, null, lease, executor);
    }

    private PyList SplitRaw(string input, long limit, PyBytes? bytes, PyRegexBufferLease? lease,
        Func<Regex, string, int, Match>? executor = null)
    {
        var parts = new List<object?>();
        int position = 0, copiedEnd = 0; long changed = 0; bool retry = false;
        // 切片仅分配最终结果，不包装公开Match、不缓存全匹配列表或每组全输入快照。
        object Slice(int start, int length) => lease is not null ? lease.CopyRange(start, length)
            : bytes is not null ? PyRegexBytes.Slice(bytes, start, length)
            : start == 0 && length == input.Length ? input : input.Substring(start, length);
        while (position <= input.Length)
        {
            Regex regex = retry ? _nonempty.Value : _search;
            Match result = executor is null ? RunBytes(regex, input, position) : executor(regex, input, position);
            if (!result.Success)
            {
                if (!retry || position == input.Length) break;
                // text按真实UTF16代理对推进一个码点；孤立代理码元保持一个位置，bytes永远只前进1。
                position += BytePattern is null && char.IsHighSurrogate(input[position])
                    && position + 1 < input.Length && char.IsLowSurrogate(input[position + 1]) ? 2 : 1;
                retry = false; continue;
            }
            parts.Add(Slice(copiedEnd, result.Index - copiedEnd));
            for (int number = 1; number <= Groups; number++)
            {
                Group group = result.Groups["ryu" + number];
                parts.Add(group.Success ? Slice(group.Index, group.Length) : PyNone.Instance);
            }
            copiedEnd = result.Index + result.Length;
            // 限额按分隔匹配数而非输出项；到达后立即取尾片，不再执行后端扫描。
            if (limit > 0 && ++changed >= limit) break;
            position = copiedEnd; retry = result.Length == 0;
        }
        parts.Add(Slice(copiedEnd, input.Length - copiedEnd));
        return new PyList(parts);
    }
}
