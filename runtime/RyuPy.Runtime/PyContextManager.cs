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
public sealed partial class PyGeneratorContext : PyNativeObject, IPyNativeCallable
{
    /// <summary>调用工厂获得生成器，但不恢复其函数体；公开属性也是协议实际读取的状态。</summary>
    public PyGeneratorContext(object? function, object?[] args, string[] names, object?[] values)
    {
        var keywords = new PyDict();
        for (int i = 0; i < names.Length; i++) keywords.Set(names[i], values[i]);
        _attributes.Set("gen", PyOps.CallValue(function, args, names, values));
        _attributes.Set("func", function);
        _attributes.Set("args", new PyTuple((object?[])args.Clone()));
        _attributes.Set("kwds", keywords);
        object? doc;
        try { doc = PyObject.GetAttr(function, "__doc__"); }
        catch (PyAttributeError) { doc = null; }
        _attributes.Set("__doc__", doc is null or PyNone ? "Helper for @contextmanager decorator." : doc);
    }
    /// <inheritdoc/>
    public override string TypeName => "_GeneratorContextManager";
    private object? Generator => GetAttr("gen");
    private object? Next() => Generator is PyGenerator gen ? gen.Next(null, false)
        : PyOps.CallBuiltin("next", [Generator], [], []);
    private object? Throw(PyBaseException error) => Generator is PyGenerator gen ? gen.Throw(error)
        : PyOps.CallValue(PyOps.PrepareMethod(Generator, "throw"), [error], [], []);
    private void Close()
    {
        if (Generator is PyGenerator gen) gen.Close();
        else PyOps.CallValue(PyOps.PrepareMethod(Generator, "close"), [], [], []);
    }
    private object? Enter()
    {
        // 与contextlib一致，顺序删除公开重建状态；失败时不启动生成器。
        DelAttr("args"); DelAttr("kwds"); DelAttr("func");
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
    public object? Invoke(object?[] args, string[] names, object?[] values) =>
        CallMethodFunction.Invoke([this, .. args], names, values);

    private object? Decorate(object? function)
    {
        var wrapper = PyContextLib.Forward("inner", (callArgs, callNames, callValues) =>
        {
            var context = Recreate();
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
