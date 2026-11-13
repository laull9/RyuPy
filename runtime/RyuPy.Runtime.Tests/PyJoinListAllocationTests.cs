// 精确list的join只消除临时引用快照；保BCL身份、物化与其它迭代协议，不测wall。
using RyuPy.Runtime;

namespace RyuPy.Runtime.Tests;

[Collection("生成器帧栈")]
public sealed class PyJoinListAllocationTests
{
    private static string Join(object value, string separator = "") =>
        Assert.IsType<string>(PyOps.CallMethod(separator, "join", [value], [], []));

    [Fact]
    public void ExactListKeepsValuesIdentityAndOriginalElements()
    {
        string text = new string('x', 71);
        Assert.Same(text, Join(new PyList([text]), "--"));
        Assert.Same(string.Empty, Join(new PyList()));
        var list = new PyList(["中文", "😀", "\ud800", "é"]);
        object?[] before = [.. list.Items];
        Assert.Equal("中文|😀|\ud800|é", Join(list, "|"));
        for (int i = 0; i < before.Length; i++) Assert.Same(before[i], list.GetItem(i));
        list.Append(1L);
        Assert.Throws<PyTypeError>(() => Join(list));
        Assert.Equal(5, list.Count);
    }

    [Fact]
    public void StringSubclassPayloadNeverCallsConversion()
    {
        var cls = (PyClass)PyOps.MakeClass("JoinText", [PyType.Get("str")], null);
        PyOps.FinalizeOrdinaryClass(cls);
        cls.SetAttr("__str__", PyNativeFunctions.CreateVariadic("__str__", _ => throw new PyValueError("callback")));
        // 既有边界仍拒绝覆盖__len__；不伪称本次扩展子类slot兼容。
        Assert.Throws<PyNotImplementedError>(() => cls.SetAttr("__len__",
            PyNativeFunctions.CreateVariadic("__len__", _ => throw new PyValueError("callback"))));
        string payload = new string('z', 75);
        object text = cls.Invoke([payload], [], [])!;
        Assert.Same(payload, Join(new PyList([text])));
        Assert.Equal(payload + "|a", Join(new PyList([text, "a"]), "|"));
    }

    private sealed class FailingIterator : IPyIterator
    {
        public int Calls;
        public bool MoveNext() { if (++Calls == 1) return true; throw new PyValueError("later"); }
        public object Current() => 1L;
    }

    [Fact]
    public void NonListIteratorStillDrainsBeforeCheckingItemTypes()
    {
        var iterator = new FailingIterator();
        Assert.Throws<PyValueError>(() => Join(iterator));
        Assert.Equal(2, iterator.Calls);
    }

    [Fact]
    public void NumericBackingStillMaterializesOnFailedJoin()
    {
        var list = Assert.IsType<PyList>(PyNumericListOps.Create());
        PyNumericListOps.AppendF64(list, 1.25);
        PyNumericListOps.AppendF64(list, 2.5);
        Assert.Throws<PyTypeError>(() => Join(list));
        object? first = list.GetItem(0), second = list.GetItem(1);
        Assert.Same(first, list.Items[0]); Assert.Same(second, list.Items[1]);
        Assert.Same(first, list.GetItem(0)); Assert.Same(second, list.GetItem(1));
    }

    // 旧核心作为分配对照，不修改产品策略；公开新路径还计入分派额外成本。
    private static string Previous(PyList list)
    {
        object?[] items = PyOps.ToSequence(list);
        var parts = new string[items.Length];
        for (int i = 0; i < items.Length; i++) parts[i] = (string)items[i]!;
        return string.Join("", parts);
    }

    [Fact]
    public void LongExactListRemovesOneReferenceArrayAllocation()
    {
        var list = new PyList(Enumerable.Repeat<object?>("ab", 1024));
        for (int i = 0; i < 20; i++) { Join(list); Previous(list); }
        long start = GC.GetAllocatedBytesForCurrentThread();
        for (int i = 0; i < 100; i++) Previous(list);
        long before = GC.GetAllocatedBytesForCurrentThread() - start;
        start = GC.GetAllocatedBytesForCurrentThread();
        for (int i = 0; i < 100; i++) Join(list);
        long after = GC.GetAllocatedBytesForCurrentThread() - start;
        Assert.True(before - after >= 4000 * 100, $"before={before}, after={after}");
    }
}
