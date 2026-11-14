// 宿主验证冷读取的共享字典身份；不以不受支持的legacy builtins模块替代真实字典测试。
using Xunit;

namespace RyuPy.Runtime.Tests;

[Collection("生成器帧栈")]
public class PyMapFallbackTests
{
    [Fact]
    public void UnboundMapReadsLiveDefaultButNeverOverridesBoundValues()
    {
        var builtins = PyModuleBuiltins.Default;
        object? original = builtins.Get("map");
        try
        {
            Assert.Same(PyMap.Type, PyOps.LoadDeletedModuleName(PyUnbound.Instance, "map"));
            var replacement = new PyList([]);
            builtins.Set("map", replacement);
            Assert.Same(replacement, PyOps.LoadDeletedModuleName(PyUnbound.Instance, "map"));
            Assert.Same(original, PyOps.LoadDeletedModuleName(original, "map"));
            Assert.Same(PyNone.Instance, PyOps.LoadDeletedModuleName(PyNone.Instance, "map"));
            builtins.Set("map", PyNone.Instance);
            Assert.Same(PyNone.Instance, PyOps.LoadDeletedModuleName(PyUnbound.Instance, "map"));
            builtins.DeleteIfPresent("map");
            Assert.Throws<PyNameError>(() => PyOps.LoadDeletedModuleName(PyUnbound.Instance, "map"));
            Assert.Throws<PyNameError>(() => PyOps.CheckModuleDeleteValue(PyUnbound.Instance, "map"));
        }
        finally { builtins.Set("map", original); }
    }
}
