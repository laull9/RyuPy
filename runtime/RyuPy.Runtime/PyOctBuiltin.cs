// oct的中性整数协议：精确整数快路，类型级__index__单次调用，复用二幂格式内核并保留警告/错误现场。
using System.Numerics;

namespace RyuPy.Runtime;

internal static class PyOctBuiltin
{
    internal static string Invoke(object?[] args, string[] names)
    {
        if (names.Length != 0) throw Own(new PyTypeError("oct() takes no keyword arguments"));
        if (args.Length != 1)
            throw Own(new PyTypeError($"oct() takes exactly one argument ({args.Length} given)"));
        object? integer = args[0];
        if (!PyInt.IsInt(integer))
        {
            // 不读实例同名字段，也不调用__int__/原对象__format__；用户槽异常保持原身份传播。
            if (integer is IPyForeignObject foreign) integer = foreign.ReadScalar("int");
            else if (integer is PyInstance { ForeignInstance: IPyForeignObject foreignInstance })
                integer = foreignInstance.ReadScalar("int");
            else if (!PyIndexSlot.TryInvoke(integer, out integer))
                throw Own(new PyTypeError($"'{PyRepr.TypeName(args[0])}' object cannot be interpreted as an integer"));
            if (!PyInt.IsInt(integer))
                throw Own(new PyTypeError($"__index__ returned non-int (type {PyRepr.TypeName(integer)})"));
            if (integer is bool) PyWarnings.IndexReturnedStrictSubclass("bool");
        }
        return PyBuiltinFormat.AlternateInteger(PyInt.ToBigInteger(integer!), 8, "0o");
    }

    private static T Own<T>(T error) where T : PyBaseException => (T)PyErrors.PrepareRaise(error);
}

internal static partial class PyBuiltinFormat
{
    // 已验证的按位二幂转换不经过十进制字符串，也不为每个digit复制/除法BigInteger。
    internal static string AlternateInteger(BigInteger value, int radix, string prefix)
    {
        string digits = BaseDigits(BigInteger.Abs(value), radix, upper: false);
        Capacity((long)digits.Length + prefix.Length + (value.Sign < 0 ? 1 : 0));
        return value.Sign < 0 ? "-" + prefix + digits : prefix + digits;
    }
}
