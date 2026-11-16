// 随包编译源码位置：JIT/AOT 共用程序集根，拒绝逃逸及未冻结的路径。
using RyuPy.Runtime;

namespace RyuPy.Runtime.Tests;

public class PyBundledModuleLocationTests
{
    [Fact]
    public void BundleLocationUsesApplicationRoot()
    {
        const string relative = "python/ryupy_compiled/pkg/unit.py";
        Assert.Equal(Path.Combine(AppContext.BaseDirectory, "python", "ryupy_compiled", "pkg", "unit.py"),
            PyModuleLocations.Resolve(3, relative));
    }

    [Theory]
    [InlineData("/tmp/unit.py")]
    [InlineData("python/ryupy_compiled/../unit.py")]
    [InlineData("python/ryupy_compiled/pkg\\unit.py")]
    [InlineData("python/other/unit.py")]
    [InlineData("python/ryupy_compiled//unit.py")]
    public void BundleLocationRejectsPathsOutsideFrozenTree(string value) =>
        Assert.Throws<PyRuntimeError>(() => PyModuleLocations.Resolve(3, value));
}
