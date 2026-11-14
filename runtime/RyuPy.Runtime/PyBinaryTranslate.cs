// bytes/bytearray.translate：严格buffer输入、局部256项映射与两遍直接输出。
// 核心不调用Python转换协议；无输入快照、可变内容缓存或导出锁，保原输入与结果别名合同。
using System;
using System.Runtime.InteropServices;

namespace RyuPy.Runtime;

internal static class PyBinaryTranslate
{
    private static readonly object MissingDelete = new();
    internal static object Invoke(object receiver, object?[] args, string[] names, object?[] values, int offset = 0)
    {
        try
        {
            var (table, delete) = Bind(args, names, values, offset);
            // 窄快路放在调用层，不改变映射核心的局部span活区与JIT循环形状。
            // 先验证delete，released空view/显式None仍报错；非空delete核心会再做一次只读验证。
            if (receiver is PyBytes unchanged && table is null or PyNone
                && (ReferenceEquals(delete, MissingDelete) || Buffer(delete).IsEmpty)) return unchanged;
            return Translate(receiver, table, delete);
        }
        catch (PyBaseException error)
        {
            // 只补本地buffer/绑定错误的现场；不替换原异常对象、已有帧或context。
            PyErrors.PrepareRaise(error);
            throw;
        }
    }

    internal static PyBaseException Error(PyBaseException error) => (PyBaseException)PyErrors.PrepareRaise(error);

    private static (object? Table, object? Delete) Bind(object?[] args, string[] names, object?[] values, int offset)
    {
        int positional = args.Length - offset;
        long total = (long)positional + names.Length;
        // CP先计位置+关键字总数，再检查必填位置table，最后识别关键字。
        // root用offset排除self，不复制实参；显式None与省略delete不能混同。
        if (total > 2)
            throw new PyTypeError($"translate() takes at most 2 {(positional == 0 ? "keyword " : "")}arguments ({total} given)");
        if (positional == 0)
            throw new PyTypeError("translate() takes at least 1 positional argument (0 given)");
        object? delete = positional == 2 ? args[offset + 1] : MissingDelete;
        for (int i = 0; i < names.Length; i++)
        {
            if (names[i] != "delete")
                throw new PyTypeError($"'{names[i]}' is an invalid keyword argument for translate()");
            delete = values[i];
        }
        return (args[offset], delete);
    }

    private static object Translate(object receiver, object? table, object? delete)
    {
        Span<ushort> mapping = stackalloc ushort[256];
        if (table is null or PyNone)
        {
            for (int i = 0; i < mapping.Length; i++) mapping[i] = (ushort)i;
        }
        else
        {
            ReadOnlySpan<byte> bytes = Buffer(table);
            // table检查必须先于delete获取；空receiver也不能绕过这两次验证。
            if (bytes.Length != 256) throw new PyValueError("translation table must be 256 characters long");
            for (int i = 0; i < mapping.Length; i++) mapping[i] = bytes[i];
        }
        ReadOnlySpan<byte> deleted = ReferenceEquals(delete, MissingDelete) ? ReadOnlySpan<byte>.Empty : Buffer(delete);
        foreach (byte value in deleted) mapping[value] = 256;
        ReadOnlySpan<byte> source = receiver is PyBytes immutable ? immutable.Span
            : CollectionsMarshal.AsSpan(((PyByteArray)receiver).Bytes);
        int length = 0;
        bool changed = false;
        foreach (byte value in source)
        {
            ushort mapped = mapping[value];
            if (mapped != 256) length++;
            if (mapped != value) changed = true;
        }
        if (receiver is PyBytes original)
        {
            if (!changed) return original;
            var result = new byte[length];
            Write(source, mapping, result);
            return new PyBytes(result);
        }
        // bytearray即使无变化或为空也必须fresh；直接填最终List，不经中间byte[]复制。
        var mutable = new PyByteArray(Array.Empty<byte>());
        mutable.Bytes.EnsureCapacity(length);
        CollectionsMarshal.SetCount(mutable.Bytes, length);
        Write(source, mapping, CollectionsMarshal.AsSpan(mutable.Bytes));
        return mutable;
    }

    private static void Write(ReadOnlySpan<byte> source, ReadOnlySpan<ushort> mapping, Span<byte> target)
    {
        int index = 0;
        foreach (byte value in source)
        {
            ushort mapped = mapping[value];
            if (mapped != 256) target[index++] = (byte)mapped;
        }
    }

    private static ReadOnlySpan<byte> Buffer(object? value) => value switch
    {
        PyBytes bytes => bytes.Span,
        PyByteArray bytes => CollectionsMarshal.AsSpan(bytes.Bytes),
        PyMemoryView view => view.ReadSynchronousBuffer(),
        // 不运行未知buffer回调，更不以__bytes__/迭代协议冒充buffer。
        PyInstance instance when instance.Class.TryLookup("__buffer__", out _, out _) =>
            throw new PyNotImplementedError("translate does not support user-defined buffer providers"),
        _ => throw new PyTypeError($"a bytes-like object is required, not '{PyRepr.TypeName(value)}'"),
    };
}
