// 编译产品启动的真实Runtime PE增量：旧公开初始化器保持，新增仅同签名基础入口，无额外重载。
namespace RyuPy.Runtime.Tests;

public partial class PyTraceStateArtifactTests
{
    [Fact]
    public void CompiledStartupAddsOnlyTheDeclaredInitializerAndPreservesLegacyAbi()
    {
        var actual = PublicAbi(typeof(PySystem).Assembly.Location)
            .Where(item => item.StartsWith("M:RyuPy.Runtime.PySystem:Initialize", StringComparison.Ordinal))
            .Order(StringComparer.Ordinal).ToArray();
        Assert.Equal(new[] {
            "M:RyuPy.Runtime.PySystem:Initialize:150:[0;0;2](String,String[])->Void:",
            "M:RyuPy.Runtime.PySystem:InitializeCompiledBase:150:[0;0;2](String,String[])->Void:"
        }, actual);
    }
}
