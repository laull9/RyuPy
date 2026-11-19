// 模板结果的独立writer：仅终端整对象首次写入保身份；部分字面与非终端文本物化。
// 空片不占输出，已经物化的结果不因后续空片回退为用户str子类；不复制CP内存实现。
using System.Text;

namespace RyuPy.Runtime;

internal sealed class PyStringTemplateWriter
{
    // 与已验收值级format相同的实际CLR字符串容量，不给正常模板增加任意小上限。
    private const long ManagedStringMaxLength = 0x3FFFFFDF;
    private StringBuilder? _buffer;
    private object? _terminalObject;
    private long _length;

    internal void Literal(object source, string text, int start, int length, bool terminal)
    {
        if (length == 0) return;
        Reserve(length);
        if (_buffer is null && _terminalObject is null && terminal && start == 0 && length == text.Length)
        {
            _terminalObject = source;
            return;
        }
        Buffer().Append(text, start, length);
    }

    internal void Result(object value, bool terminal)
    {
        string text = PyStringLayout.RequireRaw(value);
        if (text.Length == 0) return;
        Reserve(text.Length);
        if (_buffer is null && _terminalObject is null && terminal)
        {
            _terminalObject = value;
            return;
        }
        Buffer().Append(text);
    }

    internal object Finish() => _length == 0 ? string.Empty
        : _terminalObject ?? _buffer!.ToString();

    private StringBuilder Buffer()
    {
        // 终端借用之后不应再有写入；仍支持降为精确文本，避免任何静默丢片。
        if (_buffer is null) _buffer = new StringBuilder();
        if (_terminalObject is not null)
        {
            _buffer.Append(PyStringLayout.RequireRaw(_terminalObject));
            _terminalObject = null;
        }
        return _buffer;
    }

    internal static void CheckCapacity(long length)
    {
        if (length > ManagedStringMaxLength)
            throw PyStringTemplate.Error(new PyNotImplementedError("str.format: result exceeds managed string capacity"));
    }

    private void Reserve(int length)
    {
        CheckCapacity(_length + length);
        _length += length;
    }
}
