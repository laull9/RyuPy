// 候选代码目录独立于Registry/Cache；真实导入或finder才整组无回调check/commit。
// 首切只支持新鲜串行产品；不迁移已缓存native父，不回滚提交后的正文副作用。
using System;
using System.Collections.Generic;
using System.Text.Json;
namespace RyuPy.Runtime;
public static partial class PyModules
{
    private static readonly Dictionary<string, Registration> CandidateCode = new(StringComparer.Ordinal);
    private static readonly Dictionary<string, Registration> ActiveUnavailable = new(StringComparer.Ordinal);
    private static readonly Dictionary<string, string> CandidateErrors = new(StringComparer.Ordinal);
    private static readonly Dictionary<string, Registration> CandidateLocations = new(StringComparer.Ordinal);
    private static readonly Dictionary<string, string[]> CandidateDependencies = new(StringComparer.Ordinal);
    private static readonly HashSet<string> CandidateRoots = new(StringComparer.Ordinal);
    private static readonly HashSet<string> ProtectedNative = new(StringComparer.Ordinal);
    private static readonly HashSet<string> OutsideSources = new(StringComparer.Ordinal);
    private static string? candidatePlan;

    /// <summary>冷注册只保存代码，不进入活动表、不初始化、不改变父属性。</summary>
    public static void RegisterCandidateSource(string name, string package, string file, long kind,
        string path, bool globals, bool isPackage, PyModuleInit init, PyModuleGet get, PyModuleSet set)
    {
        if (kind is not (1 or 2)) throw new PyValueError("unknown candidate source location");
        string resolved = PyModuleLocations.Resolve(kind, path);
        AddCandidate(name, new Registration(package, file, init, get, set, resolved, globals,
            new ResourceLocation(isPackage, kind, resolved), true));
    }
    /// <summary>实例候选保原执行ABI；此冷路不创建模块实例。</summary>
    public static void RegisterCandidateInstance(string name, string package, string file, long kind,
        string path, bool isPackage, PyModuleExecute execute)
    {
        if (kind is not (1 or 2)) throw new PyValueError("unknown candidate instance location");
        string resolved = PyModuleLocations.Resolve(kind, path);
        var code = new PyCompiledModuleCode(name, package, resolved, isPackage, execute);
        AddCandidate(name, new Registration(package, file, () => { },
            _ => throw new PyRuntimeError("candidate instance has no singleton globals"), SetDynamic,
            resolved, true, new ResourceLocation(isPackage, kind, resolved), true, code));
    }
    private static void AddCandidate(string name, Registration registration)
    {
        if (!CandidateCode.TryAdd(name, registration)) throw new PyValueError("duplicate candidate source identity");
    }

    /// <summary>编译器完整静态计划；所有字段验证后发布，不能从Python属性推导权限。</summary>
    public static void ConfigureOptionalSources(string payload)
    {
        if (candidatePlan is not null)
        {
            if (candidatePlan == payload) return;
            throw new PyNotImplementedError("conflicting optional source product");
        }
        if (payload.Length > 4 * 1024 * 1024) throw new PyValueError("optional source plan exceeds budget");
        using var document = JsonDocument.Parse(payload);
        var root = document.RootElement;
        if (!root.TryGetProperty("schema_version", out var schema))
            throw new PyValueError("missing optional source field");
        int version = schema.GetInt32();
        if (version == 3) { ConfigureDefaultSources(root, payload); return; }
        foreach (var code in CandidateCode.Values)
            if (code.ResourceLocation is not { Kind: 1 }) throw new PyValueError("old candidate plans require kind1");
        string[] closed = [];
        if (version == 1)
            RequireCandidateFields(root, "schema_version", "roots", "dependencies", "locations", "deferred", "outside", "protected");
        else if (version == 2) closed = ValidateClosedPlan(root);
        else throw new PyValueError("unknown optional source schema");
        var dependencies = new Dictionary<string, string[]>(StringComparer.Ordinal);
        var locations = new Dictionary<string, Registration>(StringComparer.Ordinal);
        var errors = new Dictionary<string, string>(StringComparer.Ordinal);
        var roots = Strings(root.GetProperty("roots"));
        var protect = Strings(root.GetProperty("protected"));
        var outside = Strings(root.GetProperty("outside"));
        foreach (var item in root.GetProperty("dependencies").EnumerateObject())
            dependencies.Add(item.Name, Strings(item.Value));
        foreach (var item in root.GetProperty("locations").EnumerateObject())
        {
            var value = item.Value;
            RequireCandidateFields(value, "package", "file", "path", "is_package");
            string path = PyModuleLocations.Resolve(1, value.GetProperty("path").GetString()!);
            locations.Add(item.Name, new Registration(value.GetProperty("package").GetString()!,
                value.GetProperty("file").GetString()!, () => { }, _ => null, SetDynamic, path, true,
                new ResourceLocation(value.GetProperty("is_package").GetBoolean(), 1, path), true));
        }
        foreach (var item in root.GetProperty("deferred").EnumerateObject())
        {
            if (!locations.ContainsKey(item.Name)) throw new PyValueError("deferred source lacks real location");
            errors.Add(item.Name, item.Value.GetString()!);
        }
        if (dependencies.Count > 512 || locations.Count > 512) throw new PyValueError("optional source node budget exceeded");
        foreach (string name in roots)
            if (!locations.ContainsKey(name)) throw new PyValueError("candidate root lacks source identity");
        foreach (string name in locations.Keys)
            if (!CandidateCode.ContainsKey(name) && !errors.ContainsKey(name)) throw new PyValueError("source identity lacks code or failure");
        foreach (string name in CandidateCode.Keys)
            if (!locations.ContainsKey(name)) throw new PyValueError("candidate code lacks source identity");
        foreach (var pair in dependencies) CandidateDependencies.Add(pair.Key, pair.Value);
        foreach (var pair in locations) CandidateLocations.Add(pair.Key, pair.Value);
        foreach (var pair in errors) CandidateErrors.Add(pair.Key, pair.Value);
        CandidateRoots.UnionWith(roots); ProtectedNative.UnionWith(protect);
        foreach (string name in outside)
        {
            OutsideSources.Add(name);
            for (int dot = name.IndexOf('.'); dot >= 0; dot = name.IndexOf('.', dot + 1)) OutsideSources.Add(name[..dot]);
        }
        foreach (string name in closed) AddClosedPrefixes(name);
        candidatePlan = payload;
    }
    private static void RequireCandidateFields(JsonElement value, params string[] fields)
    {
        var names = new HashSet<string>(StringComparer.Ordinal);
        foreach (var item in value.EnumerateObject())
            if (Array.IndexOf(fields, item.Name) < 0 || !names.Add(item.Name)) throw new PyValueError("unknown or duplicate optional source field");
        if (names.Count != fields.Length) throw new PyValueError("missing optional source field");
    }
    private static string[] Strings(JsonElement value)
    {
        var names = new List<string>();
        foreach (var item in value.EnumerateArray())
        {
            string name = item.GetString() ?? throw new PyValueError("null source name");
            if (name.Length == 0 || names.Count >= 4096) throw new PyValueError("invalid source names/budget");
            names.Add(name);
        }
        return names.ToArray();
    }

    // Import只有没有active实现的明确请求可激活；finder另有来源发现资格，不能混用。
    private static void ActivateCandidates(string name, bool finder)
    {
        if (!CandidateRoots.Contains(name)) return;
        if (Registry.TryGetValue(name, out var existing)
            && (!finder || ReferenceEquals(existing, CandidateCode.GetValueOrDefault(name)))) return;
        if (ActiveUnavailable.ContainsKey(name)) return;
        var closure = new HashSet<string>(StringComparer.Ordinal);
        var pending = new Stack<string>(); pending.Push(name);
        while (pending.TryPop(out string? current))
        {
            if (!closure.Add(current)) continue;
            if (CandidateDependencies.TryGetValue(current, out var dependencies))
                foreach (string dependency in dependencies) pending.Push(dependency);
        }
        // 此处只读内部字典/精确string，无getter、import、hash/eq Python协议。
        if (PySystem.HasExplicitFinderConfiguration || Cache.TryGetValue("sys", out var system)
            && (!PySystem.IsOriginalSystem(system) || system.DynamicAttributes.ContainsKey("meta_path")
                || system.DynamicAttributes.ContainsKey("path_hooks") || system.DynamicAttributes.ContainsKey("path_importer_cache")))
            throw CandidateUnsupported("explicit finder configuration");
        foreach (string member in closure)
        {
            if (!CandidateCode.ContainsKey(member) && !CandidateErrors.ContainsKey(member)) continue;
            if (IsCacheAliasOrDescendant(member)) throw CandidateUnsupported("cache alias collision");
            if (CandidateErrors.ContainsKey(member))
            {
                if (!CandidateLocations.TryGetValue(member, out var unavailable))
                    throw new PyRuntimeError("deferred candidate lost its source identity");
                if (Registry.ContainsKey(member) || Cache.ContainsKey(member))
                    throw CandidateUnsupported("unavailable source collides with an existing identity");
                foreach (string protectedName in ProtectedNative)
                    if (protectedName == member || protectedName.StartsWith(member + ".", StringComparison.Ordinal))
                        throw CandidateUnsupported("unavailable source collides with a certified native identity");
                if (ActiveUnavailable.TryGetValue(member, out var activeUnavailable)
                    && !ReferenceEquals(activeUnavailable, unavailable))
                    throw CandidateUnsupported("unavailable source identity changed");
            }
            if (CandidateCode.TryGetValue(member, out var code))
            {
                if (Registry.TryGetValue(member, out var active) && ReferenceEquals(active, code)) continue;
                if (Cache.ContainsKey(member)) throw CandidateUnsupported("an existing family member is cached");
                if (Registry.TryGetValue(member, out active) && !active.NativeRegistration)
                    throw CandidateUnsupported("active source identity collision");
                foreach (string protectedName in ProtectedNative)
                    if (protectedName == member || protectedName.StartsWith(member + ".", StringComparison.Ordinal))
                        throw CandidateUnsupported("certified native identity collision");
                if (!CandidateLocations.TryGetValue(member, out var location)
                    || location.ResolvedFile != code.ResolvedFile || location.ResourceLocation != code.ResourceLocation)
                    throw new PyRuntimeError("candidate source identity mismatch");
            }
        }
        // 语义检查全部成功后发布；Unavailable不抢任何已有native/source/cache身份。
        foreach (string member in closure)
        {
            if (CandidateCode.TryGetValue(member, out var code)) Registry[member] = code;
            else if (CandidateErrors.ContainsKey(member))
                ActiveUnavailable[member] = CandidateLocations[member];
        }
    }
    private static PyNotImplementedError CandidateUnsupported(string reason) => new("optional source activation: " + reason);
    private static bool ThrowUnavailableImport(string name, PyModule? parent, string? child)
    {
        if (ActiveUnavailable.TryGetValue(name, out var location))
        {
            RequireSourceImportPath(name, parent, child, location);
            throw new PyNotImplementedError(CandidateErrors[name]);
        }
        if (OutsideSources.Contains(name)) throw new PyNotImplementedError("module is outside the planned source domain");
        if (ClosedUnplanned.Contains(name)) RequireClosedFallback(name);
        return false;
    }
}
