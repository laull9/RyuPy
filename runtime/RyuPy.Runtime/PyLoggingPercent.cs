// 日志专用的有限百分号格式，不把 format-spec 或 CLR 格式串冒充 Python 的 % 协议。
using System;
using System.Text;
namespace RyuPy.Runtime;
internal static class PyLoggingPercent
{
    internal static string Format(string format, object? arguments)
    {
        var output = new StringBuilder(); int used = 0;
        object?[] values = arguments is PyTuple tuple ? tuple.Items : [arguments];
        bool mapping = false;
        for (int i = 0; i < format.Length; i++)
        {
            if (format[i] != '%') { output.Append(format[i]); continue; }
            if (++i == format.Length) throw new PyValueError("incomplete format");
            if (format[i] == '%') { output.Append('%'); continue; }
            object? value;
            if (format[i] == '(')
            {
                int end = format.IndexOf(')', i + 1);
                if (end < 0) throw new PyValueError("incomplete format key");
                string key = format[(i + 1)..end]; mapping = true;
                if (arguments is not PyDict dict) throw new PyTypeError("format requires a mapping");
                value = dict.Get(key); i = end + 1;
                if (i == format.Length) throw new PyValueError("incomplete format");
            }
            else
            {
                if (used >= values.Length) throw new PyTypeError("not enough arguments for format string");
                value = values[used++];
            }
            output.Append(format[i] switch
            {
                's' => PyRepr.Str(value), 'r' => PyRepr.Repr(value),
                'd' => Decimal(value),
                _ => throw new PyNotImplementedError("logging percent format supports only %s, %r, %d and %%"),
            });
        }
        if (!mapping && used != values.Length && arguments is not PyDict) throw new PyTypeError("not all arguments converted during string formatting");
        return output.ToString();
    }
    private static string Decimal(object? value)
    {
        if (PyInt.IsInt(value)) return PyInt.ToBigInteger(value!).ToString(System.Globalization.CultureInfo.InvariantCulture);
        if (value is double number) return PyOps.TruncateToInteger(number, "float").ToString(System.Globalization.CultureInfo.InvariantCulture);
        throw new PyTypeError("%d format: a real number is required");
    }
}
