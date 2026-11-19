// 完整循环helper的无写入守卫、背板、终值和公开ABI精确合同。
using System.Numerics;
using System.Reflection;
using RyuPy.Runtime;

namespace RyuPy.Runtime.Tests;

public class PyGuardedListLoopTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void MarksOnlyProgressionAndReturnsFinalInteger(bool compact)
    {
        var list = compact ? Assert.IsType<PyList>(PyOps.Binary(PyOps.Mul, new PyList([true]), 10L))
            : new PyList(Enumerable.Repeat<object?>(PyOps.TrueObject, 10));
        Assert.Equal(10L, PyOps.GuardedListSetFalseLoop(list, 1L, 8L, 3L));
        for (int i = 0; i < 10; i++)
            Assert.Same(i is 1 or 4 or 7 ? PyOps.FalseObject : PyOps.TrueObject, list.GetItem(i));
    }

    [Fact]
    public void OverflowReturnsExactBigIntegerAfterLastWrite()
    {
        var list = new PyList([true, true]);
        Assert.Equal(BigInteger.One << 63,
            Assert.IsType<BigInteger>(PyOps.GuardedListSetFalseLoop(list, 1L, 1L, long.MaxValue)));
        Assert.Same(PyOps.FalseObject, list.GetItem(1));
    }

    [Fact]
    public void RejectsAllInvalidGuardsWithoutMutation()
    {
        object?[][] inputs = [
            [true, 1L, 1L], [0L, true, 1L], [0L, 1L, true],
            [0, 1L, 1L], [0L, 1, 1L], [0L, 1L, 1],
            [BigInteger.Zero, 1L, 1L], [0L, 1L, BigInteger.One],
            [-1L, 1L, 1L], [2L, 1L, 1L], [0L, 2L, 1L],
            [0L, 1L, 0L], [0L, 1L, -1L], [null, 1L, 1L]
        ];
        foreach (var args in inputs)
        {
            var list = new PyList(["first", "second"]);
            Assert.Null(PyOps.GuardedListSetFalseLoop(list, args[0], args[1], args[2]));
            Assert.Equal(new object?[] { "first", "second" }, list.Items);
        }
        Assert.Null(PyOps.GuardedListSetFalseLoop(new PyDict(), 0L, 1L, 1L));
    }

    [Fact]
    public void NumericStorageRejectsWithoutMaterializing()
    {
        object holder = PyNumericListOps.Create();
        PyNumericListOps.AppendF64(holder, 1.0);
        var list = Assert.IsType<PyList>(holder);
        Assert.Null(PyOps.GuardedListSetFalseLoop(list, 0L, 0L, 1L));
        Assert.Null(typeof(PyList).GetField("_items", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(list));
        Assert.Equal(1.0, list.GetItem(0));
    }

    [Fact]
    public void HeterogeneousStoragePreservesUntouchedReferences()
    {
        object marker = new();
        var list = new PyList([marker, "x", marker]);
        Assert.Same(PyOps.BoxInt(2), PyOps.GuardedListSetFalseLoop(list, 1L, 1L, 1L));
        Assert.Same(marker, list.GetItem(0)); Assert.Same(marker, list.GetItem(2));
        Assert.Same(PyOps.FalseObject, list.GetItem(1));
    }

    [Fact]
    public void PublicAbiIsExactlyOneFourObjectMethod()
    {
        var methods = typeof(PyOps).GetMethods(BindingFlags.Public | BindingFlags.Static)
            .Where(m => m.Name == "GuardedListSetFalseLoop").ToArray();
        var method = Assert.Single(methods);
        Assert.Equal(typeof(object), method.ReturnType);
        Assert.Equal(Enumerable.Repeat(typeof(object), 4), method.GetParameters().Select(p => p.ParameterType));
        Assert.DoesNotContain(typeof(PyList).GetMethods(BindingFlags.Public | BindingFlags.Instance),
            m => m.Name == "GuardedSetFalseLoop");
    }
}
