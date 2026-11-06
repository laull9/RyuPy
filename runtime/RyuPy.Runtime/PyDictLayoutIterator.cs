// 新布局独立游标：实时值、大小错误粘住、正常耗尽永久；同尺寸结构替换明确暂缓。
namespace RyuPy.Runtime;

internal sealed class PyDictLayoutIterator : IPyIterator
{
    private readonly PyDict _dict;
    private readonly PyDictViewKind _kind;
    private readonly bool _reverse;
    private readonly int _size;
    private readonly long _version;
    private int _position;
    private bool _finished;
    private bool _sizeError;
    private object? _current;
    internal PyDictLayoutIterator(PyDict dict, PyDictViewKind kind, bool reverse = false)
    {
        _dict = dict; _kind = kind; _reverse = reverse;
        _size = dict.Count; _version = dict.Version;
        _position = reverse ? dict.SlotCount - 1 : 0;
    }
    public bool MoveNext()
    {
        if (_finished) return false;
        if (_sizeError || _dict.Count != _size)
        {
            _sizeError = true; _current = null;
            throw PyDictLayout.Own(new PyRuntimeError("dictionary changed size during iteration"));
        }
        if (_version != _dict.Version)
            throw PyDictLayout.Own(new PyNotImplementedError("dict iterator after same-size structural mutation is not supported"));
        while (_position >= 0 && _position < _dict.SlotCount)
        {
            int slot = _position; _position += _reverse ? -1 : 1;
            if (!_dict.TryKeyAt(slot, out object? key)) continue;
            _current = _kind switch
            {
                PyDictViewKind.Keys => key,
                PyDictViewKind.Values => _dict.ValueAt(slot),
                _ => new PyTuple([key, _dict.ValueAt(slot)]),
            };
            return true;
        }
        _finished = true; _current = null; return false;
    }
    public object? Current() => _current;
}
