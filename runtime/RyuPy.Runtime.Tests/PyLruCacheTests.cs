// LRU 回归覆盖强引用、缓存哈希、关键字顺序、类型键、重入及方法绑定。
using System;
using System.Runtime.CompilerServices;
using Xunit;

namespace RyuPy.Runtime.Tests;

public class PyLruCacheTests
{
    private static object Cache(PyFunction function, object? maximum = null, bool typed = false)
    {
        object? decorator = PyFunctoolsTests.Invoke(PyFunctoolsTests.Module["lru_cache"], [maximum ?? PyNone.Instance], ["typed"], [typed]);
        return PyFunctoolsTests.Invoke(decorator, [function])!;
    }
    private static object? Call(object cache, params object?[] args) => PyFunctoolsTests.Invoke(cache, args);
    private static object? Method(object cache, string name) => PyFunctoolsTests.Invoke(PyObject.GetAttr(cache, name), []);
    private static long Stat(object cache, string name) => (long)PyObject.GetAttr(Method(cache, "cache_info"), name)!;

    [Fact]
    public void BoundedCacheEvictsLeastRecentAndKeepsResultIdentity()
    {
        int calls = 0;
        object cache = Cache(PyFunctoolsTests.Function("f", (_, args, _, _) => { calls++; return new PyList(args); }), 2L);
        object? first = Call(cache, 1L); Assert.Same(first, Call(cache, 1L));
        Call(cache, 2L); Call(cache, 1L); Call(cache, 3L); Call(cache, 2L);
        Assert.Equal(4, calls); Assert.Equal(2L, Stat(cache, "hits")); Assert.Equal(4L, Stat(cache, "misses"));
        Assert.Equal(2L, Stat(cache, "currsize"));
        Method(cache, "cache_clear"); Assert.Equal(0L, Stat(cache, "hits")); Assert.Equal(0L, Stat(cache, "currsize"));
        var options = Assert.IsType<PyDict>(Method(cache, "cache_parameters")); options.Set("maxsize", 99L);
        Assert.Equal(2L, Assert.IsType<PyDict>(Method(cache, "cache_parameters")).Get("maxsize"));
    }

    [Fact]
    public void KeysKeepKeywordOrderFastTypesAndTypedDirectArguments()
    {
        int calls = 0;
        var function = PyFunctoolsTests.Function("f", (_, _, _, _) => ++calls);
        object cache = Cache(function);
        PyFunctoolsTests.Invoke(cache, [], ["a", "b"], [1L, 2L]);
        PyFunctoolsTests.Invoke(cache, [], ["b", "a"], [2L, 1L]);
        Assert.Equal(2, calls);
        Call(cache, 1L); Call(cache, 1.0); Call(cache, true);
        Assert.Equal(4, calls); // float/bool 属于通用元组键，int 是独立快键。
        object typed = Cache(function, 10L, true);
        Call(typed, 1L); Call(typed, 1.0); Call(typed, true);
        Assert.Equal(3L, Stat(typed, "currsize"));
        object nested = Cache(function, 10L, true);
        Call(nested, new PyTuple([1L])); Call(nested, new PyTuple([1.0]));
        Assert.Equal(1L, Stat(nested, "currsize"));
    }

    [Fact]
    public void HashIsOncePerCallAndDisabledCacheDoesNotHash()
    {
        int hashes = 0;
        var cls = (PyClass)PyOps.MakeClass("Key", [], null);
        cls.SetAttr("__hash__", PyFunctoolsTests.Function("hash", (_, _, _, _) => { hashes++; return 7L; }));
        object key = cls.Invoke([], [], [])!;
        object cache = Cache(PyFunctoolsTests.Function("f", (_, _, _, _) => 1L), 1L);
        Call(cache, key); Assert.Equal(1, hashes);
        Call(cache, key); Assert.Equal(2, hashes);
        Call(cache, 2L); Assert.Equal(2, hashes); // 淘汰不能重新计算旧键的hash。
        Assert.Throws<PyTypeError>(() => Call(cache, new PyList())); Assert.Equal(2L, Stat(cache, "misses"));
        object disabled = Cache(PyFunctoolsTests.Function("f", (_, _, _, _) => 1L), 0L);
        Call(disabled, new PyList()); Assert.Equal(1L, Stat(disabled, "misses")); Assert.Equal(0L, Stat(disabled, "currsize"));
    }

    [Fact]
    public void ExceptionsAreNotCachedAndRecursiveInsertionIsNotOverwritten()
    {
        int errors = 0;
        object failing = Cache(PyFunctoolsTests.Function("bad", (_, _, _, _) => { errors++; throw new PyValueError("bad"); }));
        Assert.Throws<PyValueError>(() => Call(failing, 1L)); Assert.Throws<PyValueError>(() => Call(failing, 1L));
        Assert.Equal(2, errors); Assert.Equal(0L, Stat(failing, "currsize"));
        object? recursive = null; bool entered = false;
        var function = PyFunctoolsTests.Function("recursive", (_, args, _, _) =>
        {
            if (entered) return "inner";
            entered = true; Call(recursive!, args); return "outer";
        });
        recursive = Cache(function, 2L);
        Assert.Equal("outer", Call(recursive, 1L)); Assert.Equal("inner", Call(recursive, 1L));
        Assert.Equal(1L, Stat(recursive, "currsize"));
    }

    [Fact]
    public void EqualityCanClearCacheWithoutReturningRemovedEntry()
    {
        object? cache = null; bool clear = false;
        var cls = (PyClass)PyOps.MakeClass("EqualKey", [], null);
        cls.SetAttr("__hash__", PyFunctoolsTests.Function("hash", (_, _, _, _) => 1L));
        cls.SetAttr("__eq__", PyFunctoolsTests.Function("eq", (_, _, _, _) =>
        { if (clear) { clear = false; Method(cache!, "cache_clear"); } return true; }));
        int calls = 0;
        cache = Cache(PyFunctoolsTests.Function("f", (_, _, _, _) => ++calls));
        Call(cache, cls.Invoke([], [], [])); clear = true;
        Assert.Equal(2, Call(cache, cls.Invoke([], [], [])));
        Assert.Equal(1L, Stat(cache, "currsize"));
    }

    [Fact]
    public void WrapperBindsMethodsAndCacheInfoHasTupleProtocol()
    {
        var function = PyFunctoolsTests.Function("method", (_, args, _, _) => args[1]);
        object cache = Cache(function);
        var cls = (PyClass)PyOps.MakeClass("Holder", [], null); cls.SetAttr("method", cache);
        object instance = cls.Invoke([], [], [])!;
        Assert.Equal(4L, PyFunctoolsTests.Invoke(PyObject.GetAttr(instance, "method"), [4L]));
        Assert.Same(function, PyObject.GetAttr(cache, "__wrapped__"));
        object info = Method(cache, "cache_info")!;
        Assert.Equal(true, PyOps.CallBuiltin("isinstance", [info, PyType.Get("tuple")], [], []));
        var iterator = PyIter.Create(info); int count = 0; while (iterator.MoveNext()) count++;
        Assert.Equal(4, count);
        Assert.Equal("CacheInfo(hits=0, misses=1, maxsize=None, currsize=1)", PyRepr.Repr(info));
    }

    [Fact]
    public void CacheClearReleasesStrongReferences()
    {
        object cache = Cache(PyFunctoolsTests.Function("f", (_, _, _, _) => null));
        WeakReference reference = Populate(cache);
        GC.Collect(); GC.WaitForPendingFinalizers(); GC.Collect(); Assert.True(reference.IsAlive);
        Method(cache, "cache_clear");
        GC.Collect(); GC.WaitForPendingFinalizers(); GC.Collect(); Assert.False(reference.IsAlive);
        GC.KeepAlive(cache);
    }
    [Fact]
    public void ConfigurationPreservesParametersAndDefersTypedConversionToDecoration()
    {
        var function = PyFunctoolsTests.Function("f", (_, _, _, _) => 1L);
        object boolCache = Cache(function, true);
        Assert.Equal(true, Assert.IsType<PyDict>(Method(boolCache, "cache_parameters")).Get("maxsize"));
        Assert.Equal(1L, PyObject.GetAttr(Method(boolCache, "cache_info"), "maxsize"));
        object negative = Cache(function, -5L);
        Assert.Equal(0L, Assert.IsType<PyDict>(Method(negative, "cache_parameters")).Get("maxsize"));
        object? hugeDecorator = PyFunctoolsTests.Invoke(PyFunctoolsTests.Module["lru_cache"], [System.Numerics.BigInteger.One << 100]);
        Assert.Throws<PyOverflowError>(() => PyFunctoolsTests.Invoke(hugeDecorator, [function]));
        int boolCalls = 0;
        var cls = (PyClass)PyOps.MakeClass("Flag", [], null);
        cls.SetAttr("__bool__", PyFunctoolsTests.Function("bool", (_, _, _, _) => { boolCalls++; return true; }));
        object flag = cls.Invoke([], [], [])!;
        object? decorator = PyFunctoolsTests.Invoke(PyFunctoolsTests.Module["lru_cache"], [2L], ["typed"], [flag]);
        Assert.Equal(0, boolCalls);
        object cache = PyFunctoolsTests.Invoke(decorator, [function])!;
        Assert.Equal(1, boolCalls);
        Assert.Same(flag, Assert.IsType<PyDict>(Method(cache, "cache_parameters")).Get("typed"));
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static WeakReference Populate(object cache)
    { var key = new object(); Call(cache, key); return new WeakReference(key); }
}
