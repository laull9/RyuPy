// 固定CP即时oracle对实际内部候选+普通fallback；仅显式测试构建，不证明编译器优化命中。
#if RECORDS_CP_ORACLE_TEST
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using Xunit;
using Xunit.Abstractions;

namespace RyuPy.Runtime.Tests;

[Collection("生成器帧栈")]
public sealed class PyDictMaterializedCpOracleTests(ITestOutputHelper output)
{
    private readonly List<string[]> _events = [];
    private readonly Dictionary<PyDict, PyDict.StringPairAccess> _access = [];
    private int _fast, _fallback;
    private static string B(bool value) => value ? "true" : "false";
    private static string S(object? value) => PyRepr.Str(value);
    private void Emit(params string[] parts) => _events.Add(parts);

    private void Increment(object owner, object key)
    {
        if (owner is PyDict dict)
        {
            if (!_access.TryGetValue(dict, out var access))
                _access.Add(dict, access = new PyDict.StringPairAccess(dict));
            if (access.TryIncrementMaterializedKey(key)) { _fast++; return; }
        }
        _fallback++;
        // 真实公开协议与产品整数内核；拒绝前没有执行过这些调用，不能重复get/add/set。
        object old = PyOps.CallMethod(owner, "get", [key, PyInt.Box(0L)], [], [])!;
        object updated = PyOps.Binary(PyOps.Add, old, PyInt.Box(1L));
        PyOps.SetItem(owner, key, updated);
    }

    [Fact]
    public void ActualCandidateAndFallbackMatchFreshPinnedCPythonEvents()
        => RunOracle(false);

    [Fact]
    public void DictSubclassTupleKeyRemainsExplicitKnownIncompatibility() => RunOracle(true);

    private void RunOracle(bool known)
    {
        string oraclePath = Environment.GetEnvironmentVariable("RYUPY_RECORDS_CP_ORACLE")
            ?? throw new InvalidOperationException("缺少本次CP oracle");
        string actualPath = Environment.GetEnvironmentVariable(known ? "RYUPY_RECORDS_CP_KNOWN" : "RYUPY_RECORDS_CP_ACTUAL")
            ?? throw new InvalidOperationException("缺少实际事件输出路径");
        string sha = Environment.GetEnvironmentVariable("RYUPY_RECORDS_CP_SHA")
            ?? throw new InvalidOperationException("缺少实际提交");
        string token = Environment.GetEnvironmentVariable("RYUPY_RECORDS_CP_TOKEN")
            ?? throw new InvalidOperationException("缺少本次token");
        using var document = JsonDocument.Parse(File.ReadAllText(oraclePath));
        var expected = document.RootElement;
        Assert.Equal(1, expected.GetProperty("schema").GetInt32());
        Assert.Equal("runtime-candidate-only", expected.GetProperty("scope").GetString());
        Assert.Equal(new[] { 3, 12, 13 }, expected.GetProperty("python").EnumerateArray().Select(x => x.GetInt32()).ToArray());
        Assert.Equal(sha, expected.GetProperty("commit").GetString());
        Assert.Equal(token, expected.GetProperty("token").GetString());
        bool passed = false;
        try
        {
            if (known) ExerciseKnownIncompatibility(); else Exercise();
            var expectedEvents = expected.GetProperty("events").EnumerateArray()
                .Where(x => x[0].GetString()!.StartsWith("custom-", StringComparison.Ordinal) == known).ToArray();
            if (known)
            {
                // CP成功而Runtime按现有布局边界拒绝：明确记录差异，不伪装成相等。
                Assert.Equal(3, expectedEvents.Length);
                Assert.Equal(new[] { "custom-result", "6", "true" }, expectedEvents[2].EnumerateArray().Select(x => x.GetString()).ToArray());
                for (int i = 0; i < 2; i++)
                    Assert.Equal(expectedEvents[i].EnumerateArray().Select(x => x.GetString()).ToArray(), _events[i]);
                Assert.Equal(new[] { "custom-rejected", "NotImplementedError", "dict layout with non-scalar keys is not supported", "0" }, _events[2]);
                Assert.Equal(3, _events.Count); Assert.Equal(0, _fast); Assert.Equal(1, _fallback);
            }
            else
            {
                Assert.Equal(22, expectedEvents.Length); Assert.Equal(expectedEvents.Length, _events.Count);
                for (int i = 0; i < _events.Count; i++)
                    Assert.Equal(expectedEvents[i].EnumerateArray().Select(x => x.GetString()).ToArray(), _events[i]);
                Assert.Equal(311, _fast); Assert.Equal(6, _fallback);
            }
            passed = true;
            output.WriteLine($"records-cp-oracle commit={sha} token={token} events={_events.Count} fast={_fast} fallback={_fallback} scope={(known ? "known-incompatibility" : "runtime-candidate-only")}");
        }
        finally
        {
            // 失败仍保留部分轨迹；CreateNew禁止读取/覆盖旧运行输出。
            using var stream = new FileStream(actualPath, FileMode.CreateNew, FileAccess.Write);
            JsonSerializer.Serialize(stream, new { schema = 1, scope = known ? "known-incompatibility" : "runtime-candidate-only", commit = sha,
                token, passed, fast = _fast, fallback = _fallback, events = _events });
        }
    }

    private void ExerciseKnownIncompatibility()
    {
        var first = new PyTuple(["ann", "buy"]);
        var customClass = (PyClass)PyOps.MakeClass("Custom", [PyType.Get("dict")], null);
        customClass.SetAttr("get", PyNativeFunctions.CreateVariadic("get", args =>
        { var key = (PyTuple)args[1]!; Emit("custom-get", (string)key.Items[0]!, (string)key.Items[1]!); return PyInt.Box(5L); }));
        customClass.SetAttr("__setitem__", PyNativeFunctions.CreateVariadic("set", args =>
        {
            Emit("custom-set", S(args[2]));
            return PyOps.CallValue(PyObject.GetAttr(PyType.Get("dict"), "__setitem__"), [args[0], args[1], args[2]], [], []);
        }));
        PyOps.FinalizeOrdinaryClass(customClass);
        object custom = customClass.Invoke([], [], [])!;
        var error = Assert.Throws<PyNotImplementedError>(() => Increment(custom, first));
        Emit("custom-rejected", "NotImplementedError", error.Message, S(PyOps.Len(custom)));

    }

    private void Exercise()
    {
        var d = new PyDict(); var first = new PyTuple(["ann", "buy"]); Increment(d, first);
        Emit("identity-miss", S(d.Get(first)), B(ReferenceEquals(d.Keys.First(), first)));
        var later = new PyTuple(["ann", "buy"]); Increment(d, later);
        Emit("identity-hit", S(d.Get(later)), B(ReferenceEquals(d.Keys.First(), first)), B(ReferenceEquals(d.Keys.First(), later)));
        d.Delete(first); Increment(d, later);
        Emit("identity-reinsert", S(d.Get(later)), B(ReferenceEquals(d.Keys.First(), later)));

        (string Label, object Seed)[] seeds = [("-6", PyInt.Box(-6L)), ("255", PyInt.Box(255L)),
            ("256", PyInt.Box(256L)), ("max", PyInt.Box(long.MaxValue)), ("min", PyInt.Box(long.MinValue)), ("bool", true)];
        foreach (var (label, seed) in seeds)
        {
            d = new PyDict(); d.Set(first, seed); Increment(d, first); object value = d.Get(first)!;
            // CP int(str(value))在小整数域返回canonical对象；这里直接比较产品装箱合同，
            // 不把另一个字符串解析入口的缓存差异混入本候选。
            object reference = value is long number ? PyInt.Box(number) : PyOps.FromDecimalString(S(value));
            Emit("integer", label, S(value), PyRepr.TypeName(value), B(ReferenceEquals(value, reference)));
        }

        d = new PyDict(); var empty = new PyTuple(["", "buy"]); var nul = new PyTuple(["\0", "buy"]);
        Increment(d, empty); Increment(d, nul); Increment(d, empty);
        foreach (var (key, value) in d.Entries()) Emit("collision-domain", (string)((PyTuple)key!).Items[0]!, S(value));

        d = new PyDict(); var replacement = new PyTuple(["ann", "buy"]);
        var replaceClass = new PyClass("ReplaceCounter");
        replaceClass.SetAttr("__add__", PyNativeFunctions.CreateVariadic("add", args =>
        {
            Emit("add-replace", S(args[1]), B(ReferenceEquals(d.Keys.First(), first)));
            d.Delete(first); d.Set(replacement, PyInt.Box(100L)); return PyInt.Box(41L);
        }));
        d.Set(first, new PyInstance(replaceClass)); Increment(d, first);
        Emit("replace-result", S(d.Get(first)), B(ReferenceEquals(d.Keys.First(), replacement)), B(ReferenceEquals(d.Keys.First(), first)));

        var throwClass = new PyClass("ThrowCounter");
        throwClass.SetAttr("__add__", PyNativeFunctions.CreateVariadic("add", args =>
        { Emit("add-throw", S(args[1])); throw new PyValueError("counter failed"); }));
        d = new PyDict(); var old = new PyInstance(throwClass); d.Set(first, old);
        try { Increment(d, first); Assert.Fail("必须传播ValueError"); }
        catch (PyValueError error) { Emit("exception", "ValueError", error.Message, B(ReferenceEquals(d.Get(first), old))); }

        bool observing = false; var keyClass = new PyClass("UserKey");
        var stored = new PyInstance(keyClass); var query = new PyInstance(keyClass);
        string Label(object? key) => ReferenceEquals(key, stored) ? "stored" : ReferenceEquals(key, query) ? "query" : "unexpected";
        keyClass.SetAttr("__hash__", PyNativeFunctions.CreateVariadic("hash", args =>
        { if (observing) Emit("hash", Label(args[0])); return 123L; }));
        keyClass.SetAttr("__eq__", PyNativeFunctions.CreateVariadic("eq", args =>
        { if (observing) Emit("eq", Label(args[0]), Label(args[1])); return true; }));
        d = new PyDict(); d.Set(stored, PyInt.Box(4L)); observing = true; Increment(d, query);
        var entry = d.Entries().First();
        Emit("user-key-result", S(entry.Value), B(ReferenceEquals(entry.Key, stored)), B(ReferenceEquals(entry.Key, query)));
        observing = false;

        d = new PyDict();
        for (int i = 0; i < 300; i++) Increment(d, new PyTuple([i.ToString(System.Globalization.CultureInfo.InvariantCulture), "buy"]));
        object zero = d.Keys.First()!; d.Set(new PyTuple(["outside", "buy"]), PyInt.Box(0L)); Increment(d, zero);
        Emit("budget-external", d.Count.ToString(), S(d.Get(zero)), B(ReferenceEquals(d.Keys.First(), zero)));
        d.Clear(); Increment(d, zero);
        Emit("budget-clear", d.Count.ToString(), S(d.Get(zero)), B(ReferenceEquals(d.Keys.First(), zero)));
    }
}
#endif
