using RyuPy.Runtime;
using Xunit;

namespace RyuPy.Runtime.Tests;

/// <summary>future 事实元数据与编译标志分离；读取未启用的特性描述不擅自开启其语义。</summary>
public class PyFutureTests
{
    [Fact]
    public void FeatureObjectsHaveStableIdentityAndPinnedReleaseFacts()
    {
        var attributes = PyFuture.CreateAttributes();
        object? division = attributes["division"];
        Assert.Same(division, PyFuture.CreateAttributes()["division"]);
        Assert.Equal(131072L, PyObject.GetAttr(division, "compiler_flag"));
        Assert.Equal(new object?[] { 2L, 2L, 0L, "alpha", 2L },
            Assert.IsType<PyTuple>(PyOps.CallValue(PyObject.GetAttr(division, "getOptionalRelease"), [], [], [])).Items);
        Assert.Equal("_Feature((2, 2, 0, 'alpha', 2), (3, 0, 0, 'alpha', 0), 131072)", PyRepr.Repr(division));
        Assert.Same(PyNone.Instance, PyObject.GetAttr(attributes["annotations"], "mandatory"));
        Assert.Equal(10, Assert.IsType<PyList>(attributes["all_feature_names"]).Count);
        Assert.Throws<PyNotImplementedError>(() => PyOps.CallValue(attributes["_Feature"], [], [], []));
    }
}
