// 不加载解释器的冷边界回归：机械验证嵌套入口、未知来源与展开后的诊断帧隔离。
// 反射仅用于测试私有状态，不增加产品公开ABI或NativeAOT运行时反射路径。
using System.Reflection;
using RyuPy.Runtime;

namespace RyuPy.CPython.Tests;

[CollectionDefinition("carrier来源冷门", DisableParallelization = true)]
public sealed class CarrierOriginCollection { }

[Collection("carrier来源冷门")]
public sealed class CPythonCarrierOriginTests
{
    private const BindingFlags PrivateStatic = BindingFlags.Static | BindingFlags.NonPublic;
    private static IDisposable Boundary(long depth)
    {
        Type type = typeof(CPythonHost).GetNestedType("ManagedCarrierCallbackScope", BindingFlags.NonPublic)!;
        ConstructorInfo constructor = type.GetConstructor(BindingFlags.NonPublic | BindingFlags.Instance,
            null, [typeof(long)], null)!;
        return (IDisposable)constructor.Invoke([depth]);
    }
    private static bool NewFrame => (bool)typeof(CPythonHost)
        .GetProperty("HasNewCompiledCarrierFrame", PrivateStatic)!.GetValue(null)!;
    private static void Retire() => typeof(CPythonHost)
        .GetMethod("SuppressRetiredCarrierFrames", PrivateStatic)!.Invoke(null, null);

    [Fact]
    public void NestedCallbackRestoresItsCallerAndDepthAloneCannotAuthorizeUnknownSource()
    {
        long start = PyTraceback.CaptureDepth();
        try
        {
            Assert.False(NewFrame);
            using (Boundary(start))
            {
                Assert.False(NewFrame);
                long outer = PyTraceback.PushFrame("boundary.py", "outer");
                Assert.True(NewFrame);
                // 新深度只是候选；真实TryCallCarrier还要求3out提供可信instance来源。
                Assert.Equal(3, PyTraceback.CompiledNativeCallSite(out _, out _, out _));
                using (Boundary(PyTraceback.CaptureDepth()))
                {
                    Assert.False(NewFrame);
                    long inner = PyTraceback.PushFrame("boundary.py", "inner");
                    Assert.True(NewFrame);
                    PyTraceback.PopTo(inner); Assert.False(NewFrame);
                }
                Assert.True(NewFrame);
                PyTraceback.PopTo(outer); Assert.False(NewFrame);
            }
            Assert.False(NewFrame);
        }
        finally { PyTraceback.PopTo(start + 1); }
    }

    [Fact]
    public void ErrorExportRejectsRetiredFramesButAllowsARealNewNestedCallback()
    {
        long start = PyTraceback.CaptureDepth();
        try
        {
            using (Boundary(start))
            {
                long failed = PyTraceback.PushFrame("boundary.py", "failed_body");
                Assert.True(NewFrame);
                Retire(); Assert.False(NewFrame);
                using (Boundary(PyTraceback.CaptureDepth()))
                {
                    long nested = PyTraceback.PushFrame("boundary.py", "error_protocol");
                    Assert.True(NewFrame);
                    PyTraceback.PopTo(nested);
                }
                Assert.False(NewFrame);
                PyTraceback.PopTo(failed); Assert.False(NewFrame);
            }
            Assert.False(NewFrame);
        }
        finally { PyTraceback.PopTo(start + 1); }
    }
}
