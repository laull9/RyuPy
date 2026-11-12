// candidate v3：全发现图的冷身份一次验证后发布，不从公开Python属性反推权限。
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
namespace RyuPy.Runtime;
public static partial class PyModules
{
    // 纯结构阶段不触平台位置/注册表；测试只能证明解码，不能冒充Native authority。
    internal static void ValidateDefaultSourceStructure(JsonElement root)
    {
        RequireCandidateFields(root, "schema_version", "roots", "dependencies", "locations",
            "deferred", "outside", "protected", "closed_unplanned", "external_dependencies");
        if (root.GetProperty("schema_version").GetInt32() != 3) throw new PyValueError("not default source v3");
        foreach (string field in new[] { "roots", "outside", "protected", "closed_unplanned", "external_dependencies" })
            _ = StrictNames(root.GetProperty(field));
        var locations = StrictKeys(root.GetProperty("locations"));
        if (locations.Count > 256) throw new PyValueError("default source location budget");
        _ = StrictKeys(root.GetProperty("dependencies"));
        _ = StrictKeys(root.GetProperty("deferred"));
        foreach (var item in root.GetProperty("locations").EnumerateObject())
        {
            var value = item.Value;
            RequireCandidateFields(value, "package", "file", "kind", "path", "is_package", "registration");
            if (value.GetProperty("kind").GetInt32() != 2) throw new PyValueError("not default kind2");
            string? package = value.GetProperty("package").GetString();
            string? file = value.GetProperty("file").GetString();
            string? path = value.GetProperty("path").GetString();
            string? registration = value.GetProperty("registration").GetString();
            bool isPackage = value.GetProperty("is_package").GetBoolean();
            string expectedPackage = isPackage ? item.Name
                : item.Name.Contains('.') ? item.Name[..item.Name.LastIndexOf('.')] : "";
            if (package is null || package != expectedPackage || string.IsNullOrEmpty(file) || string.IsNullOrEmpty(path)
                || registration is not ("active" or "candidate" or "deferred"))
                throw new PyValueError("invalid default source shape");
        }
    }
    private static void ConfigureDefaultSources(JsonElement root, string payload)
    {
        ValidateDefaultSourceStructure(root);
        RequireCandidateFields(root, "schema_version", "roots", "dependencies", "locations",
            "deferred", "outside", "protected", "closed_unplanned", "external_dependencies");
        var external = StrictNames(root.GetProperty("external_dependencies"));
        var roots = StrictNames(root.GetProperty("roots"));
        var outside = StrictNames(root.GetProperty("outside"));
        var protect = StrictNames(root.GetProperty("protected"));
        var closed = StrictNames(root.GetProperty("closed_unplanned"));
        _ = StrictKeys(root.GetProperty("locations"));
        _ = StrictKeys(root.GetProperty("dependencies"));
        _ = StrictKeys(root.GetProperty("deferred"));
        var locations = new Dictionary<string, Registration>(StringComparer.Ordinal);
        var classes = new Dictionary<string, string>(StringComparer.Ordinal);
        var errors = new Dictionary<string, string>(StringComparer.Ordinal);
        var dependencies = new Dictionary<string, string[]>(StringComparer.Ordinal);
        foreach (var item in root.GetProperty("deferred").EnumerateObject())
        {
            string reason = item.Value.GetString() ?? "";
            if (reason.Length == 0) throw new PyValueError("empty deferred source reason");
            errors.Add(item.Name, reason);
        }
        foreach (var item in root.GetProperty("locations").EnumerateObject())
        {
            var value = item.Value;
            RequireCandidateFields(value, "package", "file", "kind", "path", "is_package", "registration");
            if (value.GetProperty("kind").GetInt32() != 2) throw new PyValueError("default source kind must be 2");
            string path = PyModuleLocations.Resolve(2, value.GetProperty("path").GetString()!);
            string package = value.GetProperty("package").GetString()
                ?? throw new PyValueError("null source package");
            string file = value.GetProperty("file").GetString()
                ?? throw new PyValueError("null source file");
            bool isPackage = value.GetProperty("is_package").GetBoolean();
            string expectedPackage = isPackage ? item.Name
                : item.Name.Contains('.') ? item.Name[..item.Name.LastIndexOf('.')] : "";
            if (file.Length == 0 || package != expectedPackage)
                throw new PyValueError("incoherent source package or empty file");
            string classification = value.GetProperty("registration").GetString()!;
            // 配置必须发生在域正文前；同名或已初始化子族不能事后补权限。
            foreach (string cached in Cache.Keys)
                if (cached == item.Name || cached.StartsWith(item.Name + ".", StringComparison.Ordinal))
                    throw new PyValueError("default source family already initialized");
            if (IsCacheAliasOrDescendant(item.Name)) throw new PyValueError("default source cache alias");
            Registration registration;
            if (classification == "candidate")
            {
                if (!CandidateCode.TryGetValue(item.Name, out registration!) || errors.ContainsKey(item.Name)
                    || (Registry.TryGetValue(item.Name, out var active) && !active.NativeRegistration))
                    throw new PyValueError("invalid candidate registration identity");
            }
            else if (classification == "active")
            {
                if (!Registry.TryGetValue(item.Name, out registration!) || registration.NativeRegistration
                    || CandidateCode.ContainsKey(item.Name) || errors.ContainsKey(item.Name)
                    || IsCacheAliasOrDescendant(item.Name)) throw new PyValueError("invalid active source identity");
            }
            else if (classification == "deferred")
            {
                if (Registry.ContainsKey(item.Name) || CandidateCode.ContainsKey(item.Name)
                    || !errors.ContainsKey(item.Name)) throw new PyValueError("invalid deferred source identity");
                registration = new Registration(package, file, () => { }, _ => null, SetDynamic,
                    path, true, new ResourceLocation(isPackage, 2, path), true);
            }
            else throw new PyValueError("unknown source registration class");
            if (!registration.MetadataEnabled || registration.Package != package || registration.File != file
                || registration.ResolvedFile != path || registration.ResourceLocation is not { Kind: 2 } source
                || source.IsPackage != isPackage || source.Anchor != path)
                throw new PyValueError("default source location disagrees with registration");
            if (registration.InstanceCode is { } code && (code.Name != item.Name || code.Package != package
                || code.File != path || code.IsPackage != isPackage))
                throw new PyValueError("default source instance identity mismatch");
            locations.Add(item.Name, registration); classes.Add(item.Name, classification);
        }
        if (locations.Count > 256) throw new PyValueError("default source locations exceed budget");
        foreach (string name in CandidateCode.Keys)
            if (!classes.TryGetValue(name, out var kind) || kind != "candidate") throw new PyValueError("uncovered candidate code");
        foreach (string name in errors.Keys)
            if (!classes.TryGetValue(name, out var kind) || kind != "deferred") throw new PyValueError("uncovered deferred source");
        foreach (string name in roots)
            if (!classes.TryGetValue(name, out var kind) || kind == "active") throw new PyValueError("invalid candidate root");
        foreach (string name in outside)
            if (locations.ContainsKey(name) || closed.Contains(name)) throw new PyValueError("outside identity conflict");
        foreach (string name in closed)
            if (locations.ContainsKey(name) || roots.Contains(name) || protect.Contains(name)) throw new PyValueError("closed identity conflict");
        foreach (var item in root.GetProperty("dependencies").EnumerateObject())
        {
            if (!locations.ContainsKey(item.Name)) throw new PyValueError("dependency source missing");
            var names = StrictNames(item.Value);
            var values = new string[names.Count]; names.CopyTo(values); dependencies.Add(item.Name, values);
        }
        var remaining = new HashSet<string>(StringComparer.Ordinal);
        foreach (var values in dependencies.Values)
            foreach (string name in values)
                if (!locations.ContainsKey(name) && !outside.Contains(name) && !closed.Contains(name) && !protect.Contains(name)) remaining.Add(name);
        if (!remaining.SetEquals(external)) throw new PyValueError("external dependency classification mismatch");
        foreach (var pair in locations)
        {
            int dot = pair.Key.LastIndexOf('.');
            if (dot < 0) continue;
            if (!locations.TryGetValue(pair.Key[..dot], out var parent)
                || parent.ResourceLocation is not { IsPackage: true } parentLocation)
                throw new PyValueError("default source lacks real package ancestor");
            string directory = Path.GetDirectoryName(parentLocation.Anchor)!;
            string child = pair.Key[(dot + 1)..];
            string expected = pair.Value.ResourceLocation!.IsPackage
                ? Path.Combine(directory, child, "__init__.py") : Path.Combine(directory, child + ".py");
            if (Path.GetFileName(parentLocation.Anchor) != "__init__.py" || expected != pair.Value.ResolvedFile)
                throw new PyValueError("default source parent geometry mismatch");
        }
        var codes = new HashSet<PyCompiledModuleCode>(ReferenceEqualityComparer.Instance);
        foreach (var registration in locations.Values)
            if (registration.InstanceCode is { } code && !codes.Add(code))
                throw new PyValueError("duplicate default source code identity");
        // 旧静态状态只能为空；所有会抛的语义核验均在发布前，且不执行用户回调。
        if (CandidateLocations.Count != 0 || CandidateDependencies.Count != 0 || DefaultSourceAuthority.Count != 0)
            throw new PyValueError("default source state is already configured");
        foreach (var pair in locations)
        {
            CandidateLocations.Add(pair.Key, pair.Value); DefaultSourceAuthority.Add(pair.Value);
            if (pair.Value.InstanceCode is { } code) DefaultCodeAuthority.Add(code, pair.Value);
        }
        foreach (var pair in dependencies) CandidateDependencies.Add(pair.Key, pair.Value);
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
}
