// 临时enumerate分量分离的协议与ABI：底层Current恰好一次，索引仍为原有新盒。
using RyuPy.Runtime;
namespace RyuPy.Runtime.Tests;
public class PyComprehensionOpsTests
{
    private sealed class Probe : IPyIterator
    {
        internal int Reads;
        internal readonly object Value = new();
        public bool MoveNext() => true;
        public object? Current() { Reads++; return Value; }
    }
    [Fact]
    public void ExactComponentsPreserveOrderAndIdentity()
    {
        var inner = new Probe(); var iterator = new PyEnumerateIter(inner, 1000);
        Assert.True(PyOps.IsEnumerateIterator(iterator));
        Assert.False(PyOps.IsEnumerateIterator(new PyList()));
        Assert.True(iterator.MoveNext());
        Assert.Equal(1000L, PyOps.EnumerateCurrentIndex(iterator));
        Assert.Equal(0, inner.Reads);
        Assert.Same(inner.Value, PyOps.EnumerateCurrentValue(iterator));
        Assert.Equal(1, inner.Reads);
        Assert.True(iterator.MoveNext());
        Assert.Equal(1001L, PyOps.EnumerateCurrentIndex(iterator));
    }
    [Fact]
    public void CompilerAbiUsesExactBooleanAndObjectResults()
    {
        Assert.Equal(typeof(bool), typeof(PyOps).GetMethod("IsEnumerateIterator")!.ReturnType);
        Assert.Equal(typeof(object), typeof(PyOps).GetMethod("EnumerateCurrentIndex")!.ReturnType);
        Assert.Equal(typeof(object), typeof(PyOps).GetMethod("EnumerateCurrentValue")!.ReturnType);
    }

    [Fact]
    public void PrivateRangeCapacityIsBoundedAndDoesNotConsume()
    {
        var range = new PyRangeIter(new PyRange(10, -1, -2));
        var empty = (PyList)PyOps.MakeComprehensionList(range);
        Assert.Equal(0, empty.Count); Assert.Equal(6, empty.Items.Capacity);
        Assert.True(range.MoveNext()); Assert.Equal(10L, range.Current());
        Assert.Equal(5, ((PyList)PyOps.MakeComprehensionList(range)).Items.Capacity);
        var wide = new PyRangeIter(new PyRange(0, long.MaxValue, 1));
        Assert.Equal(65536, ((PyList)PyOps.MakeComprehensionList(wide)).Items.Capacity);
        var negative = new PyRangeIter(new PyRange(long.MaxValue, long.MinValue, long.MinValue));
        Assert.Equal(2, ((PyList)PyOps.MakeComprehensionList(negative)).Items.Capacity);
        var probe = new Probe();
        Assert.Equal(0, ((PyList)PyOps.MakeComprehensionList(probe)).Count);
        Assert.Equal(0, probe.Reads);
    }
}
