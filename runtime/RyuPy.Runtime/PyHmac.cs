// HMAC 全部交给 BCL 增量认证；compare_digest 仅承诺同类型同长度字节比较阶段使用 FixedTimeEquals。
using System;
using System.Collections.Generic;
using System.Security.Cryptography;

namespace RyuPy.Runtime;

internal static class PyHmac
{
    internal static Dictionary<string, object?> CreateAttributes() => new(StringComparer.Ordinal)
    {
        ["new"] = PyNativeFunctions.Create("new", ["key", "msg", "digestmod"], [null, PyNone.Instance, PyNone.Instance], 1,
            arguments => Create(arguments[0], arguments[1], arguments[2])),
        ["HMAC"] = new PyNativeType("HMAC", "hmac", (args, names, values) =>
            PyOps.CallValue(PyNativeFunctions.Create("HMAC", ["key", "msg", "digestmod"], [null, PyNone.Instance, PyNone.Instance], 1,
                arguments => Create(arguments[0], arguments[1], arguments[2])), args, names, values), value => value is PyHashState { Authenticated: true }),
        ["digest"] = PyNativeFunctions.Create("digest", ["key", "msg", "digest"], [null, null, null], 3,
            arguments => OneShot(arguments[0], arguments[1], arguments[2])),
        ["compare_digest"] = PyNativeFunctions.Create("compare_digest", ["a", "b"], [null, null], 2,
            arguments => Compare(arguments[0], arguments[1]), ["posonly", "posonly"]),
    };

    private static PyHashState Create(object? key, object? message, object? digest)
    {
        // new/HMAC 的 key 合同是 bytes 或 bytearray；一次性 digest 则接受通用连续 buffer。
        if (key is not PyBytes and not PyByteArray)
            throw new PyTypeError($"key: expected bytes or bytearray, but got '{PyRepr.TypeName(key)}'");
        if (digest is null or PyNone || digest is string { Length: 0 })
            throw new PyTypeError("Missing required argument 'digestmod'.");
        PyHashlib.Algorithm algorithm = PyHashlib.Resolve(digest, constructor: true);
        byte[] secret = PyBinaryBuffer.Read(key).ToArray();
        try
        {
            IncrementalHash engine = IncrementalHash.CreateHMAC(algorithm.BclName, secret);
            try
            {
                if (message is not null and not PyNone) engine.AppendData(PyBinaryBuffer.HashData(message));
                return new PyHashState(algorithm, engine, true);
            }
            catch { engine.Dispose(); throw; }
        }
        catch (CryptographicException) { throw new PyValueError($"unsupported hash type {algorithm.Name}"); }
        finally { CryptographicOperations.ZeroMemory(secret); }
    }

    private static PyBytes OneShot(object? key, object? message, object? digest)
    {
        ReadOnlySpan<byte> keyBytes = PyBinaryBuffer.Read(key);
        ReadOnlySpan<byte> messageBytes = PyBinaryBuffer.Read(message);
        PyHashlib.Algorithm algorithm = PyHashlib.Resolve(digest, constructor: true);
        byte[] secret = keyBytes.ToArray();
        try
        {
            using IncrementalHash engine = IncrementalHash.CreateHMAC(algorithm.BclName, secret);
            engine.AppendData(messageBytes);
            return new PyBytes(engine.GetHashAndReset());
        }
        catch (CryptographicException) { throw new PyValueError($"unsupported hash type {algorithm.Name}"); }
        finally { CryptographicOperations.ZeroMemory(secret); }
    }

    private static bool Compare(object? left, object? right)
    {
        if (left is string leftText && right is string rightText)
        {
            foreach (char current in leftText + rightText)
                if (current > 127) throw new PyTypeError("comparing strings with non-ASCII characters is not supported");
            return CryptographicOperations.FixedTimeEquals(PyBinaryBuffer.Ascii(leftText), PyBinaryBuffer.Ascii(rightText));
        }
        if (left is string || right is string) throw new PyTypeError("a bytes-like object is required, not 'str'");
        return CryptographicOperations.FixedTimeEquals(PyBinaryBuffer.Read(left), PyBinaryBuffer.Read(right));
    }
}
