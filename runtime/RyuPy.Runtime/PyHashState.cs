// hashlib/HMAC 的有界增量状态对象：BCL Clone/GetCurrentHash 保持非破坏性快照，不缓存或重放历史明文。
using System;
using System.Security.Cryptography;

namespace RyuPy.Runtime;

internal sealed class PyHashState(PyHashlib.Algorithm algorithm, IncrementalHash engine, bool authenticated) : PyNativeObject
{
    // 序列化同一对象的操作；Python 当前无并行入口，但状态复制也不依赖 BCL 的并发安全假设。
    private readonly object gate = new();
    public override string TypeName => authenticated ? "HMAC" : "HASH";
    internal bool Authenticated => authenticated;

    public override object? GetAttr(string name) => name switch
    {
        "name" => (authenticated ? "hmac-" : "") + algorithm.Name,
        "digest_size" => (long)algorithm.Size,
        "block_size" => (long)algorithm.Block,
        "update" => PyNativeFunctions.Create("update", [authenticated ? "msg" : "data"], [null], 1,
            arguments => Update(arguments[0]), [authenticated ? "pos" : "posonly"]),
        "digest" => NoArgs("digest", () => new PyBytes(Digest())),
        "hexdigest" => NoArgs("hexdigest", () => Convert.ToHexStringLower(Digest())),
        "copy" => NoArgs("copy", Copy),
        _ => throw Missing(name),
    };

    public override void SetAttr(string name, object? value)
    {
        if (authenticated && name is "digest_size" or "block_size")
            throw new PyNotImplementedError("writing HMAC metadata is not supported");
        base.SetAttr(name, value);
    }

    private static PyFunction NoArgs(string name, Func<object?> operation) =>
        PyNativeFunctions.Create(name, [], [], 0, _ => operation());

    private object Update(object? value)
    {
        ReadOnlySpan<byte> bytes = PyBinaryBuffer.HashData(value);
        lock (gate) engine.AppendData(bytes);
        return PyNone.Instance;
    }

    internal byte[] Digest()
    {
        lock (gate) return engine.GetCurrentHash();
    }

    private object Copy()
    {
        lock (gate) return new PyHashState(algorithm, engine.Clone(), authenticated);
    }
}
