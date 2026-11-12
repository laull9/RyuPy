// 精确set方法的统一路由、参数验证及失败时可见状态。
using Xunit;
namespace RyuPy.Runtime.Tests;

public class PySetFamilyTests
{
    [Theory]
    [InlineData("direct")]
    [InlineData("saved")]
    [InlineData("pic")]
    public void RoutesShareQueryUpdateAndValidation(string route)
    {
        var set = new PySet([1L, 2L]);
        object? Call(string name, object?[] args, string[]? names = null) => route switch
        {
            "saved" => PyOps.CallValue(PyObject.GetAttr(set, name), args, names ?? [], names is null ? [] : [0L]),
            "pic" => PyPic.CallMethod(3928, set, name, args, names ?? [], names is null ? [] : [0L]),
            _ => PyOps.CallMethod(set, name, args, names ?? [], names is null ? [] : [0L]),
        };
        Assert.Equal(true, Call("isdisjoint", [new PyList([4L])]));
        Assert.Equal(true, Call("issubset", [new PyList([1L, 2L, 3L])]));
        Assert.Same(PyNone.Instance, Call("update", [new PyList([3L]), new PyTuple([4L])]));
        Assert.Equal(4, set.Count);
        Assert.Throws<PyTypeError>(() => Call("update", [new PyList([9L])], ["bad"]));
        Assert.False(set.Contains(9L));
        Assert.Throws<PyTypeError>(() => Call("copy", [1L]));
        Assert.Throws<PyTypeError>(() => Call("isdisjoint", []));
        Assert.Same(PyNone.Instance, Call("difference_update", [set]));
        Assert.Equal(0, set.Count);
    }

    [Fact]
    public void QueryShortCircuitsWithoutConsumingTail()
    {
        foreach (string name in new[] { "isdisjoint", "issuperset", "issubset", "intersection" })
        {
            var source = new FailAfterOne();
            var set = new PySet(name == "issuperset" ? [8L] : [2L]);
            PyOps.CallMethod(set, name, [source], [], []);
            Assert.Equal(1, source.Calls);
        }
    }

    private sealed class FailAfterOne : IPyIterator
    {
        public int Calls;
        public bool MoveNext() { if (++Calls > 1) throw new PyValueError("tail"); return true; }
        public object Current() => 2L;
    }
}
