// 日志记录保存原始消息与实参；每个 handler 格式化时才触发用户字符串转换。
namespace RyuPy.Runtime;
internal sealed class PyLoggingRecord
{
    internal readonly string Name;
    internal readonly long Level;
    internal readonly object? Message;
    internal readonly object?[] Arguments;
    internal PyLoggingRecord(string name, long level, object? message, object?[] arguments)
    { Name = name; Level = level; Message = message; Arguments = arguments; }
    internal string GetMessage()
    {
        string message = PyRepr.Str(Message);
        if (Arguments.Length == 0) return message;
        object arguments = Arguments.Length == 1 && Arguments[0] is PyDict dict && dict.Count != 0
            ? dict : Arguments.Length == 1 && Arguments[0] is PyDictInstance derived && derived.Payload.Count != 0
                ? derived : new PyTuple(Arguments);
        return PyLoggingPercent.Format(message, arguments);
    }
}
/// <summary>logging 的首批百分号风格 Formatter。</summary>
public sealed class PyLoggingFormatter : PyNativeObject
{
    private readonly string _format;
    /// <summary>保存格式；不提前转换用户消息。</summary>
    public PyLoggingFormatter(string? format)
    {
        _format = string.IsNullOrEmpty(format) ? "%(message)s" : format;
        if (!ValidMapping(_format)) throw new PyValueError("Invalid format for percent style");
    }
    private static bool ValidMapping(string format)
    {
        // 验证至少存在一个合法映射占位符；未实现的宽度/转换在实际格式化时明确拒绝。
        for (int i = 0; i + 2 < format.Length; i++)
        {
            if (format[i] != '%' || format[i + 1] != '(') continue;
            int end = i + 2;
            while (end < format.Length && (char.IsLetterOrDigit(format[end]) || format[end] == '_')) end++;
            if (end == i + 2 || end >= format.Length || format[end++] != ')') continue;
            while (end < format.Length && "#0+- ".Contains(format[end])) end++;
            if (end < format.Length && format[end] == '*') end++;
            else while (end < format.Length && char.IsAsciiDigit(format[end])) end++;
            if (end < format.Length && format[end] == '.')
            {
                end++;
                if (end < format.Length && format[end] == '*') end++;
                else { int start = end; while (end < format.Length && char.IsAsciiDigit(format[end])) end++; if (end == start) continue; }
            }
            if (end < format.Length && "diouxefgcrsa%".Contains(char.ToLowerInvariant(format[end]))) return true;
        }
        return false;
    }
    /// <inheritdoc/>
    public override string TypeName => "Formatter";
    /// <inheritdoc/>
    public override object? GetAttr(string name) => throw Missing(name);
    internal string Format(PyLoggingRecord record)
    {
        var fields = new PyDict(); fields.Set("name", record.Name); fields.Set("levelno", record.Level);
        fields.Set("levelname", PyLogging.LevelName(record.Level)); fields.Set("message", record.GetMessage());
        try { return PyLoggingPercent.Format(_format, fields); }
        catch (PyKeyError error) { throw new PyValueError("Formatting field not found in record: " + error.PyMessage); }
    }
}
