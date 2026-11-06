using System;
using System.Globalization;
using System.Text;

namespace RyuPy.Runtime;

/// <summary>
/// 单遍执行有限的 Python 字符串百分号格式，保留逐项转换副作用与参数消费次序。
/// 不复用 CLR 复合格式，不将未实现的宽度或浮点格式悄悄近似处理。
/// </summary>
public static class PyPercentFormat
{
    /// <summary>实现 %s/%r/%a/%d/%i/%u/%%；tuple 是参数表，其它对象是一个参数。</summary>
    public static string Format(string format, object? arguments)
    {
        var output = new StringBuilder(format.Length);
        object?[]? tuple = (arguments as PyTuple)?.Items;
        int count = tuple?.Length ?? 1, used = 0;
        // CPython 在入口固定 mapping slot 资格；转换回调改变类属性不能追溯改变资格。
        bool? mapping = MappingSlot(arguments);
        for (int i = 0; i < format.Length; i++)
        {
            char conversion = format[i];
            if (conversion != '%') { output.Append(conversion); continue; }
            if (++i == format.Length) throw new PyValueError("incomplete format");
            conversion = format[i];
            if (conversion == '%') { output.Append('%'); continue; }
            object? value;
            if (conversion == '(')
            {
                RequireMapping(mapping);
                string key = ReadMappingKey(format, ref i);
                // 完整键先读取，再验证转换字符；缺键错误不能被后续格式错误盖住。
                value = PyOps.GetItem(arguments, key);
                used = count;
                if (i == format.Length) throw new PyValueError("incomplete format");
                conversion = format[i];
                RejectUnsupportedSpecification(conversion);
            }
            else
            {
                RejectUnsupportedSpecification(conversion);
                // 未知转换字符也先检查是否还有参数；不得整串预验证改变异常顺序。
                if (used == count) throw new PyTypeError("not enough arguments for format string");
                value = tuple is null ? arguments : tuple[used];
                used++;
            }
            output.Append(conversion switch
            {
                's' => PyRepr.Str(value),
                'r' => PyRepr.Repr(value),
                'a' => Ascii(PyRepr.Repr(value)),
                'd' or 'i' or 'u' => Decimal(value, conversion),
                'o' or 'x' or 'X' or 'e' or 'E' or 'f' or 'F' or 'g' or 'G' or 'c'
                    or 'h' or 'l' or 'L' => throw Unsupported(),
                _ => throw InvalidConversion(format, i),
            });
        }
        if (used != count && !AllowsUnusedArgument(mapping))
            throw new PyTypeError("not all arguments converted during string formatting");
        return output.ToString();
    }

    private static PyNotImplementedError Unsupported() => new(
        "str percent formatting supports only %s, %r, %a, %d, %i, %u and %% without flags, width or precision");

    private static string Decimal(object? value, char conversion)
    {
        if (PyInt.TryI64(value!, out long small)) return small.ToString(CultureInfo.InvariantCulture);
        if (PyInt.IsInt(value)) return PyInt.ToBigInteger(value!).ToString(CultureInfo.InvariantCulture);
        if (value is double number)
            return PyOps.TruncateToInteger(number, "float").ToString(CultureInfo.InvariantCulture);
        long depth = PyTraceback.CaptureDepth();
        try
        {
            if (PyObject.TryMethod(value, "__int__", [], out object? converted)
                || PyObject.TryMethod(value, "__index__", [], out converted))
            {
                // bool 返回会触发 CPython DeprecationWarning；本切不静默吞警告。
                if (converted is bool) throw new PyNotImplementedError("bool returned from integer conversion in str percent formatting is not supported");
                if (PyInt.IsInt(converted)) return PyInt.ToBigInteger(converted!).ToString(CultureInfo.InvariantCulture);
            }
        }
        catch (PyTypeError)
        {
            // CPython 替换转换协议的 TypeError；被吞异常的编译帧不能泄漏到新错误。
            // 其它异常原样传播，保留原始回调 traceback。
            PyTraceback.PopTo(depth + 1);
            throw DecimalTypeError(value, conversion);
        }
        throw DecimalTypeError(value, conversion);
    }

    private static PyTypeError DecimalTypeError(object? value, char conversion)
    {
        var error = new PyTypeError($"%{conversion} format: a real number is required, not {PyRepr.TypeName(value)}");
        // 被替换的错误要在调用者帧仍在时保存快照，不能等进程报告阶段才补。
        return (PyTypeError)PyErrors.PrepareRaise(error);
    }

    private static void RejectUnsupportedSpecification(char conversion)
    {
        if (conversion is '*' or '.' or '#' or '-' or '+' or ' ' || char.IsAsciiDigit(conversion))
            throw Unsupported();
    }

    private static string ReadMappingKey(string format, ref int index)
    {
        int begin = ++index, depth = 1;
        while (index < format.Length)
        {
            char character = format[index++];
            if (character == '(') depth++;
            else if (character == ')' && --depth == 0) return format[begin..(index - 1)];
        }
        throw new PyValueError("incomplete format key");
    }

    private static void RequireMapping(bool? mapping)
    {
        if (mapping is null) throw UnsupportedNativeMapping();
        if (!mapping.Value) throw new PyTypeError("format requires a mapping");
    }

    private static bool AllowsUnusedArgument(bool? mapping)
    {
        if (mapping is null) throw UnsupportedNativeMapping();
        return mapping.Value;
    }

    private static PyNotImplementedError UnsupportedNativeMapping() => new(
        "mapping-slot classification for this native object in str percent formatting is not supported");

    private static bool? MappingSlot(object? value)
    {
        // 字符串和tuple不作为mapping参数；空tuple仍通过实际零参数计数自然成功。
        // 已知原生布局逐项登记，绝不通过调用用户getitem或遍历对象来探测。
        if (value is PyDict or PyDictInstance or IPyNativeMapping or PyList or PyBytes or PyByteArray or PyRange or PyMemoryView) return true;
        if (value is PyInstance instance) return instance.Class.TryLookup("__getitem__", out _, out _);
        if (value is PyBaseException exception && exception.PythonClass is not null)
            return exception.PythonClass.TryLookup("__getitem__", out _, out _);
        // deque的下标来自sequence slot，不等于CPython的mapping slot。
        if (value is PyDeque) return false;
        if (value is PyNativeObject) return null;
        return false;
    }

    private static string Ascii(string representation)
    {
        var result = new StringBuilder(representation.Length);
        for (int i = 0; i < representation.Length; i++)
        {
            int point = representation[i];
            if (point < 128) { result.Append((char)point); continue; }
            if (char.IsHighSurrogate(representation[i]) && i + 1 < representation.Length
                && char.IsLowSurrogate(representation[i + 1]))
                point = char.ConvertToUtf32(representation[i], representation[++i]);
            result.Append(point <= 255 ? "\\x" : point <= 65535 ? "\\u" : "\\U");
            result.Append(point.ToString(point <= 255 ? "x2" : point <= 65535 ? "x4" : "x8", CultureInfo.InvariantCulture));
        }
        return result.ToString();
    }

    private static PyValueError InvalidConversion(string format, int index)
    {
        int point = format[index];
        if (char.IsHighSurrogate(format[index]) && index + 1 < format.Length
            && char.IsLowSurrogate(format[index + 1]))
            point = char.ConvertToUtf32(format[index], format[index + 1]);
        char visible = point >= 32 && point < 127 ? (char)point : '?';
        return new PyValueError($"unsupported format character '{visible}' (0x{point:x}) at index {CodePointIndex(format, index)}");
    }

    private static int CodePointIndex(string text, int end)
    {
        int count = 0;
        for (int i = 0; i < end; i++, count++)
            if (char.IsHighSurrogate(text[i]) && i + 1 < end && char.IsLowSurrogate(text[i + 1])) i++;
        return count;
    }
}
