using System;
using System.Collections.Generic;
using System.Numerics;
using System.Security.Cryptography;
using System.Text;

namespace RyuPy.Runtime;

/// <summary>random原生模块：模块方法绑定同一默认实例，显式Random实例各有独立MT状态。</summary>
public static class PyRandomModule
{
    private static readonly Lazy<Dictionary<string, object?>> Attributes = new(Build);
    /// <summary>返回稳定模块导出；不使用System.Random冒充CPython随机序列。</summary>
    public static Dictionary<string, object?> CreateAttributes() => Attributes.Value;
    private static Dictionary<string, object?> Build()
    {
        var defaults = new PyRandom(PyNone.Instance);
        var factory = PyNativeFunctions.Create("Random", ["x"], [PyNone.Instance], 0, a => new PyRandom(a[0]));
        var result = new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["Random"] = new PyNativeType("Random", "random", (args, names, values) =>
                PyOps.CallValue(factory, args, names, values), value => value is PyRandom),
        };
        foreach (string name in new[] { "seed", "random", "getrandbits", "randrange", "randint", "choice", "shuffle",
            "getstate", "setstate", "gauss" }) result[name] = defaults.GetAttr(name);
        return result;
    }
}

/// <summary>受限Random实例；精确复现固定CPython的取字顺序、拒绝采样与53位浮点转换。</summary>
public sealed class PyRandom : PyNativeObject
{
    private readonly PyRandomState _state = new();
    internal PyRandom(object? seed) => Seed(seed, 2L);
    /// <inheritdoc />
    public override string TypeName => "Random";
    /// <inheritdoc />
    public override object? GetAttr(string name) => name switch
    {
        "seed" => PyNativeFunctions.Create(name, ["a", "version"], [PyNone.Instance, 2L], 0,
            a => { Seed(a[0], a[1]); return null; }),
        "random" => PyNativeFunctions.Create(name, [], [], 0, _ => _state.Random()),
        "getrandbits" => PyNativeFunctions.Create(name, ["k"], [null], 1,
            a => Integer(_state.Bits(BitCount(a[0]))), ["posonly"]),
        "randrange" => PyNativeFunctions.Create(name, ["start", "stop", "step"], [null, PyNone.Instance, 1L], 1,
            a => Integer(Range(a[0], a[1], a[2]))),
        "randint" => PyNativeFunctions.Create(name, ["a", "b"], [null, null], 2,
            a => Integer(Range(a[0], PyOps.Binary(PyOps.Add, a[1]!, 1L), 1L))),
        "choice" => PyNativeFunctions.Create(name, ["seq"], [null], 1, a => Choice(a[0])),
        "shuffle" => PyNativeFunctions.Create(name, ["x"], [null], 1, a => { Shuffle(a[0]); return null; }),
        "getstate" or "setstate" or "gauss" => Unsupported(name),
        _ => throw Missing(name),
    };

    private static PyFunction Unsupported(string name) => new(name, PySignature.Create(name, [], [], []),
        (_, _, _, _) => throw new PyNotImplementedError("random." + name + " is not supported"), [], false);

    private void Seed(object? seed, object? version)
    {
        // 仅接受实际内置标量，不让未支持的version对象触发用户相等协议。
        bool versionTwo = version switch
        {
            long value => value == 2,
            int value => value == 2,
            BigInteger value => value == 2,
            double value => value == 2,
            float value => value == 2,
            _ => false,
        };
        if (!versionTwo) throw new PyNotImplementedError("random seed supports only scalar version 2");
        if (seed is null or PyNone)
        {
            // 未给种子使用操作系统熵；MT输出仍不是密码学随机数。
            byte[] entropy = RandomNumberGenerator.GetBytes(624 * 4);
            var words = new uint[624];
            for (int index = 0; index < words.Length; index++)
                words[index] = System.Buffers.Binary.BinaryPrimitives.ReadUInt32LittleEndian(entropy.AsSpan(index * 4, 4));
            _state.SeedWords(words);
            return;
        }
        if (PyInt.IsInt(seed)) { _state.Seed(PyInt.ToBigInteger(seed!)); return; }
        byte[] bytes;
        switch (seed)
        {
            case string text:
                try { bytes = new UTF8Encoding(false, true).GetBytes(text); }
                catch (EncoderFallbackException) { throw new PyUnicodeEncodeError("surrogates not allowed in random string seed"); }
                break;
            case PyBytes immutable: bytes = immutable.Bytes; break;
            case PyByteArray mutable: bytes = mutable.Bytes.ToArray(); break;
            case double or float: throw new PyNotImplementedError("random float seeds are not supported");
            default: throw new PyTypeError("The only supported seed types are: None, int, float, str, bytes, and bytearray.");
        }
        // version=2把原字节接SHA-512摘要，再作为大端非负整数使用全部位。
        byte[] digest = SHA512.HashData(bytes);
        var expanded = new byte[bytes.Length + digest.Length];
        bytes.CopyTo(expanded, 0);
        digest.CopyTo(expanded, bytes.Length);
        _state.Seed(new BigInteger(expanded, isUnsigned: true, isBigEndian: true));
    }

    private static object Integer(BigInteger value)
    {
        if (value >= long.MinValue && value <= long.MaxValue) return PyInt.Box((long)value);
        return value;
    }

    private static BigInteger Index(object? value)
    {
        if (PyInt.IsInt(value)) return PyInt.ToBigInteger(value!);
        object? original = value;
        if (!PyObject.TryMethod(value, "__index__", [], out value))
            throw new PyTypeError($"'{PyRepr.TypeName(original)}' object cannot be interpreted as an integer");
        if (!PyInt.IsInt(value)) throw new PyTypeError("__index__ returned non-int");
        return PyInt.ToBigInteger(value!);
    }

    private static int BitCount(object? value)
    {
        BigInteger count = Index(value);
        if (count.Sign < 0) throw new PyValueError("number of bits must be non-negative");
        // 复用运行时大整数资源上限，不把超大请求转换为CLR数组溢出或内存崩溃。
        if (count > PyInt.MaxBitShift) throw new PyNotImplementedError("random bit count exceeds the runtime resource limit");
        return (int)count;
    }

    private BigInteger Below(BigInteger limit)
    {
        if (limit.Sign <= 0) throw new PyValueError("empty range for randrange()");
        long bits = limit.GetBitLength();
        if (bits > PyInt.MaxBitShift) throw new PyNotImplementedError("random range exceeds the runtime resource limit");
        // 必须是limit.bit_length()而非(limit-1).bit_length()；2的幂也保留拒绝采样的消费顺序。
        BigInteger candidate;
        do { candidate = _state.Bits((int)bits); } while (candidate >= limit);
        return candidate;
    }

    private BigInteger Range(object? start, object? stop, object? step)
    {
        BigInteger first = Index(start);
        if (stop is null or PyNone)
        {
            if (step is not long unit || unit != 1) throw new PyTypeError("Missing a non-None stop argument");
            return Below(first);
        }
        BigInteger end = Index(stop);
        BigInteger stride = Index(step);
        BigInteger width = end - first;
        if (stride.IsZero) throw new PyValueError("zero step for randrange()");
        if (stride == 1)
        {
            if (width.Sign <= 0) throw new PyValueError($"empty range in randrange({first}, {end})");
            return first + Below(width);
        }
        if (stride.Sign > 0 && width.Sign <= 0 || stride.Sign < 0 && width.Sign >= 0)
            throw new PyValueError("empty range for randrange()");
        BigInteger count = (BigInteger.Abs(width) - 1) / BigInteger.Abs(stride) + 1;
        return first + stride * Below(count);
    }

    private static long Length(object? sequence)
    {
        long count;
        if (PyObject.TryMethod(sequence, "__len__", [], out object? result))
        {
            BigInteger integer = Index(result);
            if (integer > long.MaxValue) throw new PyOverflowError("cannot fit int into an index-sized integer");
            if (integer.Sign < 0) throw new PyValueError("__len__() should return >= 0");
            count = (long)integer;
        }
        else count = PyOps.Len(sequence);
        if (count < 0) throw new PyValueError("__len__() should return >= 0");
        return count;
    }

    private object? Choice(object? sequence)
    {
        if (Length(sequence) == 0) throw new PyIndexError("Cannot choose from an empty sequence");
        // 两次len保持Python实现的可观察自定义序列协议，不先物化成list。
        return PyOps.GetItem(sequence, Integer(Below(Length(sequence))));
    }

    private void Shuffle(object? sequence)
    {
        for (long index = Length(sequence) - 1; index > 0; index--)
        {
            object selected = Integer(Below(index + 1));
            object? left = PyOps.GetItem(sequence, selected);
            object? right = PyOps.GetItem(sequence, index);
            PyOps.SetItem(sequence, index, left);
            PyOps.SetItem(sequence, selected, right);
        }
    }
}
