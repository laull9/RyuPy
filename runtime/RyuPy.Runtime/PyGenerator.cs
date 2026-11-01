using System;

namespace RyuPy.Runtime;

/// <summary>恢复编译器生成的状态机；返回本次 yield 的值，完成状态由帧首槽指示。</summary>
/// <param name="frame">每实例独立的状态及局部变量数组。</param>
public delegate object? PyResume(object frame);

/// <summary>普通 yield 的惰性迭代器；暂停期间不占用运行时 traceback 帧。</summary>
/// <remarks>
/// 首槽为 boxed long 程序计数器，非负数表示可恢复，-1 表示完成。
/// 其余槽由 MIR 状态机保存局部值或共享 cell 引用；不复制 cell 内容。
/// 当前不提供 send、throw 和 close 协议。
/// </remarks>
public sealed class PyGenerator : IPyIterator
{
    private PyResume? _resume;
    private object?[]? _frame;
    private readonly string _source;
    private readonly string _name;
    private object? _current;
    private bool _running;

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
    }

    /// <summary>执行到下一个 yield 或完成；None 是合法产出，不是耗尽哨兵。</summary>
    public bool MoveNext() => MoveNextCore(out _);

    // 完成值只属于本次恢复；普通迭代直接丢弃，不能留给后续 next 领取。
    private bool MoveNextCore(out object? returnValue)
    {
        returnValue = null;
        if (_running) throw new PyValueError("generator already executing");
        if (_frame is null) return false;
        _running = true;
        long depth = 0;
        try
        {
            depth = PyTraceback.PushFrame(_source, _name);
            var frame = _frame;
            if (ReadState(frame) == -1) { Finish(); return false; }
            object? value = _resume!(frame);
            if (ReadState(frame) == -1)
            {
                returnValue = value;
                Finish();
                return false;
            }
            _current = value;
            return true;
        }
        catch (PyStopIteration stop)
        {
            // PEP 479：函数体泄漏的 StopIteration 是错误，不能假装正常耗尽。
            PyTraceback.Attach(stop);
            var error = new PyRuntimeError("generator raised StopIteration").WithCause(stop);
            error.Context = stop;
            PyTraceback.Attach(error);
            Finish();
            throw error;
        }
        catch (PyBaseException error)
        {
            PyTraceback.Attach(error);
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
            _running = false;
        }
    }

    /// <summary>执行 Python next 协议，仅本次完成能观察生成器返回值。</summary>
    /// <param name="defaultValue">耗尽时返回的默认值。</param>
    /// <param name="hasDefault">是否提供默认值。</param>
    public object? Next(object? defaultValue, bool hasDefault)
    {
        if (MoveNextCore(out object? returnValue)) return Current();
        if (hasDefault) return defaultValue;
        // 必须在恢复过程的异常转换区外抛出，正常完成不属于 PEP 479 错误。
        throw new PyStopIteration(returnValue);
    }

    /// <summary>读取最近一次成功恢复的产出。</summary>
    public object? Current() => _current;

    /// <summary>分派目前开放的生成器迭代协议方法。</summary>
    /// <param name="name">Python 方法名。</param>
    /// <param name="args">已展开的位置参数。</param>
    public object? CallMethod(string name, object?[] args)
    {
        if (name is "send" or "throw" or "close")
            throw new PyNotImplementedError($"generator.{name}() is not supported yet");
        if (name is not ("__iter__" or "__next__"))
            throw new PyAttributeError($"'generator' object has no attribute '{name}'");
        if (args.Length != 0)
            throw new PyTypeError($"generator.{name}() takes no arguments ({args.Length} given)");
        if (name == "__iter__") return this;
        return Next(null, false);
    }

    private static long ReadState(object?[] frame)
    {
        if (frame.Length == 0 || frame[0] is not long state || state < -1)
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
    }
}
