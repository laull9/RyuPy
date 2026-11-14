// 正则bytes输出的buffer冷拼接：callback结束后才借用可变结果，所有借用覆盖最终分配/复制并统一释放。
// exact全bytes仍走原Join；此入口不缓存内容，不扩大公开模板/escape，也不调用用户转换协议。
using System;
using System.Collections.Generic;

namespace RyuPy.Runtime;

internal static partial class PyRegexBytes
{
    // beforeCopy仅供每调用受控失败专项；正常入口不创建委托，不保存hook或改变公开ABI。
    internal static PyBytes JoinBufferParts(List<object?> parts, PyBytes empty, Action? beforeCopy = null)
    {
        // 先完成容器分配再取得任何lease。取得后的槽位赋值不会分配或调用用户代码。
        var leases = new PyRegexBufferLease?[parts.Count];
        try
        {
            long length = 0;
            for (int index = 0; index < parts.Count; index++)
            {
                object? part = parts[index];
                if (part is PyBytes bytes) { length += bytes.Count; continue; }
                PyRegexBufferLease lease;
                if (part is PyByteArray array) lease = PyRegexBufferSubject.Acquire(array);
                else if (part is PyMemoryView view)
                {
                    // 仅内建view活性检查的错误映射；不先借Subject生成另一已Own异常再重包。
                    try { lease = view.AcquireRegexExport(); }
                    catch (PyValueError) { throw JoinItemError(index, part); }
                }
                else throw JoinItemError(index, part);
                leases[index] = lease; length += lease.Length;
            }
            // 与原Join相同：先检查所有片段的类型，再检查CLR容量，不制造OOM或任意限额。
            CheckCapacity(length);
            beforeCopy?.Invoke();
            if (length == 0) return empty;
            var result = new byte[(int)length]; int offset = 0;
            for (int index = 0; index < parts.Count; index++)
            {
                if (leases[index] is PyRegexBufferLease lease)
                { lease.CopyTo(result.AsSpan(offset, lease.Length)); offset += lease.Length; }
                else
                {
                    var bytes = (PyBytes)parts[index]!;
                    bytes.Span.CopyTo(result.AsSpan(offset)); offset += bytes.Count;
                }
            }
            return new PyBytes(result);
        }
        finally
        {
            foreach (PyRegexBufferLease? lease in leases) lease?.Dispose();
        }
    }

    private static PyBaseException JoinItemError(int index, object? value) => Own(new PyTypeError(
        $"sequence item {index}: expected a bytes-like object, {PyRepr.TypeName(value)} found"));
}
