// math 的实数协议不同于 float()：不接受字符串，并严格验证 __float__/__index__ 的结果类型。
using System;
using System.Numerics;

namespace RyuPy.Runtime;

internal static class PyMathConversion
{
    internal static double Real(object? value)
    {
        if (value is IPyForeignObject foreign) return (double)foreign.ReadScalar("float");
        if (value is double || PyInt.IsInt(value)) return PyOps.ToDouble(value!);
        if (PyObject.TryMethod(value, "__float__", [], out object? floating))
        {
            if (floating is double number) return number;
            throw new PyTypeError($"__float__ returned non-float (type {PyRepr.TypeName(floating)})");
        }
        if (PyObject.TryMethod(value, "__index__", [], out object? indexed))
        {
            if (PyInt.IsInt(indexed)) return PyOps.ToDouble(indexed!);
            throw new PyTypeError($"__index__ returned non-int (type {PyRepr.TypeName(indexed)})");
        }
        throw new PyTypeError($"must be real number, not {PyRepr.TypeName(value)}");
    }

    internal static object? Integral(object? value, string operation)
    {
        if (PyInt.IsInt(value)) return PyInt.Normalize(PyInt.ToBigInteger(value!));
        if (value is not double && PyObject.TryMethod(value, "__" + operation + "__", [], out object? result))
            return result;
        if (value is not double && operation == "trunc")
            throw new PyTypeError($"type {PyRepr.TypeName(value)} doesn't define __trunc__ method");
        double number = Real(value);
        double rounded = operation switch { "floor" => Math.Floor(number), "ceil" => Math.Ceiling(number), _ => Math.Truncate(number) };
        return PyInt.Normalize(PyOps.TruncateToInteger(rounded, "float"));
    }

    internal static double Logarithm(object? value, bool decimalBase)
    {
        // 巨大整数求对数不先转换成 double，否则本来有限的答案会在转换时溢出。
        if (value is BigInteger integer)
        {
            if (integer.Sign <= 0) throw new PyValueError("math domain error");
            double converted = (double)integer;
            if (double.IsInfinity(converted)) return decimalBase ? BigInteger.Log10(integer) : BigInteger.Log(integer);
        }
        double number = Real(value);
        if (number <= 0) throw new PyValueError("math domain error");
        return decimalBase ? Math.Log10(number) : Math.Log(number);
    }
}
