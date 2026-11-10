// 新状态机真实错误路径在V1/legacy分别建源；普通构造与用户异常不被当作Fresh。
#if !TRACE_ADAPTER_TEST
using RyuPy.Runtime;

namespace RyuPy.Runtime.Tests;

[Collection("生成器帧栈")]
public class PyXmlTreeBuilderTraceTests
{
    [Fact]
    public void EveryOwnErrorUsesFreshOnlyAtItsColdThrowSite()
    {
        Assert.Null(PyTraceProduct.Active);
        PyTraceback.Reset(); PyErrors.LeaveHandler(null); PyAtexit.Clear();
        PySystem.Initialize("treebuilder-producers.py", []); PyNativeModules.Register();
        object? Call(object? f, params object?[] args) => PyOps.CallValue(f, args, [], []);
        var builder = (PyXmlTreeBuilder)Call(PyXmlTreeBuilder.Type)!;
        var globals = PyXmlElementTree.CreateAttributes(); object? savedComment = globals["Comment"];
        globals.Remove("Comment");
        Func<object?>[] operations =
        [
            () => Call(builder.GetAttr("end"), "empty"),
            () => Call(PyXmlTreeBuilder.Type, PyNone.Instance, PyNone.Instance),
            () => builder.GetAttr("missing"),
            () => Call(savedComment, "missing-global"),
            () => PySyntaxError.Construct("SyntaxError", [1L], []),
            () => { Call(builder.GetAttr("start"), "r", new PyDict()); Call(builder.GetAttr("end"), "r"); return Call(builder.GetAttr("start"), "second", new PyDict()); },
        ];
        PyTraceProduct.Enter(1); long depth = PyTraceback.PushFrame("treebuilder-producers.py", "caller"); PyTraceback.SetLine(17);
        try
        {
            foreach (var operation in operations)
            {
                var error = Assert.ThrowsAny<PyBaseException>(operation);
                try
                {
                    Assert.NotNull(error.TraceProductBinding);
                    Assert.Equal(TraceBeginKind.NativeFresh, error.TraceProductBinding!.Owner.Snapshot.BeginKind);
                    PyTraceProduct.Handle(error, depth);
                    Assert.Equal("caller", Assert.Single(error.Frames).Function);
                }
                finally { if (error.TraceProductBinding!.Owner.Snapshot.PendingCount is not null) PyTraceProduct.Suppress(error); }
            }
            var constructed = PySyntaxError.Construct("SyntaxError", ["not raised"], []);
            Assert.Null(constructed.TraceProductBinding);
        }
        finally { PyTraceback.Reset(); PyErrors.LeaveHandler(null); PyTraceProduct.Exit(); }
        long legacy = PyTraceback.PushFrame("treebuilder-producers.py", "legacy"); PyTraceback.SetLine(23);
        try
        {
            var original = new PyValueError("context"); object? previous = PyErrors.EnterHandler(original);
            try
            {
                var error = Assert.Throws<PyIndexError>(() => Call(builder.GetAttr("end"), "empty"));
                Assert.Null(error.TraceProductBinding); Assert.Equal("legacy", Assert.Single(error.Frames).Function);
                Assert.Same(original, error.Context);
            }
            finally { PyErrors.LeaveHandler(previous); }
        }
        finally { PyTraceback.PopTo(legacy); }
    }
}
#endif
