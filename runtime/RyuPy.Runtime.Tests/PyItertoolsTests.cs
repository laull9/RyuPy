using RyuPy.Runtime;
using Xunit;

namespace RyuPy.Runtime.Tests;

/// <summary>惰性迭代工具精确消费边界、引用身份和构造错误。</summary>
public class PyItertoolsTests
{
    private static object New(string name, params object?[] args) => PyOps.CallValue(PyItertools.CreateAttributes()[name], args, [], [])!;

    [Fact]
    public void IsliceFinishesAtExactBoundaryEvenWhenStartExceedsStop()
    {
        IPyIterator source = PyIter.Create(new PyRange(0, 10, 1));
        Assert.Empty(PyOps.ToSequence(New("islice", source, 5L, 2L)));
        Assert.True(source.MoveNext()); Assert.Equal(5L, source.Current());
        source = PyIter.Create(new PyRange(0, 10, 1));
        Assert.Equal(new object?[] { 1L, 4L }, PyOps.ToSequence(New("islice", source, 1L, 6L, 3L)));
        Assert.True(source.MoveNext()); Assert.Equal(6L, source.Current());
        Assert.Throws<PyValueError>(() => New("islice", source, -1L));
        Assert.Throws<PyValueError>(() => New("islice", source, 0L, 3L, 0L));
        Assert.Throws<PyTypeError>(() => New("islice", 1L, 0L));
    }

    [Fact]
    public void RepeatReturnsOriginalObjectAndDistinguishesOmittedFromNone()
    {
        object item = new PyList([]);
        object?[] values = PyOps.ToSequence(New("repeat", item, 3L));
        Assert.All(values, value => Assert.Same(item, value));
        Assert.Empty(PyOps.ToSequence(New("repeat", item, -1L)));
        Assert.Throws<PyTypeError>(() => New("repeat", item, PyNone.Instance));
        Assert.Equal(4, PyOps.ToSequence(New("islice", New("repeat", item), 4L)).Length);
    }

    [Fact]
    public void ChainDoesNotReadLaterInputsUntilNeeded()
    {
        object chain = New("chain", new PyTuple([1L]), 3L);
        IPyIterator iterator = PyIter.Create(chain);
        Assert.True(iterator.MoveNext()); Assert.Equal(1L, iterator.Current());
        Assert.Throws<PyTypeError>(() => iterator.MoveNext());
        Assert.False(iterator.MoveNext());
        object? factory = ((PyNativeType)PyItertools.CreateAttributes()["chain"]!).GetAttr("from_iterable");
        object combined = PyOps.CallValue(factory, [new PyTuple([new PyTuple([1L]), new PyTuple([]), new PyTuple([2L])])], [], [])!;
        Assert.Equal(new object?[] { 1L, 2L }, PyOps.ToSequence(combined));
    }
}
