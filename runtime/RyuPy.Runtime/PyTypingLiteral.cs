using System.Collections.Generic;
using System.Numerics;

namespace RyuPy.Runtime;

/// <summary>Literal的有限值集合：实际标量、真实TypeVar与不可变typing根，类型敏感去重，不执行用户相等或哈希协议。</summary>
internal static class PyTypingLiteral
{
    internal static object?[] Normalize(object?[] args)
    {
        var result = new List<object?>();
        foreach (object? arg in args)
        {
            object?[] inner = arg is PyTypingAlias { Name: "Literal" } alias ? alias.Arguments : [arg];
            foreach (object? item in inner)
            {
                object value = item ?? PyNone.Instance;
                _ = Kind(value);
                bool found = false;
                foreach (object? existing in result) if (Equal(existing, value)) { found = true; break; }
                if (!found) result.Add(value);
            }
        }
        return result.ToArray();
    }

    private static string Kind(object? value) => value switch
    {
        null or PyNone => "None", bool => "bool", long or int or BigInteger => "int", string => "str", PyTypingTypeVar => "TypeVar",
        // 已有 form 不允许用户重绑 hash/eq；按真实对象身份去重，不扩任意对象域。
        PyTypingForm => "TypingForm",
        _ => throw new PyNotImplementedError("Literal supports only None, bool, int, str, TypeVar and immutable typing form values; Enum values are not supported"),
    };

    internal static bool Equal(object? left, object? right)
    {
        string kind = Kind(left);
        if (kind != Kind(right)) return false;
        return kind switch
        {
            "None" => true,
            "bool" => (bool)left! == (bool)right!,
            "int" => PyInt.ToBigInteger(left!) == PyInt.ToBigInteger(right!),
            "TypeVar" or "TypingForm" => ReferenceEquals(left, right),
            _ => (string)left! == (string)right!,
        };
    }

    internal static PyTuple Key(object?[] args)
    {
        var values = new object?[args.Length];
        for (int index = 0; index < args.Length; index++) values[index] = new PyTuple([Kind(args[index]), args[index]]);
        return new PyTuple(values);
    }

    internal static long Hash(object?[] args)
    {
        long hash = PyHash.Of("typing.Literal");
        foreach (object? arg in args) hash ^= PyHash.Of(new PyTuple([Kind(arg), arg]));
        return hash == -1 ? -2 : hash;
    }
}
