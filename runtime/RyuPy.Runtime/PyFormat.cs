using System;
using System.Globalization;

namespace RyuPy.Runtime;

/// <summary>与 CPython 对齐的格式化规则。</summary>
public static class PyFormat
{
    /// <summary>
    /// CPython <c>repr(float)</c>：最短往返表示；十进制指数在 [-4, 16) 内用定点，否则用科学计数法。
    /// 例：<c>1.0</c>、<c>0.1</c>、<c>1e+16</c>、<c>1e-05</c>、<c>inf</c>、<c>nan</c>、<c>-0.0</c>。
    /// </summary>
    public static string FloatRepr(double value)
    {
        if (double.IsNaN(value)) return "nan";
        if (double.IsPositiveInfinity(value)) return "inf";
        if (double.IsNegativeInfinity(value)) return "-inf";

        // 处理正负零
        if (value == 0.0)
        {
            return double.IsNegative(value) ? "-0.0" : "0.0";
        }

        bool isNeg = double.IsNegative(value);
        double absVal = isNeg ? -value : value;

        // 获取 .NET Invariant 默认最短表示
        string s = absVal.ToString(CultureInfo.InvariantCulture);

        // 如果已经包含科学计数法的 E
        int eIdx = s.IndexOfAny(['E', 'e']);
        if (eIdx >= 0)
        {
            // 将大写 E 转换为小写 e，并保证指数格式为 e+05 / e-05 等
            string mantissa = s[..eIdx];
            string expPart = s[(eIdx + 1)..];
            string result = $"{mantissa}e{expPart}";
            return isNeg ? "-" + result : result;
        }

        // 检查指数范围：CPython 规定 exponent 在 [-4, 16) 内为定点，>= 16 为科学计数法
        // 当 absVal >= 1e16 且 s 仍为定点输出时，需要转为科学计数法
        if (absVal >= 1e16)
        {
            string sci = ConvertFixedToScientific(s);
            return isNeg ? "-" + sci : sci;
        }

        // 定点数：若没有小数点，必须补上 ".0"
        if (!s.Contains('.'))
        {
            s += ".0";
        }

        return isNeg ? "-" + s : s;
    }

    /// <summary>
    /// 将形如 "10000000000000000" 或 "12500000000000000.5" 的定点数转换为 Python 风格科学计数法
    /// </summary>
    private static string ConvertFixedToScientific(string s)
    {
        int dotIdx = s.IndexOf('.');
        string intPart = dotIdx >= 0 ? s[..dotIdx] : s;
        string fracPart = dotIdx >= 0 ? s[(dotIdx + 1)..] : "";

        // 指数是整数部分长度 - 1
        int exp = intPart.Length - 1;

        // 组合有效数字
        char firstDigit = intPart[0];
        string restDigits = intPart[1..] + fracPart;

        // 去除末尾多余的 0
        restDigits = restDigits.TrimEnd('0');

        string mantissa = restDigits.Length > 0 ? $"{firstDigit}.{restDigits}" : $"{firstDigit}";
        return $"{mantissa}e+{exp:D2}";
    }
}
