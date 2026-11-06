// bytes/bytearray的八个ASCII分类：共用窄零参绑定器与无复制span扫描，不解码或调用用户协议。
using System;
using System.Runtime.InteropServices;

namespace RyuPy.Runtime;

internal static class PyBinaryClassification
{
    internal static bool Has(string name) => name is "isalpha" or "isalnum" or "isdigit" or "isspace"
        or "isascii" or "islower" or "isupper" or "istitle";

    internal static bool Invoke(object receiver, string name, int positionalCount, int keywordCount)
    {
        // METH_NOARGS先拒关键字；只检查数量，正常零参不复制数组或创建绑定包装。
        string owner = receiver is PyBytes ? "bytes" : "bytearray";
        if (keywordCount != 0) throw Error($"{owner}.{name}() takes no keyword arguments");
        if (positionalCount != 0) throw Error($"{owner}.{name}() takes no arguments ({positionalCount} given)");
        ReadOnlySpan<byte> bytes = receiver is PyBytes immutable
            ? immutable.Span : CollectionsMarshal.AsSpan(((PyByteArray)receiver).Bytes);
        return name switch
        {
            "isalpha" => Alpha(bytes), "isalnum" => Alnum(bytes), "isdigit" => Digit(bytes),
            "isspace" => Space(bytes), "isascii" => Ascii(bytes),
            "islower" => Case(bytes, lower: true), "isupper" => Case(bytes, lower: false),
            "istitle" => Title(bytes),
            _ => throw new InvalidOperationException("unknown binary classification method"),
        };
    }

    internal static PyTypeError Error(string message) => (PyTypeError)PyErrors.PrepareRaise(new PyTypeError(message));

    private static bool IsLower(byte value) => value is >= (byte)'a' and <= (byte)'z';
    private static bool IsUpper(byte value) => value is >= (byte)'A' and <= (byte)'Z';
    private static bool IsAlpha(byte value) => IsLower(value) || IsUpper(value);

    private static bool Alpha(ReadOnlySpan<byte> bytes)
    {
        foreach (byte value in bytes) if (!IsAlpha(value)) return false;
        return !bytes.IsEmpty;
    }

    private static bool Alnum(ReadOnlySpan<byte> bytes)
    {
        foreach (byte value in bytes)
            if (!IsAlpha(value) && value is not (>= (byte)'0' and <= (byte)'9')) return false;
        return !bytes.IsEmpty;
    }

    private static bool Digit(ReadOnlySpan<byte> bytes)
    {
        foreach (byte value in bytes) if (value is not (>= (byte)'0' and <= (byte)'9')) return false;
        return !bytes.IsEmpty;
    }

    private static bool Space(ReadOnlySpan<byte> bytes)
    {
        // 二进制空白只有09–0D和20；1C–1F、85、A0不属于此集合。
        foreach (byte value in bytes) if (value is not (>= 9 and <= 13) and not 32) return false;
        return !bytes.IsEmpty;
    }

    private static bool Ascii(ReadOnlySpan<byte> bytes)
    {
        foreach (byte value in bytes) if (value >= 128) return false;
        return true;
    }

    private static bool Case(ReadOnlySpan<byte> bytes, bool lower)
    {
        bool cased = false;
        foreach (byte value in bytes)
        {
            if (lower ? IsUpper(value) : IsLower(value)) return false;
            if (lower ? IsLower(value) : IsUpper(value)) cased = true;
        }
        return cased;
    }

    private static bool Title(ReadOnlySpan<byte> bytes)
    {
        bool previousCased = false, anyCased = false;
        foreach (byte value in bytes)
        {
            if (IsUpper(value))
            {
                if (previousCased) return false;
                previousCased = anyCased = true;
            }
            else if (IsLower(value))
            {
                if (!previousCased) return false;
                anyCased = true;
            }
            else previousCased = false;
        }
        return anyCased;
    }
}
