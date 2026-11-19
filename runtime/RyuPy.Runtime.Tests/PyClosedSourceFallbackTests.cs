// v2封闭fallback独立宿主反例；测试暂存静态候选状态，finally恢复，不提供生产reset接口。
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Text.Json;
using RyuPy.Runtime;
using Xunit;
namespace RyuPy.Runtime.Tests;
[Collection("生成器帧栈")]
public class PyClosedSourceFallbackTests
{
    private static string Payload(int version, string[] closed, string[]? outside = null, string[]? protect = null)
    {
        var value = new Dictionary<string, object> {
            ["schema_version"] = version, ["roots"] = Array.Empty<string>(),
            ["dependencies"] = new Dictionary<string, object>(), ["locations"] = new Dictionary<string, object>(),
            ["deferred"] = new Dictionary<string, object>(), ["outside"] = outside ?? [],
            ["protected"] = protect ?? [],
        };
        if (version == 2) value["closed_unplanned"] = closed;
        return JsonSerializer.Serialize(value);
    }
    [Fact]
    public void ClosedPlanValidationRejectsMalformedAndConflictingAuthority()
    {
        void Reject(string value) {
            using var document = JsonDocument.Parse(value);
            Assert.Throws<PyValueError>(() => PyModules.ValidateClosedPlan(document.RootElement));
        }
        Reject(Payload(2, ["bad-name"]));
        Reject(Payload(2, ["name", "name"]));
        Reject(Payload(2, ["name"], ["name"]));
        Reject(Payload(2, ["name"]).Replace("\"roots\":[]", "\"roots\":[],\"roots\":[]"));
        Reject(Payload(2, ["name"]).Replace("\"schema_version\":2", "\"schema_version\":9"));
        using var valid = JsonDocument.Parse(Payload(2, ["p.a"], ["p.b"]));
        Assert.Equal(new[] { "p.a" }, PyModules.ValidateClosedPlan(valid.RootElement));
    }
    [Fact]
    public void ClosedFallbackPreservesV1AtomicityPriorityAndLateSystemGuards()
    {
        PySystem.Initialize("closed-fallback-tests.py", []);
        using var saved = new OptionalState();
        var sys = PyModules.Import("sys");
        var dynamic = new Dictionary<string, object?>(sys.DynamicAttributes);
        var cache = (Dictionary<string, PyModule>)typeof(PyModules).GetField("Cache", BindingFlags.Static | BindingFlags.NonPublic)!.GetValue(null)!;
        var attributes = (Dictionary<string, object?>)typeof(PySystem).GetField("originalAttributes", BindingFlags.Static | BindingFlags.NonPublic)!.GetValue(null)!;
        var originalAttributes = new Dictionary<string, object?>(attributes);
        var hooks = typeof(PySystem).GetProperty("HasExplicitFinderConfiguration", BindingFlags.Static | BindingFlags.NonPublic)!;
        object? oldHooks = hooks.GetValue(null);
        string prefix = "closed_" + Guid.NewGuid().ToString("N");
        string absent = prefix + "_absent", cross = prefix + "_cross", parent = prefix + "_parent";
        string cached = prefix + "_cached", registered = prefix + "_registered", protectedAbsent = prefix + "_protected";
        try
        {
            hooks.SetValue(null, false); sys.DynamicAttributes.Clear();
            foreach (string key in new[] { "meta_path", "path_hooks", "path_importer_cache" }) attributes.Remove(key);
            Assert.Throws<PyValueError>(() => PyModules.ConfigureOptionalSources("{}"));
            // v1重复数组旧行为保留，且无closed字段不升级。
            PyModules.ConfigureOptionalSources(Payload(1, [], [prefix, prefix]));
            Assert.Throws<PyNotImplementedError>(() => PyModules.Import(prefix));
            saved.Clear();
            // 失败包不能发布部分closed/outside或占用一次配置身份。
            Assert.Throws<PyValueError>(() => PyModules.ConfigureOptionalSources(Payload(2, [absent, absent], [cross])));
            Assert.Throws<PyModuleNotFoundError>(() => PyModules.Import(cross));
            PyModules.ConfigureOptionalSources(Payload(2,
                [absent, cross + ".a", parent + ".missing", cached, registered, protectedAbsent],
                [cross + ".b"], [registered, protectedAbsent]));
            Assert.Throws<PyNotImplementedError>(() => PyModules.Import(protectedAbsent));
            Assert.Throws<PyModuleNotFoundError>(() => PyModules.Import(absent));
            Assert.Throws<PyModuleNotFoundError>(() => PyModules.Import(parent + ".missing"));
            // Outside前缀胜过closed前缀，未把预算拒绝降为缺失。
            Assert.Throws<PyNotImplementedError>(() => PyModules.Import(cross + ".a"));
            PyModules.RegisterNative(parent, new Dictionary<string, object?>());
            PyModules.Import(parent);
            Assert.Throws<PyNotImplementedError>(() => PyModules.Import(parent + ".missing"));
            PyModules.RegisterNative(cached, new Dictionary<string, object?>());
            var original = PyModules.Import(cached);
            PyModules.RegisterNative(registered, new Dictionary<string, object?> { ["marker"] = 7L });
            sys.DynamicAttributes["path_hooks"] = new PyList([]);
            Assert.Throws<PyNotImplementedError>(() => PyModules.Import(absent));
            Assert.Same(original, PyModules.Import(cached));
            Assert.Equal(7L, PyModules.Import(registered).GetAttr("marker"));
            sys.DynamicAttributes.Remove("path_hooks");
            sys.DynamicAttributes["path"] = new PyTuple([]);
            Assert.Throws<PyNotImplementedError>(() => PyModules.Import(absent));
            sys.DynamicAttributes.Remove("path");
            object? path = attributes["path"];
            attributes.Remove("path");
            PySystem.Initialize("closed-path-missing.py", []);
            Assert.Throws<PyNotImplementedError>(() => PyModules.Import(absent));
            attributes["path"] = new PyTuple([]);
            Assert.Throws<PyNotImplementedError>(() => PyModules.Import(absent));
            attributes["path"] = path;
            Assert.Throws<PyModuleNotFoundError>(() => PyModules.Import(absent));
            // 实际setter的sticky事实在删除后仍拒绝；Initialize不能重新签发path。
            sys.SetAttr("path_hooks", new PyList([]));
            attributes.Remove("path_hooks");
            PySystem.Initialize("closed-fallback-again.py", []);
            Assert.Throws<PyNotImplementedError>(() => PyModules.Import(absent));
            hooks.SetValue(null, false);
            cache.Remove("sys");
            Assert.Throws<PyNotImplementedError>(() => PyModules.Import(absent));
            cache["sys"] = original;
            Assert.Throws<PyNotImplementedError>(() => PyModules.Import(absent));
        }
        finally
        {
            cache["sys"] = sys;
            attributes.Clear(); foreach (var pair in originalAttributes) attributes.Add(pair.Key, pair.Value);
            hooks.SetValue(null, oldHooks);
            sys.DynamicAttributes.Clear(); foreach (var pair in dynamic) sys.DynamicAttributes.Add(pair.Key, pair.Value);
        }
    }
    // 仅隔离候选配置，不删除现有Registry/Cache；每个新增名字唯一。
    private sealed class OptionalState : IDisposable
    {
        private readonly List<(IDictionary Target, Hashtable Saved)> dictionaries = [];
        private readonly List<(HashSet<string> Target, string[] Saved)> sets = [];
        private readonly FieldInfo plan = typeof(PyModules).GetField("candidatePlan", BindingFlags.Static | BindingFlags.NonPublic)!;
        private readonly object? payload;
        public OptionalState()
        {
            payload = plan.GetValue(null);
            foreach (string name in new[] { "CandidateCode", "ActiveUnavailable", "CandidateErrors", "CandidateLocations", "CandidateDependencies" })
            {
                var value = (IDictionary)typeof(PyModules).GetField(name, BindingFlags.Static | BindingFlags.NonPublic)!.GetValue(null)!;
                dictionaries.Add((value, new Hashtable(value)));
            }
            foreach (string name in new[] { "CandidateRoots", "ProtectedNative", "OutsideSources", "ClosedUnplanned" })
            {
                var value = (HashSet<string>)typeof(PyModules).GetField(name, BindingFlags.Static | BindingFlags.NonPublic)!.GetValue(null)!;
                var copy = new string[value.Count]; value.CopyTo(copy); sets.Add((value, copy));
            }
            Clear();
        }
        public void Clear()
        {
            plan.SetValue(null, null);
            foreach (var pair in dictionaries) pair.Target.Clear();
            foreach (var pair in sets) pair.Target.Clear();
        }
        public void Dispose()
        {
            Clear(); plan.SetValue(null, payload);
            foreach (var pair in dictionaries) foreach (DictionaryEntry entry in pair.Saved) pair.Target.Add(entry.Key, entry.Value);
            foreach (var pair in sets) pair.Target.UnionWith(pair.Saved);
        }
    }
}
