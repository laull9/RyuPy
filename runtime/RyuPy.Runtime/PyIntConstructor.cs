// int 的完整冷参数绑定与对象整数协议；单参数 primitive 数值转换仍使用原 PyOps 快路径。
using System;
using System.Numerics;

namespace RyuPy.Runtime;

/// <summary>int 构造参数的冷绑定和用户整数转换协议。</summary>
public static class PyIntConstructor
{
    /// <summary>入口只接已求值/已展开参数，不执行星号解释器，也不静默忽略多余参数。</summary>
    public static object Invoke(object?[] args, string[] names, object?[] values)
    {
        try { return InvokeCore(args, names, values); }
        catch (PyBaseException error) { throw (PyBaseException)PyErrors.PrepareRaise(error); }
    }

    private static object InvokeCore(object?[] args, string[] names, object?[] values)
    {
        if (names.Length != values.Length) throw new PyTypeError("invalid int() keyword arguments");
        if (args.Length > 2 || names.Length > 2 - args.Length)
            throw new PyTypeError($"int() takes at most 2 arguments ({(long)args.Length + names.Length} given)");
        bool hasBase = args.Length == 2;
        object? radix = hasBase ? args[1] : null;
        foreach (string name in names)
            if (name != "base") throw new PyTypeError($"'{name}' is an invalid keyword argument for int()");
        for (int index = 0; index < names.Length; index++)
        {
            if (hasBase) throw new PyTypeError("int() got multiple values for argument 'base'");
            hasBase = true; radix = values[index];
        }
        if (args.Length == 0)
        {
            if (hasBase) throw new PyTypeError("int() missing string argument");
            return 0L;
        }
        if (!hasBase) return PyOps.ToIntValue(args[0]!);
        int numberBase = Base(radix);
        // bytearray 的读取/快照必须晚于 base.__index__，该回调可以合法修改文本。
        if (args[0] is PyStringInstance subtext) return PyIntText.Parse(subtext.Value, numberBase);
        if (args[0] is not (string or PyBytes or PyByteArray))
            throw new PyTypeError("int() can't convert non-string with explicit base");
        return PyIntText.Parse(args[0]!, numberBase);
    }

    private static int Base(object? value)
    {
        object? integer = value;
        if (integer is not (bool or int or long or BigInteger))
        {
            if (!PyObject.TryMethod(value, "__index__", [], out integer))
                throw new PyTypeError($"'{PyRepr.TypeName(value)}' object cannot be interpreted as an integer");
            integer = Result(integer, "__index__");
        }
        BigInteger number = PyInt.ToBigInteger(integer!);
        if (number != 0 && (number < 2 || number > 36))
            throw new PyValueError("int() base must be >= 2 and <= 36, or 0");
        return (int)number;
    }

    /// <summary>ToIntValue 的非 primitive 后备；不改变数值热分支或其它构造器。</summary>
    public static object ConvertObject(object? value)
    {
        try
        {
            if (value is string or PyBytes or PyByteArray) return PyIntText.Parse(value, 10);
            if (PyObject.TryMethod(value, "__int__", [], out object? result)) return Result(result, "__int__");
            if (PyObject.TryMethod(value, "__index__", [], out result)) return Result(result, "__index__");
            if (HasTrunc(value)) throw new PyNotImplementedError("int() __trunc__ delegation and its deprecation warning are not supported yet");
            if (value is PyStringInstance subtext) return PyIntText.Parse(subtext.Value, 10);
            if (value is PyMemoryView) throw new PyNotImplementedError("int() memoryview parsing is not supported yet");
            throw new PyTypeError($"int() argument must be a string, a bytes-like object or a real number, not '{PyRepr.TypeName(value)}'");
        }
        catch (PyBaseException error) { throw (PyBaseException)PyErrors.PrepareRaise(error); }
    }

    private static object Result(object? value, string slot)
    {
        if (value is bool) throw new PyNotImplementedError($"int() {slot} returning bool requires a deprecation warning");
        if (value is int or long or BigInteger) return value;
        throw new PyTypeError($"{slot} returned non-int (type {PyRepr.TypeName(value)})");
    }

    private static bool HasTrunc(object? value)
    {
        if (value is PyInstance instance) return instance.Class.TryLookup("__trunc__", out _, out _);
        if (value is PyBaseException error && error.PythonClass is not null)
            return error.PythonClass.TryLookup("__trunc__", out _, out _);
        if (value is not PyNativeObject native) return false;
        return PyObject.TryGetNativeSpecial(native, "__trunc__", out _);
    }
}
