// base64 的 Base16/32/64 常用接口；Base32 只实现 RFC4648 文本位编排，密码与压缩算法交给 BCL。
using System;
using System.Collections.Generic;
using System.Text;

namespace RyuPy.Runtime;

internal static class PyBase64
{
    private const string Alphabet32 = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";

    internal static Dictionary<string, object?> CreateAttributes() => new(StringComparer.Ordinal)
    {
        ["b64encode"] = PyNativeFunctions.Create("b64encode", ["s", "altchars"], [null, PyNone.Instance], 1,
            arguments => Encode64(arguments[0], arguments[1])),
        ["b64decode"] = PyNativeFunctions.Create("b64decode", ["s", "altchars", "validate"], [null, PyNone.Instance, false], 1,
            arguments => Decode64(arguments[0], arguments[1], arguments[2])),
        ["standard_b64encode"] = Unary("standard_b64encode", value => PyBinascii.Encode64(value)),
        ["standard_b64decode"] = Unary("standard_b64decode", value => Decode64(value, PyNone.Instance, false)),
        ["urlsafe_b64encode"] = Unary("urlsafe_b64encode", value => Encode64(value, new PyBytes([45, 95]))),
        ["urlsafe_b64decode"] = Unary("urlsafe_b64decode", value => Decode64(value, new PyBytes([45, 95]), false)),
        ["b16encode"] = Unary("b16encode", value => new PyBytes(Encoding.ASCII.GetBytes(Convert.ToHexString(PyBinaryBuffer.Read(value))))),
        ["b16decode"] = PyNativeFunctions.Create("b16decode", ["s", "casefold"], [null, false], 1,
            arguments => Decode16(arguments[0], arguments[1])),
        ["b32encode"] = Unary("b32encode", Encode32),
        ["b32decode"] = PyNativeFunctions.Create("b32decode", ["s", "casefold", "map01"], [null, false, PyNone.Instance], 1,
            arguments => Decode32(arguments[0], arguments[1], arguments[2])),
    };

    private static PyFunction Unary(string name, Func<object?, object?> body) =>
        PyNativeFunctions.Create(name, ["s"], [null], 1, arguments => body(arguments[0]));

    private static PyBytes Encode64(object? value, object? alternate)
    {
        byte[] encoded = PyBinascii.Encode64(value).Bytes;
        if (alternate is null or PyNone) return new PyBytes(encoded);
        byte[] alt = PyBinaryBuffer.Read(alternate).ToArray();
        if (alt.Length != 2) throw new PyAssertionError(PyRepr.Repr(alternate));
        for (int index = 0; index < encoded.Length; index++)
            encoded[index] = encoded[index] == '+' ? alt[0] : encoded[index] == '/' ? alt[1] : encoded[index];
        return new PyBytes(encoded);
    }

    private static PyBytes Decode64(object? value, object? alternate, object? strict)
    {
        object input = PyBinaryBuffer.DecodeInput(value);
        if (alternate is not null and not PyNone)
        {
            byte[] alt = PyBinaryBuffer.Ascii(alternate);
            if (alt.Length != 2) throw new PyAssertionError(PyRepr.Repr(alternate));
            byte[] data = PyBinaryBuffer.Read(input).ToArray();
            // bytes.translate 同时替换，重复的替代字符由最后的映射覆盖。
            for (int index = 0; index < data.Length; index++)
                data[index] = data[index] == alt[1] ? (byte)'/' : data[index] == alt[0] ? (byte)'+' : data[index];
            input = new PyBytes(data);
        }
        using var lease = PyBinaryBuffer.Acquire(input);
        bool validate = PyOps.Truthy(strict);
        return PyBinascii.Decode64(lease.Span, validate);
    }

    private static PyBytes Decode16(object? value, object? casefold)
    {
        object input = PyBinaryBuffer.DecodeInput(value);
        bool fold = PyOps.Truthy(casefold);
        byte[] data = PyBinaryBuffer.Read(input).ToArray();
        for (int index = 0; index < data.Length; index++)
        {
            if (fold && data[index] is >= (byte)'a' and <= (byte)'z') data[index] -= 32;
            if (data[index] is not (>= (byte)'0' and <= (byte)'9') and not (>= (byte)'A' and <= (byte)'F'))
                throw new PyBinasciiError("Non-base16 digit found");
        }
        return PyBinascii.Unhex(data);
    }

    private static PyBytes Encode32(object? value)
    {
        var result = new StringBuilder();
        uint accumulator = 0;
        int bits = 0;
        foreach (byte current in PyBinaryBuffer.Read(value))
        {
            accumulator = (accumulator << 8) | current;
            bits += 8;
            while (bits >= 5) { bits -= 5; result.Append(Alphabet32[(int)(accumulator >> bits) & 31]); }
        }
        if (bits > 0) result.Append(Alphabet32[(int)(accumulator << (5 - bits)) & 31]);
        while (result.Length % 8 != 0) result.Append('=');
        return new PyBytes(Encoding.ASCII.GetBytes(result.ToString()));
    }

    private static PyBytes Decode32(object? value, object? casefold, object? mapping)
    {
        object input = PyBinaryBuffer.DecodeInput(value);
        if (PyBinaryBuffer.Read(input).Length % 8 != 0) throw new PyBinasciiError("Incorrect padding");
        byte? map = null;
        if (mapping is not null and not PyNone)
        {
            byte[] mapped = PyBinaryBuffer.Ascii(mapping);
            if (mapped.Length != 1) throw new PyAssertionError(PyRepr.Repr(mapping));
            map = mapped[0];
            input = new PyBytes(PyBinaryBuffer.Read(input).ToArray());
        }
        bool fold = PyOps.Truthy(casefold);
        byte[] data = PyBinaryBuffer.Read(input).ToArray();
        int end = data.Length;
        while (end > 0 && data[end - 1] == '=') end--;
        var output = new List<byte>();
        uint accumulator = 0;
        int bits = 0;
        for (int index = 0; index < end; index++)
        {
            byte current = data[index];
            if (map.HasValue) current = current == '0' ? (byte)'O' : current == '1' ? map.Value : current;
            if (fold && current is >= (byte)'a' and <= (byte)'z') current -= 32;
            int digit = Alphabet32.IndexOf((char)current);
            if (digit < 0) throw new PyBinasciiError("Non-base32 digit found");
            accumulator = (accumulator << 5) | (uint)digit;
            bits += 5;
            if (bits >= 8) { bits -= 8; output.Add((byte)(accumulator >> bits)); }
        }
        if (data.Length % 8 != 0 || data.Length - end is not (0 or 1 or 3 or 4 or 6))
            throw new PyBinasciiError("Incorrect padding");
        return new PyBytes(output.ToArray());
    }
}
