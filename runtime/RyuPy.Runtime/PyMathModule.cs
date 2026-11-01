// math 首批标量 API：在调用宿主数学函数前后明确处理定义域、无穷和溢出。
// 普通有限输入沿用宿主 libm 精度；跨平台末位误差通过容差验收，不宣称逐位等价。
using System;
using System.Collections.Generic;

namespace RyuPy.Runtime;

/// <summary>math 原生模块的逐 API 实现，不替换编译器内部整数算术 PyMath。</summary>
public static class PyMathModule
{
    private static readonly object NoBase = new();

    /// <summary>构造已实现的公开函数和常量；未列出的 API 不伪造占位实现。</summary>
    public static Dictionary<string, object?> CreateAttributes() => new(StringComparer.Ordinal)
    {
        ["pi"] = Math.PI, ["e"] = Math.E, ["tau"] = 2 * Math.PI,
        ["inf"] = double.PositiveInfinity, ["nan"] = double.NaN,
        ["sqrt"] = Unary("sqrt", x => Domain(x, Math.Sqrt, x < 0)),
        ["fabs"] = Unary("fabs", Math.Abs),
        ["exp"] = Unary("exp", x => Range(x, Math.Exp)),
        ["sin"] = Unary("sin", x => Domain(x, Math.Sin, double.IsInfinity(x))),
        ["cos"] = Unary("cos", x => Domain(x, Math.Cos, double.IsInfinity(x))),
        ["tan"] = Unary("tan", x => Domain(x, Math.Tan, double.IsInfinity(x))),
        ["isfinite"] = One("isfinite", x => double.IsFinite(PyMathConversion.Real(x))),
        ["isinf"] = One("isinf", x => double.IsInfinity(PyMathConversion.Real(x))),
        ["isnan"] = One("isnan", x => double.IsNaN(PyMathConversion.Real(x))),
        ["floor"] = One("floor", x => PyMathConversion.Integral(x, "floor")),
        ["ceil"] = One("ceil", x => PyMathConversion.Integral(x, "ceil")),
        ["trunc"] = One("trunc", x => PyMathConversion.Integral(x, "trunc")),
        ["log10"] = One("log10", x => PyMathConversion.Logarithm(x, true)),
        ["log"] = PyNativeFunctions.Create("log", ["x", "base"], [null, NoBase], 1,
            a => Log(a[0], a[1]), ["posonly", "posonly"]),
        ["pow"] = PyNativeFunctions.Create("pow", ["x", "y"], [null, null], 2,
            a => Power(PyMathConversion.Real(a[0]), PyMathConversion.Real(a[1])), ["posonly", "posonly"]),
    };

    private static PyFunction One(string name, Func<object?, object?> operation) =>
        PyNativeFunctions.Create(name, ["x"], [null], 1, a => operation(a[0]), ["posonly"]);
    private static PyFunction Unary(string name, Func<double, double> operation) =>
        One(name, value => operation(PyMathConversion.Real(value)));
    private static double Domain(double value, Func<double, double> operation, bool invalid)
    {
        if (invalid) throw new PyValueError("math domain error");
        return operation(value);
    }
    private static double Range(double value, Func<double, double> operation)
    {
        double result = operation(value);
        if (double.IsFinite(value) && double.IsInfinity(result)) throw new PyOverflowError("math range error");
        return result;
    }
    private static double Log(object? value, object? basis)
    {
        double numerator = PyMathConversion.Logarithm(value, false);
        if (ReferenceEquals(basis, NoBase)) return numerator;
        double denominator = PyMathConversion.Logarithm(basis, false);
        if (denominator == 0) throw new PyZeroDivisionError("float division by zero");
        return numerator / denominator;
    }
    private static double Power(double x, double y)
    {
        // 这两条恒等式优先于 NaN 传播，与 CPython 的 math.pow 而非复数 ** 对齐。
        if (y == 0 || x == 1) return 1;
        if (double.IsNaN(x) || double.IsNaN(y)) return double.NaN;
        if (x == 0 && y < 0 && double.IsFinite(y)) throw new PyValueError("math domain error");
        if (x < 0 && double.IsFinite(x) && double.IsFinite(y) && y != Math.Truncate(y))
            throw new PyValueError("math domain error");
        double result = Math.Pow(x, y);
        if (double.IsInfinity(result) && double.IsFinite(x) && double.IsFinite(y))
            throw new PyOverflowError("math range error");
        return result;
    }
}
