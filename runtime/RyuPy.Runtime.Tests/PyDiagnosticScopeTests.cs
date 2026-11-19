// 一次性诊断权限的绑定/封装合同；请求不能由公开构造器铸造，也不能跨provider重复使用。
using RyuPy.Runtime;

namespace RyuPy.Runtime.Tests;

public sealed class PyDiagnosticScopeTests
{
    [Fact]
    public void RequestHasNoPublicConstructorAndIsSealed()
    {
        Assert.True(typeof(PyDiagnosticScopeRequest).IsSealed);
        Assert.Empty(typeof(PyDiagnosticScopeRequest).GetConstructors());
    }
    [Fact]
    public void RequestOnlyBindsItsProviderAndConsumesOnce()
    {
        object target = new(), other = new();
        var request = new PyDiagnosticScopeRequest(target);
        Assert.False(request.TryConsume(other));
        Assert.True(request.TryConsume(target));
        Assert.False(request.TryConsume(target));
        Assert.False(request.TryConsume(other));
    }
}
