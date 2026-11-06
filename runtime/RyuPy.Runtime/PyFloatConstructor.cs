// float 构造绑定与对象协议；只准备本入口自抛错误，不截获用户 __float__/__index__ 正文异常。
using System.Numerics;

namespace RyuPy.Runtime;

/// <summary>float 的有限位置参数绑定与冷对象转换。</summary>
public static class PyFloatConstructor
{
    /// <summary>完整展开后只接受零或一个位置参数，关键字优先拒绝。</summary>
    public static object Invoke(object?[] args, string[] names, object?[] values)
    {
        if (names.Length != 0) throw Prepared(new PyTypeError("float() takes no keyword arguments"));
        if (args.Length > 1) throw Prepared(new PyTypeError($"float expected at most 1 argument, got {args.Length}"));
        return args.Length == 0 ? 0.0 : PyOps.ToFloatValue(args[0]!);
    }

    /// <summary>大整数转换仅在 float 冷路径补溢出帧，共享 ToDouble 的行为不变。</summary>
    public static object FromBigInteger(BigInteger value)
    {
        try { return PyOps.ToDouble(value); }
        catch (PyOverflowError error) { throw Prepared(error); }
    }

    /// <summary>精确文本或普通对象槽；缺 __float__ 才尝试 __index__。</summary>
    public static object ConvertObject(object? value)
    {
        if (value is string or PyBytes or PyByteArray) return PyFloatText.Parse(value);
        if (PyObject.TryMethod(value, "__float__", [], out object? floating))
        {
            if (floating is double) return floating;
            throw Prepared(new PyTypeError($"{PyRepr.TypeName(value)}.__float__ returned non-float (type {PyRepr.TypeName(floating)})"));
        }
        if (PyObject.TryMethod(value, "__index__", [], out object? integer))
        {
            if (integer is bool)
                throw Prepared(new PyNotImplementedError("float(): __index__ returning bool requires DeprecationWarning support"));
            if (integer is BigInteger big) return FromBigInteger(big);
            if (integer is long or int) return PyOps.ToDouble(integer);
            throw Prepared(new PyTypeError($"__index__ returned non-int (type {PyRepr.TypeName(integer)})"));
        }
        if (value is PyStringInstance subtext) return PyFloatText.Parse(subtext.Value);
        if (value is PyMemoryView)
            throw Prepared(new PyNotImplementedError("float(): memoryview input is not implemented"));
        throw Prepared(new PyTypeError($"float() argument must be a string or a real number, not '{PyRepr.TypeName(value)}'"));
    }

    private static PyBaseException Prepared(PyBaseException error) => (PyBaseException)PyErrors.PrepareRaise(error);
}
