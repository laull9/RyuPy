using RyuPy.Runtime;

namespace RyuPy.Runtime.Tests;

/// <summary>守卫叶调用与普通压帧共用递归边界，检查本身不改变调用位置或帧深度。</summary>
[Collection("生成器帧栈")]
public class PyTracebackDepthTests
{
    [Fact]
    public void CheckDepthPreservesFrameAndMatchesPushFailure()
    {
        long saved = PyTraceback.CaptureDepth();
        try
        {
            PyRecursionError? pushError = null;
            while (pushError is null)
            {
                long before = PyTraceback.CaptureDepth();
                try { PyTraceback.PushFrame("depth.py", "caller"); }
                catch (PyRecursionError error)
                {
                    pushError = error;
                    Assert.Equal(before, PyTraceback.CaptureDepth());
                }
                if (pushError is null)
                {
                    PyTraceback.SetLine(37);
                    // 最后一个合法帧的下一次检查会失败；在此前成功检查不写帧。
                    try { PyTraceback.CheckDepth(); }
                    catch (PyRecursionError) { }
                    Assert.Equal(before + 1, PyTraceback.CaptureDepth());
                    Assert.Equal(37, PyTraceback.GetCallerLocation(1).Line);
                }
            }
            long full = PyTraceback.CaptureDepth();
            var checkedError = Assert.Throws<PyRecursionError>(PyTraceback.CheckDepth);
            Assert.Equal(pushError.PyMessage, checkedError.PyMessage);
            Assert.Equal(full, PyTraceback.CaptureDepth());
            Assert.Empty(checkedError.Frames);
        }
        finally { PyTraceback.PopTo(saved + 1); }
    }
}
