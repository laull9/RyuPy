using RyuPy.Runtime;
using Xunit;

namespace RyuPy.Runtime.Tests;

/// <summary>复制的身份图而非仅值相等：环、共享子节点、不可变 tuple 与安全拒绝边界。</summary>
public class PyCopyTests
{
    private static object? Deep(object? value) => PyOps.CallValue(PyCopy.CreateAttributes()["deepcopy"], [value], [], []);

    [Fact]
    public void ListAndDictionaryCyclesPreserveTopology()
    {
        var list = new PyList();
        var dictionary = new PyDict();
        list.Append(dictionary);
        dictionary.Set("self", list);
        var result = Assert.IsType<PyList>(Deep(list));
        Assert.NotSame(list, result);
        Assert.Same(result, Assert.IsType<PyDict>(result.Items[0]).Get("self"));
    }

    [Fact]
    public void TupleCycleReusesRecursivelyMemoizedResult()
    {
        var list = new PyList();
        var tuple = new PyTuple([list]);
        list.Append(tuple);
        var result = Assert.IsType<PyTuple>(Deep(tuple));
        Assert.NotSame(tuple, result);
        Assert.Same(result, Assert.IsType<PyList>(result.Items[0]).Items[0]);
        var immutable = new PyTuple([1L, "a"]);
        Assert.Same(immutable, Deep(immutable));
    }

    [Fact]
    public void CapacityAndUnsupportedMemoDoNotLeakClrErrors()
    {
        var top = new PyList();
        var current = top;
        for (int index = 0; index < 300; index++)
        {
            var next = new PyList(); current.Append(next); current = next;
        }
        Assert.Throws<PyRecursionError>(() => Deep(top));
        Assert.Throws<PyNotImplementedError>(() => PyOps.CallValue(PyCopy.CreateAttributes()["deepcopy"], [top, new PyDict()], [], []));
    }
}
