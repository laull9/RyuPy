// 可变集合的有限迭代状态：保留快照，修复大小变化及耗尽粘性，明确拒绝 clear 后同大小恢复。
// 无 clear 的同大小变异仍保既有快照边界；不模拟 CPython 哈希槽位，也不调用用户 hash/equality。
namespace RyuPy.Runtime;

internal sealed class PySetIterator : IPyIterator
{
    private readonly PySet _source;
    private readonly object?[] _snapshot;
    private readonly long _clearEpoch;
    private int _cursor;
    private bool _sizeFailed;
    private bool _finished;

    internal PySetIterator(PySet source)
    {
        _source = source;
        _snapshot = [.. source.Items];
        _clearEpoch = source.ClearEpoch;
    }

    public bool MoveNext()
    {
        // 只有实际返回 false 才耗尽；交付末项还不能跳过下一次的大小检查。
        if (_finished) return false;
        if (_sizeFailed || _source.Count != _snapshot.Length)
        {
            _sizeFailed = true;
            throw (PyRuntimeError)PyErrors.PrepareRaise(
                new PyRuntimeError("Set changed size during iteration"));
        }
        if (_source.ClearEpoch != _clearEpoch)
            throw (PyNotImplementedError)PyErrors.PrepareRaise(new PyNotImplementedError(
                "set iteration after clear with restored size is not supported yet"));
        if (_cursor >= _snapshot.Length)
        {
            _finished = true;
            return false;
        }
        _cursor++;
        return true;
    }

    // 对应最近一次成功 MoveNext 的值，不在已交付值上追加 mutation 检查。
    public object? Current() => _snapshot[_cursor - 1];
}
