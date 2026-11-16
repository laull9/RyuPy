// 来源只读接口不公开到Python；本组直接验证帧身份、异常清理和生成器暂停生命周期。
using System;
using System.Reflection;
using Xunit;

namespace RyuPy.Runtime.Tests;

[Collection("生成器帧栈")]
public class PyFunctionOriginTests
{
    private static PyFunction Function(object? module)
    {
        var result = new PyFunction("same_name", PySignature.Create("same_name", [], [], []),
            (_, _, _, _) => PyNone.Instance, []);
        PyOps.FunctionModule(result, module);
        return result;
    }

    // 仅测试检查私有持有槽；生产不使用反射，也不为测试增加Python公开读取接口。
    private static object?[] Origins() => (object?[])typeof(PyTraceback)
        .GetField("_origins", BindingFlags.NonPublic | BindingFlags.Static)!.GetValue(null)!;
    private static object? GeneratorOrigin(PyGenerator generator) => typeof(PyGenerator)
        .GetField("_origin", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(generator);
    private static object?[] Frame() => [0L, null, null, null];

    [Fact]
    public void CurrentOwnerKeepsIdentityAndSeesCallbackMutationWithoutProtocols()
    {
        long saved = PyTraceback.CaptureDepth();
        var poison = new Poison();
        var outer = Function(poison);
        var inner = Function("inner");
        try
        {
            PyTraceback.PushFrameWithOrigin("same.py", "same_name", outer);
            Assert.Same(poison, PyTraceback.GetCallerModule());
            long innerDepth = PyTraceback.PushFrameWithOrigin("same.py", "same_name", inner);
            Assert.Equal("inner", PyTraceback.GetCallerModule());
            // 模拟真实回调修改仍在执行的外层函数，弹帧后不能读旧module快照。
            PyObject.SetAttr(outer, "__module__", "changed");
            PyTraceback.PopTo(innerDepth);
            Assert.Equal("changed", PyTraceback.GetCallerModule());
            PyObject.DelAttr(outer, "__module__");
            Assert.Same(PyNone.Instance, PyTraceback.GetCallerModule());
            Assert.Equal(0, poison.Reads);
        }
        finally { PyTraceback.PopTo(saved + 1); }
    }

    [Fact]
    public void UnknownTopNeverBorrowsOuterOriginAndInvalidOriginFailsCold()
    {
        long saved = PyTraceback.CaptureDepth();
        try
        {
            PyTraceback.PushFrameWithOrigin("outer.py", "outer", Function("outer"));
            long unknown = PyTraceback.PushFrame("static.py", "static");
            Assert.Throws<PyNotImplementedError>(() => PyTraceback.GetCallerModule());
            Assert.Null(Origins()[(int)unknown - 1]);
            PyTraceback.PopTo(unknown);
            Assert.Equal("outer", PyTraceback.GetCallerModule());
            long invalid = PyTraceback.PushFrameWithOrigin("bad.py", "bad", new PyList());
            Assert.Throws<PyNotImplementedError>(() => PyTraceback.GetCallerModule());
            PyTraceback.PopTo(invalid);
            Assert.Equal("outer", PyTraceback.GetCallerModule());
        }
        finally { PyTraceback.PopTo(saved + 1); }
    }

    [Fact]
    public void ModuleExecutionMarkersAreFreshAndPreserveRawValue()
    {
        long saved = PyTraceback.CaptureDepth();
        var value = new Poison();
        object first = PyTraceback.CreateModuleExecutionSource(value);
        object retry = PyTraceback.CreateModuleExecutionSource(value);
        Assert.NotSame(first, retry);
        try
        {
            PyTraceback.PushFrameWithOrigin("entry.py", "<module>", first);
            Assert.Same(value, PyTraceback.GetCallerModule());
            Assert.Equal(0, value.Reads);
        }
        finally { PyTraceback.PopTo(saved + 1); }
    }

    [Fact]
    public void GrowthPopAndRecursionFailureKeepOwnerSlotsConsistent()
    {
        long saved = PyTraceback.CaptureDepth();
        var function = Function("deep");
        // 外部环境可把递归上限设得极大；先限测试预算，不能为测失败分配到该上限。
        int limit = (int)typeof(PyTraceback).GetField("RecursionLimit",
            BindingFlags.NonPublic | BindingFlags.Static)!.GetValue(null)!;
        Assert.InRange(limit, (int)saved + 141, 4096);
        try
        {
            for (int index = 0; index < 140; index++)
                PyTraceback.PushFrameWithOrigin("deep.py", "deep", function);
            Assert.Equal("deep", PyTraceback.GetCallerModule());
            long beforeInvalid = PyTraceback.CaptureDepth();
            PyTraceback.PopTo(0);
            PyTraceback.PopTo(beforeInvalid + 1);
            Assert.Equal(beforeInvalid, PyTraceback.CaptureDepth());
            PyTraceback.PopTo(saved + 2);
            Assert.Equal(saved + 1, PyTraceback.CaptureDepth());
            for (int index = (int)saved + 1; index < beforeInvalid; index++) Assert.Null(Origins()[index]);
            bool rejected = false;
            for (int attempt = 0; attempt <= limit; attempt++)
            {
                long before = PyTraceback.CaptureDepth();
                try { PyTraceback.PushFrameWithOrigin("deep.py", "deep", function); }
                catch (PyRecursionError)
                {
                    Assert.Equal(before, PyTraceback.CaptureDepth());
                    Assert.Same(function, Origins()[(int)before - 1]);
                    Assert.Throws<PyRecursionError>(PyTraceback.CheckDepth);
                    rejected = true;
                    break;
                }
            }
            Assert.True(rejected, "bounded recursion probe did not reach the configured limit");
        }
        finally { PyTraceback.PopTo(saved + 1); }
        for (int index = (int)saved; index < Origins().Length; index++) Assert.Null(Origins()[index]);
    }

    [Fact]
    public void ResetClearsOriginsAndNoFrameIsExplicitlyUnsupported()
    {
        Assert.Equal(0, PyTraceback.CaptureDepth());
        try
        {
            PyTraceback.PushFrameWithOrigin("reset.py", "reset", Function("reset"));
            PyTraceback.PushFrameWithOrigin("reset.py", "reset", Function("other"));
            PyTraceback.Reset();
            Assert.Equal(0, PyTraceback.CaptureDepth());
            foreach (object? origin in Origins()) Assert.Null(origin);
            Assert.Throws<PyNotImplementedError>(() => PyTraceback.GetCallerModule());
            PyTraceback.PushFrame("reuse.py", "plain");
            Assert.Throws<PyNotImplementedError>(() => PyTraceback.GetCallerModule());
        }
        finally { PyTraceback.Reset(); }
    }

    [Fact]
    public void GeneratorResumesActualOwnerAndFinishReleasesIt()
    {
        long saved = PyTraceback.CaptureDepth();
        var owner = Function("before");
        int calls = 0;
        var generator = new PyGenerator(raw =>
        {
            PyOps.GeneratorInput((object?[])raw);
            Assert.Equal(calls++ == 0 ? "before" : "after", PyTraceback.GetCallerModule());
            if (calls == 2) ((object?[])raw)[0] = -1L;
            return 7L;
        }, Frame(), "generator.py", "same_name", owner);
        try
        {
            Assert.Equal(7L, generator.Next(null, false));
            Assert.Equal(saved, PyTraceback.CaptureDepth());
            Assert.Same(owner, GeneratorOrigin(generator));
            PyObject.SetAttr(owner, "__module__", "after");
            Assert.Throws<PyStopIteration>(() => generator.Next(null, false));
            Assert.Null(GeneratorOrigin(generator));
            Assert.Equal(saved, PyTraceback.CaptureDepth());
            var unopened = new PyGenerator(_ => throw new Exception("must not run"), Frame(), "g.py", "g", owner);
            unopened.Close();
            Assert.Null(GeneratorOrigin(unopened));
            var failed = new PyGenerator(_ => throw new PyValueError("failed"), Frame(), "g.py", "g", owner);
            Assert.Throws<PyValueError>(() => failed.Next(null, false));
            Assert.Null(GeneratorOrigin(failed));
        }
        finally { PyTraceback.PopTo(saved + 1); }
    }

    [Fact]
    public void IgnoredGeneratorExitRetainsOwnerAndLegacyGeneratorIsUnknown()
    {
        long saved = PyTraceback.CaptureDepth();
        var owner = Function("owner");
        bool ignore = true;
        var generator = new PyGenerator(raw =>
        {
            Assert.Equal("owner", PyTraceback.GetCallerModule());
            try { PyOps.GeneratorInput((object?[])raw); }
            catch (PyGeneratorExit) when (ignore) { }
            return 1L;
        }, Frame(), "g.py", "g", owner);
        try
        {
            generator.Next(null, false);
            Assert.Throws<PyRuntimeError>(() => generator.Close());
            Assert.Same(owner, GeneratorOrigin(generator));
            ignore = false;
            generator.Close();
            Assert.Null(GeneratorOrigin(generator));
            var legacy = new PyGenerator(raw =>
            {
                Assert.Throws<PyNotImplementedError>(() => PyTraceback.GetCallerModule());
                ((object?[])raw)[0] = -1L;
                return PyNone.Instance;
            }, Frame(), "legacy.py", "legacy");
            Assert.Throws<PyStopIteration>(() => legacy.Next(null, false));
            Assert.Equal(saved, PyTraceback.CaptureDepth());
        }
        finally { PyTraceback.PopTo(saved + 1); }
    }

    private sealed class Poison : PyNativeObject
    {
        internal int Reads;
        public override string TypeName => "OriginPoison";
        public override object? GetAttr(string name)
        {
            Reads++;
            throw new PyValueError("origin protocols must not run");
        }
        public override string PythonRepr() => throw new PyValueError("origin repr must not run");
        public override string PythonStr() => throw new PyValueError("origin str must not run");
    }
}
