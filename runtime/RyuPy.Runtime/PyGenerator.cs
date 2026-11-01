using System;

namespace RyuPy.Runtime;

/// <summary>恢复编译器生成的状态机；返回本次 yield 的值，完成状态由帧首槽指示。</summary>
/// <param name="frame">每实例独立的状态及局部变量数组。</param>
public delegate object? PyResume(object frame);

/// <summary>普通 yield 的惰性迭代器；暂停期间不占用运行时 traceback 帧。</summary>
/// <remarks>
/// 首槽为 boxed long 程序计数器，非负数表示可恢复，-1 表示完成。
/// 槽 1 为发送值，槽 2 为注入异常，槽 3 为逻辑异常路由暂存；局部值从槽 4 开始。
/// 恢复循环吞掉内部 Retry 哨兵；只有真正 yield 才返回给调用者。
/// </remarks>
public sealed class PyGenerator : IPyIterator
{
    private PyResume? _resume;
    private object?[]? _frame;
    private readonly string _source;
    private readonly string _name;
    private object? _current;
    private bool _running;
    private bool _started;
    private readonly PyExceptionContext _contextRoot = new(null, null);
    private PyExceptionContext? _context;

    /// <summary>仅保存恢复入口和独立帧，不执行生成器函数体。</summary>
    /// <param name="resume">编译器生成的恢复入口。</param>
    /// <param name="frame">首槽为程序计数器的实例帧。</param>
    /// <param name="source">源码文件名。</param>
    /// <param name="name">Python 函数名。</param>
    public PyGenerator(PyResume resume, object?[] frame, string source, string name)
    {
        _resume = resume;
        _frame = frame;
        _source = source;
        _name = name;
        _context = _contextRoot;
    }

    /// <summary>执行到下一个 yield 或完成；None 是合法产出，不是耗尽哨兵。</summary>
    public bool MoveNext() => MoveNextCore(PyNone.Instance, out _);

    // 完成值只属于本次恢复；普通迭代直接丢弃，不能留给后续 next 领取。
    private bool MoveNextCore(object? sentValue, out object? returnValue, PyBaseException? injected = null)
    {
        returnValue = null;
        if (_running) throw new PyValueError("generator already executing");
        if (_frame is null) return false;
        // 首次非 None 发送只是调用错误，不启动也不关闭生成器。
        if (!_started && sentValue is not (null or PyNone))
            throw new PyTypeError("can't send non-None value to a just-started generator");
        _running = true;
        long depth = 0;
        bool enteredBody = false;
        PyExceptionContext? caller = PyErrors.ContextNode;
        _contextRoot.Parent = caller;
        PyErrors.ContextNode = _context;
        try
        {
            depth = PyTraceback.PushFrame(_source, _name);
            var frame = _frame;
            if (ReadState(frame) == -1) { Finish(); return false; }
            frame[1] = sentValue ?? PyNone.Instance;
            frame[2] = injected;
            // 新建生成器 throw 不执行函数体，连内部 try/finally 也尚未进入。
            if (!_started && injected is not null)
            {
                PyTraceback.Attach(injected);
                throw PyErrors.Prepare(injected);
            }
            _started = true;
            enteredBody = true;
            object? value;
            while (true)
            {
                value = _resume!(frame);
                if (!ReferenceEquals(value, PyOps.GeneratorRetry())) break;
                // 逻辑异常路由可能跳过被调用函数的正常弹帧，保留本生成器帧并清理其内层。
                PyTraceback.PopTo(depth + 1);
            }
            if (ReadState(frame) == -1)
            {
                returnValue = value;
                Finish();
                return false;
            }
            _current = value;
            return true;
        }
        catch (PyStopIteration stop) when (enteredBody)
        {
            // PEP 479：函数体泄漏的 StopIteration 是错误，不能假装正常耗尽。
            PyErrors.PrepareRaise(stop);
            var error = new PyRuntimeError("generator raised StopIteration").WithCause(stop);
            error.Context = stop;
            PyTraceback.Attach(error);
            Finish();
            throw error;
        }
        catch (PyBaseException error)
        {
            // 未进入逻辑handler的运行时错误也要保留当下上下文，之后才恢复caller。
            PyErrors.PrepareRaise(error);
            Finish();
            throw;
        }
        catch
        {
            // 即使 CLR 故障也不可再次进入已经失效的状态机。
            Finish();
            throw;
        }
        finally
        {
            if (depth != 0) PyTraceback.PopTo(depth);
            _context = _frame is null ? _contextRoot : PyErrors.ContextNode;
            _contextRoot.Parent = null;
            PyErrors.ContextNode = caller;
            _running = false;
        }
    }

    /// <summary>执行 Python next 协议，仅本次完成能观察生成器返回值。</summary>
    /// <param name="defaultValue">耗尽时返回的默认值。</param>
    /// <param name="hasDefault">是否提供默认值。</param>
    public object? Next(object? defaultValue, bool hasDefault)
    {
        if (MoveNextCore(PyNone.Instance, out object? returnValue)) return Current();
        if (hasDefault) return defaultValue;
        // 必须在恢复过程的异常转换区外抛出，正常完成不属于 PEP 479 错误。
        throw new PyStopIteration(returnValue);
    }

    /// <summary>向暂停的 yield 表达式发送值，恢复到下一次产出或完成。</summary>
    /// <param name="value">发送的 Python 对象；None 也可启动生成器。</param>
    public object? Send(object? value)
    {
        if (MoveNextCore(value, out object? returnValue)) return Current();
        throw new PyStopIteration(returnValue);
    }

    /// <summary>在暂停点注入异常；支持现代单参数异常实例或异常类型形式。</summary>
    /// <param name="exception">异常实例或异常类型。</param>
    public object? Throw(object? exception)
    {
        if (_running) throw new PyValueError("generator already executing");
        PyBaseException error = NormalizeThrown(exception);
        if (_frame is null) throw PyErrors.Prepare(error);
        if (MoveNextCore(PyNone.Instance, out object? value, error)) return Current();
        throw new PyStopIteration(value);
    }

    /// <summary>注入 GeneratorExit；若生成器仍产出则报错，但保留该暂停状态。</summary>
    public object Close()
    {
        if (_running) throw new PyValueError("generator already executing");
        if (_frame is null) return PyNone.Instance;
        if (!_started) { Finish(); return PyNone.Instance; }
        try
        {
            if (!MoveNextCore(PyNone.Instance, out _, new PyGeneratorExit())) return PyNone.Instance;
        }
        catch (PyGeneratorExit) { return PyNone.Instance; }
        // 此错误属于 close 协议，不是函数体错误，不能走 Finish 丢失暂停点。
        throw new PyRuntimeError("generator ignored GeneratorExit");
    }

    private static PyBaseException NormalizeThrown(object? exception)
    {
        object? normalized = exception switch
        {
            PyBaseException error => error,
            PyType { Name: "error" } => PyRegex.ConstructError([], [], []),
            PyType { Name: "JSONDecodeError" } => PyJsonModule.ConstructError([], [], []),
            PyType type when PyExceptionHierarchy.IsKnown(type.Name)
                => PyExceptionTypes.Create(type.Name, string.Empty),
            PyClass cls when cls.IsSubclassOfBuiltin("BaseException") => cls.Invoke([], [], []),
            _ => throw new PyTypeError("exceptions must be classes or instances deriving from BaseException"),
        };
        return normalized as PyBaseException
            ?? throw new PyTypeError("calling exception class did not return an exception instance");
    }

    /// <summary>读取最近一次成功恢复的产出。</summary>
    public object? Current() => _current;

    /// <summary>分派目前开放的生成器迭代协议方法。</summary>
    /// <param name="name">Python 方法名。</param>
    /// <param name="args">已展开的位置参数。</param>
    public object? CallMethod(string name, object?[] args)
    {
        if (name == "send")
        {
            if (args.Length != 1)
                throw new PyTypeError($"send() takes exactly one argument ({args.Length} given)");
            return Send(args[0]);
        }
        if (name == "throw")
        {
            if (args.Length != 1) throw new PyTypeError("throw() currently requires exactly one exception argument");
            return Throw(args[0]);
        }
        if (name == "close")
        {
            if (args.Length != 0) throw new PyTypeError("close() takes no arguments");
            return Close();
        }
        if (name is not ("__iter__" or "__next__"))
            throw new PyAttributeError($"'generator' object has no attribute '{name}'");
        if (args.Length != 0)
            throw new PyTypeError($"generator.{name}() takes no arguments ({args.Length} given)");
        if (name == "__iter__") return this;
        return Next(null, false);
    }

    private static long ReadState(object?[] frame)
    {
        if (frame.Length < 4 || frame[0] is not long state || state < -1)
            throw new PyRuntimeError("invalid generator frame state");
        return state;
    }

    private void Finish()
    {
        // 清空引用槽，避免已耗尽对象继续保活局部变量、闭包和恢复委托。
        if (_frame is not null) Array.Clear(_frame);
        _frame = null;
        _resume = null;
        _current = null;
        _context = _contextRoot;
    }
}
