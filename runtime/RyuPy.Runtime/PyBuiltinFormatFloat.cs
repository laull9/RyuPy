// 显式g/G复用.NET10正确舍入的标准数字后端；Python精度、特殊数和padding由本层控制。
using System;
using System.Globalization;

namespace RyuPy.Runtime;

internal static partial class PyBuiltinFormat
{
    // .NET10标准数字格式参数的实际精度范围，不是自行选择的语义截断上限。
    // https://raw.githubusercontent.com/dotnet/runtime/v10.0.0/src/libraries/Common/src/System/Number.Formatting.Common.cs
    private const int BackendMaxPrecision = 999_999_999;

    private static string GeneralFloat(double value, Spec spec)
    {
        // 调用者已经检查句法、接收类型和CP的INT_MAX精度边界；nan/inf也不能绕过这些检查。
        if (spec.Type is not ("g" or "G") || spec.Alternate || spec.CoerceZero || spec.Group != '\0')
            throw Own(new PyNotImplementedError("format: this float specification is not supported yet"));
        // 特殊数及零不调用数字后端，大precision仍是常数长度结果；只限制非零finite。
        if (double.IsFinite(value) && value != 0.0 && spec.Precision > BackendMaxPrecision)
            throw Own(new PyNotImplementedError("format: precision exceeds the numeric backend limit"));
        int width = Width(spec);
        int precision = spec.Precision < 0 ? 6 : Math.Max(1, (int)spec.Precision);
        bool upper = spec.Type == "G";
        // nan忽略输入signbit；负零仍有负号，不能用value<0判断。
        bool negative = !double.IsNaN(value) && double.IsNegative(value);
        string digits;
        if (double.IsNaN(value)) digits = upper ? "NAN" : "nan";
        else if (double.IsInfinity(value)) digits = upper ? "INF" : "inf";
        else if (value == 0.0) digits = "0";
        else
        {
            // G0/default在.NET是最短往返，不等同Python的p=1/default6；只传显式有效精度。
            // 标准G按舍入后的scale选定点/科学，指数至少两位，避免二次round或parse-double。
            string format = precision == 6 ? (upper ? "G6" : "g6")
                : spec.Type + precision.ToString(CultureInfo.InvariantCulture);
            digits = Math.Abs(value).ToString(format, CultureInfo.InvariantCulture);
        }
        string sign = negative ? "-" : spec.Sign == '+' ? "+" : spec.Sign == ' ' ? " " : "";
        char align = spec.Align == '\0' ? (spec.Zero ? '=' : '>') : spec.Align;
        string fill = spec.Zero && !spec.ExplicitFill ? "0" : spec.Fill;
        int padding = Math.Max(0, width - sign.Length - digits.Length);
        // 最终UTF-16容量先于任何sign/body/fill拼接；不捕获OOM或重试。
        Capacity((long)sign.Length + digits.Length + (long)fill.Length * padding);
        if (align == '=') return sign + Repeat(fill, padding) + digits;
        return Pad(sign + digits, padding, fill, align);
    }
}
