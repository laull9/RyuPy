// 小方案的内部栈边界：真实Python函数回调有owner，原生partial直转发仍停在未知Static帧。
// 入口钩子由本单元显式调用，不冒充已编译Python或namedtuple真实消费者的端到端验证。
using Xunit;

namespace RyuPy.Runtime.Tests;

[Collection("生成器帧栈")]
public class PyFunctionOriginBoundaryTests
{
    private static PyFunction Function(string name, PyCall body) => new(name,
        PySignature.Create(name, [], [], []), body, []);

    [Fact]
    public void StaticFrameRegainsUnknownOriginAfterRealFunctionCallbackReturnsOrThrows()
    {
        long saved = PyTraceback.CaptureDepth();
        object? previousHandler = PyErrors.EnterHandler(null);
        var outer = Function("outer", (_, _, _, _) => PyNone.Instance);
        PyOps.FunctionModule(outer, "outer.module");
        var original = new PyValueError("callback failed");
        bool fail = false;
        int calls = 0;
        var callback = Function("callback", (self, args, names, values) =>
        {
            calls++;
            Assert.Empty(args);
            Assert.Empty(names);
            Assert.Empty(values);
            // 真实Invoke把实际函数作为self传入；这里模拟编译器函数入口/出口钩子。
            long depth = PyTraceback.PushFrameWithOrigin("callback.py", "callback", self);
            try
            {
                PyTraceback.SetLine(17);
                Assert.Equal("callback.module", PyTraceback.GetCallerModule());
                PyObject.SetAttr(self, "__module__", "during.callback");
                Assert.Equal("during.callback", PyTraceback.GetCallerModule());
                if (fail) throw (PyValueError)PyErrors.PrepareRaise(original);
                return "returned";
            }
            finally { PyTraceback.PopTo(depth); }
        });
        try
        {
            PyTraceback.PushFrameWithOrigin("outer.py", "outer", outer);
            long staticDepth = PyTraceback.PushFrame("static.py", "static_output");
            PyTraceback.SetLine(9);
            foreach (bool shouldFail in new[] { false, true })
            {
                fail = shouldFail;
                PyObject.SetAttr(callback, "__module__", "callback.module");
                long before = PyTraceback.CaptureDepth();
                if (fail)
                {
                    var caught = Assert.Throws<PyValueError>(() => PyOps.CallValue(callback, [], [], []));
                    Assert.Same(original, caught);
                    Assert.Equal(new PyStackFrame("callback.py", "callback", 17), caught.Frames[0]);
                    Assert.Equal(new PyStackFrame("static.py", "static_output", 9), caught.Frames[1]);
                }
                else Assert.Equal("returned", PyOps.CallValue(callback, [], [], []));
                Assert.Equal(before, PyTraceback.CaptureDepth());
                var unsupported = Assert.Throws<PyNotImplementedError>(() => PyTraceback.GetCallerModule());
                Assert.Equal(new PyStackFrame("static.py", "static_output", 9), unsupported.Frames[0]);
                Assert.Equal("during.callback", PyObject.GetAttr(callback, "__module__"));
            }
            Assert.Equal(2, calls);
            PyTraceback.PopTo(staticDepth);
            Assert.Equal("outer.module", PyTraceback.GetCallerModule());
        }
        finally
        {
            PyTraceback.PopTo(saved + 1);
            PyErrors.LeaveHandler(previousHandler);
        }
        Assert.Equal(saved, PyTraceback.CaptureDepth());
    }

    [Fact]
    public void NativePartialForwardingDoesNotBorrowAncestorOwner()
    {
        long saved = PyTraceback.CaptureDepth();
        object? previousHandler = PyErrors.EnterHandler(null);
        var outer = Function("outer", (_, _, _, _) => PyNone.Instance);
        PyOps.FunctionModule(outer, "outer.module");
        object marker = new PyList();
        var reader = new NativeReader(marker);
        var partial = new PyPartial(reader, [marker], ["flag"], [37L]);
        try
        {
            PyTraceback.PushFrameWithOrigin("outer.py", "outer", outer);
            long staticDepth = PyTraceback.PushFrame("static.py", "static_output");
            PyTraceback.SetLine(23);
            reader.ExpectedDepth = PyTraceback.CaptureDepth();
            var error = Assert.Throws<PyNotImplementedError>(() => PyOps.CallValue(partial, [], [], []));
            Assert.Same(reader.Failure, error);
            Assert.Equal(1, reader.Calls);
            Assert.Equal(reader.ExpectedDepth, PyTraceback.CaptureDepth());
            Assert.Equal(new PyStackFrame("static.py", "static_output", 23), error.Frames[0]);
            Assert.DoesNotContain(error.Frames, frame => frame.Function is "partial" or "reader");
            Assert.Throws<PyNotImplementedError>(() => PyTraceback.GetCallerModule());
            PyTraceback.PopTo(staticDepth);
            Assert.Equal("outer.module", PyTraceback.GetCallerModule());
        }
        finally
        {
            PyTraceback.PopTo(saved + 1);
            PyErrors.LeaveHandler(previousHandler);
        }
        Assert.Equal(saved, PyTraceback.CaptureDepth());
    }

    // 只存在于本测试程序集，不登记模块或Python名字；实际partial会调用此有限原生接口。
    private sealed class NativeReader(object marker) : IPyNativeCallable
    {
        internal long ExpectedDepth;
        internal int Calls;
        internal PyNotImplementedError? Failure;
        public object? Invoke(object?[] args, string[] names, object?[] values)
        {
            Calls++;
            Assert.Single(args);
            Assert.Same(marker, args[0]);
            Assert.Equal(new[] { "flag" }, names);
            Assert.Single(values);
            Assert.Equal(37L, values[0]);
            Assert.Equal(ExpectedDepth, PyTraceback.CaptureDepth());
            try { return PyTraceback.GetCallerModule(); }
            catch (PyNotImplementedError error) { Failure = error; throw; }
        }
    }
}
