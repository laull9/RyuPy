// 可变subject替换内部正文：回调前复制前缀，续扫前刷新全窗口，释放扫描租约后才join。
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace RyuPy.Runtime;

public sealed partial class PyRegexPattern
{
    // 公共sub入口已按count→replacement顺序准备参数；此处不重复转换或模板解析。
    // executor仅供每调用受控故障专项，默认不创建delegate、不改变exact bytes执行路。
    internal object SubstituteMutableBytesPrepared(object? replacement, object source, long count, bool tuple,
        bool callable, PyRegexBytesTemplate? template, Func<Regex, string, int, Match>? executor = null)
    {
        var (parts, changed) = CollectMutableSubstitutionParts(replacement, source, count, callable, template, executor);
        // Collect返回前using已确定撤本次export；晚Join错误也不保租约。
        PyBytes output = PyRegexBytes.Join(parts, PyRegexBytes.Empty);
        return tuple ? new PyTuple([output, changed]) : output;
    }

    internal (List<object?> Parts, long Changed) CollectMutableSubstitutionParts(object? replacement, object source,
        long count, bool callable, PyRegexBytesTemplate? template, Func<Regex, string, int, Match>? executor = null)
    {
        using PyRegexBufferLease lease = PyRegexBufferSubject.Acquire(source);
        var parts = new List<object?>();
        int position = 0, copiedEnd = 0, length = lease.Length;
        long changed = 0; bool retry = false, dirty = false;
        // 无callback的literal/template只编码一次；负count仍acquire并复制尾部，但完全不调用执行器。
        string? input = count >= 0 ? lease.EncodeLatin1(length) : null;
        while (count >= 0 && position <= length)
        {
            // 只有成功callback并需要继续时置dirty；非空重试失败/游标前进没有用户写，不重复编码。
            if (dirty) { input = lease.EncodeLatin1(length); dirty = false; }
            Regex regex = retry ? _nonempty.Value : _search;
            Match result = executor is null ? RunBytes(regex, input!, position) : executor(regex, input!, position);
            if (!result.Success)
            {
                if (!retry || position == length) break;
                position++; retry = false; continue;
            }
            int first = result.Index, last = first + result.Length;
            if (copiedEnd < first) parts.Add(lease.CopyRange(copiedEnd, first - copiedEnd));
            // literal不制造Match；模板复用A的marks活读，不把宿主快照留给公开对象。
            object? inserted;
            if (callable)
            {
                // 不在无匹配/负count时提前触碰高阶槽；保持原callback异常、晚join和返回对象身份。
                PyRegexBytes.GuardSlot(replacement, "__call__");
                var match = new PyRegexMatch(this, new PyRegexMutableCaptures(this, source, result), 0, length);
                inserted = PyOps.CallValue(replacement, [match], [], []);
            }
            else inserted = template is null ? replacement : template.Expand(new PyRegexMatch(this,
                new PyRegexMutableCaptures(this, source, result), 0, length));
            if (inserted is not (null or PyNone)) parts.Add(inserted);
            copiedEnd = last; changed++;
            if (count > 0 && changed >= count) break;
            position = last; retry = result.Length == 0; dirty = callable;
        }
        if (copiedEnd < length) parts.Add(lease.CopyRange(copiedEnd, length - copiedEnd));
        return (parts, changed);
    }
}
