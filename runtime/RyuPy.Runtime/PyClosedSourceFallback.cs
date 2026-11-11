// 显式静态枚举只恢复封闭注册域的顶层fallback；不证明宿主文件缺失，不执行finder。
using System;
using System.Collections.Generic;
using System.Text.Json;
namespace RyuPy.Runtime;
public static partial class PyModules
{
    private static readonly HashSet<string> ClosedUnplanned = new(StringComparer.Ordinal);

    // v2新增权限先纯验证；v1仍走原解析，不收紧其历史数组接受规则。
    internal static string[] ValidateClosedPlan(JsonElement root)
    {
        RequireCandidateFields(root, "schema_version", "roots", "dependencies", "locations",
            "deferred", "outside", "protected", "closed_unplanned");
        if (root.GetProperty("schema_version").GetInt32() != 2)
            throw new PyValueError("unknown optional source schema");
        var roots = StrictNames(root.GetProperty("roots"));
        var outside = StrictNames(root.GetProperty("outside"));
        _ = StrictNames(root.GetProperty("protected"));
        var closed = StrictNames(root.GetProperty("closed_unplanned"));
        var locations = StrictKeys(root.GetProperty("locations"));
        var dependencies = StrictKeys(root.GetProperty("dependencies"));
        var deferred = StrictKeys(root.GetProperty("deferred"));
        if (locations.Count > 512 || dependencies.Count > 512)
            throw new PyValueError("optional source node budget exceeded");
        foreach (var property in root.GetProperty("dependencies").EnumerateObject())
        {
            _ = StrictNames(property.Value);
            if (!locations.Contains(property.Name)) throw new PyValueError("dependency lacks source identity");
        }
        foreach (string name in closed)
            if (roots.Contains(name) || outside.Contains(name)
                || locations.Contains(name) || deferred.Contains(name))
                throw new PyValueError("conflicting closed source classification");
        foreach (string name in outside)
            if (locations.Contains(name)) throw new PyValueError("conflicting outside source classification");
        var result = new string[closed.Count]; closed.CopyTo(result); return result;
    }
    private static HashSet<string> StrictNames(JsonElement value)
    {
        var result = new HashSet<string>(StringComparer.Ordinal);
        foreach (string name in Strings(value))
            if (!ValidClosedName(name) || !result.Add(name))
                throw new PyValueError("invalid or duplicate source name");
        return result;
    }
    private static HashSet<string> StrictKeys(JsonElement value)
    {
        var result = new HashSet<string>(StringComparer.Ordinal);
        foreach (var property in value.EnumerateObject())
            if (!ValidClosedName(property.Name) || !result.Add(property.Name))
                throw new PyValueError("invalid or duplicate source key");
        return result;
    }
    private static bool ValidClosedName(string name)
    {
        foreach (string part in name.Split('.'))
        {
            if (part.Length == 0 || !(char.IsAsciiLetter(part[0]) || part[0] == '_')) return false;
            foreach (char c in part) if (!(char.IsAsciiLetterOrDigit(c) || c == '_')) return false;
        }
        return true;
    }
    private static void AddClosedPrefixes(string name)
    {
        ClosedUnplanned.Add(name);
        for (int dot = name.IndexOf('.'); dot >= 0; dot = name.IndexOf('.', dot + 1))
            ClosedUnplanned.Add(name[..dot]);
    }
    // 仅真正Import的最终fallback调用。Cache/Registry及旧来源path验证均先发生。
    internal static void RequireClosedFallback(string name)
    {
        if (name.Contains('.') || CandidateLocations.ContainsKey(name)
            || ActiveUnavailable.ContainsKey(name) || OutsideSources.Contains(name))
            throw CandidateUnsupported("closed fallback is outside the top-level domain");
        // 非完整父族的手工计划也不能借closed前缀掩盖已有候选/延期身份。
        foreach (string candidate in CandidateLocations.Keys)
            if (candidate.StartsWith(name + ".", StringComparison.Ordinal))
                throw CandidateUnsupported("closed fallback overlaps a candidate family");
        foreach (string protectedName in ProtectedNative)
            if (protectedName == name || protectedName.StartsWith(name + ".", StringComparison.Ordinal))
                throw CandidateUnsupported("closed fallback collides with a certified native identity");
        if (!Cache.TryGetValue("sys", out var system) || !PySystem.HasOriginalImportPath(system))
            throw CandidateUnsupported("closed fallback requires original sys/path without finder hooks");
    }
}
