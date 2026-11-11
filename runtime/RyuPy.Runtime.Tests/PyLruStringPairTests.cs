// 两精确string命中探针：真实哈希碰撞、stored-left副作用、重入、锁外并发与旧回退合同。
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace RyuPy.Runtime.Tests;

public class PyLruStringPairTests
{
    private static PyLruCache Cache(PyCall body, object? maximum = null, bool typed = false)
    {
        object? decorator = PyFunctoolsTests.Invoke(PyFunctoolsTests.Module["lru_cache"],
            [maximum ?? PyNone.Instance], ["typed"], [typed]);
        return Assert.IsType<PyLruCache>(PyFunctoolsTests.Invoke(decorator,
            [PyFunctoolsTests.Function("f", body)]));
    }
    private static object? Call(PyLruCache cache, params object?[] args) => cache.Invoke(args, [], []);
    private static void Clear(PyLruCache cache) => PyFunctoolsTests.Invoke(cache.GetAttr("cache_clear"), []);
    private static long Stat(PyLruCache cache, string name) =>
        (long)PyObject.GetAttr(PyFunctoolsTests.Invoke(cache.GetAttr("cache_info"), []), name)!;
    private static object Key(string text, Func<object?, object?> equal, Action? hash = null)
    {
        var cls = (PyClass)PyOps.MakeClass("Collision", [], null);
        cls.SetAttr("__hash__", PyFunctoolsTests.Function("hash", (_, _, _, _) =>
        { hash?.Invoke(); return PyHash.Of(text); }));
        cls.SetAttr("__eq__", PyFunctoolsTests.Function("eq", (_, args, _, _) => equal(args[1])));
        return cls.Invoke([], [], [])!;
    }

    [Fact]
    public void EqualStringsKeepIdentityOrderAndTrueLruEviction()
    {
        int calls = 0;
        var cache = Cache((_, _, _, _) => { calls++; return new object(); }, 2L);
        object? first = Call(cache, "left", "right");
        Assert.Same(first, Call(cache, new string("left".ToCharArray()), new string("right".ToCharArray())));
        object? reverse = Call(cache, "right", "left"); Assert.NotSame(first, reverse);
        Assert.Same(first, Call(cache, "left", "right"));
        Call(cache, "third", "key"); Call(cache, "right", "left");
        Assert.Equal(4, calls); Assert.Equal(2L, Stat(cache, "hits"));
        Clear(cache); Assert.Equal(0L, Stat(cache, "currsize"));
    }

    [Fact]
    public void RealCollisionKeepsStoredLeftOrderTruthinessAndHashOnce()
    {
        var events = new List<string>(); int hashes = 0;
        var truth = (PyClass)PyOps.MakeClass("Truth", [], null);
        truth.SetAttr("__bool__", PyFunctoolsTests.Function("bool", (_, _, _, _) =>
        { events.Add("truth"); return true; }));
        object flag = truth.Invoke([], [], [])!;
        object left = Key("left", other => { Assert.Equal("left", other); events.Add("left"); return flag; }, () => hashes++);
        object right = Key("right", other => { Assert.Equal("right", other); events.Add("right"); return true; }, () => hashes++);
        // 不只声称碰撞：实际整体tuple哈希相等才进入同桶。
        Assert.Equal(PyHash.Of(new PyTuple([left, right])), PyHash.Of(new PyTuple(["left", "right"])));
        hashes = 0;
        var cache = Cache((_, _, _, _) => new object()); object? stored = Call(cache, left, right);
        Assert.Equal(2, hashes);
        Assert.Same(stored, Call(cache, "left", "right"));
        Assert.Equal(new[] { "left", "truth", "right" }, events);
        Assert.Equal(2, hashes); Assert.Equal(1L, Stat(cache, "hits"));
    }

    [Fact]
    public void FalseNotImplementedAndThrowRemainRealCollisionOutcomes()
    {
        foreach (object? outcome in new object?[] { false, PyNotImplemented.Instance })
        {
            int second = 0, calls = 0;
            object left = Key("left", _ => outcome);
            object right = Key("right", _ => { second++; return true; });
            var cache = Cache((_, _, _, _) => ++calls);
            Call(cache, left, right); Assert.Equal(2, Call(cache, "left", "right"));
            Assert.Equal(0, second);
        }
        int bodies = 0;
        var throwing = Cache((_, _, _, _) => ++bodies);
        Call(throwing, Key("left", _ => throw new PyValueError("comparison")), "right");
        Assert.Throws<PyValueError>(() => Call(throwing, "left", "right")); Assert.Equal(1, bodies);
    }

    [Fact]
    public void ClearAndInternalArgsMutationKeepSnapshotAndCompleteComparison()
    {
        // 此数组由C#直接持有Invoke调用实参；只证明内部因果，不冒Python能取得该数组。
        object?[] query = ["left", "right"]; var events = new List<string>();
        PyLruCache cache = null!; bool act = true;
        object left = Key("left", other =>
        {
            events.Add("left:" + other);
            if (act) { act = false; Clear(cache); query[0] = "changed"; query[1] = "changed2"; }
            return true;
        });
        object right = Key("right", other => { events.Add("right:" + other); return true; });
        cache = Cache((_, args, _, _) => new PyTuple((object?[])args.Clone()));
        object? removed = Call(cache, left, right);
        object? result = cache.Invoke(query, [], []);
        Assert.NotSame(removed, result);
        Assert.Equal(new[] { "left:left", "right:right" }, events);
        Assert.Equal("('changed', 'changed2')", PyRepr.Repr(result));
        // miss存储的是回调前快照；被调函数接到的仍是回调后原args。
        Assert.Same(result, Call(cache, "left", "right"));
        Assert.Equal(1L, Stat(cache, "currsize"));
    }

    [Fact]
    public void EqualityRecursiveInsertionRestartsAndDoesNotReturnRemovedEntry()
    {
        PyLruCache cache = null!; bool recurse = true; object? inner = null;
        var events = new List<string>();
        object left = Key("left", _ =>
        {
            events.Add("left");
            if (recurse) { recurse = false; Clear(cache); inner = Call(cache, "left", "right"); }
            return true;
        });
        object right = Key("right", _ => { events.Add("right"); return true; });
        cache = Cache((_, _, _, _) => new object()); object? old = Call(cache, left, right);
        object? result = Call(cache, "left", "right");
        Assert.Same(inner, result); Assert.NotSame(old, result);
        Assert.Equal(new[] { "left", "right" }, events);
    }

    [Fact]
    public void BodyRecursionKeepsInnerEntryAndExceptionDoesNotInsert()
    {
        PyLruCache cache = null!; bool recurse = true; object? inner = null;
        cache = Cache((_, _, _, _) =>
        {
            if (recurse) { recurse = false; inner = Call(cache, "left", "right"); }
            return new object();
        });
        object? outer = Call(cache, "left", "right"); Assert.NotSame(inner, outer);
        Assert.Same(inner, Call(cache, "left", "right"));
        int count = 0; var errors = Cache((_, _, _, _) => { count++; throw new PyValueError("body"); });
        Assert.Throws<PyValueError>(() => Call(errors, "left", "right"));
        Assert.Throws<PyValueError>(() => Call(errors, "left", "right"));
        Assert.Equal(2, count); Assert.Equal(0L, Stat(errors, "currsize"));
    }

    [Fact]
    public void ConcurrentSameKeyComputesOutsideLockWithoutOverwritingWinner()
    {
        using var barrier = new Barrier(2); int calls = 0;
        var cache = Cache((_, _, _, _) =>
        {
            Interlocked.Increment(ref calls);
            Assert.True(barrier.SignalAndWait(TimeSpan.FromSeconds(10)));
            return new object();
        });
        Task<object?> first = Task.Run(() => Call(cache, "left", "right"));
        Task<object?> second = Task.Run(() => Call(cache, "left", "right"));
        Assert.True(Task.WaitAll([first, second], TimeSpan.FromSeconds(20)));
        Assert.Equal(2, calls); Assert.NotSame(first.Result, second.Result);
        object? saved = Call(cache, "left", "right");
        Assert.True(ReferenceEquals(saved, first.Result) || ReferenceEquals(saved, second.Result));
        Assert.Equal(1L, Stat(cache, "currsize")); Assert.Equal(2L, Stat(cache, "misses"));
    }

    [Fact]
    public void ClearWhileBodyIsOutsideLockKeepsOriginalPostClearInsertion()
    {
        using var barrier = new Barrier(2);
        var cache = Cache((_, _, _, _) =>
        {
            Assert.True(barrier.SignalAndWait(TimeSpan.FromSeconds(10)));
            Assert.True(barrier.SignalAndWait(TimeSpan.FromSeconds(10)));
            return new object();
        });
        Task<object?> pending = Task.Run(() => Call(cache, "left", "right"));
        Assert.True(barrier.SignalAndWait(TimeSpan.FromSeconds(10)));
        Clear(cache);
        Assert.True(barrier.SignalAndWait(TimeSpan.FromSeconds(10)));
        Assert.True(pending.Wait(TimeSpan.FromSeconds(20)));
        Assert.Same(pending.Result, Call(cache, "left", "right"));
        Assert.Equal(0L, Stat(cache, "misses")); Assert.Equal(1L, Stat(cache, "hits"));
    }

    [Fact]
    public void TypedKeywordsDisabledAndSingleFastKeysKeepFallback()
    {
        int calls = 0;
        var typed = Cache((_, _, _, _) => ++calls, typed: true);
        Call(typed, "left", "right"); Call(typed, "left", "right"); Assert.Equal(1, calls);
        var cache = Cache((_, _, _, _) => ++calls);
        cache.Invoke(["left", "right"], ["a", "b"], [1L, 2L]);
        cache.Invoke(["left", "right"], ["b", "a"], [2L, 1L]);
        Assert.Equal(2L, Stat(cache, "currsize"));
        Call(cache, "left"); Call(cache, "left"); Assert.Equal(1L, Stat(cache, "hits"));
        var disabled = Cache((_, _, _, _) => new object(), 0L);
        Assert.NotSame(Call(disabled, "left", "right"), Call(disabled, "left", "right"));
        Assert.Equal(0L, Stat(disabled, "currsize"));

        // 合法str子类沿既有布局；不覆盖被禁止的__hash__/__eq__槽。
        var textClass = (PyClass)PyOps.MakeClass("Text", [PyType.Get("str")], null);
        PyOps.FinalizeOrdinaryClass(textClass);
        var left = Assert.IsAssignableFrom<PyInstance>(textClass.Invoke(["left"], [], []));
        var right = Assert.IsAssignableFrom<PyInstance>(textClass.Invoke(["right"], [], []));
        var subclassFirst = Cache((_, _, _, _) => new object());
        object? subclassResult = Call(subclassFirst, left, right);
        Assert.Same(subclassResult, Call(subclassFirst, "left", "right"));
        Assert.Equal(1L, Stat(subclassFirst, "hits"));
        var exactFirst = Cache((_, _, _, _) => new object());
        object? exactResult = Call(exactFirst, "left", "right");
        Assert.Same(exactResult, Call(exactFirst, left, right));
        Assert.Equal(1L, Stat(exactFirst, "hits"));
    }
    [Fact]
    public void PairStorageReleasesBothStringsAfterClear()
    {
        var cache = Cache((_, _, _, _) => null);
        WeakReference[] references = PopulatePair(cache);
        GC.Collect(); GC.WaitForPendingFinalizers(); GC.Collect();
        Assert.All(references, reference => Assert.True(reference.IsAlive));
        Clear(cache);
        GC.Collect(); GC.WaitForPendingFinalizers(); GC.Collect();
        Assert.All(references, reference => Assert.False(reference.IsAlive));
        GC.KeepAlive(cache);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static WeakReference[] PopulatePair(PyLruCache cache)
    {
        string first = new('x', 5), second = new('y', 5);
        Call(cache, first, second);
        return [new WeakReference(first), new WeakReference(second)];
    }

}
