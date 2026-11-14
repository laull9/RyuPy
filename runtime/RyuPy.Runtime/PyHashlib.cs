// hashlib 的有限静态算法表与 BCL 构造入口；算法名、块大小和工厂身份来自同一元数据，不探测 OpenSSL。
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;

namespace RyuPy.Runtime;

internal static class PyHashlib
{
    internal sealed record Algorithm(string Name, HashAlgorithmName BclName, int Size, int Block);
    private static readonly Algorithm[] Algorithms =
    [
        new("sha256", HashAlgorithmName.SHA256, 32, 64),
        new("sha512", HashAlgorithmName.SHA512, 64, 128),
        new("sha1", HashAlgorithmName.SHA1, 20, 64),
        new("md5", HashAlgorithmName.MD5, 16, 64),
    ];
    private static readonly ConditionalWeakTable<PyFunction, Algorithm> Constructors = new();

    internal static Dictionary<string, object?> CreateAttributes()
    {
        var attributes = new Dictionary<string, object?>(StringComparer.Ordinal);
        var names = new List<object?>();
        foreach (Algorithm algorithm in Algorithms)
        {
            var factory = PyNativeFunctions.Create(algorithm.Name, ["string", "usedforsecurity"], [new PyBytes([]), true], 0,
                arguments => Create(algorithm, arguments[0], arguments[1]), ["pos", "kwonly"]);
            Constructors.Add(factory, algorithm);
            attributes[algorithm.Name] = factory;
            names.Add(algorithm.Name);
        }
        // 只公布实际实现的集合；平台额外算法不自动扩充 Python 合同。
        attributes["algorithms_available"] = new PySet(names.ToArray());
        attributes["algorithms_guaranteed"] = new PySet(names.ToArray());
        attributes["new"] = PyNativeFunctions.Create("new", ["name", "data", "usedforsecurity"], [null, new PyBytes([]), true], 1,
            arguments => CreateNamed(arguments[0], arguments[1], arguments[2]), ["pos", "pos", "kwonly"]);
        return attributes;
    }

    private static PyHashState CreateNamed(object? name, object? data, object? security)
    {
        if (name is not string) throw new PyTypeError("name must be a string");
        _ = PyBinaryBuffer.HashData(data);
        return Create(Resolve(name), data, security);
    }

    internal static Algorithm Resolve(object? value, bool constructor = false)
    {
        if (constructor && value is PyFunction function && Constructors.TryGetValue(function, out Algorithm? algorithm))
            return algorithm;
        if (value is not string name)
        {
            if (constructor) throw new PyNotImplementedError("hmac digestmod supports registered hashlib constructors or algorithm names");
            throw new PyTypeError("name must be a string");
        }
        foreach (Algorithm candidate in Algorithms)
        {
            if (string.Equals(name, candidate.Name, StringComparison.OrdinalIgnoreCase)
                || candidate.Name.StartsWith("sha", StringComparison.Ordinal)
                && string.Equals(name, "sha-" + candidate.Name[3..], StringComparison.OrdinalIgnoreCase)) return candidate;
        }
        throw new PyValueError($"unsupported hash type {name}");
    }

    private static PyHashState Create(Algorithm algorithm, object? data, object? security)
    {
        // usedforsecurity 参与 Python 真值协议，但不会绕过宿主密码策略。
        _ = PyOps.Truthy(security);
        ReadOnlySpan<byte> bytes = PyBinaryBuffer.HashData(data);
        try
        {
            IncrementalHash engine = IncrementalHash.CreateHash(algorithm.BclName);
            try { engine.AppendData(bytes); return new PyHashState(algorithm, engine, false); }
            catch { engine.Dispose(); throw; }
        }
        catch (CryptographicException) { throw new PyValueError($"unsupported hash type {algorithm.Name}"); }
    }
}
