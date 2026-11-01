using System;
using System.Numerics;

namespace RyuPy.Runtime;

/// <summary>一等 pow 的参数绑定与整模幂；二参沿用现有动态运算协议，模幂避免创建完整幂结果。</summary>
public static class PyPowerBuiltin
{
    private static readonly PyFunction Function = PyNativeFunctions.Create("pow",
        ["base", "exp", "mod"], [null, null, PyNone.Instance], 2, values =>
            Power(values[0]!, values[1]!, values[2]));

    /// <summary>保留关键字参数与重复参数的统一错误检查。</summary>
    public static object? Invoke(object?[] args, string[] names, object?[] values) =>
        PyOps.CallValue(Function, args, names, values);

    /// <summary>浮点幂显式检查 Python 错误；尚未实现 complex 时拒绝而不是返回伪 NaN。</summary>
    public static double FloatPower(double value, double exponent)
    {
        if (value == 0 && exponent < 0 && double.IsFinite(exponent))
            throw new PyZeroDivisionError("0.0 cannot be raised to a negative power");
        if (value < 0 && double.IsFinite(value) && double.IsFinite(exponent)
            && exponent != Math.Truncate(exponent))
            throw new PyNotImplementedError("complex power results are not supported");
        double result = Math.Pow(value, exponent);
        if (double.IsInfinity(result) && double.IsFinite(value) && double.IsFinite(exponent))
            throw new PyOverflowError("numerical result out of range");
        return result;
    }

    private static object Power(object value, object exponent, object? modulus)
    {
        if (modulus is null or PyNone) return PyOps.Binary(PyOps.Pow, value, exponent);
        if (value is PyInstance or PyNativeObject || exponent is PyInstance or PyNativeObject
            || modulus is PyInstance or PyNativeObject)
            throw new PyNotImplementedError("three-argument pow with user numeric protocols is not supported");
        if (!PyInt.IsInt(value) || !PyInt.IsInt(exponent) || !PyInt.IsInt(modulus))
            throw new PyTypeError("pow() 3rd argument not allowed unless all arguments are integers");
        BigInteger number = PyInt.ToBigInteger(value), power = PyInt.ToBigInteger(exponent);
        BigInteger original = PyInt.ToBigInteger(modulus), size = BigInteger.Abs(original);
        if (size.IsZero) throw new PyValueError("pow() 3rd argument cannot be 0");
        number = ((number % size) + size) % size;
        if (power.Sign < 0)
        {
            // 扩展欧几里得求模逆，不依赖浮点近似；模 1 的结果自然为 0。
            BigInteger oldR = size, r = number, oldT = 0, t = 1;
            while (!r.IsZero)
            {
                BigInteger quotient = oldR / r;
                (oldR, r) = (r, oldR - quotient * r);
                (oldT, t) = (t, oldT - quotient * t);
            }
            if (oldR != BigInteger.One) throw new PyValueError("base is not invertible for the given modulus");
            number = ((oldT % size) + size) % size;
            power = BigInteger.Negate(power);
        }
        BigInteger result = BigInteger.ModPow(number, power, size);
        if (original.Sign < 0 && !result.IsZero) result -= size;
        return PyInt.Normalize(result);
    }
}
