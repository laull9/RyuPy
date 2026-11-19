// 新跨程序集布局冷协议恰好四条元数据；marker解析与resolver等实现类型保持内部。
namespace RyuPy.Runtime.Tests;

public partial class PyTraceStateArtifactTests
{
    private static string[] ApplicationLayoutPublicAdditions() =>
    [
        "T:RyuPy.Runtime.PyApplicationLayout:1048961:System.Object:",
        "M:RyuPy.Runtime.PyApplicationLayout:get_DependencyRoot:2198:[0;0;0]()->String:",
        "P:RyuPy.Runtime.PyApplicationLayout:DependencyRoot:0:[8;0;0]()->String",
        "M:RyuPy.Runtime.PyApplicationLayout:MapLegacyRelative:150:[0;0;1](String)->String:"
    ];
    [Fact]
    public void ApplicationLayoutHasExactlyDeclaredColdAbi()
    {
        var actual = PublicAbi(typeof(PyApplicationLayout).Assembly.Location)
            .Where(row => row.Contains(":RyuPy.Runtime.PyApplicationLayout:", StringComparison.Ordinal)).ToArray();
        Assert.Equal(ApplicationLayoutPublicAdditions().Order(StringComparer.Ordinal), actual.Order(StringComparer.Ordinal));
        Assert.False(typeof(PyApplicationLayout).GetNestedType("Layout", System.Reflection.BindingFlags.NonPublic)!.IsPublic);
    }
}
