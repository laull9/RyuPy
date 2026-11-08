// 实际PyPic通用/缓存getter的单authority合同；读取私有scratch仅用于证明测试确实走到hot分支。
// 这不是生成产物hit计数，也不替代原B/新增同位点fixture的真实JIT与NativeAOT双流验收。
#if !TRACE_ADAPTER_TEST
using System;
using System.Linq;
using System.Reflection;
using RyuPy.Runtime;

namespace RyuPy.Runtime.Tests;

[Collection("生成器帧栈")]
public sealed class PyTraceProductPicTests
{
    [Theory]
    [InlineData(false, 3801)]
    [InlineData(true, 3802)]
    public void ColdAndHotGetterFailureForwardSameOwnerWithoutLegacyAttach(bool warm, int site)
    {
        Assert.NotEqual("0", Environment.GetEnvironmentVariable("RYUPY_PIC"));
        PyTraceback.Reset();
        PyErrors.LeaveHandler(null);
        PyAtexit.Clear();
        PySystem.Initialize("pic-product-tests.py", []);
        var scratch = (object?[][])typeof(PyPic).GetField("GetterScratch", BindingFlags.NonPublic | BindingFlags.Static)!.GetValue(null)!;
        Assert.Null(scratch[site]);
        bool fail = false;
        int calls = 0;
        var signature = PySignature.Create("getter", ["self"], ["pos"], ["0"]);
        PyTypeError? replacement = null;
        var getter = new PyFunction("getter", signature, (_, args, _, _) =>
        {
            Assert.IsType<PyInstance>(args[0]);
            calls++;
            long depth = PyTraceback.PushFrame("pic-product-tests.py", "getter");
            PyTraceback.SetLine(5);
            if (fail)
            {
                replacement = new PyTypeError("target failed");
                throw (PyBaseException)PyTraceProduct.Explicit(replacement);
            }
            PyTraceback.PopTo(depth);
            return PyOps.TypeObject("TypeError");
        }, []);
        var cls = (PyClass)PyOps.MakeClass("ProductPicTarget", [], null);
        cls.SetAttr("kind", new PyProperty(getter));
        var instance = (PyInstance)cls.Invoke([], [], [])!;
        if (warm)
        {
            Assert.IsType<PyType>(PyPic.GetAttr(site, instance, "kind"));
            Assert.Null(scratch[site]); // cold成功只填条目，尚未分配hot实参数组。
        }
        fail = true;
        PyTraceProduct.Enter(1);
        PyValueError? original = null;
        object? token = null;
        try
        {
            PyTraceback.PushFrame("pic-product-tests.py", "module");
            PyTraceback.SetLine(20);
            long generator = PyTraceback.PushFrame("pic-product-tests.py", "generate");
            PyTraceback.SetLine(11);
            original = new PyValueError("original");
            PyTraceProduct.Explicit(original);
            token = PyTraceProduct.Park(original, generator);
            var ambient = PyErrors.EnterHandler(original);
            PyTraceback.SetLine(12);
            var thrown = Assert.Throws<PyTypeError>(() => PyPic.GetAttr(site, instance, "kind"));
            Assert.Same(replacement, thrown);
            replacement = thrown;
            Assert.Equal(warm ? 2 : 1, calls);
            if (warm) Assert.Same(instance, scratch[site][0]);
            else Assert.Null(scratch[site]);
            Assert.Same(original, replacement.Context);
            PyTraceProduct.Forward(replacement, generator);
            PyTraceback.PopTo(generator + 1);
            PyTraceProduct.Abort(token); token = null;
            PyTraceProduct.Forward(replacement, generator); // 同真实边界只重放receipt。
            PyErrors.LeaveHandler(ambient);
            PyTraceProduct.EscapeGenerator(replacement, generator);
            PyTraceback.PopTo(generator);
            PyTraceProduct.Terminal(replacement);
            Assert.Equal(new[] { "module", "generate", "getter" }, replacement.TraceProductBinding!.Owner.Snapshot.History.Select(f => f.Function));
            Assert.Equal(new[] { 20, 12, 5 }, replacement.TraceProductBinding.Owner.Snapshot.History.Select(f => f.Line));
            Assert.Equal(new[] { 11 }, original.TraceProductBinding!.Owner.Snapshot.History.Select(f => f.Line));
        }
        finally
        {
            if (token is not null) PyTraceProduct.Abort(token);
            if (replacement?.TraceProductBinding?.Owner.Snapshot.PendingCount is not null) PyTraceProduct.Suppress(replacement);
            if (original?.TraceProductBinding?.Owner.Snapshot.PendingCount is not null) PyTraceProduct.Suppress(original);
            PyErrors.LeaveHandler(null);
            PyTraceback.Reset();
            PyTraceProduct.Exit();
        }
    }
}
#endif
