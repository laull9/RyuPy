// 原生 async def 的独立协议对象；函数体始终由编译器生成的 CIL 状态机恢复。
using System;

namespace RyuPy.Runtime;

/// <summary>不可普通迭代的惰性协程，完成、关闭与失败后禁止再次恢复。</summary>
public sealed class PyCoroutine : PyNativeObject
{
    private readonly PyGenerator _generator;
    private bool _started;
    private bool _running;
    private bool _awaiting;

    /// <summary>只保留独立状态机，创建时不执行 Python 函数体。</summary>
    public PyCoroutine(PyGenerator generator)
    {
        _generator = generator;
        generator.IsCoroutine = true;
    }
    /// <inheritdoc/>
    public override string TypeName => "coroutine";

    /// <summary>恢复协程；首次非法发送保留尚未启动的状态。</summary>
    public object? Send(object? value)
    {
        RequireResumable();
        if (!_started && value is not (null or PyNone))
            throw new PyTypeError("can't send non-None value to a just-started coroutine");
        _started = true;
        return Resume(() => _generator.Send(value));
    }

    /// <summary>沿显式异常路由注入原异常，跨暂停 finally 由生成的 CFG 执行。</summary>
    public object? Throw(object? error)
    {
        RequireResumable();
        return Resume(() => _generator.Throw(error));
    }

    /// <summary>关闭未启动协程不进入函数体；已暂停协程传播 GeneratorExit 并执行清理。</summary>
    public object Close()
    {
        if (_running) throw new PyValueError("coroutine already executing");
        return Resume(() => _generator.Close())!;
    }

    private void RequireResumable()
    {
        if (_running) throw new PyValueError("coroutine already executing");
        if (_generator.IsClosed) throw new PyRuntimeError("cannot reuse already awaited coroutine");
    }

    private object? Resume(Func<object?> operation)
    {
        _running = true;
        try { return operation(); }
        finally { _running = false; }
    }

    internal PyCoroutineIterator AwaitIterator(bool acquire, bool asyncNext = false)
    {
        // GET_ANEXT没有GET_AWAITABLE的占用拒绝；仍登记占用以保护随后普通await的检查。
        if (acquire && !asyncNext && _awaiting && !_generator.IsClosed)
            throw new PyRuntimeError("coroutine is being awaited already");
        if (acquire) _awaiting = true;
        return new PyCoroutineIterator(this, acquire);
    }
    internal void ReleaseAwait() => _awaiting = false;

    /// <summary>外部执行域显式 __await__ 使用同一个协程状态，且不把协程当作普通迭代器。</summary>
    public PyCoroutineIterator ForeignAwaitIterator() => AwaitIterator(false);

    /// <inheritdoc/>
    public override object? GetAttr(string name) => name switch
    {
        "send" => Method(name, ["value"], a => Send(a[0])),
        "throw" => Method(name, ["exception"], a => Throw(a[0])),
        "close" => Method(name, [], _ => Close()),
        "__await__" => Method(name, [], _ => AwaitIterator(false)),
        _ => throw Missing(name),
    };

    private static PyFunction Method(string name, string[] parameters, Func<object?[], object?> body) =>
        PyNativeFunctions.Create(name, parameters, new object?[parameters.Length], parameters.Length,
            body, Array.ConvertAll(parameters, _ => "posonly"));
}

/// <summary>__await__ 的独立迭代器，将 send/throw/close 转发到同一个协程状态。</summary>
public sealed class PyCoroutineIterator : PyNativeObject, IPyIterator
{
    private readonly PyCoroutine owner;
    private readonly bool acquired;
    private object? _current;

    internal PyCoroutineIterator(PyCoroutine owner, bool acquired)
    {
        this.owner = owner;
        this.acquired = acquired;
    }
    /// <inheritdoc/>
    public override string TypeName => "coroutine_wrapper";

    private object? Forward(Func<object?> operation)
    {
        try { return operation(); }
        catch
        {
            // 完成或异常传播解除本次 await 所有权；协程本身保留真实关闭状态。
            if (acquired) owner.ReleaseAwait();
            throw;
        }
    }
    /// <summary>推进一次，完成时保留 StopIteration.value。</summary>
    public object? Next() => Send(PyNone.Instance);

    /// <summary>转发发送值；完成或异常解除本次 await 占用。</summary>
    public object? Send(object? value) => Forward(() => owner.Send(value));

    /// <summary>把原异常注入同一协程。</summary>
    public object? Throw(object? error) => Forward(() => owner.Throw(error));

    /// <summary>确定结束已挂起的对端并解除占用。</summary>
    public object Close()
    {
        try { return owner.Close(); }
        finally { if (acquired) owner.ReleaseAwait(); }
    }
    // builtin next 必须保留本次 StopIteration.value，MoveNext 只适合普通 for 消费。
    internal object? Next(object? defaultValue, bool hasDefault)
    {
        try { return Next(); }
        catch (PyStopIteration) when (hasDefault) { return defaultValue; }
    }
    /// <inheritdoc/>
    public bool MoveNext()
    {
        try { _current = Next(); return true; }
        catch (PyStopIteration) { _current = null; return false; }
    }
    /// <inheritdoc/>
    public object? Current() => _current;
    /// <inheritdoc/>
    public override object? GetAttr(string name) => name switch
    {
        "__iter__" => PyNativeFunctions.Create(name, [], [], 0, _ => this),
        "__next__" => PyNativeFunctions.Create(name, [], [], 0, _ => Next()),
        "send" => PyNativeFunctions.Create(name, ["value"], [null], 1,
         a => Send(a[0]), ["posonly"]),
        "throw" => PyNativeFunctions.Create(name, ["exception"], [null], 1,
         a => Throw(a[0]), ["posonly"]),
        "close" => PyNativeFunctions.Create(name, [], [], 0, _ => Close()),
        _ => throw Missing(name),
    };
}
