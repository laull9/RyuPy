// 正则可变subject的局部连续buffer租约：身份与扫描期导出分离，捕获只复制当前组范围。
// view租约另持exporter保护，不依赖两个finalizable对象的收尾先后；所有分配先于计数激活。
using System;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

namespace RyuPy.Runtime;

internal static class PyRegexBufferSubject
{
    internal static PyRegexBufferLease Acquire(object source)
    {
        if (source is PyByteArray array)
        {
            var lease = new PyRegexBufferLease(array, null, 0, array.Count);
            lease.Activate(); return lease;
        }
        if (source is PyMemoryView view)
        {
            // 只有已知内建view的本地活性检查；用户组号/pos协议已在此前完成，不在catch里。
            try { return view.AcquireRegexExport(); }
            catch (PyValueError)
            {
                throw PyRegexBytes.Own(new PyTypeError("expected string or bytes-like object, got 'memoryview'"));
            }
        }
        throw new InvalidOperationException("regex buffer subject must be bytearray or memoryview");
    }

    internal static PyBytes Capture(object source, int start, int end)
    {
        using PyRegexBufferLease lease = Acquire(source);
        int first = Math.Min(start, lease.Length), last = Math.Min(end, lease.Length);
        return lease.CopyRange(first, last - first);
    }
}

internal sealed class PyRegexBufferLease : IDisposable
{
    private readonly object _owner;
    private readonly PyMemoryView? _view;
    private readonly int _offset;
    private int _active;
    internal int Length { get; }

    // 构造失败时_active仍为0，部分构造对象finalize不会撤销别人的计数。
    internal PyRegexBufferLease(object owner, PyMemoryView? view, int offset, int length)
    { _owner = owner; _view = view; _offset = offset; Length = length; }

    // 调用方已完成全部可能抛出的检查/分配；此后仅内建原子计数和字段写，无用户回调。
    internal void Activate()
    {
        if (_owner is PyByteArray array) array.AcquireExport();
        Volatile.Write(ref _active, 1);
    }

    private ReadOnlySpan<byte> Bytes()
    {
        if (Volatile.Read(ref _active) == 0) throw new InvalidOperationException("regex buffer lease has been released");
        return _owner is PyBytes bytes ? bytes.Span.Slice(_offset, Length)
            : CollectionsMarshal.AsSpan(((PyByteArray)_owner).Bytes).Slice(_offset, Length);
    }

    // 只在活动lease作用域内使用，不存储到Match/缓存；供模板短检查及escape两遍扫描。
    internal ReadOnlySpan<byte> Span => Bytes();

    // 晚Join直接复制到最终数组，不为每个buffer片段另造中间bytes。
    internal void CopyTo(Span<byte> destination) => Bytes().CopyTo(destination);

    internal string EncodeLatin1(int end) => Encoding.Latin1.GetString(Bytes()[..end]);
    internal PyBytes CopyRange(int start, int length)
    {
        ReadOnlySpan<byte> bytes = Bytes().Slice(start, length);
        return length == 0 ? PyRegexBytes.Empty : new PyBytes(bytes.ToArray());
    }

    public void Dispose()
    {
        if (Interlocked.Exchange(ref _active, 0) == 0) return;
        // view可能已先finalize；撤二级计数不读取Owner，不调用公开Release或用户代码。
        _view?.ReleaseRegexExport();
        if (_owner is PyByteArray array) array.ReleaseExport();
        GC.SuppressFinalize(this);
    }

    ~PyRegexBufferLease() { Dispose(); }
}
