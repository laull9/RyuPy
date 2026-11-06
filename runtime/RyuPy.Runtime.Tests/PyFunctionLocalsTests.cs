// 普通函数locals缓存/逐槽同步协议：身份、单次字典协议、失败中间态和严格绑定。
using System;
using System.Linq;
using Xunit;

namespace RyuPy.Runtime.Tests;

[Collection("生成器帧栈")]
public sealed class PyFunctionLocalsTests(Xunit.Abstractions.ITestOutputHelper output)
{
    private static PyDict Fresh() => (PyDict)PyOps.FunctionLocalsEnsure(null);
    private static PyFunction Fn(string name, Func<object?[], object?> body) => PyNativeFunctions.CreateVariadic(name, body);
    private static PyInstance Collision(string name, Func<object?, object?> equal)
    {
        var cls = (PyClass)PyOps.MakeClass("LocalKey", [], null);
        cls.SetAttr("__hash__", Fn("__hash__", _ => PyHash.Of(name)));
        cls.SetAttr("__eq__", Fn("__eq__", args => equal(args[1])));
        PyOps.FinalizeOrdinaryClass(cls);
        return new PyInstance(cls);
    }

    [Fact]
    public void CacheKeepsIdentityAndSeparateInvocationsDoNotShare()
    {
        var cache = Fresh();
        Assert.Same(cache, PyOps.FunctionLocalsEnsure(cache));
        Assert.NotSame(cache, Fresh());
        Assert.Empty(cache.Keys);
        Assert.IsType<PyDict>(PyOps.FunctionLocalsEnsure(PyNone.Instance));
    }

    [Fact]
    public void ValuesPreserveObjectIdentityAndNoneIsBound()
    {
        var cache = Fresh(); var value = new PyList();
        PyOps.FunctionLocalsSync(cache, "value", value);
        PyOps.FunctionLocalsSync(cache, "none", PyNone.Instance);
        Assert.Same(value, cache.Get("value")); Assert.Same(PyNone.Instance, cache.Get("none"));
        value.Append(7L); Assert.Equal(7L, ((PyList)cache.Get("value")!).Items[0]);
    }

    [Fact]
    public void UnboundAndNullRemoveOnlySourceNames()
    {
        var cache = Fresh(); cache.Set("future", 9L); cache.Set("deleted", 8L); cache.Set("extra", 7L);
        PyOps.FunctionLocalsSync(cache, "future", null);
        PyOps.FunctionLocalsSync(cache, "deleted", PyUnbound.Instance);
        PyOps.FunctionLocalsSync(cache, "absent", null);
        Assert.Equal(new object?[] { "extra" }, cache.Keys.ToArray());
        Assert.Equal(7L, cache.Get("extra"));
    }

    [Fact]
    public void UpdateKeepsPositionAndDeletedBindingReinsertsLast()
    {
        var cache = Fresh(); cache.Set("first", 1L); cache.Set("second", 2L); cache.Set("extra", 3L);
        PyOps.FunctionLocalsSync(cache, "first", 4L);
        Assert.Equal(new object?[] { "first", "second", "extra" }, cache.Keys.ToArray());
        cache.Delete("first"); PyOps.FunctionLocalsSync(cache, "first", 5L);
        Assert.Equal(new object?[] { "second", "extra", "first" }, cache.Keys.ToArray());
    }

    [Fact]
    public void ExternalValueRebindingDoesNotReplaceRawLocalAndIsOverwritten()
    {
        var cache = Fresh(); var local = new PyList(); var injected = new PyList();
        PyOps.FunctionLocalsSync(cache, "value", local); cache.Set("value", injected);
        PyOps.FunctionLocalsSync(cache, "value", local);
        Assert.Same(local, cache.Get("value")); Assert.NotSame(injected, cache.Get("value"));
    }

    [Fact]
    public void EquivalentUserKeyKeepsKeyIdentityAndRunsEqualityOnce()
    {
        var cache = Fresh(); int calls = 0;
        var key = Collision("value", other => { calls++; Assert.Equal("value", other); return true; });
        cache.Set(key, 1L); calls = 0;
        PyOps.FunctionLocalsSync(cache, "value", 2L);
        Assert.Equal(1, calls); Assert.Single(cache.Keys); Assert.Same(key, cache.Keys.Single());
        Assert.Equal(2L, cache.Get(key));
    }

    [Fact]
    public void NonEquivalentCollisionRetainsExtraKey()
    {
        var cache = Fresh(); int calls = 0;
        var key = Collision("value", _ => { calls++; return false; }); cache.Set(key, 1L); calls = 0;
        PyOps.FunctionLocalsSync(cache, "value", 2L);
        Assert.Equal(1, calls); Assert.Equal(2, cache.Count); Assert.Same(key, cache.Keys.First());
        Assert.Equal(1L, cache.Get(key));
    }

    [Fact]
    public void DeleteEquivalentUserKeyOnlyComparesOnce()
    {
        var cache = Fresh(); int calls = 0;
        var key = Collision("gone", _ => { calls++; return true; }); cache.Set(key, 1L); calls = 0;
        PyOps.FunctionLocalsSync(cache, "gone", PyUnbound.Instance);
        Assert.Equal(1, calls); Assert.Empty(cache.Keys);
    }

    [Fact]
    public void DeleteSuppressesUserKeyErrorButOrdinaryDeleteDoesNot()
    {
        var cache = Fresh(); int calls = 0; var failure = PyKeyError.ForKey("from equality");
        var key = Collision("gone", _ => { calls++; throw failure; }); cache.Set(key, 1L); calls = 0;
        PyOps.FunctionLocalsSync(cache, "gone", null);
        PyOps.FunctionLocalsSync(cache, "after", 2L);
        Assert.Equal(1, calls); Assert.Equal(2, cache.Count); Assert.Equal(2L, cache.Get("after"));
        Assert.Same(failure, Assert.Throws<PyKeyError>(() => cache.Delete("gone")));
    }

    [Fact]
    public void DeleteOtherErrorLeavesPreviouslySyncedValuesAndPropagatesIdentity()
    {
        var cache = Fresh(); var failure = new PyValueError("from equality"); int calls = 0;
        cache.Set("before", 1L);
        var key = Collision("gone", _ => { calls++; throw failure; }); cache.Set(key, 7L); calls = 0;
        PyOps.FunctionLocalsSync(cache, "before", 2L);
        Assert.Same(failure, Assert.Throws<PyValueError>(() => PyOps.FunctionLocalsSync(cache, "gone", null)));
        Assert.Equal(1, calls); Assert.Equal(2L, cache.Get("before")); Assert.Same(key, cache.Keys.Last());
    }

    [Fact]
    public void UpdatingSlotDoesNotSuppressKeyError()
    {
        var cache = Fresh(); var failure = PyKeyError.ForKey("set equality");
        var key = Collision("value", _ => throw failure); cache.Set(key, 1L);
        Assert.Same(failure, Assert.Throws<PyKeyError>(() => PyOps.FunctionLocalsSync(cache, "value", 2L)));
        Assert.Equal(1L, cache.Get(key));
    }

    [Fact]
    public void EqualitySeesPartialRefreshAndMayRewriteEarlierSlot()
    {
        var cache = Fresh(); cache.Set("before", 1L); cache.Set("after", 1L);
        var key = Collision("middle", _ =>
        {
            Assert.Equal(2L, cache.Get("before")); Assert.Equal(1L, cache.Get("after"));
            cache.Set("before", 9L); return true;
        });
        cache.Set(key, 1L); PyOps.FunctionLocalsSync(cache, "before", 2L);
        PyOps.FunctionLocalsSync(cache, "middle", 2L); PyOps.FunctionLocalsSync(cache, "after", 2L);
        Assert.Equal(9L, cache.Get("before")); Assert.Equal(2L, cache.Get(key)); Assert.Equal(2L, cache.Get("after"));
    }

    [Fact]
    public void PositionalArityIsStrict()
    {
        PyOps.FunctionLocalsValidate([], [], []);
        for (int n = 1; n <= 3; n++)
            Assert.Equal($"locals() takes no arguments ({n} given)",
                Assert.Throws<PyTypeError>(() => PyOps.FunctionLocalsValidate(new object?[n], [], [])).PyMessage);
    }

    [Fact]
    public void KeywordErrorWinsOverArity()
    {
        foreach (object?[] args in new object?[][] { [], [1L], [1L, 2L] })
            Assert.Equal("locals() takes no keyword arguments",
                Assert.Throws<PyTypeError>(() => PyOps.FunctionLocalsValidate(args, ["x"], [1L])).PyMessage);
    }

    [Fact]
    public void EmptyStarAndMappingExpandBeforeValidation()
    {
        PyOps.FunctionLocalsValidate([PyOps.StarArg(new PyTuple([]))], [null], [new PyDict()]);
        Assert.Equal("locals() takes no arguments (2 given)", Assert.Throws<PyTypeError>(() =>
            PyOps.FunctionLocalsValidate([PyOps.StarArg(new PyTuple([1L, 2L]))], [], [])).PyMessage);
        var keywords = new PyDict(); keywords.Set("x", 1L);
        Assert.Equal("locals() takes no keyword arguments", Assert.Throws<PyTypeError>(() =>
            PyOps.FunctionLocalsValidate([], [null], [keywords])).PyMessage);
    }

    [Fact]
    public void IndirectRoutesRejectValidCallAndKeepBindingErrors()
    {
        object saved = PyOps.BuiltinFunction("locals"); Assert.Same(saved, PyOps.BuiltinFunction("locals"));
        Assert.Equal("locals", PyObject.GetAttr(saved, "__name__"));
        Assert.Throws<PyNotImplementedError>(() => PyOps.CallValue(saved, [], [], []));
        Assert.Throws<PyNotImplementedError>(() => PyOps.CallBuiltin("locals", [], [], []));
        Assert.Equal("locals() takes no arguments (1 given)", Assert.Throws<PyTypeError>(() =>
            PyOps.CallValue(saved, [1L], [], [])).PyMessage);
    }

    [Fact]
    public void BindingErrorPreservesAmbientExceptionAndCallerFrame()
    {
        var ambient = new PyRuntimeError("ambient"); object? token = PyErrors.EnterHandler(ambient);
        long depth = PyTraceback.PushFrame("locals.py", "caller"); PyTraceback.SetLine(23);
        try
        {
            var failure = Assert.Throws<PyTypeError>(() => PyOps.FunctionLocalsValidate([1L], [], []));
            Assert.Same(ambient, failure.Context); Assert.Single(failure.Frames);
            Assert.Equal(new PyStackFrame("locals.py", "caller", 23), failure.Frames[0]);
        }
        finally { PyTraceback.PopTo(depth); PyErrors.LeaveHandler(token); }
    }

    [Fact]
    public void DuplicateKeywordsWinBeforeNonStringAndBuiltinBinding()
    {
        var mapping = new PyDict(); mapping.Set("x", 2L); mapping.Set(1L, 3L);
        Assert.Equal("locals() got multiple values for keyword argument 'x'", Assert.Throws<PyTypeError>(() =>
            PyOps.FunctionLocalsValidate([7L], ["x", null], [1L, mapping])).PyMessage);
        var numeric = new PyDict(); numeric.Set(1L, 2L);
        Assert.Equal("locals() got multiple values for keyword argument '1'", Assert.Throws<PyTypeError>(() =>
            PyOps.FunctionLocalsValidate([], [null, null], [numeric, numeric])).PyMessage);
        Assert.Equal("keywords must be strings", Assert.Throws<PyTypeError>(() =>
            PyOps.FunctionLocalsValidate([], [null], [numeric])).PyMessage);
    }

    [Fact]
    public void InvalidExactStarSourcesUseCallSpecificErrors()
    {
        Assert.Equal("locals() argument after * must be an iterable, not int", Assert.Throws<PyTypeError>(() =>
            PyOps.FunctionLocalsValidate([PyOps.StarArg(1L)], [], [])).PyMessage);
        Assert.Equal("locals() argument after ** must be a mapping, not int", Assert.Throws<PyTypeError>(() =>
            PyOps.FunctionLocalsValidate([], [null], [1L])).PyMessage);
    }

    [Fact]
    public void CallbackClearLeavesValidDictionaryAndRejectsOuterSlotWrite()
    {
        var cache = Fresh(); bool active = false;
        var key = Collision("value", _ => { if (active) cache.Clear(); return true; });
        cache.Set(key, 1L); active = true;
        Assert.Throws<PyNotImplementedError>(() => PyOps.FunctionLocalsSync(cache, "value", 2L));
        Assert.Empty(cache.Keys); Assert.Equal(0, cache.Count);
        active = false; cache.Set("next", 3L); Assert.Equal(3L, cache.Get("next")); cache.Delete("next");
        Assert.Equal(0, cache.Count);
    }

    [Fact]
    public void CallbackDeleteSelfDoesNotDoubleDeleteOrCorruptCount()
    {
        var cache = Fresh(); PyInstance? key = null; bool active = false;
        key = Collision("value", _ => { if (active) cache.Delete(key); return true; });
        cache.Set(key, 1L); active = true;
        Assert.Throws<PyNotImplementedError>(() => PyOps.FunctionLocalsSync(cache, "value", null));
        Assert.Equal(0, cache.Count); Assert.Empty(cache.Keys);
        active = false; cache.Set("next", 3L); Assert.Equal(3L, cache.Get("next"));
    }

    [Fact]
    public void CallbackResizePreservesCompletedEffectsAndDictionaryUsability()
    {
        var cache = Fresh(); bool active = false;
        var key = Collision("value", _ =>
        {
            if (active) for (int i = 0; i < 1000; i++) cache.Set("added-" + i, (long)i);
            return true;
        });
        cache.Set(key, 1L); active = true;
        Assert.Throws<PyNotImplementedError>(() => PyOps.FunctionLocalsSync(cache, "value", 2L));
        active = false; Assert.Equal(1001, cache.Count); Assert.Equal(1L, cache.Get(key));
        Assert.Equal(999L, cache.Get("added-999")); cache.Delete(key);
        cache.Set("next", 3L); Assert.Equal(1001, cache.Count); Assert.Equal(3L, cache.Get("next"));
    }

    [Fact]
    public void UserKeyPreventsUnsafeTombstoneCompactionAndRehash()
    {
        var cache = Fresh(); int hashes = 0;
        var cls = (PyClass)PyOps.MakeClass("CompactKey", [], null);
        cls.SetAttr("__hash__", Fn("__hash__", _ => { hashes++; return 19L; }));
        PyOps.FinalizeOrdinaryClass(cls); var key = new PyInstance(cls); cache.Set(key, 5L);
        for (int i = 0; i < 80; i++) cache.Set("temporary-" + i, (long)i);
        for (int i = 0; i < 80; i++) cache.Delete("temporary-" + i);
        int before = hashes; PyOps.FunctionLocalsSync(cache, "next", 7L);
        Assert.Equal(before, hashes); Assert.Equal(2, cache.Count);
        Assert.Same(key, cache.Keys.First()); Assert.Equal(7L, cache.Get("next"));
    }

    [Fact]
    public void StringOnlyLocalsStillCompactWithoutLosingBindings()
    {
        var cache = Fresh(); cache.Set("keep", 1L);
        for (int i = 0; i < 80; i++) cache.Set("temporary-" + i, (long)i);
        for (int i = 0; i < 80; i++) cache.Delete("temporary-" + i);
        PyOps.FunctionLocalsSync(cache, "next", 7L);
        Assert.Equal(new object?[] { "keep", "next" }, cache.Keys.ToArray());
        Assert.Equal(1L, cache.Get("keep")); Assert.Equal(7L, cache.Get("next"));
    }

    [Fact]
    public void WarmRefreshMeasuresAllocationWithReusedValuesAndMissingSlots()
    {
        var cache = Fresh(); object value = 7L; object?[] empty = [];
        void Refresh()
        {
            PyOps.FunctionLocalsValidate(empty, empty, empty);
            PyOps.FunctionLocalsEnsure(cache);
            PyOps.FunctionLocalsSync(cache, "bound", value);
            PyOps.FunctionLocalsSync(cache, "unbound", null);
        }
        for (int i = 0; i < 1000; i++) Refresh();
        long before = GC.GetAllocatedBytesForCurrentThread();
        for (int i = 0; i < 1000; i++) Refresh();
        long allocated = GC.GetAllocatedBytesForCurrentThread() - before;
        Assert.Same(value, cache.Get("bound")); Assert.False(cache.Contains("unbound"));
        string root = System.IO.Path.Combine(System.IO.Path.GetTempPath(),
            $"ryupy-function-locals-allocation-{Environment.ProcessId}-{DateTime.UtcNow.Ticks}-{Guid.NewGuid():N}");
        System.IO.Directory.CreateDirectory(root);
        string path = System.IO.Path.Combine(root, "results.json");
        System.IO.File.WriteAllText(path, System.Text.Json.JsonSerializer.Serialize(new
        {
            warmup = 1000, measured = 1000, allocated_bytes = allocated,
            route = "Validate(empty)+Ensure(existing)+Sync(bound preboxed)+Sync(missing null)",
            scope = "运行时共享协议；不含编译函数调用/实参数组/数值装箱/首次字典分配，无阈值或吞吐结论"
        }, new System.Text.Json.JsonSerializerOptions { WriteIndented = true }));
        output.WriteLine($"locals warm refresh: {allocated} bytes per 1000; {path}");
    }
}
