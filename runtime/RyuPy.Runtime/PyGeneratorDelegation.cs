namespace RyuPy.Runtime;

/// <summary>yield from 的一次委托状态；不持有父生成器帧。</summary>
internal sealed class PyGeneratorDelegation
{
    private IPyIterator? _iterator;
    private object? _target;
    private bool _started;
    private bool _done;
    internal object? Current { get; private set; }
    internal object? Result { get; private set; }

    internal PyGeneratorDelegation(object? source)
    {
        // __iter__ 只在建立委托时执行一次；用户适配器中的真实对象用于发送与异常协议。
        _iterator = PyIter.Create(source);
        _target = _iterator is PyInstanceIterator custom ? custom.Instance : _iterator;
    }

    internal bool Step(object frame)
    {
        if (frame is not object?[] slots || slots.Length < 4)
            throw new PyRuntimeError("invalid generator frame state");
        object? sent = slots[1];
        object? injected = slots[2];
        slots[1] = null;
        slots[2] = null;
        if (_done) return false;
        if (injected is not (null or PyNone or PyBaseException))
            throw new PyRuntimeError("invalid generator injected exception");

        // close 查找和调用都在终值捕获区外：close 抛 StopIteration 是函数体错误。
        if (injected is PyGeneratorExit exit)
        {
            if (_target is PyGenerator generator) generator.Close();
            else if (TryOptionalMethod("close", out object? close)) PyOps.CallValue(close, [], [], []);
            throw PrepareInjected(exit);
        }

        object? throwMethod = null;
        if (injected is PyBaseException error && _target is not PyGenerator)
        {
            // throw 属性查找失败时重抛原异常，而不是把缺失属性变成新的错误。
            // 属性 getter 自身的非 AttributeError 也不可被当作委托终值消费。
            if (!TryOptionalMethod("throw", out throwMethod)) throw PrepareInjected(error);
        }
        try
        {
            object? value;
            if (injected is PyBaseException exception)
                value = _target is PyGenerator generator ? generator.Throw(exception)
                    : PyOps.CallValue(throwMethod, [exception], [], []);
            else if (!_started || sent is null or PyNone)
                value = Next();
            else
                value = _target is PyGenerator generator ? generator.Send(sent)
                    : PyOps.CallValue(PyOps.PrepareMethod(_target, "send"), [sent], [], []);
            _started = true;
            Current = value;
            return true;
        }
        catch (PyStopIteration stop)
        {
            _done = true;
            Result = stop.Value;
            Current = null;
            _iterator = null;
            _target = null;
            return false;
        }
    }

    private object? Next()
    {
        if (_target is PyGenerator generator) return generator.Next(null, false);
        if (_target is PyInstance instance)
        {
            if (PyObject.TryMethod(instance, "__next__", [], out object? next)) return next;
            throw new PyTypeError($"'{instance.Class.Name}' object is not an iterator");
        }
        if (_iterator!.MoveNext()) return _iterator.Current();
        throw new PyStopIteration();
    }

    private bool TryOptionalMethod(string name, out object? method)
    {
        try { method = PyOps.PrepareMethod(_target, name); return true; }
        catch (PyAttributeError) { method = null; return false; }
    }

    private static PyBaseException PrepareInjected(PyBaseException exception)
    {
        PyTraceback.Attach(exception);
        return PyErrors.Prepare(exception);
    }
}

public static partial class PyOps
{
    /// <summary>建立 yield from 的单次迭代器委托。</summary>
    /// <param name="source">要委托的可迭代值。</param>
    public static object GeneratorDelegateStart(object? source) => new PyGeneratorDelegation(source);

    /// <summary>消费帧中的发送值或注入异常，返回是否产生一个委托值。</summary>
    /// <param name="frame">父生成器帧。</param>
    /// <param name="delegation">委托状态。</param>
    public static bool GeneratorDelegateStep(object frame, object delegation) => Delegation(delegation).Step(frame);

    /// <summary>读取本次委托产出。</summary>
    /// <param name="delegation">委托状态。</param>
    public static object GeneratorDelegateCurrent(object delegation) => Delegation(delegation).Current ?? PyNone.Instance;

    /// <summary>读取委托完成时的 StopIteration.value。</summary>
    /// <param name="delegation">委托状态。</param>
    public static object GeneratorDelegateResult(object delegation) => Delegation(delegation).Result ?? PyNone.Instance;

    private static PyGeneratorDelegation Delegation(object value) => value as PyGeneratorDelegation
        ?? throw new PyRuntimeError("invalid generator delegation state");
}
