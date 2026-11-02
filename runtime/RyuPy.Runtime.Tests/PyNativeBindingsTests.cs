// 只验固定数值/错误边界，不加载原生库，也不把此专项当真实C ABI运行。
using System;
using System.Numerics;
using RyuPy.Runtime;
using Xunit;

namespace RyuPy.Runtime.Tests;

[Collection("生成器帧栈")]
public class PyNativeBindingsTests
{
    [Fact]
    public void ActualIntegerRepresentationsHaveExactSigned32Range()
    {
        object[] valid = [int.MinValue, -1, 0, int.MaxValue, (long)int.MinValue, 0L,
            (long)int.MaxValue, new BigInteger(int.MinValue), BigInteger.Zero, new BigInteger(int.MaxValue)];
        foreach (object value in valid)
        {
            int expected = value is BigInteger big ? (int)big : Convert.ToInt32(value);
            Assert.Equal(expected, PyNativeBindings.CheckedI32(value, "add", "left"));
        }
        object[] tooLarge = [(long)int.MinValue - 1, (long)int.MaxValue + 1,
            long.MinValue, long.MaxValue, BigInteger.One << 200, -(BigInteger.One << 200)];
        foreach (object value in tooLarge)
            Assert.Throws<PyOverflowError>(() => PyNativeBindings.CheckedI32(value, "add", "right"));
    }

    [Fact]
    public void NonIntegersAndUserConversionHooksAreRejectedWithoutCallingThem()
    {
        int called = 0;
        var cls = Assert.IsType<PyClass>(PyOps.MakeClass("CIntIndex", [], null));
        cls.SetAttr("__index__", PyNativeFunctions.Create("__index__", ["self"], [null], 1, _ => { called++; return 1L; }));
        cls.SetAttr("__int__", PyNativeFunctions.Create("__int__", ["self"], [null], 1, _ => { called++; return 1L; }));
        cls.SetAttr("__repr__", PyNativeFunctions.Create("__repr__", ["self"], [null], 1, _ => { called++; return "value"; }));
        object?[] invalid = [true, false, 1.0, 1.0m, "1", null, PyNone.Instance,
            (short)1, (uint)1, cls.Invoke([], [], [])];
        foreach (object? value in invalid)
            Assert.Throws<PyTypeError>(() => PyNativeBindings.CheckedI32(value, "add", "left"));
        Assert.Equal(0, called);
    }

    [Fact]
    public void TypeAndOverflowErrorsCaptureActualCallerAndContextImmediately()
    {
        var outer = new PyValueError("outer"); object? token = PyErrors.EnterHandler(outer);
        long depth = PyTraceback.PushFrame("binding_call.py", "caller");
        try
        {
            PyTraceback.SetLine(17);
            var type = Assert.Throws<PyTypeError>(() => PyNativeBindings.CheckedI32(true, "add", "left"));
            var range = Assert.Throws<PyOverflowError>(() => PyNativeBindings.CheckedI32(long.MaxValue, "add", "right"));
            foreach (PyBaseException error in new PyBaseException[] { type, range })
            {
                Assert.Same(outer, error.Context);
                Assert.Equal(new PyStackFrame("binding_call.py", "caller", 17), error.Frames[0]);
            }
            Assert.Same(outer, PyErrors.Current);
        }
        finally { PyTraceback.PopTo(depth); PyErrors.LeaveHandler(token); }
    }

    [Fact]
    public void OnlyThreeLoaderErrorsMapToPreparedImportErrorWithoutHostPaths()
    {
        var outer = new PyValueError("outer"); object? token = PyErrors.EnterHandler(outer);
        long depth = PyTraceback.PushFrame("binding_call.py", "load");
        try
        {
            PyTraceback.SetLine(29);
            Exception[] loaders = [new DllNotFoundException("/private/host/library"),
                new EntryPointNotFoundException("C:\\private\\host.dll"), new BadImageFormatException("secret-source-path")];
            foreach (Exception loader in loaders)
            {
                var error = Assert.IsType<PyImportError>(PyNativeBindings.LoadFailure(loader, "logical_math", "sum_i32"));
                Assert.Equal("cannot load declared native library 'logical_math' symbol 'sum_i32'", error.Message);
                Assert.DoesNotContain(loader.Message, error.Message);
                Assert.Same(outer, error.Context);
                Assert.Equal(new PyStackFrame("binding_call.py", "load", 29), error.Frames[0]);
            }
            var unrelated = new InvalidOperationException("must not map");
            Assert.Same(unrelated, Assert.Throws<InvalidOperationException>(() => PyNativeBindings.LoadFailure(unrelated, "lib", "symbol")));
            var python = new PyTypeError("original Python error");
            Assert.Same(python, Assert.Throws<PyTypeError>(() => PyNativeBindings.LoadFailure(python, "lib", "symbol")));
        }
        finally { PyTraceback.PopTo(depth); PyErrors.LeaveHandler(token); }
    }
}
