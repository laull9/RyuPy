// 程序集owner拥有不可变分发载荷；拒绝晚配/跨产品覆盖，不从源路径读取文件。
using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
namespace RyuPy.Runtime;
/// <summary>生成程序集拥有的不可变分发快照。</summary>
public static class PyDistributionSnapshot
{
    private static readonly object Gate = new();
    private static Type? owner;
    private static string? payload;
    private static bool observed;
    /// <summary>验证完整静态载荷后按真实类型owner原子发布；冲突拒绝。</summary>
    public static void Configure(Type productOwner, string value)
    {
        if (productOwner is null) throw new PyValueError("distribution snapshot owner is required");
        if (value is null) throw new PyValueError("distribution snapshot payload is required");
        // 完全验证候选后才发布；错误不能改变既有状态。
        Validate(value);
        lock (Gate)
        {
            if (owner is not null)
            {
                if (ReferenceEquals(owner, productOwner) && payload == value) return;
                throw Unsupported("conflicting generated assembly owner or payload");
            }
            if (observed) throw Unsupported("configuration after first metadata import");
            owner = productOwner; payload = value;
        }
    }
    internal static void Observe() { lock (Gate) observed = true; }
    internal static string Require()
    {
        lock (Gate) return payload ?? throw Unsupported("entry_points requires an explicit closed distribution snapshot");
    }
    private static void Validate(string value)
    {
        try
        {
            if (Encoding.UTF8.GetByteCount(value) > 4 * 1024 * 1024) throw Unsupported("snapshot exceeds 4MiB");
            using var document = JsonDocument.Parse(value);
            var root = document.RootElement;
            Fields(root, "schema_version", "mode", "roots", "distributions");
            if (root.GetProperty("schema_version").GetInt32() != 1 || root.GetProperty("mode").GetString() != "closed_snapshot")
                throw Unsupported("unknown snapshot schema");
            var roots = new HashSet<string>(StringComparer.Ordinal);
            foreach (var item in root.GetProperty("roots").EnumerateArray())
                if (item.GetString() is not { Length: > 0 } id || !roots.Add(id)) throw Unsupported("invalid root identity");
            if (roots.Count > 64) throw Unsupported("too many roots");
            int count = 0, fileCount = 0, bytes = 0;
            var keys = new HashSet<string>(StringComparer.Ordinal);
            foreach (var dist in root.GetProperty("distributions").EnumerateArray())
            {
                Fields(dist, "root", "path", "kind", "metadata_name", "metadata_version", "discovery_key", "shadowed", "directories", "files");
                if (++count > 256 || !roots.Contains(dist.GetProperty("root").GetString()!)) throw Unsupported("invalid distribution root/count");
                foreach (string field in new[] { "path", "metadata_name", "metadata_version", "discovery_key" })
                    if (dist.GetProperty(field).GetString() is not { Length: > 0 }) throw Unsupported("missing distribution identity");
                if (dist.GetProperty("kind").GetString() is not ("dist-info" or "egg-info")) throw Unsupported("unknown distribution kind");
                bool shadowed = !keys.Add(dist.GetProperty("discovery_key").GetString()!);
                if (dist.GetProperty("shadowed").GetBoolean() != shadowed) throw Unsupported("incoherent distribution precedence");
                foreach (var directory in dist.GetProperty("directories").EnumerateArray()) _ = directory.GetString();
                var names = new HashSet<string>(StringComparer.Ordinal);
                foreach (var file in dist.GetProperty("files").EnumerateArray())
                {
                    Fields(file, "path", "size", "sha256", "hex");
                    if (!names.Add(file.GetProperty("path").GetString()!)) throw Unsupported("duplicate metadata file");
                    byte[] raw = Convert.FromHexString(file.GetProperty("hex").GetString()!);
                    bytes += raw.Length;
                    if (++fileCount > 2048 || bytes > 1024 * 1024 || raw.Length != file.GetProperty("size").GetInt32()
                        || Convert.ToHexString(SHA256.HashData(raw)).ToLowerInvariant() != file.GetProperty("sha256").GetString())
                        throw Unsupported("invalid metadata bytes/hash/budget");
                }
            }
        }
        catch (Exception error) when (error is JsonException or InvalidOperationException or KeyNotFoundException or FormatException or ArgumentException or OverflowException)
        { throw new PyValueError("invalid distribution snapshot: " + error.Message); }
    }
    private static void Fields(JsonElement value, params string[] allowed)
    {
        var found = new HashSet<string>(StringComparer.Ordinal);
        foreach (var item in value.EnumerateObject())
            if (Array.IndexOf(allowed, item.Name) < 0 || !found.Add(item.Name)) throw Unsupported("unknown or duplicate snapshot field");
        if (found.Count != allowed.Length) throw Unsupported("missing snapshot field");
    }
    internal static PyNotImplementedError Unsupported(string detail) => new("importlib.metadata: " + detail);
}
