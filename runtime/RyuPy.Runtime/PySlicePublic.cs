// slice 的公共构造与任意精度 indices；独立于各容器沿用的 long 窗口算法。
// 构造保存原字段，直到 indices 才按 length/step/start/stop 顺序执行用户整数协议。
using System;
using System.Numerics;

namespace RyuPy.Runtime;

public sealed partial class PySlice
{
    /// <summary>与语法切片的 type 结果共享的真实驻留内建类型。</summary>
    public static PyType Type { get; } = PyType.Get("slice");

    /// <summary>slice(stop) / slice(start, stop[, step])；构造期不转换字段。</summary>
    /// <param name="args">已完整求值和展开的位置实参。</param>
    /// <param name="names">关键字名；任何关键字优先于位置参数个数报错。</param>
    public static PySlice Create(object?[] args, string[] names)
    {
        if (names.Length != 0) throw PublicError(new PyTypeError("slice() takes no keyword arguments"));
        if (args.Length == 0) throw PublicError(new PyTypeError("slice expected at least 1 argument, got 0"));
        if (args.Length > 3)
            throw PublicError(new PyTypeError($"slice expected at most 3 arguments, got {args.Length}"));
        return args.Length == 1 ? new PySlice(PyNone.Instance, args[0], PyNone.Instance)
            : new PySlice(args[0], args[1], args.Length == 3 ? args[2] : PyNone.Instance);
    }

    internal object? GetAttribute(string name) => name switch
    {
        "start" => Start ?? PyNone.Instance,
        "stop" => Stop ?? PyNone.Instance,
        "step" => Step ?? PyNone.Instance,
        "__class__" => Type,
        _ when PySliceMembers.HasMethod(name) => PySliceMembers.Bound(this, name),
        "__reduce__" or "__reduce_ex__" or "__getstate__" or "__new__" or "__doc__"
            or "__str__" or "__format__" or "__sizeof__" or "__dir__" =>
            throw PublicError(new PyNotImplementedError($"slice.{name} is not supported")),
        _ => throw PublicError(new PyAttributeError($"'slice' object has no attribute '{name}'")),
    };

    // 长度和所有分量都保任意精度；不能调用内部 Indices(long) 或把 step 夹到机器宽度。
    internal PyTuple PublicIndices(object? lengthValue)
    {
        try
        {
            BigInteger length = PublicIndex(lengthValue, lengthArgument: true);
            if (length.Sign < 0) throw PublicError(new PyValueError("length should not be negative"));
            BigInteger step = Step is null or PyNone ? BigInteger.One : PublicIndex(Step, lengthArgument: false);
            if (step.IsZero) throw PublicError(new PyValueError("slice step cannot be zero"));
            bool backwards = step.Sign < 0;
            BigInteger start = Start is null or PyNone ? (backwards ? length - 1 : BigInteger.Zero)
                : PublicBound(PublicIndex(Start, lengthArgument: false), length, backwards);
            BigInteger stop = Stop is null or PyNone ? (backwards ? BigInteger.MinusOne : length)
                : PublicBound(PublicIndex(Stop, lengthArgument: false), length, backwards);
            return new PyTuple([PyInt.Normalize(start), PyInt.Normalize(stop), PyInt.Normalize(step)]);
        }
        catch (PyBaseException error)
        {
            // 只补共享协议新错误缺失的帧；已有用户异常身份、帧和 context 不覆盖。
            PyErrors.PrepareRaise(error);
            throw;
        }
    }

    private static BigInteger PublicBound(BigInteger value, BigInteger length, bool backwards)
    {
        if (value.Sign < 0) value += length;
        if (value.Sign < 0) return backwards ? BigInteger.MinusOne : BigInteger.Zero;
        if (value >= length) return backwards ? length - 1 : length;
        return value;
    }

    private static BigInteger PublicIndex(object? value, bool lengthArgument)
    {
        object? integer = value;
        if (!PyInt.IsInt(integer))
        {
            GuardProtocol(value, "__index__");
            if (!PyObject.TryMethod(value, "__index__", [], out integer))
                throw PublicError(new PyTypeError(lengthArgument
                    ? $"'{PyRepr.TypeName(value)}' object cannot be interpreted as an integer"
                    : "slice indices must be integers or None or have an __index__ method"));
            if (integer is bool)
                throw PublicError(new PyNotImplementedError("slice.indices: __index__ returning bool requires DeprecationWarning support"));
            if (!PyInt.IsInt(integer))
                throw PublicError(new PyTypeError($"__index__ returned non-int (type {PyRepr.TypeName(integer)})"));
        }
        return PyInt.ToBigInteger(integer!);
    }

    // 原始slot的普通函数/static/class绑定已验证；其它合法可调用或descriptor不能错误追加self。
    private static void GuardProtocol(object? value, string name)
    {
        PyClass? cls = value is PyInstance instance ? instance.Class
            : value is PyBaseException exception ? exception.PythonClass : null;
        if (cls is null || !cls.TryLookup(name, out _, out object? raw)) return;
        if (name == "__hash__" && raw is null or PyNone)
            throw PublicError(new PyTypeError($"unhashable type: '{cls.Name}'"));
        if (raw is PyFunction or PyStaticMethod or PyClassMethod || name == "__hash__" && PyBaseObjectMethods.IsRootHash(raw)) return;
        // 已知非callable由原入口按TypeError处理；不把不支持的合法描述符伪装成非法。
        if (raw is null or PyNone or bool or int or long or BigInteger or double or string
            or PyBytes or PyByteArray or PyList or PyTuple or PyDict or PySet or PyFrozenSet) return;
        throw PublicError(new PyNotImplementedError($"slice {name} callable or descriptor shape is not supported"));
    }

    internal static T PublicError<T>(T error) where T : PyBaseException => (T)PyErrors.PrepareRaise(error);
}
