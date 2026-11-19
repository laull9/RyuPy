// 生成器上下文的异常身份、PEP479、清理与装饰器重建测试，不依赖编译器内部 CFG。
using System;
using Xunit;

namespace RyuPy.Runtime.Tests;

public class PyContextManagerTests
{
    private static object Create(Func<PyGenerator> factory)
    {
        var function = PyNativeFunctions.Create("resource", [], [], 0, _ => factory());
        // 合成生成器函数有测试预定名，但没有真实源码行号。
        PyObject.SetAttr(function, "__qualname__", "resource");
        object? decorated = PyFunctoolsTests.Invoke(PyContextLib.CreateAttributes()["contextmanager"], [function]);
        return PyFunctoolsTests.Invoke(decorated, [])!;
    }
    private static PyGenerator Generator(PyResume resume) => new(resume, [0L, PyNone.Instance, null, null], "test.py", "resource");
    private static object? Method(object context, string name, params object?[] args) =>
        PyFunctoolsTests.Invoke(PyObject.GetAttr(context, name), args);

    [Fact]
    public void ExactlyOneYieldAndCleanupAreEnforced()
    {
        int cleanup = 0;
        object context = Create(() => Generator(raw =>
        {
            var frame = (object?[])raw;
            if ((long)frame[0]! == 0) { frame[0] = 1L; return 7L; }
            try { PyOps.GeneratorInput(frame); frame[0] = -1L; return null; }
            finally { cleanup++; }
        }));
        Assert.Equal(7L, Method(context, "__enter__"));
        Assert.Equal(false, Method(context, "__exit__", null, null, null)); Assert.Equal(1, cleanup);
        object empty = Create(() => Generator(raw => { ((object?[])raw)[0] = -1L; return null; }));
        Assert.Equal("generator didn't yield", Assert.Throws<PyRuntimeError>(() => Method(empty, "__enter__")).PyMessage);
        object twice = Create(() => Generator(raw =>
        {
            var frame = (object?[])raw; long state = (long)frame[0]!;
            if (state < 2) { frame[0] = state + 1; return state; }
            try { return PyOps.GeneratorInput(frame); } finally { cleanup++; }
        }));
        Method(twice, "__enter__");
        Assert.Equal("generator didn't stop", Assert.Throws<PyRuntimeError>(() => Method(twice, "__exit__", null, null, null)).PyMessage);
        Assert.Equal(2, cleanup);
    }

    [Fact]
    public void OriginalExceptionsAndPep479AreNotSuppressed()
    {
        foreach (PyBaseException error in new PyBaseException[] { new PyValueError("body"), new PyStopIteration("body"), new PyRuntimeError("body") })
        {
            object context = Create(() => Generator(raw =>
            {
                var frame = (object?[])raw;
                if ((long)frame[0]! == 0) { frame[0] = 1L; return null; }
                return PyOps.GeneratorInput(frame);
            }));
            Method(context, "__enter__");
            Assert.Equal(false, Method(context, "__exit__", PyObject.TypeOf(error), error, null));
        }
    }

    [Fact]
    public void SwallowedExceptionsAreSuppressedAndNewErrorsPropagate()
    {
        var original = new PyValueError("body"); var replacement = new PyTypeError("cleanup");
        foreach (bool swallow in new[] { true, false })
        {
            object context = Create(() => Generator(raw =>
            {
                var frame = (object?[])raw;
                if ((long)frame[0]! == 0) { frame[0] = 1L; return null; }
                try { PyOps.GeneratorInput(frame); }
                catch (PyValueError) { if (!swallow) throw replacement; }
                frame[0] = -1L; return null;
            }));
            Method(context, "__enter__");
            if (swallow) Assert.Equal(true, Method(context, "__exit__", PyObject.TypeOf(original), original, null));
            else Assert.Same(replacement, Assert.Throws<PyTypeError>(() => Method(context, "__exit__", PyObject.TypeOf(original), original, null)));
        }
    }

    [Fact]
    public void DecoratorRecreatesContextForEveryInvocation()
    {
        int entries = 0, exits = 0;
        object context = Create(() => Generator(raw =>
        {
            var frame = (object?[])raw;
            if ((long)frame[0]! == 0) { entries++; frame[0] = 1L; return null; }
            PyOps.GeneratorInput(frame); exits++; frame[0] = -1L; return null;
        }));
        var function = PyNativeFunctions.Create("body", ["x"], [null], 1, a => a[0]);
        PyObject.SetAttr(function, "__qualname__", "body");
        object? wrapper = PyFunctoolsTests.Invoke(context, [function]);
        Assert.Equal(1L, PyFunctoolsTests.Invoke(wrapper, [1L]));
        Assert.Equal(2L, PyFunctoolsTests.Invoke(wrapper, [2L]));
        Assert.Equal(2, entries); Assert.Equal(2, exits);
        Assert.Same(function, PyObject.GetAttr(wrapper, "__wrapped__"));
    }
    [Fact]
    public void YieldAfterThrowClosesAndCleanupErrorsTakePrecedence()
    {
        foreach (bool cleanupFails in new[] { false, true })
        {
            int closes = 0;
            object context = Create(() => Generator(raw =>
            {
                var frame = (object?[])raw; long state = (long)frame[0]!;
                if (state == 0) { frame[0] = 1L; return null; }
                if (state == 1)
                {
                    try { PyOps.GeneratorInput(frame); }
                    catch (PyValueError) { frame[0] = 2L; return "bad yield"; }
                }
                closes++;
                if (cleanupFails) throw new PyTypeError("cleanup failed");
                return PyOps.GeneratorInput(frame);
            }));
            Method(context, "__enter__"); var error = new PyValueError("body");
            if (cleanupFails)
                Assert.Equal("cleanup failed", Assert.Throws<PyTypeError>(() => Method(context, "__exit__", PyObject.TypeOf(error), error, null)).PyMessage);
            else
                Assert.Equal("generator didn't stop after throw()", Assert.Throws<PyRuntimeError>(() => Method(context, "__exit__", PyObject.TypeOf(error), error, null)).PyMessage);
            Assert.Equal(1, closes);
        }
    }

    [Fact]
    public void DecoratorFailureChainsBodyAndRestoresOuterHandler()
    {
        var outer = new PyKeyError("outer"); var body = new PyValueError("body");
        object? previous = PyErrors.EnterHandler(outer);
        try
        {
            object context = Create(() => Generator(raw =>
            {
                var frame = (object?[])raw; long state = (long)frame[0]!;
                if (state == 0) { frame[0] = 1L; return null; }
                if (state == 1)
                {
                    try { PyOps.GeneratorInput(frame); }
                    catch (PyValueError) { frame[0] = 2L; return "invalid"; }
                }
                return PyOps.GeneratorInput(frame);
            }));
            var function = PyFunctoolsTests.Function("body", (_, _, _, _) => throw body);
            object? wrapper = PyFunctoolsTests.Invoke(context, [function]);
            var failure = Assert.Throws<PyRuntimeError>(() => PyFunctoolsTests.Invoke(wrapper, []));
            Assert.Same(body, failure.Context); Assert.Same(outer, body.Context);
            Assert.Same(outer, PyErrors.Reraise());
        }
        finally { PyErrors.LeaveHandler(previous); }
    }

    [Fact]
    public void MissingYieldKeepsSuppressedStopIterationAsContext()
    {
        object context = Create(() => Generator(raw => { ((object?[])raw)[0] = -1L; return null; }));
        var failure = Assert.Throws<PyRuntimeError>(() => Method(context, "__enter__"));
        Assert.IsType<PyStopIteration>(failure.Context);
        Assert.Null(failure.Cause);
    }

}
