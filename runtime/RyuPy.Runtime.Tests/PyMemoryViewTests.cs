using RyuPy.Runtime;
using Xunit;

namespace RyuPy.Runtime.Tests;

/// <summary>视图共享、独立导出租约、c格式buffer复制与显式未支持边界。</summary>
public class PyMemoryViewTests
{
    private static PyMemoryView View(object value) => (PyMemoryView)PyMemoryView.Create([value], [], []);
    private static object? Call(PyMemoryView view, string name, params object?[] args) => PyOps.CallValue(view.GetAttr(name), args, [], []);

    [Fact]
    public void SliceKeepsTemporaryLeaseWhenIndexReleasesOriginalView()
    {
        foreach (bool resize in new[] { false, true })
        {
            var data = new PyByteArray([65, 66]);
            var view = View(data);
            var cls = new PyClass("SliceReleaseIndex");
            cls.SetAttr("__index__", PyNativeFunctions.Create("__index__", ["self"], [null], 1, _ =>
            {
                view.Release();
                if (resize) data.DelItem(0L);
                return 0L;
            }));
            var slice = new PySlice(new PyInstance(cls), PyNone.Instance, PyNone.Instance);
            if (resize) Assert.Throws<PyBufferError>(() => view.GetItem(slice));
            else
            {
                var child = (PyMemoryView)view.GetItem(slice);
                Assert.Equal(new byte[] { 65, 66 }, child.CopyBuffer());
                child.Release();
            }
            data.Append(67L);
            Assert.Equal(3, data.Count);
        }
    }

    [Fact]
    public void IndexCallbacksCannotWriteAfterReleasingTheirView()
    {
        foreach (bool releaseInKey in new[] { true, false })
        {
            var data = new PyByteArray([65]);
            var view = View(data);
            var cls = new PyClass("ReleasingIndex");
            cls.SetAttr("__index__", PyNativeFunctions.Create("__index__", ["self"], [null], 1, _ =>
            {
                view.Release(); data.DelItem(0L); return releaseInKey ? 0L : 66L;
            }));
            var callback = new PyInstance(cls);
            Assert.Throws<PyValueError>(() => view.SetItem(releaseInKey ? callback : 0L, releaseInKey ? 66L : callback));
            Assert.Equal(0, data.Count);
        }
    }

    [Fact]
    public void UserIndexExceptionIsNotRewrittenAsRangeFailure()
    {
        var cls = new PyClass("FailingIndex");
        var failure = new PyOverflowError("user index failure");
        cls.SetAttr("__index__", PyNativeFunctions.Create("__index__", ["self"], [null], 1, _ => throw failure));
        var value = new PyInstance(cls);
        var data = new PyByteArray([1]);
        var view = View(data);
        try
        {
            Assert.Same(failure, Assert.Throws<PyOverflowError>(() => data.Append(value)));
            Assert.Throws<PyValueError>(() => view.SetItem(0L, value));
            Assert.Throws<PyValueError>(() => data.Append(System.Numerics.BigInteger.One << 100));
        }
        finally { view.Release(); }
    }

    [Fact]
    public void CachedReadOnlyHashSurvivesReleaseButUncomputedHashDoesNot()
    {
        var view = View(new PyBytes([65, 66]));
        object? hash = Call(view, "__hash__");
        view.Release();
        Assert.Equal(hash, Call(view, "__hash__"));
        var fresh = View(new PyBytes([65, 66]));
        fresh.Release();
        Assert.Throws<PyValueError>(() => Call(fresh, "__hash__"));
    }

    [Fact]
    public void KeywordArgumentsAndReleasedTypeChecksKeepPythonOrder()
    {
        var view = (PyMemoryView)PyMemoryView.Create([], ["object"], [new PyBytes([65, 66])]);
        var chars = (PyMemoryView)PyOps.CallValue(view.GetAttr("cast"), [], ["format"], ["c"])!;
        try
        {
            Assert.Equal(new byte[] { 65, 66 }, ((PyBytes)PyOps.CallValue(view.GetAttr("tobytes"), [], ["order"], ["F"])!).Bytes);
            Assert.Throws<PyTypeError>(() => Call(view, "cast", "B", PyNone.Instance));
            Assert.Throws<PyValueError>(() => Call(view, "tobytes", "X"));
            view.Release();
            Assert.Throws<PyTypeError>(() => Call(view, "cast", 1L));
            Assert.Throws<PyTypeError>(() => Call(view, "tobytes", 1L));
            Assert.Throws<PyValueError>(() => Call(view, "cast", "B", PyNone.Instance));
        }
        finally { view.Release(); chars.Release(); }
    }

    [Fact]
    public void ByteArrayWritesStayVisibleAndChildOutlivesParent()
    {
        var data = new PyByteArray([97, 98, 99]);
        var root = View(data);
        var chars = (PyMemoryView)Call(root, "cast", "c")!;
        var child = View(chars);
        try
        {
            data.SetItem(1L, 88L);
            Assert.Equal(new byte[] { 88 }, ((PyBytes)child.GetItem(1L)).Bytes);
            chars.SetItem(0L, new PyBytes([81]));
            Assert.Equal(81L, root.GetItem(0L));
            root.Release(); chars.Release();
            Assert.Equal(new byte[] { 81, 88, 99 }, child.CopyBuffer());
            Assert.Throws<PyBufferError>(() => data.Append(1L));
        }
        finally { root.Release(); chars.Release(); child.Release(); }
        data.Append(1L);
        Assert.Equal(4, data.Count);
    }

    [Fact]
    public void ReleasedOperationsFailButReleaseIsIdempotent()
    {
        var view = View(new PyBytes([1]));
        view.Release(); view.Release();
        Assert.Throws<PyValueError>(() => view.GetItem(0L));
        Assert.Throws<PyValueError>(() => view.CopyBuffer());
        Assert.Throws<PyValueError>(() => view.GetAttr("obj"));
        Assert.Throws<PyValueError>(() => Call(view, "cast", "B"));
    }

    [Fact]
    public void CharacterFormatIsNotAnIterableOfIntegerBytes()
    {
        var root = View(new PyBytes([65, 66]));
        var chars = (PyMemoryView)Call(root, "cast", "c")!;
        try
        {
            var output = new PyByteArray([]);
            output.Extend(chars);
            Assert.Equal(new byte[] { 65, 66 }, output.ToBytes().Bytes);
            Assert.Throws<PyTypeError>(() => output.Extend(PyOps.ToSequence(chars)));
            Assert.Throws<PyTypeError>(() => chars.SetItem(0L, new PyBytes([1])));
        }
        finally { root.Release(); chars.Release(); }
    }

    [Fact]
    public void EveryExistingResizeEntryHonorsExportsWithoutRejectingNoOps()
    {
        var data = new PyByteArray([1, 2]);
        var view = View(data);
        try
        {
            Assert.Throws<PyBufferError>(() => data.Append(3L));
            Assert.Throws<PyBufferError>(() => data.Extend(new PyBytes([3])));
            Assert.Throws<PyBufferError>(() => data.DelItem(0L));
            Assert.Throws<PyBufferError>(() => PyOps.InPlaceBinary(PyOps.Add, data, data));
            Assert.Throws<PyBufferError>(() => PyOps.InPlaceBinary(PyOps.Mul, data, 0L));
            data.Extend(new PyBytes([]));
            Assert.Same(data, PyOps.InPlaceBinary(PyOps.Mul, data, 1L));
            Assert.Throws<PyIndexError>(() => data.DelItem(99L));
            Assert.Throws<PyValueError>(() => data.Extend(new PyList([999L])));
        }
        finally { view.Release(); }
        data.DelItem(0L);
        Assert.Equal(1, data.Count);
    }

    [Fact]
    public void UnsupportedLayoutDoesNotCopyOrPretendToBeAView()
    {
        var view = View(new PyBytes([1, 2]));
        try
        {
            Assert.Throws<PyNotImplementedError>(() => Call(view, "cast", "I"));
            Assert.Throws<PyNotImplementedError>(() => Call(view, "cast", "B", new PyList([2L])));
            Assert.Equal("B", view.GetAttr("format"));
        }
        finally { view.Release(); }
        Assert.Throws<PyTypeError>(() => View(new PyList([1L])));
    }
}
