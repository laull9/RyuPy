using System;
using System.Numerics;

namespace RyuPy.Runtime;

public static partial class PyOps
{
    /// <summary>真值判定（条件、<c>not</c>、<c>and</c>/<c>or</c> 共用）。</summary>
    /// <remarks>
    /// 空容器为假是 Python 最常见的惯用法（<c>if not items:</c> / <c>while queue:</c>），
    /// 因此每一种容器都必须显式列出——漏掉一类会静默进入错误分支，而不是报错。
    /// 字符串按**码点**判空：UTF-16 的长度不影响「是否为空」这一结论。
    /// </remarks>
    public static bool Truthy(object? value) => value switch
    {
        null => false,
        PyNone => false,
        PyUnbound => false,
        bool b => b,
        long l => l != 0,
        int i => i != 0,
        BigInteger big => !big.IsZero,
        double d => d != 0.0,
        string s => s.Length > 0,
        PyList list => list.Count > 0,
        PyTuple tuple => tuple.Length > 0,
        PyDict dict => dict.Count > 0,
        PySet set => set.Count > 0,
        PyFrozenSet frozen => frozen.Count > 0,
        PyBytes bytes => bytes.Count > 0,
        PyByteArray array => array.Count > 0,
        PyDictView view => view.Count > 0,
        PyInstance instance => TruthyInstance(instance),
        _ => true,
    };

    /// <summary>普通实例的真值：`__bool__` → `__len__` → 真。</summary>
    private static bool TruthyInstance(PyInstance instance)
    {
        if (PyObject.TryMethod(instance, "__bool__", [], out object? custom))
        {
            return custom is bool b
                ? b
                : throw new PyTypeError(
                    $"__bool__ should return bool, returned {PyRepr.TypeName(custom)}"
                );
        }
        if (PyObject.TryMethod(instance, "__len__", [], out object? length))
        {
            return PyCast.ToInt64(length) != 0;
        }
        return true;
    }

    // ───────────────────────── 用户运算槽（M2.5）─────────────────────────

    private static readonly string[] BinaryMagicNames =
    {
        "", "__add__", "__sub__", "__mul__", "__truediv__", "__floordiv__",
        "__mod__", "__pow__", "__and__", "__or__", "__xor__", "__lshift__", "__rshift__",
    };

    private static readonly string[] ReflectedMagicNames =
    {
        "", "__radd__", "__rsub__", "__rmul__", "__rtruediv__", "__rfloordiv__",
        "__rmod__", "__rpow__", "__rand__", "__ror__", "__rxor__", "__rlshift__", "__rrshift__",
    };

    /// <summary>尝试用户二元运算槽：左侧 `__op__`，再右侧 `__rop__`。</summary>
    private static bool TryBinaryMagic(int op, object a, object b, out object? result)
    {
        result = null;
        if (op <= 0 || op >= BinaryMagicNames.Length)
        {
            return false;
        }
        if (PyObject.TryMethod1(a, BinaryMagicNames[op], b, out result))
        {
            return true;
        }
        return PyObject.TryMethod1(b, ReflectedMagicNames[op], a, out result);
    }

    private static bool TryUnaryMagic(string name, object value, out object? result) =>
        PyObject.TryMethod(value, name, [], out result);

    /// <summary>比较运算的用户槽：`__lt__` 等返回真值，不等则试右侧反射方法。</summary>
    private static bool TryRichCompare(int op, object a, object b, out bool result)
    {
        string forward;
        string reflected;
        switch (op)
        {
            case Lt:
                forward = "__lt__";
                reflected = "__gt__";
                break;
            case Le:
                forward = "__le__";
                reflected = "__ge__";
                break;
            case Gt:
                forward = "__gt__";
                reflected = "__lt__";
                break;
            case Ge:
                forward = "__ge__";
                reflected = "__le__";
                break;
            default:
                result = false;
                return false;
        }
        if (PyObject.TryMethod1(a, forward, b, out object? value))
        {
            result = Truthy(value);
            return true;
        }
        if (PyObject.TryMethod1(b, reflected, a, out object? reflectedValue))
        {
            result = Truthy(reflectedValue);
            return true;
        }
        result = false;
        return false;
    }

}
