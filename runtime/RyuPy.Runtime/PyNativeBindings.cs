// 固定 i32,i32→i64 声明的冷协议边界；不提供通用FFI解释器或用户数值转换。
using System;
using System.Numerics;
using System.Runtime.ExceptionServices;

namespace RyuPy.Runtime;

/// <summary>生成的固定数值原生绑定所用的类型、工厂与错误冷边界。</summary>
public static partial class PyNativeBindings
{
    /// <summary>仅实际整数表示；bool/用户协议不进入C，BigInteger先比较范围再转换。</summary>
    public static int CheckedI32(object? value, string function, string parameter)
    {
        value = ReadExactScalar(value, "exact_int");
        switch (value)
        {
            case int small:
                return small;
            case long integer:
                if (integer >= int.MinValue && integer <= int.MaxValue) return (int)integer;
                throw Prepared(new PyOverflowError($"{function}() argument '{parameter}' is outside signed 32-bit range"));
            case BigInteger big:
                if (big >= int.MinValue && big <= int.MaxValue) return (int)big;
                throw Prepared(new PyOverflowError($"{function}() argument '{parameter}' is outside signed 32-bit range"));
            default:
                // 不格式化value或调用__index__/__int__/__repr__，错误本身也无用户副作用。
                throw Prepared(new PyTypeError($"{function}() argument '{parameter}' must be an actual integer, not bool or a converted value"));
        }
    }

    // C ABI只读取外部精确标量的载荷；普通原生值保持现有严格检查和常用热路。
    internal static object? ReadExactScalar(object? value, string kind)
    {
        if (value is not IPyForeignObject foreign) return value;
        try { return foreign.ReadScalar(kind); }
        catch (PyBaseException error) { PyErrors.PrepareRaise(error); throw; }
    }

    /// <summary>仅三个已声明CLR加载错误变成Python ImportError；保留库定位与底层加载原因。</summary>
    public static PyBaseException LoadFailure(Exception error, string library, string symbol)
    {
        if (error is DllNotFoundException or EntryPointNotFoundException or BadImageFormatException)
            return Prepared(new PyImportError($"cannot load declared native library '{library}' symbol '{symbol}': {error.Message}"));
        // 生成wrapper只有三种typed catch；若调用者误用此边界，仍保原类型/对象/栈，不宽映射。
        ExceptionDispatchInfo.Capture(error).Throw();
        throw new InvalidOperationException("unreachable after rethrow");
    }

    private static PyBaseException Prepared(PyBaseException error) =>
        (PyBaseException)PyErrors.PrepareRaise(error);
}
