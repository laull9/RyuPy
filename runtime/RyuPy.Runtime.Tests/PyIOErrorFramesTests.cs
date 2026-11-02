// I/O 宿主异常映射必须在真实调用现场保存已有报告帧；不改变纯映射器或公开 traceback 对象范围。
using System;
using System.IO;
using RyuPy.Runtime;
using Xunit;

namespace RyuPy.Runtime.Tests;

[Collection("生成器帧栈")]
public class PyIOErrorFramesTests
{
    private static string Missing() => Path.Combine(Path.GetTempPath(), "ryupy-missing-" + Guid.NewGuid().ToString("N"), "data.txt");

    [Fact]
    public void OpenMissingCapturesCallerAndCalleeBeforeWithEntry()
    {
        long depth = PyTraceback.PushFrame("main.py", "<module>");
        try
        {
            PyTraceback.SetLine(11);
            PyTraceback.PushFrame("english.py", "load_text"); PyTraceback.SetLine(14);
            var error = Assert.Throws<PyFileNotFoundError>(() => PyIO.Open([Missing(), "r"], ["encoding"], ["utf-8"]));
            Assert.Equal(new PyStackFrame("english.py", "load_text", 14), error.Frames[0]);
            Assert.Equal(new PyStackFrame("main.py", "<module>", 11), error.Frames[1]);
            string formatted = PyTraceback.Format(error);
            int caller = formatted.IndexOf("line 11, in <module>", StringComparison.Ordinal);
            int callee = formatted.IndexOf("line 14, in load_text", StringComparison.Ordinal);
            Assert.True(caller >= 0 && callee > caller);
            Assert.Contains("FileNotFoundError:", formatted);
        }
        finally { PyTraceback.PopTo(depth); }
    }

    [Fact]
    public void MappedThrowKeepsActiveContextButPureMapperDoesNotAttachOrPrepare()
    {
        var outer = new PyValueError("outer");
        object? previous = PyErrors.EnterHandler(outer);
        long depth = PyTraceback.PushFrame("handler.py", "load");
        try
        {
            PyTraceback.SetLine(23);
            var pure = Assert.IsType<PyFileNotFoundError>(PyIO.FileSystemError(new FileNotFoundException("pure")));
            Assert.Empty(pure.Frames); Assert.Null(pure.Context);
            var mapped = Assert.Throws<PyFileNotFoundError>(() => PyIO.Open([Missing()], [], []));
            Assert.Same(outer, mapped.Context); Assert.Same(outer, PyErrors.Current);
            Assert.Equal(new PyStackFrame("handler.py", "load", 23), mapped.Frames[0]);
        }
        finally { PyTraceback.PopTo(depth); PyErrors.LeaveHandler(previous); }
    }
}
