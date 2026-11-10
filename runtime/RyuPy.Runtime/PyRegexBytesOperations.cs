// bytes域冷调用与替换输出：数值转换先于消费，callback结果保原引用到最终join；旧text循环不迁移。
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace RyuPy.Runtime;

internal static partial class PyRegexBytes
{
    internal static PyBaseException Own(PyBaseException error) => (PyBaseException)PyErrors.PrepareRaise(error);
    internal static PyBaseException Unsupported(string operation) => Own(new PyNotImplementedError("re: " + operation + " is not supported"));

    internal static long Index(object? value)
    {
        object? integer = value;
        if (integer is not (bool or int or long or BigInteger))
        {
            GuardSlot(value, "__index__");
            if (!PyObject.TryMethod(value, "__index__", [], out integer))
                throw Own(new PyTypeError($"'{PyRepr.TypeName(value)}' object cannot be interpreted as an integer"));
            if (integer is bool) throw Unsupported("bytes regex __index__ returning bool requires DeprecationWarning");
            if (integer is not (int or long or BigInteger))
                throw Own(new PyTypeError($"__index__ returned non-int (type {PyRepr.TypeName(integer)})"));
        }
        if (integer is bool flag) return flag ? 1 : 0;
        if (integer is int small) return small;
        if (integer is long machine) return machine;
        BigInteger large = (BigInteger)integer!;
        if (large < long.MinValue || large > long.MaxValue)
            throw Own(new PyOverflowError("Python int too large to convert to C ssize_t"));
        return (long)large;
    }

    // group号饱和后必落在有限组范围外，直接BigInteger比较避免错误借用pos/count Overflow。
    internal static int GroupNumber(PyRegexPattern pattern, object? value)
    {
        if (TryInteger(value, out BigInteger number))
        {
            if (number >= 0 && number <= pattern.Groups) return (int)number;
        }
        else if (value is string name)
        {
            if (pattern.Names.TryGetValue(name, out int named)) return named;
        }
        else if (pattern.Names.Count != 0)
        {
            if (!PyHash.IsHashable(value)) throw Own(new PyTypeError($"unhashable type: '{PyRepr.TypeName(value)}'"));
            // 只有非常规name查询才建临时Python字典，保用户hash/eq；不增Pattern常驻字段。
            var names = new PyDict();
            foreach (var pair in pattern.Names) names.Set(pair.Key, (long)pair.Value);
            if (names.TryGet(value, out object? found)) return (int)(long)found!;
        }
        throw Own(new PyIndexError("no such group"));
    }

    internal static int ErrorStop(object value, int length)
    {
        if (!TryInteger(value, out BigInteger number))
            throw Own(new PyTypeError("slice indices must be integers or None or have an __index__ method"));
        if (number < 0) number += length;
        return number < 0 ? 0 : number > length ? length : (int)number;
    }

    private static bool TryInteger(object? value, out BigInteger number)
    {
        object? integer = value;
        if (!PyInt.IsInt(integer))
        {
            GuardSlot(value, "__index__");
            if (!PyObject.TryMethod(value, "__index__", [], out integer)) { number = 0; return false; }
            if (integer is bool) throw Unsupported("bytes regex __index__ returning bool requires DeprecationWarning");
            if (!PyInt.IsInt(integer)) throw Own(new PyTypeError($"__index__ returned non-int (type {PyRepr.TypeName(integer)})"));
        }
        number = PyInt.ToBigInteger(integer!); return true;
    }

    internal static void GuardSlot(object? value, string name)
    {
        PyClass? cls = value is PyInstance instance ? instance.Class : (value as PyBaseException)?.PythonClass;
        if (cls is not null && cls.TryLookup(name, out _, out object? raw)
            && raw is PyInstance descriptor && descriptor.Class.TryLookup("__get__", out _, out _))
            throw Unsupported("bytes regex custom " + name + " descriptor");
    }

    internal static PyBytes Subject(object? value) => value switch
    {
        PyBytes bytes => bytes,
        PyByteArray or PyMemoryView => throw Unsupported("mutable or view regex subject"),
        string or PyStringInstance => throw Own(new PyTypeError("cannot use a bytes pattern on a string-like object")),
        _ => throw Own(new PyTypeError($"expected string or bytes-like object, got '{PyRepr.TypeName(value)}'")),
    };

    internal static PyBytes Escape(object? value)
    {
        if (value is PyByteArray or PyMemoryView) return EscapeBuffer(value);
        if (value is not PyBytes bytes)
            throw Own(new PyTypeError($"decoding to str: need a bytes-like object, {PyRepr.TypeName(value)} found"));
        var output = new List<byte>();
        foreach (byte item in bytes.Bytes)
        {
            if ("()[]{}?*+-|^$\\.&~# \t\n\r\v\f".Contains((char)item)) output.Add(92);
            output.Add(item);
        }
        return new PyBytes(output.ToArray());
    }

    // sub literal只短借检查，不复制原replacement；含反斜杠时同一租约内快照，解析前先释放。
    internal static PyRegexBytesTemplate? PrepareArrayTemplate(PyRegexPattern pattern, PyByteArray source)
    {
        PyBytes snapshot;
        using (PyRegexBufferLease lease = PyRegexBufferSubject.Acquire(source))
        {
            if (!lease.Span.Contains((byte)92)) return null;
            snapshot = lease.CopyRange(0, lease.Length);
        }
        return new PyRegexBytesTemplate(pattern, snapshot);
    }

    // Match.expand即使literal也编译；source字段接收此快照，保re.error.pattern不随原array改写。
    internal static PyBytes SnapshotArrayTemplate(PyByteArray source)
    {
        using PyRegexBufferLease lease = PyRegexBufferSubject.Acquire(source);
        return lease.CopyRange(0, lease.Length);
    }

    private static PyBytes EscapeBuffer(object source)
    {
        PyRegexBufferLease lease;
        if (source is PyMemoryView view)
        {
            try { lease = view.AcquireRegexExport(); }
            catch (PyValueError)
            { throw Own(new PyTypeError("decoding to str: need a bytes-like object, memoryview found")); }
        }
        else lease = PyRegexBufferSubject.Acquire(source);
        using (lease)
        {
            ReadOnlySpan<byte> input = lease.Span;
            const string special = "()[]{}?*+-|^$\\.&~# \t\n\r\v\f";
            long length = input.Length;
            foreach (byte item in input) if (special.Contains((char)item)) length++;
            if (length > Array.MaxLength) throw Unsupported("bytes regex escape over the CLR buffer capacity");
            if (length == 0) return Empty;
            var output = new byte[(int)length]; int offset = 0;
            foreach (byte item in input)
            {
                if (special.Contains((char)item)) output[offset++] = 92;
                output[offset++] = item;
            }
            return new PyBytes(output);
        }
    }

    internal static PyBytes Join(List<object?> parts, PyBytes empty)
    {
        long length = 0;
        for (int index = 0; index < parts.Count; index++)
        {
            object? part = parts[index];
            if (part is PyByteArray or PyMemoryView) return JoinBufferParts(parts, empty);
            if (part is not PyBytes bytes)
                throw Own(new PyTypeError($"sequence item {index}: expected a bytes-like object, {PyRepr.TypeName(part)} found"));
            length += bytes.Count;
        }
        if (parts.Count == 0) return empty;
        if (parts.Count == 1) return (PyBytes)parts[0]!;
        CheckCapacity(length);
        var result = new byte[(int)length]; int offset = 0;
        foreach (object? part in parts)
        {
            var bytes = (PyBytes)part!; bytes.Span.CopyTo(result.AsSpan(offset)); offset += bytes.Count;
        }
        return result.Length == 0 ? empty : new PyBytes(result);
    }

    // 只按真实CLR数组容量守门，验证全部片段类型后才调用；不捕获OOM或设任意更小限额。
    internal static void CheckCapacity(long length)
    {
        if (length > Array.MaxLength) throw Unsupported("bytes regex replacement over the CLR buffer capacity");
    }
}

public partial class PyRegexPattern
{
    internal object ApplyBytesInput(string method, object? value, object? start, object? end)
    {
        long first = PyRegexBytes.Index(start), last = PyRegexBytes.Index(end);
        if (value is PyByteArray or PyMemoryView)
        {
            // 两个index协议全部完成后才构造持租约iterator；eager与exact执行正文不迁移。
            if (method == "finditer") return CreateMutableBytesIterator(value, first, last);
            if (method is "match" or "search" or "fullmatch" or "findall")
                return ApplyMutableBytes(method, value, first, last);
        }
        return ApplyBytes(method, PyRegexBytes.Subject(value), first, last);
    }

    internal object SubstituteBytesInput(object? replacement, object? value, object? countValue, bool tuple)
    {
        long count = PyRegexBytes.Index(countValue);
        bool callable = PyOps.CallBuiltin("callable", [replacement], [], []) is true
            && (replacement is not PyInstance instance || instance.Class.TryLookup("__call__", out _, out _));
        PyRegexBytesTemplate? template = null;
        if (!callable)
        {
            if (replacement is PyMemoryView) throw PyRegexBytes.Unsupported("mutable or view regex replacement template");
            if (replacement is PyByteArray array) template = PyRegexBytes.PrepareArrayTemplate(this, array);
            else
            {
                if (replacement is PyStringInstance) throw PyRegexBytes.Unsupported("str subclass regex replacement template");
                bool literal = replacement is string text && !text.Contains('\\')
                    || replacement is PyBytes bytes && Array.IndexOf(bytes.Bytes, (byte)92) < 0;
                if (!literal) template = new PyRegexBytesTemplate(this, replacement);
            }
        }
        // count与模板准备完才取得可变subject；exact bytes正文保原路径与身份规则。
        if (value is PyByteArray or PyMemoryView)
            return SubstituteMutableBytesPrepared(replacement, value, count, tuple, callable, template);
        PyBytes subject = PyRegexBytes.Subject(value);
        var parts = new List<object?>(); int end = 0; long changed = 0;
        if (count >= 0)
        {
            string encoded = Encoding.Latin1.GetString(subject.Bytes);
            foreach (PyRegexMatch match in EnumerateBytes(subject, encoded, 0, subject.Count))
            {
                if (end < match.Raw.Index) parts.Add(PyRegexBytes.Slice(subject, end, match.Raw.Index - end));
                object? inserted;
                if (callable)
                {
                    // 只在真的调用时拒绝高阶槽；无匹配不能额外触发用户协议或NI。
                    PyRegexBytes.GuardSlot(replacement, "__call__");
                    inserted = PyOps.CallValue(replacement, [match], [], []);
                }
                else inserted = template is null ? replacement : template.Expand(match);
                if (inserted is not (null or PyNone)) parts.Add(inserted);
                end = match.Raw.Index + match.Raw.Length; changed++;
                if (count > 0 && changed >= count) break;
            }
        }
        if (end < subject.Count) parts.Add(PyRegexBytes.Slice(subject, end, subject.Count - end));
        PyBytes output = PyRegexBytes.Join(parts, subject.Count == 0 ? subject : PyRegexBytes.Empty);
        return tuple ? new PyTuple([output, changed]) : output;
    }
}
