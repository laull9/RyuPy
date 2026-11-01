using Xunit;
namespace RyuPy.Runtime.Tests;

/// <summary>Ellipsis不依赖宿主对象字符串，构造/哈希/显示均保持真实单例协议。</summary>
public class PyEllipsisTests
{
    [Fact]
    public void SingletonConstructionAndDisplayAreStable()
    {
        Assert.Same(PyEllipsis.Instance, PyEllipsis.Create([], []));
        Assert.Equal("ellipsis", PyEllipsis.Instance.TypeName);
        Assert.Equal("Ellipsis", PyRepr.Repr(PyEllipsis.Instance));
        Assert.Equal("Ellipsis", PyRepr.Str(PyEllipsis.Instance));
        Assert.Equal(PyHash.Of(PyEllipsis.Instance), PyHash.Of(PyEllipsis.Create([], [])));
        Assert.Throws<PyTypeError>(() => PyEllipsis.Create([1L], []));
        Assert.Throws<PyTypeError>(() => PyEllipsis.Create([], ["value"]));
    }
}
