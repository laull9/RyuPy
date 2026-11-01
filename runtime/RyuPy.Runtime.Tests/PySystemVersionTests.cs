using RyuPy.Runtime;
using Xunit;

namespace RyuPy.Runtime.Tests;

/// <summary>版本元数据与实现身份分开验收；不把不同实现字符串伪作 CPython 差分。</summary>
public class PySystemVersionTests
{
    [Fact]
    public void MetadataIsTupleCompatibleAndImmutable()
    {
        PyVersionInfo version = PySystemVersion.VersionInfo;
        Assert.Same(version, PySystemVersion.VersionInfo);
        Assert.Equal(new object?[] { 3L, 12L, 13L, "final", 0L }, PyOps.ToSequence(version));
        Assert.Equal(3L, PyOps.GetItem(version, 0L));
        Assert.Equal(13L, version.GetAttr("micro"));
        Assert.Throws<PyAttributeError>(() => version.SetAttr("major", 4L));
        Assert.Throws<PyAttributeError>(() => version.DelAttr("minor"));
        Assert.True(PyHash.Equal(version, new PyTuple([3L, 12L, 13L, "final", 0L])));
        Assert.True(PyOps.Compare(PyOps.Gt, version, new PyTuple([3L, 12L])));
        Assert.Equal(PyHash.Of(new PyTuple([3L, 12L, 13L, "final", 0L])), PyHash.Of(version));
        Assert.Throws<PyTypeError>(() => PyOps.Compare(PyOps.Lt, version, new PyList([3L])));
        Assert.Throws<PyTypeError>(() => PyOps.Compare(PyOps.Lt, new PyList([3L]), version));
    }

    [Fact]
    public void ImplementationNeverClaimsToBeCpython()
    {
        Assert.Equal("ryupy", PySystemVersion.Implementation.GetAttr("name"));
        Assert.Contains("RyuPy", PySystemVersion.Version);
        Assert.DoesNotContain("CPython", PySystemVersion.Version);
        _ = PySystemVersion.VersionInfo;
        Assert.True(PyNativeType.LookupByName("version_info")!.IsSubclassOfBuiltin("tuple"));
    }
}
