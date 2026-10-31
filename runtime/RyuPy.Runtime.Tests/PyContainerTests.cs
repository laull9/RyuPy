using System;
using Xunit;

namespace RyuPy.Runtime.Tests;

/// <summary>容器构造、比较与可变写入的差分测试：期望值以 CPython 3.12 的实际行为为准。</summary>
public class PyContainerTests
{
    private static PyList L(params object?[] items) => new(items);

    private static PyTuple T(params object?[] items) => new(items);

    [Fact]
    public void SequencesCompareLexicographically()
    {
        Assert.True(PyOps.Compare(PyOps.Lt, T(1L, 2L), T(1L, 3L)));
        Assert.True(PyOps.Compare(PyOps.Le, T(1L, 2L), T(1L, 2L)));
        Assert.True(PyOps.Compare(PyOps.Lt, T(1L), T(1L, 2L)));
        Assert.True(PyOps.Compare(PyOps.Gt, L(2L), L(1L)));
        Assert.True(PyOps.Compare(PyOps.Lt, new PyBytes([1, 2]), new PyBytes([1, 3])));
    }

    [Fact]
    public void ElementTypeErrorNamesElementsNotContainers()
    {
        // CPython：(1, 2) < (1, "a") 报的是 int 与 str，而不是 tuple 与 tuple
        var error = Assert.Throws<PyTypeError>(() =>
            PyOps.Compare(PyOps.Lt, T(1L, 2L), T(1L, "a"))
        );
        Assert.Contains(
            "'<' not supported between instances of 'int' and 'str'",
            error.PyMessage
        );
    }

    [Fact]
    public void CrossSequenceOrderingIsRejected()
    {
        var error = Assert.Throws<PyTypeError>(() =>
            PyOps.Compare(PyOps.Lt, L(1L), T(1L))
        );
        Assert.Contains(
            "'<' not supported between instances of 'list' and 'tuple'",
            error.PyMessage
        );
    }

    [Fact]
    public void BytesAndByteArrayAreInterchangeableForEquality()
    {
        Assert.True(PyHash.Equal(new PyBytes([1, 2]), new PyByteArray([1, 2])));
        Assert.True(PyHash.Equal(new PyByteArray([1, 2]), new PyBytes([1, 2])));
        Assert.False(PyHash.Equal(new PyBytes([1, 2]), new PyBytes([1, 2, 3])));
        Assert.False(PyHash.Equal(new PyBytes([1, 2]), new PyTuple([1L, 2L])));
    }

    [Fact]
    public void ByteArrayDeletionSupportsIndexAndSlice()
    {
        var buffer = new PyByteArray([65, 66, 67, 68]);
        buffer.DelItem(0L);
        Assert.Equal(3, buffer.Count);
        Assert.Equal((long)'B', buffer.GetItem(0));

        // Python：del b[0] 后剩 BCD，再 del b[1:3] 删掉 CD，只剩 B
        buffer.DelItem(new PySlice(1L, 3L, null));
        Assert.Equal([66], [.. buffer.Bytes]);

        // 负步长也要从前往后删，否则下标会在移除后失效
        var reversed = new PyByteArray([1, 2, 3, 4]);
        reversed.DelItem(new PySlice(null, null, -2L));
        Assert.Equal([1, 3], [.. reversed.Bytes]);
    }

    [Fact]
    public void ByteArrayDeletionRejectsOutOfRangeIndex()
    {
        var buffer = new PyByteArray([65]);
        Assert.Throws<PyIndexError>(() => buffer.DelItem(9L));
        Assert.Throws<PyTypeError>(() => buffer.DelItem("x"));
    }

    [Fact]
    public void ListAliasingSharesMutation()
    {
        var list = L(1L, 2L);
        var alias = list;
        list.Append(3L);
        Assert.True(PyHash.Equal(alias, L(1L, 2L, 3L)));
        Assert.True(PyOps.Compare(PyOps.Eq, alias, list));
    }
}
