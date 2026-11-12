// Decimal有限文本：保留符号、尾零和指数；非法文本需decimal信号层，当前明确NI而非伪ValueError。
using System;
using System.Globalization;
using System.Numerics;
using System.Text;

namespace RyuPy.Runtime;

internal static class PyDecimalText
{
    // 与64位CP decimal构造表示域一致；不是Context精度/舍入，也不限制±10^9合法指数。
    internal const long MaximumAdjustedExponent = 999999999999999999;
    internal const long MinimumExponent = -1999999999999999997;

    internal static PyDecimal Parse(string input)
    {
        // CP Decimal先去外围Unicode空白，再忽略任意位置下划线；Unicode Nd按十进制数字归一。
        string trimmed = input.Trim();
        var normalized = new StringBuilder(trimmed.Length);
        for (int i = 0; i < trimmed.Length; i++)
        {
            char ch = trimmed[i];
            if (ch == '_') continue;
            int digit = CharUnicodeInfo.GetDecimalDigitValue(trimmed, i);
            if (digit >= 0)
            {
                normalized.Append((char)('0' + digit));
                if (char.IsHighSurrogate(ch) && i + 1 < trimmed.Length && char.IsLowSurrogate(trimmed[i + 1])) i++;
            }
            else normalized.Append(ch);
        }
        string text = normalized.ToString();
        int position = 0;
        bool negative = false;
        if (text.Length != 0 && text[0] is '+' or '-') { negative = text[0] == '-'; position++; }
        string rest = text[position..];
        if (rest.StartsWith("nan", StringComparison.OrdinalIgnoreCase)
            || rest.StartsWith("snan", StringComparison.OrdinalIgnoreCase)
            || rest.StartsWith("inf", StringComparison.OrdinalIgnoreCase))
            throw PyDecimal.Unsupported("non-finite construction and decimal signals");
        var digits = new StringBuilder(text.Length);
        bool dot = false, any = false;
        int fractional = 0;
        while (position < text.Length && text[position] is not ('e' or 'E'))
        {
            char ch = text[position++];
            if (ch == '.' && !dot) { dot = true; continue; }
            if (ch is < '0' or > '9') throw Invalid();
            digits.Append(ch); any = true;
            if (dot) fractional++;
        }
        if (!any) throw Invalid();
        BigInteger exponent = BigInteger.Zero;
        if (position < text.Length)
        {
            position++;
            int start = position;
            if (position < text.Length && text[position] is '+' or '-') position++;
            int firstDigit = position;
            while (position < text.Length && text[position] is >= '0' and <= '9') position++;
            if (position == firstDigit || position != text.Length) throw Invalid();
            exponent = BigInteger.Parse(text.AsSpan(start), NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture);
        }
        exponent -= fractional;
        string coefficient = digits.ToString().TrimStart('0');
        if (coefficient.Length == 0) coefficient = "0";
        BigInteger adjusted = exponent + coefficient.Length - 1;
        if (exponent < MinimumExponent || exponent > MaximumAdjustedExponent || adjusted > MaximumAdjustedExponent)
            throw PyDecimal.Unsupported("construction outside finite CP exponent range requires InvalidOperation");
        return new PyDecimal(negative, coefficient, (long)exponent);
    }

    internal static string Format(PyDecimal value)
    {
        string digits = value.Digits;
        long exponent = value.Exponent;
        long adjusted = exponent + digits.Length - 1;
        string sign = value.Negative ? "-" : "";
        if (exponent <= 0 && adjusted >= -6)
        {
            long point = digits.Length + exponent;
            // 此分支输出长度由已存在系数长度+至多六个前导零限定，不随巨大指数分配。
            if (point <= 0) return sign + "0." + new string('0', checked((int)-point)) + digits;
            if (point == digits.Length) return sign + digits;
            return sign + digits.Insert((int)point, ".");
        }
        string mantissa = digits.Length == 1 ? digits : digits.Insert(1, ".");
        return sign + mantissa + "E" + (adjusted >= 0 ? "+" : "") + adjusted.ToString(CultureInfo.InvariantCulture);
    }

    private static PyNotImplementedError Invalid() => PyDecimal.Unsupported("invalid text requires decimal.InvalidOperation");
}
