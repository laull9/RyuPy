using RyuPy.Runtime;

namespace RyuPy.Runtime.Tests;

/// <summary>精确字符串延迟物化不改变空串、快照、动态回退与失败后的身份。</summary>
public class PyStringAccumulatorTests
{
    [Fact]
    public void PlainStringSubclassKeepsIdentityUntilDynamicAddition()
    {
        var cls = (PyClass)PyOps.MakeClass("Text", [PyType.Get("str")], null);
        PyOps.FinalizeOrdinaryClass(cls);
        object text = cls.Invoke(["seed"], [], [])!;
        object state = PyNumericAccumulator.Create(text);
        Assert.Same(text, PyNumericAccumulator.Value(state));
        PyNumericAccumulator.Add(state, "suffix");
        Assert.Equal("seedsuffix", Assert.IsType<string>(PyNumericAccumulator.Value(state)));
        object buffered = PyNumericAccumulator.Create("a");
        PyNumericAccumulator.Add(buffered, "b");
        PyNumericAccumulator.Add(buffered, text);
        Assert.Equal("abseed", Assert.IsType<string>(PyNumericAccumulator.Value(buffered)));
    }

    [Fact]
    public void EmptyAndSharedStringsKeepIdentity()
    {
        string seed = new string('x', 3);
        object state = PyNumericAccumulator.Create(seed);
        Assert.Same(seed, PyNumericAccumulator.Value(state));
        PyNumericAccumulator.Add(state, "");
        Assert.Same(seed, PyNumericAccumulator.Value(state));
        object empty = PyNumericAccumulator.Create("");
        PyNumericAccumulator.Add(empty, seed);
        Assert.Same(seed, PyNumericAccumulator.Value(empty));
    }

    [Fact]
    public void SnapshotsAreCachedAndNeverMutated()
    {
        object state = PyNumericAccumulator.Create("prefix");
        PyNumericAccumulator.Add(state, "first");
        object? first = PyNumericAccumulator.Value(state);
        Assert.Same(first, PyNumericAccumulator.Value(state));
        PyNumericAccumulator.Add(state, "");
        Assert.Same(first, PyNumericAccumulator.Value(state));
        for (int i = 0; i < 1000; i++) PyNumericAccumulator.Add(state, "尾\ud800");
        Assert.Equal("prefixfirst", first);
        Assert.Equal("prefixfirst" + string.Concat(Enumerable.Repeat("尾\ud800", 1000)),
            PyNumericAccumulator.Value(state));
        Assert.NotSame(first, PyNumericAccumulator.Value(state));
    }

    [Fact]
    public void FailureKeepsMaterializedSnapshotAndLaterAppendWorks()
    {
        object state = PyNumericAccumulator.Create("a");
        PyNumericAccumulator.Add(state, "b");
        object? before = PyNumericAccumulator.Value(state);
        Assert.Throws<PyTypeError>(() => PyNumericAccumulator.Add(state, 1L));
        Assert.Same(before, PyNumericAccumulator.Value(state));
        PyNumericAccumulator.Add(state, "c");
        Assert.Equal("abc", PyNumericAccumulator.Value(state));
        Assert.Equal("ab", before);
    }

    [Fact]
    public void ReflectedAndInPlaceResultsRemainDynamicObjects()
    {
        var cls = new PyClass("Custom");
        var custom = new PyInstance(cls);
        object shared = new string('z', 2);
        cls.SetAttr("__radd__", PyRuntimeDataclassTests.Fn("__radd__", ["self", "other"], args =>
        {
            Assert.Equal("ab", args[1]);
            return custom;
        }));
        cls.SetAttr("__iadd__", PyRuntimeDataclassTests.Fn("__iadd__", ["self", "other"], _ => shared));
        object state = PyNumericAccumulator.Create("a");
        PyNumericAccumulator.Add(state, "b");
        PyNumericAccumulator.Add(state, custom);
        Assert.Same(custom, PyNumericAccumulator.Value(state));
        PyNumericAccumulator.Add(state, 1L);
        Assert.Same(shared, PyNumericAccumulator.Value(state));
        PyNumericAccumulator.Add(state, "!");
        Assert.Equal("zz!", PyNumericAccumulator.Value(state));
    }
}
