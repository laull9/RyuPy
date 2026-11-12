// starmap 入口、惰性及恢复规则；完整 Python 调用/重入由差分用例覆盖。
using RyuPy.Runtime;
using Xunit;

namespace RyuPy.Runtime.Tests;

public class PyStarMapTests
{
    private static object New(object? function, object? source) =>
        PyOps.CallValue(PyItertools.CreateAttributes()["starmap"], [function, source], [], [])!;

    [Fact]
    public void ConstructorDoesNotConsumeOrValidateCallable()
    {
        Assert.Empty(PyOps.ToSequence(New(null, new PyList([]))));
        var rows = new PyList([]);
        object iterator = New(null, rows);
        rows.Items.Add(new PyTuple([]));
        Assert.Throws<PyTypeError>(() => ((IPyIterator)iterator).MoveNext());
        Assert.False(((IPyIterator)iterator).MoveNext());
    }

    [Fact]
    public void BadRowAndCallableFailureDoNotCloseSource()
    {
        int calls = 0;
        var function = PyNativeFunctions.Create("f", ["x"], [null], 1, args =>
        {
            calls++;
            if (calls == 1) throw new PyValueError("first");
            return args[0];
        });
        IPyIterator iterator = (IPyIterator)New(function, new PyTuple([
            new PyTuple([1L]), 99L, new PyTuple([2L])]));
        Assert.Throws<PyValueError>(() => iterator.MoveNext());
        Assert.Throws<PyTypeError>(() => iterator.MoveNext());
        Assert.True(iterator.MoveNext());
        Assert.Equal(2L, iterator.Current());
        Assert.False(iterator.MoveNext());
        Assert.Equal(2, calls);
    }

    [Fact]
    public void TypeIdentityAndPositionalOnlyArityAreStable()
    {
        object? type = PyItertools.CreateAttributes()["starmap"];
        object iterator = New(null, new PyTuple([]));
        Assert.Same(type, PyObject.TypeOf(iterator));
        Assert.Throws<PyTypeError>(() => PyOps.CallValue(type, [null], ["iterable"], [new PyTuple([])]));
        Assert.Throws<PyTypeError>(() => PyOps.CallValue(type, [], [], []));
    }
}
