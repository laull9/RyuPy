using System;
using System.Globalization;

namespace RyuPy.Runtime;

/// <summary>与 CPython 对齐的格式化规则。</summary>
public static class PyFormat
{
    /// <summary>
    /// CPython <c>repr(float)</c>：最短往返表示；十进制指数在 [-4, 16) 内用定点，否则用科学计数法。
    /// 例：<c>1.0</c>、<c>0.1</c>、<c>1e+16</c>、<c>1e-05</c>、<c>inf</c>、<c>nan</c>。
    /// </summary>
    public static string FloatRepr(double value)
    {
        if (double.IsNaN(value)) return "nan";
        if (double.IsPositiveInfinity(value)) return "inf";
        if (double.IsNegativeInfinity(value)) return "-inf";

        // TODO(M1.4)：.NET 的 "R" 已是最短往返数字，但指数格式与 CPython 不同
        // （.NET: "1E+16"，Python: "1e+16"；.NET: "1E-05"，Python: "1e-05"），且整数值缺少 ".0"。
        // 正确做法：取出最短数字串与十进制指数，再按 CPython 规则重新排版。用 e2e 用例 floats.py 验证。
        string s = value.ToString("R", CultureInfo.InvariantCulture);
        if (s.IndexOfAny(['.', 'E', 'e']) < 0) s += ".0";
        return s;
    }
}
