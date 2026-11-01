using System;
using System.Collections.Generic;
using System.Numerics;

namespace RyuPy.Runtime;

/// <summary>
/// 内建函数的运行时分派（`len` / `sorted` / `min` / `isinstance` …）。
/// </summary>
/// <remarks>
/// 编译器把调用点统一改写成 <c>PyOps.CallBuiltin(name, args, kwNames, kwValues)</c>。
/// 这样做的理由：内建函数的行为由 Python 的对象协议决定，编译期只需知道名字；
/// 关键字参数的绑定/报错也在这里统一处理。
///
/// 每个内建函数都显式声明允许的关键字（见 <see cref="RejectUnknownKeywords"/>），
/// 未知关键字立即抛 `TypeError`，避免“多了个参数却被静默忽略”。
/// </remarks>
public static partial class PyOps
{
    private static object? BuiltinDispatch(
        string name,
        object?[] args,
        string[] kwNames,
        object?[] kwValues
    ) => name switch
    {
        "len" => BuiltinLen(args, kwNames),
        "ord" => WithKeywords(name, args, kwNames, () => UnicodeOrd(Single(name, args))),
        "chr" => WithKeywords(name, args, kwNames, () => UnicodeChr(Single(name, args))),
        "list" => BuiltinSequenceConstruct(args, kwNames, static items => new PyList(items)),
        "tuple" => BuiltinSequenceConstruct(args, kwNames, static items => new PyTuple(items)),
        "set" => BuiltinSequenceConstruct(args, kwNames, static items => new PySet(items)),
        "frozenset" => BuiltinSequenceConstruct(
            args,
            kwNames,
            static items => new PyFrozenSet(items)
        ),
        "dict" => BuiltinDictWithKeywords(args, kwNames, kwValues),
        "bytes" => args.Length == 0
            ? new PyBytes([])
            : new PyBytes(PyEncoding.ToBytes(args[0])),
        "bytearray" => args.Length == 0
            ? new PyByteArray([])
            : new PyByteArray(PyEncoding.ToBytes(args[0])),
        "sorted" => BuiltinSortedKw(args, kwNames, kwValues),
        "reversed" => BuiltinReversed(args, kwNames),
        "enumerate" => BuiltinEnumerate(args, kwNames, kwValues),
        "zip" => BuiltinZip(args, kwNames),
        "sum" => BuiltinSum(args, kwNames, kwValues),
        "min" => BuiltinMinMax("min", args, kwNames, kwValues),
        "max" => BuiltinMinMax("max", args, kwNames, kwValues),
        "any" => BuiltinAnyAll("any", args, kwNames),
        "all" => BuiltinAnyAll("all", args, kwNames),
        "round" => BuiltinRound(args, kwNames),
        "range" => BuiltinRange(args, kwNames),
        "hash" => WithKeywords(name, args, kwNames, () => (long)PyHash.Of(Single(name, args))),
        "repr" => WithKeywords(name, args, kwNames, () => PyRepr.Repr(Single(name, args))),
        "str" => WithKeywords(
            name,
            args,
            kwNames,
            () => args.Length == 0 ? string.Empty : PyRepr.Str(args[0])
        ),
        "bool" => WithKeywords(
            name,
            args,
            kwNames,
            () => args.Length == 0 ? false : Truthy(args[0])
        ),
        "int" => WithKeywords(
            name,
            args,
            kwNames,
            () => args.Length == 0 ? (object)0L : ToIntValue(args[0]!)
        ),
        "float" => WithKeywords(
            name,
            args,
            kwNames,
            () => args.Length == 0 ? 0.0 : ToFloatValue(args[0]!)
        ),
        "abs" => WithKeywords(
            name,
            args,
            kwNames,
            () => AbsValue(Single(name, args)!)
        ),
        "iter" => WithKeywords(name, args, kwNames, () => PyIter.Create(Single(name, args))),
        "next" => BuiltinNext(args, kwNames),
        "id" => WithKeywords(name, args, kwNames, () => Identity(Single(name, args))),
        "isinstance" => BuiltinIsInstance(args, kwNames),
        "issubclass" => BuiltinIsSubclass(args, kwNames),
        "property" => BuiltinProperty(args, kwNames),
        "staticmethod" => BuiltinDescriptorWrapper(args, kwNames, "staticmethod", static v => new PyStaticMethod(v)),
        "classmethod" => BuiltinDescriptorWrapper(args, kwNames, "classmethod", static v => new PyClassMethod(v)),
        "super" => WithKeywords(name, args, kwNames, () => PyMethodContext.MakeSuper()),
        "getattr" => BuiltinGetAttr(args, kwNames),
        "hasattr" => BuiltinHasAttr(args, kwNames),
        "setattr" => BuiltinSetAttr(args, kwNames),
        "open" => PyIO.Open(args, kwNames, kwValues),
        "input" => BuiltinInput(args, kwNames),
        "print" => BuiltinPrint(args, kwNames, kwValues),
        "callable" => WithKeywords(
            name,
            args,
            kwNames,
            () => IsCallable(Single(name, args))
        ),
        _ => throw new PyNameError($"name '{name}' is not defined"),
    };

    // ───────────────────────── 关键字工具 ─────────────────────────

    private static void RejectUnknownKeywords(
        string name,
        string[] names,
        params string[] allowed
    )
    {
        foreach (string given in names)
        {
            if (Array.IndexOf(allowed, given) < 0)
            {
                throw new PyTypeError(
                    $"{name}() got an unexpected keyword argument '{given}'"
                );
            }
        }
    }

    private static object? KeywordValue(string[] names, object?[] values, string key)
    {
        int index = Array.IndexOf(names, key);
        return index >= 0 ? values[index] : null;
    }

    private static bool KeywordBool(string[] names, object?[] values, string key) =>
        KeywordValue(names, values, key) is { } value && Truthy(value);

    /// <summary>没有关键字时才允许调用的内建（保持错误消息一致）。</summary>
    private static object? WithKeywords(
        string name,
        object?[] args,
        string[] names,
        Func<object?> body
    )
    {
        if (names.Length > 0)
        {
            throw new PyTypeError(
                $"{name}() got an unexpected keyword argument '{names[0]}'"
            );
        }
        return body();
    }

    // ───────────────────────── 构造 ─────────────────────────

    private static object? BuiltinLen(object?[] args, string[] names)
    {
        RejectUnknownKeywords("len", names);
        return Len(Single("len", args));
    }

    private static object BuiltinSequenceConstruct(
        object?[] args,
        string[] names,
        Func<object?[], object> construct
    )
    {
        RejectUnknownKeywords("sequence", names);
        object?[] items = args.Length == 0 ? [] : ToSequence(args[0]);
        return construct(items);
    }

    private static object BuiltinDictWithKeywords(
        object?[] args,
        string[] names,
        object?[] values
    )
    {
        var dict = new PyDict();
        if (args.Length > 1)
        {
            throw new PyTypeError($"dict expected at most 1 argument, got {args.Length}");
        }
        if (args.Length == 1)
        {
            dict.Update(args[0]);
        }
        for (int i = 0; i < names.Length; i++)
        {
            dict.Set(names[i], values[i]);
        }
        return dict;
    }

    // ───────────────────────── 迭代集合 ─────────────────────────

    private static object BuiltinSortedKw(
        object?[] args,
        string[] names,
        object?[] values
    )
    {
        RejectUnknownKeywords("sorted", names, "key", "reverse");
        object?[] items = ToSequence(Single("sorted", args));
        var list = new List<object?>(items);
        object? key = KeywordValue(names, values, "key");
        bool reverse = KeywordBool(names, values, "reverse");
        PySort.SortBy(
            list,
            reverse,
            key is null or PyNone
                ? null
                : item => CallValue(key, [item], [], [])
        );
        return new PyList(list);
    }

    private static object BuiltinReversed(object?[] args, string[] names)
    {
        RejectUnknownKeywords("reversed", names);
        object?[] items = ToSequence(Single("reversed", args));
        return new PyReversedIter(items);
    }

    private static object BuiltinEnumerate(
        object?[] args,
        string[] names,
        object?[] values
    )
    {
        RejectUnknownKeywords("enumerate", names, "start");
        if (args.Length < 1)
        {
            throw new PyTypeError("enumerate expected at least 1 argument, got 0");
        }
        if (args.Length > 2)
        {
            throw new PyTypeError($"enumerate expected at most 2 arguments, got {args.Length}");
        }
        // `start` 既可以按位置也可以按关键字传
        object? raw = args.Length > 1 ? args[1] : KeywordValue(names, values, "start");
        long start = raw is null ? 0 : ToIndex(raw, "start");
        return new PyEnumerateIter(PyIter.Create(args[0]), start);
    }

    private static object BuiltinZip(object?[] args, string[] names)
    {
        RejectUnknownKeywords("zip", names);
        var inputs = new List<IPyIterator>(args.Length);
        foreach (object? arg in args)
        {
            inputs.Add(PyIter.Create(arg));
        }
        return new PyZipIter(inputs);
    }

    private static object BuiltinSum(object?[] args, string[] names, object?[] values)
    {
        RejectUnknownKeywords("sum", names, "start");
        if (args.Length < 1)
        {
            throw new PyTypeError("sum expected at least 1 argument, got 0");
        }
        object? total = KeywordValue(names, values, "start") ?? 0L;
        if (args.Length > 1 && Array.IndexOf(names, "start") < 0)
        {
            total = args[1];
            if (args.Length > 2)
            {
                throw new PyTypeError("sum takes at most 2 arguments");
            }
        }
        foreach (object? item in ToSequence(args[0]))
        {
            total = Binary(Add, total ?? PyNone.Instance, item ?? PyNone.Instance);
        }
        return total ?? PyNone.Instance;
    }

    private static object? BuiltinMinMax(
        string name,
        object?[] args,
        string[] names,
        object?[] values
    )
    {
        RejectUnknownKeywords(name, names, "key", "default");
        object? key = KeywordValue(names, values, "key");
        bool hasDefault = Array.IndexOf(names, "default") >= 0;
        object? fallback = KeywordValue(names, values, "default");

        List<object?> items;
        if (args.Length == 1)
        {
            items = [.. ToSequence(args[0])];
        }
        else if (args.Length >= 2)
        {
            items = [.. args];
        }
        else
        {
            throw new PyTypeError($"{name} expected at least 1 argument, got 0");
        }
        if (items.Count == 0)
        {
            if (hasDefault)
            {
                return fallback;
            }
            throw new PyValueError($"{name}() arg is an empty sequence");
        }

        Func<object?, object?> keyOf = key is null or PyNone
            ? static item => item
            : item => CallValue(key, [item], [], []);

        object? best = items[0];
        object? bestKey = keyOf(best) ?? PyNone.Instance;
        bool wantLarger = name == "max";
        for (int i = 1; i < items.Count; i++)
        {
            object? candidateKey = keyOf(items[i]) ?? PyNone.Instance;
            bool better = wantLarger
                ? Compare(Lt, bestKey ?? PyNone.Instance, candidateKey ?? PyNone.Instance)
                : Compare(Lt, candidateKey ?? PyNone.Instance, bestKey ?? PyNone.Instance);
            if (better)
            {
                best = items[i];
                bestKey = candidateKey;
            }
        }
        return best;
    }

    private static object BuiltinAnyAll(string name, object?[] args, string[] names)
    {
        RejectUnknownKeywords(name, names);
        bool wantAny = name == "any";
        foreach (object? item in ToSequence(Single(name, args)))
        {
            bool truthy = Truthy(item);
            if (wantAny && truthy)
            {
                return true;
            }
            if (!wantAny && !truthy)
            {
                return false;
            }
        }
        return !wantAny;
    }

    private static object? BuiltinRound(object?[] args, string[] names)
    {
        RejectUnknownKeywords("round", names);
        if (args.Length < 1 || args.Length > 2)
        {
            throw new PyTypeError($"round expected 1 or 2 arguments, got {args.Length}");
        }
        if (args[0] is long or int or BigInteger)
        {
            BigInteger integer = PyInt.ToBigInteger(args[0]!);
            if (args.Length == 2)
            {
                long digits = ToIndex(args[1]!, "ndigits");
                return digits >= 0
                    ? PyInt.Normalize(integer)
                    : PyInt.Normalize(RoundBigInteger(integer, (int)-digits));
            }
            return PyInt.Normalize(integer);
        }
        double value = ToDouble(args[0]!);
        if (double.IsNaN(value) || double.IsInfinity(value))
        {
            if (args.Length == 2)
            {
                return value;
            }
            throw new PyValueError(
                double.IsNaN(value)
                    ? "cannot convert float NaN to integer"
                    : "cannot convert float infinity to integer"
            );
        }
        if (args.Length == 2)
        {
            long digits = ToIndex(args[1]!, "ndigits");
            if (digits < 0)
            {
                // 负 ndigits：按 10^(-digits) 取整（`round(123.456, -1) == 120.0`）
                double unit = Math.Pow(10, -digits);
                return Math.Round(value / unit, MidpointRounding.ToEven) * unit;
            }
            // 直接缩放成 double 会在 2.675 这类“二进制略小于中点”的值上
            // 恰好落回 267.5，从而多进一位（CPython 得 2.67）。这里按 double 的
            // 精确二进制分解做十进制舍入，结果与 CPython 的 round 一致。
            if (digits > 15)
            {
                return value;
            }
            return RoundDoubleExact(value, (int)digits);
        }
        return (long)Math.Round(value, MidpointRounding.ToEven);
    }

    /// <summary>
    /// 按 double 的精确二进制值做十进制四舍五入（银行家舍入）。
    /// </summary>
    /// <remarks>
    /// double 可以表示为 <c>m * 2^e</c>：拆出尾数与指数后乘以 <c>5^d</c> 并调整
    /// 二进制指数，就得到精确的 <c>value * 10^d</c>，再对 2 的幂做「取半、向偶数」。
    /// 这样 <c>round(2.675, 2)</c> 会得到 2.67（真实二进制值小于中点），与 CPython 一致。
    /// </remarks>
    private static double RoundDoubleExact(double value, int digits)
    {
        bool negative = value < 0;
        long bits = BitConverter.DoubleToInt64Bits(Math.Abs(value));
        int biasedExponent = (int)((bits >> 52) & 0x7FF);
        long mantissa = bits & 0xF_FFFF_FFFF_FFFF;
        int exponent;
        if (biasedExponent == 0)
        {
            exponent = 1 - 1023 - 52;
        }
        else
        {
            mantissa |= 1L << 52;
            exponent = biasedExponent - 1023 - 52;
        }

        BigInteger scaled = new BigInteger(mantissa) * BigInteger.Pow(5, digits);
        int shift = exponent + digits;
        BigInteger rounded;
        if (shift >= 0)
        {
            rounded = scaled << shift;
        }
        else
        {
            int k = -shift;
            BigInteger quotient = BigInteger.DivRem(
                scaled,
                BigInteger.One << k,
                out BigInteger remainder
            );
            BigInteger half = BigInteger.One << (k - 1);
            if (remainder > half || (remainder == half && !quotient.IsEven))
            {
                quotient += 1;
            }
            rounded = quotient;
        }
        if (negative)
        {
            rounded = -rounded;
        }
        return (double)rounded / Math.Pow(10, digits);
    }

    private static BigInteger RoundBigInteger(BigInteger value, int digits)
    {
        BigInteger scale = BigInteger.Pow(10, digits);
        BigInteger quotient = BigInteger.DivRem(value, scale, out BigInteger remainder);
        BigInteger twice = BigInteger.Abs(remainder) * 2;
        if (twice > scale || (twice == scale && !quotient.IsEven))
        {
            quotient += value.Sign >= 0 ? 1 : -1;
        }
        return quotient * scale;
    }

    private static object BuiltinRange(object?[] args, string[] names)
    {
        RejectUnknownKeywords("range", names);
        return args.Length switch
        {
            1 => new PyRange(0, ToIndex(args[0]!, "range"), 1),
            2 => new PyRange(ToIndex(args[0]!, "range"), ToIndex(args[1]!, "range"), 1),
            3 => new PyRange(
                ToIndex(args[0]!, "range"),
                ToIndex(args[1]!, "range"),
                ToIndex(args[2]!, "range")
            ),
            _ => throw new PyTypeError($"range expected 1 to 3 arguments, got {args.Length}"),
        };
    }

    private static object? BuiltinNext(object?[] args, string[] names)
    {
        RejectUnknownKeywords("next", names);
        if (args.Length is < 1 or > 2)
        {
            throw new PyTypeError($"next expected 1 or 2 arguments, got {args.Length}");
        }
        if (args[0] is PyGenerator generator)
            return generator.Next(args.Length == 2 ? args[1] : null, args.Length == 2);
        if (args[0] is PyInstance instance)
        {
            try
            {
                if (PyObject.TryMethod(instance, "__next__", [], out object? next))
                {
                    return next;
                }
            }
            catch (PyStopIteration)
            {
                if (args.Length == 2)
                {
                    return args[1];
                }
                throw;
            }
            if (args.Length == 2)
            {
                return args[1];
            }
            throw new PyTypeError(
                $"'{instance.Class.Name}' object is not an iterator"
            );
        }
        IPyIterator iterator = args[0] is IPyIterator it
            ? it
            : throw new PyTypeError(
                $"'{PyRepr.TypeName(args[0])}' object is not an iterator"
            );
        if (iterator.MoveNext())
        {
            return iterator.Current();
        }
        if (args.Length == 2)
        {
            return args[1];
        }
        throw new PyStopIteration(null);
    }

    // ───────────────────────── 对象内建 ─────────────────────────

    /// <summary>
    /// `id(x)`：稳定标识。对象身份由运行时的引用决定；装箱数值每次调用可能不同，
    /// 与 CPython 的地址语义存在已登记偏差（compatibility.md D002）。
    /// </summary>
    private static long Identity(object? value) =>
        value is null ? 0 : System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(value);

    private static object BuiltinIsInstance(object?[] args, string[] names)
    {
        RejectUnknownKeywords("isinstance", names);
        if (args.Length != 2)
        {
            throw new PyTypeError($"isinstance expected 2 arguments, got {args.Length}");
        }
        if (args[1] is PyTuple alternatives)
        {
            foreach (object? candidate in alternatives.Items)
            {
                if (MatchesType(args[0], candidate))
                {
                    return true;
                }
            }
            return false;
        }
        return MatchesType(args[0], args[1]);
    }

}
