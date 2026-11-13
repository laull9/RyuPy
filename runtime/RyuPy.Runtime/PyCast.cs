using System;
using System.Numerics;

namespace RyuPy.Runtime;

/// <summary>
/// 动态值 → 静态槽位的拆箱转换。
/// </summary>
/// <remarks>
/// 静态特化路径（Phase 1 起）的局部变量与参数是强类型的。当动态值流入静态槽位时，
/// 编译器插入一次显式拆箱；类型不符必须报 Python <c>TypeError</c>，不能靠 CLR 的
/// <c>unbox.any</c> 抛 <c>InvalidCastException</c>——那会把 .NET 类型名泄漏给用户
/// （语义陷阱 #19 的边界）。
///
/// 这些消息在 CPython 里没有对应文本（CPython 没有静态参数类型），因此按「函数实参」口吻
/// 组织，并在 semantics.md 登记为 RyuPy 自有诊断文本。
/// </remarks>
public static class PyCast
{
    /// <summary>拆箱为 <c>int64</c>：接受 <c>int</c> 与 <c>bool</c>。</summary>
    public static long ToInt64(object? value)
    {
        switch (value)
        {
            case IPyForeignObject foreign: return ToInt64(foreign.ReadScalar("int"));
            case bool b:
                return b ? 1 : 0;
            case long l:
                return l;
            case int i:
                return i;
            case BigInteger big when big >= long.MinValue && big <= long.MaxValue:
                return (long)big;
            case BigInteger:
                throw new PyOverflowError("int too large to convert to int64");
            default:
                throw Mismatch("int", value);
        }
    }

    /// <summary>拆箱为 <c>float64</c>：接受 <c>int</c>、<c>bool</c> 与 <c>float</c>。</summary>
    public static double ToFloat64(object? value) => value switch
    {
        IPyForeignObject foreign => (double)foreign.ReadScalar("float"),
        bool b => b ? 1.0 : 0.0,
        long l => l,
        int i => i,
        double d => d,
        BigInteger big => PyOps.ToDouble(big),
        _ => throw Mismatch("float", value),
    };

    /// <summary>拆箱为 <c>bool</c>：按 Python 真值规则（任何值都合法）。</summary>
    public static bool ToBool(object? value) => PyOps.Truthy(value);

    /// <summary>拆箱为 <c>str</c>：只接受字符串，不做隐式 <c>str()</c>。</summary>
    public static string ToStr(object? value) => value is IPyForeignObject foreign
        ? (string)foreign.ReadScalar("str") : value as string ?? throw Mismatch("str", value);

    /// <summary><c>range()</c> 步长拆箱为 <c>int64</c>，并检查不能为 0。</summary>
    /// <remarks>
    /// 静态路径在编译期就能拒绝字面量 0，动态路径只能在运行时检查——
    /// 否则步长为 0 的计数循环会变成死循环。
    /// </remarks>
    public static long RangeStep(object? value)
    {
        long step = ToInt64(value);
        if (step == 0)
        {
            throw new PyValueError("range() arg 3 must not be zero");
        }
        return step;
    }

    private static PyTypeError Mismatch(string expected, object? value) =>
        new($"argument must be '{expected}', got '{PyRepr.TypeName(value)}'");
}
