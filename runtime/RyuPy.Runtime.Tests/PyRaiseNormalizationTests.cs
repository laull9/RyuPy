// 默认旧API重构回归：归一化、Attach和当前context顺序仍由真实运行时执行。
using RyuPy.Runtime;
namespace RyuPy.Runtime.Tests;

[Collection("生成器帧栈")]
public class PyRaiseNormalizationTests
{
    [Fact]
    public void PrepareRaisePreservesIdentityExistingFramesAndContext()
    {
        PyTraceback.Reset();
        long depth = PyTraceback.PushFrame("normalization.py", "source");
        PyTraceback.SetLine(7);
        var current = new PyValueError("context");
        object? previous = PyErrors.EnterHandler(current);
        try
        {
            var error = new PyTypeError("error");
            Assert.Same(error, PyErrors.PrepareRaise(error));
            Assert.Same(current, error.Context);
            var frames = error.Frames;
            Assert.Single(frames);
            Assert.Equal(7, frames[0].Line);
            PyTraceback.SetLine(9);
            Assert.Same(error, PyErrors.PrepareRaise(error));
            Assert.Same(frames, error.Frames);
        }
        finally { PyErrors.LeaveHandler(previous); PyTraceback.PopTo(depth); }
    }

    [Fact]
    public void PrepareRaiseRejectsNonExceptionWithoutChangingCurrentContext()
    {
        var current = new PyValueError("context");
        object? previous = PyErrors.EnterHandler(current);
        try
        {
            Assert.Throws<PyTypeError>(() => PyErrors.PrepareRaise(1L));
            Assert.Same(current, PyErrors.Current);
        }
        finally { PyErrors.LeaveHandler(previous); }
    }
}
