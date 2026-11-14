// binascii 的十六进制、Base64 和 CRC32 合同；Convert 负责编码/字节转换，前置状态机约束 Python 解码规则。
using System;
using System.Collections.Generic;
using System.Text;

namespace RyuPy.Runtime;

internal static class PyBinascii
{
    private static readonly uint[] CrcTable = CreateCrcTable();

    internal static Dictionary<string, object?> CreateAttributes()
    {
        var hex = PyNativeFunctions.Create("hexlify", ["data", "sep", "bytes_per_sep"], [null, null, 1L], 1,
            arguments => Hex(arguments[0], arguments[1], arguments[2]));
        var unhex = PyNativeFunctions.Create("unhexlify", ["hexstr"], [null], 1,
            arguments => Unhex(PyBinaryBuffer.Ascii(arguments[0])), ["posonly"]);
        return new(StringComparer.Ordinal)
        {
            ["Error"] = PyType.Get("Error"),
            ["hexlify"] = hex, ["b2a_hex"] = hex, ["unhexlify"] = unhex, ["a2b_hex"] = unhex,
            ["crc32"] = PyNativeFunctions.Create("crc32", ["data", "crc"], [null, 0L], 1,
                arguments => Crc32(arguments[0], arguments[1]), ["posonly", "posonly"]),
            ["b2a_base64"] = PyNativeFunctions.Create("b2a_base64", ["data", "newline"], [null, true], 1,
                arguments => EncodeWithFlag(arguments[0], arguments[1]), ["posonly", "kwonly"]),
            ["a2b_base64"] = PyNativeFunctions.Create("a2b_base64", ["data", "strict_mode"], [null, false], 1,
                arguments => DecodeWithFlag(arguments[0], arguments[1]), ["posonly", "kwonly"]),
        };
    }

    internal static PyBytes Encode64(object? value, bool newline = false) => new(Encoding.ASCII.GetBytes(
        Convert.ToBase64String(PyBinaryBuffer.Read(value)) + (newline ? "\n" : "")));

    private static PyBytes EncodeWithFlag(object? value, object? newline)
    {
        using var lease = PyBinaryBuffer.Acquire(value);
        bool appendNewline = PyOps.Truthy(newline);
        return new PyBytes(Encoding.ASCII.GetBytes(Convert.ToBase64String(lease.Span) + (appendNewline ? "\n" : "")));
    }

    private static PyBytes DecodeWithFlag(object? value, object? strict)
    {
        object? input = value is string ? new PyBytes(PyBinaryBuffer.Ascii(value)) : value;
        using var lease = PyBinaryBuffer.Acquire(input);
        bool validate = PyOps.Truthy(strict);
        return Decode64(lease.Span, validate);
    }

    internal static PyBytes Decode64(ReadOnlySpan<byte> data, bool strict)
    {
        var clean = new StringBuilder(data.Length);
        int quad = 0, pads = 0, count = 0;
        for (int index = 0; index < data.Length; index++)
        {
            byte current = data[index];
            if (current == '=')
            {
                if (strict && index == 0) throw new PyBinasciiError("Leading padding not allowed");
                if (strict && quad == 0) throw new PyBinasciiError("Excess padding not allowed");
                pads++;
                if (quad >= 2 && quad + pads >= 4)
                {
                    if (strict && index + 1 != data.Length) throw new PyBinasciiError("Excess data after padding");
                    clean.Append('=', 4 - quad);
                    return Convert64(clean);
                }
                continue;
            }
            bool alphabet = current is >= (byte)'A' and <= (byte)'Z' or >= (byte)'a' and <= (byte)'z'
                or >= (byte)'0' and <= (byte)'9' or (byte)'+' or (byte)'/';
            if (!alphabet)
            {
                if (strict) throw new PyBinasciiError("Only base64 data is allowed");
                continue;
            }
            if (strict && pads != 0) throw new PyBinasciiError("Discontinuous padding not allowed");
            pads = 0;
            clean.Append((char)current);
            quad = (quad + 1) % 4;
            count++;
        }
        if (quad == 1) throw new PyBinasciiError($"Invalid base64-encoded string: number of data characters ({count}) cannot be 1 more than a multiple of 4");
        if (quad != 0) throw new PyBinasciiError("Incorrect padding");
        return Convert64(clean);
    }

    private static PyBytes Convert64(StringBuilder text)
    {
        try { return new PyBytes(Convert.FromBase64String(text.ToString())); }
        catch (FormatException) { throw new PyBinasciiError("Incorrect padding"); }
    }

    internal static PyBytes Unhex(byte[] data)
    {
        if (data.Length % 2 != 0) throw new PyBinasciiError("Odd-length string");
        try { return new PyBytes(Convert.FromHexString(Encoding.ASCII.GetString(data))); }
        catch (FormatException) { throw new PyBinasciiError("Non-hexadecimal digit found"); }
    }

    private static PyBytes Hex(object? value, object? separator, object? groupValue)
    {
        using var lease = PyBinaryBuffer.Acquire(value);
        byte sep = 0;
        bool separated = separator is not null;
        if (separated)
        {
            if (separator is not string and not PyBytes) throw new PyTypeError("sep must be str or bytes.");
            int length = separator is string text ? text.Length : ((PyBytes)separator!).Count;
            if (length != 1) throw new PyValueError("sep must be length 1.");
            int code = separator is string single ? single[0] : ((PyBytes)separator!).Bytes[0];
            if (separator is string && code > 127) throw new PyValueError("sep must be ASCII.");
            sep = (byte)code;
        }
        var group = PyBinaryBuffer.Index(groupValue);
        if (group < int.MinValue || group > int.MaxValue) throw new PyOverflowError("Python int too large to convert to C int");
        ReadOnlySpan<byte> data = lease.Span;
        string hex = Convert.ToHexStringLower(data);
        if (!separated || group == 0) return new PyBytes(Encoding.ASCII.GetBytes(hex));
        long size = Math.Abs((long)group);
        var result = new StringBuilder();
        for (int index = 0; index < data.Length; index++)
        {
            if (index > 0 && (group < 0 ? index % size == 0 : (data.Length - index) % size == 0)) result.Append((char)sep);
            result.Append(hex, index * 2, 2);
        }
        return new PyBytes(Encoding.Latin1.GetBytes(result.ToString()));
    }

    private static long Crc32(object? value, object? initial)
    {
        // CPython 的 unsigned mask 入口允许任意精度初值；CRC 是校验和，不能用于认证。
        using var lease = PyBinaryBuffer.Acquire(value);
        uint crc = (uint)(PyBinaryBuffer.Index(initial) & uint.MaxValue) ^ uint.MaxValue;
        foreach (byte current in lease.Span) crc = CrcTable[(crc ^ current) & 255] ^ (crc >> 8);
        return crc ^ uint.MaxValue;
    }

    private static uint[] CreateCrcTable()
    {
        var table = new uint[256];
        for (int index = 0; index < table.Length; index++)
        {
            uint value = (uint)index;
            for (int bit = 0; bit < 8; bit++) value = (value >> 1) ^ ((value & 1) == 0 ? 0 : 0xedb88320U);
            table[index] = value;
        }
        return table;
    }
}
