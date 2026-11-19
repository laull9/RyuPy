// 应用依赖根由显式marker冻结：只查AppContext目录及其exact internal子目录，旧无marker路径保持原值。
// 编译逻辑位置仍可携带_internal前缀，新布局只在组合绝对路径时映射；不搜索祖先、cwd或环境路径。
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace RyuPy.Runtime;

/// <summary>编译入口与provider共享的只读应用布局冷协议。</summary>
public static partial class PyApplicationLayout
{
    internal const string Marker = ".ryupy-layout.json";
    internal sealed record Layout(string DependencyRoot, bool Canonical);
    private static readonly Lazy<Layout> Current = new(() => ResolveForDirectory(AppContext.BaseDirectory));
    /// <summary>有有效marker时返回同一应用的internal目录，否则返回原AppContext目录。</summary>
    public static string DependencyRoot => Current.Value.DependencyRoot;
    /// <summary>仅新布局把旧逻辑_internal前缀映射到依赖根内部，旧产品原值保持。</summary>
    public static string MapLegacyRelative(string relative) => MapLegacyRelative(relative, Current.Value);

    internal static string MapLegacyRelative(string relative, Layout layout)
    {
        ArgumentNullException.ThrowIfNull(relative);
        if (!layout.Canonical) return relative;
        if (relative == "_internal") return "";
        return relative.StartsWith("_internal/", StringComparison.Ordinal) ? relative[10..] : relative;
    }
    internal static Layout ResolveForDirectory(string directory)
    {
        if (!Path.IsPathFullyQualified(directory)) throw new PyRuntimeError("application base directory is not absolute");
        string root = Path.TrimEndingDirectorySeparator(Path.GetFullPath(directory));
        string first = Path.Combine(root, Marker), second = Path.Combine(root, "internal", Marker);
        bool atBase = HasMarker(first), atChild = HasMarker(second);
        if (atBase && atChild) throw new PyRuntimeError("application layout markers are ambiguous");
        if (!atBase && !atChild) return new(root, false);
        string marker = atBase ? first : second;
        string dependency = Path.GetDirectoryName(marker)!;
        if (Path.GetFileName(dependency) != "internal")
            throw new PyRuntimeError("application layout marker must be inside exact internal directory");
        if ((File.GetAttributes(dependency) & FileAttributes.ReparsePoint) != 0)
            throw new PyRuntimeError("application dependency directory cannot be a link");
        var info = new FileInfo(marker);
        if (info.Length > 4096) throw new PyRuntimeError("application layout marker exceeds size limit");
        try
        {
            byte[] bytes = new byte[4097];
            using var stream = File.OpenRead(marker);
            int count = stream.ReadAtLeast(bytes, bytes.Length, throwOnEndOfStream: false);
            if (count > 4096) throw new PyRuntimeError("application layout marker exceeds size limit");
            using JsonDocument document = JsonDocument.Parse(bytes.AsMemory(0, count));
            JsonElement value = document.RootElement;
            var names = new HashSet<string>(StringComparer.Ordinal);
            if (value.ValueKind != JsonValueKind.Object) throw InvalidMarker();
            foreach (JsonProperty field in value.EnumerateObject())
                if (!names.Add(field.Name)) throw InvalidMarker();
            if (!names.SetEquals(["schema_version", "kind", "payload_mode", "entry", "entry_kind"]) ||
                !value.GetProperty("schema_version").TryGetInt32(out int schema) || schema != 1 ||
                value.GetProperty("kind").GetString() != "ryupy-application-layout" ||
                value.GetProperty("payload_mode").GetString() != "canonical-v1" ||
                value.GetProperty("entry_kind").GetString() is not ("apphost" or "native_aot")) throw InvalidMarker();
            string? entry = value.GetProperty("entry").GetString();
            if (string.IsNullOrEmpty(entry) || entry is "." or ".." || entry.Contains('/') || entry.Contains('\\') ||
                entry.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0 || Array.Exists(entry.ToCharArray(), char.IsControl))
                throw InvalidMarker();
            string application = Path.GetDirectoryName(dependency)!;
            string executable = Path.Combine(application, entry);
            if (!File.Exists(executable) || (File.GetAttributes(executable) & (FileAttributes.Directory | FileAttributes.ReparsePoint)) != 0)
                throw new PyRuntimeError("application layout entry is missing or is not a regular root file");
            return new(dependency, true);
        }
        catch (JsonException error) { throw new PyRuntimeError("invalid application layout marker: " + error.Message); }
        catch (InvalidOperationException) { throw InvalidMarker(); }
    }
    private static bool HasMarker(string path)
    {
        try
        {
            FileAttributes attributes = File.GetAttributes(path);
            if ((attributes & (FileAttributes.Directory | FileAttributes.ReparsePoint)) != 0)
                throw new PyRuntimeError("application layout marker is not a regular file");
            return true;
        }
        catch (FileNotFoundException) { return false; }
        catch (DirectoryNotFoundException) { return false; }
    }
    private static PyRuntimeError InvalidMarker() => new("invalid application layout marker schema");
}
