// contextmanager 将生成器异常注入协议适配为 with 协议；是否抑制必须按异常身份而不是类型判断。
using System;
using System.Collections.Generic;

namespace RyuPy.Runtime;

/// <summary>contextlib 原生首批入口。</summary>
public static class PyContextLib
{
    /// <summary>创建生成器上下文装饰器。</summary>
    public static Dictionary<string, object?> CreateAttributes() => new()
    {
        ["contextmanager"] = PyNativeFunctions.Create("contextmanager", ["func"], [null], 1, a => Decorate(a[0])),
    };
    private static object? Decorate(object? function)
    {
        var helper = Forward("helper", (args, names, values) => new PyGeneratorContext(function, args, names, values));
        return PyFunctools.UpdateWrapper(helper, function);
    }
    internal static PyFunction Forward(string name, Func<object?[], string[], object?[], object?> body) =>
        new(name, PySignature.Create(name, [], [], []), (_, args, names, values) => body(args, names, values), []);
}

/// <summary>每次 with 独占一个生成器；作为装饰器使用时每次调用重新创建上下文。</summary>
public sealed class PyGeneratorContext : PyNativeObject, IPyNativeCallable
{
    private object? _function;
    private object?[]? _args, _values;
    private string[]? _names;
    private readonly object? _generator;
    /// <summary>调用工厂获得生成器，但不恢复其函数体。</summary>
    public PyGeneratorContext(object? function, object?[] args, string[] names, object?[] values)
    {
        _function = function; _args = (object?[])args.Clone(); _names = (string[])names.Clone(); _values = (object?[])values.Clone();
        _generator = PyOps.CallValue(function, args, names, values);
    }
    /// <inheritdoc/>
    public override string TypeName => "_GeneratorContextManager";
    /// <inheritdoc/>
    public override object? GetAttr(string name) => name switch
    {
        "__enter__" => PyNativeFunctions.Create(name, [], [], 0, _ => Enter()),
        "__exit__" => PyNativeFunctions.Create(name, ["typ", "value", "traceback"], [null, null, null], 3,
            a => Exit(a[0], a[1]), ["posonly", "posonly", "posonly"]),
        "__call__" => this,
        _ => throw Missing(name),
    };
    private object? Next() => _generator is PyGenerator gen ? gen.Next(null, false)
        : PyOps.CallBuiltin("next", [_generator], [], []);
    private object? Throw(PyBaseException error) => _generator is PyGenerator gen ? gen.Throw(error)
        : PyOps.CallValue(PyOps.PrepareMethod(_generator, "throw"), [error], [], []);
    private void Close()
    {
        if (_generator is PyGenerator gen) gen.Close();
        else PyOps.CallValue(PyOps.PrepareMethod(_generator, "close"), [], [], []);
    }
    private object? Enter()
    {
        if (_args is null) throw new PyAttributeError("'_GeneratorContextManager' object has no attribute 'args'");
        _function = null; _args = null; _names = null; _values = null;
        try { return Next(); }
        catch (PyStopIteration stopped)
        {
            object? previous = PyErrors.EnterHandler(stopped);
            try { throw (PyBaseException)PyErrors.PrepareRaise(new PyRuntimeError("generator didn't yield").WithCause(null)); }
            finally { PyErrors.LeaveHandler(previous); }
        }
    }
    private bool Exit(object? type, object? value)
    {
        if (type is null or PyNone)
        {
            try { Next(); }
            catch (PyStopIteration) { return false; }
            FailAndClose("generator didn't stop");
            return false;
        }
        PyBaseException original = value as PyBaseException ?? type switch
        {
            PyType t when PyExceptionHierarchy.IsKnown(t.Name) => PyExceptionTypes.Create(t.Name, ""),
            PyClass cls => cls.Invoke([], [], []) as PyBaseException ?? throw new PyTypeError("exception required"),
            _ => throw new PyTypeError("exception required"),
        };
        PyStackFrame[] frames = original.Frames;
        try { Throw(original); }
        catch (PyStopIteration stopped) { return !ReferenceEquals(stopped, original); }
        catch (PyRuntimeError error)
        {
            if (ReferenceEquals(error, original) || original is PyStopIteration && ReferenceEquals(error.Cause, original))
            { original.Frames = frames; return false; }
            throw;
        }
        catch (PyBaseException error)
        {
            if (!ReferenceEquals(error, original)) throw;
            original.Frames = frames; return false;
        }
        FailAndClose("generator didn't stop after throw()");
        return false;
    }
    private void FailAndClose(string message)
    {
        var failure = new PyRuntimeError(message);
        PyErrors.PrepareRaise(failure);
        // Python 的 try/raise/finally 在执行 close 时仍以协议错误为当前异常。
        object? previous = PyErrors.EnterHandler(failure);
        try
        {
            try { Close(); }
            catch (PyBaseException cleanup) { PyErrors.PrepareRaise(cleanup); throw; }
        }
        finally { PyErrors.LeaveHandler(previous); }
        throw failure;
    }
    /// <summary>作为 ContextDecorator 使用时，包装函数每次调用独立重建上下文。</summary>
    public object? Invoke(object?[] args, string[] names, object?[] values)
    {
        if (args.Length != 1 || names.Length != 0) throw new PyTypeError("ContextDecorator.__call__ expects one function");
        object? function = args[0];
        var wrapper = PyContextLib.Forward("inner", (callArgs, callNames, callValues) =>
        {
            if (_args is null) throw new PyAttributeError("'_GeneratorContextManager' object has no attribute 'func'");
            var context = new PyGeneratorContext(_function, _args, _names!, _values!);
            context.Enter(); object? result;
            try { result = PyOps.CallValue(function, callArgs, callNames, callValues); }
            catch (PyBaseException error)
            {
                // CLR catch 不会自动设置 Python 当前异常；Exit/清理中的新异常必须链到函数体异常。
                PyErrors.PrepareRaise(error);
                object? previous = PyErrors.EnterHandler(error);
                try
                {
                    try
                    {
                        if (context.Exit(PyObject.TypeOf(error), error)) return PyNone.Instance;
                    }
                    catch (PyBaseException replacement) { PyErrors.PrepareRaise(replacement); throw; }
                    throw;
                }
                finally { PyErrors.LeaveHandler(previous); }
            }
            context.Exit(null, null); return result;
        });
        return PyFunctools.UpdateWrapper(wrapper, function);
    }
}
