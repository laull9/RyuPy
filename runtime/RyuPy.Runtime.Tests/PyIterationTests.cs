// 迭代协议回归：__iter__ 的返回对象不应再次求迭代器，且必须验证 __next__。
using Xunit;

namespace RyuPy.Runtime.Tests;

public class PyIterationTests
{
    private static PyFunction Fn(string name, PyCall body) => new(
        name, PySignature.Create(name, ["self"], ["pos"], ["0"]), body, []);

    [Fact]
    public void ReturnedIteratorDoesNotRepeatIterSideEffects()
    {
        int calls = 0;
        var counterClass = (PyClass)PyOps.MakeClass("Counter", [], null);
        counterClass.SetAttr("__iter__", Fn("__iter__", (_, args, _, _) => { calls++; return args[0]; }));
        counterClass.SetAttr("__next__", Fn("__next__", (_, _, _, _) => 7L));
        var counter = (PyInstance)counterClass.Invoke([], [], [])!;
        var sourceClass = (PyClass)PyOps.MakeClass("Source", [], null);
        sourceClass.SetAttr("__iter__", Fn("__iter__", (_, _, _, _) => counter));
        var source = (PyInstance)sourceClass.Invoke([], [], [])!;
        IPyIterator iterator = PyIter.Create(source);
        Assert.Equal(0, calls);
        Assert.True(iterator.MoveNext());
        Assert.Equal(7L, iterator.Current());
    }

    [Fact]
    public void IterableResultWithoutNextIsRejectedImmediately()
    {
        var cls = (PyClass)PyOps.MakeClass("Invalid", [], null);
        cls.SetAttr("__iter__", Fn("__iter__", (_, _, _, _) => new PyList([])));
        var value = (PyInstance)cls.Invoke([], [], [])!;
        var error = Assert.Throws<PyTypeError>(() => PyIter.Create(value));
        Assert.Equal("iter() returned non-iterator of type 'list'", error.Message);
    }
}
