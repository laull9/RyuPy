using RyuPy.Runtime;
using Xunit;

namespace RyuPy.Runtime.Tests;

/// <summary>用户目录真实槽位过滤、类继承与显式未支持边界，不伪造 object 的完整目录。</summary>
public class PyDirTests
{
    [Fact]
    public void DirectoryUsesOnlyAssignedSlotsOfThisInstance()
    {
        var cls = new PyClass("DirCase"); cls.SetAttr("value", 1L);
        var first = new PyInstance(cls); var second = new PyInstance(cls);
        PyObject.SetAttr(first, "local", 2L); PyObject.SetAttr(second, "other", 3L);
        Assert.Equal(new object?[] { "local", "value" }, PyOps.ToSequence(PyDir.Call([first], [], [])));
        PyObject.DelAttr(first, "local");
        Assert.Equal(new object?[] { "value" }, PyOps.ToSequence(PyDir.Call([first], [], [])));
    }

    [Fact]
    public void CustomDirectoryIsSortedWithoutDeduplicatingOrStringCoercion()
    {
        var cls = new PyClass("CustomDir");
        cls.SetAttr("__dir__", PyNativeFunctions.Create("__dir__", ["self"], [null], 1,
            _ => new PyList([3L, 1L, 1L])));
        Assert.Equal(new object?[] { 1L, 1L, 3L }, PyOps.ToSequence(PyDir.Call([new PyInstance(cls)], [], [])));
        Assert.Equal(new object?[] { "__dir__" }, PyOps.ToSequence(PyDir.Call([cls], [], [])));
    }

    [Fact]
    public void UnimplementedObjectKindsAndFrameDirectoryFailExplicitly()
    {
        Assert.Throws<PyNotImplementedError>(() => PyDir.Call([], [], []));
        Assert.Throws<PyNotImplementedError>(() => PyDir.Call([1L], [], []));
        Assert.Throws<PyTypeError>(() => PyDir.Call([1L, 2L], [], []));
    }
}
