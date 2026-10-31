using System;
using System.Globalization;
using System.Numerics;
using System.Text;

namespace RyuPy.Runtime;

/// <summary>
/// f-string 的单段格式化：<c>format(value, spec)</c> 与 <c>!r</c> / <c>!s</c> 转换。
/// </summary>
/// <remarks>
/// 支持的格式说明符（这是 CPython 语法的常用子集）：
/// <c>[[fill]align][sign][#][0][width][,][.precision][type]</c>。
/// <list type="bullet">
/// <item>align：<c>&lt;</c> 左对齐、<c>&gt;</c> 右对齐、<c>^</c> 居中、<c>=</c> 符号后填充；</item>
/// <item>type：整数 <c>d b o x X c</c>，浮点 <c>f e E g G %</c>，字符串 <c>s</c>；</item>
/// <item>未识别的规格抛 <c>ValueError</c>（与 CPython 一致），不静默忽略。</item>
/// </list>
/// 不做完整实现的部分（嵌套规格、本地化 <c>n</c>、复杂舍入）按需再补；当前覆盖项目里
/// 常见的宽度、补零、精度与进制。
/// </remarks>
public static partial class PyOps
{
    /// <summary>格式化一段 f-string 插值。</summary>
    /// <param name="value">插值表达式的值。</param>
    /// <param name="spec">格式说明符（无则为空串）。</param>
    /// <param name="conversion">
    /// <c>!r</c> / <c>!s</c> / <c>!a</c> 的字节值；没有转换时为 0。
    /// </param>
    public static string FormatValue(object? value, string spec, int conversion)
    {
        if (conversion == 'r' || conversion == 'a')
        {
            string text = conversion == 'a' ? AsciiRepr(value) : PyRepr.Repr(value);
            return ApplyTextSpec(text, spec);
        }
        if (conversion == 's')
        {
            return ApplyTextSpec(PyRepr.Str(value), spec);
        }
        return value switch
        {
            bool b => FormatBool(b, spec),
            long or int or BigInteger => FormatInteger(PyInt.ToBigInteger(value), spec),
            double d => FormatFloat(d, spec),
            float f => FormatFloat(f, spec),
            string s => ApplyTextSpec(s, spec),
            _ => ApplyTextSpec(PyRepr.Str(value), spec),
        };
    }

    private static string FormatBool(bool value, string spec)
    {
        var parsed = ParseSpec(spec);
        // `f"{True:s}"` 输出 True；数值类型（d/x/o）按 1/0 处理
        if (parsed.Type is '\0' or 's')
        {
            return ApplyTextSpec(value ? "True" : "False", spec);
        }
        return FormatInteger(value ? BigInteger.One : BigInteger.Zero, spec);
    }

    private static string FormatInteger(BigInteger value, string spec)
    {
        var parsed = ParseSpec(spec);
        char type = parsed.Type == '\0' ? 'd' : parsed.Type;
        bool negative = value.Sign < 0;
        BigInteger magnitude = BigInteger.Abs(value);

        string digits = type switch
        {
            'd' or 'n' => magnitude.ToString(CultureInfo.InvariantCulture),
            'b' => ToBase(magnitude, 2),
            'o' => ToBase(magnitude, 8),
            'x' => ToBase(magnitude, 16).ToLowerInvariant(),
            'X' => ToBase(magnitude, 16).ToUpperInvariant(),
            'c' => char.ConvertFromUtf32((int)magnitude),
            _ => throw new PyValueError($"Unknown format code '{type}' for object of type 'int'"),
        };
        if (parsed.Group && type is 'd' or 'n')
        {
            digits = GroupThousands(digits);
        }

        string prefix = string.Empty;
        if (parsed.Alternate)
        {
            prefix = type switch
            {
                'b' => "0b",
                'o' => "0o",
                'x' => "0x",
                'X' => "0X",
                _ => string.Empty,
            };
        }

        string sign = negative ? "-" : parsed.Sign switch
        {
            '+' => "+",
            ' ' => " ",
            _ => string.Empty,
        };
        return PadNumeric(sign, prefix, digits, parsed);
    }

    private static string FormatFloat(double value, string spec)
    {
        var parsed = ParseSpec(spec);
        if (double.IsNaN(value))
        {
            return PadText(PyFormat.FloatRepr(value), parsed);
        }
        if (double.IsInfinity(value))
        {
            string text = value > 0 ? "inf" : "-inf";
            return PadText(text, parsed);
        }

        char type = parsed.Type;
        int precision = parsed.Precision;
        string digits;
        if (type == '%')
        {
            double percent = value * 100;
            digits = percent.ToString(
                    "F" + (precision < 0 ? 6 : precision),
                    CultureInfo.InvariantCulture
                ) + "%";
        }
        else if (type is 'f' or 'F')
        {
            digits = value.ToString(
                "F" + (precision < 0 ? 6 : precision),
                CultureInfo.InvariantCulture
            );
        }
        else if (type is 'e' or 'E')
        {
            digits = NormalizeExponent(
                value.ToString(
                    "E" + (precision < 0 ? 6 : precision),
                    CultureInfo.InvariantCulture
                ),
                type == 'E'
            );
        }
        else if (type is 'g' or 'G')
        {
            digits = precision < 0
                ? PyFormat.FloatRepr(value)
                : value.ToString("G" + precision, CultureInfo.InvariantCulture);
        }
        else if (precision >= 0)
        {
            // 无类型但有精度：按定点输出（足以覆盖常见用例）
            digits = value.ToString("F" + precision, CultureInfo.InvariantCulture);
        }
        else
        {
            digits = PyFormat.FloatRepr(value);
        }

        bool negative = digits.StartsWith('-');
        if (negative)
        {
            digits = digits[1..];
        }
        if (parsed.Group)
        {
            int dot = digits.IndexOf('.');
            string whole = dot < 0 ? digits : digits[..dot];
            string rest = dot < 0 ? string.Empty : digits[dot..];
            digits = GroupThousands(whole) + rest;
        }
        string sign = negative ? "-" : parsed.Sign switch
        {
            '+' => "+",
            ' ' => " ",
            _ => string.Empty,
        };
        return PadNumeric(sign, string.Empty, digits, parsed);
    }

    private static string ApplyTextSpec(string text, string spec)
    {
        var parsed = ParseSpec(spec);
        if (parsed.Type is not ('\0' or 's'))
        {
            throw new PyValueError($"Unknown format code '{parsed.Type}' for object of type 'str'");
        }
        if (parsed.Precision >= 0 && text.Length > parsed.Precision)
        {
            text = text[..parsed.Precision];
        }
        return PadText(text, parsed);
    }

    private static string PadText(string text, FormatSpec spec)
    {
        if (spec.Width <= text.Length)
        {
            return text;
        }
        int padding = spec.Width - text.Length;
        return spec.Align switch
        {
            '<' => text + new string(spec.Fill, padding),
            '^' => new string(spec.Fill, padding / 2)
                + text
                + new string(spec.Fill, padding - padding / 2),
            _ => new string(spec.Fill, padding) + text,
        };
    }

    private static string PadNumeric(string sign, string prefix, string digits, FormatSpec spec)
    {
        string body = prefix + digits;
        int length = sign.Length + body.Length;
        if (spec.Width <= length)
        {
            return sign + body;
        }
        int padding = spec.Width - length;
        if (spec.Align == '<')
        {
            return sign + body + new string(spec.Fill, padding);
        }
        if (spec.Align == '^')
        {
            int left = padding / 2;
            return new string(spec.Fill, left)
                + sign
                + body
                + new string(spec.Fill, padding - left);
        }
        if (spec.Align == '=' || spec.Zero)
        {
            // 符号后填充零（`{x:08d}`）
            return sign + new string(spec.Fill, padding) + body;
        }
        return new string(spec.Fill, padding) + sign + body;
    }

    private static string ToBase(BigInteger value, int radix)
    {
        if (value.IsZero)
        {
            return "0";
        }
        const string Digits = "0123456789abcdefghijklmnopqrstuvwxyz";
        var builder = new StringBuilder();
        BigInteger current = value;
        while (!current.IsZero)
        {
            current = BigInteger.DivRem(current, radix, out BigInteger remainder);
            builder.Insert(0, Digits[(int)remainder]);
        }
        return builder.ToString();
    }

    private static string GroupThousands(string digits)
    {
        if (digits.Length <= 3)
        {
            return digits;
        }
        var builder = new StringBuilder();
        int first = digits.Length % 3 == 0 ? 3 : digits.Length % 3;
        builder.Append(digits[..first]);
        for (int i = first; i < digits.Length; i += 3)
        {
            builder.Append(',');
            builder.Append(digits, i, 3);
        }
        return builder.ToString();
    }

    /// <summary>把 .NET 的三位指数（<c>E+004</c>）改成 Python 的两位（<c>e+04</c>）。</summary>
    private static string NormalizeExponent(string text, bool upper)
    {
        int index = text.IndexOfAny(['E', 'e']);
        if (index < 0)
        {
            return text;
        }
        string mantissa = text[..index];
        string exponent = text[(index + 1)..];
        char sign = exponent[0];
        string digits = exponent[1..].TrimStart('0');
        if (digits.Length < 2)
        {
            digits = digits.PadLeft(2, '0');
        }
        return $"{mantissa}{(upper ? 'E' : 'e')}{sign}{digits}";
    }

    private static string AsciiRepr(object? value)
    {
        string repr = PyRepr.Repr(value);
        var builder = new StringBuilder(repr.Length);
        foreach (char ch in repr)
        {
            if (ch >= 0x20 && ch < 0x7f)
            {
                builder.Append(ch);
            }
            else if (ch <= 0xff)
            {
                builder.Append("\\x").Append(((int)ch).ToString("x2"));
            }
            else
            {
                builder.Append("\\u").Append(((int)ch).ToString("x4"));
            }
        }
        return builder.ToString();
    }

    /// <summary>解析后的格式说明符。</summary>
    private struct FormatSpec
    {
        public char Fill;
        public char Align;
        public char Sign;
        public bool Alternate;
        public bool Zero;
        public int Width;
        public bool Group;
        public int Precision;
        public char Type;
    }

    private static FormatSpec ParseSpec(string spec)
    {
        var result = new FormatSpec
        {
            Fill = ' ',
            Align = '\0',
            Sign = '\0',
            Alternate = false,
            Zero = false,
            Width = -1,
            Group = false,
            Precision = -1,
            Type = '\0',
        };
        int i = 0;
        // [[fill]align]
        if (spec.Length >= 2 && IsAlign(spec[1]))
        {
            result.Fill = spec[0];
            result.Align = spec[1];
            i = 2;
        }
        else if (spec.Length >= 1 && IsAlign(spec[0]))
        {
            result.Align = spec[0];
            i = 1;
        }
        // [sign]
        if (i < spec.Length && spec[i] is '+' or '-' or ' ')
        {
            result.Sign = spec[i++];
        }
        // [#]
        if (i < spec.Length && spec[i] == '#')
        {
            result.Alternate = true;
            i++;
        }
        // [0]
        if (i < spec.Length && spec[i] == '0')
        {
            result.Zero = true;
            result.Fill = '0';
            i++;
        }
        // [width]
        int widthStart = i;
        while (i < spec.Length && char.IsAsciiDigit(spec[i]))
        {
            i++;
        }
        if (i > widthStart)
        {
            result.Width = int.Parse(spec[widthStart..i], CultureInfo.InvariantCulture);
        }
        // [,]
        if (i < spec.Length && spec[i] is ',' or '_')
        {
            result.Group = true;
            i++;
        }
        // [.precision]
        if (i < spec.Length && spec[i] == '.')
        {
            i++;
            int start = i;
            while (i < spec.Length && char.IsAsciiDigit(spec[i]))
            {
                i++;
            }
            if (i == start)
            {
                throw new PyValueError($"Invalid format specifier '{spec}'");
            }
            result.Precision = int.Parse(spec[start..i], CultureInfo.InvariantCulture);
        }
        // [type]
        if (i < spec.Length)
        {
            char type = spec[i++];
            if (!"bcdoxXeEfFgGns%".Contains(type))
            {
                throw new PyValueError($"Unknown format code '{type}'");
            }
            result.Type = type;
        }
        if (i != spec.Length)
        {
            throw new PyValueError($"Invalid format specifier '{spec}'");
        }
        return result;
    }

    private static bool IsAlign(char ch) => ch is '<' or '>' or '^' or '=';
}
