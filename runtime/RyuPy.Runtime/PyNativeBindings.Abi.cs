// v3 函数对象与返回物化；用常规 C# 数值设施保留完整 uint64 范围。
using System;
using System.Numerics;

namespace RyuPy.Runtime;

public static partial class PyNativeBindings
{
    /// <summary>位置专用必填参数工厂；名字/个数完全来自声明而非业务函数名。</summary>
    public static PyFunction CreateAbi(string name, string names, Func<object?[], object?> body)
    {
        string[] parameters = names.Length == 0 ? [] : names.Split(',');
        string[] kinds = new string[parameters.Length];
        string[] defaults = new string[parameters.Length];
        Array.Fill(kinds, "posonly");
        Array.Fill(defaults, "0");
        PySignature signature = PySignature.Create(name, parameters, kinds, defaults);
        return new PyFunction(name, signature, (self, args, keywords, values) =>
        {
            object?[] bound;
            try { bound = PyBinder.Bind(signature, args, keywords, values, self.Defaults); }
            catch (PyBaseException error) { PyErrors.PrepareRaise(error); throw; }
            return body(bound) ?? PyNone.Instance;
        }, new object?[parameters.Length], false);
    }

    /// <summary>保留 Python 整数小值缓存。</summary>
    public static object BoxSigned(long value) => PyInt.Box(value);
    /// <summary>大于 signed64 的返回值保持 BigInteger，不丢精度或变号。</summary>
    public static object BoxUnsigned(ulong value) => value <= long.MaxValue
        ? PyInt.Box((long)value) : new BigInteger(value);
    /// <summary>float32 先提升为 binary64，保留舍入、NaN 和带符号零。</summary>
    public static object BoxFloating(double value) => value;
    /// <summary>拒绝非规范布尔返回，避免把错误状态静默吞成 true。</summary>
    public static object BoxBoolean(byte value) => value switch
    {
        0 => false, 1 => true,
        _ => throw Prepared(new PyValueError("declared native bool8 result must be 0 or 1"))
    };
}
